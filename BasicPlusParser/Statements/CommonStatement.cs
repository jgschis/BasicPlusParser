using System.Collections.Generic;

namespace BasicPlusParser.Statements
{
    public class CommonStatement : Statement
    {
        public string CommonName;
        public List<string> GlovalVars = new();
    }
}
