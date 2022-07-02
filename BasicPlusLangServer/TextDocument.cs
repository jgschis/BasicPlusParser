using BasicPlusParser;
using OmniSharp.Extensions.LanguageServer.Protocol;


namespace BasicPlusLangServer
{
    public class TextDocument
    {
        public string Text;
        public DocumentUri Uri;
        public int? Version;
        public Procedure Proc;
    }
}
