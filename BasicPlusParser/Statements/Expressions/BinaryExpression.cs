using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Statements.Expressions
{
    public class BinaryExpression : Expression
    {
        public Expression Left;
        public Expression Right;
        public string Operator;
    }
}
