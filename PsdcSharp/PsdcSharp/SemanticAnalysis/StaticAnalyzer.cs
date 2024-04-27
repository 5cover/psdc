using Scover.Psdc.CodeGeneration;
using Scover.Psdc.Parsing.Nodes;

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

        AddScope(null, root);
        foreach (var decl in root.Declarations) {
            AnalyzeDeclaration(scopes[root], decl);
        }

        if (!seenMainProgram) {
            messenger.Report(Message.ErrorMissingMainProgram(root.SourceTokens));
        }

        return new(root, scopes);

        void AnalyzeDeclaration(Scope parentScope, Node.Declaration decl)
        {
            AddScopeIfNecessary(parentScope, decl);

            switch (decl) {
            case Node.Declaration.TypeAlias alias:
                CreateTypeOrError(parentScope, alias.Type).MatchSome(type
                 => parentScope.AddSymbolOrError(messenger, new Symbol.TypeAlias(alias.Name, alias.SourceTokens, type)));
                break;

            case Node.Declaration.Constant constant:
                CreateTypeOrError(parentScope, constant.Type)
                .Combine(EvaluateTypeOrError(parentScope, constant.Value))
                .MatchSome((declaredType, inferredType) => {
                    if (!declaredType.Equals(inferredType)) {
                        messenger.Report(Message.ErrorDeclaredInferredTypeMismatch(constant.SourceTokens, declaredType, inferredType));
                    }
                    if (!constant.Value.IsConstant(parentScope)) {
                        messenger.Report(Message.ErrorExpectedConstantExpression(constant.SourceTokens));
                    }
                    parentScope.AddSymbolOrError(messenger, new Symbol.Constant(constant.Name, constant.SourceTokens, declaredType, constant.Value));
                });
                break;

            case Node.Declaration.Function func:
                CreateTypeOrError(parentScope, func.Signature.ReturnType).MatchSome(returnType
                 => HandleCallableDeclaration(parentScope, new Symbol.Function(
                       func.Signature.Name,
                       func.Signature.SourceTokens,
                       CreateParameters(parentScope, func.Signature.Parameters),
                       returnType)));
                break;

            case Node.Declaration.FunctionDefinition funcDef:
                CreateTypeOrError(parentScope, funcDef.Signature.ReturnType).MatchSome(returnType
                 => AnalyzeCallableDefinition(parentScope, funcDef, new Symbol.Function(
                        funcDef.Signature.Name,
                        funcDef.Signature.SourceTokens,
                        CreateParameters(parentScope, funcDef.Signature.Parameters),
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
                HandleCallableDeclaration(parentScope, new Symbol.Procedure(
                    proc.Signature.Name,
                    proc.Signature.SourceTokens,
                    CreateParameters(parentScope, proc.Signature.Parameters)));
                break;

            case Node.Declaration.ProcedureDefinition procDef:
                AnalyzeCallableDefinition(parentScope, procDef, new Symbol.Procedure(
                    procDef.Signature.Name,
                    procDef.Signature.SourceTokens,
                    CreateParameters(parentScope, procDef.Signature.Parameters)));
                break;

            default:
                throw decl.ToUnmatchedException();
            }
        }

        void AnalyzeStatement(Scope parentScope, Node.Statement stmt, Action<Node.Expression>? hookExpr = null)
        {
            AddScopeIfNecessary(parentScope, stmt);

            switch (stmt) {
            case Node.Statement.Nop nop:
                break;
            case Node.Statement.Alternative alternative:
                AnalyzeExpression(parentScope, alternative.If.Condition, hookExpr);
                AddScope(parentScope, alternative.If);
                AnalyzeScopedBlock(alternative.If);
                foreach (var elseIf in alternative.ElseIfs) {
                    AnalyzeExpression(parentScope, elseIf.Condition, hookExpr);
                    AddScope(parentScope, elseIf);
                    AnalyzeScopedBlock(elseIf);
                }
                alternative.Else.MatchSome(@else => {
                    AddScope(parentScope, @else);
                    AnalyzeScopedBlock(@else);
                });
                break;
            case Node.Statement.Assignment assignment:
                AnalyzeExpression(parentScope, assignment.Target, hookExpr);
                if (assignment.Target is Node.Expression.LValue.VariableReference varRef
                    && parentScope.TryGetSymbolOrError<Symbol.Variable>(messenger, varRef.Name, out var targetVar)) {
                    if (targetVar is Symbol.Constant constant) {
                        messenger.Report(Message.ErrorConstantAssignment(assignment, constant));
                    }
                }
                AnalyzeExpression(parentScope, assignment.Value, hookExpr);
                break;
            case Node.Statement.DoWhileLoop doWhileLoop:
                AnalyzeScopedBlock(doWhileLoop);
                AnalyzeExpression(parentScope, doWhileLoop.Condition, hookExpr);
                break;
            case Node.Statement.BuiltinEcrire ecrire:
                AnalyzeExpression(parentScope, ecrire.ArgumentNomLog, hookExpr);
                AnalyzeExpression(parentScope, ecrire.ArgumentExpression, hookExpr);
                break;
            case Node.Statement.BuiltinFermer fermer:
                AnalyzeExpression(parentScope, fermer.ArgumentNomLog, hookExpr);
                break;
            case Node.Statement.ForLoop forLoop:
                AnalyzeExpression(parentScope, forLoop.Start, hookExpr);
                forLoop.Step.MatchSome(step => AnalyzeExpression(parentScope, step, hookExpr));
                AnalyzeExpression(parentScope, forLoop.End, hookExpr);
                AnalyzeScopedBlock(forLoop);
                break;
            case Node.Statement.BuiltinLire lire:
                AnalyzeExpression(parentScope, lire.ArgumentNomLog, hookExpr);
                AnalyzeExpression(parentScope, lire.ArgumentVariable, hookExpr);
                break;
            case Node.Statement.BuiltinOuvrirAjout ouvrirAjout:
                AnalyzeExpression(parentScope, ouvrirAjout.ArgumentNomLog, hookExpr);
                break;
            case Node.Statement.BuiltinOuvrirEcriture ouvrirEcriture:
                AnalyzeExpression(parentScope, ouvrirEcriture.ArgumentNomLog, hookExpr);
                break;
            case Node.Statement.BuiltinOuvrirLecture ouvrirLecture:
                AnalyzeExpression(parentScope, ouvrirLecture.ArgumentNomLog, hookExpr);
                break;
            case Node.Statement.ProcedureCall call:
                HandleCall<Symbol.Procedure>(parentScope, call);
                break;
            case Node.Statement.BuiltinEcrireEcran ecrireEcran:
                foreach (var arg in ecrireEcran.Arguments) {
                    AnalyzeExpression(parentScope, arg, hookExpr);
                }
                break;
            case Node.Statement.BuiltinLireClavier lireClavier:
                AnalyzeExpression(parentScope, lireClavier.ArgumentVariable, hookExpr);
                break;
            case Node.Statement.RepeatLoop repeatLoop:
                AnalyzeScopedBlock(repeatLoop);
                break;
            case Node.Statement.Return ret:
                AnalyzeExpression(parentScope, ret.Value, hookExpr);
                break;
            case Node.Statement.LocalVariable varDecl:
                CreateTypeOrError(parentScope, varDecl.Type).MatchSome(type => {
                    foreach (var name in varDecl.Names) {
                        parentScope.AddSymbolOrError(messenger, new Symbol.Variable(name, varDecl.SourceTokens, type));
                    }
                });
                break;
            case Node.Statement.WhileLoop whileLoop:
                AnalyzeScopedBlock(whileLoop);
                break;
            case Node.Statement.Switch switchCase:
                AnalyzeExpression(parentScope, switchCase.Expression, hookExpr);
                foreach (var @case in switchCase.Cases) {
                    AnalyzeExpression(parentScope, @case.When, hookExpr);
                    AddScope(parentScope, @case);
                    AnalyzeScopedBlock(@case);
                }
                switchCase.Default.MatchSome(@default => {
                    AddScope(parentScope, @default);
                    AnalyzeScopedBlock(@default);
                });
                break;

            default:
                throw stmt.ToUnmatchedException();
            }
        }

        void AnalyzeExpression(Scope parentScope, Node.Expression expr, Action<Node.Expression>? hookExpr = null)
        {
            AddScopeIfNecessary(parentScope, expr);
            hookExpr?.Invoke(expr);

            switch (expr) {
            case Node.Expression.Bracketed bracketed:
                AnalyzeExpression(parentScope, bracketed.Expression, hookExpr);
                break;
            case Node.Expression.LValue.Bracketed bracketed:
                AnalyzeExpression(parentScope, bracketed.LValue, hookExpr);
                break;
            case Node.Expression.BuiltinFdf fdf:
                AnalyzeExpression(parentScope, fdf.ArgumentNomLog, hookExpr);
                break;
            case Node.Expression.FunctionCall call:
                HandleCall<Symbol.Function>(parentScope, call);
                break;
            case Node.Expression.Literal literal:
                break;
            case Node.Expression.OperationBinary opBin:
                AnalyzeExpression(parentScope, opBin.Operand1, hookExpr);
                if (opBin.Operator is Parsing.BinaryOperator.Divide
                 && opBin.Operand2.EvaluateValue(parentScope).FlatMap(Parse.ToInt32).Map(val => val == 0).ValueOr(false)) {
                    messenger.Report(Message.WarningDivisionByZero(opBin.SourceTokens));
                }
                AnalyzeExpression(parentScope, opBin.Operand2, hookExpr);
                break;
            case Node.Expression.OperationUnary opUn:
                AnalyzeExpression(parentScope, opUn.Operand, hookExpr);
                break;
            case Node.Expression.LValue.ArraySubscript arraySub:
                AnalyzeExpression(parentScope, arraySub.Array, hookExpr);
                foreach (var i in arraySub.Indexes) {
                    AnalyzeExpression(parentScope, i, hookExpr);
                }
                break;
            case Node.Expression.LValue.ComponentAccess componentAccess:
                AnalyzeExpression(parentScope, componentAccess.Structure, hookExpr);
                break;
            case Node.Expression.LValue.VariableReference varRef:
                _ = parentScope.GetSymbolOrError<Symbol.Variable>(varRef.Name).DiscardError(messenger.Report);
                break;
            default:
                throw expr.ToUnmatchedException();
            }
        }

        void AddScopeIfNecessary(Scope? parentScope, Node node)
        {
            if (node is ScopedNode sn) {
                AddScope(parentScope, sn);
            }
        }

        void AddScope(Scope? parentScope, ScopedNode scopedNode)
        {
            scopes.Add(scopedNode, new(parentScope));
        }

        void AddParameters(Scope scope, IEnumerable<Symbol.Parameter> parameters)
        {
            foreach (var param in parameters) {
                if (!scope.TryAdd(param, out var existingSymbol)) {
                    messenger.Report(Message.ErrorRedefinedSymbol(param, existingSymbol));
                }
            }
        }

        List<Symbol.Parameter> CreateParameters(ReadOnlyScope scope, IEnumerable<Node.FormalParameter> parameters)
         => parameters.Select(param => CreateTypeOrError(scope, param.Type).Map(type
            => new Symbol.Parameter(param.Name, param.SourceTokens, type, param.Mode))).WhereSome().ToList();

        void HandleCall<TSymbol>(Scope parentScope, CallNode call) where TSymbol : CallableSymbol
        {
            parentScope.GetSymbolOrError<TSymbol>(call.Name).DiscardError(messenger.Report).MatchSome(callee => {
                if (!call.Parameters.ZipStrict(callee.Parameters, (effective, formal)
                    => effective.Mode.Equals(formal.Mode)
                    && EvaluateTypeOrError(parentScope, effective.Value).Map(type => formal.Type.Equals(type)).ValueOr(false))
                 .All(b => b)) {
                    messenger.Report(Message.ErrorCallParameterMismatch(call));
                }
            });
            foreach (var effective in call.Parameters) {
                AnalyzeExpression(parentScope, effective.Value);
            }
        }

        void HandleCallableDeclaration<T>(Scope parentScope, T sub) where T : CallableSymbol, IEquatable<T?>
        {
            if (!parentScope.TryAdd(sub, out var existingSymbol)) {
                if (existingSymbol is T existingSub) {
                    if (!sub.Equals(existingSub)) {
                        messenger.Report(Message.ErrorSignatureMismatch(sub, existingSub));
                    }
                } else {
                    messenger.Report(Message.ErrorRedefinedSymbol(sub, existingSymbol));
                }
            }
        }

        void AnalyzeCallableDefinition<T>(Scope parentScope, BlockNode node, T sub) where T : CallableSymbol, IEquatable<T?>
        {
            HandleCallableDefinition(parentScope, sub);
            AddParameters(scopes[node], sub.Parameters);

            Dictionary<Node.Identifier, bool> outputParametersAssigned = sub.Parameters
                .Where(param => param.Mode is ParameterMode.Out)
                .ToDictionary(keySelector: param => param.Name, elementSelector: _ => false);

            AnalyzeScopedBlock(node, hookExpr: expr => {
                if (expr is Node.Expression.LValue.VariableReference varRef
                 && outputParametersAssigned.ContainsKey(varRef.Name)) {
                    outputParametersAssigned[varRef.Name] = true;
                }
            });

            foreach (var unassignedOutParam in outputParametersAssigned.Where(kv => !kv.Value).Select(kv => kv.Key)) {
                messenger.Report(Message.ErrorOutputParameterNeverAssigned(unassignedOutParam));
            }
        }

        void HandleCallableDefinition<T>(Scope parentScope, T sub) where T : CallableSymbol, IEquatable<T?>
        {
            if (parentScope.TryAdd(sub, out var existingSymbol)) {
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

        Option<EvaluatedType> CreateTypeOrError(ReadOnlyScope scope, Node.Type type) => type switch {
            Node.Type.String => EvaluatedType.String.Instance.Some(),
            Node.Type.AliasReference alias
             => scope.GetSymbolOrError<Symbol.TypeAlias>(alias.Name).DiscardError(messenger.Report)
                    .Map(aliasType => new EvaluatedType.AliasReference(alias.Name, aliasType.TargetType)),
            Node.Type.Complete.AliasReference alias
             => scope.GetSymbolOrError<Symbol.TypeAlias>(alias.Name).DiscardError(messenger.Report)
                    .Map(aliasType => new EvaluatedType.AliasReference(alias.Name, aliasType.TargetType)),
            Node.Type.Complete.Array array => CreateTypeOrError(scope, array.Type).Map(elementType
            => new EvaluatedType.Array(elementType, array.Dimensions)),
            Node.Type.Complete.LengthedString str => new EvaluatedType.LengthedString(str.Length).Some(),
            Node.Type.Complete.Structure structure => CreateStructureOrError(scope, structure),
            Node.Type.Complete.Primitive p => new EvaluatedType.Primitive(p.Type).Some(),
            _ => throw type.ToUnmatchedException(),
        };

        Option<EvaluatedType.Structure> CreateStructureOrError(ReadOnlyScope scope, Node.Type.Complete.Structure structure)
        {
            Dictionary<Node.Identifier, EvaluatedType> components = [];
            bool atLeastOneNameWasntAdded = false;
            foreach (var comp in structure.Components) {
                CreateTypeOrError(scope, comp.Type).Match(type => {
                    foreach (var name in comp.Names) {
                        if (!components.TryAdd(name, type)) {
                            messenger.Report(Message.ErrorStructureDuplicateComponent(comp.SourceTokens, name));
                        }
                    }
                }, none: () => atLeastOneNameWasntAdded = true);
            }

            // Don't create the structure if we weren't able to create all components names. This will prevent further errors when using the structure.
            return atLeastOneNameWasntAdded
             ? Option.None<EvaluatedType.Structure>()
             : new EvaluatedType.Structure(components).Some();
        }

        Option<EvaluatedType> EvaluateTypeOrError(ReadOnlyScope scope, Node.Expression expr)
         => expr.EvaluateType(scope).DiscardError(messenger.Report);
    }
}
