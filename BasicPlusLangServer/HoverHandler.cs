using BasicPlusParser;
using BasicPlusParser.Statements.Expressions;
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
                Symbol symbol = doc.Proc.GetSymbol(request.Position.Line + 1, request.Position.Character, request.TextDocument.Uri.ToString());
                if (symbol == null)
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

        protected override HoverRegistrationOptions CreateRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
        {
            return new HoverRegistrationOptions()
            {
                DocumentSelector = DocumentSelector.ForPattern(@"**/*.bp")
            };
        }
    }
}
