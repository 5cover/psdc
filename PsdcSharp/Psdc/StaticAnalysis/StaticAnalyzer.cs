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
    readonly Messenger _msger;

    bool _seenMainProgram;
    ValueOption<Symbol.Function> _currentFunction;

    StaticAnalyzer(Messenger messenger) => _msger = messenger;

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
                _msger.Report(Message.ErrorRedefinedSymbol(param, existingSymbol));
            }
        }
    }

    void AnalyzeCallableDefinition<T>(Scope scope, BlockNode node, T sub) where T : Symbol.Callable, IEquatable<T?>
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
            scope.AddSymbolOrError(_msger, new Symbol.TypeAlias(alias.Name, alias.SourceTokens,
                EvaluateType(scope, alias.Type)));
            break;

        case Declaration.Constant constant: {
            var value = AnalyzeExpression(scope, constant.Value);
            var declaredType = EvaluateType(scope, constant.Type);

            if (value.Value is not ValueStatus.Comptime) {
                _msger.Report(Message.ErrorConstantExpressionExpected(constant.Value.SourceTokens));
            }

            if (!value.Type.IsConvertibleTo(declaredType)) {
                _msger.Report(Message.ErrorExpressionHasWrongType(
                    constant.Value.SourceTokens, declaredType, value.Type));

                // if the provided value wasn't of the right type, use a non-const value of the declared type.
                // the constant is still added to the scope.
                value = declaredType.UninitializedValue;
            }

            scope.AddSymbolOrError(_msger,
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
                _msger.Report(Message.ErrorRedefinedMainProgram(mainProgram));
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

    delegate bool TypeComparer(EvaluatedType actual, EvaluatedType expected);

    Value AnalyzeInitializer<TType>(ReadOnlyScope scope, Initializer initializer, TypeComparer typeComparer, TType targetType) where TType : EvaluatedType
    {
        switch (initializer) {
        case Expression expr:
            return AnalyzeExpression(scope, expr, typeComparer, targetType);
        case Initializer.Braced<Designator.Array> array: {
            // check if target type is array, same elements, same length
            if (targetType is not ArrayType targetArrayType) {
                _msger.Report(Message.ErrorUnsupportedInitializer(initializer.SourceTokens, targetType));
                return targetType.UninitializedValue;
            }

            var dimensions = targetArrayType.Dimensions.Select(d => d.Value).ToArray();

            Value[] arrayItems = Enumerable.Repeat(
                targetArrayType.ItemType.DefaultValue,
                dimensions.Product()).ToArray();

            int currentIndex = 0;

            foreach (var value in array.Values) {
                (int flatIndex, Option<int[]> index) = value.Designator.Match(
                    d => {
                        if (d.Index.Count != dimensions.Length) {
                            _msger.Report(Message.ErrorIndexWrongRank(d.SourceTokens, d.Index.Count, dimensions.Length));
                            return (currentIndex, Option.None<int[]>());
                        }
                        return d.Index
                        .Select(i => GetConstantExpression<IntegerType, int>(IntegerType.Instance, scope, i))
                        .Accumulate()
                        .MatchError(Function.Foreach<Message>(_msger.Report))
                        .Match(index => {
                            var actualIndex = index.Select(i => i.Value - 1).ToArray();
                            return (actualIndex.FlattenedIndex(dimensions.Select(d => d - 1)), actualIndex.Some());
                        },
                        () => (currentIndex, Option.None<int[]>())); // stay in the same place if designator invalid
                    },
                    () => (currentIndex++, Option.None<int[]>()));

                if (flatIndex >= 0 && flatIndex < arrayItems.Length) {
                    arrayItems[flatIndex] = AnalyzeInitializer(scope, value.Initializer, typeComparer, targetArrayType.ItemType);
                } else {
                    _msger.Report(index.Match(
                        index => Message.ErrorIndexOutOfBounds(value.SourceTokens, index.Select(i => i + 1).ToArray(), dimensions),
                        () => Message.ErrorExcessElementInArrayInitializer(value.SourceTokens)));
                }
            }

            return targetArrayType.Instantiate(arrayItems);
        }
        case Initializer.Braced<Designator.Structure> structure: {
            throw new NotImplementedException(); // todo: implement
        }
        default:
            throw initializer.ToUnmatchedException();
        }
    }

    Value AnalyzeExpression(ReadOnlyScope scope, Expression expr, TypeComparer typeComparer, EvaluatedType expectedType)
    {
        var value = AnalyzeExpression(scope, expr);
        if (!typeComparer(value.Type, expectedType)) {
            _msger.Report(Message.ErrorExpressionHasWrongType(expr.SourceTokens, expectedType, value.Type));
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
            foreach (var error in res.Messages) {
                _msger.Report(error.GetOperationMessage(opBin, left.Type, right.Type));
            }
            return res.Value;
        }
        case Expression.UnaryOperation opUn: {
            var operand = AnalyzeExpression(scope, opUn.Operand);
            var res = opUn.Operator.EvaluateOperation(operand);
            foreach (var error in res.Messages) {
                _msger.Report(error.GetOperationMessage(opUn, operand.Type));
            }
            return res.Value;
        }
        case Expression.FunctionCall call:
            return HandleCall<Symbol.Function>(scope, call)
                .Map(f => f.ReturnType).ValueOr(UnknownType.Inferred)
                .RuntimeValue;

        case Expression.BuiltinFdf fdf:
            AnalyzeExpression(scope, fdf.ArgumentNomLog);
            return BooleanType.Instance.RuntimeValue;

        case Expression.Lvalue.ArraySubscript arrSub: {
            foreach (var i in arrSub.Index) {
                var indexType = AnalyzeExpression(scope, i).Type;
                if (!indexType.IsConvertibleTo(IntegerType.Instance)) {
                    _msger.Report(Message.ErrorNonIntegerIndex(i.SourceTokens, indexType));
                }
            }

            var actualType = AnalyzeExpression(scope, arrSub.Array).Type;

            if (actualType is ArrayType arrayType) {
                // todo: get comptime value
                return arrayType.ItemType.RuntimeValue;
            } else {
                _msger.Report(Message.ErrorSubscriptOfNonArray(arrSub, actualType));
                return UnknownType.Inferred.DefaultValue;
            }
        }
        case Expression.Lvalue.ComponentAccess compAccess: {
            var actualType = AnalyzeExpression(scope, compAccess.Structure).Type;
            if (actualType is not StructureType structType) {
                _msger.Report(Message.ErrrorComponentAccessOfNonStruct(compAccess, actualType));
            } else if (!structType.Components.Map.TryGetValue(compAccess.ComponentName, out var componentType)) {
                _msger.Report(Message.ErrorStructureComponentDoesntExist(compAccess, structType.Alias));
            } else {
                // todo: get comptime value
                return componentType.RuntimeValue;
            }
            return UnknownType.Inferred.DefaultValue;
        }
        case Expression.Lvalue.VariableReference varRef:
            return scope.GetSymbol<Symbol.NameTypeBinding>(varRef.Name)
            .MatchError(_msger.Report)
            .Map(variable => variable is Symbol.Constant constant
                ? constant.Value
                : variable.Type.RuntimeValue)
            .ValueOr(UnknownType.Inferred.DefaultValue);
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
            AnalyzeExpression(scope, alternative.If.Condition, EvaluatedType.IsConvertibleTo, BooleanType.Instance);
            SetParentScope(alternative.If, scope);
            AnalyzeScopedBlock(alternative.If);
            foreach (var elseIf in alternative.ElseIfs) {
                AnalyzeExpression(scope, elseIf.Condition, EvaluatedType.IsConvertibleTo, BooleanType.Instance);
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
                    => _msger.Report(Message.ErrorConstantAssignment(assignment, constant)));
            }
            AnalyzeExpression(scope, assignment.Value, EvaluatedType.IsAssignableTo, target.Type);
            break;
        }
        case Statement.DoWhileLoop doWhileLoop:
            AnalyzeScopedBlock(doWhileLoop);
            AnalyzeExpression(scope, doWhileLoop.Condition, EvaluatedType.IsConvertibleTo, BooleanType.Instance);
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
            AnalyzeExpression(scope, repeatLoop.Condition, EvaluatedType.IsConvertibleTo, BooleanType.Instance);
            break;

        case Statement.Return ret:
            _currentFunction.Match(
                func => AnalyzeExpression(scope, ret.Value, EvaluatedType.IsConvertibleTo, func.ReturnType),
                () => {
                    AnalyzeExpression(scope, ret.Value);
                    _msger.Report(Message.ErrorReturnInNonFunction(ret.SourceTokens));
                });
            break;

        case Statement.LocalVariable varDecl: {
            var type = EvaluateType(scope, varDecl.Binding.Type);
            // here: evaluated the initializer and produce its value (of type varDecl.Binding.Type)
            var initializer = varDecl.Initializer.Map(i => AnalyzeInitializer(scope, i, EvaluatedType.IsAssignableTo, type));
            foreach (var name in varDecl.Binding.Names) {
                scope.AddSymbolOrError(_msger, new Symbol.Variable(name, varDecl.SourceTokens, type, initializer));
            }
            break;
        }
        case Statement.WhileLoop whileLoop: {
            AnalyzeScopedBlock(whileLoop);
            AnalyzeExpression(scope, whileLoop.Condition, EvaluatedType.IsConvertibleTo, BooleanType.Instance);
            break;
        }
        case Statement.Switch switchCase: {
            var type = AnalyzeExpression(scope, switchCase.Expression).Type;
            foreach (var @case in switchCase.Cases) {
                SetParentScope(@case, scope);

                var when = AnalyzeExpression(scope, @case.When, EvaluatedType.IsConvertibleTo, type);
                if (when.Value is not ValueStatus.Comptime) {
                    _msger.Report(Message.ErrorConstantExpressionExpected(@case.When.SourceTokens));
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

    Option<TSymbol> HandleCall<TSymbol>(ReadOnlyScope scope, NodeCall call) where TSymbol : Symbol.Callable
    {
        var symbol = scope.GetSymbol<TSymbol>(call.Name).MatchError(_msger.Report);
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
                _msger.Report(Message.ErrorCallParameterMismatch(call.SourceTokens, callable, problems));
            }
        }, none: () => {
            // If the callable symbol wasn't found, still analyze the parameter expressions.
            foreach (var actual in call.Parameters) {
                AnalyzeExpression(scope, actual.Value);
            }
        });
        return symbol;
    }

    void HandleCallableDeclaration<T>(Scope scope, T sub) where T : Symbol.Callable
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

    void HandleCallableDefinition<T>(Scope scope, T sub) where T : Symbol.Callable
    {
        if (scope.TryAdd(sub, out var existingSymbol)) {
            sub.MarkAsDefined();
        } else if (existingSymbol is T existingSub) {
            if (existingSub.HasBeenDefined) {
                _msger.Report(Message.ErrorRedefinedSymbol(sub, existingSub));
            } else if (!sub.SemanticsEqual(existingSub)) {
                _msger.Report(Message.ErrorSignatureMismatch(sub, existingSub));
            }
        } else {
            _msger.Report(Message.ErrorRedefinedSymbol(sub, existingSymbol));
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
         => scope.GetSymbol<Symbol.TypeAlias>(alias.Name).MatchError(_msger.Report)
                .Map(aliasType => aliasType.TargetType.ToAliasReference(alias.Name))
            .ValueOr(UnknownType.Declared(_msger.Input, type)),
        Type.Complete.Array array => EvaluateArrayType(scope, array),
        Type.Complete.Boolean => BooleanType.Instance,
        Type.Complete.Character => CharacterType.Instance,
        Type.Complete.File file => FileType.Instance,
        Type.Complete.Integer p => IntegerType.Instance,
        Type.Complete.LengthedString str => EvaluateLengthedStringType(scope, str),
        Type.Complete.Real p => RealType.Instance,
        Type.Complete.Structure structure => EvaluateStructureType(scope, structure),
        Type.String => StringType.Instance,
        _ => throw type.ToUnmatchedException(),
    };

    EvaluatedType EvaluateLengthedStringType(ReadOnlyScope scope, Type.Complete.LengthedString str)
     => GetConstantExpression<IntegerType, int>(IntegerType.Instance, scope, str.Length)
        .MatchError(_msger.Report)
        .Map(LengthedStringType.Create)
        .ValueOr<EvaluatedType>(UnknownType.Declared(_msger.Input, str));

    EvaluatedType EvaluateArrayType(ReadOnlyScope scope, Type.Complete.Array array)
     => array.Dimensions.Select(d => GetConstantExpression<IntegerType, int>(IntegerType.Instance, scope, d)).Accumulate()
        .MatchError(Function.Foreach<Message>(_msger.Report))
        .Map(values => new ArrayType(EvaluateType(scope, array.Type), values.ToList()))
        .ValueOr<EvaluatedType>(UnknownType.Declared(_msger.Input, array));

    Option<ConstantExpression<TUnderlying>, Message> GetConstantExpression<TType, TUnderlying>(TType type, ReadOnlyScope scope, Expression expr)
    where TType : EvaluatedType
    {
        var value = AnalyzeExpression(scope, expr);
        return value is Value<TType, TUnderlying> tval
            ? tval.Value.Comptime.Map(v =>
                ConstantExpression.Create(expr, v))
                .OrWithError(Message.ErrorConstantExpressionExpected(expr.SourceTokens))
            : Message.ErrorExpressionHasWrongType(expr.SourceTokens, type, value.Type).None<ConstantExpression<TUnderlying>, Message>();
    }

    StructureType EvaluateStructureType(ReadOnlyScope scope, Type.Complete.Structure structure)
    {
        Dictionary<Identifier, EvaluatedType> componentsMap = [];
        List<KeyValuePair<Identifier, EvaluatedType>> componentsList = [];
        foreach (var comp in structure.Components) {
            foreach (var name in comp.Names) {
                var type = EvaluateType(scope, comp.Type);
                if (!componentsMap.TryAdd(name, type)) {
                    _msger.Report(Message.ErrorStructureDuplicateComponent(comp.SourceTokens, name));
                }
                componentsList.Add(new(name, type));
            }
        }
        return new StructureType(new(componentsMap, componentsList));
    }
}
