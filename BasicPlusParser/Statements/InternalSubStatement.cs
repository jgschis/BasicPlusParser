using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser
{
    public class InternalSubStatement : Statement
    {
        public string Name;
        public List<Statement> Statements = new List<Statement>();
    }
}
