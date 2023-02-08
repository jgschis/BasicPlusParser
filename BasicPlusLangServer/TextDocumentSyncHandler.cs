using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using BasicPlusParser;
using BasicPlusParser.Analyser;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace BasicPlusLangServer
{
	class TextDocumentSyncHandler : TextDocumentSyncHandlerBase
    {
        readonly ILogger _logger;
        readonly ILanguageServerFacade _facade;
        readonly TextDocumentManager _documents;
        readonly OiClientFactory _oiClientFactory;

        public TextDocumentSyncHandler(ILogger<TextDocumentSyncHandler> logger,
            ILanguageServerFacade facade, TextDocumentManager textDocumentManager, OiClientFactory oiClientFactory)
        {
            _logger = logger;
            _facade = facade;
            _documents = textDocumentManager;
            _oiClientFactory = oiClientFactory;
        }

        public override Task<Unit> Handle(DidCloseTextDocumentParams parms, CancellationToken token){
            return Unit.Task;
        }

         public async override Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken token){
            var documentPath = request.TextDocument.Uri.ToString();
            var text = request.ContentChanges.FirstOrDefault()?.Text??"";

            var document = _documents.GetDocument(documentPath);
            if (document != null)
            {
                document.Text = text;
                Procedure proc = await ValidateDocument(document);
                document.Proc = proc;
                _documents.UpdateDocument(document);
            }

            return await Unit.Task;
        }


        public async override Task<Unit> Handle(DidOpenTextDocumentParams parms, CancellationToken token){

            var doc = new TextDocument { Text = parms.TextDocument.Text, Uri = parms.TextDocument.Uri, Version = parms.TextDocument.Version };
            Procedure procedure = await ValidateDocument(doc);
            _documents.UpdateDocument(new TextDocument { Text = parms.TextDocument.Text, Uri = parms.TextDocument.Uri, Version = parms.TextDocument.Version,Proc = procedure  });

            return await Unit.Task;
        }
        public override Task<Unit> Handle(DidSaveTextDocumentParams parms, CancellationToken token){
      
            return Unit.Task;
        }

        public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri documentUri){
            return new TextDocumentAttributes(documentUri, "bp");
        }

        protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(SynchronizationCapability syncCaps, ClientCapabilities clientCaps){
           return new TextDocumentSyncRegistrationOptions{
                Change = TextDocumentSyncKind.Full,
                Save = new SaveOptions() { IncludeText = true },
                DocumentSelector = DocumentSelector.ForPattern(@"**/*.bp"),
           };
        }


        public static Range GetRange(Procedure procedure, string textDocumentUri, string tokenFileName, Range defaultRange ) {
            if (textDocumentUri != tokenFileName) {
                if (procedure.SymbolTable.TryGetInsertSymbol(tokenFileName, out var symbol)) {
                    return new Range(symbol.Token.LineNo - 1, symbol.Token.StartCol, symbol.Token.EndLineNo - 1, symbol.Token.EndCol);
                }
            }
            return defaultRange;
        }

        async Task<Procedure> ValidateDocument(TextDocument textDocument)
        {
            var client = await _oiClientFactory.GetClient();

            string fileName = textDocument.Uri.ToString();
            string appName = Directory.GetParent(fileName)?.Name ?? throw new InvalidOperationException($"Failed to obtain application name from path: {fileName}");

            Parser parser = new Parser(textDocument.Text, fileName, client, appName);
            Procedure procedure = parser.Parse();

            UnreachableCodeAnalyser uca = new(procedure);
            uca.Analyse();
            UnassignedVariableAnalyser uva = new(procedure);
            uva.Analyse();

            List<Diagnostic> diagnoistics = new();
            foreach (var error in procedure.Errors.Errors)
            {
                Range defaultRange = new Range(error.LineNo - 1, error.StartCol, error.EndLineNo - 1, error.EndCol);

                Diagnostic diagnostic = new() {
                    Severity = error.PType switch { ParserDiagnosticType.Warning => DiagnosticSeverity.Warning, _ => DiagnosticSeverity.Error },
                    Range = GetRange(procedure, textDocument.Uri.ToString(), error.FileName, defaultRange),
                    Message = error.Message
                };

                diagnoistics.Add(diagnostic);
            }

            foreach (var stmt in uca.UnreachableStatements)

            {
                Diagnostic diagnostic = new()
                {
                    Severity = DiagnosticSeverity.Warning,
                    Range = GetRange(procedure,textDocument.Uri.ToString(),stmt.FileName,new Range(stmt.LineNo - 1, stmt.LineCol, stmt.LineNo - 1, stmt.EndCol)),
                    Message = "Unreachable code detected.",
                };
                diagnoistics.Add(diagnostic);
            }

            foreach (var result in uva.UnassignedVars.Values)
            {
                var token = result.Item1;

                Diagnostic diagnostic = new()
                {
                    Severity = DiagnosticSeverity.Warning,
                    Range = GetRange(procedure ,textDocument.Uri.ToString(),token.FileName, new Range(token.LineNo - 1, token.StartCol, token.LineNo - 1, token.EndCol)),
                    Message = $"The variable {token.Text} is not definitively assigned.",
                };
                diagnoistics.Add(diagnostic);
            }


            foreach (var label in uca.UnreachableLabels)
            {
                Diagnostic diagnostic = new()
                {
                    Severity = DiagnosticSeverity.Warning,
                    Range = GetRange(procedure, textDocument.Uri.ToString(), label.FileName, new Range(label.LineNo - 1, label.LineCol, label.LineNo - 1, label.EndCol)),
                    Message = "Unreachable label detected.",
                };
                diagnoistics.Add(diagnostic);
            }

            foreach (var label in uca.LabelsReachedViaFallThRough)
            {
                Diagnostic diagnostic = new()
                {
                    Severity = DiagnosticSeverity.Warning,
                    Range = GetRange(procedure,textDocument.Uri.ToString(),label.FileName, new Range(label.LineNo - 1, label.LineCol, label.LineNo - 1, label.EndCol)),
                    Message = "Label reached via fallthrough. Is this intentional?",
                };
                diagnoistics.Add(diagnostic);
            }


            _facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
            {
                Diagnostics = new Container<Diagnostic>(diagnoistics.ToArray()),
                Uri = textDocument.Uri,
                Version = textDocument.Version
            });

            return procedure;
        }
    }
}