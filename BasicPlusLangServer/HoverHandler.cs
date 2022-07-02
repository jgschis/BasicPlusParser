using BasicPlusParser;
using BasicPlusParser.Statements.Expressions;
using BasicPlusParser.Tokens;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;


namespace BasicPlusLangServer
{
    internal class HoverHandler : HoverHandlerBase
    {
        readonly TextDocumentManager _documentManager;

        public HoverHandler(TextDocumentManager documentManager)
        {
            _documentManager = documentManager;
        }

        public override Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
        {
            Hover? hover = null;

            var doc = _documentManager.GetDocument(request.TextDocument.Uri.ToString());
            if (doc != null)
            {
                var token = FindClosestToken(doc.Proc.Tokens, request.Position.Line + 1, request.Position.Character);
                if (!doc.Proc.SymbolTable.SymbolIndex.TryGetValue($"{token.LineNo}.{token.StartCol}", out Symbol symbol))
                {
                    return Task.FromResult<Hover?>(null);
                }

                string scope =  symbol.Scope switch
                {
                    VariableScope.Local => "(local)",
                    VariableScope.Parameter => "(parameter)",
                    VariableScope.Common => "(common)",
                    VariableScope.System => "(system)",
                    _ => ""
                };

                if (symbol.Kind == BasicPlusParser.SymbolKind.Equate)
                {
                    switch (symbol.EquateValue)
                    {
                        case NumExpression e:
                            hover = new Hover() { Contents = new MarkedStringsOrMarkupContent($"(equate) {symbol.Token.Text} = {e.Value}") };
                            break;
                        case StringExpression e:
                            hover = new Hover() { Contents = new MarkedStringsOrMarkupContent(($"(equate) {symbol.Token.Text} = {e.Value}")) };
                            break;
                        default:
                            hover = new Hover() { Contents = new MarkedStringsOrMarkupContent($"(equate) {symbol.Token.Text} = ?") };
                            break;
                    }
                }
                else if (symbol.Kind == BasicPlusParser.SymbolKind.Variable)
                {
                    if (symbol.Type == VariableType.Matrix)
                    {
                        hover = new Hover() { Contents = new MarkedStringsOrMarkupContent($"{scope} (matrix) {symbol.Token.Text}") };
                    }
                    else
                    {
                        hover = new Hover() { Contents = new MarkedStringsOrMarkupContent($"{scope} {symbol.Token.Text}") };
                    }
                }
                else if (symbol.Kind == BasicPlusParser.SymbolKind.Function)
                {
                    hover = new Hover() { Contents = new MarkedStringsOrMarkupContent($"(function) {symbol.Token.Text}") };

                }
                else if (symbol.Kind == BasicPlusParser.SymbolKind.Subroutine)
                {
                    hover = new Hover() { Contents = new MarkedStringsOrMarkupContent($"(subroutine) {symbol.Token.Text}") };

                }
                else if (symbol.Kind == BasicPlusParser.SymbolKind.Insert)
                {
                    hover = new Hover() { Contents = new MarkedStringsOrMarkupContent($"(insert) {symbol.Token.Text}") };
                }
                else if (symbol.Kind == BasicPlusParser.SymbolKind.Label)
                {
                    hover = new Hover() { Contents = new MarkedStringsOrMarkupContent($"(label) {symbol.Token.Text}") };
                }

            }
            return Task.FromResult(hover);
        }


        Token FindClosestToken(List<Token> tokens,int lineNo, int lineCol)
        {
            int min = 0;
            int max = tokens.Count;
            int index = 0;
            while (min<=max)
            {
                 index = ((max - min) / 2) + min;

                Token token = tokens[index];
                if (token.LineNo == lineNo && token.StartCol <= lineCol && token.EndCol >= lineCol)
                {
                    // Return exact match
                    return token;
                }
                else if (token.LineNo < lineNo || (token.LineNo == lineNo && token.StartCol < lineCol))
                {
                    min = index + 1;
                }
                else
                {
                    max = index - 1;
                }

            }
            // Return closest match
            return tokens[index];
        }



        protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
        {
            return new HoverRegistrationOptions()
            {
                DocumentSelector = DocumentSelector.ForPattern(@"**/*.txt")
            };
        }
    }
}
