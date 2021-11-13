using BasicPlusParser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasicPlusParser.Statements.Expressions;

namespace BasicPlusParser.Statements
{
    public class MatAssignmentStatement : Statement
    {
        public IdExpression Variable;
        public Expression Col;
        public Expression Row;
        public Expression Value;
    }
}
