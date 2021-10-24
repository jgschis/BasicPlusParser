using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser
{
    public class ReadVStatement : Statement
    {
        public string Variable;
        public Expression TableVar;
        public Expression Key;
        public Expression Column;
        public List<Statement> Then;
        public List<Statement> Else;
    }
}
