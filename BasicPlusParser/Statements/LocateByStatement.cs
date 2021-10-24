using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser
{
    public class LocateByStatement : Statement
    {
        public List<Statement> Then;
        public List<Statement> Else;
        public Expression Needle;
        public Expression Haystack;
        public Expression Seq;
        public string Pos;
        public Expression Delim;
    }
}
