using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasicPlusParser.Statements.Expressions;

namespace BasicPlusParser
{
    class SquareBracketArrayAssignmentStatement : Statement
    {

        public List<Expression> Indexes = new();
        public Expression Value;
        public IdExpression Variable;
    }

}
