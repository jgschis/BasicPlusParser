﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasicPlusParser.Statements.Expressions;

namespace BasicPlusParser
{
    public class PlusAssignmentStatement : Statement
    {
        public IdExpression Variable;
        public Expression Value;
    }
}
