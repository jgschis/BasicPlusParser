using BasicPlusParser.Tokens;
using System.Collections.Generic;


namespace BasicPlusParser
{
    public class Procedure
    {
        public string Name;
        public ProcedureType PType;
        public List<Statement> Statements = new();
        public ParseErrors Errors;
        public Symbols SymbolTable = new();


        // If the file is empty, return a "blank" program.
        public Procedure() { }
        
        public Procedure(ProcedureType pType, string name, List<string> args)
        {
            PType = pType;
            Name = name;
        }
    }
}
