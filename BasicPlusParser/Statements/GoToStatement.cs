using BasicPlusParser.Statements.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser
{
    class GoToStatement : Statement
    {
        public IdExpression Label;
    }
}
