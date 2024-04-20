using Scover.Psdc.CodeGeneration;
using Scover.Psdc.Parsing.Nodes;
using Scover.Psdc.Tokenization;

namespace Scover.Psdc.SemanticAnalysis;

internal sealed class SemanticAst(Node.Algorithm root, IReadOnlyDictionary<ScopedNode, Scope> scopes)
{
    public Node.Algorithm Root => root;

    public IReadOnlyDictionary<ScopedNode, Scope> Scopes => scopes;
}

internal sealed class SemanticAnalyzer(Node.Algorithm root) : MessageProvider
{
    public SemanticAst AnalyzeSemantics()
    {
        Dictionary<ScopedNode, Scope> scopes = new();
        bool seenMainProgram = false;

        AddScopeIfNecessary(null, root);
        foreach (var decl in root.Declarations) {
            AnalyzeDeclaration(scopes[root], decl);
        }

        if (!seenMainProgram) {
            AddMessage(Message.ErrorMissingMainProgram());
        }

        return new(root, scopes);

        void AnalyzeDeclaration(Scope parentScope, Node.Declaration decl)
        {
            AddScopeIfNecessary(parentScope, decl);

            switch (decl) {
            case Node.Declaration.Alias alias:
                CreateTypeOrError(parentScope, alias.Type).MatchSome(type
                 => AddSymbolOrError(parentScope, new Symbol.TypeAlias(alias.Name, alias.SourceTokens, type)));
                break;

            case Node.Declaration.Constant constant:
                EvaluateTypeOrError(parentScope, constant.Value).MatchSome(type
                 => AddSymbolOrError(parentScope, new Symbol.Constant(constant.Name, constant.SourceTokens, type, constant.Value)));
                break;

            case Node.Declaration.Function func:
                CreateTypeOrError(parentScope, func.Signature.ReturnType).MatchSome(returnType
                 => HandleSubroutineDeclaration(parentScope, new Symbol.Function(
                       func.Signature.Name,
                       func.Signature.SourceTokens,
                       CreateParameters(parentScope, func.Signature.Parameters),
                       returnType)));
                break;

            case Node.Declaration.FunctionDefinition funcDef:
                CreateTypeOrError(parentScope, funcDef.Signature.ReturnType).MatchSome(returnType => {
                    var parameters = CreateParameters(parentScope, funcDef.Signature.Parameters);
                    HandleSubroutineDefinition(parentScope, new Symbol.Function(
                                            funcDef.Signature.Name,
                                            funcDef.Signature.SourceTokens,
                                            parameters,
                                            returnType));
                    AddParameters(scopes[funcDef], parameters);
                    HandleScopedBlock(funcDef);
                });
                break;

            case Node.Declaration.MainProgram mainProgram:
                if (seenMainProgram) {
                    AddMessage(Message.ErrorRedefinedMainProgram(mainProgram));
                }
                seenMainProgram = true;
                HandleScopedBlock(mainProgram);
                break;

            case Node.Declaration.Procedure proc:
                HandleSubroutineDeclaration(parentScope, new Symbol.Procedure(
                    proc.Signature.Name,
                    proc.Signature.SourceTokens,
                    CreateParameters(parentScope, proc.Signature.Parameters)));
                break;

            case Node.Declaration.ProcedureDefinition procDef:
                var parameters = CreateParameters(parentScope, procDef.Signature.Parameters);
                HandleSubroutineDefinition(parentScope, new Symbol.Procedure(
                    procDef.Signature.Name,
                    procDef.Signature.SourceTokens,
                    parameters));
                AddParameters(scopes[procDef], parameters);
                HandleScopedBlock(procDef);
                break;

            default:
                throw decl.ToUnmatchedException();
            }
        }

        void AnalyzeStatement(Scope parentScope, Node.Statement stmt)
        {
            AddScopeIfNecessary(parentScope, stmt);

            switch (stmt) {
            case Node.Statement.Alternative alternative:
                AnalyzeExpression(parentScope, alternative.If.Condition);
                AddScopeIfNecessary(parentScope, alternative.If);
                HandleScopedBlock(alternative.If);
                foreach (var elseIf in alternative.ElseIfs) {
                    AddScopeIfNecessary(parentScope, elseIf);
                    AnalyzeExpression(parentScope, elseIf.Condition);
                    HandleScopedBlock(elseIf);
                }
                alternative.Else.MatchSome(@else => {
                    AddScopeIfNecessary(parentScope, @else);
                    HandleScopedBlock(@else);
                });
                break;
            case Node.Statement.Assignment assignment:
                if (TryGetSymbolOrError<Symbol.Variable>(parentScope, assignment.SourceTokens, assignment.Target, out var targetVariable)) {
                    if (targetVariable is Symbol.Parameter parameter && parameter.Mode is ParameterMode.In) {
                        AddMessage(Message.WarningInputParameterAssignment(assignment));
                    } else if (targetVariable is Symbol.Constant constant) {
                        AddMessage(Message.ErrorConstantAssignment(assignment, constant));
                    }
                }
                AnalyzeExpression(parentScope, assignment.Value);
                break;
            case Node.Statement.DoWhileLoop doWhileLoop:
                HandleScopedBlock(doWhileLoop);
                AnalyzeExpression(parentScope, doWhileLoop.Condition);
                break;
            case Node.Statement.Ecrire ecrire:
                AnalyzeExpression(parentScope, ecrire.Argument1);
                AnalyzeExpression(parentScope, ecrire.Argument2);
                break;
            case Node.Statement.Fermer fermer:
                AnalyzeExpression(parentScope, fermer.Argument);
                break;
            case Node.Statement.ForLoop forLoop:
                AnalyzeExpression(parentScope, forLoop.Start);
                forLoop.Step.MatchSome(step => AnalyzeExpression(parentScope, step));
                AnalyzeExpression(parentScope, forLoop.End);
                HandleScopedBlock(forLoop);
                break;
            case Node.Statement.Lire lire:
                AnalyzeExpression(parentScope, lire.Argument1);
                AnalyzeExpression(parentScope, lire.Argument2);
                break;
            case Node.Statement.OuvrirAjout ouvrirAjout:
                AnalyzeExpression(parentScope, ouvrirAjout.Argument);
                break;
            case Node.Statement.OuvrirEcriture ouvrirEcriture:
                AnalyzeExpression(parentScope, ouvrirEcriture.Argument);
                break;
            case Node.Statement.OuvrirLecture ouvrirLecture:
                AnalyzeExpression(parentScope, ouvrirLecture.Argument);
                break;
            case Node.Statement.EcrireEcran ecrireEcran:
                foreach (var arg in ecrireEcran.Arguments) {
                    AnalyzeExpression(parentScope, arg);
                }
                break;
            case Node.Statement.LireClavier lireClavier:
                AnalyzeExpression(parentScope, lireClavier.Argument);
                break;
            case Node.Statement.RepeatLoop repeatLoop:
                HandleScopedBlock(repeatLoop);
                break;
            case Node.Statement.Return ret:
                AnalyzeExpression(parentScope, ret.Value);
                break;
            case Node.Statement.VariableDeclaration varDecl:
                CreateTypeOrError(parentScope, varDecl.Type).MatchSome(type => {
                    foreach (var name in varDecl.Names) {
                        //Console.WriteLine($"adding variable {name} to scope {parentScope.GetHashCode()}");
                        AddSymbolOrError(parentScope, new Symbol.Variable(name, varDecl.SourceTokens, type));
                    }
                });
                break;
            case Node.Statement.WhileLoop whileLoop:
                HandleScopedBlock(whileLoop);
                break;

            default:
                throw stmt.ToUnmatchedException();
            }
        }

        void AnalyzeExpression(Scope parentScope, Node.Expression expr)
        {
            AddScopeIfNecessary(parentScope, expr);

            switch (expr) {
            case Node.Expression.ArraySubscript arraySub:
                AnalyzeExpression(parentScope, arraySub.Array);
                foreach (var i in arraySub.Indexes) {
                    AnalyzeExpression(parentScope, i);
                }
                break;
            case Node.Expression.Bracketed bracketed:
                AnalyzeExpression(parentScope, bracketed.Expression);
                break;
            case Node.Expression.BuiltinFdf fdf:
                AnalyzeExpression(parentScope, fdf.Argument);
                break;
            case Node.Expression.Call call:
                GetSymbolOrError<Symbol.Function>(parentScope, call.SourceTokens, call.Name).MatchSome(func => {
                    if (!call.Parameters.ZipStrict(func.Parameters, (effective, format)
                        => effective.Mode.Equals(format.Mode)
                        && EvaluateTypeOrError(parentScope, effective.Value).Map(type => format.Type.Equals(type)).ValueOr(false))
                     .All(b => b)) {
                        AddMessage(Message.ErrorCallParameterMismatch(call, func));
                    }
                });
                break;
            case Node.Expression.ComponentAccess componentAccess:
                break;
            case Node.Expression.Literal literal:
                break;
            case Node.Expression.OperationBinary opBin:
                AnalyzeExpression(parentScope, opBin.Operand1);
                if (opBin.Operator is Parsing.BinaryOperator.Divide
                 && opBin.Operand2.EvaluateValue(parentScope).FlatMap(Parse.ToInt32).Map(val => val == 0).ValueOr(false)) {
                    AddMessage(Message.WarningDivisionByZero(opBin.SourceTokens));
                }
                AnalyzeExpression(parentScope, opBin.Operand2);
                break;
            case Node.Expression.OperationUnary opUn:
                AnalyzeExpression(parentScope, opUn.Operand);
                break;
            case Node.Expression.VariableReference varRef:
                //Console.WriteLine($"fetching variable {varRef.Name} from scope {parentScope.GetHashCode()}");
                _ = GetSymbolOrError<Symbol.Variable>(parentScope, varRef.SourceTokens, varRef.Name);
                break;
            default:
                throw expr.ToUnmatchedException();
            }
        }

        void AddScopeIfNecessary(Scope? parentScope, Node node)
        {
            if (node is ScopedNode sn) {
                scopes[sn] = new(parentScope);
                //Console.WriteLine($"Created scope of parent {parentScope?.GetHashCode().ToString() ?? "(null)"} for {node.GetType().Name}: {scopes[sn].GetHashCode()}");
            }
        }

        void AddParameters(Scope scope, IEnumerable<Symbol.Parameter> parameters)
        {
            foreach (var param in parameters) {
                if (!scope.TryAdd(param, out var existingSymbol)) {
                    AddMessage(Message.ErrorRedefinedSymbol(param, existingSymbol));
                }
            }
        }

        List<Symbol.Parameter> CreateParameters(ReadOnlyScope scope, IEnumerable<Node.FormalParameter> parameters)
         => parameters.Select(param => CreateTypeOrError(scope, param.Type).Map(type
            => new Symbol.Parameter(param.Name, param.SourceTokens, type, param.Mode))).WhereSome().ToList();

        void HandleSubroutineDeclaration<T>(Scope parentScope, T sub) where T : DeclarableDefinableSymbol, IEquatable<T?>
        {
            if (!parentScope.TryAdd(sub, out var existingSymbol)) {
                if (existingSymbol is T existingSub) {
                    if (!sub.Equals(existingSub)) {
                        AddMessage(Message.ErrorSignatureMismatch(sub, existingSub));
                    }
                } else {
                    AddMessage(Message.ErrorRedefinedSymbol(sub, existingSymbol));
                }
            }
        }

        void HandleSubroutineDefinition<T>(Scope parentScope, T sub) where T : DeclarableDefinableSymbol, IEquatable<T?>
        {
            if (parentScope.TryAdd(sub, out var existingSymbol)) {
                sub.MarkAsDefined();
            } else if (existingSymbol is T existingSub) {
                if (existingSub.HasBeenDefined) {
                    AddMessage(Message.ErrorRedefinedSymbol(sub, existingSub));
                } else if (!sub.Equals(existingSub)) {
                    AddMessage(Message.ErrorSignatureMismatch(sub, existingSub));
                }
            } else {
                AddMessage(Message.ErrorRedefinedSymbol(sub, existingSymbol));
            }
        }

        void HandleScopedBlock(BlockNode scopedBlock)
        {
            foreach (var stmt in scopedBlock.Block) {
                AnalyzeStatement(scopes[scopedBlock], stmt);
            }
        }

        Option<EvaluatedType> CreateTypeOrError(ReadOnlyScope scope, Node.Type type) => type switch {
            Node.Type.String => EvaluatedType.String.Instance.Some(),
            Node.Type.Primitive p => new EvaluatedType.Primitive(p.Type).Some(),
            Node.Type.AliasReference alias => GetSymbolOrError<Symbol.TypeAlias>(scope, alias.SourceTokens, alias.Name)
                    .Map(aliasType => new EvaluatedType.AliasReference(alias.Name, aliasType.TargetType)),
            Node.Type.Array array => CreateTypeOrError(scope, array.Type).Map(elementType
            => new EvaluatedType.Array(elementType, array.Dimensions)),
            Node.Type.StringLengthed str => new EvaluatedType.StringLengthed(str.Length).Some(),
            _ => throw type.ToUnmatchedException(),
        };

        Option<EvaluatedType> EvaluateTypeOrError(ReadOnlyScope scope, Node.Expression expr)
        {
            var type = expr.EvaluateType(scope);
            if (!type.HasValue) {
                AddMessage(type.Error(expr.SourceTokens));
            }
            return type.DiscardError();
        }

        Option<T> GetSymbolOrError<T>(ReadOnlyScope scope, Partition<Token> sourceTokens, string name) where T : Symbol
        {
            var ret = scope.GetSymbol<T>(name);
            if (!ret.HasValue) {
                AddMessage(Message.ErrorUndefinedSymbol<T>(sourceTokens, name));
            }
            return ret;
        }

        bool TryGetSymbolOrError<T>(ReadOnlyScope scope, Partition<Token> sourceTokens, string name, out T? symbol) where T : Symbol
        {
            bool found = scope.TryGetSymbol<T>(name, out symbol);
            if (!found) {
                AddMessage(Message.ErrorUndefinedSymbol<T>(sourceTokens, name));
            }
            return found;
        }

        void AddSymbolOrError(Scope scope, Symbol symbol)
        {
            if (!scope.TryAdd(symbol, out var existingSymbol)) {
                AddMessage(Message.ErrorRedefinedSymbol(symbol, existingSymbol));
            }
        }
    }
}
