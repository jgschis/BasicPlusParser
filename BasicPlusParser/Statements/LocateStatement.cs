using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Statements
{
    public class LocateStatement : Statement
    {
        public List<Statement> Then;
        public List<Statement> Else;
        public string Start;
        public Expression Delim;
        public Expression Needle;
        public Expression Haystack;
    }
}
