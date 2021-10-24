﻿using BasicPlusParser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser
{
    public class MatchesExpression : Expression
    {
        public MatchesExpression(Token token, params Expression[] children) : base(token, children) { }
    }
}
