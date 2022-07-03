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
            return new CompletionItem { Detail = "test1", InsertText = "helo world1", Label = "lab2l" };

        }

        public async override Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {

     
            List<CompletionItem> completionItems = new List<CompletionItem>();
            completionItems.Add(new CompletionItem { Detail = "test",InsertText = "helo world",Label = "labl"});
            completionItems.Add(new CompletionItem { Detail = "test1", InsertText = "helo world1", Label = "lab2l" });

            return new CompletionList(completionItems);
            
        }

        protected override CompletionRegistrationOptions CreateRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
        {

            return new CompletionRegistrationOptions()
            {
                DocumentSelector = DocumentSelector.ForPattern(@"**/*.txt"),
                ResolveProvider = true,
                TriggerCharacters = new[] { "\n","\r" },
                AllCommitCharacters = new[] { "\n", "\r" }
                
            };
        }
    }
}
