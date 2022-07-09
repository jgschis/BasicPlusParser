using BasicPlusParser.Statements;
using BasicPlusParser.Tokens;
using System.Collections.Generic;
using System.Linq;

namespace BasicPlusParser.Analyser
{
    public class UnassignedVariableAnalyser
    {
        readonly Procedure _prog;

        public List<(Token, Statement)> UnassignedVars = new();

        //When we take a goto or gosub, store the label so we don't take it again (to avoid infinite recursion).
        readonly Stack<string> JumpsTaken = new();
      
        public UnassignedVariableAnalyser(Procedure prog)
        {
            _prog = prog;
        }

        public (bool, HashSet<Token>, HashSet<Token>) AnalyseCore(IEnumerable<Statement> statements, HashSet<Token> env = null)
        {
            /*
             * TODO explain why this works.
             *
             */

            bool branchReturns = false;
            // Contains variables that are definitivley assigned in the current block being analysed.
            HashSet<Token> definiteInLabel = new(new TokenEqualityComparer());
            // Contains variables that are definitely assigned when returning from a gosub.
            HashSet<Token> definiteFromGosub = new(new TokenEqualityComparer());
         

            if (env != null)
            {
                definiteInLabel.UnionWith(env);
            }

            foreach (Statement statement in statements)
            {
                foreach (var err in statement.GetReferencedVars().Where(v => !definiteInLabel.Contains(v)))
                {
                    // Don't report the same error twice.
                    if (!UnassignedVars.Any(x => object.ReferenceEquals(x.Item1, err)))
                    {
                        // System variables cannot be "unassigned", so just ignore them.
                        // I think a better way to handle this would be to pass them in the env variable.
                        if (err is not SystemVariableToken)
                        {
                            UnassignedVars.Add((err, statement));
                        }
                    }
                }

                definiteInLabel.UnionWith(statement.GetAssignedVars());

                if (!branchReturns)
                {
                    definiteFromGosub.UnionWith(statement.GetAssignedVars());
                }

                switch (statement)
                {
                    case ReturnStatement:
                        return (true, definiteFromGosub, definiteInLabel);
                    case ThenElseStatement s:
                        (var thenReturns, var thenVars,var thenLocalVars) = AnalyseCore(s.Then, definiteInLabel);
                        (var elseReturns, var elseVars, var elseLocalVars) = AnalyseCore(s.Else, definiteInLabel);
                        definiteFromGosub.UnionWith(thenVars.Intersect(elseVars,new TokenEqualityComparer()));
                        if (!thenReturns && !elseReturns) definiteInLabel.UnionWith(thenLocalVars.Intersect(elseLocalVars, new TokenEqualityComparer()));
                        if (!elseReturns && thenReturns) definiteInLabel.UnionWith(elseLocalVars);
                        if (elseReturns && !thenReturns) definiteInLabel.UnionWith(thenLocalVars);
                        if (thenReturns && elseReturns) return (true, definiteFromGosub, definiteInLabel);
                        branchReturns |= thenReturns | elseReturns;
                        break;
                    case LoopRepeatStatement s:
                       (var loopRepeatReturns, var loopRepeatVars, var loopLocalVars )= AnalyseCore(s.Statements, definiteInLabel);
                        definiteFromGosub.UnionWith(loopRepeatVars);
                        if (!loopRepeatReturns)
                        {
                            definiteInLabel.UnionWith(loopRepeatVars);
                        }
                        if (loopRepeatReturns) return (true, definiteFromGosub, definiteInLabel);
                        break;
                    case ForNextStatement s:
                        (var forNextReturns, var forNextVars, var forNextLocalVars) = AnalyseCore(s.Statements, definiteInLabel);
                        definiteFromGosub.UnionWith(forNextVars);
                        if (!forNextReturns)
                        {
                            definiteInLabel.UnionWith(forNextVars);
                        }
                        if (forNextReturns) return (true, definiteFromGosub, definiteInLabel);
                        break;
                    case CaseStmt s:
                        foreach (Case @case in s.Cases)
                        {
                            AnalyseCore(@case.Statements, definiteInLabel);
                        }
                        break;
                    case GosubStatement s:
                        if (!JumpsTaken.Contains(s.Label.Name))
                        {
                            if (_prog.SymbolTable.Labels.ContainsKey(s.Label.Name)){
                                var stmtsAfterLabel = _prog.SymbolTable.Labels[s.Label.Name].StatementsFollowingLabel;
                                JumpsTaken.Push(s.Label.Name);
                                (var gosubReturns, var gosubVars, var gosubLocalVars) = AnalyseCore(stmtsAfterLabel, definiteInLabel);
                                JumpsTaken.Pop();
                                definiteInLabel.UnionWith(gosubVars);
                                definiteFromGosub.UnionWith(gosubVars);  
                            }
                        }
                        break;
                    case GoToStatement s:
                        if (!JumpsTaken.Contains(s.Label.Name))
                        {
                            if (_prog.SymbolTable.Labels.ContainsKey(s.Label.Name))
                            {
                                JumpsTaken.Push(s.Label.Name);
                                (var gotoReturns, var gotoVars, _) = AnalyseCore(_prog.SymbolTable.Labels[s.Label.Name].StatementsFollowingLabel, definiteInLabel);
                                JumpsTaken.Pop();
                            }
                        }
                        return (true, definiteFromGosub, definiteInLabel);

                    case OnGosubStatement s:
                        HashSet<Token> onGosubVars = new(new TokenEqualityComparer());
                        bool firstTime = true;
                        foreach (var label in s.Labels)
                        {
                            if (!JumpsTaken.Contains(label.Name))
                            {
                                if (_prog.SymbolTable.Labels.ContainsKey(label.Name))
                                {
                                    JumpsTaken.Push(label.Name);
                                    (var gosubReturns, var gosubVars, _) = AnalyseCore(_prog.SymbolTable.Labels[label.Name].StatementsFollowingLabel, definiteInLabel);
                                    JumpsTaken.Pop();
                                    if (firstTime)
                                    {
                                        firstTime = false;
                                        onGosubVars.UnionWith(gosubVars);
                                    }
                                    else
                                    {
                                        onGosubVars.IntersectWith(gosubVars);
                                    }
                                }
                            }
                        }
                        definiteInLabel.UnionWith(onGosubVars);
                        definiteFromGosub.UnionWith(onGosubVars);
                        break;
                    case OnGotoStatement s:
                        foreach (var label in s.Labels)
                        {
                            if (!JumpsTaken.Contains(label.Name))
                            {
                                if (_prog.SymbolTable.Labels.ContainsKey(label.Name))
                                {
                                    JumpsTaken.Push(label.Name);
                                    AnalyseCore(_prog.SymbolTable.Labels[label.Name].StatementsFollowingLabel, definiteInLabel);
                                    JumpsTaken.Pop();
                                }
                            }
                        }
                        return (true, definiteFromGosub, definiteInLabel);

                }
            }
            return (false, definiteFromGosub, definiteInLabel);
        }

        public void Analyse()
        {
            HashSet<Token> env = new( _prog.SymbolTable.ProcedureParameters, new TokenEqualityComparer());
            AnalyseCore(_prog.Statements, env);
        }
    }
}
