using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasicPlusParser.Statements.Expressions;

namespace BasicPlusParser.Statements
{
    public class WriteStatement : ThenElseStatement
    {
        public Expression Expr;
        public Expression Handle;
        public Expression Key;
    }
}
