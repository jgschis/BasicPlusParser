using BasicPlusParser.Tokens;
using System.Collections.Generic;


namespace BasicPlusParser
{
    public class OiProgram
    {
        string Name;
        public ProgramType PType;
        public List<Statement> Statements = new List<Statement>();
        public Dictionary<string,Local> Locals = new Dictionary<string,Local>();
        public Dictionary<string, Local> Parms = new Dictionary<string, Local>();

        public OiProgram(ProgramType pType, string name, List<Token> args)
        {

            PType = pType;
            Name = name;
            foreach (Token arg in args)
            {
                Local local = new()
                {
                    IsParam = true,
                    Name = arg.Text,
                    Value = null
                };
                Locals.Add(local.Name,local);
                Parms.Add(local.Name,local);
            }
        }
    }
}
