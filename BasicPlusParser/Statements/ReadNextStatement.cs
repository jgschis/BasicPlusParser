using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser
{
    public class ReadNextStatement : Statement
    {
        public Expression Cursor;
        public string Variable;
        public string Value;
        public List<Statement> Then;
        public List<Statement> Else;

    }
}
