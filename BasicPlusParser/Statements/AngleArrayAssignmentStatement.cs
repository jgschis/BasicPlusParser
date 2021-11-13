using System.Collections.Generic;
using BasicPlusParser.Statements.Expressions;

namespace BasicPlusParser
{
    class AngleArrayAssignmentStatement : Statement
    {
        public IdExpression Variable;
        public Expression Value;
        public List<Expression> Indexes;
    }
}
