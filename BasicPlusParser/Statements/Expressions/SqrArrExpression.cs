﻿using BasicPlusParser.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Statements.Expressions
{
    class SqrArrExpression : Expression
    {
        public List<Expression> Indexes;
        public Expression Source;
    }
}
