using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasicPlusParser.Statements.Expressions;

namespace BasicPlusParser.Statements
{
    public class RemoveStatement : Statement
    {
        public IdExpression Flag;
        public IdExpression Pos;
        public Expression From;
        public IdExpression Variable;
    }
}
