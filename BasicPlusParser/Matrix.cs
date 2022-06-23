using BasicPlusParser.Statements.Expressions;
using BasicPlusParser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser
{
    public class Matrix : IdExpression
    {
        public Expression Col;
        public Expression Row;

        public Matrix(Token token,Expression col, Expression row, IdentifierType identifierType = IdentifierType.Assignment):base(token,identifierType)
        {
            Col = col;
            Row = row;
        }
    }
}
