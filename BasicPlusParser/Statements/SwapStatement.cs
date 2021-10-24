using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Statements
{
    public class SwapStatement : Statement
    {
        public Expression Old;
        public Expression New;
        public string Name;
    }
}
