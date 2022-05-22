using OmniSharp.Extensions.LanguageServer.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusLangServer
{
    public class TextDocument
    {
        public string Text;
        public DocumentUri Uri;
        public int? Version;
    }
}
