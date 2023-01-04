using BasicPlusParser.Tokens;
using System.Collections.Generic;

namespace BasicPlusParser
{
    public class ParseErrors
    {
        public bool HasError;

        public List<ParseError> Errors = new();

        public void ReportError(int line ,string message, int startCol, int endCol, string fileName, ParserDiagnosticType pType = ParserDiagnosticType.Error)
        {
            Errors.Add(new ParseError
            {
                LineNo = line,
                Message =message,
                StartCol = startCol,
                EndCol = endCol,
                EndLineNo = line,
                PType = pType,
                FileName = fileName

            });
            HasError = true;
        }

        public void ReportError(Token token, string message, ParserDiagnosticType pType = ParserDiagnosticType.Error)
        {
            int endCol = token.EndCol;
            int endLineNo = token.EndLineNo;

            if (token is NewLineToken)
            {
                // If the token is a new line token, then we don't want to span multiple new lines...
                endCol = int.MaxValue;
                endLineNo = token.LineNo;
            }

            Errors.Add(new ParseError
            {
                LineNo = token.LineNo,
                Message = message,
                StartCol = token.StartCol,
                EndCol = endCol,
                EndLineNo = endLineNo,
                PType = pType,
                FileName = token.FileName
            }); 
            HasError = true;
        }
    }
}
