using BasicPlusParser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser
{
    class AngleArrExpression : Expression
    {
        public AngleArrExpression(Token token, params Expression[] children) : base(token, children) { }

    }
}
