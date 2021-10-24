using BasicPlusParser.Tokens;

namespace BasicPlusParser.Statements.Expressions
{
    class NullExpression : Expression
    {
        public NullExpression(Token token, params Expression[] children) : base(token, children) { }
    }
}
