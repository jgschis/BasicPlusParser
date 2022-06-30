using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser
{
    public static class SystemVariables
    {
        public static readonly HashSet<string> vars = new()
        {
            "@vm",
            "@svm",
            "@rm",
            "@tm",
            "@stm",
            "@fm",
            "@window",
            "@lower.case",
            "@upper.case"
        };
    }
}
