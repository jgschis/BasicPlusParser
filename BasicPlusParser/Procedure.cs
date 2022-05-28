using BasicPlusParser.Tokens;
using System.Collections.Generic;


namespace BasicPlusParser
{
    public class Procedure
    {
        public string Name;
        public ProcedureType PType;
        public List<Statement> Statements = new();
        public List<string> Parameters;
        public Dictionary<string, Label> Labels = new();
        public ParseErrors Errors;
        public Dictionary<string, Matrix> Matricies = new();
        public HashSet<string> Functions = new();
        public HashSet<string> Subroutines = new();
        public HashSet<string> Equates = new();


        // If the file is empty, return a "blank" program.
        public Procedure() { }
        
        public Procedure(ProcedureType pType, string name, List<string> args)
        {
            PType = pType;
            Name = name;
            Parameters = args;  
        }
    }
}
