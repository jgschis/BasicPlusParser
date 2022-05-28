using BasicPlusParser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser
{
    public class ParseException : Exception
    {
        public Token Token;
        public ParseException()
        {
         
        }

        public ParseException(Token token, string message)
            : base(message)
        {
            Token = token;
        }

        public ParseException(string message, Exception inner)
            : base(message, inner)
        {

        }

        public ParseException(Token token, string message, Exception inner)
           : base(message, inner)
        {
            Token = token;
        }

    }
}
