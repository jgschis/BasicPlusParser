using BasicPlusParser;
using BasicPlusParser.Tokens;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Linq;

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
            var doc = _documentManager.GetDocument(identifier.TextDocument.Uri.ToString());
            if (doc != null)
            {

                // Unfortunately the highligting of tokens must be applied in the order in which the tokens appear.
                // That means we need to merge the two token lists together.
                int i = 0;
                int j = 0;
                while (true)
                {
                    if (i < doc.Proc.Tokens.Count && j < doc.Proc.CommentTokens.Count)
                    {
                        if (doc.Proc.Tokens[i].Pos < doc.Proc.CommentTokens[j].Pos)
                        {
                            ApplyHighlightingToToken(doc.Proc.Tokens[i++], builder);
                        }
                        else
                        {
                            ApplyHighlightingToToken(doc.Proc.CommentTokens[j++], builder);
                        }
                    } 
                    else if (i < doc.Proc.Tokens.Count)
                    {
                        ApplyHighlightingToToken(doc.Proc.Tokens[i++], builder);
                    }
                    else if (j < doc.Proc.CommentTokens.Count)
                    {
                        ApplyHighlightingToToken(doc.Proc.CommentTokens[j++], builder);
                    }
                    else
                    {
                        break;
                    }
                }
               
            }
            return Unit.Task;
        }


        void ApplyHighlightingToToken(Token token, SemanticTokensBuilder builder)
        {

            if (token is EofToken) return;

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

                builder.Push(new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(i - 1, startCol, i - 1, endCol), token.LsClass);
            }
        }

    }
}
