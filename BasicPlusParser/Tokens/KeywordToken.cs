﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Tokens
{
    public class KeywordToken :IdentifierToken
    {
        public override string LsClass { get; set; } = "keyword";
    }
}
