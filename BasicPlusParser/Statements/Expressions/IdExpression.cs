using BasicPlusParser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Statements.Expressions
{
    public class IdExpression : Expression
    {
        public string Name
        {
            set { NameRaw = value; }
            get
            {
                return NameRaw?.ToLower();
            }
        }
        public string NameRaw;


        public IdentifierType IdentifierType;

        public IdExpression(string name, IdentifierType identifierType = IdentifierType.Assignment)
        {
            Name = name;
            IdentifierType = identifierType;
        }

    }
}
