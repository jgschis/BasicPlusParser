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
    }
}
