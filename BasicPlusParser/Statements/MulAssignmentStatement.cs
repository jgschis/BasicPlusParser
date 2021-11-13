using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasicPlusParser.Statements.Expressions;

namespace BasicPlusParser
{
    public class MulAssignmentStatement : Statement
    {
        public IdExpression Name;
        public Expression Value;
    }
}
