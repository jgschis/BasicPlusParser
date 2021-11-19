using BasicPlusParser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Statements.Expressions
{
    public class MatrixIndexExpression : Expression
    {
        public IdExpression Name;
        public Expression Col;
        public Expression Row;
    }
}
