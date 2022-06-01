using System.Collections.Generic;

namespace BasicPlusParser.Statements
{
    public class CaseStmt : Statement
    {
        public List<Case> Cases;

        public override HashSet<string> GetReferencedVars()
        {
            HashSet<string> referencedVars = new();
            foreach (var @case in Cases)
            {
                referencedVars.UnionWith(@case.Condition.GetReferencedVars());
            }
            return referencedVars;
        }
    }
}
