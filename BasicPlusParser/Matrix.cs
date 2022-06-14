using BasicPlusParser.Statements.Expressions;
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

        public Matrix(string name,Expression col, Expression row, IdentifierType identifierType = IdentifierType.Assignment):base(name,identifierType)
        {
            Col = col;
            Row = row;
        }
    }
}
