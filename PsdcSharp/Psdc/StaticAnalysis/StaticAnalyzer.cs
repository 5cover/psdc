using Scover.Psdc.Language;
using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;

using static Scover.Psdc.Parsing.Node;
using Type = Scover.Psdc.Parsing.Node.Type;

namespace Scover.Psdc.StaticAnalysis;

public sealed partial class StaticAnalyzer
{
    readonly Dictionary<Expression, EvaluatedType> _inferredTypes = [];
    readonly Dictionary<NodeScoped, Scope> _scopes = [];
    readonly Messenger _messenger;

    bool _seenMainProgram;
    ValueOption<Symbol.Function> _currentFunction;

    StaticAnalyzer(Messenger messenger) => _messenger = messenger;

    public static SemanticAst Analyze(Messenger messenger, Algorithm root)
    {
        StaticAnalyzer a = new(messenger);

        a.SetParentScope(root, null);
        foreach (var decl in root.Declarations) {
            a.AnalyzeDeclaration(a._scopes[root], decl);
        }

        if (!a._seenMainProgram) {
            messenger.Report(Message.ErrorMissingMainProgram(root.SourceTokens));
        }

        return new(root, a._scopes, a._inferredTypes);
    }

    void AddParameters(Scope scope, IEnumerable<Symbol.Parameter> parameters)
    {
        foreach (var param in parameters) {
            if (!scope.TryAdd(param, out var existingSymbol)) {
                _messenger.Report(Message.ErrorRedefinedSymbol(param, existingSymbol));
            }
        }
    }

    void AnalyzeCallableDefinition<T>(Scope scope, BlockNode node, T sub) where T : CallableSymbol, IEquatable<T?>
    {
        HandleCallableDefinition(scope, sub);
        AddParameters(_scopes[node], sub.Parameters);

        AnalyzeScopedBlock(node);
    }

    void AnalyzeDeclaration(Scope scope, Declaration decl)
    {
        SetParentScopeIfNecessary(decl, scope);

        switch (decl) {
        case Declaration.TypeAlias alias:
            scope.AddSymbolOrError(_messenger, new Symbol.TypeAlias(alias.Name, alias.SourceTokens,
                EvaluateType(scope, alias.Type)));
            break;

        case Declaration.Constant constant: {
            var value = AnalyzeExpression(scope, constant.Value);
            var declaredType = EvaluateType(scope, constant.Type);

            if (!value.IsKnown) {
                _messenger.Report(Message.ErrorConstantExpressionExpected(constant.Value.SourceTokens));
            }

            if (!value.Type.IsConvertibleTo(declaredType)) {
                _messenger.Report(Message.ErrorExpressionHasWrongType(
                    constant.Value.SourceTokens, declaredType, value.Type));

                // if the provided value wasn't of the right type, use a non-const value of the declared type.
                // the constant is still added to the scope.
                value = declaredType.UnknownValue;
            }

            scope.AddSymbolOrError(_messenger,
                new Symbol.Constant(constant.Name, constant.SourceTokens, declaredType, value));

            break;
        }
        case Declaration.Function func:
            HandleCallableDeclaration(scope, new Symbol.Function(
                    func.Signature.Name,
                    func.Signature.SourceTokens,
                    CreateParameters(scope, func.Signature.Parameters),
                    EvaluateType(scope, func.Signature.ReturnType)));
            break;

        case Declaration.FunctionDefinition funcDef: {
            Symbol.Function func = new(
                funcDef.Signature.Name,
                funcDef.Signature.SourceTokens,
                CreateParameters(scope, funcDef.Signature.Parameters),
                EvaluateType(scope, funcDef.Signature.ReturnType));
            _currentFunction = func;
            AnalyzeCallableDefinition(scope, funcDef, func);
            _currentFunction = default;
            break;
        }
        case Declaration.MainProgram mainProgram:
            if (_seenMainProgram) {
                _messenger.Report(Message.ErrorRedefinedMainProgram(mainProgram));
            }
            _seenMainProgram = true;
            AnalyzeScopedBlock(mainProgram);
            break;

        case Declaration.Procedure proc:
            HandleCallableDeclaration(scope, new Symbol.Procedure(
                proc.Signature.Name,
                proc.Signature.SourceTokens,
                CreateParameters(scope, proc.Signature.Parameters)));
            break;

        case Declaration.ProcedureDefinition procDef:
            AnalyzeCallableDefinition(scope, procDef, new Symbol.Procedure(
                procDef.Signature.Name,
                procDef.Signature.SourceTokens,
                CreateParameters(scope, procDef.Signature.Parameters)));
            break;

        default:
            throw decl.ToUnmatchedException();
        }
    }

    Value AnalyzeExpression(ReadOnlyScope scope, Expression expr, TypeEquivalencePredicate typesPredicate, Option<EvaluatedType> expectedType)
    {
        var value = AnalyzeExpression(scope, expr);
        expectedType.MatchSome(expectedType => {
            if (!typesPredicate(value.Type, expectedType)) {
                _messenger.Report(Message.ErrorExpressionHasWrongType(expr.SourceTokens, expectedType, value.Type));
            }
        });
        return value;
    }

    delegate bool TypeEquivalencePredicate(EvaluatedType actual, EvaluatedType expected);

    Value AnalyzeExpression(ReadOnlyScope scope, Expression expr, TypeEquivalencePredicate typesPredicate, EvaluatedType expectedType)
    {
        var value = AnalyzeExpression(scope, expr);
        if (!typesPredicate(value.Type, expectedType)) {
            _messenger.Report(Message.ErrorExpressionHasWrongType(expr.SourceTokens, expectedType, value.Type));
        }
        return value;
    }

    Value AnalyzeExpression(ReadOnlyScope scope, Expression expr)
    {
        var value = EvaluateValue(scope, expr);
        _inferredTypes.Add(expr, value.Type);
        return value;
    }

    Value EvaluateValue(ReadOnlyScope scope, Expression expr)
    {
        switch (expr) {
        case Expression.Literal lit:
            return lit.CreateValue();
        case NodeBracketedExpression b:
            return AnalyzeExpression(scope, b.ContainedExpression);
        case Expression.BinaryOperation opBin: {
            var left = AnalyzeExpression(scope, opBin.Left);
            var right = AnalyzeExpression(scope, opBin.Right);
            var res = opBin.Operator.EvaluateOperation(left, right);
            foreach (var error in res.Errors) {
                _messenger.Report(error.GetOperationMessage(opBin, left.Type, right.Type));
            }
            return res.Value;
        }
        case Expression.UnaryOperation opUn: {
            var operand = AnalyzeExpression(scope, opUn.Operand);
            var res = opUn.Operator.EvaluateOperation(operand);
            foreach (var error in res.Errors) {
                _messenger.Report(error.GetOperationMessage(opUn, operand.Type));
            }
            return res.Value;
        }
        case Expression.FunctionCall call:
            return HandleCall<Symbol.Function>(scope, call)
                .Map(f => f.ReturnType).ValueOr(EvaluatedType.Unknown.Inferred)
                .UnknownValue;

        case Expression.BuiltinFdf fdf:
            AnalyzeExpression(scope, fdf.ArgumentNomLog);
            return EvaluatedType.Boolean.Instance.CreateValue(Option.None<bool>());

        case Expression.Lvalue.ArraySubscript arrSub: {
            foreach (var index in arrSub.Indexes) {
                var indexType = AnalyzeExpression(scope, index).Type;
                if (!indexType.IsConvertibleTo(EvaluatedType.Integer.Instance)) {
                    _messenger.Report(Message.ErrorNonIntegerIndex(index.SourceTokens, indexType));
                }
            }

            var actualType = AnalyzeExpression(scope, arrSub.Array).Type;

            if (actualType is EvaluatedType.Array arrayType) {
                return arrayType.ElementType.UnknownValue;
            } else {
                _messenger.Report(Message.ErrorSubscriptOfNonArray(arrSub, actualType));
                return EvaluatedType.Unknown.Inferred.UnknownValue;
            }
        }
        case Expression.Lvalue.ComponentAccess compAccess: {
            var actualType = AnalyzeExpression(scope, compAccess.Structure).Type;
            if (actualType is not EvaluatedType.Structure structType) {
                _messenger.Report(Message.ErrrorComponentAccessOfNonStruct(compAccess, actualType));
            } else if (!structType.Components.Map.TryGetValue(compAccess.ComponentName, out var componentType)) {
                _messenger.Report(Message.ErrorStructureComponentDoesntExist(compAccess, structType.Alias));
            } else {
                return componentType.UnknownValue;
            }
            return EvaluatedType.Unknown.Inferred.UnknownValue;
        }
        case Expression.Lvalue.VariableReference varRef:
            return scope.GetSymbol<Symbol.Variable>(varRef.Name)
            .MatchError(_messenger.Report)
            .Map(variable => variable is Symbol.Constant constant
                ? constant.Value
                : variable.Type.UnknownValue)
            .ValueOr(EvaluatedType.Unknown.Inferred.UnknownValue);
        default:
            throw expr.ToUnmatchedException();
        }
    }

    void AnalyzeScopedBlock(BlockNode scopedBlock)
    {
        foreach (var stmt in scopedBlock.Block) {
            AnalyzeStatement(_scopes[scopedBlock], stmt);
        }
    }

    void AnalyzeStatement(Scope scope, Statement stmt)
    {
        SetParentScopeIfNecessary(stmt, scope);

        switch (stmt) {
        case Statement.Nop nop:
            break;

        case Statement.Alternative alternative:
            AnalyzeExpression(scope, alternative.If.Condition, EvaluatedType.IsConvertibleTo, EvaluatedType.Boolean.Instance);
            SetParentScope(alternative.If, scope);
            AnalyzeScopedBlock(alternative.If);
            foreach (var elseIf in alternative.ElseIfs) {
                AnalyzeExpression(scope, elseIf.Condition, EvaluatedType.IsConvertibleTo, EvaluatedType.Boolean.Instance);
                SetParentScope(elseIf, scope);
                AnalyzeScopedBlock(elseIf);
            }
            alternative.Else.MatchSome(@else => {
                SetParentScope(@else, scope);
                AnalyzeScopedBlock(@else);
            });
            break;

        case Statement.Assignment assignment: {
            Value target = AnalyzeExpression(scope, assignment.Target);
            if (assignment.Target is Expression.Lvalue.VariableReference varRef) {
                scope.GetSymbol<Symbol.Constant>(varRef.Name).MatchSome(constant
                    => _messenger.Report(Message.ErrorConstantAssignment(assignment, constant)));
            }
            AnalyzeExpression(scope, assignment.Value, EvaluatedType.IsAssignableTo, target.Type);
            break;
        }
        case Statement.DoWhileLoop doWhileLoop:
            AnalyzeScopedBlock(doWhileLoop);
            AnalyzeExpression(scope, doWhileLoop.Condition, EvaluatedType.IsConvertibleTo, EvaluatedType.Boolean.Instance);
            break;
        case Statement.ForLoop forLoop:
            AnalyzeExpression(scope, forLoop.Start);
            forLoop.Step.MatchSome(step => AnalyzeExpression(scope, step));
            AnalyzeExpression(scope, forLoop.End);
            AnalyzeScopedBlock(forLoop);
            break;

        case Statement.Builtin.Lire lire:
            AnalyzeExpression(scope, lire.ArgumentNomLog);
            AnalyzeExpression(scope, lire.ArgumentVariable);
            break;

        case Statement.Builtin.Ecrire ecrire:
            AnalyzeExpression(scope, ecrire.ArgumentNomLog);
            AnalyzeExpression(scope, ecrire.ArgumentExpression);
            break;

        case Statement.Builtin.Fermer fermer:
            AnalyzeExpression(scope, fermer.ArgumentNomLog);
            break;

        case Statement.Builtin.OuvrirAjout ouvrirAjout:
            AnalyzeExpression(scope, ouvrirAjout.ArgumentNomLog);
            break;

        case Statement.Builtin.OuvrirEcriture ouvrirEcriture:
            AnalyzeExpression(scope, ouvrirEcriture.ArgumentNomLog);
            break;

        case Statement.Builtin.OuvrirLecture ouvrirLecture:
            AnalyzeExpression(scope, ouvrirLecture.ArgumentNomLog);
            break;

        case Statement.Builtin.Assigner assigner:
            AnalyzeExpression(scope, assigner.ArgumentNomExt);
            AnalyzeExpression(scope, assigner.ArgumentNomLog);
            break;

        case Statement.Builtin.EcrireEcran ecrireEcran:
            foreach (var arg in ecrireEcran.Arguments) {
                AnalyzeExpression(scope, arg);
            }
            break;

        case Statement.Builtin.LireClavier lireClavier:
            AnalyzeExpression(scope, lireClavier.ArgumentVariable);
            break;

        case Statement.ProcedureCall call:
            HandleCall<Symbol.Procedure>(scope, call);
            break;

        case Statement.RepeatLoop repeatLoop:
            AnalyzeScopedBlock(repeatLoop);
            AnalyzeExpression(scope, repeatLoop.Condition, EvaluatedType.IsConvertibleTo, EvaluatedType.Boolean.Instance);
            break;

        case Statement.Return ret:
            _currentFunction.Match(
                func => AnalyzeExpression(scope, ret.Value, EvaluatedType.IsConvertibleTo, func.ReturnType),
                () => {
                    AnalyzeExpression(scope, ret.Value);
                    _messenger.Report(Message.ErrorReturnInNonFunction(ret.SourceTokens));
                });
            break;

        case Statement.LocalVariable varDecl: {
            var type = EvaluateType(scope, varDecl.Type);
            foreach (var name in varDecl.Names) {
                scope.AddSymbolOrError(_messenger, new Symbol.Variable(name, varDecl.SourceTokens, type));
            }
            break;
        }
        case Statement.WhileLoop whileLoop: {
            AnalyzeScopedBlock(whileLoop);
            Value condition = AnalyzeExpression(scope, whileLoop.Condition, EvaluatedType.IsConvertibleTo, EvaluatedType.Boolean.Instance);
            break;
        }
        case Statement.Switch switchCase: {
            var type = AnalyzeExpression(scope, switchCase.Expression).Type;
            foreach (var @case in switchCase.Cases) {
                SetParentScope(@case, scope);

                Value val = AnalyzeExpression(scope, @case.When, EvaluatedType.IsConvertibleTo, type);
                if (!val.IsKnown) {
                    _messenger.Report(Message.ErrorConstantExpressionExpected(@case.When.SourceTokens));
                }
                AnalyzeScopedBlock(@case);
            }
            switchCase.Default.MatchSome(@default => {
                SetParentScope(@default, scope);
                AnalyzeScopedBlock(@default);
            });
            break;
        }
        default:
            throw stmt.ToUnmatchedException();
        }
    }

    List<Symbol.Parameter> CreateParameters(ReadOnlyScope scope, IEnumerable<ParameterFormal> parameters)
        => parameters.Select(param
            => new Symbol.Parameter(param.Name, param.SourceTokens,
                EvaluateType(scope, param.Type), param.Mode))
        .ToList();

    Option<TSymbol> HandleCall<TSymbol>(ReadOnlyScope scope, NodeCall call) where TSymbol : CallableSymbol
    {
        var symbol = scope.GetSymbol<TSymbol>(call.Name).MatchError(_messenger.Report);
        symbol.Match(callable => {
            List<string> problems = [];

            if (call.Parameters.Count != callable.Parameters.Count) {
                problems.Add(Message.ProblemWrongNumberOfArguments(
                    callable.Parameters.Count, call.Parameters.Count));
            }

            foreach (var (actual, formal) in call.Parameters.Zip(callable.Parameters)) {
                if (actual.Mode != formal.Mode) {
                    problems.Add(Message.ProblemWrongArgumentMode(formal.Name,
                        actual.Mode.RepresentationActual, formal.Mode.RepresentationFormal));
                }

                var actualType = AnalyzeExpression(scope, actual.Value).Type;
                if (!actualType.IsConvertibleTo(formal.Type)) {
                    problems.Add(Message.ProblemWrongArgumentType(formal.Name, formal.Type, actualType));
                }
            }

            if (problems.Count > 0) {
                _messenger.Report(Message.ErrorCallParameterMismatch(call.SourceTokens, callable, problems));
            }
        }, none: () => {
            // If the callable symbol wasn't found, still analyze the parameter expressions.
            foreach (var actual in call.Parameters) {
                AnalyzeExpression(scope, actual.Value);
            }
        });
        return symbol;
    }

    void HandleCallableDeclaration<T>(Scope scope, T sub) where T : CallableSymbol
    {
        if (!scope.TryAdd(sub, out var existingSymbol)) {
            if (existingSymbol is T existingSub) {
                if (!sub.SemanticsEqual(existingSub)) {
                    _messenger.Report(Message.ErrorSignatureMismatch(sub, existingSub));
                }
            } else {
                _messenger.Report(Message.ErrorRedefinedSymbol(sub, existingSymbol));
            }
        }
    }

    void HandleCallableDefinition<T>(Scope scope, T sub) where T : CallableSymbol
    {
        if (scope.TryAdd(sub, out var existingSymbol)) {
            sub.MarkAsDefined();
        } else if (existingSymbol is T existingSub) {
            if (existingSub.HasBeenDefined) {
                _messenger.Report(Message.ErrorRedefinedSymbol(sub, existingSub));
            } else if (!sub.SemanticsEqual(existingSub)) {
                _messenger.Report(Message.ErrorSignatureMismatch(sub, existingSub));
            }
        } else {
            _messenger.Report(Message.ErrorRedefinedSymbol(sub, existingSymbol));
        }
    }

    void SetParentScope(NodeScoped scopedNode, Scope? parentScope) => _scopes.Add(scopedNode, new(parentScope));

    void SetParentScopeIfNecessary(Node node, Scope? parentScope)
    {
        if (node is NodeScoped sn) {
            SetParentScope(sn, parentScope);
        }
    }

    EvaluatedType EvaluateType(ReadOnlyScope scope, Type type) => type switch {
        NodeAliasReference alias
         => scope.GetSymbol<Symbol.TypeAlias>(alias.Name).MatchError(_messenger.Report)
                .Map(aliasType => aliasType.TargetType.ToAliasReference(alias.Name))
            .ValueOr(EvaluatedType.Unknown.Declared(_messenger.Input, type)),
        Type.Complete.Array array => EvaluateArrayType(scope, array),
        Type.Complete.Boolean => EvaluatedType.Boolean.Instance,
        Type.Complete.Character => EvaluatedType.Character.Instance,
        Type.Complete.File file => EvaluatedType.File.Instance,
        Type.Complete.Integer p => EvaluatedType.Integer.Instance,
        Type.Complete.LengthedString str => EvaluateLengthedStringType(scope, str),
        Type.Complete.Real p => EvaluatedType.Real.Instance,
        Type.Complete.Structure structure => EvaluateStructureType(scope, structure),
        Type.String => EvaluatedType.String.Instance,
        _ => throw type.ToUnmatchedException(),
    };

    EvaluatedType EvaluateLengthedStringType(ReadOnlyScope scope, Type.Complete.LengthedString str)
     => GetConstantExpression<EvaluatedType.Integer, int>(EvaluatedType.Integer.Instance, scope, str.Length)
        .MatchError(_messenger.Report)
        .Map(EvaluatedType.LengthedString.Create)
        .ValueOr(EvaluatedType.Unknown.Declared(_messenger.Input, str));

    EvaluatedType EvaluateArrayType(ReadOnlyScope scope, Type.Complete.Array array)
     => array.Dimensions.Select(d => GetConstantExpression<EvaluatedType.Integer, int>(EvaluatedType.Integer.Instance, scope, d)).Accumulate()
        .MatchError(Function.Foreach<Message>(_messenger.Report))
        .Map(values => EvaluatedType.Array.Create(EvaluateType(scope, array.Type), values.ToList()))
        .ValueOr(EvaluatedType.Unknown.Declared(_messenger.Input, array));

    Option<ConstantExpression<TUnderlying>, Message> GetConstantExpression<TType, TUnderlying>(TType type, ReadOnlyScope scope, Expression expr)
    where TType : EvaluatedType
    {
        var value = AnalyzeExpression(scope, expr);
        return value is Value<TType, TUnderlying> tval
            ? tval.UnderlyingValue.Map(v =>
                ConstantExpression.Create(expr, v))
                .OrWithError(Message.ErrorConstantExpressionExpected(expr.SourceTokens))
            : Message.ErrorExpressionHasWrongType(expr.SourceTokens, type, value.Type).None<ConstantExpression<TUnderlying>, Message>();
    }

    EvaluatedType.Structure EvaluateStructureType(ReadOnlyScope scope, Type.Complete.Structure structure)
    {
        Dictionary<Identifier, EvaluatedType> componentsMap = [];
        List<(Identifier, EvaluatedType)> componentsList = [];
        foreach (var comp in structure.Components) {
            foreach (var name in comp.Names) {
                var type = EvaluateType(scope, comp.Type);
                if (!componentsMap.TryAdd(name, type)) {
                    _messenger.Report(Message.ErrorStructureDuplicateComponent(comp.SourceTokens, name));
                }
                componentsList.Add((name, type));
            }
        }
        return new EvaluatedType.Structure(new(componentsMap, componentsList));
    }
}
