using System.Collections.Generic;
using BasicPlusParser.Statements.Expressions;
using BasicPlusParser.Tokens;

namespace BasicPlusParser.Statements
{
    public class OnGotoStatement : Statement
    {
        public Expression Index;
        public List<IdExpression> Labels;
    }
}
