using BasicPlusParser.Tokens;
using System.Collections.Generic;


namespace BasicPlusParser
{
    public class Procedure
    {
        public string Name;
        public ProcedureType PType;
        public List<Statement> Statements = new();
        public ParseErrors Errors = new();
        public Symbols SymbolTable = new();
        public List<Region> Regions = new();
        public List<Token> Tokens = new();
        public List<Token> CommentTokens = new();

        // If the file is empty, return a "blank" program.
        public Procedure() { }
        
        public Procedure(string name, ProcedureType pType)
        {
            PType = pType;
            Name = name;
        }

        public Symbol GetSymbol(int lineNo, int col)
        {
            SymbolReference symbolRef;
            int min = 0;
            int max = Tokens.Count;
            int index = 0;
            while (min <= max)
            {
                index = ((max - min) / 2) + min;

                Token token = Tokens[index];
                if (token.LineNo == lineNo && token.StartCol <= col && token.EndCol >= col && SymbolTable.SymbolIndex.TryGetValue($"{token.LineNo}.{token.StartCol}", out symbolRef))
                {
                    // Return exact match
                    return symbolRef.Symbol;
                }
                else if (token.LineNo < lineNo || (token.LineNo == lineNo && token.StartCol < col))
                {
                    min = index + 1;
                }
                else
                {
                    max = index - 1;
                }
            }
            // Return closest match
            SymbolTable.SymbolIndex.TryGetValue($"{Tokens[index].LineNo}.{Tokens[index].StartCol}", out symbolRef);
            return symbolRef?.Symbol;
        }

        public Token GetToken(int lineNo, int col)
        {
            int min = 0;
            int max = Tokens.Count;
            int index = 0;
            while (min <= max)
            {
                index = ((max - min) / 2) + min;

                Token token = Tokens[index];
                if (token.LineNo == lineNo && token.StartCol <= col && token.EndCol >= col)
                {
                    // Return exact match
                    return token;
                }
                else if (token.LineNo < lineNo || (token.LineNo == lineNo && token.StartCol < col))
                {
                    min = index + 1;
                }
                else
                {
                    max = index - 1;
                }
            }
            // Return closest match
            return Tokens[index];
        }

        // Merges comment and non-comment tokens into single list.
        public IEnumerable<Token> GetTokens(bool includeComments = false)
        {
            int i = 0;
            int j = 0;
            while (true)
            {
                if (i < Tokens.Count && j < CommentTokens.Count)
                {
                    if (Tokens[i].Pos < CommentTokens[j].Pos)
                    {
                        yield return Tokens[i++];
                    }
                    else
                    {
                        yield return CommentTokens[j++];
                    }
                }
                else if (i < Tokens.Count)
                {
                    yield return Tokens[i++];
                }
                else if (j < CommentTokens.Count)
                {
                    yield return CommentTokens[j++];
                }
                else
                {
                    break;
                }
            }
        }     
    }
}

