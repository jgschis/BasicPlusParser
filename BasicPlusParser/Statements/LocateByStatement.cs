using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasicPlusParser.Statements.Expressions;

namespace BasicPlusParser
{
    public class LocateByStatement : ThenElseStatement
    {
        public Expression Needle;
        public Expression Haystack;
        public Expression Seq;
        public IdExpression Pos;
        public Expression Delim;
    }
}
