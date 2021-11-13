using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Tokens
{
    public abstract class Token
    {
        public int LineNo { get; set; }
        public string Text { get; set; }
        public int Pos { get; set; }
        public int Col { get; set; }
        public bool DisallowFunction = false;
    }
}
