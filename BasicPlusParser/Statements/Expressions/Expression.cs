using BasicPlusParser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser
{
    public abstract class Expression : Statement
    {
        public Token Token;
        public List<Expression> Childen = null;

        public Expression(Token token, params Expression[] children)
        {
            Token = token;
            if (children.Length > 0)
            {
                Childen = new List<Expression>();
                foreach (Expression child in children)
                {
                    Childen.Add(child);
                }
            }
        }
    }
}
