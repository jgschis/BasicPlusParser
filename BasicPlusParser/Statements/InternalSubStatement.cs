using BasicPlusParser.Statements.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser
{
    public class InternalSubStatement : Statement
    {
        public IdExpression Label;
        public List<Statement> Statements = new List<Statement>();
    }
}
