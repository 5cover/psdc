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

        AddScope(null, root);
        foreach (var decl in root.Declarations) {
            AnalyzeDeclaration(scopes[root], decl);
        }

        if (!seenMainProgram) {
            AddMessage(Message.ErrorMissingMainProgram(root.SourceTokens));
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
                CreateTypeOrError(parentScope, constant.Type)
                .Combine(EvaluateTypeOrError(parentScope, constant.Value))
                .MatchSome((declaredType, inferredType) => {
                    if (!declaredType.Equals(inferredType)) {
                        AddMessage(Message.ErrorDeclaredInferredTypeMismatch(constant.SourceTokens, declaredType, inferredType));
                    }
                    if (!constant.Value.IsConstant(parentScope)) {
                        AddMessage(Message.ErrorExpectedConstantExpression(constant.SourceTokens));
                    }
                    AddSymbolOrError(parentScope, new Symbol.Constant(constant.Name, constant.SourceTokens, declaredType, constant.Value));
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
                CreateTypeOrError(parentScope, funcDef.Signature.ReturnType).MatchSome(returnType => {
                    var parameters = CreateParameters(parentScope, funcDef.Signature.Parameters);
                    HandleCallableDefinition(parentScope, new Symbol.Function(
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
                HandleCallableDeclaration(parentScope, new Symbol.Procedure(
                    proc.Signature.Name,
                    proc.Signature.SourceTokens,
                    CreateParameters(parentScope, proc.Signature.Parameters)));
                break;

            case Node.Declaration.ProcedureDefinition procDef:
                var parameters = CreateParameters(parentScope, procDef.Signature.Parameters);
                HandleCallableDefinition(parentScope, new Symbol.Procedure(
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
                AddScope(parentScope, alternative.If);
                HandleScopedBlock(alternative.If);
                foreach (var elseIf in alternative.ElseIfs) {
                    AnalyzeExpression(parentScope, elseIf.Condition);
                    AddScope(parentScope, elseIf);
                    HandleScopedBlock(elseIf);
                }
                alternative.Else.MatchSome(@else => {
                    AddScope(parentScope, @else);
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
            case Node.Statement.ProcedureCall call:
                HandleCall<Symbol.Procedure>(parentScope, call);
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
                        AddSymbolOrError(parentScope, new Symbol.Variable(name, varDecl.SourceTokens, type));
                    }
                });
                break;
            case Node.Statement.WhileLoop whileLoop:
                HandleScopedBlock(whileLoop);
                break;
            case Node.Statement.Switch switchCase:
                AnalyzeExpression(parentScope, switchCase.Expression);
                foreach (var @case in switchCase.Cases) {
                    AnalyzeExpression(parentScope, @case.When);
                    AddScope(parentScope, @case);
                    HandleScopedBlock(@case);
                }
                switchCase.Default.MatchSome(@default => {
                    AddScope(parentScope, @default);
                    HandleScopedBlock(@default);
                });
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
            case Node.Expression.FunctionCall call:
                HandleCall<Symbol.Function>(parentScope, call);
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
                _ = GetSymbolOrError<Symbol.Variable>(parentScope, varRef.SourceTokens, varRef.Name);
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
                    AddMessage(Message.ErrorRedefinedSymbol(param, existingSymbol));
                }
            }
        }

        List<Symbol.Parameter> CreateParameters(ReadOnlyScope scope, IEnumerable<Node.FormalParameter> parameters)
         => parameters.Select(param => CreateTypeOrError(scope, param.Type).Map(type
            => new Symbol.Parameter(param.Name, param.SourceTokens, type, param.Mode))).WhereSome().ToList();

        void HandleCall<TSymbol>(Scope parentScope, CallNode call) where TSymbol : CallableSymbol
        {
            GetSymbolOrError<TSymbol>(parentScope, call.SourceTokens, call.Name).MatchSome(callee => {
                if (!call.Parameters.ZipStrict(callee.Parameters, (effective, formal)
                    => effective.Mode.Equals(formal.Mode)
                    && EvaluateTypeOrError(parentScope, effective.Value).Map(type => formal.Type.Equals(type)).ValueOr(false))
                 .All(b => b)) {
                    AddMessage(Message.ErrorCallParameterMismatch(call));
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
                        AddMessage(Message.ErrorSignatureMismatch(sub, existingSub));
                    }
                } else {
                    AddMessage(Message.ErrorRedefinedSymbol(sub, existingSymbol));
                }
            }
        }

        void HandleCallableDefinition<T>(Scope parentScope, T sub) where T : CallableSymbol, IEquatable<T?>
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
            Node.Type.StructureDefinition structure => CreateStructureOrError(scope, structure),
            _ => throw type.ToUnmatchedException(),
        };

        Option<EvaluatedType.Structure> CreateStructureOrError(ReadOnlyScope scope, Node.Type.StructureDefinition structure)
        {
            Dictionary<string, EvaluatedType> components = new();
            bool atLeastOneNameWasntAdded = false;
            foreach (var comp in structure.Components) {
                CreateTypeOrError(scope, comp.Type).Match(type => {
                    foreach (var name in comp.Names) {
                        if (!components.TryAdd(name, type)) {
                            AddMessage(Message.ErrorStructureDuplicateComponent(comp.SourceTokens, name));
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
