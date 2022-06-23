using BasicPlusParser.Tokens;


namespace BasicPlusParser.Statements.Expressions
{
    public class IdExpression : Expression
    {
        public Token Token;
        public string Name
        {
            set { NameRaw = value; }
            get
            {
                return Token.Text.ToLower();
            }
        }
        public string NameRaw;


        public IdentifierType IdentifierType;

        public IdExpression(Token token, IdentifierType identifierType = IdentifierType.Assignment)
        {
            Token = token;
            IdentifierType = identifierType;
            Name = token.Text;
        }

    }
}
