﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Statements
{
    public class OnGosubStatement : Statement
    {
        public Expression Index;
        public List<string> Labels;
    }
}
