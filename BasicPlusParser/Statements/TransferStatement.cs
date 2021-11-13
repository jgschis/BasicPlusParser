using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasicPlusParser.Statements.Expressions;

namespace BasicPlusParser
{
    class TransferStatement : Statement
    {
        public IdExpression From;
        public IdExpression To;
    }
}
