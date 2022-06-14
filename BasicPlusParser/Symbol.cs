using BasicPlusParser.Statements.Expressions;
using BasicPlusParser.Tokens;

namespace BasicPlusParser
{
    public enum SymbolKind
    {
        Function,
        Subroutine,
        Insert,
        Label,
        Variable,
        Equate,
        CommonLabel
    }

    public enum VariableType
    {
        Matrix,
        Dynamic
    }

    public enum VariableScope
    {
        Parameter,
        Local,
        Common,
        System
    }

    public class Symbol
    {
        public Token Token;
        public SymbolKind Kind;
        public VariableType? Type;
        public VariableScope? Scope;
        public Expression MatrixCols;
        public Expression MatrixRows;
        public readonly Expression EquateValue;
        public bool LabelDeclared = false;

        public Symbol(Token token, SymbolKind kind)
        {
            Kind = kind;
            Token = token;
        }

        public Symbol(Token token, SymbolKind kind, Expression equateValue) : this(token, kind)
        {
            EquateValue = equateValue;
        }

        public Symbol(Token token, SymbolKind kind, Expression cols, Expression rows, VariableScope scope) : this(token,kind)
        {
            MatrixCols = cols;
            MatrixRows = rows;
            Type = VariableType.Matrix;
            Scope = scope;
        }

        public Symbol(Token token, SymbolKind kind, VariableScope scope) : this(token, kind)
        {
            Type = VariableType.Dynamic;
            Scope = scope;
        }
    }
}
