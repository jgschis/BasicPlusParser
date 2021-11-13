using BasicPlusParser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasicPlusParser.Statements.Expressions;

namespace BasicPlusParser.Statements
{
    public class MatStatement : Statement
    {
        public IdExpression Variable;
        public Expression Expr;
        public IdExpression OtherMatrix;
    }
}
