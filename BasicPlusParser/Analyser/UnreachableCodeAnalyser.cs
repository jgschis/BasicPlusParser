using System.Collections.Generic;

namespace BasicPlusParser.Analyser
{
    public class UnreachableCodeAnalyser
    {
        const string START = "";

        OiProgram _prog;
        readonly List<string> UnreachableStatements = new();
        string _currLabel = START;
        Dictionary<string, List<string>> _reachabilityGraph = new();
        HashSet<string> _reachableLabels = new();


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
                    AddUnreachableStatement(statement);
                }

                bool childrenReturn = false;
                switch (statement)
                {
                    case ReturnStatement:
                        returnStatementSeen = true;
                        break;
                    case InternalSubStatement s:
                        if (!(returnStatementSeen || gotoStatementSeen)) UpdateReachabilityGraph(s.Label.Name);
                        _currLabel = s.Label.Name;
                        returnStatementSeen = false;
                        gotoStatementSeen = false;
                        break;
                    case GoToStatement s:
                        if (!statementNotReachable) UpdateReachabilityGraph(s.Label.Name);
                        gotoStatementSeen = true;
                        break;
                    case GosubStatement s:
                        if (!statementNotReachable) UpdateReachabilityGraph(s.Label.Name);
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
    

        void AddUnreachableStatement(Statement s)
        {
            UnreachableStatements.Add($"[{s.LineNo}]: {s}");
        }

        void UpdateReachabilityGraph(string label)
        {
            _reachabilityGraph[_currLabel].Add(label);
        }

        void AnalyseReachabilityGraphCore(string label)
        {
            foreach (string edgeLabel in _reachabilityGraph[label])
            {
                if (!_reachableLabels.Contains(edgeLabel))
                {
                    _reachableLabels.Add(edgeLabel);
                    AnalyseReachabilityGraphCore(edgeLabel);
                }
            }
        }

        void AnalyseReachabilityGraph()
        {
            AnalyseReachabilityGraphCore(START);
            foreach(var label in _prog.Labels)
            {
                if (!_reachableLabels.Contains(label.Key))
                {
                    AddUnreachableStatement(label.Value.Item1[label.Value.pos - 1]);
                }
            }
        }

        public void Analyse()
        {
            _reachabilityGraph[START] = new();
            foreach (var lbl in _prog.Labels)
           {
                _reachabilityGraph[lbl.Key] = new();
           }
            AnalyseCore(_prog.Statements);
            AnalyseReachabilityGraph();
        }
    }
}
