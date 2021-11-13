using BasicPlusParser.Statements.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicPlusParser.Statements
{
    public class DeclareStatement : Statement
    {
        public List<IdExpression> Functions = new List<IdExpression>();
        public ProgramType PType;
    }
}
