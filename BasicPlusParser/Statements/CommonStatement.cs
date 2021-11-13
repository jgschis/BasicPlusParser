using BasicPlusParser.Statements.Expressions;
using System.Collections.Generic;

namespace BasicPlusParser.Statements
{
    public class CommonStatement : Statement
    {
        public IdExpression CommonName;
        public List<IdExpression> GlovalVars = new();
    }
}
