using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser
{
    public class ForNextStatement : Statement
    {
        public Expression Step;
        public List<Statement> Statements = new List<Statement>();
        public AssignmentStatement Start;
        public Expression End;
    }
}
