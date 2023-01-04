using BasicPlusParser.Statements.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Statements
{
    public class InsertStatement : Statement
    {
        public IdExpression Name;
        public List<Statement> Statements;


    }
}
