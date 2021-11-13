using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasicPlusParser.Statements.Expressions;

namespace BasicPlusParser.Statements
{
    public class MatReadStatement : ThenElseStatement
    {
        public IdExpression Var;
        public Expression Handle;
        public Expression Cursor;
        public Expression Key;
    }
}
