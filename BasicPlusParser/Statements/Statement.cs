using System.Collections.Generic;
using System.Reflection;
using System;
using BasicPlusParser.Statements.Expressions;
using BasicPlusParser.Tokens;

namespace BasicPlusParser
{
    public abstract class Statement
    {
        public int LineNo;
        public int LineCol;
        public int EndCol;
      
        public virtual HashSet<Token> GetAssignedVars()
        {
            HashSet<Token> assignedVars = new(new TokenEqualityComparer());

            if (this is IdExpression es && es.IdentifierType == IdentifierType.Assignment)
            {
                assignedVars.Add(es.Token);
            }
            else
            {
                FieldInfo[] fields = this.GetType().GetFields();
                foreach (var field in fields)
                {
                    object temp = field.GetValue(this);
                    switch (temp)
                    {
                        case Statement s:
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
            }

            return assignedVars;
        }
    

        public virtual HashSet<Token> GetReferencedVars()
        {
            HashSet<Token> referencedVars = new(new TokenEqualityComparer());

            if (this is IdExpression es && es.IdentifierType == IdentifierType.Reference)
            {
                referencedVars.Add(es.Token);
            }
            else
            {
                FieldInfo[] fields = this.GetType().GetFields();
                foreach (var field in fields)
                {
                    switch (field.GetValue(this))
                    {
                        case Statement e:
                            referencedVars.UnionWith(e.GetReferencedVars());
                            break;
                        case List<Expression> ls:
                            foreach (var v in ls)
                            {
                                referencedVars.UnionWith(v.GetReferencedVars());
                            }
                            break;
                    }
                }
            }
            return referencedVars;
        }
    }
}
