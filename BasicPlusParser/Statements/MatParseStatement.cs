﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Statements
{
    public class MatParseStatement : Statement
    {
        public string Variable;
        public string Matrix;
        public Expression Delim;
    }
}
