using BasicPlusParser.Statements;
using System.Collections.Generic;

namespace BasicPlusParser.Analyser
{
    public class UnreachableCodeAnalyser
    {
        const string START = "";

        readonly Procedure _prog;
        public readonly List<Statement> UnreachableStatements = new();
        public readonly List<Statement> UnreachableLabels = new();
        string _currLabel = START;
        readonly Dictionary<string, List<string>> _reachabilityGraph = new();
        readonly HashSet<string> _reachableLabels = new();


        public UnreachableCodeAnalyser(Procedure prog)
        {
            _prog = prog;
        }

         bool AnalyseBlock(List<Statement> statements)
        {  
            
            // Does the block we are currently analysing unconditionally return?
            // If so, every subsequent statement is unreachable.
            bool blockReturns = false;

            foreach (Statement statement in statements)
            {
                if (blockReturns && statement is not InternalSubStatement)
                {
                    AddUnreachableStatement(statement);
                }
                switch (statement)
                {
                    case ReturnStatement:
                        blockReturns = true;
                        break;
                    case InternalSubStatement s:
                        if (!blockReturns) UpdateReachabilityGraph(s.Label.Name);
                        _currLabel = s.Label.Name;
                        blockReturns = false;
                        break;
                    case GoToStatement s:
                        if (!blockReturns) UpdateReachabilityGraph(s.Label.Name);
                        blockReturns = true;
                        break;
                    case GosubStatement s:
                        if (!blockReturns) UpdateReachabilityGraph(s.Label.Name);
                        break;
                    case ThenElseStatement s:
                        bool thenReturns = AnalyseBlock(s.Then);
                        bool elseReturns = AnalyseBlock(s.Else);
                        blockReturns = thenReturns && elseReturns;
                        break;
                    case ForNextStatement s:
                        blockReturns |= AnalyseBlock(s.Statements);
                        break;
                    case LoopRepeatStatement s:
                        blockReturns |= AnalyseBlock(s.Statements);
                        break;
                    case CaseStmt s:
                        foreach (Case @case in s.Cases)
                        {
                            AnalyseBlock(@case.Statements);
                        }
                        break;
                    case OnGosubStatement s:
                        if (!blockReturns) s.Labels.ForEach( label => UpdateReachabilityGraph(label.Name));
                        break;
                    case OnGotoStatement s:
                        if (!blockReturns) s.Labels.ForEach(label => UpdateReachabilityGraph(label.Name));
                        blockReturns = true;
                        break;
                }
            }
            return blockReturns;
        }
    
        void AddUnreachableStatement(Statement s)
        {
            UnreachableStatements.Add(s);
        }

        void UpdateReachabilityGraph(string label)
        {
            if (_reachabilityGraph.ContainsKey(_currLabel)) {
                _reachabilityGraph[_currLabel].Add(label);
            }
        }

        void AnalyseReachabilityGraphCore(string label)
        {
            if (_reachabilityGraph.ContainsKey(label)){
                foreach (string edgeLabel in _reachabilityGraph[label])
                {
                    if (!_reachableLabels.Contains(edgeLabel))
                    {
                        _reachableLabels.Add(edgeLabel);
                        AnalyseReachabilityGraphCore(edgeLabel);
                    }
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
                    UnreachableLabels.Add(label.Value.LabelStmt);
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
            AnalyseBlock(_prog.Statements);
            AnalyseReachabilityGraph();
        }
    }
}
