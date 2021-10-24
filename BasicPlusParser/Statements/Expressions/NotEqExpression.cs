using BasicPlusParser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser
{
    class NotEqExpression : Expression
    {
        public NotEqExpression(Token token, params Expression[] children) : base(token, children) { }

    }
}
