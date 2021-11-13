using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasicPlusParser.Statements.Expressions;

namespace BasicPlusParser.Statements
{
    public class ReadStatement : ThenElseStatement
    {
        public Expression Cursor;
        public Expression Handle;
        public Expression Key;
        public IdExpression Variable;
    }
}