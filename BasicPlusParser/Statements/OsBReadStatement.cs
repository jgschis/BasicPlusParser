using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasicPlusParser.Statements.Expressions;

namespace BasicPlusParser
{
    public class OsBreadStatement : Statement
    {
        public IdExpression FileVariable;
        public IdExpression Variable;
        public Expression Byte;
        public Expression Length;
    }
}
