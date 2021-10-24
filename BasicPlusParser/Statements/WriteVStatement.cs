using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Statements
{
    public class WriteVStatement : Statement
    {
        public Expression Expr;
        public Expression Handle;
        public Expression Key;
        public Expression Col;
        public List<Statement> Then;
        public List<Statement> Else;
    }
}
