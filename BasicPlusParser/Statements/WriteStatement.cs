using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Statements
{
    public class WriteStatement : Statement
    {
        public Expression Expr;
        public Expression Handle;
        public Expression Key;
        public List<Statement> Then;
        public List<Statement> Else;
    }
}
