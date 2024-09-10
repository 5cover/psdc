using Scover.Psdc.Pseudocode;
using Scover.Psdc.Messages;
using static Scover.Psdc.StaticAnalysis.SemanticNode;
using Scover.Psdc.Parsing;
using System.Diagnostics;
using System.Collections.Immutable;

namespace Scover.Psdc.StaticAnalysis;

public sealed partial class StaticAnalyzer
{
    private enum MainProgramStatus {
        NotYet,
        Inside,
        Seen,
    }
    readonly Messenger _msger;

    MainProgramStatus _mainProgramStatus;
    ValueOption<Symbol.Callable> _currentCallable;

    StaticAnalyzer(Messenger messenger) => _msger = messenger;

    void EvaluateCompilerDirective(Scope scope, Node.CompilerDirective compilerDirective)
    {
        _msger.Report(Message.SuggestionFeatureNotOfficial(compilerDirective.SourceTokens, "compiler directives"));
        switch (compilerDirective) {
        case Node.CompilerDirective.Assert cd: {
            GetComptimeValue<BooleanType, bool>(BooleanType.Instance, EvaluateExpression(scope, cd.Expression))
            .DropError(_msger.Report)
            .Tap(@true => {
                if (!@true) {
                    _msger.Report(Message.ErrorAssertionFailed(compilerDirective,
                        cd.Message.Bind(msgExpr => GetComptimeValue<StringType, string>(
                            StringType.Instance, EvaluateExpression(scope, msgExpr)).DropError(_msger.Report))));
                }
            });
            break;
        }
        case Node.CompilerDirective.EvalExpr cd: {
            _msger.Report(Message.DebugEvaluateExpression(cd.SourceTokens, EvaluateExpression(scope, cd.Expression).Value));
            break;
        }
        case Node.CompilerDirective.EvalType cd: {
            _msger.Report(Message.DebugEvaluateType(cd.SourceTokens, EvaluateType(scope, cd.Type)));
            break;
        }
        default:
            throw compilerDirective.ToUnmatchedException();
        }
    }

    public static Algorithm Analyze(Messenger messenger, Node.Algorithm root)
    {
        StaticAnalyzer a = new(messenger);

        MutableScope scope = new(null);

        Algorithm semanticAst = new(new(scope, root.SourceTokens), root.Title,
            root.Declarations.Select(d => a.AnalyzeDeclaration(scope, d)).WhereSome().ToArray());

        foreach (var callable in scope.GetSymbols<Symbol.Callable>().Where(c => !c.HasBeenDefined)) {
            messenger.Report(Message.ErrorCallableNotDefined(callable));
        }

        return semanticAst;
    }

    ValueOption<Declaration> AnalyzeDeclaration(MutableScope scope, Node.Declaration decl)
    {
        SemanticMetadata meta = new(scope, decl.SourceTokens);

        switch (decl) {
        case Node.Nop: {
            return new Nop(meta);
        }
        case Node.Declaration.Constant constant: {
            var type = EvaluateType(scope, constant.Type);
            var init = AnalyzeInitializer(scope, constant.Value, EvaluatedType.IsAssignableTo, type);
            if (init is Expression) {
                _msger.Report(Message.SuggestionFeatureNotOfficialScalarInitializers(init));
            }

            if (init.Value.Status is not ValueStatus.Comptime) {
                _msger.Report(Message.ErrorComptimeExpressionExpected(constant.Value.SourceTokens));
            }

            scope.AddOrError(_msger, new Symbol.Constant(constant.Name, constant.SourceTokens, type, init.Value));

            return new Declaration.Constant(meta, type, constant.Name, init);
        }
        case Node.Declaration.Function d: {
            var sig = AnalyzeSignature(scope, d.Signature);
            AddCallableDeclarationSymbol(scope, MakeSymbol(sig, DeclareParameter));
            return new Declaration.Callable(meta, sig);
        }
        case Node.Declaration.FunctionDefinition d: {
            MutableScope funcScope = new(scope);
            var sig = AnalyzeSignature(scope, d.Signature);

            var f = MakeSymbol(sig, DefineParameter(inScope: funcScope));
            AddCallableDefinitionSymbol(scope, f);

            _currentCallable = f;
            var sFuncDef = new Declaration.CallableDefinition(meta, sig, AnalyzeStatements(funcScope, d.Block));
            _currentCallable = default;

            return sFuncDef;
        }
        case Node.Declaration.MainProgram d: {
            if (_mainProgramStatus is not MainProgramStatus.NotYet) {
                _msger.Report(Message.ErrorRedefinedMainProgram(d));
            }
            _mainProgramStatus = MainProgramStatus.Inside;
            Declaration.MainProgram mp = new(meta, AnalyzeStatements(new(scope), d.Block));
            _mainProgramStatus = MainProgramStatus.Seen;
            return mp;
        }
        case Node.Declaration.Procedure d: {
            var sig = AnalyzeSignature(scope, d.Signature);
            AddCallableDeclarationSymbol(scope, MakeSymbol(sig, DeclareParameter));
            return new Declaration.Callable(meta, sig);
        }
        case Node.Declaration.ProcedureDefinition d: {
            MutableScope procScope = new(scope);
            var sig = AnalyzeSignature(scope, d.Signature);
            var p = MakeSymbol(sig, DefineParameter(inScope: procScope));
            AddCallableDefinitionSymbol(scope, p);
            _currentCallable = p;
            Declaration.CallableDefinition sProcDef = new(meta, sig, AnalyzeStatements(procScope, d.Block));
            _currentCallable = default;
            return sProcDef;
        }
        case Node.Declaration.TypeAlias d: {
            var type = EvaluateType(scope, d.Type);
            scope.AddOrError(_msger, new Symbol.TypeAlias(d.Name, d.SourceTokens, type));
            return new Declaration.TypeAlias(meta, d.Name, type);
        }
        case Node.CompilerDirective cd: {
            EvaluateCompilerDirective(scope, cd);
            return default;
        }
        default:
            throw decl.ToUnmatchedException();
        }
    }

    CallableSignature AnalyzeSignature(Scope scope, Node.FunctionSignature sig) => new(new(scope, sig.SourceTokens),
        sig.Name, AnalyzeParameters(scope, sig.Parameters), EvaluateType(scope, sig.ReturnType));

    CallableSignature AnalyzeSignature(Scope scope, Node.ProcedureSignature sig) => new(new(scope, sig.SourceTokens),
        sig.Name, AnalyzeParameters(scope, sig.Parameters), VoidType.Instance);

    static Symbol.Callable MakeSymbol(CallableSignature sig, Func<ParameterFormal, Symbol.Parameter> makeParameterSymbol)
     => new(sig.Name, sig.Meta.SourceTokens, sig.Parameters.Select(makeParameterSymbol).ToArray(), sig.ReturnType);

    ParameterFormal[] AnalyzeParameters(Scope scope, IEnumerable<Node.ParameterFormal> parameters)
     => parameters.Select(p => new ParameterFormal(new(scope, p.SourceTokens),
            p.Mode, p.Name, EvaluateType(scope, p.Type))).ToArray();

    ParameterActual[] AnalyzeParameters(Scope scope, IEnumerable<Node.ParameterActual> parameters)
     => parameters.Select(p => new ParameterActual(new(scope, p.SourceTokens),
            p.Mode, EvaluateExpression(scope, p.Value))).ToArray();

    void AddCallableDeclarationSymbol(MutableScope scope, Symbol.Callable sub)
    {
        if (!scope.TryAdd(sub, out var existingSymbol)) {
            if (existingSymbol is Symbol.Callable existingSub) {
                if (!sub.SemanticsEqual(existingSub)) {
                    _msger.Report(Message.ErrorSignatureMismatch(sub, existingSub));
                }
            } else {
                _msger.Report(Message.ErrorRedefinedSymbol(sub, existingSymbol));
            }
        }
    }

    void AddCallableDefinitionSymbol(MutableScope scope, Symbol.Callable sub)
    {
        if (scope.TryAdd(sub, out var existingSymbol)) {
            sub.MarkAsDefined();
        } else if (existingSymbol is Symbol.Callable existingSub) {
            if (existingSub.HasBeenDefined) {
                _msger.Report(Message.ErrorRedefinedSymbol(sub, existingSub));
            } else if (sub.SemanticsEqual(existingSub)) {
                existingSub.MarkAsDefined();
            } else {
                _msger.Report(Message.ErrorSignatureMismatch(sub, existingSub));
            }
        } else {
            _msger.Report(Message.ErrorRedefinedSymbol(sub, existingSymbol));
        }
    }

    Func<ParameterFormal, Symbol.Parameter> DefineParameter(MutableScope inScope)
     => param => {
         var symb = DeclareParameter(param);
         if (!inScope.TryAdd(symb, out var existingSymb)) {
             _msger.Report(Message.ErrorRedefinedSymbol(symb, existingSymb));
         }
         return symb;
     };

    static Symbol.Parameter DeclareParameter(ParameterFormal param)
     => new(param.Name, param.Meta.SourceTokens, param.Type, param.Mode);

    Statement[] AnalyzeStatements(MutableScope scope, IEnumerable<Node.Statement> statements) => statements.Select(s => AnalyzeStatement(scope, s)).WhereSome().ToArray();

    ValueOption<Statement> AnalyzeStatement(MutableScope scope, Node.Statement statement)
    {
        SemanticMetadata meta = new(scope, statement.SourceTokens);
        switch (statement) {
        case Node.Statement.ExpressionStatement e: {
            var expr = EvaluateExpression(scope, e.Expression);
            if (!expr.Value.Type.IsConvertibleTo(VoidType.Instance)) {
                _msger.Report(Message.SuggestionExpressionValueUnused(e.Expression));
            }
            return new Statement.ExpressionStatement(meta, expr);
        }
        case Node.Nop: {
            return new Nop(meta);
        }
        case Node.Statement.Alternative s: {
            return new Statement.Alternative(meta,
                new(new(scope, s.If.SourceTokens),
                    EvaluateExpression(scope, s.If.Condition, EvaluatedType.IsConvertibleTo, BooleanType.Instance),
                    AnalyzeStatements(new(scope), s.If.Block)
                ),
                s.ElseIfs.Select(elseIf => new Statement.Alternative.ElseIfClause(new(scope, elseIf.SourceTokens),
                    EvaluateExpression(scope, elseIf.Condition, EvaluatedType.IsConvertibleTo, BooleanType.Instance),
                    AnalyzeStatements(new(scope), elseIf.Block)
                )).ToArray(),
                @s.Else.Map(@else => new Statement.Alternative.ElseClause(new(scope, @else.SourceTokens),
                    AnalyzeStatements(new(scope), @else.Block)
                )));
        }
        case Node.Statement.Assignment s: {
            if (s.Target is Node.Expression.Lvalue.VariableReference varRef) {
                scope.GetSymbol<Symbol.Constant>(varRef.Name)
                .Tap(constant => _msger.Report(Message.ErrorConstantAssignment(s, constant)));
            }
            var target = EvaluateLvalue(scope, s.Target);
            var value = EvaluateExpression(scope, s.Value, EvaluatedType.IsAssignableTo, target.Value.Type);
            return new Statement.Assignment(meta, target, value);
        }
        case Node.Statement.Builtin.Assigner s: {
            return new Statement.Builtin.Assigner(meta,
                EvaluateLvalue(scope, s.ArgumentNomLog, EvaluatedType.IsConvertibleTo, FileType.Instance),
                EvaluateExpression(scope, s.ArgumentNomExt, EvaluatedType.IsConvertibleTo, StringType.Instance));
        }
        case Node.Statement.Builtin.Ecrire s: {
            return new Statement.Builtin.Ecrire(meta,
                EvaluateExpression(scope, s.ArgumentNomLog, EvaluatedType.IsConvertibleTo, FileType.Instance),
                EvaluateExpression(scope, s.ArgumentExpression));
        }
        case Node.Statement.Builtin.EcrireEcran s: {
            return new Statement.Builtin.EcrireEcran(meta,
                s.Arguments.Select(a => EvaluateExpression(scope, a)).ToArray());
        }
        case Node.Statement.Builtin.Fermer s: {
            return new Statement.Builtin.Fermer(meta,
                EvaluateExpression(scope, s.ArgumentNomLog, EvaluatedType.IsConvertibleTo, FileType.Instance));
        }
        case Node.Statement.Builtin.Lire s: {
            return new Statement.Builtin.Lire(meta,
                EvaluateExpression(scope, s.ArgumentNomLog, EvaluatedType.IsConvertibleTo, FileType.Instance),
                EvaluateLvalue(scope, s.ArgumentVariable));
        }
        case Node.Statement.Builtin.LireClavier s: {
            return new Statement.Builtin.LireClavier(meta,
                EvaluateLvalue(scope, s.ArgumentVariable));
        }
        case Node.Statement.Builtin.OuvrirAjout s: {
            return new Statement.Builtin.OuvrirAjout(meta,
                EvaluateExpression(scope, s.ArgumentNomLog, EvaluatedType.IsConvertibleTo, FileType.Instance));
        }
        case Node.Statement.Builtin.OuvrirEcriture s: {
            return new Statement.Builtin.OuvrirEcriture(meta,
                EvaluateExpression(scope, s.ArgumentNomLog, EvaluatedType.IsConvertibleTo, FileType.Instance));
        }
        case Node.Statement.Builtin.OuvrirLecture s: {
            return new Statement.Builtin.OuvrirLecture(meta,
                EvaluateExpression(scope, s.ArgumentNomLog, EvaluatedType.IsConvertibleTo, FileType.Instance));
        }
        case Node.Statement.DoWhileLoop s: {
            return new Statement.DoWhileLoop(meta,
                EvaluateExpression(scope, s.Condition, EvaluatedType.IsConvertibleTo, BooleanType.Instance),
                AnalyzeStatements(new(scope), s.Block));
        }
        case Node.Statement.ForLoop s: {
            var variant = EvaluateLvalue(scope, s.Variant);
            return new Statement.ForLoop(meta,
                variant,
                EvaluateExpression(scope, s.Start, EvaluatedType.IsAssignableTo, variant.Value.Type),
                EvaluateExpression(scope, s.End, EvaluatedType.IsConvertibleTo, variant.Value.Type),
                s.Step.Map(e => EvaluateExpression(scope, e, EvaluatedType.IsConvertibleTo, variant.Value.Type)),
                AnalyzeStatements(new(scope), s.Block));
        }
        case Node.Statement.LocalVariable s: {
            var type = EvaluateType(scope, s.Declaration.Type);
            var declaration = new VariableDeclaration(new(scope, s.Declaration.SourceTokens),
                s.Declaration.Names,
                type);
            var initializer = s.Value.Map(i => {
                var init = AnalyzeInitializer(scope, i, EvaluatedType.IsAssignableTo, type);
                if (init is Expression) {
                    _msger.Report(Message.SuggestionFeatureNotOfficialScalarInitializers(init));
                }
                return init;

            });

            foreach (var name in declaration.Names) {
                scope.AddOrError(_msger, new Symbol.LocalVariable(name, s.SourceTokens, type, initializer.Map(i => i.Value)));
            }

            return new Statement.LocalVariable(meta, declaration, initializer);
        }
        case Node.Statement.RepeatLoop s: {
            return new Statement.RepeatLoop(meta,
                EvaluateExpression(scope, s.Condition, EvaluatedType.IsConvertibleTo, BooleanType.Instance),
                AnalyzeStatements(new(scope), s.Block));
        }
        case Node.Statement.Return s: {
            return new Statement.Return(meta, _currentCallable.Match(
                func => EvaluateTypedValue(func.ReturnType),
                () => {
                    if (_mainProgramStatus is MainProgramStatus.Inside) {
                        return EvaluateTypedValue(IntegerType.Instance);
                    } else {
                        _msger.Report(Message.ErrorReturnInNonReturnable(s.SourceTokens));
                        return s.Value.Map(v => EvaluateExpression(scope, v));
                    }
                }));
            
            Option<Expression> EvaluateTypedValue(EvaluatedType expectedType) => s.Value
                .Map(v => EvaluateExpression(scope, v, EvaluatedType.IsConvertibleTo, expectedType))
                .Tap(none: () => {
                    if (!expectedType.IsConvertibleTo(VoidType.Instance)) {
                        _msger.Report(Message.ErrorReturnExpectsValue(s.SourceTokens, expectedType));
                    }
                });
        }
        case Node.Statement.Switch s: {
            var expr = EvaluateExpression(scope, s.Expression);
            if (expr.Value.Type.IsConvertibleTo(StringType.Instance)) {
                _msger.Report(Message.ErrorCannotSwitchOnString(s));
            }
            return new Statement.Switch(meta,
                expr,
                s.Cases.Select(c => {
                    var caseExpr = EvaluateExpression(scope, c.Value, EvaluatedType.IsConvertibleTo, expr.Value.Type);
                    if (caseExpr.Value.Status is not ValueStatus.Comptime) {
                        _msger.Report(Message.ErrorComptimeExpressionExpected(c.Value.SourceTokens));
                    }
                    return new Statement.Switch.Case(new(scope, c.SourceTokens), caseExpr, AnalyzeStatements(scope, c.Block));
                }).ToArray(),
                s.Default.Map(d => new Statement.Switch.DefaultCase(new(scope, d.SourceTokens), AnalyzeStatements(scope, d.Block))));
        }
        case Node.Statement.WhileLoop s: {
            return new Statement.WhileLoop(meta,
                EvaluateExpression(scope, s.Condition, EvaluatedType.IsConvertibleTo, BooleanType.Instance),
                AnalyzeStatements(new(scope), s.Block));
        }
        case Node.CompilerDirective cd: {
            EvaluateCompilerDirective(scope, cd);
            return default;
        }
        default: {
            throw statement.ToUnmatchedException();
        }
        }
    }

    Initializer AnalyzeInitializer(Scope scope, Node.Initializer initializer, TypeComparer typeComparer, EvaluatedType targetType)
    {
        SemanticMetadata meta = new(scope, initializer.SourceTokens);

        switch (initializer) {
        case Node.Expression expr:
            return EvaluateExpression(scope, expr, typeComparer, targetType);
        case Node.Initializer.Braced braced: {
            var (value, items) = Evaluate();
            return new Initializer.Braced(meta, items, value);

            (Value, IReadOnlyList<Initializer.Braced.Item>) Evaluate()
            {
                switch (targetType) {
                case ArrayType targetArrayType: {
                    // Deeply initializes each subobject to its default value.
                    var arrayValue = targetArrayType.DefaultValue; // todo: let this be a comptime value abstraction (couple bewteen type and comptime value that is implicitly convertible to Value)

                    var currentPath = Option.None<InitializerPath.Array>();

                    var sitems = AnalyzeItems(scope, braced, (item, sdes) => {
                        if (sdes.Length > 0) {
                            if (sdes[0] is Designator.Array first) {
                                currentPath = EvaluateArrayPath(scope, first, sdes.AsSpan()[1..], targetArrayType)
                                    .DropError(_msger.Report).Or(currentPath);
                            } else {
                                _msger.Report(Message.ErrorUnsupportedDesignator(sdes[0].Meta.SourceTokens, targetArrayType));
                            }
                        } else {
                            currentPath = currentPath.Match(
                                p => p.Advance(item.Value.SourceTokens),
                                () => InitializerPath.Array.OfFirstObject(targetArrayType, braced.Items[0].SourceTokens))
                            .DropError(_msger.Report);
                        }

                        return currentPath.Map(p => p.Type.ItemType);
                    }, sitem => currentPath.Bind(p => p.SetValue(
                                sitem.Meta.SourceTokens,
                                arrayValue.Status.ComptimeValue.Unwrap(),
                                sitem.Value.Value)
                            .DropError(_msger.Report))
                        .Tap(v => arrayValue = v)
                    ).ToArray();

                    return (arrayValue, sitems);
                }
                case StructureType targetStructType: {
                    var structValue = targetStructType.DefaultValue;

                    var currentPath = Option.None<InitializerPath.Structure>();

                    var sitems = AnalyzeItems(scope, braced, (item, sdes) => {
                        if (sdes.Length > 0) {
                            if (sdes[0] is Designator.Structure first) {
                                currentPath = EvaluateStructurePath(scope, first, sdes.AsSpan()[1..], targetStructType)
                                    .DropError(_msger.Report).Or(currentPath);
                            } else {
                                _msger.Report(Message.ErrorUnsupportedDesignator(sdes[0].Meta.SourceTokens, targetStructType));
                            }
                        } else {
                            currentPath = currentPath.Match(
                                p => p.Advance(item.Value.SourceTokens),
                                () => InitializerPath.Structure.OfFirstObject(targetStructType, braced.Items[0].SourceTokens))
                            .DropError(_msger.Report);
                        }

                        return currentPath.Map(p => p.Type.Components.Map[p.First.Component]);
                    }, sitem => currentPath.Bind(p => p.SetValue(sitem.Meta.SourceTokens, structValue.Status.ComptimeValue.Unwrap(), sitem.Value.Value)
                                           .DropError(_msger.Report))
                                .Tap(v => structValue = v))
                    .ToArray();

                    return (structValue, sitems);
                }
                default: {
                    _msger.Report(Message.ErrorUnsupportedInitializer(initializer.SourceTokens, targetType));
                    return (UnknownType.Inferred.DefaultValue, AnalyzeItems(scope, braced, (_, _) => Option.None<EvaluatedType>(), null).ToArray());
                }
                }
            }
        }
        default:
            throw initializer.ToUnmatchedException();
        }

        IEnumerable<Initializer.Braced.Item> AnalyzeItems(Scope scope, Node.Initializer.Braced braced,
            Func<Node.Initializer.Braced.ValuedItem, ImmutableArray<Designator>, Option<EvaluatedType>> itemTargetType,
            Action<Initializer.Braced.Item>? onItemAdded)
        {
            foreach (var item in braced.Items) {
                switch (item) {
                case Node.Initializer.Braced.ValuedItem i: {
                    var sdes = i.Designators
                    .Select(d => AnalyzeDesignator(scope, d).DropError(_msger.Report))
                    .WhereSome().ToImmutableArray();

                    if (itemTargetType(i, sdes) is { HasValue: true } t) {
                        var sinit = AnalyzeInitializer(scope, i.Value, typeComparer, t.Value);
                        var sitem = new Initializer.Braced.Item(new(scope, item.SourceTokens), sdes, sinit);
                        onItemAdded?.Invoke(sitem);
                        yield return sitem;
                    }
                    break;
                }
                case Node.CompilerDirective cd: {
                    EvaluateCompilerDirective(scope, cd);
                    break;
                }
                default:
                    throw item.ToUnmatchedException();
                }
            }
        }
    }

    Option<Designator, Message> AnalyzeDesignator(Scope scope, Node.Designator designator) => designator switch {
        Node.Designator.Array a => AnalyzeArrayDesignator(scope, a),
        Node.Designator.Structure s => AnalyzeStructureDesignator(scope, s).Some<Designator, Message>(),
        _ => throw designator.ToUnmatchedException(),
    };

    ValueOption<Designator.Array, Message> AnalyzeArrayDesignator(Scope scope, Node.Designator.Array designator)
     => GetComptimeExpression<IntegerType, int>(IntegerType.Instance, scope, designator.Index)
        .Map(i => new Designator.Array(new(scope, designator.SourceTokens), i));

    static Designator.Structure AnalyzeStructureDesignator(Scope scope, Node.Designator.Structure designator)
     => new(new(scope, designator.SourceTokens), designator.Component);

    Expression EvaluateExpression(Scope scope, Node.Expression expr, TypeComparer typeComparer, EvaluatedType targetType)
     => EvaluateExpression(scope, expr, v => CheckType(expr.SourceTokens, typeComparer, targetType, v));

    Expression EvaluateExpression(Scope scope, Node.Expression expr, Func<Value, Value>? adjustValue = null)
    {
        adjustValue ??= v => v;
        SemanticMetadata meta = new(scope, expr.SourceTokens);
        switch (expr) {
        case Node.Expression.Literal lit: {
            var value = lit.CreateValue();
            return new Expression.Literal(meta, lit.Value, adjustValue(value));
        }
        case Node.Expression.Bracketed b: {
            var contained = EvaluateExpression(scope, b.ContainedExpression);
            return new Expression.Bracketed(meta, contained, adjustValue(contained.Value));
        }
        case Node.Expression.BinaryOperation opBin: {
            var left = EvaluateExpression(scope, opBin.Left);
            var right = EvaluateExpression(scope, opBin.Right);

            var result = opBin.Operator.EvaluateOperation(left.Value, right.Value);
            foreach (var msg in result.Messages) {
                _msger.Report(msg(opBin, left.Value.Type, right.Value.Type));
            }

            return new Expression.BinaryOperation(meta, left, AnalyzeOperator(scope, opBin.Operator), right, adjustValue(result.Value));
        }
        case Node.Expression.UnaryOperation opUn: {
            var operand = EvaluateExpression(scope, opUn.Operand);

            var result = this.EvaluateOperation(scope, opUn.Operator, operand.Value);
            foreach (var msg in result.Messages) {
                _msger.Report(msg(opUn, operand.Value.Type));
            }

            return new Expression.UnaryOperation(meta, AnalyzeOperator(scope, opUn.Operator), operand, adjustValue(result.Value));
        }
        case Node.Expression.Call call: {
            var parameters = AnalyzeParameters(scope, call.Parameters);

            var callable = scope.GetSymbol<Symbol.Callable>(call.Callee).DropError(_msger.Report).Tap(callable => {
                List<string> problems = [];

                if (call.Parameters.Count != callable.Parameters.Count) {
                    problems.Add(Message.ProblemWrongNumberOfArguments(
                        callable.Parameters.Count, call.Parameters.Count));
                }

                foreach (var (actual, formal) in parameters.Zip(callable.Parameters)) {
                    if (actual.Mode != formal.Mode) {
                        problems.Add(Message.ProblemWrongArgumentMode(formal.Name,
                            formal.Mode.RepresentationActual, actual.Mode.RepresentationActual));
                    }

                    var actualType = actual.Value.Value.Type;
                    if (!actualType.IsConvertibleTo(formal.Type)) {
                        problems.Add(Message.ProblemWrongArgumentType(formal.Name, formal.Type, actualType));
                    }
                }

                if (problems.Count > 0) {
                    _msger.Report(Message.ErrorCallParameterMismatch(call.SourceTokens, callable, problems));
                }
            }, none: () => {
                // If the callable symbol wasn't found, still analyze the parameter expressions.
                foreach (var actual in call.Parameters) {
                    EvaluateExpression(scope, actual.Value);
                }
            });

            return new Expression.Call(meta, call.Callee, parameters,
                adjustValue(callable
                    .Map(f => f.ReturnType)
                    .ValueOr(UnknownType.Inferred)
                    .RuntimeValue));
        }
        case Node.Expression.BuiltinFdf fdf: {
            return new Expression.BuiltinFdf(meta,
                EvaluateExpression(scope, fdf.ArgumentNomLog, EvaluatedType.IsConvertibleTo, FileType.Instance),
                adjustValue(BooleanType.Instance.RuntimeValue));
        }
        case Node.Expression.Lvalue lvalue: {
            return EvaluateLvalue(scope, lvalue, adjustValue);
        }
        default:
            throw expr.ToUnmatchedException();
        }
    }

    Expression.Lvalue EvaluateLvalue(Scope scope, Node.Expression.Lvalue expr, TypeComparer typeComparer, EvaluatedType targetType)
     => EvaluateLvalue(scope, expr, v => CheckType(expr.SourceTokens, typeComparer, targetType, v));

    Expression.Lvalue EvaluateLvalue(Scope scope, Node.Expression.Lvalue lvalue, Func<Value, Value>? adjustValue = null)
    {
        adjustValue ??= v => v;
        SemanticMetadata meta = new(scope, lvalue.SourceTokens);
        switch (lvalue) {
        case Node.Expression.Lvalue.ArraySubscript arrSub: {
            var index = EvaluateExpression(scope, arrSub.Index);

            var array = EvaluateExpression(scope, arrSub.Array);
            return new Expression.Lvalue.ArraySubscript(meta, array, index, adjustValue(Evaluate()));

            Value Evaluate()
            {
                if (array.Value is ArrayValue arrVal) {
                    ValueOption<int> actualIndex;

                    if (index.Value is IntegerValue intVal) {
                        actualIndex = intVal.Status.ComptimeValue;
                    } else {
                        _msger.Report(Message.ErrorNonIntegerIndex(index.Meta.SourceTokens, index.Value.Type));
                        return arrVal.Type.ItemType.InvalidValue;
                    }

                    var length = arrVal.Type.Length.Value;

                    if (actualIndex is { HasValue: true } i && !i.Value.Indexes(length, 1)) {
                        _msger.Report(Message.ErrorIndexOutOfBounds(arrSub.SourceTokens, i.Value, length));
                        return arrVal.Type.ItemType.InvalidValue;
                    }

                    return arrVal.Status.ComptimeValue.Zip(actualIndex)
                        .Map((arr, index) => arr[index - 1])
                        .ValueOr(arrVal.Type.ItemType.RuntimeValue);
                } else {
                    _msger.Report(Message.ErrorSubscriptOfNonArray(arrSub, array.Value.Type));
                    return UnknownType.Inferred.InvalidValue;
                }
            }
        }
        case Node.Expression.Lvalue.Bracketed b: {
            var contained = EvaluateLvalue(scope, b.ContainedLvalue);
            return new Expression.Lvalue.Bracketed(meta, contained, adjustValue(contained.Value));
        }
        case Node.Expression.Lvalue.ComponentAccess compAccess: {
            var @struct = EvaluateExpression(scope, compAccess.Structure);
            return new Expression.Lvalue.ComponentAccess(meta, @struct, compAccess.ComponentName, adjustValue(Evaluate()));

            Value Evaluate()
            {
                if (@struct.Value is not StructureValue structVal) {
                    _msger.Report(Message.ErrrorComponentAccessOfNonStruct(compAccess, @struct.Value.Type));
                } else if (!structVal.Type.Components.Map.TryGetValue(compAccess.ComponentName, out var componentType)) {
                    _msger.Report(Message.ErrorStructureComponentDoesntExist(compAccess.ComponentName, structVal.Type));
                } else {
                    return structVal.Status.ComptimeValue
                        .Map(s => s.Map[compAccess.ComponentName])
                        .ValueOr(componentType.RuntimeValue);
                }
                return UnknownType.Inferred.InvalidValue;
            }
        }
        case Node.Expression.Lvalue.VariableReference varRef: {
            return new Expression.Lvalue.VariableReference(meta, varRef.Name,
                adjustValue(scope.GetSymbol<Symbol.Variable>(varRef.Name)
                    .DropError(_msger.Report)
                    .Map(vp => vp is Symbol.Constant constant
                        ? constant.Value
                        : vp.Type.RuntimeValue)
                    .ValueOr(UnknownType.Inferred.InvalidValue)));
        }
        default:
            throw lvalue.ToUnmatchedException();
        }
    }

    static BinaryOperator AnalyzeOperator(Scope scope, Node.BinaryOperator binOp)
    {
        SemanticMetadata meta = new(scope, binOp.SourceTokens);
        return binOp switch {
            Node.BinaryOperator.Add => new BinaryOperator.Add(meta),
            Node.BinaryOperator.And => new BinaryOperator.And(meta),
            Node.BinaryOperator.Divide => new BinaryOperator.Divide(meta),
            Node.BinaryOperator.Equal => new BinaryOperator.Equal(meta),
            Node.BinaryOperator.GreaterThan => new BinaryOperator.GreaterThan(meta),
            Node.BinaryOperator.GreaterThanOrEqual => new BinaryOperator.GreaterThanOrEqual(meta),
            Node.BinaryOperator.LessThan => new BinaryOperator.LessThan(meta),
            Node.BinaryOperator.LessThanOrEqual => new BinaryOperator.LessThanOrEqual(meta),
            Node.BinaryOperator.Mod => new BinaryOperator.Mod(meta),
            Node.BinaryOperator.Multiply => new BinaryOperator.Multiply(meta),
            Node.BinaryOperator.NotEqual => new BinaryOperator.NotEqual(meta),
            Node.BinaryOperator.Or => new BinaryOperator.Or(meta),
            Node.BinaryOperator.Subtract => new BinaryOperator.Subtract(meta),
            Node.BinaryOperator.Xor => new BinaryOperator.Xor(meta),
            _ => throw binOp.ToUnmatchedException(),
        };
    }

    Value CheckType(SourceTokens context, TypeComparer typeComparer, EvaluatedType targetType, Value value)
    {
        if (typeComparer(value.Type, targetType)) {
            return value;
        }
        _msger.Report(Message.ErrorExpressionHasWrongType(context, targetType, value.Type));
        return targetType.InvalidValue;
    }

    UnaryOperator AnalyzeOperator(Scope scope, Node.UnaryOperator unOp)
    {
        SemanticMetadata meta = new(scope, unOp.SourceTokens);
        switch (unOp) {
        case Node.UnaryOperator.Cast op: {
            _msger.Report(Message.SuggestionFeatureNotOfficial(op.SourceTokens, "type casts"));
            return new UnaryOperator.Cast(meta, EvaluateType(scope, op.Target));
        }
        case Node.UnaryOperator.Minus: {
            return new UnaryOperator.Minus(meta);
        }
        case Node.UnaryOperator.Not: {
            return new UnaryOperator.Not(meta);
        }
        case Node.UnaryOperator.Plus: {
            return new UnaryOperator.Plus(meta);
        }
        default:
            throw unOp.ToUnmatchedException();
        };
    }

    internal EvaluatedType EvaluateType(Scope scope, Node.Type type) => type switch {
        Node.Type.AliasReference alias
         => scope.GetSymbol<Symbol.TypeAlias>(alias.Name).DropError(_msger.Report)
                .Map(aliasType => aliasType.Type.ToAliasReference(alias.Name))
            .ValueOr(UnknownType.Declared(_msger.Input, type)),
        Node.Type.Array array => EvaluateArrayType(scope, array),
        Node.Type.Boolean => BooleanType.Instance,
        Node.Type.Character => CharacterType.Instance,
        Node.Type.File file => FileType.Instance,
        Node.Type.Integer p => IntegerType.Instance,
        Node.Type.LengthedString str => EvaluateLengthedStringType(scope, str),
        Node.Type.Real p => RealType.Instance,
        Node.Type.Structure structure => EvaluateStructureType(scope, structure),
        Node.Type.String => StringType.Instance,
        _ => throw type.ToUnmatchedException(),
    };

    EvaluatedType EvaluateLengthedStringType(Scope scope, Node.Type.LengthedString str)
     => GetComptimeExpression<IntegerType, int>(IntegerType.Instance, scope, str.Length)
        .DropError(_msger.Report)
        .Map(LengthedStringType.Create)
        .ValueOr<EvaluatedType>(UnknownType.Declared(_msger.Input, str));

    EvaluatedType EvaluateArrayType(Scope scope, Node.Type.Array array)
     => array.Dimensions.Select(d => GetComptimeExpression<IntegerType, int>(IntegerType.Instance, scope, d)).Sequence()
        .DropError(Function.Foreach<Message>(_msger.Report))
        .Map(dimensions => {
            var type = EvaluateType(scope, array.Type);
            Debug.Assert(dimensions.Any());
            // Start innermost with the least significant dimension
            foreach (var dim in dimensions.Reverse()) {
                type = new ArrayType(type, dim);
            }
            return type;
        })
        .ValueOr(UnknownType.Declared(_msger.Input, array));

    static ValueOption<TUnderlying, Message> GetComptimeValue<TType, TUnderlying>(TType expectedType, Expression expr)
    where TType : EvaluatedType
     => expr is { Value: Value<TType, TUnderlying> v }
        ? v.Status.ComptimeValue.OrWithError(Message.ErrorComptimeExpressionExpected(expr.Meta.SourceTokens))
        : Message.ErrorExpressionHasWrongType(expr.Meta.SourceTokens, expectedType, expr.Value.Type);

    Option<ComptimeExpression<TUnderlying>, Message> GetComptimeExpression<TType, TUnderlying>(TType type, Scope scope, Node.Expression expr)
    where TType : EvaluatedType
    {
        var sexpr = EvaluateExpression(scope, expr);
        return sexpr.Value is Value<TType, TUnderlying> tval
            ? tval.Status.ComptimeValue.Map(v =>
                ComptimeExpression.Create(sexpr, v))
                .OrWithError(Message.ErrorComptimeExpressionExpected(expr.SourceTokens))
            : Message.ErrorExpressionHasWrongType(expr.SourceTokens, type, sexpr.Value.Type).None<ComptimeExpression<TUnderlying>, Message>();
    }

    StructureType EvaluateStructureType(Scope scope, Node.Type.Structure structure)
    {
        var components = ImmutableOrderedMap<Identifier, EvaluatedType>.Empty;
        foreach (var c in structure.Components) {
            switch (c) {
            case Node.VariableDeclaration comp: {
                foreach (var name in comp.Names) {
                    var type = EvaluateType(scope, comp.Type);
                    if (!components.TryAdd(name, type, out components)) {
                        _msger.Report(Message.ErrorStructureDuplicateComponent(comp.SourceTokens, name));
                    }
                }
                break;
            }
            case Node.CompilerDirective cd: {
                EvaluateCompilerDirective(scope, cd);
                break;
            }
            default:
                throw c.ToUnmatchedException();
            }
        }
        return new StructureType(components);
    }

    delegate bool TypeComparer(EvaluatedType actual, EvaluatedType expected);
}
