using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasicPlusParser.Statements.Expressions;

namespace BasicPlusParser.Statements
{
    public class ConvertStatement : Statement
    {
        public Expression From;
        public Expression To;
        public Expression In;
    }
}
