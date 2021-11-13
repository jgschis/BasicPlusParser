using BasicPlusParser.Tokens;
using System.Linq;


namespace BasicPlusParser.Statements.Expressions
{
    class ComparisonExpression : BinaryExpression
    {
        public bool CaseSensitieve = true;
        public bool FullPrecision = false;
        
    }
}
