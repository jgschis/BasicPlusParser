﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser
{

    public enum ParserDiagnosticType
    {
        Warning,
        Error,
        Info
    }


    public class ParseError
    {
        public string Message;
        public int LineNo;
        public int StartCol;
        public int EndCol;
        public int EndLineNo;
        public ParserDiagnosticType PType;
        public string FileName;
    }
}
