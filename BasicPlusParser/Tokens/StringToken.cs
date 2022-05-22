using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Tokens
{
    public class StringToken : Token
    {
        public string Str { get; set; }
        public char Delim { get; set; }

        public override string LsClass { get; set; } =  "string";
    }
}
