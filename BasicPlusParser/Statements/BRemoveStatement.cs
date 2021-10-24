using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Statements
{
    public class BRemoveStatement : Statement
    {
        public string Flag;
        public string Pos;
        public Expression From;
        public string Var;
    }
}
