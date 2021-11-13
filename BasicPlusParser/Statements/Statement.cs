using System.Collections.Generic;
using System.Reflection;
using System;
using BasicPlusParser.Statements.Expressions;

namespace BasicPlusParser
{
    public abstract class Statement
    {
        public int LineNo;
      

        public virtual HashSet<string> GetAssignedVars()
        {
            HashSet<string> assignedVars = new();
            FieldInfo[] fields = this.GetType().GetFields();
            foreach (var field in fields)
            {
                object temp = field.GetValue(this);
                switch (temp)
                {

                    case IdExpression es when es.IdentifierType == IdentifierType.Assignment:
                        assignedVars.Add(es.Name);
                        break;

                    case Expression s:
                        assignedVars.UnionWith(s.GetAssignedVars());
                        break;
                    case List<Expression> ls:
                        foreach (var v in ls)
                        {
                            assignedVars.UnionWith(v.GetAssignedVars());
                        }
                        break;
                  
                }
            }
            return assignedVars;
        }
    

        public virtual HashSet<string> GetReferencedVars()
        {
            HashSet<string> referencedVars = new();
            FieldInfo[] fields = this.GetType().GetFields();
            foreach (var field in fields)
            {

                switch (field.GetValue(this))
                {

                    case IdExpression es when es.IdentifierType == IdentifierType.Reference:
                        referencedVars.Add(es.Name);
                        break;
    
                    case Expression e:
                        referencedVars.UnionWith(e.GetReferencedVars());
                        break;
                    case List<Expression> ls:
                        foreach (var v in ls)
                        {
                            referencedVars.UnionWith(v.GetAssignedVars());
                        }
                        break;
                }
            }
            return referencedVars;
        }

    }
}
