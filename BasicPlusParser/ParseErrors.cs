using BasicPlusParser.Tokens;
using System.Collections.Generic;

namespace BasicPlusParser
{
    public class ParseErrors
    {
        public bool HasError;

        public List<ParseError> Errors = new();

        public void ReportError(int line ,string message, int startCol, int endCol)
        {
            Errors.Add(new ParseError
            {
                LineNo = line,
                Message =message,
                StartCol = startCol,
                EndCol = endCol
            });
            HasError = true;
        }

        public void ReportError(Token token, string message)
        {
            Errors.Add(new ParseError
            {
                LineNo = token.LineNo,
                Message = message,
                StartCol = token.StartCol,
                EndCol = token.EndCol
            });
            HasError = true;
        }
    }
}
