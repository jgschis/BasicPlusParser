﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasicPlusParser.Statements.Expressions;

namespace BasicPlusParser
{
    public class AssignmentStatement : Statement
    {
        public Expression Value;
        public IdExpression Variable;
    }
}
