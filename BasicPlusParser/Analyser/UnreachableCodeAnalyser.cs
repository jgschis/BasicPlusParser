using System.Collections.Generic;

namespace BasicPlusParser.Analyser
{
    public class UnreachableCodeAnalyser
    {
        OiProgram _prog;
        readonly List<Statement> UnreachableStatements = new();

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
                if (statement is not InternalSubStatement && (returnStatementSeen || gotoStatementSeen)){
                    UnreachableStatements.Add(statement);
                }

                bool childrenReturn = false;
                switch (statement)
                {
                    case ReturnStatement:
                        returnStatementSeen = true;
                        break;
                    case InternalSubStatement:
                        returnStatementSeen = false;
                        gotoStatementSeen = false;
                        break;
                    case GoToStatement:
                        gotoStatementSeen = true;
                        break;
                    case ThenElseStatement s:
                        childrenReturn = true && AnalyseCore(s.Then) && AnalyseCore(s.Else);
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
        }
    }
}
