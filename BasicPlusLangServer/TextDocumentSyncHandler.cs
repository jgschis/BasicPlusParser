using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using MediatR;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using BasicPlusParser;

namespace BasicPlusLangServer
{
	class TextDocumentSyncHandler : TextDocumentSyncHandlerBase
    {
        readonly ILogger _logger;
        readonly ILanguageServerFacade _facade;
        readonly TextDocumentManager _documents;

        public TextDocumentSyncHandler(ILogger<TextDocumentSyncHandler> logger,
            ILanguageServerFacade facade, TextDocumentManager textDocumentManager)
        {
            _logger = logger;
            _facade = facade;
            _documents = textDocumentManager;
        }

        public override Task<Unit> Handle(DidCloseTextDocumentParams parms, CancellationToken token){
            return Unit.Task;
        }

         public override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken token){

            Debugger.Break();
      
            var documentPath = request.TextDocument.Uri.ToString();
            var text = request.ContentChanges.FirstOrDefault()?.Text??"";

            var document = _documents.GetDocument(documentPath);
            if (document != null)
            {
                document.Text = text;
                _documents.UpdateDocument(document);
                ValidateDocument(document);
            }
            
            return Unit.Task;
        }


        public override Task<Unit> Handle(DidOpenTextDocumentParams parms, CancellationToken token){

            var doc = new TextDocument { Text = parms.TextDocument.Text, Uri = parms.TextDocument.Uri, Version = parms.TextDocument.Version };
            _documents.UpdateDocument(new TextDocument { Text= parms.TextDocument.Text, Uri = parms.TextDocument.Uri,Version = parms.TextDocument.Version});
            ValidateDocument(doc);

            return Unit.Task;
        }
        public override Task<Unit> Handle(DidSaveTextDocumentParams parms, CancellationToken token){
           
            return Unit.Task;
        }

        public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri documentUri){
            _logger.LogInformation("GetTextDocumentAttributes completed");
            return new TextDocumentAttributes(documentUri, "txt");
        }

        protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(SynchronizationCapability syncCaps, ClientCapabilities clientCaps){
           _logger.LogInformation("CreateRegistrationOptions completed");
           return new TextDocumentSyncRegistrationOptions{
                Change = TextDocumentSyncKind.Full,
                Save = new SaveOptions() { IncludeText = true },
                DocumentSelector = DocumentSelector.ForPattern(@"**/*.txt"),
           };
        }

        void ValidateDocument(TextDocument textDocument)
        {
            Parser parser = new Parser(textDocument.Text);
            OiProgram program = parser.Parse();

            List<Diagnostic> diagnoistics = new();
            foreach (var error in program.Errors.Errors)
            {
                Diagnostic diagnostic = new()
                {
                    Severity = DiagnosticSeverity.Error,
                    Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(error.LineNo - 1, error.StartCol, error.LineNo - 1, error.EndCol),
                    Message = error.Message,
                    Source = "ex",
                    Code = "a"
                };
                diagnoistics.Add(diagnostic);
            }
            _facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
            {
                Diagnostics = new Container<Diagnostic>(diagnoistics.ToArray()),
                Uri = textDocument.Uri,
                Version = textDocument.Version
            });
        }
    }
}