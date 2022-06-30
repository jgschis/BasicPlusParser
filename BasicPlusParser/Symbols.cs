using BasicPlusParser.Statements.Expressions;
using BasicPlusParser.Tokens;
using System.Collections.Generic;

namespace BasicPlusParser
{
    public class Symbols
    {

        public Dictionary<string, Label> Labels = new();
        public List<Token> LabelReferences = new();
        public List<Token> ProcedureParameters = new();

        Dictionary<string, Symbol> _symbols = new();
        public Dictionary<string, Symbol> SymbolIndex = new();

        bool AddSymbol(Symbol symbol)
        {
            UpdateIndex(symbol);
            return _symbols.TryAdd(GetSymbolKey(symbol.Kind,symbol.Token), symbol);
        }

        string GetSymbolKey(SymbolKind kind, Token token)
        {
            return GetSymbolKey(kind, token.Text);
        }

        string GetSymbolKey(SymbolKind kind, string name)
        {
            return $"{kind}.{name.ToLower()}";
        }

        void UpdateIndex(Symbol symbol)
        {
            UpdateIndex(symbol.Token.LineNo, symbol.Token.StartCol, symbol);
        }

        void UpdateIndex(int lineNo,int lineCol, Symbol symbol)
        {
            string key = $"{lineNo}.{lineCol}";
            SymbolIndex.TryAdd(key, symbol);
        }

        public void AddFunctionDeclaration(Token token)
        {
            Symbol symbol = new(token, SymbolKind.Function);
            AddSymbol(symbol);
        }

        public void AddSubroutineDeclaation(Token token)
        {
            Symbol symbol = new(token, SymbolKind.Subroutine);
            AddSymbol(symbol);
        }

        public void AddInsert(Token token)
        {
            Symbol symbol = new(token, SymbolKind.Insert);
            AddSymbol(symbol);
        }

        public void AddEquateDeclaration(Token token, Expression value)
        {
            AddSymbol(new(token, SymbolKind.Equate, value));
        }

        public void AddLabelDeclaration(Token token)
        {
            if (!_symbols.TryGetValue(GetSymbolKey(SymbolKind.Label, token), out Symbol symbol))
            {
                symbol = new Symbol(token, SymbolKind.Label);
                symbol.LabelDeclared = true;
                AddSymbol(symbol);
            }
            else
            {
                symbol.LabelDeclared = true;
                UpdateIndex(token.LineNo, token.StartCol, symbol);
            }
        }

        public void AddCommonLabel(Token token)
        {
            AddSymbol(new(token, SymbolKind.CommonLabel));
        }

        public void AddMatrixDeclaration(Token token, Expression cols, Expression rows, VariableScope scope = VariableScope.Local)
        {
            AddSymbol(new(token, SymbolKind.Variable,cols,rows,scope));
        }
        public void AddCommonDeclaration(Token token)
        {
            AddSymbol(new(token, SymbolKind.Variable, scope: VariableScope.Common));
        }

        public void AddCommonDeclaration(Token token, Expression col, Expression row)
        {
            AddSymbol(new(token, SymbolKind.Variable,col,row, VariableScope.Common));
        }

        public void AddSubroutineReference(Token token)
        {
            if (_symbols.TryGetValue(GetSymbolKey(SymbolKind.Subroutine, token), out Symbol symbol))
            {
                UpdateIndex(token.LineNo, token.StartCol, symbol);
            }
        }

        public void AddFunctionReference(Token token)
        {
            if (_symbols.TryGetValue(GetSymbolKey(SymbolKind.Function,token),out Symbol symbol))
            {
                UpdateIndex(token.LineNo,token.StartCol,symbol);
            }
        }

        public void AddLabelReference(Token token)
        {
            if (!_symbols.TryGetValue(GetSymbolKey(SymbolKind.Label, token), out Symbol symbol))
            {
                symbol = new Symbol(token, SymbolKind.Label);
                AddSymbol(symbol);
            }
            else { 
                UpdateIndex(token.LineNo, token.StartCol, symbol);
            }

            LabelReferences.Add(token);
        }

        public bool IsMatrix(Token token)
        {
            if (_symbols.TryGetValue(GetSymbolKey(SymbolKind.Variable, token),out Symbol symbol)) {
                return symbol.Type == VariableType.Matrix;

            } else
            {
                return false;
            }
        }

        public bool IsFunctionDeclared(Token token)
        {
            return _symbols.ContainsKey(GetSymbolKey(SymbolKind.Function, token));
        }

        public bool IsSubroutineDeclared(Token token)
        {
            return _symbols.ContainsKey(GetSymbolKey(SymbolKind.Subroutine, token));
        }

        public bool IsLabelDeclared(Token token)
        {
            if (_symbols.TryGetValue(GetSymbolKey(SymbolKind.Label,token),out Symbol symbol))
            {
                return symbol.LabelDeclared;
            }
            return false;
        }

        public bool ContainsEquateOrVaraible(Token token)
        {
            return  _symbols.ContainsKey(GetSymbolKey(SymbolKind.Equate, token)) ||
                    _symbols.ContainsKey(GetSymbolKey(SymbolKind.Variable, token));
        }

        public void AddVariableReference(Token token, VariableScope scope = VariableScope.Local)
        {
            Symbol symbol;
            bool found =
                _symbols.TryGetValue(GetSymbolKey(SymbolKind.Equate, token), out symbol) ||
                _symbols.TryGetValue(GetSymbolKey(SymbolKind.Variable, token), out symbol);


            if (found)
            {
                UpdateIndex(token.LineNo,token.StartCol,symbol);
            } else
            {
                if (token is SystemVariableToken)
                {
                    scope = VariableScope.System;
                }
                AddSymbol(new Symbol(token, SymbolKind.Variable, scope: scope));
            }
        }

        public bool IsCommonBlockNameDefined(Token token)
        {
            return _symbols.ContainsKey(GetSymbolKey(SymbolKind.CommonLabel, token));
        }

        public  void AddParameter(Token token, bool isMatrix = false)
        {
            Symbol symbol;
            if (isMatrix)
            {
                symbol = new Symbol(token, SymbolKind.Variable, null, null, VariableScope.Parameter);
            } else
            {
                symbol = new Symbol(token, SymbolKind.Variable, VariableScope.Parameter);
            }
            AddSymbol(symbol);
            ProcedureParameters.Add(token);
        }
    }
}
