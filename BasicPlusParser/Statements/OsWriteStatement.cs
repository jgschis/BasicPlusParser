using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasicPlusParser.Statements.Expressions;

namespace BasicPlusParser.Statements
{
    public class OsWriteStatement : Statement
    {
        public Expression Expr;
        public Expression Location;
    }
}
