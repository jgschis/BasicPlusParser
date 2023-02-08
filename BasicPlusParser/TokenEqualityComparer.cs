using BasicPlusParser.Tokens;
using System.Collections.Generic;

namespace BasicPlusParser
{
    public class TokenEqualityComparer : IEqualityComparer<Token>
    {
        public bool Equals(Token x, Token y)
        {
            return string.Equals(x.Text, y.Text, System.StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(Token token)
        {
            return token.Text.GetHashCode(System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
