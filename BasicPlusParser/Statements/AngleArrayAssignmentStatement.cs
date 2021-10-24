using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser
{
    class AngleArrayAssignmentStatement : Statement
    {
        public string Name;
        public Expression Expr;
        public List<Expression> Indexes;
    }
}
