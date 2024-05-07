using Scover.Psdc.Language;
using Scover.Psdc.Messages;
using Scover.Psdc.Parsing;

using static Scover.Psdc.Parsing.Node;
using Type = Scover.Psdc.Parsing.Node.Type;

namespace Scover.Psdc.StaticAnalysis;

sealed partial class StaticAnalyzer
{
    readonly Dictionary<Expression, Option<EvaluatedType>> _inferredTypes = [];
    readonly Dictionary<NodeScoped, Scope> _scopes = [];
    readonly Messenger _messenger;
    bool _seenMainProgram;

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

            if (!value.IsConstant) {
                _messenger.Report(Message.ErrorConstantExpressionExpected(constant.Value.SourceTokens));
            }

            if (value.Type.Map(t => !t.SemanticsEqual(declaredType)).ValueOr(true)) {
                
                value.Type.MatchSome(t => _messenger.Report(Message.ErrorExpressionHasWrongType(
                    constant.Value.SourceTokens, declaredType, t)));

                // if the provided value wasn't of the right type, use a non-const value of the declared type.
                // the constant is still added to the scope.
                value = Value.Of(declaredType);
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

        case Declaration.FunctionDefinition funcDef:
            AnalyzeCallableDefinition(scope, funcDef, new Symbol.Function(
                    funcDef.Signature.Name,
                    funcDef.Signature.SourceTokens,
                    CreateParameters(scope, funcDef.Signature.Parameters),
                    EvaluateType(scope, funcDef.Signature.ReturnType)));
            break;

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

    Value AnalyzeExpression(ReadOnlyScope scope, Expression expr)
    {
        var value = EvaluateExpression(scope, expr);
        _inferredTypes.Add(expr, value.Type);
        return value;
    }

    Value EvaluateExpression(ReadOnlyScope scope, Expression expr)
    {
        switch (expr) {
        case Expression.Literal lit:
            return lit.Value;
        case NodeBracketedExpression b:
            return AnalyzeExpression(scope, b.ContainedExpression);
        case Expression.BinaryOperation opBin: {
            var left = AnalyzeExpression(scope, opBin.Left);
            var right = AnalyzeExpression(scope, opBin.Right);
            var res = EvaluateOperation(opBin.Operator, left, right);
            left.Type.Combine(right.Type).MatchSome((tleft, tright) => {
                foreach (var error in res.Errors) {
                    _messenger.Report(GetOperationMessage(error, opBin, tleft, tright));
                }
            });
            return res.Value;
        }
        case Expression.UnaryOperation opUn: {
            var operand = AnalyzeExpression(scope, opUn.Operand);
            var res = EvaluateOperation(opUn.Operator, operand);
            operand.Type.MatchSome(t => {
                foreach (var error in res.Errors) {
                    _messenger.Report(GetOperationMessage(error, opUn, t));
                }
            });
            return res.Value;
        }
        case Expression.FunctionCall call:
            return HandleCall<Symbol.Function>(scope, call)
                .Map(f => Value.Of(f.ReturnType))
                .ValueOr(Value.UnknownInferred);
        case Expression.BuiltinFdf fdf:
            AnalyzeExpression(scope, fdf.ArgumentNomLog);
            return Value.Of(EvaluatedType.Boolean.Instance);

        case Expression.Lvalue.ArraySubscript arrSub:
            foreach (var index in arrSub.Indexes) {
                AnalyzeExpression(scope, index).Type
                    .When(t => !t.SemanticsEqual(EvaluatedType.Integer.Instance))
                    .MatchSome(t => _messenger.Report(Message.ErrorNonIntegerIndex(index.SourceTokens, t)));
            }

            var arrayType = AnalyzeExpression(scope, arrSub.Array).Type;

            arrayType.When(t => t is not EvaluatedType.Array)
                    .MatchSome(t => _messenger.Report(Message.ErrorSubscriptOfNonArray(arrSub, t)));

            return Value.Of(arrayType);

        case Expression.Lvalue.ComponentAccess compAccess:
            return Value.Of(AnalyzeExpression(scope, compAccess.Structure).Type.Bind(t => {
                if (t is not EvaluatedType.Structure structType) {
                    _messenger.Report(Message.ErrrorComponentAccessOfNonStruct(compAccess, t));
                } else if (!structType.Components.TryGetValue(compAccess.ComponentName, out var compType)) {
                    _messenger.Report(Message.ErrorStructureComponentDoesntExist(compAccess, structType.Alias));
                } else {
                    return compType.Some();
                }
                return Option.None<EvaluatedType>();
            }));
        case Expression.Lvalue.VariableReference varRef:
            return scope.GetSymbol<Symbol.Variable>(varRef.Name)
            .MatchError(_messenger.Report)
            .Map(variable => variable is Symbol.Constant constant
                ? constant.Value
                : Value.Of(variable.Type))
            .ValueOr(Value.UnknownInferred);
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
            AnalyzeExpression(scope, alternative.If.Condition);
            SetParentScope(alternative.If, scope);
            AnalyzeScopedBlock(alternative.If);
            foreach (var elseIf in alternative.ElseIfs) {
                AnalyzeExpression(scope, elseIf.Condition);
                SetParentScope(elseIf, scope);
                AnalyzeScopedBlock(elseIf);
            }
            alternative.Else.MatchSome(@else => {
                SetParentScope(@else, scope);
                AnalyzeScopedBlock(@else);
            });
            break;

        case Statement.Assignment assignment:
            AnalyzeExpression(scope, assignment.Target);
            if (assignment.Target is Expression.Lvalue.VariableReference varRef) {
                scope.GetSymbol<Symbol.Constant>(varRef.Name).MatchSome(constant
                    => _messenger.Report(Message.ErrorConstantAssignment(assignment, constant)));
            }
            AnalyzeExpression(scope, assignment.Value);
            break;

        case Statement.DoWhileLoop doWhileLoop:
            AnalyzeScopedBlock(doWhileLoop);
            AnalyzeExpression(scope, doWhileLoop.Condition);
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
            break;

        case Statement.Return ret:
            AnalyzeExpression(scope, ret.Value);
            break;

        case Statement.LocalVariable varDecl:
            var type = EvaluateType(scope, varDecl.Type);
            foreach (var name in varDecl.Names) {
                scope.AddSymbolOrError(_messenger, new Symbol.Variable(name, varDecl.SourceTokens, type));
            }
            break;

        case Statement.WhileLoop whileLoop:
            AnalyzeScopedBlock(whileLoop);
            break;

        case Statement.Switch switchCase:
            AnalyzeExpression(scope, switchCase.Expression);
            foreach (var @case in switchCase.Cases) {
                AnalyzeExpression(scope, @case.When);
                SetParentScope(@case, scope);
                AnalyzeScopedBlock(@case);
            }
            switchCase.Default.MatchSome(@default => {
                SetParentScope(@default, scope);
                AnalyzeScopedBlock(@default);
            });
            break;

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

                AnalyzeExpression(scope, actual.Value).Type
                    .When(t => !t.IsAssignableTo(formal.Type))
                    .MatchSome(t => problems.Add(Message.ProblemWrongArgumentType(formal.Name, formal.Type, t)));
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
     => GetConstantExpression<Value.Integer, int>(scope, str.Length)
        .MatchError(e => e.MatchSome(_messenger.Report))
        .Map(EvaluatedType.LengthedString.Create)
        .ValueOr(EvaluatedType.Unknown.Declared(_messenger.Input, str));

    EvaluatedType EvaluateArrayType(ReadOnlyScope scope, Type.Complete.Array array)
     => array.Dimensions.Select(d => GetConstantExpression<Value.Integer, int>(scope, d)).Accumulate()
        .MatchError(Function.Foreach<Option<Message>>(e => e.MatchSome(_messenger.Report)))
        .Map(values => EvaluatedType.Array.Create(EvaluateType(scope, array.Type), values.ToList()))
        .ValueOr(EvaluatedType.Unknown.Declared(_messenger.Input, array));

    Option<ConstantExpression<TValue>, Option<Message>> GetConstantExpression<TVal, TValue>(ReadOnlyScope scope, Expression expr)
        where TVal : Value<TVal>, Value<TVal, TValue>
    {
        var value = AnalyzeExpression(scope, expr);
        return value is TVal tval
            ? tval.Value.Map(val => ConstantExpression.Create(expr, val))
                .OrWithError(Message.ErrorConstantExpressionExpected(expr.SourceTokens).Some())
            : value.Type.Map(t => Message.ErrorExpressionHasWrongType(expr.SourceTokens, TVal.ExpectedType, t))
                .None<ConstantExpression<TValue>, Option<Message>>();
    }

    EvaluatedType.Structure EvaluateStructureType(ReadOnlyScope scope, Type.Complete.Structure structure)
    {
        Dictionary<Identifier, EvaluatedType> components = [];
        foreach (var comp in structure.Components) {
            foreach (var name in comp.Names) {
                if (!components.TryAdd(name, EvaluateType(scope, comp.Type))) {
                    _messenger.Report(Message.ErrorStructureDuplicateComponent(comp.SourceTokens, name));
                }
            }
        }
        return new EvaluatedType.Structure(components);
    }
}
