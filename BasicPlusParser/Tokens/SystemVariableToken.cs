﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Tokens
{
    public class SystemVariableToken :  IdentifierToken
    {
       public override string LsClass { get; set; } = "type";
    }
}