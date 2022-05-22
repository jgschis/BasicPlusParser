using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Concurrent;

namespace BasicPlusLangServer
{
    public class TextDocumentManager
    {
        ConcurrentDictionary<string, TextDocument> _documents = new();

        public void UpdateDocument(TextDocument textDocument)
        {
            _documents.AddOrUpdate(textDocument.Uri.ToString(), textDocument, (k, v) => textDocument);
        }

        public TextDocument GetDocument(string documentPath)
        {
            return _documents.TryGetValue(documentPath, out var textDocument) ? textDocument : null;
        }
    }
}



