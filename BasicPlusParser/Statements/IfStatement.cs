using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser
{
    public class IfStatement : Statement
    {
        public Expression Condition;
        public List<Statement> ThenBlock = new List<Statement>();
        public List<Statement> ElseBlock = new List<Statement>();
    }
}
