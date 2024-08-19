using Scover.Psdc.Language;
using Scover.Psdc.Messages;
using static Scover.Psdc.StaticAnalysis.SemanticNode;
using Scover.Psdc.Parsing;

namespace Scover.Psdc.StaticAnalysis;

public sealed class StaticAnalyzer
{
    readonly Messenger _msger;

    bool _seenMainProgram;
    ValueOption<FunctionSignature> _currentFunction;

    StaticAnalyzer(Messenger messenger) => _msger = messenger;

    void EvaluateCompilerDirective(Scope scope, Node.CompilerDirective compilerDirective)
    {
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
        case Node.CompilerDirective.EvaluateExpr cd: {
            _msger.Report(Message.DebugEvaluateExpression(cd.SourceTokens, EvaluateExpression(scope, cd.Expression).Value));
            break;
        }
        case Node.CompilerDirective.EvaluateType cd: {
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
        case Node.Declaration.Constant constant: {
            var type = EvaluateType(scope, constant.Type);
            var init = EvaluateInitializer(scope, constant.Value, EvaluatedType.IsAssignableTo, type);

            if (init.Value.Status is not ValueStatus.Comptime) {
                _msger.Report(Message.ErrorComptimeExpressionExpected(constant.Value.SourceTokens));
            }

            scope.AddOrError(_msger, new Symbol.Constant(constant.Name, constant.SourceTokens, type, init.Value));

            return new Declaration.Constant(meta, type, constant.Name, init);
        }
        case Node.Declaration.Function d: {
            var sig = AnalyzeSignature(scope, d.Signature);
            AddCallableDeclarationSymbol(scope, MakeSymbol(sig, DeclareParameter));
            return new Declaration.Function(meta, sig);
        }
        case Node.Declaration.FunctionDefinition d: {
            MutableScope funcScope = new(scope);
            var sig = AnalyzeSignature(scope, d.Signature);
            AddCallableDefinitionSymbol(scope, MakeSymbol(sig, DefineParameter(inScope: funcScope)));
            _currentFunction = sig;
            var sFuncDef = new Declaration.FunctionDefinition(meta, sig, AnalyzeStatements(funcScope, d.Block));
            _currentFunction = default;
            return sFuncDef;
        }
        case Node.Declaration.MainProgram d: {
            if (_seenMainProgram) {
                _msger.Report(Message.ErrorRedefinedMainProgram(d));
            }
            _seenMainProgram = true;
            return new Declaration.MainProgram(meta, AnalyzeStatements(new(scope), d.Block));
        }
        case Node.Declaration.Procedure d: {
            var sig = AnalyzeSignature(scope, d.Signature);
            AddCallableDeclarationSymbol(scope, MakeSymbol(sig, DeclareParameter));
            return new Declaration.Procedure(meta, sig);
        }
        case Node.Declaration.ProcedureDefinition d: {
            MutableScope procScope = new(scope);
            var sig = AnalyzeSignature(scope, d.Signature);
            AddCallableDefinitionSymbol(scope, MakeSymbol(sig, DefineParameter(inScope: procScope)));
            return new Declaration.ProcedureDefinition(meta, sig, AnalyzeStatements(procScope, d.Block));
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

    FunctionSignature AnalyzeSignature(Scope scope, Node.FunctionSignature sig) => new(new(scope, sig.SourceTokens),
        sig.Name, AnalyzeParameters(scope, sig.Parameters), EvaluateType(scope, sig.ReturnType));

    ProcedureSignature AnalyzeSignature(Scope scope, Node.ProcedureSignature sig) => new(new(scope, sig.SourceTokens),
        sig.Name, AnalyzeParameters(scope, sig.Parameters));

    static Symbol.Function MakeSymbol(FunctionSignature sig, Func<ParameterFormal, Symbol.Parameter> makeParameterSymbol)
     => new(sig.Name, sig.Meta.SourceTokens, sig.Parameters.Select(makeParameterSymbol).ToArray(), sig.ReturnType);

    static Symbol.Procedure MakeSymbol(ProcedureSignature sig, Func<ParameterFormal, Symbol.Parameter> makeParameterSymbol)
     => new(sig.Name, sig.Meta.SourceTokens, sig.Parameters.Select(makeParameterSymbol).ToArray());

    ParameterFormal[] AnalyzeParameters(Scope scope, IEnumerable<Node.ParameterFormal> parameters)
     => parameters.Select(p => new ParameterFormal(new(scope, p.SourceTokens),
            p.Mode, p.Name, EvaluateType(scope, p.Type))).ToArray();

    ParameterActual[] AnalyzeParameters(Scope scope, IEnumerable<Node.ParameterActual> parameters)
     => parameters.Select(p => new ParameterActual(new(scope, p.SourceTokens),
            p.Mode, EvaluateExpression(scope, p.Value))).ToArray();

    void AddCallableDeclarationSymbol<T>(MutableScope scope, T sub) where T : Symbol.Callable
    {
        if (!scope.TryAdd(sub, out var existingSymbol)) {
            if (existingSymbol is T existingSub) {
                if (!sub.SemanticsEqual(existingSub)) {
                    _msger.Report(Message.ErrorSignatureMismatch(sub, existingSub));
                }
            } else {
                _msger.Report(Message.ErrorRedefinedSymbol(sub, existingSymbol));
            }
        }
    }

    void AddCallableDefinitionSymbol<TSymbol>(MutableScope scope, TSymbol sub) where TSymbol : Symbol.Callable
    {
        if (scope.TryAdd(sub, out var existingSymbol)) {
            sub.MarkAsDefined();
        } else if (existingSymbol is TSymbol existingSub) {
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
            var initializer = s.Initializer.Map(i => EvaluateInitializer(scope, i, EvaluatedType.IsAssignableTo, type));

            foreach (var name in declaration.Names) {
                scope.AddOrError(_msger, new Symbol.Variable(name, s.SourceTokens, type, initializer.Map(i => i.Value)));
            }

            return new Statement.LocalVariable(meta, declaration, initializer);
        }
        case Node.Statement.Nop: {
            return new Statement.Nop(meta);
        }
        case Node.Statement.ProcedureCall s: {
            DiagnoseCall<Symbol.Procedure>(scope, s);
            return new Statement.ProcedureCall(meta, s.Callee, AnalyzeParameters(scope, s.Parameters));
        }
        case Node.Statement.RepeatLoop s: {
            return new Statement.RepeatLoop(meta,
                EvaluateExpression(scope, s.Condition, EvaluatedType.IsConvertibleTo, BooleanType.Instance),
                AnalyzeStatements(new(scope), s.Block));
        }
        case Node.Statement.Return s: {
            return new Statement.Return(meta, _currentFunction.Match(
                func => EvaluateExpression(scope, s.Value, EvaluatedType.IsConvertibleTo, func.ReturnType),
                () => {
                    _msger.Report(Message.ErrorReturnInNonFunction(s.SourceTokens));
                    return EvaluateExpression(scope, s.Value);
                }));
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
        case Node.Statement.CompilerDirective cd: {
            EvaluateCompilerDirective(scope, cd);
            return default;
        }
        default: {
            throw statement.ToUnmatchedException();
        }
        }
    }

    Option<TSymbol> DiagnoseCall<TSymbol>(Scope scope, Node.Call call) where TSymbol : Symbol.Callable
    {
        var callable = scope.GetSymbol<TSymbol>(call.Callee).DropError(_msger.Report);
        callable.Tap(callable => {
            List<string> problems = [];

            if (call.Parameters.Count != callable.Parameters.Count) {
                problems.Add(Message.ProblemWrongNumberOfArguments(
                    callable.Parameters.Count, call.Parameters.Count));
            }

            foreach (var (actual, formal) in call.Parameters.Zip(callable.Parameters)) {
                if (actual.Mode != formal.Mode) {
                    problems.Add(Message.ProblemWrongArgumentMode(formal.Name,
                        formal.Mode.RepresentationActual, actual.Mode.RepresentationActual));
                }

                var actualType = EvaluateExpression(scope, actual.Value).Value.Type;
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
        return callable;
    }

    Initializer EvaluateInitializer(Scope scope, Node.Initializer initializer, TypeComparer typeComparer, EvaluatedType targetType)
    {
        SemanticMetadata meta = new(scope, initializer.SourceTokens);

        switch (initializer) {
        case Node.Expression expr:
            return EvaluateExpression(scope, expr, typeComparer, targetType);
        case Node.Initializer.Braced init: {
            var (items, value) = Evaluate();
            return new Initializer.Braced(meta, items, value);

            (IReadOnlyList<Initializer.Braced.Item>, Value) Evaluate()
            {
                switch (targetType) {
                case ArrayType targetArrayType: {
                    var dimensions = targetArrayType.Dimensions.Select(d => d.Value).ToArray();

                    int currentIndex = 0;
                    Value[] arrayValue = Enumerable.Repeat(
                        targetArrayType.ItemType.DefaultValue,
                        dimensions.Product()).ToArray();

                    var sitems = AnalyzeItems(scope, init, item => item.Designator
                        .Match(designator => designator
                            .SomeAs<Node.Designator.Array>()
                            .OrWithError(Message.ErrorUnsupportedDesignator(designator.SourceTokens, targetType).Yield())
                            .Must(des => des.Index.Count == dimensions.Length,
                                    des => Message.ErrorIndexWrongRank(des.SourceTokens, des.Index.Count, dimensions.Length).Yield())
                            .Map(des => AnalyzeArrayDesignator(scope, des))
                            .Bind(sdes => sdes.Index
                                .Select(i => GetComptimeValue<IntegerType, int>(IntegerType.Instance, i))
                                .Sequence().Map(i => (sdes, i)))
                            .Map((sdes, index) => {
                                var flatIndex = index.Select(i => i - 1)
                                                .FlatIndex(dimensions.Select(d => d - 1));
                                currentIndex = flatIndex + 1;
                                return (sdes.Some(), flatIndex, index.Some());
                            })
                            .DropError(Function.Foreach<Message>(_msger.Report)),

                            () => (Option.None<Designator.Array>(), currentIndex++, Option.None<IReadOnlyList<int>>()).Some()
                        ).Map((sdes, flatIndex, index) => {
                            var sitem = AnalyzeItem(item, targetArrayType.ItemType, sdes);

                            if (flatIndex >= 0 && flatIndex < arrayValue.Length) {
                                arrayValue[flatIndex] = sitem.Initializer.Value;
                            } else {
                                _msger.Report(Message.ErrorIndexOutOfBounds(item.Designator.ValueOr<Node>(item).SourceTokens,
                                    GetOutOfBoundsDimIndexProblems(
                                        index.ValueOr(() => flatIndex.NDimIndex(dimensions))
                                                .Select(i => (i + 1).Some()).Zip(dimensions))));
                            }

                            return sitem;
                        })).ToArray();
                    return (sitems, targetArrayType.Instantiate(arrayValue));
                }
                case StructureType targetStructType: {
                    int currentComponentIndex = 0;
                    Dictionary<Identifier, Value> structValues = [];

                    var sitems = AnalyzeItems(scope, init, item => item.Designator
                        .Match(designator => designator.SomeAs<Node.Designator.Structure>()
                            .OrWithError(Message.ErrorUnsupportedDesignator(designator.SourceTokens, targetType))
                            .Bind(des => targetStructType.Components.Map.GetEntryOrNone(des.Component)
                                  .OrWithError(Message.ErrorStructureComponentDoesntExist(des.Component, targetStructType))
                                  .Map(kvp => (AnalyzeStructureDesignator(scope, des), kvp)))
                            .Map((des, kvp) => {
                                currentComponentIndex = targetStructType.Components.List.IndexOf(kvp).Unwrap();
                                return (des.Some(), kvp);
                            }),
                            () => targetStructType.Components.List
                                .ElementAtOrNone(currentComponentIndex++)
                                .Map(kvp => (Option.None<Designator.Structure>(), kvp))
                                .OrWithError(Message.ErrorExcessElementInInitializer(item.SourceTokens))
                        ).Match(
                            (des, kvp) => {
                                var sitem = AnalyzeItem(item, kvp.Value, des);
                                structValues[kvp.Key] = sitem.Initializer.Value;
                                return sitem;
                            },
                            msg => {
                                _msger.Report(msg);
                                return Option.None<Initializer.Braced.Item>();
                            }
                        )).ToArray();

                    return (sitems, targetStructType.Instantiate(structValues));
                }
                default: {
                    _msger.Report(Message.ErrorUnsupportedInitializer(initializer.SourceTokens, targetType));
                    return (AnalyzeItems(scope, init,
                                i => AnalyzeItem(i, targetType,
                                    i.Designator.Map(d => AnalyzeDesignator(scope, d))).Some())
                            .ToArray(),
                            targetType.InvalidValue);
                }
                }
            }
        }
        default:
            throw initializer.ToUnmatchedException();
        }

        IEnumerable<Initializer.Braced.Item> AnalyzeItems(Scope scope, Node.Initializer.Braced braced,
            Func<Node.Initializer.Braced.ValuedItem, Option<Initializer.Braced.Item>> evaluateValuedItem)
        {
            foreach (var item in braced.Items) {
                switch (item) {
                case Node.Initializer.Braced.ValuedItem i: {
                    if (evaluateValuedItem(i) is { HasValue: true } evalI) {
                        yield return evalI.Value;
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

        Initializer.Braced.Item AnalyzeItem(Node.Initializer.Braced.ValuedItem item, EvaluatedType targetType, Option<Designator> designator)
         => new(new(scope, item.SourceTokens), designator,
                EvaluateInitializer(scope, item.Initializer, typeComparer, targetType));

        Designator AnalyzeDesignator(Scope scope, Node.Designator d) => d switch {
            Node.Designator.Array a => AnalyzeArrayDesignator(scope, a),
            Node.Designator.Structure s => AnalyzeStructureDesignator(scope, s),
            _ => throw d.ToUnmatchedException(),
        };

        Designator.Structure AnalyzeStructureDesignator(Scope scope, Node.Designator.Structure s)
         => new(new(scope, s.SourceTokens), s.Component);
        Designator.Array AnalyzeArrayDesignator(Scope scope, Node.Designator.Array a)
         => new(new(scope, a.SourceTokens), a.Index.Select(i => EvaluateExpression(scope, i)).ToArray());
    }

    Expression EvaluateExpression(Scope scope, Node.Expression expr, TypeComparer typeComparer, EvaluatedType targetType)
    {
        var sexpr = EvaluateExpression(scope, expr);
        if (!typeComparer(sexpr.Value.Type, targetType)) {
            _msger.Report(Message.ErrorExpressionHasWrongType(expr.SourceTokens, targetType, sexpr.Value.Type));
        }
        return sexpr;
    }

    Expression EvaluateExpression(Scope scope, Node.Expression expr)
    {
        SemanticMetadata meta = new(scope, expr.SourceTokens);
        switch (expr) {
        case Node.Expression.Literal lit: {
            var value = lit.CreateValue();
            return new Expression.Literal(meta, lit.Value, value);
        }
        case Node.Expression.Bracketed b: {
            var contained = EvaluateExpression(scope, b.ContainedExpression);
            return new Expression.Bracketed(meta, contained, contained.Value);
        }
        case Node.Expression.BinaryOperation opBin: {
            var left = EvaluateExpression(scope, opBin.Left);
            var right = EvaluateExpression(scope, opBin.Right);

            var result = opBin.Operator.EvaluateOperation(left.Value, right.Value);
            foreach (var msg in result.Messages) {
                _msger.Report(msg(opBin, left.Value.Type, right.Value.Type));
            }

            return new Expression.BinaryOperation(meta, left, AnalyzeOperator(scope, opBin.Operator), right, result.Value);
        }
        case Node.Expression.UnaryOperation opUn: {
            var operand = EvaluateExpression(scope, opUn.Operand);

            var result = this.EvaluateOperation(scope, opUn.Operator, operand.Value);
            foreach (var msg in result.Messages) {
                _msger.Report(msg(opUn, operand.Value.Type));
            }

            return new Expression.UnaryOperation(meta, AnalyzeOperator(scope, opUn.Operator), operand, result.Value);
        }
        case Node.Expression.FunctionCall call: {
            return new Expression.FunctionCall(meta, call.Callee, AnalyzeParameters(scope, call.Parameters),
                DiagnoseCall<Symbol.Function>(scope, call)
                .Map(f => f.ReturnType).ValueOr(UnknownType.Inferred)
                .RuntimeValue);
        }
        case Node.Expression.BuiltinFdf fdf: {
            return new Expression.BuiltinFdf(meta,
                EvaluateExpression(scope, fdf.ArgumentNomLog, EvaluatedType.IsConvertibleTo, FileType.Instance),
                BooleanType.Instance.RuntimeValue);
        }
        case Node.Expression.Lvalue lvalue: {
            return EvaluateLvalue(scope, lvalue);
        }
        default:
            throw expr.ToUnmatchedException();
        }
    }

    Expression.Lvalue EvaluateLvalue(Scope scope, Node.Expression.Lvalue expr, TypeComparer typeComparer, EvaluatedType targetType)
    {
        var sexpr = EvaluateLvalue(scope, expr);
        if (!typeComparer(sexpr.Value.Type, targetType)) {
            _msger.Report(Message.ErrorExpressionHasWrongType(expr.SourceTokens, targetType, sexpr.Value.Type));
        }
        return sexpr;
    }

    Expression.Lvalue EvaluateLvalue(Scope scope, Node.Expression.Lvalue lvalue)
    {
        SemanticMetadata meta = new(scope, lvalue.SourceTokens);
        switch (lvalue) {
        case Node.Expression.Lvalue.ArraySubscript arrSub: {
            var index = arrSub.Index.Select(i => EvaluateExpression(scope, i));

            var array = EvaluateExpression(scope, arrSub.Array);
            return new Expression.Lvalue.ArraySubscript(meta, array, index.ToArray(), Evaluate());

            Value Evaluate()
            {
                var dimIndexes = index.Select(i => {
                    if (i.Value is IntegerValue intVal) {
                        return intVal.Status.ComptimeValue;
                    } else {
                        _msger.Report(Message.ErrorNonIntegerIndex(i.Meta.SourceTokens, i.Value.Type));
                        return default;
                    }
                }).ToArray();

                if (array.Value is ArrayValue arrVal) {
                    if (dimIndexes.Length != arrVal.Type.Dimensions.Count) {
                        _msger.Report(Message.ErrorIndexWrongRank(
                            arrSub.SourceTokens,
                            dimIndexes.Length,
                            arrVal.Type.Dimensions.Count));
                        return arrVal.Type.ItemType.InvalidValue;
                    }

                    var outOfBoundsDims = GetOutOfBoundsDimIndexProblems(dimIndexes.Zip(
                            arrVal.Type.Dimensions,
                            (index, length) => (index, length.Value)));

                    if (outOfBoundsDims.Length > 0) {
                        _msger.Report(Message.ErrorIndexOutOfBounds(arrSub.SourceTokens, outOfBoundsDims));
                        return arrVal.Type.ItemType.InvalidValue;
                    }

                    return arrVal.Status.ComptimeValue.Zip(dimIndexes.Sequence())
                        .Map((arr, index) => arr[index.FlatIndex(arrVal.Type.Dimensions.Select(d => d.Value))])
                        .ValueOr(arrVal.Type.ItemType.RuntimeValue);
                } else {
                    _msger.Report(Message.ErrorSubscriptOfNonArray(arrSub, array.Value.Type));
                    return UnknownType.Inferred.InvalidValue;
                }
            }
        }
        case Node.Expression.Lvalue.Bracketed b: {
            var contained = EvaluateLvalue(scope, b.ContainedLvalue);
            return new Expression.Lvalue.Bracketed(meta, contained, contained.Value);
        }
        case Node.Expression.Lvalue.ComponentAccess compAccess: {
            var @struct = EvaluateExpression(scope, compAccess.Structure);
            return new Expression.Lvalue.ComponentAccess(meta, @struct, compAccess.ComponentName, Evaluate());

            Value Evaluate()
            {
                if (@struct.Value is not StructureValue structVal) {
                    _msger.Report(Message.ErrrorComponentAccessOfNonStruct(compAccess, @struct.Value.Type));
                } else if (!structVal.Type.Components.Map.TryGetValue(compAccess.ComponentName, out var componentType)) {
                    _msger.Report(Message.ErrorStructureComponentDoesntExist(compAccess.ComponentName, structVal.Type));
                } else {
                    return structVal.Status.ComptimeValue
                        .Map(s => s[compAccess.ComponentName])
                        .ValueOr(componentType.RuntimeValue);
                }
                return UnknownType.Inferred.InvalidValue;
            }
        }
        case Node.Expression.Lvalue.VariableReference varRef: {
            return new Expression.Lvalue.VariableReference(meta, varRef.Name,
                scope.GetSymbol<Symbol.ValueProvider>(varRef.Name)
                    .DropError(_msger.Report)
                    .Map(vp => vp is Symbol.Constant constant
                        ? constant.Value
                        : vp.Type.RuntimeValue)
                    .ValueOr(UnknownType.Inferred.InvalidValue));
        }
        default:
            throw lvalue.ToUnmatchedException();
        }
    }

    static string[] GetOutOfBoundsDimIndexProblems(IEnumerable<(ValueOption<int> Index, int Length)> dimIndexes)
     => dimIndexes.Where((d) => d.Index.Map(i => i < 1 || i > d.Length).ValueOr(false))
                  .Select((d, i) => Message.ProblemOutOfBoundsDimension(
                        i, d.Index.Value, d.Length)).ToArray();

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

    internal UnaryOperator AnalyzeOperator(Scope scope, Node.UnaryOperator unOp)
    {
        SemanticMetadata meta = new(scope, unOp.SourceTokens);
        return unOp switch {
            Node.UnaryOperator.Cast op => new UnaryOperator.Cast(meta, EvaluateType(scope, op.Target)),
            Node.UnaryOperator.Minus => new UnaryOperator.Minus(meta),
            Node.UnaryOperator.Not => new UnaryOperator.Not(meta),
            Node.UnaryOperator.Plus => new UnaryOperator.Plus(meta),
            _ => throw unOp.ToUnmatchedException(),
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
        .Map(values => new ArrayType(EvaluateType(scope, array.Type), values))
        .ValueOr<EvaluatedType>(UnknownType.Declared(_msger.Input, array));

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
        Dictionary<Identifier, EvaluatedType> componentsMap = [];
        List<KeyValuePair<Identifier, EvaluatedType>> componentsList = [];
        foreach (var c in structure.Components) {
            switch (c) {
            case Node.VariableDeclaration comp: {
                foreach (var name in comp.Names) {
                    var type = EvaluateType(scope, comp.Type);
                    if (componentsMap.TryAdd(name, type)) {
                        componentsList.Add(new(name, type));
                    } else {
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
        return new StructureType(new(componentsMap, componentsList));
    }

    delegate bool TypeComparer(EvaluatedType actual, EvaluatedType expected);
}
