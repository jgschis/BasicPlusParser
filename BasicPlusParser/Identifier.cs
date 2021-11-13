
namespace BasicPlusParser
{
    public class Identifier
    {
        public string Name;
        public IdentifierType IdentifierType;

        public Identifier(string name, IdentifierType identifierType = IdentifierType.Assignment)
        {
            Name = name;
            IdentifierType = identifierType;
       }
    }
}
