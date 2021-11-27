using BasicPlusParser.Tokens;
using System.Collections.Generic;


namespace BasicPlusParser
{
    public class OiProgram
    {
        public string Name;
        public ProgramType PType;
        public List<Statement> Statements = new();
        public List<string> Parameters;
        public Dictionary<string, Label> Labels = new();
        public ParseErrors Errors;
        public Dictionary<string, Matrix> Matricies = new();
        public HashSet<string> Functions = new();
        public HashSet<string> Subroutines = new();
        public HashSet<string> Equates = new();



        public OiProgram(ProgramType pType, string name, List<string> args)
        {
            PType = pType;
            Name = name;
            Parameters = args;  
        }
    }
}
