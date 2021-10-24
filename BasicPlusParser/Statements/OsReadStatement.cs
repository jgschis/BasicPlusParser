using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Statements
{
    public class OsReadStatement : Statement
    {
        public string Variable;
        public Expression FilePath;
        public List<Statement> ThenBlock;
        public List<Statement> ElseBlock;
    }
}
