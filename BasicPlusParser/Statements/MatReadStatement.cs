using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Statements
{
    public class MatReadStatement : Statement
    {
        public string Var;
        public Expression Handle;
        public Expression Cursor;
        public Expression Key;
        public List<Statement> Then;
        public List<Statement> Else;
    }
}
