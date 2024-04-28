using Scover.Psdc.Parsing;

namespace Scover.Psdc.StaticAnalysis;

internal sealed class SemanticAst(Node.Algorithm root, IReadOnlyDictionary<ScopedNode, Scope> scopes)
{
    public Node.Algorithm Root => root;

    public IReadOnlyDictionary<ScopedNode, Scope> Scopes => scopes;
}

internal sealed class StaticAnalyzer(Messenger messenger, Node.Algorithm root)
{
    public SemanticAst AnalyzeSemantics()
    {
        Dictionary<ScopedNode, Scope> scopes = [];
        bool seenMainProgram = false;

        SetParentScope(root, null);
        foreach (var decl in root.Declarations) {
            AnalyzeDeclaration(scopes[root], decl);
        }

        if (!seenMainProgram) {
            messenger.Report(Message.ErrorMissingMainProgram(root.SourceTokens));
        }

        return new(root, scopes);

        void AnalyzeDeclaration(Scope scope, Node.Declaration decl)
        {
            SetParentScopeIfNecessary(decl, scope);

            switch (decl) {
            case Node.Declaration.TypeAlias alias:
                alias.Type.CreateTypeOrError(scope, messenger).MatchSome(type
                 => scope.AddSymbolOrError(messenger, new Symbol.TypeAlias(alias.Name, alias.SourceTokens, type)));
                break;

            case Node.Declaration.Constant constant:
                constant.Type.CreateTypeOrError(scope, messenger)
                .Combine(constant.Value.EvaluateType(scope).MapError(messenger.Report))
                .MatchSome((declaredType, inferredType) => {
                    if (!declaredType.Equals(inferredType)) {
                        messenger.Report(Message.ErrorDeclaredInferredTypeMismatch(constant.SourceTokens, declaredType, inferredType));
                    }
                    if (!constant.Value.IsConstant(scope)) {
                        messenger.Report(Message.ErrorExpectedConstantExpression(constant.SourceTokens));
                    }
                    scope.AddSymbolOrError(messenger, new Symbol.Constant(constant.Name, constant.SourceTokens, declaredType, constant.Value));
                });
                break;

            case Node.Declaration.Function func:
                func.Signature.ReturnType.CreateTypeOrError(scope, messenger).MatchSome(returnType
                 => HandleCallableDeclaration(scope, new Symbol.Function(
                       func.Signature.Name,
                       func.Signature.SourceTokens,
                       CreateParameters(scope, func.Signature.Parameters),
                       returnType)));
                break;

            case Node.Declaration.FunctionDefinition funcDef:
                funcDef.Signature.ReturnType.CreateTypeOrError(scope, messenger).MatchSome(returnType
                 => AnalyzeCallableDefinition(scope, funcDef, new Symbol.Function(
                        funcDef.Signature.Name,
                        funcDef.Signature.SourceTokens,
                        CreateParameters(scope, funcDef.Signature.Parameters),
                        returnType)));
                break;

            case Node.Declaration.MainProgram mainProgram:
                if (seenMainProgram) {
                    messenger.Report(Message.ErrorRedefinedMainProgram(mainProgram));
                }
                seenMainProgram = true;
                AnalyzeScopedBlock(mainProgram);
                break;

            case Node.Declaration.Procedure proc:
                HandleCallableDeclaration(scope, new Symbol.Procedure(
                    proc.Signature.Name,
                    proc.Signature.SourceTokens,
                    CreateParameters(scope, proc.Signature.Parameters)));
                break;

            case Node.Declaration.ProcedureDefinition procDef:
                AnalyzeCallableDefinition(scope, procDef, new Symbol.Procedure(
                    procDef.Signature.Name,
                    procDef.Signature.SourceTokens,
                    CreateParameters(scope, procDef.Signature.Parameters)));
                break;

            default:
                throw decl.ToUnmatchedException();
            }
        }

        void AnalyzeStatement(Scope scope, Node.Statement stmt, Action<Node.Expression>? hookExpr = null)
        {
            SetParentScopeIfNecessary(stmt, scope);

            switch (stmt) {
            case Node.Statement.Nop nop:
                break;
            case Node.Statement.Alternative alternative:
                AnalyzeExpression(scope, alternative.If.Condition, hookExpr);
                SetParentScope(alternative.If, scope);
                AnalyzeScopedBlock(alternative.If);
                foreach (var elseIf in alternative.ElseIfs) {
                    AnalyzeExpression(scope, elseIf.Condition, hookExpr);
                    SetParentScope(elseIf, scope);
                    AnalyzeScopedBlock(elseIf);
                }
                alternative.Else.MatchSome(@else => {
                    SetParentScope(@else, scope);
                    AnalyzeScopedBlock(@else);
                });
                break;
            case Node.Statement.Assignment assignment:
                AnalyzeExpression(scope, assignment.Target, hookExpr);
                if (assignment.Target is Node.Expression.Lvalue.VariableReference varRef
                    && scope.TryGetSymbolOrError<Symbol.Variable>(messenger, varRef.Name, out var targetVar)) {
                    if (targetVar is Symbol.Constant constant) {
                        messenger.Report(Message.ErrorConstantAssignment(assignment, constant));
                    }
                }
                AnalyzeExpression(scope, assignment.Value, hookExpr);
                break;
            case Node.Statement.DoWhileLoop doWhileLoop:
                AnalyzeScopedBlock(doWhileLoop);
                AnalyzeExpression(scope, doWhileLoop.Condition, hookExpr);
                break;
            case Node.Statement.BuiltinEcrire ecrire:
                AnalyzeExpression(scope, ecrire.ArgumentNomLog, hookExpr);
                AnalyzeExpression(scope, ecrire.ArgumentExpression, hookExpr);
                break;
            case Node.Statement.BuiltinFermer fermer:
                AnalyzeExpression(scope, fermer.ArgumentNomLog, hookExpr);
                break;
            case Node.Statement.ForLoop forLoop:
                AnalyzeExpression(scope, forLoop.Start, hookExpr);
                forLoop.Step.MatchSome(step => AnalyzeExpression(scope, step, hookExpr));
                AnalyzeExpression(scope, forLoop.End, hookExpr);
                AnalyzeScopedBlock(forLoop);
                break;
            case Node.Statement.BuiltinLire lire:
                AnalyzeExpression(scope, lire.ArgumentNomLog, hookExpr);
                AnalyzeExpression(scope, lire.ArgumentVariable, hookExpr);
                break;
            case Node.Statement.BuiltinOuvrirAjout ouvrirAjout:
                AnalyzeExpression(scope, ouvrirAjout.ArgumentNomLog, hookExpr);
                break;
            case Node.Statement.BuiltinOuvrirEcriture ouvrirEcriture:
                AnalyzeExpression(scope, ouvrirEcriture.ArgumentNomLog, hookExpr);
                break;
            case Node.Statement.BuiltinOuvrirLecture ouvrirLecture:
                AnalyzeExpression(scope, ouvrirLecture.ArgumentNomLog, hookExpr);
                break;
            case Node.Statement.ProcedureCall call:
                HandleCall<Symbol.Procedure>(scope, call);
                break;
            case Node.Statement.BuiltinEcrireEcran ecrireEcran:
                foreach (var arg in ecrireEcran.Arguments) {
                    AnalyzeExpression(scope, arg, hookExpr);
                }
                break;
            case Node.Statement.BuiltinLireClavier lireClavier:
                AnalyzeExpression(scope, lireClavier.ArgumentVariable, hookExpr);
                break;
            case Node.Statement.RepeatLoop repeatLoop:
                AnalyzeScopedBlock(repeatLoop);
                break;
            case Node.Statement.Return ret:
                AnalyzeExpression(scope, ret.Value, hookExpr);
                break;
            case Node.Statement.LocalVariable varDecl:
                varDecl.Type.CreateTypeOrError(scope, messenger).MatchSome(type => {
                    foreach (var name in varDecl.Names) {
                        scope.AddSymbolOrError(messenger, new Symbol.Variable(name, varDecl.SourceTokens, type));
                    }
                });
                break;
            case Node.Statement.WhileLoop whileLoop:
                AnalyzeScopedBlock(whileLoop);
                break;
            case Node.Statement.Switch switchCase:
                AnalyzeExpression(scope, switchCase.Expression, hookExpr);
                foreach (var @case in switchCase.Cases) {
                    AnalyzeExpression(scope, @case.When, hookExpr);
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

        void AnalyzeExpression(Scope scope, Node.Expression expr, Action<Node.Expression>? hookExpr = null)
        {
            SetParentScopeIfNecessary(expr, scope);
            hookExpr?.Invoke(expr);

            switch (expr) {
            case Node.Expression.Bracketed bracketed:
                AnalyzeExpression(scope, bracketed.Expression, hookExpr);
                break;
            case Node.Expression.Lvalue.Bracketed bracketed:
                AnalyzeExpression(scope, bracketed.Lvalue, hookExpr);
                break;
            case Node.Expression.BuiltinFdf fdf:
                AnalyzeExpression(scope, fdf.ArgumentNomLog, hookExpr);
                break;
            case Node.Expression.FunctionCall call:
                HandleCall<Symbol.Function>(scope, call);
                break;
            case Node.Expression.Literal literal:
                break;
            case Node.Expression.OperationBinary opBin:
                AnalyzeExpression(scope, opBin.Operand1, hookExpr);
                if (opBin.Operator is BinaryOperator.Divide
                 && opBin.Operand2.EvaluateValue(scope)
                    .FlatMapError(messenger.Report)
                    .FlatMap(Parse.ToInt32)
                    .Map(val => val == 0)
                    .ValueOr(false)) {
                    messenger.Report(Message.WarningDivisionByZero(opBin.SourceTokens));
                }
                AnalyzeExpression(scope, opBin.Operand2, hookExpr);
                break;
            case Node.Expression.OperationUnary opUn:
                AnalyzeExpression(scope, opUn.Operand, hookExpr);
                break;
            case Node.Expression.Lvalue.ArraySubscript arraySub:
                AnalyzeExpression(scope, arraySub.Array, hookExpr);
                foreach (var i in arraySub.Indexes) {
                    AnalyzeExpression(scope, i, hookExpr);
                }
                break;
            case Node.Expression.Lvalue.ComponentAccess componentAccess:
                AnalyzeExpression(scope, componentAccess.Structure, hookExpr);
                break;
            case Node.Expression.Lvalue.VariableReference varRef:
                _ = scope.GetSymbol<Symbol.Variable>(varRef.Name).MapError(messenger.Report);
                break;
            default:
                throw expr.ToUnmatchedException();
            }
        }

        void SetParentScopeIfNecessary(Node node, Scope? parentScope)
        {
            if (node is ScopedNode sn) {
                SetParentScope(sn, parentScope);
            }
        }

        void SetParentScope(ScopedNode scopedNode, Scope? parentScope) => scopes.Add(scopedNode, new(parentScope));

        void AddParameters(Scope scope, IEnumerable<Symbol.Parameter> parameters)
        {
            foreach (var param in parameters) {
                if (!scope.TryAdd(param, out var existingSymbol)) {
                    messenger.Report(Message.ErrorRedefinedSymbol(param, existingSymbol));
                }
            }
        }

        List<Symbol.Parameter> CreateParameters(ReadOnlyScope scope, IEnumerable<Node.ParameterFormal> parameters)
         => parameters.Select(param => param.Type.CreateTypeOrError(scope, messenger).Map(type
            => new Symbol.Parameter(param.Name, param.SourceTokens, type, param.Mode))).WhereSome().ToList();

        void HandleCall<TSymbol>(Scope scope, CallNode call) where TSymbol : CallableSymbol
        {
            scope.GetSymbol<TSymbol>(call.Name).MapError(messenger.Report).MatchSome(callee => {
                if (!call.Parameters.ZipStrict(callee.Parameters, (effective, formal)
                    => effective.Mode.Equals(formal.Mode)
                    && effective.Value.EvaluateType(scope).MapError(messenger.Report)
                       .Map(type => formal.Type.Equals(type)).ValueOr(false))
                 .All(b => b)) {
                    messenger.Report(Message.ErrorCallParameterMismatch(call));
                }
            });
            foreach (var effective in call.Parameters) {
                AnalyzeExpression(scope, effective.Value);
            }
        }

        void HandleCallableDeclaration<T>(Scope scope, T sub) where T : CallableSymbol, IEquatable<T?>
        {
            if (!scope.TryAdd(sub, out var existingSymbol)) {
                if (existingSymbol is T existingSub) {
                    if (!sub.Equals(existingSub)) {
                        messenger.Report(Message.ErrorSignatureMismatch(sub, existingSub));
                    }
                } else {
                    messenger.Report(Message.ErrorRedefinedSymbol(sub, existingSymbol));
                }
            }
        }

        void AnalyzeCallableDefinition<T>(Scope scope, BlockNode node, T sub) where T : CallableSymbol, IEquatable<T?>
        {
            HandleCallableDefinition(scope, sub);
            AddParameters(scopes[node], sub.Parameters);

            Dictionary<Identifier, bool> outputParametersAssigned = sub.Parameters
                .Where(param => param.Mode is ParameterMode.Out)
                .ToDictionary(keySelector: param => param.Name, elementSelector: _ => false);

            AnalyzeScopedBlock(node, hookExpr: expr => {
                if (expr is Node.Expression.Lvalue.VariableReference varRef
                 && outputParametersAssigned.ContainsKey(varRef.Name)) {
                    outputParametersAssigned[varRef.Name] = true;
                }
            });

            foreach (var unassignedOutParam in outputParametersAssigned.Where(kv => !kv.Value).Select(kv => kv.Key)) {
                messenger.Report(Message.ErrorOutputParameterNeverAssigned(unassignedOutParam));
            }
        }

        void HandleCallableDefinition<T>(Scope scope, T sub) where T : CallableSymbol, IEquatable<T?>
        {
            if (scope.TryAdd(sub, out var existingSymbol)) {
                sub.MarkAsDefined();
            } else if (existingSymbol is T existingSub) {
                if (existingSub.HasBeenDefined) {
                    messenger.Report(Message.ErrorRedefinedSymbol(sub, existingSub));
                } else if (!sub.Equals(existingSub)) {
                    messenger.Report(Message.ErrorSignatureMismatch(sub, existingSub));
                }
            } else {
                messenger.Report(Message.ErrorRedefinedSymbol(sub, existingSymbol));
            }
        }

        void AnalyzeScopedBlock(BlockNode scopedBlock, Action<Node.Expression>? hookExpr = null)
        {
            foreach (var stmt in scopedBlock.Block) {
                AnalyzeStatement(scopes[scopedBlock], stmt, hookExpr);
            }
        }
    }
}
