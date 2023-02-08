using BasicPlusParser.Statements;
using BasicPlusParser.Statements.Expressions;
using BasicPlusParser.Tokens;
using System.Collections.Generic;
using System.Linq;

namespace BasicPlusParser.Analyser
{
    public class UnassignedVariableAnalyser {
        readonly Procedure _prog;

        public Dictionary<string, (Token, Statement)> UnassignedVars = new();

        //When we take a goto or gosub, store the label so we don't take it again (to avoid infinite recursion).
        readonly Stack<string> JumpsTaken = new();

        readonly TokenEqualityComparer tokenEqualityComparer = new();

        public UnassignedVariableAnalyser(Procedure prog) {
            _prog = prog;
        }

        (bool, HashSet<Token>, HashSet<Token>) AnalyseCore(IEnumerable<Statement> statements, IReadOnlySet<Token> env) {

            bool branchReturns = false;
            // Contains variables that are definitivley assigned in the current block being analysed.
            // HashSet<Token> definiteInLabel = new HashSet<Token>(tokenEqualityComparer);
            // Contains variables that are definitely assigned when returning from a gosub.
            HashSet<Token> definiteFromGosub = new(tokenEqualityComparer);
            HashSet<Token> definiteInLabel = new(tokenEqualityComparer);


            foreach (Statement statement in statements) {
                foreach (Token token in statement.GetReferencedVars().Where(v =>  !(env.Contains(v) || definiteInLabel.Contains(v))))
                {
                    string key = $"{token.FileName}|{token.Pos}";
                    // Don't report the same error twice.
                    if (!UnassignedVars.ContainsKey(key))
                    {
                        if (statement is EquStatemnet equStatement && equStatement.Value is MatrixIndexExpression) {
                            // An equate to a matrix index expression is ok (equ a to my_mat(1))
                            continue;
                        }  

                        // System variables cannot be "unassigned", so just ignore them.
                        // I think a better way to handle this would be to pass them in the env variable.
                        if (token is SystemVariableToken)
                        {
                            continue;
                        }

                        if (_prog.SymbolTable.IsCommonVariable(token)) {
                            continue;
                        }

                        UnassignedVars.Add(key, (token, statement));
                    }
                }


                HashSet<Token> assignedVars = statement.GetAssignedVars();
                definiteInLabel.UnionWith(assignedVars);

                if (!branchReturns) {
                    definiteFromGosub.UnionWith(assignedVars);
                }


                switch (statement) {
                    case InsertStatement s:
                        (var insertReturns, var insertVars, var insertLocalVars) = AnalyseCore(s.Statements, GetEnv(definiteInLabel,env));
                        definiteFromGosub.UnionWith(insertVars);
                        if (!insertReturns) {
                            definiteInLabel.UnionWith(insertVars);
                        }
                        if (insertReturns) return (true, definiteFromGosub, definiteInLabel);
                        break;


                    case ReturnStatement:
                        return (true, definiteFromGosub, definiteInLabel);
                    case ThenElseStatement s:

                        var newLocal = GetEnv(definiteInLabel, env);
                        (var thenReturns, var thenVars, var thenLocalVars) = AnalyseCore(s.Then, newLocal);
                        (var elseReturns, var elseVars, var elseLocalVars) = AnalyseCore(s.Else, newLocal);


                        definiteFromGosub.UnionWith(thenVars.Intersect(elseVars, tokenEqualityComparer));

                        if (!thenReturns && !elseReturns) {
                            definiteInLabel.UnionWith(thenLocalVars.Intersect(elseLocalVars, tokenEqualityComparer));
                        }

                        if (!elseReturns && thenReturns) definiteInLabel.UnionWith(elseLocalVars);
                        if (elseReturns && !thenReturns) definiteInLabel.UnionWith(thenLocalVars);
                        if (thenReturns && elseReturns) return (true, definiteFromGosub, definiteInLabel);

                        branchReturns |= thenReturns | elseReturns;
                        break;

                    case LoopRepeatStatement s:
                        (var loopRepeatReturns, var loopRepeatVars, var loopLocalVars) = AnalyseCore(s.Statements, GetEnv(definiteInLabel, env));
                        definiteFromGosub.UnionWith(loopRepeatVars);
                        if (!loopRepeatReturns) {
                            definiteInLabel.UnionWith(loopRepeatVars);
                        }
                        if (loopRepeatReturns) return (true, definiteFromGosub, definiteInLabel);
                        break;
                    case ForNextStatement s:
                        (var forNextReturns, var forNextVars, var forNextLocalVars) = AnalyseCore(s.Statements, GetEnv(definiteInLabel, env));
                        definiteFromGosub.UnionWith(forNextVars);
                        if (!forNextReturns) {
                            definiteInLabel.UnionWith(forNextVars);
                        }
                        if (forNextReturns) return (true, definiteFromGosub, definiteInLabel);
                        break;
                    case CaseStmt s:
                        var nlocal = GetEnv(definiteInLabel, env);

                        foreach (Case @case in s.Cases) {
                            AnalyseCore(@case.Statements, nlocal);
                        }
                        break;
                    case GosubStatement s:
                        if (!JumpsTaken.Contains(s.Label.Name)) {
                            if (_prog.SymbolTable.Labels.ContainsKey(s.Label.Name)) {
                                var stmtsAfterLabel = _prog.SymbolTable.Labels[s.Label.Name].StatementsFollowingLabel;
                                JumpsTaken.Push(s.Label.Name);
                                (_, var gosubVars, _) = AnalyseCore(stmtsAfterLabel, GetEnv(definiteInLabel, env));
                                JumpsTaken.Pop();
                                definiteInLabel.UnionWith(gosubVars);
                                definiteFromGosub.UnionWith(gosubVars);
                            }
                        }
                        break;
                    case GoToStatement s:
                        if (!JumpsTaken.Contains(s.Label.Name)) {
                            if (_prog.SymbolTable.Labels.ContainsKey(s.Label.Name)) {
                                JumpsTaken.Push(s.Label.Name);
                                (_, _, _) = AnalyseCore(_prog.SymbolTable.Labels[s.Label.Name].StatementsFollowingLabel, GetEnv(definiteInLabel, env));
                                JumpsTaken.Pop();
                            }
                        }
                        return (true, definiteFromGosub, definiteInLabel);

                    case OnGosubStatement s:
                        HashSet<Token> onGosubVars = new(tokenEqualityComparer);

                        bool firstTime = true;
                        foreach (var label in s.Labels) {
                            if (!JumpsTaken.Contains(label.Name)) {
                                if (_prog.SymbolTable.Labels.ContainsKey(label.Name)) {
                                    JumpsTaken.Push(label.Name);
                                    (_, var gosubVars, _) = AnalyseCore(_prog.SymbolTable.Labels[label.Name].StatementsFollowingLabel, GetEnv(definiteInLabel, env));
                                    JumpsTaken.Pop();
                                    if (firstTime) {
                                        firstTime = false;
                                        onGosubVars.UnionWith(gosubVars);
                                    } else {
                                        onGosubVars.IntersectWith(gosubVars);
                                    }
                                }
                            }
                        }
                        definiteInLabel.UnionWith(onGosubVars);
                        definiteFromGosub.UnionWith(onGosubVars);


                        break;
                    case OnGotoStatement s:
                        foreach (var label in s.Labels) {
                            if (!JumpsTaken.Contains(label.Name)) {
                                if (_prog.SymbolTable.Labels.ContainsKey(label.Name)) {
                                    JumpsTaken.Push(label.Name);
                                    AnalyseCore(_prog.SymbolTable.Labels[label.Name].StatementsFollowingLabel, GetEnv(definiteInLabel, env));
                                    JumpsTaken.Pop();
                                }
                            }
                        }
                        return (true, definiteFromGosub, definiteInLabel);

                }
            }
            return (false, definiteFromGosub, definiteInLabel);
        }


        HashSet<Token>GetEnv(HashSet<Token> local, IReadOnlySet<Token> env) {

            var a = new HashSet<Token>(env, tokenEqualityComparer);
            //a.UnionWith(env);
            a.UnionWith(local);

            return a;
        }

        public void Analyse()
        {
            HashSet<Token> env = new( _prog.SymbolTable.ProcedureParameters, tokenEqualityComparer);
            AnalyseCore(_prog.Statements, env);
        }
    }
}