using BasicPlusParser.Tokens;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;


namespace BasicPlusLangServer
{
    public class CompletionHandler : CompletionHandlerBase
    {

        readonly TextDocumentManager _documentManager;

        public CompletionHandler(TextDocumentManager textDocManager)
        {
            _documentManager = textDocManager;
        }


        public async override Task<CompletionItem> Handle(CompletionItem request, CancellationToken cancellationToken)
        {
            return request;
        }

        public async override Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            var doc = _documentManager.GetDocument(request.TextDocument.Uri.ToString());
             if (doc == null)
            {
                List<CompletionItem> a = new List<CompletionItem>();
                a.Add(new CompletionItem { Detail = "abc1", InsertText = "end", Label = "abc2" });
                a.Add(new CompletionItem { Detail = "abc2", InsertText = "endz", Label = "abc3" });

                return new CompletionList(a);
            }

            List<CompletionItem> completionItems = new List<CompletionItem>();

            Token token = doc.Proc.GetToken(request.Position.Line + 1, request.Position.Character);
            if (token is ThenToken)
            {
                completionItems.Add(new CompletionItem { Detail = "abc1", InsertText = "end", Label = "abc2" });
                completionItems.Add(new CompletionItem { Detail = "abc2", InsertText = "endz", Label = "abc3" });

            }

            return new CompletionList(completionItems);   
        }

        protected override CompletionRegistrationOptions CreateRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
        {

            return new CompletionRegistrationOptions()
            {
                DocumentSelector = DocumentSelector.ForPattern(@"**/*.txt"),
                ResolveProvider = true,
                TriggerCharacters = new[] { "\r","\n" },
                AllCommitCharacters = new[] { "\r","\n"}
            };
        }
    }
}
