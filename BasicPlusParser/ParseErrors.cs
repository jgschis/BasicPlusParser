using BasicPlusParser.Tokens;
using System.Collections.Generic;

namespace BasicPlusParser
{
    public class ParseErrors
    {
        public bool HasError;

        public List<string> Errors = new();

        public void ReportError(int line ,string message)
        {
            Errors.Add($"[Line {line}: {message}]");
            HasError = true;
        }

        public void ReportError(Token token, string message)
        {
            Errors.Add($"[Line {token.LineNo}: {message}]");
            HasError = true;
        }

    }
}
