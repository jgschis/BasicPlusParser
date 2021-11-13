using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasicPlusParser.Statements.Expressions;

namespace BasicPlusParser
{
    public class ReadVStatement : ThenElseStatement
    {
        public IdExpression Variable;
        public Expression TableVar;
        public Expression Key;
        public Expression Column;
    }
}
