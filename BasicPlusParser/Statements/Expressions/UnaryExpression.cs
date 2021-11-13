using BasicPlusParser.Statements.Expressions;
using BasicPlusParser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Statements.Expressions
{
    public class UnaryExpression : Expression
    {
        public Expression Argument;
        public string Operator;
    }
}
