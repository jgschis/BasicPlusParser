using BasicPlusParser.Tokens;

namespace BasicPlusParser
{
    public class SymbolReference
    {
        public Symbol Symbol;
        public Token Token;

        public SymbolReference(Symbol symbol, Token token)
        {
            Symbol = symbol;
            Token = token; 
        }
    }
}
