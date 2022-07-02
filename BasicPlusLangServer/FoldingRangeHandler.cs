using BasicPlusParser;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;


namespace BasicPlusLangServer
{
    internal class FoldingRangeHandler : FoldingRangeHandlerBase
    {
        readonly TextDocumentManager _documentManager;

        public FoldingRangeHandler(TextDocumentManager textDocManager)
        {
            _documentManager = textDocManager;
        }


        public override Task<Container<FoldingRange>?> Handle(FoldingRangeRequestParam request, CancellationToken cancellationToken)
        {
            var foldingRanges = new List<FoldingRange>();

            var doc = _documentManager.GetDocument(request.TextDocument.Uri.ToString());
            if (doc != null)
            {

                foreach (var region in doc.Proc.Regions)
                {
                    foldingRanges.Add(new FoldingRange
                    {
                        StartLine = region.StartLineNo - 1,
                        StartCharacter = region.StartCharPos,
                        EndLine = region.EndLineNo -1,
                        EndCharacter = region.EndCharPos,
                        Kind = FoldingRangeKind.Region
                    }); ;
                }
            }
            return Task.FromResult<Container<FoldingRange>?>(new Container<FoldingRange>(foldingRanges.ToArray()));
        }

        protected override FoldingRangeRegistrationOptions CreateRegistrationOptions(FoldingRangeCapability capability, ClientCapabilities clientCapabilities)
        {
            return new FoldingRangeRegistrationOptions
            {
                DocumentSelector = DocumentSelector.ForPattern(@"**/*.txt")
            };
        }
    }
}
