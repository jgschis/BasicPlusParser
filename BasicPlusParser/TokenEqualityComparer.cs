using BasicPlusParser.Tokens;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BasicPlusParser
{
    internal class TokenEqualityComparer : IEqualityComparer<Token>
    {
        public bool Equals(Token x, Token y)
        {
            return x.Text.ToLower() == y.Text.ToLower();
        }

        public int GetHashCode([DisallowNull] Token obj)
        {
            return obj.Text.ToLower().GetHashCode();
        }
    }
}
