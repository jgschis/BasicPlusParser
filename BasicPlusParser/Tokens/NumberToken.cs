using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Tokens
{
    class NumberToken : Token
    {
        public override string LsClass { get; set; } = "number";
    }
}
