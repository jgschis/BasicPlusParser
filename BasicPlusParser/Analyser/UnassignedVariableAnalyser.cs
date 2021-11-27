using System.Collections.Generic;
using System.Linq;

namespace BasicPlusParser.Analyser
{
    public class UnassignedVariableAnalyser
    {
        OiProgram _prog;


        public List<(string, Statement)> UnassignedVars = new();
      
        
        public UnassignedVariableAnalyser(OiProgram prog)
        {
            _prog = prog;
        }

        public (bool, HashSet<string>) AnalyseCore(IEnumerable<Statement> statements, HashSet<string> env = null)
        {

            bool branchReturns = false;
            HashSet<string> definiteLocalScope = new();
            HashSet<string> definiteOuterScope = new();
            if (env != null)
            {
                definiteLocalScope.UnionWith(env);
            }

            foreach (Statement statement in statements)
            {
                definiteLocalScope.UnionWith(statement.GetAssignedVars());
                if (!branchReturns)
                {
                    definiteOuterScope.UnionWith(statement.GetAssignedVars());
                }

                foreach (var err in statement.GetReferencedVars().Where(v => !definiteLocalScope.Contains(v)))
                {
                    UnassignedVars.Add((err, statement));
                }

                switch (statement)
                {
                    case ReturnStatement:
                        return (true, definiteOuterScope);

                    case GoToStatement s:
                        (var gotReturns, var gotoVars) = AnalyseCore(_prog.Labels[s.Label.Name].Item1.Skip(_prog.Labels[s.Label.Name].pos), definiteLocalScope);
                        definiteOuterScope.UnionWith(gotoVars);
                        // Goto always (effectively) returns...
                        return (true, definiteOuterScope);
     
                    case ThenElseStatement s:
                        (var thenReturns, var thenVars) = AnalyseCore(s.Then, definiteLocalScope);
                        (var elseReturns, var elseVars) = AnalyseCore(s.Else, definiteLocalScope);
                        definiteOuterScope.UnionWith(thenVars.Intersect(elseVars));
                        if (!thenReturns && !elseReturns) definiteLocalScope.UnionWith(thenVars.Intersect(elseVars));
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
                        var stmtsAfterLabel = _prog.Labels[s.Label.Name].Item1.Skip(_prog.Labels[s.Label.Name].pos);
                        (var gosubReturns, var gosubVars) = AnalyseCore(stmtsAfterLabel, definiteLocalScope);
                        definiteLocalScope.UnionWith(gosubVars);
                        definiteOuterScope.UnionWith(gosubVars);
                        break;
                } 
            }
            return (false, definiteOuterScope);
        }

        public  void Analyse()
        {

            HashSet<string> env = new();
            env.Add("true$");
            env.Add("false$");
            env.Add("red$");
            env.Add("@window");
            env.Add("@svm");
            env.Add("@vm");
            env.Add("@fm");
            env.Add("param1");
            env.Add("focus");
            env.Add("blue$");
            env.Add("event");
            env.Add("white$");
            env.Add("purchase_data");
            env.Add("data");
            env.Add("checkout_data");


            AnalyseCore(_prog.Statements,env);
        }
    }
}
