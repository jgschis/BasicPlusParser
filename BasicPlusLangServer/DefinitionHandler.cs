using BasicPlusParser;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;


namespace BasicPlusLangServer
{
    internal class DefinitionHandler : DefinitionHandlerBase
    {
        readonly TextDocumentManager _documentManager;

        public DefinitionHandler(TextDocumentManager textDocManager)
        {
            _documentManager = textDocManager;
        }


        public override async Task<LocationOrLocationLinks> Handle(DefinitionParams request, CancellationToken cancellationToken)
        {
            var doc = _documentManager.GetDocument(request.TextDocument.Uri.ToString());
            if (doc == null)
            {
                return new LocationOrLocationLinks();
            }

            Symbol symbol = doc.Proc.GetSymbol(request.Position.Line + 1, request.Position.Character, request.TextDocument.Uri.ToString());
            if (symbol == null)
            {
                return new LocationOrLocationLinks();
            }

            return new LocationOrLocationLinks(new LocationOrLocationLink(new Location 
                { Uri = symbol.Token.FileName,
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(symbol.Token.LineNo-1,symbol.Token.StartCol,symbol.Token.LineNo-1,symbol.Token.EndCol)}));
        }

        protected override DefinitionRegistrationOptions CreateRegistrationOptions(DefinitionCapability capability, ClientCapabilities clientCapabilities)
        {
            return new DefinitionRegistrationOptions()
            {
                DocumentSelector = DocumentSelector.ForPattern(@"**/*.bp")
            };
        }
    }
}
