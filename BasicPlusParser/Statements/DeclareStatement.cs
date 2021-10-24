using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Statements
{
    public class DeclareStatement : Statement
    {
        public List<string> Functions = new List<string>();
        public ProgramType PType;
    }
}
