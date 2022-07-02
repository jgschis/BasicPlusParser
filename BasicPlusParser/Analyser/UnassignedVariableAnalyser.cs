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

        public (bool, HashSet<Token>) AnalyseCore(IEnumerable<Statement> statements, HashSet<Token> env = null)
        {
            bool branchReturns = false;
            // Contains variables that are definitivley assigned in the current block being analysed.
            HashSet<Token> definiteLocalScope = new(new TokenEqualityComparer());
            // Contains variables that are definitely assigned in the current block being analysed that came from another block.
            HashSet<Token> definiteOuterScope = new(new TokenEqualityComparer());
            // A block is the smallest unit of code that is not guaranteed to be executed. 
            // For example, each branch of an if statement is a block.

            if (env != null)
            {
                definiteLocalScope.UnionWith(env);
            }

            foreach (Statement statement in statements)
            {
                foreach (var err in statement.GetReferencedVars().Where(v => !definiteLocalScope.Contains(v)))
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

                definiteLocalScope.UnionWith(statement.GetAssignedVars());
                if (!branchReturns)
                {
                    definiteOuterScope.UnionWith(statement.GetAssignedVars());
                }

                switch (statement)
                {
                    case ReturnStatement:
                        return (true, definiteOuterScope);
                    case ThenElseStatement s:
                        (var thenReturns, var thenVars) = AnalyseCore(s.Then, definiteLocalScope);
                        (var elseReturns, var elseVars) = AnalyseCore(s.Else, definiteLocalScope);
                        definiteOuterScope.UnionWith(thenVars.Intersect(elseVars,new TokenEqualityComparer()));
                        if (!thenReturns && !elseReturns) definiteLocalScope.UnionWith(thenVars.Intersect(elseVars, new TokenEqualityComparer()));
                        if (!elseReturns && thenReturns) definiteLocalScope.UnionWith(elseVars);
                        if (elseReturns && !thenReturns) definiteLocalScope.UnionWith(thenVars);
                        if (thenReturns && elseReturns) return (true, definiteOuterScope);
                        branchReturns |= thenReturns | elseReturns;
                        break;
                    case LoopRepeatStatement s:
                       (var loopRepeatReturns, var loopRepeatVars )= AnalyseCore(s.Statements, definiteLocalScope);
                        definiteOuterScope.UnionWith(loopRepeatVars);
                        if (!loopRepeatReturns)
                        {
                            definiteLocalScope.UnionWith(loopRepeatVars);
                        }
                        if (loopRepeatReturns) return (true, definiteOuterScope);
                        break;
                    case ForNextStatement s:
                        (var forNextReturns, var forNextVars) = AnalyseCore(s.Statements, definiteLocalScope);
                        definiteOuterScope.UnionWith(forNextVars);
                        if (!forNextReturns)
                        {
                            definiteLocalScope.UnionWith(forNextVars);
                        }
                        if (forNextReturns) return (true, definiteOuterScope);
                        break;
                    case CaseStmt s:
                        foreach (Case @case in s.Cases)
                        {
                            AnalyseCore(@case.Statements, definiteLocalScope);
                        }
                        break;
                    case GosubStatement s:
                        if (!JumpsTaken.Contains(s.Label.Name))
                        {
                            if (_prog.SymbolTable.Labels.ContainsKey(s.Label.Name)){
                                var stmtsAfterLabel = _prog.SymbolTable.Labels[s.Label.Name].StatementsFollowingLabel;
                                JumpsTaken.Push(s.Label.Name);
                                (var gosubReturns, var gosubVars) = AnalyseCore(stmtsAfterLabel, definiteLocalScope);
                                JumpsTaken.Pop();
                                definiteLocalScope.UnionWith(gosubVars);
                                definiteOuterScope.UnionWith(gosubVars);  
                            }
                        }
                        break;
                    case GoToStatement s:
                        if (!JumpsTaken.Contains(s.Label.Name))
                        {
                            if (_prog.SymbolTable.Labels.ContainsKey(s.Label.Name))
                            {
                                JumpsTaken.Push(s.Label.Name);
                                (var gotoReturns, var gotoVars) = AnalyseCore(_prog.SymbolTable.Labels[s.Label.Name].StatementsFollowingLabel, definiteLocalScope);
                                JumpsTaken.Pop();
                                // Goto always (effectively) returns...
                            }
                        }
                        return (true, definiteOuterScope);

                    case OnGosubStatement s:
                        HashSet<Token> onGosubVars = new(new TokenEqualityComparer());
                        bool firstTime = true;
                        foreach (var label in s.Labels)
                        {
                            if (_prog.SymbolTable.Labels.ContainsKey(label.Name))
                            {
                                JumpsTaken.Push(label.Name);
                                (var gosubReturns, var gosubVars) = AnalyseCore(_prog.SymbolTable.Labels[label.Name].StatementsFollowingLabel, definiteLocalScope);
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
                        definiteLocalScope.UnionWith(onGosubVars);
                        definiteOuterScope.UnionWith(onGosubVars);
                        break;
                    case OnGotoStatement s:
                        foreach (var label in s.Labels)
                        {
                            if (_prog.SymbolTable.Labels.ContainsKey(label.Name))
                            {
                                JumpsTaken.Push(label.Name);
                                AnalyseCore(_prog.SymbolTable.Labels[label.Name].StatementsFollowingLabel, definiteLocalScope);
                                JumpsTaken.Pop();
                            }
                        }
                        return (true, definiteOuterScope);

                }
            }
            return (false, definiteOuterScope);
        }

        public void Analyse()
        {
            HashSet<Token> env = new( _prog.SymbolTable.ProcedureParameters, new TokenEqualityComparer());
            AnalyseCore(_prog.Statements, env);
        }
    }
}
