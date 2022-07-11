using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusLangServer
{
    public class DocumentSymbolHandler : DocumentSymbolHandlerBase
    {
        readonly TextDocumentManager _documentManager;

        public DocumentSymbolHandler(TextDocumentManager textDocManager)
        {
            _documentManager = textDocManager;
        }

        public async override Task<SymbolInformationOrDocumentSymbolContainer> Handle(DocumentSymbolParams request, CancellationToken cancellationToken)
        {
            List<SymbolInformationOrDocumentSymbol> symbols = new();

            var doc = _documentManager.GetDocument(request.TextDocument.Uri.ToString());

            if (doc == null)
            {
                return new SymbolInformationOrDocumentSymbolContainer();
            }


            foreach ((var key, var value) in doc.Proc.SymbolTable._Symbols)
            {
                SymbolKind? kind = GetSymbolKind(value);
                if (kind != null)
                {
                    symbols.Add(new SymbolInformationOrDocumentSymbol(new DocumentSymbol
                    {
                        Name = value.Token.Text,
                        Kind = (SymbolKind) kind,
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(value.Token.LineNo - 1, value.Token.StartCol, value.Token.LineNo - 1, value.Token.EndCol),
                        SelectionRange = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(value.Token.LineNo - 1, value.Token.StartCol, value.Token.LineNo - 1, value.Token.EndCol)
                    }));
                }

            }

            return new SymbolInformationOrDocumentSymbolContainer(symbols);
        }

        SymbolKind? GetSymbolKind(BasicPlusParser.Symbol symbol)
        {
            switch (symbol.Kind)
            {
                case BasicPlusParser.SymbolKind.Equate:
                    return SymbolKind.Constant;
                case BasicPlusParser.SymbolKind.Label:
                    return SymbolKind.Method;
                case BasicPlusParser.SymbolKind.Insert:
                    return SymbolKind.Module;
                case BasicPlusParser.SymbolKind.Variable:
                    return SymbolKind.Variable;
                default:
                    return null;
            }
        }

        protected override DocumentSymbolRegistrationOptions CreateRegistrationOptions(DocumentSymbolCapability capability, ClientCapabilities clientCapabilities)
        {
            return new DocumentSymbolRegistrationOptions
            {
                DocumentSelector = DocumentSelector.ForPattern(@"**/*.txt")
            };
        }
    }
}
