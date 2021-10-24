using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser
{
    class SquareBracketArrayAssignmentStatement : Statement
    {

        public List<Expression> Indexes = new List<Expression>();
        public Expression Expr;
        public string Name;
    }

}
