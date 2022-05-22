using BasicPlusParser;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Diagnostics;

namespace BasicPlusLangServer
{
    internal class SemanticTokenHandler : SemanticTokensHandlerBase
    {
        TextDocumentManager _documentManager;

        public SemanticTokenHandler(TextDocumentManager documentManager)
        {
            _documentManager = documentManager;
        }

        protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(SemanticTokensCapability capability, ClientCapabilities clientCapabilities)
        {
            Debugger.Break();
            return new SemanticTokensRegistrationOptions
            {
                DocumentSelector = DocumentSelector.ForPattern(@"**/*.txt"),
                Legend = new SemanticTokensLegend
                {
                    TokenModifiers = capability.TokenModifiers,
                    TokenTypes = capability.TokenTypes
                },
                Full = new SemanticTokensCapabilityRequestFull
                {
                    Delta = true
                },
                Range = true
            };
        }

        protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(ITextDocumentIdentifierParams @params, CancellationToken cancellationToken)
        {
            return Task.FromResult(new SemanticTokensDocument(RegistrationOptions.Legend));
        }

        protected override Task Tokenize(SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier, CancellationToken cancellationToken)
        {

            Debugger.Break();
            var doc = _documentManager.GetDocument(identifier.TextDocument.Uri.ToString());
            if (doc != null)
            {
                var tokenizer = new Tokenizer(doc.Text, opts:new TokenizerOptions { IncludeComments = true,  IncludeEofToken = false});
                var tokens = tokenizer.Tokenise();

                foreach (var token in tokens)
                {
                    for (int i = token.LineNo; i <= token.EndLineNo; i++)
                    {

                        int startCol = token.StartCol;
                        int endCol = token.EndCol;

                        if (i > token.LineNo)
                        {
                            startCol = 0;
                        }

                        if (i < token.EndLineNo)
                        {
                            endCol = int.MaxValue;
                        }

                        builder.Push(new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(i-1, startCol, i-1, endCol), token.LsClass);

                    }

                }
            }

            return Unit.Task;
        }
    }
}
