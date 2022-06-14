using BasicPlusParser.Statements.Expressions;
using BasicPlusParser.Tokens;

namespace BasicPlusParser
{
    class GoToStatement : Statement
    {
        public IdExpression Label;
    }
}
