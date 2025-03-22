using Scover.Psdc.Pseudocode;
using Scover.Psdc.Messages;

using static Scover.Psdc.StaticAnalysis.SemanticNode;

using Scover.Psdc.Parsing;

using System.Diagnostics;
using System.Collections.Immutable;

namespace Scover.Psdc.StaticAnalysis;

public sealed partial class StaticAnalyzer
{
    enum MainProgramStatus
    {
        NotYet,
        Inside,
        Seen,
    }
    readonly Messenger _msger;
    readonly string _input;

    MainProgramStatus _mainProgramStatus;
    ValueOption<Symbol.Callable> _currentCallable;

    StaticAnalyzer(Messenger messenger, string input) => (_msger, _input) = (messenger, input);

    void EvaluateCompilerDirective(Scope scope, Node.CompilerDirective compilerDirective)
    {
        _msger.Report(Message.HintUnofficialFeature(compilerDirective.Location, "compiler directives"));
        switch (compilerDirective) {
        case Node.CompilerDirective.Assert cd: {
            GetComptimeValue<BooleanType, bool>(BooleanType.Instance, EvaluateExpression(scope, cd.Expr))
               .DropError(_msger.Report)
               .Tap(@true => {
                    if (!@true) {
                        _msger.Report(Message.ErrorAssertionFailed(compilerDirective.Location,
                            cd.Message.Bind(msgExpr => GetComptimeValue<StringType, string>(
                                StringType.Instance, EvaluateExpression(scope, msgExpr)).DropError(_msger.Report))));
                    }
                });
            break;
        }
        case Node.CompilerDirective.EvalExpr cd: {
            _msger.Report(Message.DebugEvaluateExpression(cd.Location, EvaluateExpression(scope, cd.Expr).Value));
            break;
        }
        case Node.CompilerDirective.EvalType cd: {
            _msger.Report(Message.DebugEvaluateType(cd.Location, EvaluateType(scope, cd.Type)));
            break;
        }
        default: throw compilerDirective.ToUnmatchedException();
        }
    }

    public static Algorithm Analyze(Messenger messenger, string input, Node.Algorithm ast)
    {
        StaticAnalyzer a = new(messenger, input);

        MutableScope scope = new(null);

        foreach (var directive in ast.LeadingDirectives) {
            a.EvaluateCompilerDirective(scope, directive);
        }

        Algorithm semanticAst = new(new(scope, ast.Location), ast.Title,
            ast.Declarations.Select(d => a.AnalyzeDeclaration(scope, d)).WhereSome().ToArray());

        foreach (var callable in scope.GetSymbols<Symbol.Callable>().Where(c => !c.HasBeenDefined)) {
            messenger.Report(Message.ErrorCallableNotDefined(callable));
        }

        return semanticAst;
    }

    ValueOption<Declaration> AnalyzeDeclaration(MutableScope scope, Node.Declaration decl)
    {
        SemanticMetadata meta = new(scope, decl.Location);

        switch (decl) {
        case Node.Nop: {
            return new Nop(meta);
        }
        case Node.Declaration.Constant constant: {
            var type = EvaluateType(scope, constant.Type);
            var init = AnalyzeInitializer(scope, constant.Value, EvaluatedType.IsAssignableTo, type);
            if (init is Expr) {
                _msger.Report(Message.HintUnofficialFeatureScalarInitializers(constant.Value.Location));
            }

            if (init.Value.Status is not ValueStatus.Comptime) {
                _msger.Report(Message.ErrorComptimeExpressionExpected(constant.Value.Location));
            }

            scope.AddOrError(_msger, new Symbol.Constant(constant.Name, constant.Location, type, init.Value));

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
            var sFuncDef = new Declaration.CallableDefinition(meta, sig, AnalyzeStatements(funcScope, d.Body));
            _currentCallable = default;

            return sFuncDef;
        }
        case Node.Declaration.MainProgram d: {
            if (_mainProgramStatus is not MainProgramStatus.NotYet) {
                _msger.Report(Message.ErrorRedefinedMainProgram(d.Location));
            }
            _mainProgramStatus = MainProgramStatus.Inside;
            Declaration.MainProgram mp = new(meta, AnalyzeStatements(new(scope), d.Body));
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
            Declaration.CallableDefinition sProcDef = new(meta, sig, AnalyzeStatements(procScope, d.Body));
            _currentCallable = default;
            return sProcDef;
        }
        case Node.Declaration.TypeAlias d: {
            var type = EvaluateType(scope, d.Type);
            scope.AddOrError(_msger, new Symbol.TypeAlias(d.Name, d.Location, type));
            return new Declaration.TypeAlias(meta, d.Name, type);
        }
        case Node.CompilerDirective cd: {
            EvaluateCompilerDirective(scope, cd);
            return default;
        }
        default: throw decl.ToUnmatchedException();
        }
    }

    CallableSignature AnalyzeSignature(Scope scope, Node.FunctionSignature sig) => new(new(scope, sig.Location),
        sig.Name, AnalyzeParameters(scope, sig.Parameters), EvaluateType(scope, sig.ReturnType));

    CallableSignature AnalyzeSignature(Scope scope, Node.ProcedureSignature sig) => new(new(scope, sig.Location),
        sig.Name, AnalyzeParameters(scope, sig.Parameters), VoidType.Instance);

    static Symbol.Callable MakeSymbol(CallableSignature sig, Func<ParameterFormal, Symbol.Parameter> makeParameterSymbol) =>
        new(sig.Name, sig.Meta.Location, sig.Parameters.Select(makeParameterSymbol).ToArray(), sig.ReturnType);

    ParameterFormal[] AnalyzeParameters(Scope scope, IEnumerable<Node.ParameterFormal> parameters) => parameters.Select(p =>
        new ParameterFormal(new(scope, p.Location),
            p.Mode, p.Name, EvaluateType(scope, p.Type))).ToArray();

    ParameterActual[] AnalyzeParameters(Scope scope, IEnumerable<Node.ParameterActual> parameters) => parameters.Select(p =>
        new ParameterActual(new(scope, p.Location),
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

    Func<ParameterFormal, Symbol.Parameter> DefineParameter(MutableScope inScope) => param => {
        var symb = DeclareParameter(param);
        if (!inScope.TryAdd(symb, out var existingSymb)) {
            _msger.Report(Message.ErrorRedefinedSymbol(symb, existingSymb));
        }
        return symb;
    };

    static Symbol.Parameter DeclareParameter(ParameterFormal param) => new(param.Name, param.Meta.Location, param.Type, param.Mode);

    Statement[] AnalyzeStatements(MutableScope scope, IEnumerable<Node.Stmt> statements) =>
        statements.Select(s => AnalyzeStatement(scope, s)).WhereSome().ToArray();

    ValueOption<Statement> AnalyzeStatement(MutableScope scope, Node.Stmt statement)
    {
        SemanticMetadata meta = new(scope, statement.Location);
        switch (statement) {
        case Node.Stmt.ExprStmt e: {
            var expr = EvaluateExpression(scope, e.Expr);
            if (!expr.Value.Type.IsConvertibleTo(VoidType.Instance)) {
                _msger.Report(Message.HintExpressionValueUnused(e.Expr.Location));
            }
            return new Statement.ExpressionStatement(meta, expr);
        }
        case Node.Nop: {
            return new Nop(meta);
        }
        case Node.Stmt.Alternative s: {
            return new Statement.Alternative(meta,
                new(new(scope, s.If.Location),
                    EvaluateExpression(scope, s.If.Condition, EvaluatedType.IsConvertibleTo, BooleanType.Instance),
                    AnalyzeStatements(new(scope), s.If.Body)
                ),
                s.ElseIfs.Select(elseIf => new Statement.Alternative.ElseIfClause(new(scope, elseIf.Location),
                    EvaluateExpression(scope, elseIf.Condition, EvaluatedType.IsConvertibleTo, BooleanType.Instance),
                    AnalyzeStatements(new(scope), elseIf.Body)
                )).ToArray(),
                s.Else.Map(@else => new Statement.Alternative.ElseClause(new(scope, @else.Location),
                    AnalyzeStatements(new(scope), @else.Body)
                )));
        }
        case Node.Stmt.Assignment s: {
            if (s.Target is Node.Expr.Lvalue.VariableReference varRef) {
                scope.GetSymbol<Symbol.Constant>(varRef.Name)
                   .Tap(constant => _msger.Report(Message.ErrorConstantAssignment(s.Location, constant)));
            }
            var target = EvaluateLvalue(scope, s.Target);
            var value = EvaluateExpression(scope, s.Value, EvaluatedType.IsAssignableTo, target.Value.Type);
            return new Statement.Assignment(meta, target, value);
        }
        case Node.Stmt.Builtin.Assigner s: {
            return new Statement.Builtin.Assigner(meta,
                EvaluateLvalue(scope, s.ArgumentNomLog, EvaluatedType.IsConvertibleTo, FileType.Instance),
                EvaluateExpression(scope, s.ArgumentNomExt, EvaluatedType.IsConvertibleTo, StringType.Instance));
        }
        case Node.Stmt.Builtin.Ecrire s: {
            return new Statement.Builtin.Ecrire(meta,
                EvaluateExpression(scope, s.ArgumentNomLog, EvaluatedType.IsConvertibleTo, FileType.Instance),
                EvaluateExpression(scope, s.ArgumentExpression));
        }
        case Node.Stmt.Builtin.EcrireEcran s: {
            return new Statement.Builtin.EcrireEcran(meta,
                s.Arguments.Select(a => EvaluateExpression(scope, a)).ToArray());
        }
        case Node.Stmt.Builtin.Fermer s: {
            return new Statement.Builtin.Fermer(meta,
                EvaluateExpression(scope, s.ArgumentNomLog, EvaluatedType.IsConvertibleTo, FileType.Instance));
        }
        case Node.Stmt.Builtin.Lire s: {
            return new Statement.Builtin.Lire(meta,
                EvaluateExpression(scope, s.ArgumentNomLog, EvaluatedType.IsConvertibleTo, FileType.Instance),
                EvaluateLvalue(scope, s.ArgumentVariable));
        }
        case Node.Stmt.Builtin.LireClavier s: {
            return new Statement.Builtin.LireClavier(meta,
                EvaluateLvalue(scope, s.ArgumentVariable));
        }
        case Node.Stmt.Builtin.OuvrirAjout s: {
            return new Statement.Builtin.OuvrirAjout(meta,
                EvaluateExpression(scope, s.ArgumentNomLog, EvaluatedType.IsConvertibleTo, FileType.Instance));
        }
        case Node.Stmt.Builtin.OuvrirEcriture s: {
            return new Statement.Builtin.OuvrirEcriture(meta,
                EvaluateExpression(scope, s.ArgumentNomLog, EvaluatedType.IsConvertibleTo, FileType.Instance));
        }
        case Node.Stmt.Builtin.OuvrirLecture s: {
            return new Statement.Builtin.OuvrirLecture(meta,
                EvaluateExpression(scope, s.ArgumentNomLog, EvaluatedType.IsConvertibleTo, FileType.Instance));
        }
        case Node.Stmt.DoWhileLoop s: {
            return new Statement.DoWhileLoop(meta,
                EvaluateExpression(scope, s.Condition, EvaluatedType.IsConvertibleTo, BooleanType.Instance),
                AnalyzeStatements(new(scope), s.Body));
        }
        case Node.Stmt.ForLoop s: {
            var variant = EvaluateLvalue(scope, s.Variant);
            return new Statement.ForLoop(meta,
                variant,
                EvaluateExpression(scope, s.Start, EvaluatedType.IsAssignableTo, variant.Value.Type),
                EvaluateExpression(scope, s.End, EvaluatedType.IsConvertibleTo, variant.Value.Type),
                s.Step.Map(e => EvaluateExpression(scope, e, EvaluatedType.IsConvertibleTo, variant.Value.Type)),
                AnalyzeStatements(new(scope), s.Body));
        }
        case Node.Stmt.LocalVariable s: {
            var type = EvaluateType(scope, s.Decl.Type);
            var declaration = new VariableDeclaration(new(scope, s.Decl.Location),
                s.Decl.Names,
                type);
            var initializer = s.Value.Map(i => {
                var init = AnalyzeInitializer(scope, i, EvaluatedType.IsAssignableTo, type);
                if (init is Expr) {
                    _msger.Report(Message.HintUnofficialFeatureScalarInitializers(init.Meta.Location));
                }
                return init;
            });

            foreach (var name in declaration.Names) {
                scope.AddOrError(_msger, new Symbol.LocalVariable(name, s.Location, type, initializer.Map(i => i.Value)));
            }

            return new Statement.LocalVariable(meta, declaration, initializer);
        }
        case Node.Stmt.RepeatLoop s: {
            return new Statement.RepeatLoop(meta,
                EvaluateExpression(scope, s.Condition, EvaluatedType.IsConvertibleTo, BooleanType.Instance),
                AnalyzeStatements(new(scope), s.Body));
        }
        case Node.Stmt.Return s: {
            return new Statement.Return(meta, _currentCallable.Match(
                func => EvaluateTypedValue(func.ReturnType),
                () => {
                    if (_mainProgramStatus is MainProgramStatus.Inside) {
                        return EvaluateTypedValue(IntegerType.Instance);
                    }
                    _msger.Report(Message.ErrorReturnInNonReturnable(s.Location));
                    return s.Value.Map(v => EvaluateExpression(scope, v));
                }));

            Option<Expr> EvaluateTypedValue(EvaluatedType expectedType) => s.Value
               .Map(v => EvaluateExpression(scope, v, EvaluatedType.IsConvertibleTo, expectedType))
               .Tap(none: () => {
                    if (!expectedType.IsConvertibleTo(VoidType.Instance)) {
                        _msger.Report(Message.ErrorReturnExpectsValue(s.Location, expectedType));
                    }
                });
        }
        case Node.Stmt.Switch s: {
            var expr = EvaluateExpression(scope, s.Expr);
            if (expr.Value.Type.IsConvertibleTo(StringType.Instance)) {
                _msger.Report(Message.ErrorCannotSwitchOnString(s.Location));
            }
            return new Statement.Switch(meta,
                expr,
                s.Cases.Select(
                    Statement.Switch.Case (@case, i) => {
                        switch (@case) {
                        case Node.Stmt.Switch.Case.OfValue c: {
                            var caseExpr = EvaluateExpression(scope, c.Value, EvaluatedType.IsConvertibleTo, expr.Value.Type);
                            if (caseExpr.Value.Status is not ValueStatus.Comptime) {
                                _msger.Report(Message.ErrorComptimeExpressionExpected(c.Value.Location));
                            }
                            return new Statement.Switch.Case.OfValue(new(scope, c.Location), caseExpr, AnalyzeStatements(scope, c.Body));
                        }
                        case Node.Stmt.Switch.Case.Default d: {
                            if (i != s.Cases.Count - 1) {
                                _msger.Report(Message.ErrorSwitchDefaultIsNotLast(d.Location));
                            }
                            return new Statement.Switch.Case.Default(new(scope, d.Location), AnalyzeStatements(scope, d.Body));
                        }
                        default: {
                            throw @case.ToUnmatchedException();
                        }
                        }
                    }).ToArray());
        }
        case Node.Stmt.WhileLoop s: {
            return new Statement.WhileLoop(meta,
                EvaluateExpression(scope, s.Condition, EvaluatedType.IsConvertibleTo, BooleanType.Instance),
                AnalyzeStatements(new(scope), s.Body));
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
        SemanticMetadata meta = new(scope, initializer.Location);

        switch (initializer) {
        case Node.Expr expr: return EvaluateExpression(scope, expr, typeComparer, targetType);
        case Node.Initializer.Braced braced: {
            var (value, items) = Evaluate();
            return new Initializer.Braced(meta, items, value);

            (Value, IReadOnlyList<Initializer.Braced.Item>) Evaluate()
            {
                switch (targetType) {
                case ArrayType targetArrayType: {
                    // Deeply initializes each subobject to its default value.
                    var arrayValue = targetArrayType
                       .DefaultValue; // todo: let this be a comptime value abstraction (couple bewteen type and comptime value that is implicitly convertible to Value)

                    var currentPath = Option.None<InitializerPath.Array>();

                    var sitems = AnalyzeItems(scope, braced, (item, sdes) => {
                            if (sdes.Length > 0) {
                                if (sdes[0] is Designator.Array first) {
                                    currentPath = EvaluateArrayPath(scope, first, sdes.AsSpan()[1..], targetArrayType)
                                       .DropError(_msger.Report).Or(currentPath);
                                } else {
                                    _msger.Report(Message.ErrorUnsupportedDesignator(sdes[0].Meta.Location, targetArrayType));
                                }
                            } else {
                                currentPath = currentPath.Match(
                                        p => p.Advance(item.Value.Location),
                                        () => InitializerPath.Array.OfFirstObject(targetArrayType, braced.Items[0].Location))
                                   .DropError(_msger.Report);
                            }

                            return currentPath.Map(p => p.Type.ItemType);
                        }, sitem => currentPath.Bind(p => p.SetValue(
                                    sitem.Meta.Location,
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
                                    _msger.Report(Message.ErrorUnsupportedDesignator(sdes[0].Meta.Location, targetStructType));
                                }
                            } else {
                                currentPath = currentPath.Match(
                                        p => p.Advance(item.Value.Location),
                                        () => InitializerPath.Structure.OfFirstObject(targetStructType, braced.Items[0].Location))
                                   .DropError(_msger.Report);
                            }

                            return currentPath.Map(p => p.Type.Components.Map[p.First.Component]);
                        }, sitem => currentPath.Bind(p => p.SetValue(sitem.Meta.Location, structValue.Status.ComptimeValue.Unwrap(), sitem.Value.Value)
                               .DropError(_msger.Report))
                           .Tap(v => structValue = v))
                       .ToArray();

                    return (structValue, sitems);
                }
                default: {
                    _msger.Report(Message.ErrorUnsupportedInitializer(initializer.Location, targetType));
                    return (UnknownType.Inferred.DefaultValue, AnalyzeItems(scope, braced, (_, _) => Option.None<EvaluatedType>(), null).ToArray());
                }
                }
            }
        }
        default: throw initializer.ToUnmatchedException();
        }

        IEnumerable<Initializer.Braced.Item> AnalyzeItems(
            Scope scope,
            Node.Initializer.Braced braced,
            Func<Node.Initializer.Braced.ValuedItem, ImmutableArray<Designator>, Option<EvaluatedType>> itemTargetType,
            Action<Initializer.Braced.Item>? onItemAdded
        )
        {
            foreach (var item in braced.Items) {
                switch (item) {
                case Node.Initializer.Braced.ValuedItem i: {
                    var sdes = i.Designators
                       .Select(d => AnalyzeDesignator(scope, d).DropError(_msger.Report))
                       .WhereSome().ToImmutableArray();

                    if (itemTargetType(i, sdes) is { HasValue: true } t) {
                        var sinit = AnalyzeInitializer(scope, i.Value, typeComparer, t.Value);
                        var sitem = new Initializer.Braced.Item(new(scope, item.Location), sdes, sinit);
                        onItemAdded?.Invoke(sitem);
                        yield return sitem;
                    }
                    break;
                }
                case Node.CompilerDirective cd: {
                    EvaluateCompilerDirective(scope, cd);
                    break;
                }
                default: throw item.ToUnmatchedException();
                }
            }
        }
    }

    Option<Designator, Message> AnalyzeDesignator(Scope scope, Node.Designator designator) => designator switch {
        Node.Designator.Array a => AnalyzeArrayDesignator(scope, a),
        Node.Designator.Structure s => AnalyzeStructureDesignator(scope, s).Some<Designator, Message>(),
        _ => throw designator.ToUnmatchedException(),
    };

    ValueOption<Designator.Array, Message> AnalyzeArrayDesignator(Scope scope, Node.Designator.Array designator) =>
        GetComptimeExpression<IntegerType, int>(IntegerType.Instance, scope, designator.Index)
           .Map(i => new Designator.Array(new(scope, designator.Location), i));

    static Designator.Structure AnalyzeStructureDesignator(Scope scope, Node.Designator.Structure designator) =>
        new(new(scope, designator.Location), designator.Comp);

    Expr EvaluateExpression(Scope scope, Node.Expr expr, TypeComparer typeComparer, EvaluatedType targetType) =>
        EvaluateExpression(scope, expr, v => CheckType(expr.Location, typeComparer, targetType, v));

    Expr EvaluateExpression(Scope scope, Node.Expr expr, Func<Value, Value>? adjustValue = null)
    {
        adjustValue ??= v => v;
        SemanticMetadata meta = new(scope, expr.Location);
        switch (expr) {
        case Node.Expr.Literal lit: {
            var value = lit.CreateValue();
            return new Expr.Literal(meta, lit.Value, adjustValue(value));
        }
        case Node.Expr.ParenExprImpl b: {
            var contained = EvaluateExpression(scope, b.InnerExpr);
            return new Expr.ParenExprImpl(meta, contained, adjustValue(contained.Value));
        }
        case Node.Expr.BinaryOperation opBin: {
            var left = EvaluateExpression(scope, opBin.Left);
            var right = EvaluateExpression(scope, opBin.Right);

            var result = opBin.Operator.EvaluateOperation(left.Value, right.Value);
            foreach (var msg in result.Messages) {
                _msger.Report(msg(opBin, left.Value.Type, right.Value.Type));
            }

            return new Expr.BinaryOperation(meta, left, AnalyzeOperator(scope, opBin.Operator), right, adjustValue(result.Value));
        }
        case Node.Expr.UnaryOperation opUn: {
            var operand = EvaluateExpression(scope, opUn.Operand);

            var result = this.EvaluateOperation(scope, opUn.Operator, operand.Value);
            foreach (var msg in result.Messages) {
                _msger.Report(msg(opUn, operand.Value.Type));
            }

            return new Expr.UnaryOperation(meta, AnalyzeOperator(scope, opUn.Operator), operand, adjustValue(result.Value));
        }
        case Node.Expr.Call call: {
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
                    _msger.Report(Message.ErrorCallParameterMismatch(call.Location, callable, problems));
                }
            }, none: () => {
                // If the callable symbol wasn't found, still analyze the parameter expressions.
                foreach (var actual in call.Parameters) {
                    EvaluateExpression(scope, actual.Value);
                }
            });

            return new Expr.Call(meta, call.Callee, parameters,
                adjustValue(callable
                   .Map(f => f.ReturnType)
                   .ValueOr(UnknownType.Inferred)
                   .RuntimeValue));
        }
        case Node.Expr.BuiltinFdf fdf: {
            return new Expr.BuiltinFdf(meta,
                EvaluateExpression(scope, fdf.ArgumentNomLog, EvaluatedType.IsConvertibleTo, FileType.Instance),
                adjustValue(BooleanType.Instance.RuntimeValue));
        }
        case Node.Expr.Lvalue lvalue: {
            return EvaluateLvalue(scope, lvalue, adjustValue);
        }
        default: throw expr.ToUnmatchedException();
        }
    }

    Expr.Lvalue EvaluateLvalue(Scope scope, Node.Expr.Lvalue expr, TypeComparer typeComparer, EvaluatedType targetType) =>
        EvaluateLvalue(scope, expr, v => CheckType(expr.Location, typeComparer, targetType, v));

    Expr.Lvalue EvaluateLvalue(Scope scope, Node.Expr.Lvalue lvalue, Func<Value, Value>? adjustValue = null)
    {
        adjustValue ??= v => v;
        SemanticMetadata meta = new(scope, lvalue.Location);
        switch (lvalue) {
        case Node.Expr.Lvalue.ArraySubscript arrSub: {
            var index = EvaluateExpression(scope, arrSub.Index);

            var array = EvaluateExpression(scope, arrSub.Array);
            return new Expr.Lvalue.ArraySubscript(meta, array, index, adjustValue(Evaluate()));

            Value Evaluate()
            {
                if (array.Value is ArrayValue arrVal) {
                    ValueOption<int> actualIndex;

                    if (index.Value is IntegerValue intVal) {
                        actualIndex = intVal.Status.ComptimeValue;
                    } else {
                        _msger.Report(Message.ErrorNonIntegerIndex(index.Meta.Location, index.Value.Type));
                        return arrVal.Type.ItemType.InvalidValue;
                    }

                    var length = arrVal.Type.Length.Value;

                    if (actualIndex is { HasValue: true } i && !i.Value.Indexes(length, 1)) {
                        _msger.Report(Message.ErrorIndexOutOfBounds(arrSub.Location, i.Value, length));
                        return arrVal.Type.ItemType.InvalidValue;
                    }

                    return arrVal.Status.ComptimeValue.Zip(actualIndex)
                       .Map((arr, index) => arr[index - 1])
                       .ValueOr(arrVal.Type.ItemType.RuntimeValue);
                }
                _msger.Report(Message.ErrorSubscriptOfNonArray(arrSub.Location, array.Value.Type));
                return UnknownType.Inferred.InvalidValue;
            }
        }
        case Node.Expr.Lvalue.ParenLValue b: {
            var contained = EvaluateLvalue(scope, b.ContainedLvalue);
            return new Expr.Lvalue.ParenLValue(meta, contained, adjustValue(contained.Value));
        }
        case Node.Expr.Lvalue.ComponentAccess compAccess: {
            var @struct = EvaluateExpression(scope, compAccess.Structure);
            return new Expr.Lvalue.ComponentAccess(meta, @struct, compAccess.ComponentName, adjustValue(Evaluate()));

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
        case Node.Expr.Lvalue.VariableReference varRef: {
            return new Expr.Lvalue.VariableReference(meta, varRef.Name,
                adjustValue(scope.GetSymbol<Symbol.Variable>(varRef.Name)
                   .DropError(_msger.Report)
                   .Map(vp => vp is Symbol.Constant constant
                        ? constant.Value
                        : vp.Type.RuntimeValue)
                   .ValueOr(UnknownType.Inferred.InvalidValue)));
        }
        default: throw lvalue.ToUnmatchedException();
        }
    }

    static BinaryOperator AnalyzeOperator(Scope scope, Node.BinaryOperator binOp)
    {
        SemanticMetadata meta = new(scope, binOp.Location);
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

    Value CheckType(FixedRange location, TypeComparer typeComparer, EvaluatedType targetType, Value value)
    {
        if (typeComparer(value.Type, targetType)) {
            return value;
        }
        if (targetType is not UnknownType && value.Type is not UnknownType) {
            _msger.Report(Message.ErrorExpressionHasWrongType(location, targetType, value.Type));
        }
        return targetType.InvalidValue;
    }

    UnaryOperator AnalyzeOperator(Scope scope, Node.UnaryOperator unOp)
    {
        SemanticMetadata meta = new(scope, unOp.Location);
        switch (unOp) {
        case Node.UnaryOperator.Cast op: {
            _msger.Report(Message.HintUnofficialFeature(op.Location, "type casts"));
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
        default: throw unOp.ToUnmatchedException();
        }
    }

    internal EvaluatedType EvaluateType(Scope scope, Node.Type type) => type switch {
        Node.Type.AliasReference alias
            => scope.GetSymbol<Symbol.TypeAlias>(alias.Name).DropError(_msger.Report)
               .Map(aliasType => aliasType.Type.ToAliasReference(alias.Name))
               .ValueOr(UnknownType.Declared(_input, type.Location)),
        Node.Type.Array array => EvaluateArrayType(scope, array),
        Node.Type.Boolean => BooleanType.Instance,
        Node.Type.Character => CharacterType.Instance,
        Node.Type.Integer => IntegerType.Instance,
        Node.Type.LengthedString str => EvaluateLengthedStringType(scope, str),
        Node.Type.Real => RealType.Instance,
        Node.Type.Structure structure => EvaluateStructureType(scope, structure),
        Node.Type.String => StringType.Instance,
        _ => throw type.ToUnmatchedException(),
    };

    EvaluatedType EvaluateLengthedStringType(Scope scope, Node.Type.LengthedString str) =>
        GetComptimeExpression<IntegerType, int>(IntegerType.Instance, scope, str.Length)
           .DropError(_msger.Report)
           .Map(LengthedStringType.Create)
           .ValueOr<EvaluatedType>(UnknownType.Declared(_input, str.Location));

    EvaluatedType EvaluateArrayType(Scope scope, Node.Type.Array array) => array.Dimensions
       .Select(d => GetComptimeExpression<IntegerType, int>(IntegerType.Instance, scope, d)).Sequence()
       .DropError(Function.Foreach<Message>(_msger.Report))
       .Map(dimensions => {
            var type = EvaluateType(scope, array.Type);
            Debug.Assert(dimensions.Any());
            // Start innermost with the least significant dimension
            return dimensions.Reverse().Aggregate(type, (current, dim) => new ArrayType(current, dim));
        })
       .ValueOr(UnknownType.Declared(_input, array.Location));

    static ValueOption<TUnderlying, Message> GetComptimeValue<TType, TUnderlying>(TType expectedType, Expr expr)
    where TType : EvaluatedType => expr is { Value: Value<TType, TUnderlying> v }
        ? v.Status.ComptimeValue.OrWithError(Message.ErrorComptimeExpressionExpected(expr.Meta.Location))
        : Message.ErrorExpressionHasWrongType(expr.Meta.Location, expectedType, expr.Value.Type);

    Option<ComptimeExpression<TUnderlying>, Message> GetComptimeExpression<TType, TUnderlying>(TType type, Scope scope, Node.Expr expr)
    where TType : EvaluatedType
    {
        var sexpr = EvaluateExpression(scope, expr);
        return sexpr.Value is Value<TType, TUnderlying> tval
            ? tval.Status.ComptimeValue.Map(v =>
                    ComptimeExpression.Create(sexpr, v))
               .OrWithError(Message.ErrorComptimeExpressionExpected(expr.Location))
            : Message.ErrorExpressionHasWrongType(expr.Location, type, sexpr.Value.Type).None<ComptimeExpression<TUnderlying>, Message>();
    }

    StructureType EvaluateStructureType(Scope scope, Node.Type.Structure structure)
    {
        var components = ImmutableOrderedMap<Ident, EvaluatedType>.Empty;
        foreach (var c in structure.Components) {
            switch (c) {
            case Node.VariableDeclaration comp: {
                foreach (var name in comp.Names) {
                    var type = EvaluateType(scope, comp.Type);
                    if (!components.TryAdd(name, type, out components)) {
                        _msger.Report(Message.ErrorStructureDuplicateComponent(comp.Location, name));
                    }
                }
                break;
            }
            case Node.CompilerDirective cd: {
                EvaluateCompilerDirective(scope, cd);
                break;
            }
            default: throw c.ToUnmatchedException();
            }
        }
        return new StructureType(components);
    }

    delegate bool TypeComparer(EvaluatedType actual, EvaluatedType expected);
}
