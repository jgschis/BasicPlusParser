using BasicPlusParser.Tokens;
using System.Linq;


namespace BasicPlusParser
{
    class ComparisonExpression : Expression
    {
        public bool CaseSensitieve = true;
        public bool FullPrecision = false;

        public ComparisonExpression(Token token, params Expression[] children) : base(token, children)

        {
            if (token.Text.ToLower().Last() == 'c'){
                CaseSensitieve = true;
            } else if(token.Text.ToLower().Last() == 'x')
            {
                FullPrecision = true;
            }
        }
    }
}
