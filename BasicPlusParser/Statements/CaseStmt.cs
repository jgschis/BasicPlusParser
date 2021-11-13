using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BasicPlusParser.Statements.Expressions;

namespace BasicPlusParser
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
