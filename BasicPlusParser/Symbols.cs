﻿using BasicPlusParser.Statements.Expressions;
using BasicPlusParser.Tokens;
using System.Collections.Generic;

namespace BasicPlusParser
{
    public class Symbols
    {
        public Dictionary<string, Label> Labels = new();
        public List<Token> LabelReferences = new();
        public List<Token> ProcedureParameters = new();

        public Dictionary<string, Symbol> _Symbols { get; private set; } = new();

        public Dictionary<string, SymbolReference> SymbolIndex = new();

        bool AddSymbol(Symbol symbol)
        {
            UpdateIndex(symbol.Token,symbol);
            return _Symbols.TryAdd(GetSymbolKey(symbol.Kind,symbol.Token), symbol);
        }

        string GetSymbolKey(SymbolKind kind, Token token)
        {
            return GetSymbolKey(kind, token.Text);
        }

        string GetSymbolKey(SymbolKind kind, string name)
        {
            return $"{kind}.{name.ToLower()}";
        }

        void UpdateIndex(Token token, Symbol symbol)
        {
            SymbolReference reference = new SymbolReference(symbol, token);
            string key = $"{token.LineNo}.{token.StartCol}.{token.FileName}";
            SymbolIndex.TryAdd(key, reference);
            symbol.References.Add(reference);
        }

        public void AddFunctionDeclaration(Token token)
        {
            Symbol symbol = new(token, SymbolKind.Function);
            AddSymbol(symbol);
        }

        public void AddSubroutineDeclaration(Token token)
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
            if (!_Symbols.TryGetValue(GetSymbolKey(SymbolKind.Label, token), out Symbol symbol))
            {
                symbol = new Symbol(token, SymbolKind.Label);
                symbol.LabelDeclared = true;
                AddSymbol(symbol);
            }
            else
            {
                symbol.LabelDeclared = true;
                symbol.Token = token;
                UpdateIndex(token, symbol);
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
            if (_Symbols.TryGetValue(GetSymbolKey(SymbolKind.Subroutine, token), out Symbol symbol))
            {
                UpdateIndex(token, symbol);
            }
        }

        public void AddFunctionReference(Token token)
        {
            if (_Symbols.TryGetValue(GetSymbolKey(SymbolKind.Function,token),out Symbol symbol))
            {
                UpdateIndex(token, symbol);
            }
        }

        public void AddLabelReference(Token token)
        {
            if (!_Symbols.TryGetValue(GetSymbolKey(SymbolKind.Label, token), out Symbol symbol))
            {
                symbol = new Symbol(token, SymbolKind.Label);
                AddSymbol(symbol);
            }
            else { 
                UpdateIndex(token, symbol);
            }

            LabelReferences.Add(token);
        }

        public bool IsMatrix(Token token)
        {
            if (_Symbols.TryGetValue(GetSymbolKey(SymbolKind.Variable, token),out Symbol symbol)) {
                return symbol.Type == VariableType.Matrix;

            } else
            {
                return false;
            }
        }

        public bool IsFunctionDeclared(Token token)
        {
            return _Symbols.ContainsKey(GetSymbolKey(SymbolKind.Function, token));
        }

        public bool IsSubroutineDeclared(Token token)
        {
            return _Symbols.ContainsKey(GetSymbolKey(SymbolKind.Subroutine, token));
        }

        public bool IsLabelDeclared(Token token)
        {
            if (_Symbols.TryGetValue(GetSymbolKey(SymbolKind.Label,token),out Symbol symbol))
            {
                return symbol.LabelDeclared;
            }
            return false;
        }

         public bool IsMatrixDeclared(Token token){
            if (_Symbols.ContainsKey(GetSymbolKey(SymbolKind.Equate, token))) {
                return true;
            }

            if (!_Symbols.TryGetValue(GetSymbolKey(SymbolKind.Variable, token), out Symbol symbol)){
                return false;
            }

            // If the symbol is defined as a matrix parameter, then that's OK.
            if (symbol.Scope == VariableScope.Parameter && symbol.Type == VariableType.Matrix) {
                return false;
            }

            return true;
        }

        public bool IsCommonBlockNameDefined(Token token)
        {
            return _Symbols.ContainsKey(GetSymbolKey(SymbolKind.CommonLabel, token));
        }

        public bool IsParameterDefined(Token token)
        {
            if (_Symbols.TryGetValue(GetSymbolKey(SymbolKind.Variable, token), out Symbol symbol)){
                return symbol.Scope == VariableScope.Parameter;
            } else
            {
                return false;
            }
        }

        public bool ContainsEquateOrVaraible(Token token)
        {
            return _Symbols.ContainsKey(GetSymbolKey(SymbolKind.Equate, token)) ||
                    _Symbols.ContainsKey(GetSymbolKey(SymbolKind.Variable, token));
        }

       
        public void AddVariableReference(Token token, VariableScope scope = VariableScope.Local)
        {
            Symbol symbol;
            bool found =
                _Symbols.TryGetValue(GetSymbolKey(SymbolKind.Equate, token), out symbol) ||
                _Symbols.TryGetValue(GetSymbolKey(SymbolKind.Variable, token), out symbol);


            if (found)
            {
                UpdateIndex(token, symbol);
            } else
            {
                if (token is SystemVariableToken)
                {
                    scope = VariableScope.System;
                }
                AddSymbol(new Symbol(token, SymbolKind.Variable, scope: scope));
            }
        }

        public void AddParameter(Token token, bool isMatrix = false)
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

        public bool IsCommonVariable(Token token) {
            if (!_Symbols.TryGetValue(GetSymbolKey(SymbolKind.Variable, token), out Symbol symbol)){
                return false;
            }

            return symbol.Scope == VariableScope.Common;
        }

        public bool TryGetInsertSymbol(string insertName, out Symbol symbol) {
            return _Symbols.TryGetValue(GetSymbolKey(SymbolKind.Insert, insertName),out symbol);
        }
    }
}
