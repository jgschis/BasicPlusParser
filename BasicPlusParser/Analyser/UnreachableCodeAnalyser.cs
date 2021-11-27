using System.Collections.Generic;

namespace BasicPlusParser.Analyser
{
    public class UnreachableCodeAnalyser
    {
        OiProgram _prog;
        readonly List<Statement> UnreachableStatements = new();
        List<string> _labelsSeen = new();
        public UnreachableCodeAnalyser(OiProgram prog)
        {
            _prog = prog;
        }

         bool AnalyseCore(List<Statement> statements)
        {
            bool returnStatementSeen = false;
            bool gotoStatementSeen = false;
            foreach (Statement statement in statements)
            {
                bool statementNotReachable = statement is not InternalSubStatement && (returnStatementSeen || gotoStatementSeen);
                if (statementNotReachable)
                {
                    UnreachableStatements.Add(statement);
                }

                bool childrenReturn = false;
                switch (statement)
                {
                    case ReturnStatement:
                        returnStatementSeen = true;
                        break;
                    case InternalSubStatement s:
                        if (!(returnStatementSeen || gotoStatementSeen)) _labelsSeen.Add(s.Label.Name);
                        returnStatementSeen = false;
                        gotoStatementSeen = false;
                        break;
                    case GoToStatement s:
                        if (!statementNotReachable) _labelsSeen.Add(s.Label.Name);
                        gotoStatementSeen = true;
                        break;
                    case GosubStatement s:
                        if (!statementNotReachable) _labelsSeen.Add(s.Label.Name);
                        break;
                    case ThenElseStatement s:
                        childrenReturn = true && AnalyseCore(s.Then);
                        childrenReturn &= AnalyseCore(s.Else);
                        break;
                    case ForNextStatement s:
                        childrenReturn = true && AnalyseCore(s.Statements);
                        break;
                    case LoopRepeatStatement s:
                        childrenReturn = true && AnalyseCore(s.Statements);
                        break;
                    case CaseStmt s:
                        foreach (Case @case in s.Cases)
                        {
                            AnalyseCore(@case.Statements);
                        }
                        break;                      
                }
                returnStatementSeen |= childrenReturn;
            }
            return returnStatementSeen;
        }
    
        public void Analyse()
        {
             AnalyseCore(_prog.Statements);
             foreach (var lbl in _prog.Labels)
            {
                if (!_labelsSeen.Contains(lbl.Key))
                {
                    UnreachableStatements.Add(lbl.Value.Item1[lbl.Value.pos-1]);
                }
            }

        }
    }
}
