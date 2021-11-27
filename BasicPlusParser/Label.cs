using BasicPlusParser.Tokens;
using System.Collections.Generic;
using System.Linq;

namespace BasicPlusParser
{
    public class Label
    {
        // By storing where a label is (the list of statements it's in and its position in that list) we can jump to it.
        // Perhaps a better way to do this would be to use a linked list to store each statement, and then store a reference to
        // the label statement directly.
        public int Pos;
        public List<Statement> Statements;
        public Statement LabelStmt => Statements[Pos - 1];
        public IEnumerable<Statement> StatementsFollowingLabel => Statements.Skip(Pos);

        public Label (int pos, List<Statement> statements)
        {
            Pos = pos;
            Statements = statements;
        }
    }
}
