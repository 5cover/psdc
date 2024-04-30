using Scover.Psdc.Parsing;
using static Scover.Psdc.Parsing.Node;

namespace Scover.Psdc.StaticAnalysis;

internal sealed class SemanticAst(Algorithm root, IReadOnlyDictionary<NodeScoped, Scope> scopes)
{
    public Algorithm Root => root;

    public IReadOnlyDictionary<NodeScoped, Scope> Scopes => scopes;
}

internal sealed class StaticAnalyzer(Messenger messenger, Algorithm root)
{
    public SemanticAst AnalyzeSemantics()
    {
        Dictionary<NodeScoped, Scope> scopes = [];
        bool seenMainProgram = false;

        SetParentScope(root, null);
        foreach (var decl in root.Declarations) {
            AnalyzeDeclaration(scopes[root], decl);
        }

        if (!seenMainProgram) {
            messenger.Report(Message.ErrorMissingMainProgram(root.SourceTokens));
        }

        return new(root, scopes);

        void AnalyzeDeclaration(Scope scope, Declaration decl)
        {
            SetParentScopeIfNecessary(decl, scope);

            switch (decl) {
            case Declaration.TypeAlias alias:
                scope.AddSymbolOrError(messenger, new Symbol.TypeAlias(alias.Name, alias.SourceTokens,
                    alias.Type.CreateTypeOrError(scope, messenger)));
                break;

            case Declaration.Constant constant:
                constant.Value.EvaluateType(scope).MatchError(messenger.Report).MatchSome(inferredType => {
                    var declaredType = constant.Type.CreateTypeOrError(scope, messenger);

                    if (!declaredType.SemanticsEqual(inferredType)) {
                        messenger.Report(Message.ErrorDeclaredInferredTypeMismatch(constant.SourceTokens, declaredType, inferredType));
                    }
                    if (!constant.Value.IsConstant(scope)) {
                        messenger.Report(Message.ErrorConstantExpressionExpected(constant.SourceTokens));
                    }
                    scope.AddSymbolOrError(messenger, new Symbol.Constant(constant.Name, constant.SourceTokens, declaredType, constant.Value));
                });
                break;

            case Declaration.Function func:
                HandleCallableDeclaration(scope, new Symbol.Function(
                      func.Signature.Name,
                      func.Signature.SourceTokens,
                      CreateParameters(scope, func.Signature.Parameters),
                      func.Signature.ReturnType.CreateTypeOrError(scope, messenger)));
                break;

            case Declaration.FunctionDefinition funcDef:
                AnalyzeCallableDefinition(scope, funcDef, new Symbol.Function(
                        funcDef.Signature.Name,
                        funcDef.Signature.SourceTokens,
                        CreateParameters(scope, funcDef.Signature.Parameters),
                        funcDef.Signature.ReturnType.CreateTypeOrError(scope, messenger)));
                break;

            case Declaration.MainProgram mainProgram:
                if (seenMainProgram) {
                    messenger.Report(Message.ErrorRedefinedMainProgram(mainProgram));
                }
                seenMainProgram = true;
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

        void AnalyzeStatement(Scope scope, Statement stmt, Action<Expression>? hookExpr = null)
        {
            SetParentScopeIfNecessary(stmt, scope);

            switch (stmt) {
            case Statement.Nop nop:
                break;

            case Statement.Alternative alternative:
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

            case Statement.Assignment assignment:
                AnalyzeExpression(scope, assignment.Target, hookExpr);
                if (assignment.Target is Expression.Lvalue.VariableReference varRef) {
                    scope.GetSymbol<Symbol.Constant>(varRef.Name).MatchSome(constant
                     => messenger.Report(Message.ErrorConstantAssignment(assignment, constant)));
                }
                AnalyzeExpression(scope, assignment.Value, hookExpr);
                break;

            case Statement.DoWhileLoop doWhileLoop:
                AnalyzeScopedBlock(doWhileLoop);
                AnalyzeExpression(scope, doWhileLoop.Condition, hookExpr);
                break;

            case Statement.Builtin.Ecrire ecrire:
                AnalyzeExpression(scope, ecrire.ArgumentNomLog, hookExpr);
                AnalyzeExpression(scope, ecrire.ArgumentExpression, hookExpr);
                break;

            case Statement.Builtin.Fermer fermer:
                AnalyzeExpression(scope, fermer.ArgumentNomLog, hookExpr);
                break;

            case Statement.ForLoop forLoop:
                AnalyzeExpression(scope, forLoop.Start, hookExpr);
                forLoop.Step.MatchSome(step => AnalyzeExpression(scope, step, hookExpr));
                AnalyzeExpression(scope, forLoop.End, hookExpr);
                AnalyzeScopedBlock(forLoop);
                break;

            case Statement.Builtin.Lire lire:
                AnalyzeExpression(scope, lire.ArgumentNomLog, hookExpr);
                AnalyzeExpression(scope, lire.ArgumentVariable, hookExpr);
                break;

            case Statement.Builtin.OuvrirAjout ouvrirAjout:
                AnalyzeExpression(scope, ouvrirAjout.ArgumentNomLog, hookExpr);
                break;

            case Statement.Builtin.OuvrirEcriture ouvrirEcriture:
                AnalyzeExpression(scope, ouvrirEcriture.ArgumentNomLog, hookExpr);
                break;

            case Statement.Builtin.OuvrirLecture ouvrirLecture:
                AnalyzeExpression(scope, ouvrirLecture.ArgumentNomLog, hookExpr);
                break;

            case Statement.ProcedureCall call:
                HandleCall<Symbol.Procedure>(scope, call);
                break;
            case Statement.Builtin.EcrireEcran ecrireEcran:
                foreach (var arg in ecrireEcran.Arguments) {
                    AnalyzeExpression(scope, arg, hookExpr);
                }
                break;

            case Statement.Builtin.LireClavier lireClavier:
                AnalyzeExpression(scope, lireClavier.ArgumentVariable, hookExpr);
                break;

            case Statement.RepeatLoop repeatLoop:
                AnalyzeScopedBlock(repeatLoop);
                break;

            case Statement.Return ret:
                AnalyzeExpression(scope, ret.Value, hookExpr);
                break;

            case Statement.LocalVariable varDecl:
                var type = varDecl.Type.CreateTypeOrError(scope, messenger);
                foreach (var name in varDecl.Names) {
                    scope.AddSymbolOrError(messenger, new Symbol.Variable(name, varDecl.SourceTokens, type));
                }
                break;

            case Statement.WhileLoop whileLoop:
                AnalyzeScopedBlock(whileLoop);
                break;

            case Statement.Switch switchCase:
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

        void AnalyzeExpression(Scope scope, Expression expr, Action<Expression>? hookExpr = null)
        {
            SetParentScopeIfNecessary(expr, scope);
            hookExpr?.Invoke(expr);

            // Try to evaluate the expression as a constant.
            // This will fold any constants the expression contains and perform various checks.
            // No message is reported if the expression isn't constant.
            // Detects indirect division by zero errors. Example : (4 / (2 + 3 - 5))
            expr.EvaluateValue(scope)
                .MatchError(e => e.MatchSome(messenger.Report));
        }

        void SetParentScopeIfNecessary(Node node, Scope? parentScope)
        {
            if (node is NodeScoped sn) {
                SetParentScope(sn, parentScope);
            }
        }

        void SetParentScope(NodeScoped scopedNode, Scope? parentScope) => scopes.Add(scopedNode, new(parentScope));

        void AddParameters(Scope scope, IEnumerable<Symbol.Parameter> parameters)
        {
            foreach (var param in parameters) {
                if (!scope.TryAdd(param, out var existingSymbol)) {
                    messenger.Report(Message.ErrorRedefinedSymbol(param, existingSymbol));
                }
            }
        }

        List<Symbol.Parameter> CreateParameters(ReadOnlyScope scope, IEnumerable<ParameterFormal> parameters)
         => parameters.Select(param
             => new Symbol.Parameter(param.Name, param.SourceTokens,
                    param.Type.CreateTypeOrError(scope, messenger), param.Mode))
            .ToList();

        void HandleCall<TSymbol>(Scope scope, NodeCall call) where TSymbol : CallableSymbol
        {
            scope.GetSymbol<TSymbol>(call.Name).MatchError(messenger.Report).MatchSome(callable => {
                List<string> problems = new();

                if (call.Parameters.Count != callable.Parameters.Count) {
                    problems.Add(Message.ProblemWrongNumberOfArguments(
                        call.Parameters.Count, callable.Parameters.Count));
                }

                foreach (var (actual, formal) in call.Parameters.Zip(callable.Parameters)) {
                    if (actual.Mode != formal.Mode) {
                        problems.Add(Message.ProblemWrongArgumentMode(formal.Name,
                            actual.Mode.RepresentationActual, formal.Mode.RepresentationFormal));
                    }
                    
                    actual.Value.EvaluateType(scope).MatchError(messenger.Report).MatchSome(actualType => {
                        if (!actualType.IsAssignableTo(formal.Type)) {
                            problems.Add(Message.ProblemWrongArgumentType(formal.Name,
                                formal.Type, actualType));
                        }
                    });
                }

                if (problems.Count > 0) {
                    messenger.Report(Message.ErrorCallParameterMismatch(call.SourceTokens, callable, problems));
                }
            });

            foreach (var actual in call.Parameters) {
                AnalyzeExpression(scope, actual.Value);
            }
        }

        void HandleCallableDeclaration<T>(Scope scope, T sub) where T : CallableSymbol, IEquatable<T?>
        {
            if (!scope.TryAdd(sub, out var existingSymbol)) {
                if (existingSymbol is T existingSub) {
                    if (!sub.SemanticsEqual(existingSub)) {
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
                .Where(param => param.Mode == ParameterMode.Out)
                .ToDictionary(keySelector: param => param.Name, elementSelector: _ => false);

            AnalyzeScopedBlock(node, hookExpr: expr => {
                if (expr is Expression.Lvalue.VariableReference varRef
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
                } else if (!sub.SemanticsEqual(existingSub)) {
                    messenger.Report(Message.ErrorSignatureMismatch(sub, existingSub));
                }
            } else {
                messenger.Report(Message.ErrorRedefinedSymbol(sub, existingSymbol));
            }
        }

        void AnalyzeScopedBlock(BlockNode scopedBlock, Action<Expression>? hookExpr = null)
        {
            foreach (var stmt in scopedBlock.Block) {
                AnalyzeStatement(scopes[scopedBlock], stmt, hookExpr);
            }
        }
    }
}
