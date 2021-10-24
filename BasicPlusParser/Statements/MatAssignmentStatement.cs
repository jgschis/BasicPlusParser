using BasicPlusParser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Statements
{
    public class MatAssignmentStatement : Statement
    {
        public IdentifierToken Name;
        public Expression Expr;
        public IdentifierToken OtherMatrix;
    }
}
