using BasicPlusParser.Statements.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Statements
{
    public class EquStatemnet : Statement
    {
        public IdExpression Variable;
        public Expression Value;
    }
}
