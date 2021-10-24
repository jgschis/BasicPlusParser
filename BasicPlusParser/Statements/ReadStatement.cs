using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Statements
{
    public class ReadStatement : Statement
    {
        public Expression Cursor;
        public Expression Handle;
        public List<Statement> Then;
        public List<Statement> Else;
        public Expression Key;
        public string Var;
    }
}