using BasicPlusParser.Tokens;
using System.Collections.Generic;

namespace BasicPlusParser
{
    public class TokenizerOutput
    {
        // This contains all tokens except trivial tokens.
        public List<Token> Tokens;
        // This contains all tokens that are not important in terms of what the progrma does. For example, comment tokens.
        public List<Token> TrivalTokens;

        public ParseErrors TokenErrors = new();
    }
}
