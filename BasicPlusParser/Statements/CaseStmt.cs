using BasicPlusParser.Tokens;
using System.Collections.Generic;

namespace BasicPlusParser.Statements
{
    public class CaseStmt : Statement
    {
        public List<Case> Cases;

        public override HashSet<Token> GetReferencedVars()
        {
            HashSet<Token> referencedVars = new(new TokenEqualityComparer());
            foreach (var @case in Cases)
            {
                referencedVars.UnionWith(@case.Condition.GetReferencedVars());
            }
            return referencedVars;
        }
    }
}
