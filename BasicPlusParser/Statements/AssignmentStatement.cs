using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser
{
    public class AssignmentStatement : Statement
    {
        public Expression Expr;
        public string Name;
    }
}
