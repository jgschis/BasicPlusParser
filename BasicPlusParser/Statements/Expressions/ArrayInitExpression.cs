using BasicPlusParser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Statements.Expressions
{
    class ArrayInitExpression : Expression
    {
        public ArrayInitExpression(Token token, params Expression[] children) : base(token, children) { }

    }
}
