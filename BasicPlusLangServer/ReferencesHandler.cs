using BasicPlusParser;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace BasicPlusLangServer
{
    internal class ReferencesHandler : ReferencesHandlerBase
    {

        readonly TextDocumentManager _documentManager;

        public ReferencesHandler(TextDocumentManager textDocManager)
        {
            _documentManager = textDocManager;
        }


        public async override Task<LocationContainer> Handle(ReferenceParams request, CancellationToken cancellationToken)
        {
            var doc = _documentManager.GetDocument(request.TextDocument.Uri.ToString());
            if (doc == null)
            {
                return new LocationContainer();
            }

            Symbol symbol = doc.Proc.GetSymbol(request.Position.Line + 1, request.Position.Character, request.TextDocument.Uri.ToString());
            if (symbol == null)
            {
                return new LocationContainer();
            }

            List<Location> locations = new List<Location>();
            foreach (SymbolReference reference in symbol.References)
            {
                locations.Add(new Location
                {
                    Uri = symbol.Token.FileName,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(reference.Token.LineNo - 1, reference.Token.StartCol, reference.Token.LineNo- 1, reference.Token.EndCol)
                });
            }

            return new LocationContainer(locations);
        }

        protected override ReferenceRegistrationOptions CreateRegistrationOptions(ReferenceCapability capability, ClientCapabilities clientCapabilities)
        {
            return new ReferenceRegistrationOptions()
            {
                DocumentSelector = DocumentSelector.ForPattern(@"**/*.bp")
            };
        }
    }
}
