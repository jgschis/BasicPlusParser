using BasicPlusParser.Tokens;

namespace BasicPlusParser
{
    class AndExpression : Expression

    {
        public AndExpression(Token token, params Expression[] children) : base(token, children) { }
    }

}
