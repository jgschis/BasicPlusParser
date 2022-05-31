using BasicPlusParser.Tokens;
using System.Collections.Generic;

namespace BasicPlusParser
{
    public class TokenizerOutput
    {
        // This contains all tokens except comment tokens. The parser uses this list.
        // Includnig comments in the list of tokens makes the parsing more complicated.
        public List<Token> Tokens;
        // This contains all the comment tokens. This is used for syntax highlighting.
        public List<Token> CommentTokens;
    }
}
