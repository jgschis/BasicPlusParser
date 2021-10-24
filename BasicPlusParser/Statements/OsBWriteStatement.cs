using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser
{
    public class OsBWriteStatement : Statement
    {
        public string FileVar;
        public Expression Expr;
        public Expression Byte;
    }
}
