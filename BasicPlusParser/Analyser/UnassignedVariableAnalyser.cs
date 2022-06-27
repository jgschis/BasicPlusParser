using BasicPlusParser.Statements;
using BasicPlusParser.Tokens;
using System.Collections.Generic;
using System.Linq;

namespace BasicPlusParser.Analyser
{
    public class UnassignedVariableAnalyser
    {
        Procedure _prog;


        public List<(Token, Statement)> UnassignedVars = new();

        //When we take a goto or gosub statement, store the statement so we don't take it again (to avoid infinite recursion)
        readonly List<Statement> JumpsTaken = new();
      
        
        public UnassignedVariableAnalyser(Procedure prog)
        {
            _prog = prog;
        }

        public (bool, HashSet<Token>) AnalyseCore(IEnumerable<Statement> statements, HashSet<Token> env = null)
        {

            bool branchReturns = false;
            // Contains variables that are definitivley assigned in a block
            HashSet<Token> definiteLocalScope = new(new TokenEqualityComparer());
            // Contains variables that are definitely assigned globally.
            HashSet<Token> definiteOuterScope = new(new TokenEqualityComparer());
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
                        UnassignedVars.Add((err, statement));
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

                    case GoToStatement s:
                        if (!JumpsTaken.Any(x=> object.ReferenceEquals(s,x)))
                        {
                            if (_prog.SymbolTable.Labels.ContainsKey(s.Label.Name))
                            {
                                JumpsTaken.Add(s);
                                (var gotReturns, var gotoVars) = AnalyseCore(_prog.SymbolTable.Labels[s.Label.Name].StatementsFollowingLabel, definiteLocalScope);
                                definiteOuterScope.UnionWith(gotoVars);
                                // Goto always (effectively) returns...
                            }
                        }
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
                        if (!JumpsTaken.Any(x => object.ReferenceEquals(s, x)))
                        {
                            if (_prog.SymbolTable.Labels.ContainsKey(s.Label.Name)){
                                var stmtsAfterLabel = _prog.SymbolTable.Labels[s.Label.Name].StatementsFollowingLabel;
                                JumpsTaken.Add(s);
                                (var gosubReturns, var gosubVars) = AnalyseCore(stmtsAfterLabel, definiteLocalScope);
                                definiteLocalScope.UnionWith(gosubVars);
                                definiteOuterScope.UnionWith(gosubVars);  
                            }
                        }
                        break;

                } 
            }
            return (false, definiteOuterScope);
        }

        public  void Analyse()
        {
            HashSet<Token> env = new( _prog.SymbolTable.ProcedureParameters, new TokenEqualityComparer());
            AnalyseCore(_prog.Statements, env);
        }
    }
}
