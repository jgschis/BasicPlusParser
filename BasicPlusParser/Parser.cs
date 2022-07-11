using System;
using System.Collections.Generic;
using System.Linq;
using BasicPlusParser.Tokens;
using BasicPlusParser.Statements;
using BasicPlusParser.Statements.Expressions;

namespace BasicPlusParser
{
    public class Parser
    {
        readonly string _text;

        int _nextTokenIndex = 0;
        List<Token> _tokens;
        List<Token> _commentTokens;
        Token _nextToken => _nextTokenIndex < _tokens.Count ? _tokens[_nextTokenIndex] : _tokens.Last();
        Token _prevToken => _nextTokenIndex > 0 ? _tokens[_nextTokenIndex - 1] : null;

        ParseErrors _parseErrors = new();
        List<Region> _regions = new();
        Symbols _symbolTable = new();

        public Parser(string text)
        {
            _text = text;
        }

        public Procedure Parse()
        {
            if (_text == "")
            {
                // If there is no source code, return an "empty" procedure.
                return new Procedure();
            }

            Tokenizer tokenizer = new(_text);
            TokenizerOutput tokenizerOutput = tokenizer.Tokenise();
            _tokens = tokenizerOutput.Tokens;
            _commentTokens = tokenizerOutput.CommentTokens;
            _parseErrors.Errors.AddRange(tokenizerOutput.TokenErrors.Errors);

            Procedure procedure = ParseProcedureDeclaration();
            procedure.Statements = ParseStmts(() => IsAtEnd());
            CheckIfJumpLabelsAreDefined();
            procedure.Errors = _parseErrors;
            procedure.SymbolTable = _symbolTable;
            procedure.Regions = _regions;
            procedure.Tokens = _tokens;
            procedure.CommentTokens = _commentTokens;
            return procedure;
        }

        Procedure ParseProcedureDeclaration()
        {
            ProcedureType procedureType;
            ConsumeToken(typeof(CompileToken), optional: true);

            if (!NextTokenIs(out Token procedureTypeToken, typeof(SubroutineToken), typeof(FunctionToken), typeof(InsertDeclarationToken))) {
                _parseErrors.ReportError(PeekNextToken(), "Procedure type missing. Must be either function, subroutine or insert.");
                // Default procedure  to subroutine so we can continue parsing for more errors...
                procedureType = ProcedureType.Subroutine;
            }
            else if (procedureTypeToken is FunctionToken)
            {
                procedureType = ProcedureType.Function;
            }
            else if (procedureTypeToken is SubroutineToken)
            {
                procedureType = ProcedureType.Subroutine;
            }
            else
            {
                procedureType = ProcedureType.Insert;
            }

            string procedureName;
            if (!NextTokenIs(out Token procedureNameToken, typeof(IdentifierToken))) {
                _parseErrors.ReportError(PeekNextToken(), "Procedure name missing or invalid.");
                procedureName = "";
            } else {
                procedureNameToken.LsClass = "function";
                procedureName = procedureNameToken.Text;
            }

            if (new List<string> {"function","subroutie","insert" }.Contains(procedureName.ToLower())){
                _parseErrors.ReportError(PeekNextToken(), "Procedure name is invalid.");
            }

            bool hasLeftParen = false;
            if (procedureType == ProcedureType.Function || procedureType == ProcedureType.Subroutine)
            {
                if (!NextTokenIs(typeof(LParenToken))){
                    _parseErrors.ReportError(PeekNextToken(), "Left parentheses is missing.");
                } else
                {
                    hasLeftParen = true;
                }

                if (PeekNextToken() is IdentifierToken || PeekNextToken() is MatToken)
                {
                    do
                    {
                        bool isMatrix =  NextTokenIs(typeof(MatToken));

                        if (NextTokenIs(out Token argNameToken,typeof(IdentifierToken))) {

                            if (_symbolTable.IsParameterDefined(argNameToken))
                            {
                                _parseErrors.ReportError(argNameToken, $"Paraneter {argNameToken.Text} has already been defined.");
                            }
                            else if (argNameToken is SystemVariableToken)
                            {
                                _parseErrors.ReportError(argNameToken, $"A system variable cannot be used as the name of a parameter.");
                            }
                            else
                            {
                                _symbolTable.AddParameter(argNameToken, isMatrix);
                            }
                        }
                        else
                        {
                            _parseErrors.ReportError(PeekNextToken(), "Parameter name missing or invalid.");
                        }

                    } while (NextTokenIs(typeof(CommaToken)));
                }

                if (!NextTokenIs(typeof(RParenToken))){
                    // No need to report missing right paren, if the left paren isn't present...
                    if (hasLeftParen) _parseErrors.ReportError(PeekNextToken(), "Right parentheses is missing.");
                }
            }

            if (!NextTokenIs(typeof(NewLineToken))){
                _parseErrors.ReportError(PeekNextToken(),"Procedure delaration must end in a new line.");
            }
            return new Procedure(procedureName, procedureType);
        }

        public List<Statement> ParseStmts(Func<bool> stop, bool inLoop = false)
        {
            return ParseStmts(_ => stop(), inLoop: inLoop);
        }

        public List<Statement> ParseStmts(Func<List<Statement>, bool> stop, bool inLoop = false)
        {
            List<Statement> statements = new();
            while (!stop(statements) && !IsAtEnd())
            {
                try
                {
                    int lineNo = GetLineNo();
                    int lineCol = GetLineCol();
                    Statement statement = ParseStmt(inLoop: inLoop);
                    statements.Add(statement);
                    AnnotateStmt(statements, lineNo, lineCol);
                    if (stop(statements)) break;
                    ConsumeStatementSeparator(statement);
                }
                catch (ParseException e)
                {
                    _parseErrors.ReportError(e.Token, e.Message);
                    Sync(stop, statements);
                }
            }
            return statements;
        }

        public Statement ParseStmt(bool inLoop = false)
        {
            Token token = GetNextToken();
            if (token is IdentifierToken)
            {
                if (IsMatrix(token))
                {
                    return ParseMatrixAssignmentStmt(token);
                }
                else if (NextTokenIs(typeof(EqualToken)))
                {
                    return ParseAssignmentStmt(token);
                }
                else if (NextTokenIs(typeof(LAngleBracketToken)))
                {
                    return ParseAngleAssignmentStmt(token);
                }
                else if (!token.DisallowFunction && NextTokenIs(typeof(LSqrBracketToken)))
                {
                    return ParseSquareBracketArrayAssignmentStmt(token);
                }
                else if (NextTokenIs(typeof(ColonToken)))
                {
                    return ParseInternalSubStmt(token);
                }
                else if (NextTokenIs(typeof(PlusEqualToken)))
                {
                    return ParsePlusAssignmentStmt(token);
                }
                else if (NextTokenIs(typeof(MinusEqualToken)))
                {
                    return ParseMinusAssignmentStmt(token);
                }
                else if (NextTokenIs(typeof(SlashEqualToken)))
                {
                    return ParseDivideAssignmentStmt(token);
                }
                else if (NextTokenIs(typeof(StarEqualToken)))
                {
                    return ParseMulAssignmentStmt(token);
                }
                else if (NextTokenIs(typeof(ColonEqualToken)))
                {
                    return ParseConcatAssignmentStmt(token);
                }
                else if (!token.DisallowFunction && NextTokenIs(typeof(LParenToken)))
                {
                    return ParseFunctionCallStmt(token);
                }
                else if (token is BeginToken)
                {
                    return ParseCaseStmt(token);
                }
                else if (token is ReturnToken)
                {
                    return ParseReturnStmt();
                }
                else if (token is ForToken)
                {
                    return ParseForNextStmt(token);
                }
                else if (token is LoopToken)
                {
                    return ParseLoopRepeatStmt(token);
                }
                else if (token is GosubToken)
                {
                    return ParseGosubStmt();
                }
                else if (token is EquToken)
                {
                    return ParseEquStatement(token);
                }
                else if (token is InsertToken)
                {
                    return ParseInsertStmt();
                }
                else if (token is DeclareToken)
                {
                    return ParseDeclareStmt();
                }
                else if (token is CallToken)
                {
                    return ParseCallStmt();
                }
                else if (token is RemoveToken)
                {
                    return ParseRemoveStmt();
                }
                else if (token is MatToken)
                {
                    return ParseMatStmt();
                }
                else if (token is SwapToken)
                {
                    return ParseSwapStmt();
                }
                else if (token is LocateToken)
                {
                    return ParseLocateStmt();
                }
                else if (token is NullToken)
                {
                    return ParseNullStmt();
                }
                else if (token is ConvertToken)
                {
                    return ParseConvertStmt();
                }
                else if (token is DebugToken)
                {
                    return ParseDebugStmt();
                }
                else if (token is OpenToken)
                {
                    return ParseOpenStmt();
                }
                else if (token is ReadToken)
                {
                    return ParseReadStmt();
                }
                else if (token is WriteToken)
                {
                    return ParseWriteStmt();
                }
                else if (token is DeleteToken)
                {
                    return ParseDeleteSmt();
                }
                else if (token is LockToken)
                {
                    return ParseLockStmt();
                }
                else if (token is UnlockToken)
                {
                    return ParseUnlockStmt();
                }
                else if (token is ClearSelectToken)
                {
                    return ParseClearSelectStmt();
                }
                else if (token is ReadNextToken)
                {
                    return ParseReadNextStmt();
                }
                else if (token is GoToToken)
                {
                    return ParseGoToStmt();
                }
                else if (token is TransferToken)
                {
                    return ParseTransferStmt();
                }
                else if (token is MatReadToken)
                {
                    return ParseMatReadStmt();
                }
                else if (token is MatWriteToken)
                {
                    return ParseMatWriteStmt();
                }
                else if (token is OsWriteToken)
                {
                    return ParseOsWriteStmt();
                }
                else if (token is OsReadToken)
                {
                    return ParseOsReadStmt();
                }
                else if (token is DimensionToken)
                {
                    return ParseDimStmt(token);
                }
                else if (token is CommonToken)
                {
                    return ParseCommonStmt();
                }
                else if (token is FreeCommonToken)
                {
                    return ParseFreeCommonStmt();
                }
                else if (token is InitRndToken)
                {
                    return ParseInitRndStmt();
                }
                else if (token is SelectToken)
                {
                    return ParseSelectStmt();
                }
                else if (token is FlushToken)
                {
                    return ParseFlushStmt();
                }
                else if (token is GarbageCollectToken)
                {
                    return ParseGarbageCollectStmt();
                }
                else if (token is ReadVToken)
                {
                    return ParseReadVStmt();
                }
                else if (token is ReadOToken)
                {
                    return ParseReadOStmt();
                }
                else if (token is MatParseToken)
                {
                    return ParseMatParseStmt();
                }
                else if (token is InitDirToken)
                {
                    return ParseInitDirStmt();
                }
                else if (token is WriteVToken)
                {
                    return ParseWriteVStmt();
                }
                else if (token is OnToken)
                {
                    return ParseOnGosubOrOnGotoStmt();
                }
                else if (token is OsOpenToken)
                {
                    return ParseOsOpenStmt();
                }
                else if (token is OsDeleteToken)
                {
                    return ParseOsDeleteStmt();
                }
                else if (token is OsCloseToken)
                {
                    return ParseOsCloseStmt();
                }
                else if (token is OsBWriteToken)
                {
                    return ParseOsBWriteStmt();
                }
                else if (token is OsBReadToken)
                {
                    return ParseOsBReadStmt();
                }
                else if (token is BRemoveToken)
                {
                    return ParseBRemoveStmt();
                }
                else if (token is AbortToken)
                {
                    return ParseAbortAllStmt();
                }
            }
            else if (token is PragmaToken)
            {
                return ParsePragmaStmt();
            }
            else if (token is EndToken)
            {
                return ParseEndStmt(token);
            }
            else if (token is IfToken)
            {
                return ParseIfStmt();
            }
            else if (token is WhileToken)
            {
                return ParseWhileStmt(token,inLoop);
            }
            else if (token is UntilToken)
            {
                return ParseUntilStmt(token,inLoop);
            }
            else if (token is SemiColonToken)
            {
                return new EmptyStatement();
            }
            else if (token is NewLineToken)
            {
                return new EmptyStatement();
            }
           
            throw Error(token, $"{token.Text} is not a valid statement.");
        }

        void ConsumeStatementSeparator(Statement statement)
        {
            if (PeekNextToken() is EofToken)
            {
                return;
            }

            if (NextTokenIs(typeof(NewLineToken)))
            {
                return;
            }

            if (NextTokenIs(typeof(SemiColonToken)))
            {
                ConsumeToken(typeof(NewLineToken), optional:true);
                return;
            }

            // The internal sub and empty statements doesn't need a separator...
            if (statement is InternalSubStatement || statement is EmptyStatement) return;

            throw Error(_prevToken, "Semicolon or newline expected after statement.");
        }

        void AnnotateStmt(List<Statement> statements, int lineNo, int lineCol)
        {
            Statement statement = statements.Last();
            statement.LineNo = lineNo;
            statement.LineCol = lineCol;
            statement.EndCol = _prevToken.EndCol;
            switch (statement)
            {
                case
                    InternalSubStatement s:
                    _symbolTable.Labels.TryAdd(s.Label.Name, new Label(statements.Count, statements));
                    break;
            }
        }

        void Sync(Func<List<Statement>, bool> stop, List<Statement> statements)
        {
            // When an error occurs, lets start parsing again after the next new line or semicolon.
            while (!stop(statements) && _nextToken is not NewLineToken 
                                     && _nextToken is not SemiColonToken 
                                     && _nextToken is not EofToken 
                                     && _prevToken is not NewLineToken)
            {
                _nextTokenIndex += 1;
            }

            if (_nextToken is  NewLineToken || _nextToken is SemiColonToken)
            {
                _nextTokenIndex++;
            }
        }

        Statement ParseUnlockStmt()
        {
            Expression handle = null;
            Expression cursor = null;
            if (NextTokenIs(typeof(AllToken))){
                return new UnlockAllStatement();
            }

            if (NextTokenIs(typeof(CursorToken)))
            {
                cursor = ParseExpr();
            }
            else
            {
                handle = ParseExpr();
            }
            ConsumeToken(typeof(CommaToken));
            Expression key = ParseExpr();

            (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock(optional:true);
            return new UnlockStatement
            {
                Cursor = cursor,
                Else = elseBlock,
                Handle = handle,
                Key = key,
                Then = thenBlock,
            };
        }

        Statement ParseLockStmt()
        {
            Expression handle = null;
            Expression cursor = null;
            if (NextTokenIs(typeof(CursorToken)))
            {
                cursor = ParseExpr();
            }
            else
            {
                handle = ParseExpr();
            }
            ConsumeToken(typeof(CommaToken));
            Expression key = ParseExpr();

            (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock();
            return new LockStatement
            {
                Cursor = cursor,
                Else = elseBlock,
                Handle = handle,
                Key = key,
                Then = thenBlock,
            };
        }

        Statement ParseDeleteSmt()
        {
            Expression handle = null;
            Expression cursor = null;
            if (NextTokenIs(typeof(CursorToken)))
            {
                cursor = ParseExpr();
            }
            else
            {
                handle = ParseExpr();
            }
            ConsumeToken(typeof(CommaToken));
            Expression key = ParseExpr();

            (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock(optional:true);
            return new DeleteStatement
            {
                Cursor = cursor,
                Else = elseBlock,
                Handle = handle,
                Key = key,
                Then = thenBlock,
            };
        }

        (List<Statement> thenBlock, List<Statement> elseBlock) ParseThenElseBlock(bool optional = false)
        {
            List<Statement> elseBlock = new();
            List<Statement> thenBlock = new();
            bool hasThen = false;
            bool hasElse = false;
            bool isSingleLineThenElseStmt = false;

            if (NextTokenIs(out Token thenToken, typeof(ThenToken)))
            {
                hasThen = true;
                if (NextTokenIs(typeof(NewLineToken)))
                {
                    thenBlock = ParseStmts(_ =>PeekNextToken() is EndToken);

                    if (NextTokenIs(out Token endToken, typeof(EndToken)))
                    {
                        _regions.Add(new Region(thenToken.LineNo, thenToken.EndCol, endToken.LineNo, endToken.EndCol));
                    }
                    else
                    {
                        _parseErrors.ReportError(_prevToken, "end expected.");
                    }
                }
                else
                {
                    isSingleLineThenElseStmt = true;
                    thenBlock = ParseStmts(_ => PeekNextToken() is ElseToken || PeekNextToken() is NewLineToken || IsAtEnd() ||
                        (PeekNextToken() is SemiColonToken && PeekNextToken(1) is NewLineToken));
                    if (thenBlock.All(s => s is EmptyStatement) || thenBlock.Count == 0)
                    {
                        _parseErrors.ReportError(_nextToken, "Then block requires at least 1 statement.");
                    }
                }
            }

            if (NextTokenIs(out Token elseToken, typeof(ElseToken)))
            {
                hasElse = true;
                if (!isSingleLineThenElseStmt && NextTokenIs(typeof(NewLineToken)))
                {
                    elseBlock = ParseStmts(() => PeekNextToken() is EndToken);
                    if (NextTokenIs(out Token endToken, typeof(EndToken)))
                    {
                        _regions.Add(new Region(elseToken.LineNo, elseToken.EndCol, endToken.LineNo, endToken.EndCol));
                    }
                    else
                    {
                        _parseErrors.ReportError(PeekNextToken(), "end expected.");
                    }
                }
                else 
                {     
                    elseBlock = ParseStmts(() => PeekNextToken() is NewLineToken || IsAtEnd() || PeekNextToken() is ElseToken ||
                      (PeekNextToken() is SemiColonToken && PeekNextToken(1) is NewLineToken));

                    if (elseBlock.All(s => s is EmptyStatement) || elseBlock.Count == 0)
                    {
                        _parseErrors.ReportError(_nextToken, "Else block block requires at least 1 statement.");
                    }
                }
            }

            if (!(hasElse || hasThen) && optional == false)
            {
                throw Error(_prevToken, "Then or else block expected.");
            }

            return (thenBlock, elseBlock);
        }

        Statement ParseMatWriteStmt()
        {
            Expression expr = ParseExpr();
            ConsumeToken("Expected on or to", false, typeof(OnToken), typeof(ToToken));
            Expression handle = ParseExpr();
            ConsumeToken(typeof(CommaToken));
            Expression key = ParseExpr();

            (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock();
            return new MatWriteStatement
            {
                Else = elseBlock,
                Handle = handle,
                Key = key,
                Then = thenBlock,
                Expr = expr
            };
        }

        Statement ParseWriteStmt()
        {
            Expression expr = ParseExpr();
            ConsumeToken("Expected on or to", false, typeof(OnToken), typeof(ToToken));
            Expression handle = ParseExpr();
            ConsumeToken(typeof(CommaToken));
            Expression key = ParseExpr();

            (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock();
            return new WriteStatement
            {
                Else = elseBlock,
                Handle = handle,
                Key = key,
                Then = thenBlock,
                Expr = expr
            };
        }

        Statement ParseMatReadStmt()
        {
            Token var = ConsumeIdToken();
            ConsumeToken(typeof(FromToken));
            Expression handle = null;
            Expression cursor = null;
            if (NextTokenIs(typeof(CursorToken)))
            {
                cursor = ParseExpr();
            }
            else
            {
                handle = ParseExpr();
            }
            ConsumeToken(typeof(CommaToken));
            Expression key = ParseExpr();

            (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock();

            return new MatReadStatement
            {
                Cursor = cursor,
                Else = elseBlock,
                Handle = handle,
                Key = key,
                Then = thenBlock,
                Var = new IdExpression(var)
            };
        }

        Statement ParseReadStmt()
        {
            Token var = ConsumeIdToken();
            ConsumeToken(typeof(FromToken));
            Expression handle = null;
            Expression cursor = null;
            if (NextTokenIs(typeof(CursorToken))){
                cursor = ParseExpr();
            }
            else
            {
                handle = ParseExpr();
            }
            ConsumeToken(typeof(CommaToken));
            Expression key = ParseExpr();

            (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock();

            return new ReadStatement
            {
                Cursor = cursor,
                Else = elseBlock,
                Handle = handle,
                Key = key,
                Then = thenBlock,
                Variable = new IdExpression(var,IdentifierType.Assignment)
            };
        }

        AssignmentStatement ParseAssignmentStmt(Token token)
        {
            Expression expr = ParseExpr();
            _symbolTable.AddVariableReference(token);
            return new AssignmentStatement { Value = expr, Variable = new IdExpression(token,IdentifierType.Assignment) };
        }

        Statement ParseDeclareStmt()
        {
            List<IdExpression> functions = new();
            ProcedureType pType;
            Token token = ConsumeToken("Expected subroutine or function.", false, typeof(SubroutineToken), typeof(FunctionToken)); 
            if (token is SubroutineToken)
            {
                pType = ProcedureType.Subroutine;
            } 
            else 
            {
                pType = ProcedureType.Function;
            }

            do
            {
                Token func = ConsumeIdToken(addIdentifierToSymbolTable: false); ;
                func.LsClass = "function";
                functions.Add(new IdExpression(func, IdentifierType.Function));
                if (pType == ProcedureType.Subroutine)
                {
                    _symbolTable.AddSubroutineDeclaation(func);
                } else
                {
                    _symbolTable.AddFunctionDeclaration(func);
                }
            } while (NextTokenIs(typeof(CommaToken)));

            return new DeclareStatement
            {
                PType = pType,
                Functions = functions
            };
        }

        Statement ParseOpenStmt()
        {
            Expression table = ParseExpr();
            ConsumeToken(typeof(ToToken));
            Token handle = ConsumeIdToken();

            (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock();

            return new OpenStatement
            {
                Else = elseBlock,
                Handle = new IdExpression(handle),
                Table = table,
                Then = thenBlock
            };
        }

        Statement ParseDebugStmt()
        {
            return new DebugStatement();
        }

          Statement ParseNullStmt()
        {
            return new NullStatement();
        }

        Statement ParseGoToStmt()
        {
            Token label = ConsumeIdToken(addIdentifierToSymbolTable: false);
            label.LsClass = "label";
            GoToStatement stmt = new GoToStatement
            {
                Label = new IdExpression(label, IdentifierType.Label),
            };
            _symbolTable.AddLabelReference(label);
            return stmt;
        }

        Statement ParseLocateStmt()
        {
            bool isBy = false;
            Expression seq = null;

            Expression needle = ParseExpr();
            ConsumeToken(typeof(InToken));
            Expression haystack = ParseExpr();
            if (NextTokenIs(typeof(ByToken)))
            {
                isBy = true;
                seq = ParseExpr();
            }
            Expression delim = null;
            if (NextTokenIs(typeof(UsingToken)))
            {
                delim = ParseExpr();
            }
            ConsumeToken(typeof(SettingToken));
            Token pos = ConsumeIdToken();
            (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock();
            
            if (isBy)
            {
                return new LocateByStatement
                {
                    Needle = needle,
                    Haystack = haystack,
                    Delim = delim,
                    Else = elseBlock,
                    Pos = new IdExpression(pos),
                    Seq = seq,
                    Then = thenBlock
                };
            } else
            {
                return new LocateStatement
                {
                    Delim = delim,
                    Else = elseBlock,
                    Haystack = haystack,
                    Needle = needle,
                    Start = new IdExpression(pos),
                    Then = thenBlock
                };
            } 
        }

        Statement ParseOnGosubOrOnGotoStmt()
        {
            bool isGosub = false;

            Expression index = ParseExpr();

            Token jmpToken = ConsumeToken("Expected goto or gosub.", false, typeof(GosubToken), typeof(GoToToken));
            if (jmpToken is GosubToken)
            {
                isGosub = true;
            }

            List<IdExpression> labels = new();

            do
            {
                Token label = ConsumeIdToken(addIdentifierToSymbolTable:false);
                label.LsClass = "label";
                labels.Add(new IdExpression(label, IdentifierType.Label));
                _symbolTable.AddLabelReference(label);

            } while (NextTokenIs(typeof(CommaToken)));


            Statement stmt;
            if (isGosub)
            {
                stmt = new OnGosubStatement
                {
                    Index = index,
                    Labels = labels,
                };
            } else
            {
                stmt = new OnGotoStatement
                {
                    Index = index,
                    Labels = labels,
                };
            }
            return stmt;
        }

        Statement ParseIfStmt()
        {
            Expression condition = ParseExpr();
            (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock();
            return new IfStatement
            {
                Condition = condition,
                Then = thenBlock,
                Else = elseBlock
            };
        }

        Case ParseCase()
        {
            Expression cond = ParseExpr();
            ConsumeSemiColonsUntilEndOfLine();
            List<Statement> statements = ParseStmts(() => _nextToken is  CaseToken || _nextToken is EndToken || _nextToken is EofToken);
            return new Case
            {
                Condition = cond,
                Statements = statements
            };
        }

        Statement ParseCaseStmt(Token token)
        {
            ConsumeToken(typeof(CaseToken));
            ConsumeSemiColonsUntilEndOfLine();
            List<Case> cases = new();
            while (NextTokenIs(typeof(CaseToken)))
            {
                cases.Add(ParseCase());
            }

            ConsumeToken(typeof(EndToken));
            ConsumeToken(typeof(CaseToken));
            _regions.Add(new Region(token.LineNo,token.EndCol,_prevToken.EndLineNo,_prevToken.EndCol));

            return new CaseStmt
            {
                Cases = cases
            };
        }

        Statement ParseAngleAssignmentStmt(Token token)
        {
            List<Expression> indexes = new();
            do
            {
                indexes.Add(ParseExpr(inArray: true));
            }
            while (NextTokenIs(typeof(CommaToken)));

            if (indexes.Count > 3)
            {
                _parseErrors.ReportError(token, $"Array {token.Text} has more than 3 indexes.");
            }

            ConsumeToken(typeof(RAngleBracketToken));
            ConsumeToken(typeof(EqualToken));
            Expression expr = ParseExpr();
            _symbolTable.AddVariableReference(token);
            return new AngleArrayAssignmentStatement
            {
                Indexes = indexes,
                Variable = new IdExpression(token, IdentifierType.Reference),
                Value = expr
            };
        }

        Statement ParseWhileStmt(Token token, bool inLoop)
        {
            Expression cond = ParseExpr();
            ConsumeToken(typeof(DoToken),optional:true);

            if (!inLoop)
            {
                _parseErrors.ReportError(token, "The while statment can only be used in the top level of a loop.");
            }

            return new WhileStatement
            {
                Condition = cond
            };
        }

        Statement ParseUntilStmt(Token token, bool inLoop)
        {
            Expression cond = ParseExpr();
            ConsumeToken(typeof(DoToken),optional:true);

            if (!inLoop)
            {
                _parseErrors.ReportError(token, "The until statment can only be used at the top level of a loop.");
            }

            return new UntilStatement
            {
                Condition = cond
            };
        }

        Statement ParseEquStatement(Token token)
        {
            Token var = ConsumeIdToken(addIdentifierToSymbolTable:false);

            ConsumeToken(typeof(ToToken));
            Expression val = ParseExpr();
            
            if (var is SystemVariableToken)
            {
                _parseErrors.ReportError(var, $"A system variable cannot be the name of an equate.");

            }
            else if (_symbolTable.ContainsEquateOrVaraible(var))
            {
                _parseErrors.ReportError(var, $"The symbol {var.Text} has already been defined.");
            }
            else
            {
                _symbolTable.AddEquateDeclaration(var, val);
            }

           
            return new EquStatemnet
            {
                Variable = new IdExpression(var),
                Value = val
            };
        }

        Statement ParseFunctionCallStmt(Token token)
        {
            token.LsClass = "function";
            FuncExpression funcExpr = (FuncExpression) ParseFunc(token, mustReturnValue:false);
            return new FunctionCallStatement
            {
                Expr = funcExpr
            };
        }

        Statement ParseLoopRepeatStmt(Token token)
        {
            List<Statement> statements = new();
            ConsumeToken(typeof(NewLineToken), optional: true);
            statements = ParseStmts(() => PeekNextToken() is RepeatToken || IsAtEnd(), inLoop: true);
            if (NextTokenIs(out Token repeatToken, typeof(RepeatToken))){
                _regions.Add(new Region(token.LineNo, token.EndCol, repeatToken.LineNo, repeatToken.EndCol));
            } else
            {
                _parseErrors.ReportError(token, "repeat expected");
            }

            if (!statements.Any(s => s is UntilStatement || s is WhileStatement || s is ReturnStatement || s is GoToStatement)){
                _parseErrors.ReportError(token, "Infinite loop detected. Add a while or until statement.", ParserDiagnosticType.Warning);
            }

            return new LoopRepeatStatement
            {
                Statements = statements
            };
        }

        public Statement ParseMatStmt()
        {
            Token matrix = ConsumeIdToken();
            if (!IsMatrix(matrix))
            {
                _parseErrors.ReportError(matrix, $"The identifier {matrix.Text} must be dimensioned.");
            }

            Expression value = null;
            IdExpression otherMatrix = null;
            ConsumeToken(typeof(EqualToken));
            if (NextTokenIs(typeof(MatToken))) {

                Token otherMatrixToken = ConsumeIdToken();
                if (!IsMatrix(otherMatrixToken))
                {
                    _parseErrors.ReportError(otherMatrixToken, $"The identifier {otherMatrixToken.Text} must be dimensioned.");
                }
                otherMatrix = new IdExpression(otherMatrixToken, IdentifierType.Reference);
            }
            else
            {
                value = ParseExpr();
            }
           
            return new MatStatement
            {
                Variable = new IdExpression(matrix),
                Value = value,
                OtherMatrix = otherMatrix
            };
        }

        public Statement ParseForNextStmt(Token token)
        {
            List<Statement> statements = new();
            Token startVar = ConsumeIdToken();
            ConsumeToken(typeof(EqualToken));
            AssignmentStatement index = ParseAssignmentStmt(startVar);
            ConsumeToken(typeof(ToToken));
            Expression end = ParseExpr();
            Expression step = null;

            if (NextTokenIs(typeof(StepToken))) {
                step = ParseExpr();
            }

            ConsumeToken(typeof(NewLineToken), optional: true);
            statements = ParseStmts(() => PeekNextToken() is NextToken || IsAtEnd(), inLoop: true);

            if (NextTokenIs(typeof(NextToken)))
            {
                ConsumeIdToken(optional: true);

                _regions.Add(new Region(token.LineNo, token.EndCol, _prevToken.LineNo, _prevToken.EndCol));
            } else
            {
                _parseErrors.ReportError(token, "next expected.");
            }

            return new ForNextStatement
            {
                Start = index,
                Statements = statements,
                End = end,
                Step = step
            };
        }

        Statement ParseReadNextStmt()
        {
            Token value = null;
            Expression cursor = null;

            Token variable = ConsumeIdToken();

            if (NextTokenIs(typeof(CommaToken)))
            {
                value = ConsumeIdToken();
            }

            if (NextTokenIs( typeof(UsingToken)))
            {
                cursor = ParseExpr();
            }

            (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock();

            return new ReadNextStatement
            {
                Cursor = cursor,
                Value = value != null ? new IdExpression(value) : null,
                Variable = variable != null ? new IdExpression(variable) : null,
                Else = elseBlock,
                Then = thenBlock
            };

        }

        Statement ParseRemoveStmt()
        {
            Token var = ConsumeIdToken();
            ConsumeToken(typeof(FromToken));
            Expression from = ParseExpr();
            ConsumeToken(typeof(AtToken));
            Token pos = ConsumeIdToken();
            ConsumeToken(typeof(SettingToken));
            Token flag = ConsumeIdToken();
            return new RemoveStatement
            {
                Variable = new IdExpression(var),
                From = from,
                Pos = new IdExpression(pos),
                Flag = new IdExpression(flag)
            };

        }

        public Statement ParseSquareBracketArrayAssignmentStmt(Token token)
        {
            List<Expression> indexes = new();
            do
            {
                indexes.Add(ParseExpr());
            }
            while (NextTokenIs(typeof(CommaToken)));

            if (indexes.Count > 2)
            {
                _parseErrors.ReportError(token, "The square bracket operator ony allows for 2 indexes.");
            }

            ConsumeToken(typeof(RSqrBracketToken));
            ConsumeToken(typeof(EqualToken));
            Expression expr = ParseExpr();
            _symbolTable.AddVariableReference(token);
            return new SquareBracketArrayAssignmentStatement
            {
                Indexes = indexes,
                Variable = new IdExpression(token, IdentifierType.Reference),
                Value = expr
            };
        }

        Statement ParseReturnStmt()
        {
            Expression expr = ParseExpr(optional: true);
            return new ReturnStatement
            {
                Argument = expr
            };
        }

        public Statement ParseSwapStmt()
        {
            Expression oldVal = ParseExpr();
            ConsumeToken(typeof(WithToken));
            Expression newVal = ParseExpr();
            ConsumeToken(typeof(InToken));
            Token variable = ConsumeIdToken();
            return new SwapStatement
            {
                Variable = new IdExpression(variable),
                New = newVal,
                Old = oldVal
            };
        }

        Statement ParseInternalSubStmt(Token token)
        {
            // At first I tried to store all statements that were part of this "interanl subroutine" in a list
            // referenced by this statement, but that cannot work, as an internal subroutine can span multiple "blocks",
            // as the below code demonstrates.
            /*
                for i = 1 to 3
                    in_loop = 1
                    v:
                       next i
                       end_of_loop = 1
            */

            //ExpectStatementEnd();
            //List<Statement> statements = ParseStmts(x => x.Count > 0 &&  x.Last() is ReturnStatement 
            //     || PeekNextToken() is EofToken);   


            if (_symbolTable.IsLabelDeclared(token)){
                _parseErrors.ReportError(token, $"The label {token.Text} has already been defined.");
            }

            token.LsClass = "label";
            _symbolTable.AddLabelDeclaration(token);
            return new InternalSubStatement
            {
                Label = new IdExpression(token,IdentifierType.Label),
               // Statements = statements
            };
        }

        Statement ParseGosubStmt()
        {
            Token label = ConsumeIdToken(addIdentifierToSymbolTable:false);
            label.LsClass = "label";
            GosubStatement gosubStmt = new GosubStatement
            {
                Label = new IdExpression(label, IdentifierType.Label),
            };
            _symbolTable.AddLabelReference(label);
            return gosubStmt;
        }

        Statement ParsePlusAssignmentStmt(Token token)
        {
            Expression expr = ParseExpr();
            _symbolTable.AddVariableReference(token);
            return new PlusAssignmentStatement
            {
                Value = expr,
                Variable = new IdExpression(token, IdentifierType.Reference)
            };
        }

        Statement ParseInsertStmt()
        {
            Token insert = ConsumeIdToken(addIdentifierToSymbolTable: false);
            _symbolTable.AddInsert(insert);
            return new InsertStatement
            {
                Name = new IdExpression(insert, IdentifierType.Insert)
            };
        }

        Statement ParseMinusAssignmentStmt(Token token)
        {
            Expression expr = ParseExpr();
            _symbolTable.AddVariableReference(token);
            return new MinusAssignmentStatement
            {
                Value = expr,
                Variable = new IdExpression(token,IdentifierType.Reference)
            };
        }

        Statement ParseDivideAssignmentStmt(Token token)
        {
            Expression expr = ParseExpr();
            _symbolTable.AddVariableReference(token);
            return new DivideAssignmentStatement
            {
                Value = expr,
                Variable = new IdExpression(token, IdentifierType.Reference)
            };
        }
        Statement ParseMulAssignmentStmt(Token token)
        {
            Expression expr = ParseExpr();
            _symbolTable.AddVariableReference(token);
            return new MulAssignmentStatement
            {
                Value = expr,
                Name = new IdExpression(token,IdentifierType.Reference)
            };
        }

        Statement ParseConvertStmt()
        {
            Expression from = ParseExpr();
            ConsumeToken(typeof(ToToken));
            Expression to = ParseExpr();
            ConsumeToken(typeof(InToken));
            Expression inPart = ParseExpr();
           
            return new ConvertStatement
            {
                From = from,
                To = to,
                In = inPart
            };
        }

        Statement ParseConcatAssignmentStmt(Token token)
        {
            Expression expr = ParseExpr();
            _symbolTable.AddVariableReference(token);
            return new ConcatAssignmentStatement
            {
                Value = expr,
                Variable = new IdExpression(token, IdentifierType.Reference)
            };
        }

        Statement ParseClearSelectStmt()
        {
            Expression cursor = ParseExpr(optional: true);
            return new ClearSelectStatement
            {
                Cursor = cursor
            };
        }

        Statement ParseCallStmt()
        {
            FuncExpression funcExpr;
            bool useAtOperator = NextTokenIs(typeof(AtOperatorToken));
            Token funcName = ConsumeIdToken();
            if (!useAtOperator)
            {
                funcName.LsClass = "method";

            }

            if (NextTokenIs(typeof(LParenToken))){
                funcExpr = (FuncExpression)ParseFunc(funcName, mustReturnValue: false, declarationRequired: false);
            } else
            {
                funcExpr = new FuncExpression { Function = new IdExpression(funcName, IdentifierType.Function), Args = new() };
            }
                return new CallStatement
            {
                Expr = funcExpr
            };
        }

        Statement ParseTransferStmt()
        {
            Token from = ConsumeIdToken();
            ConsumeToken(typeof(ToToken));
            Token to = ConsumeIdToken();
            return new TransferStatement
            {
                From = new IdExpression(from, IdentifierType.Reference),
                To = new IdExpression(to, IdentifierType.Reference)
            };
        }

        Statement ParseOsReadStmt()
        {
            Token variable = ConsumeIdToken();
            ConsumeToken(typeof(FromToken));
            Expression filePath = ParseExpr();
            (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock();

            return new OsReadStatement
            {
                Variable = new IdExpression(variable),
                Else = elseBlock,
                FilePath = filePath,
                Then = thenBlock
            };
        }

        Statement ParseDimStmt(Token token)
        {
            List<Matrix> matricies = new();
            do
            {
                Token matVar = ConsumeIdToken(addIdentifierToSymbolTable: false);
                ConsumeToken(typeof(LParenToken));
                Expression row = ParseExpr();
                Expression col = null;
                if (NextTokenIs(typeof(CommaToken)))
                {
                    col = ParseExpr();
                }

                ConsumeToken(typeof(RParenToken));
                Matrix matrix = new Matrix(matVar, col, row);
                matricies.Add(matrix);

                if (matVar is SystemVariableToken)
                {
                    _parseErrors.ReportError(matVar, $"A system variable cannot be the name of a matrix.");

                }
                else if (_symbolTable.ContainsEquateOrVaraible(matVar))
                {
                    _parseErrors.ReportError(matVar, $"The symbol {matVar.Text} has already been defined.");

                }
                else
                {
                    _symbolTable.AddMatrixDeclaration(matVar, col, row);
                }

            } while (NextTokenIs(typeof(CommaToken)));

            return new DimStatement
            {
                matricies = matricies
            };
        }

        Statement ParsePragmaStmt()
        {
            PragmaOption opt;
            Token pragmaType = ConsumeToken(null, false, typeof(OutputToken), typeof(PreCompToken));
            if (pragmaType is OutputToken){
                opt = PragmaOption.Output;
            }
            else
            {
                opt = PragmaOption.PreComp;
            }

            Token option = ConsumeIdToken();
            return new PragmaStatement
            {
                Keyword = opt,
                Option = option.Text
            };
        }

        Statement ParseOsWriteStmt()
        {
            Expression expr = ParseExpr();
            ConsumeToken(null,false,typeof(ToToken), typeof(OnToken));
            Expression location = ParseExpr();
            return new OsWriteStatement
            {
                Expr = expr,
                Location = location
            };
        }

        Statement ParseCommonStmt()
        {
            int nSlashes = 1;
            ConsumeToken(typeof(SlashToken));
            if (NextTokenIs(typeof(SlashToken))) {
                nSlashes += 1;
            }

            Token commonBlockId = ConsumeIdToken(addIdentifierToSymbolTable:false);
            if (_symbolTable.IsCommonBlockNameDefined(commonBlockId)){
                _parseErrors.ReportError(commonBlockId, $"The symbol {commonBlockId.Text} has already been defined.");
            }

            if (commonBlockId is SystemVariableToken)
            {
                _parseErrors.ReportError(commonBlockId, $"A system variable cannot be used as a common block id.");
            }

            for (int i = 1; i <= nSlashes;i++)
            {
                ConsumeToken(typeof(SlashToken));
            }

            List<IdExpression> globalVars = new();
            do
            {
                Token name = ConsumeIdToken(addIdentifierToSymbolTable: false);

                bool isMatrix = false;
                Expression matCols = null;
                Expression matRows = null;
                if (NextTokenIs(typeof(LParenToken)))
                {
                    isMatrix = true;
                    matCols = ParseExpr();
                    matRows = null;

                    if (NextTokenIs(typeof(CommaToken)))
                    {
                        matRows = ParseExpr();
                    }
                    ConsumeToken(typeof(RParenToken));
                }


                if (_symbolTable.ContainsEquateOrVaraible(name)){
                    _parseErrors.ReportError(name,$"The symbol {name.Text} has already been defined.");
                }
                else if (name is SystemVariableToken)
                {
                    _parseErrors.ReportError(name, "A system variable cannot be used for the name of a common variable.");
                }
                else
                {
                    if (isMatrix)
                    {
                        _symbolTable.AddCommonDeclaration(name,matCols,matRows);
                    }
                    else
                    {
                        _symbolTable.AddCommonDeclaration(name);

                    }
                }

                globalVars.Add(new IdExpression(name));

            } while (NextTokenIs(typeof(CommaToken)));


            _symbolTable.AddCommonLabel(commonBlockId);
            return new CommonStatement
            {
                CommonName = new IdExpression(commonBlockId),
                GlovalVars = globalVars
            };
        }

        Statement ParseInitRndStmt()
        {
            Expression seed = ParseExpr();

            return new InitRndStatement
            {
                Seed = seed
            };
        }

        Statement ParseSelectStmt()
        {
            Expression tableVar = ParseExpr();

            return new SelectStatement
            {
                TableVar = tableVar
            };
        }

        Statement ParseFlushStmt()
        {
            return new FlushStatement();
        }

        Statement ParseGarbageCollectStmt()
        {
            return new GarbageCollectStatement();
        }

        Statement ParseReadOStmt()
        {
            Token variable = ConsumeIdToken();
            ConsumeToken(typeof(FromToken));
            Expression cursor = null;
            Expression tableVar = null;
            if (NextTokenIs(typeof(CursorToken)))
            {
                cursor = ParseExpr();
            } else
            {
                tableVar = ParseExpr();
            }
            ConsumeToken(typeof(CommaToken));
            Expression key = ParseExpr();
         
            (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock();
            return new ReadOStatement
            {
                Key = key,
                TableVar = tableVar,
                Variable = new IdExpression(variable),
                Else = elseBlock,
                Then = thenBlock,
                Cursor = cursor
            };
        }



        Statement ParseReadVStmt()
        {
            Token variable = ConsumeIdToken();
            ConsumeToken(typeof(FromToken));
            Expression tableVar = ParseExpr();
            ConsumeToken(typeof(CommaToken));
            Expression key = ParseExpr();
            ConsumeToken(typeof(CommaToken));
            Expression col = ParseExpr();

            (List<Statement> thenBlock,List<Statement> elseBlock)  = ParseThenElseBlock();
            return new ReadVStatement
            {
                Column  = col,
                Key = key,
                TableVar = tableVar,
                Variable = new IdExpression(variable),
                Else = elseBlock,
                Then = thenBlock
            };
        }

        Statement ParseMatParseStmt()
        {
            Token variable = ConsumeIdToken();
            ConsumeToken(typeof(IntoToken));
            Token matrixVar = ConsumeIdToken();
            Expression delim = null;
            if (NextTokenIs(typeof(UsingToken)))
            {
                delim = ParseExpr();
            }

            return new MatParseStatement
            {
                Delim = delim,
                Matrix = new IdExpression(matrixVar),
                Variable = new IdExpression(variable)
            };
        }

        Statement ParseInitDirStmt()
        {
            Expression path = ParseExpr();
            return new InitDirStatement
            {
                Path = path
            };
        }

        Statement ParseWriteVStmt()
        {
            Expression expr = ParseExpr();
            ConsumeToken(null,false,typeof(OnToken), typeof(ToToken));
            Expression handle = ParseExpr();
            ConsumeToken(typeof(CommaToken));
            Expression key = ParseExpr();
            ConsumeToken(typeof(CommaToken));
            Expression col = ParseExpr();


            (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock();
            return new WriteVStatement
            {
                Else = elseBlock,
                Handle = handle,
                Key = key,
                Then = thenBlock,
                Expr = expr,
                Col = col
            };
        }

        Statement ParseFreeCommonStmt()
        {
            Token label = ConsumeIdToken();
            return new FreeCommonStatement
            {
                Variable = new IdExpression(label,identifierType: IdentifierType.Reference)
            };
        }

        Statement ParseOsCloseStmt()
        {
            Token variable = ConsumeIdToken();

            return new OsCloseStatement
            {
                FileVar = new IdExpression(variable, IdentifierType.Reference)
            };
        }

        Statement ParseEndStmt(Token token)
        {
            if (!IsProgramEnd())
            {
                throw Error(token, "Statements exists after the end statement.");
            }

            return new EndStatement();
        }


        Statement ParseOsBWriteStmt()
        {
            Expression expr = ParseExpr();
            ConsumeToken(null,false,typeof(OnToken),typeof(ToToken));
            Token fileVar = ConsumeIdToken();
            ConsumeToken(typeof(AtToken));
            Expression @byte = ParseExpr();
            return new OsBWriteStatement
            {
                Byte = @byte,
                Expr = expr,
                Variable = new IdExpression(fileVar, IdentifierType.Reference)
            };
        }

        Statement ParseOsBReadStmt()
        {
            Token variable = ConsumeIdToken();
            ConsumeToken(typeof(FromToken));
            Token fileVar = ConsumeIdToken();
            ConsumeToken(typeof(AtToken));
            Expression byt = ParseExpr();
            ConsumeToken(typeof(LengthToken));
            Expression length = ParseExpr();
            return new OsBreadStatement
            {
                Byte = byt,
                FileVariable = new IdExpression(fileVar, IdentifierType.Reference), 
                Length = length,
                Variable = new IdExpression(variable)
            };
        }

        Statement ParseBRemoveStmt()
        {
            Token variable = ConsumeIdToken();
            ConsumeToken(typeof(FromToken));
            Expression from = ParseExpr();
            ConsumeToken(typeof(AtToken));
            Token pos = ConsumeIdToken();
            ConsumeToken(typeof(SettingToken));
            Token flag = ConsumeIdToken();
            return new BRemoveStatement
            {
                Variable = new IdExpression(variable),
                From = from,
                Pos = new IdExpression(pos),
                Flag = new IdExpression(flag)
            };
        }

        Statement ParseMatrixAssignmentStmt (Token token)
        {

            if (!NextTokenIs(typeof(LParenToken))){
                throw Error(token, $"{token.Text} is a matrix and therefore requires a subscript.");
            }


            Expression row = ParseExpr();
            Expression col = null;
            if (NextTokenIs(typeof(CommaToken)))
            {
                col = ParseExpr();
            }


            ConsumeToken(typeof(RParenToken));
            ConsumeToken(typeof(EqualToken));
            Expression value = ParseExpr();

            _symbolTable.AddVariableReference(token);
            return new MatAssignmentStatement
            {
                Value = value,
                Col = col,
                Row = row,
                Variable = new IdExpression(token, IdentifierType.Assignment)
            };
        }

        Statement ParseOsDeleteStmt()
        {
            Expression filePath = ParseExpr();
            return new OsDeleteStatement
            {
                FilePath = filePath
            };
        }

        Statement ParseOsOpenStmt()
        {
            Expression filePath = ParseExpr();
            ConsumeToken(typeof(ToToken));
            Token variable = ConsumeIdToken();
            (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock();

            return new OsOpenStatement
            {
                FilePath = filePath,
                Variable = new IdExpression(variable),
                Else = elseBlock,
                Then = thenBlock
            };
        }

        Statement ParseAbortAllStmt()
        {
            ConsumeToken(typeof(AllToken));
            return new AbortAllStatement();
        }

        Expression ParseExpr(bool inArray = false, bool optional = false)
        {
            if (IsStatementEnd() && optional)
            {
                return null;
            }

            int tokenIndex = _nextTokenIndex;
            Expression expr = null;
            try
            {
                expr = ParseLogExpr(inArray: inArray);
            }
            catch
            {
                if (tokenIndex + 1 == _nextTokenIndex  && optional)
                {
                    _nextTokenIndex = tokenIndex;
                    return null;
                }
                else
                {
                    throw;
                }
            }

            while (NextTokenIs(out Token optoken, typeof(AndToken), typeof(OrToken), typeof(MatchesToken)))
            {
                Expression right = this.ParseLogExpr(inArray: inArray);


                if (optoken is OrToken)
                {
                    expr = new OrExpression { Left = expr, Right = right, Operator = optoken.Text };
                }
                else if (optoken is MatchesToken)
                {
                    expr = new MatchesExpression { Left = expr, Right = right, Operator = optoken.Text };
                }
                else
                {
                    expr = new AndExpression { Left = expr, Right = right, Operator = optoken.Text };
                }
            }
            return expr;
        }

        Expression ParseLogExpr(bool inArray = false)
        {
            Expression expr = ParseConcatExpr();
            while (NextTokenIs( out Token optoken, typeof(LAngleBracketToken), typeof(RAngleBracketToken), typeof(EqualToken), typeof(ExcalmEqToken),
                typeof(HashTagToken), typeof(GeToken), typeof(LteToken),typeof(EqToken),typeof(NeToken),
                typeof(LtToken),typeof(LeToken),typeof(GtToken),typeof(EqcToken),typeof(NecToken),
                typeof(LtcToken),typeof(LecToken),typeof(GtcToken),typeof(GecToken),typeof(EqxToken),
                typeof(NexToken),typeof(LtxToken),typeof(GtxToken),typeof(LexToken),typeof(GexToken), typeof(ExclamToken)))
            {
                if (optoken is RAngleBracketToken && inArray)
                {
                    // Since we are in an array, take the right angle bracket as the terminator of the array.
                    _nextTokenIndex -= 1;
                    return expr;
                }

                Expression right;
                if  (optoken is LAngleBracketToken && NextTokenIs(typeof(RAngleBracketToken))){
                    right = ParseConcatExpr();
                    expr = new NotEqExpression { Left = expr, Right = right, Operator = optoken.Text };
                    continue;
                }

                if (optoken is RAngleBracketToken && NextTokenIs(typeof(EqualToken)))
                {
                    right = ParseConcatExpr();
                    expr = new GtEqExpression { Left = expr, Right = right, Operator = optoken.Text };
                    continue;
                }

                right = ParseConcatExpr();
                
                if (optoken is LAngleBracketToken || optoken is LtToken || optoken is LteToken || optoken is LtxToken)
                {
                  expr = new LtExpression { Left = expr, Right = right, Operator = optoken.Text };
                }
                else if (optoken is RAngleBracketToken || optoken is GtToken || optoken is GtxToken || optoken is GtcToken)
                {
                    expr = new GtExpression { Left = expr, Right = right, Operator = optoken.Text };
                }
                else if (optoken is ExcalmEqToken || optoken is HashTagToken || optoken is NeToken || optoken is NexToken || optoken is NecToken || optoken is ExclamToken)
                {
                    expr = new NotEqExpression { Left = expr, Right = right, Operator = optoken.Text };
                }
                else if (optoken is LteToken || optoken is LeToken || optoken is LexToken || optoken is LecToken)
                {
                    expr = new LtEqExpression { Left = expr, Right = right, Operator = optoken.Text };
                }
                else if (optoken is GeToken || optoken is GecToken || optoken is GexToken)
                {
                    expr = new GtEqExpression { Left = expr, Right = right, Operator = optoken.Text };
                }
                else
                {
                    expr = new EqExpression { Left = expr, Right = right, Operator = optoken.Text };
                }
            }
            return expr;
        }

        Expression ParseConcatExpr()
        {
            Expression expr = ParseAddExpr();
            while (NextTokenIs(out Token optoken, typeof(ColonToken), typeof(MultiValueConcatToken)))
            {
                Expression right = this.ParseAddExpr();

                if (optoken is MultiValueConcatToken)
                {
                    expr = new MultiValueConcatExpression { Left = expr, Right = right, Operator = optoken.Text };

                }
                else
                {
                    expr = new ConcatExpression { Left = expr, Right = right, Operator = optoken.Text };
                }
            }
            return expr;
        }

        Expression ParseAddExpr()
        {
            Expression expr = ParseMulExpr();
            while (NextTokenIs(out Token optoken, typeof(PlusToken), typeof(MinusToken),
                typeof(MultiValueSubToken),typeof(MultiValueAddToken))) {
                Expression right = this.ParseMulExpr();
                if (optoken is PlusToken)
                {
                    expr = new AddExpression { Left = expr, Right = right, Operator = optoken.Text };
                }
                else if(optoken is MultiValueAddToken)
                {
                    expr = new MultiValueAddExpression { Left = expr, Right = right, Operator = optoken.Text };
                }
                else if(optoken is MultiValueSubToken)
                {
                    expr = new MultiValueSubExpression { Left = expr, Right = right, Operator = optoken.Text };
                }
                else
                {
                    expr = new SubExpression { Left = expr, Right = right, Operator = optoken.Text };
                }
            }
            return expr;
        }

        Expression ParseMulExpr()
        {
            Expression expr = ParsePowerExpr();
            while (NextTokenIs(out Token optoken, typeof(StarToken), typeof(SlashToken),
                typeof(MultiValueMullToken),typeof(MultiValueDivToken))) {
                Expression right = ParsePowerExpr();
                if (optoken is StarToken)
                {
                    expr = new MulExpression { Left = expr, Right = right, Operator = optoken.Text };
                }
                else if (optoken is MultiValueDivToken)
                {
                    expr = new MultiValueDivExpression { Left = expr, Right = right, Operator = optoken.Text };
                }
                else if (optoken is MultiValueMullToken)
                {
                    expr = new MultiValueMullExpression { Left = expr, Right = right, Operator = optoken.Text };
                }
                else
                {
                    expr = new DivExpression { Left = expr, Right = right, Operator = optoken.Text };
                }
            }
            return expr;
        }

        Expression ParsePowerExpr()
        {
            Expression expr = ParseArrayExpr();
            while (NextTokenIs(out Token optoken, typeof(PowerToken)))
            {
                Expression right = ParseAtom();
                expr = new PowerExpression { Left = expr, Right = right, Operator = optoken.Text };
            }
            return expr;
        }

        Expression ParseArrayExpr()
        {
            Expression expr = ParseAtom();

            while (NextTokenIs(out Token optoken, typeof(LAngleBracketToken), typeof(LSqrBracketToken)))
            {
                if (optoken is LAngleBracketToken)
                {
                    Expression arrayExpr = ParseAngleArray(optoken, expr);
                    if (arrayExpr == null)
                    {
                        // Not an array so break out.
                        break;
                    }
                    else
                    {
                        expr = arrayExpr;
                    }
                }
                else
                {
                    expr = ParseSqrBracketArray(optoken, expr);
                }
            }

            return expr;
        }


        Expression ParseAtom()
        {
            Expression expr;
            Token token = GetNextToken();

            if (token is IdentifierToken)
            {
                if (IsMatrix(token))
                {
                    expr = ParseMatrixIndexExpression(token);
                }
                else if (NextTokenIs(typeof(LParenToken)))
                {
                    if (token.DisallowFunction == false)
                    {
                        expr = ParseFunc(token);
                    }
                    else
                    {
                        throw Error(token, "Expression expected.");
                    }
                }
                else if (PeekNextToken() is MinusToken && PeekNextToken(1) is RAngleBracketToken)
                {
                    expr = ParseOleExpr(token);
                }
                else
                {
                    expr = new IdExpression(token, IdentifierType.Reference);
                    _symbolTable.AddVariableReference(token);
                }
            }
            else if (token is NumberToken)
            {
                expr = new NumExpression { Value = token.Text };
            }
            else if (token is StringToken)
            {
                expr = new StringExpression { Value = token.Text };
            }
            else if (token is LParenToken)
            {
                expr = ParseExpr();
                ConsumeToken(typeof(RParenToken));
            }
            else if (token is LCurlyToken)
            {
                expr = ParseExpr();
                ConsumeToken(typeof(RCurlyToken));
                expr = new CurlyExpression { Value = expr };
            }
            else if (token is MinusToken)
            {
                 expr = new NegateExpression { Operator = token.Text, Argument = ParseAtom() };  
            }
            else if (token is PlusToken)
            {
                // Unary + has not meaning, so just ignore it.
                expr = ParseAtom();
            }
            else if (token is NullToken)
            {
                expr = new NullExpression();
            }
            else if (token is LSqrBracketToken)
            {
                expr = ParseArrayInitExpression(token);
            }
            else if (token is IfToken)
            {
                expr = ParseIfExpression(token);
            }
            else
            {
                throw Error(token, "Expression expected.");
            }

            return expr;
        }

        Expression ParseOleExpr(Token token)
        {
            ConsumeToken(typeof(MinusToken));
            ConsumeToken(typeof(RAngleBracketToken));

            Token memberToken = ConsumeIdToken();
            Expression memberExpr;

            if (NextTokenIs(typeof(LParenToken))){
                memberExpr = ParseFunc(memberToken, declarationRequired:false);
                memberToken.LsClass = "variable";
            }
            else
            {
                // TODO make new reference type.
                memberExpr = new IdExpression(memberToken, IdentifierType.Assignment);
            }

            _symbolTable.AddVariableReference(token);
            return new OleExpression { Object = new IdExpression(token, IdentifierType.Reference), Member = memberExpr };

        }

        Expression ParseArrayInitExpression(Token token)
        {
            List<Expression> indexes = new();
            do
            {
                indexes.Add(ParseExpr());

            } while (NextTokenIs(typeof(CommaToken)));
            ConsumeToken(typeof(RSqrBracketToken));
            return new ArrayInitExpression { Sequence = indexes };
        }


        Expression ParseIfExpression(Token token)
        {
            Expression thenBlock = null;
            Expression elseBlock = null;
            Expression cond = ParseExpr();
            if (NextTokenIs(typeof(ThenToken)))
            {
                thenBlock = ParseExpr();
            }
            else
            {
                _parseErrors.ReportError(PeekNextToken(), "then statement is required.");
            }
            if (NextTokenIs(typeof(ElseToken)))
            {
                elseBlock = ParseExpr();
            } else
            {
                _parseErrors.ReportError(PeekNextToken(), "else statement is required.");
            }
            return new IfExpression { Then = thenBlock, Condition = cond, Else = elseBlock };
        }

        Expression ParseAngleArray(Token token, Expression baseExpr)
        {
            if (PeekNextToken() is RAngleBracketToken)
            {
                // We don't have an array.
                _nextTokenIndex -= 1;
                return null;
            }


            int tokenPos = _nextTokenIndex - 1;
            List<Expression> indexes = new();
    
            do
            {
                indexes.Add(ParseExpr(inArray: true));
            } while (NextTokenIs(typeof(CommaToken)));

            if (PeekNextToken() is RAngleBracketToken)
            {
                if (indexes.Count >3)
                {
                    _parseErrors.ReportError(((IdExpression)baseExpr).Token, $"Array {((IdExpression)baseExpr).Token.Text} has more than 3 indexes.");
                }
                _nextTokenIndex += 1;
                return new AngleArrExpression { Indexes = indexes, Source = baseExpr };
            }
            else
            {
                // We assumed that we had an array, but we do not.
                // Backtrack.
                _nextTokenIndex = tokenPos;
                return null;
            }
        }

        Expression ParseSqrBracketArray(Token token, Expression expr)
        {
            List<Expression> indexes = new();
            do
            {
                indexes.Add(ParseExpr());

            } while (NextTokenIs(typeof(CommaToken)));

            if (indexes.Count > 2)
            {
                _parseErrors.ReportError(((IdExpression)expr).Token, $"Array {((IdExpression)expr).Token.Text} has more than 2 indexes.");
            }

            ConsumeToken(typeof(RSqrBracketToken));
            return new SqrArrExpression { Indexes = indexes, Source = expr };
        }

        Expression ParseFunc(Token token, bool mustReturnValue = true, bool declarationRequired = true)
        {
            token.LsClass = "function";
            if (declarationRequired)
            {
                if (mustReturnValue)
                {
                    if (!_symbolTable.IsFunctionDeclared(token))
                    {
                        _parseErrors.ReportError(token, $"{token.Text} must be declared as a function");
                    } else
                    {
                        _symbolTable.AddFunctionReference(token);
                    }
                }
                else
                {
                    if (!_symbolTable.IsSubroutineDeclared(token))
                    {
                        _parseErrors.ReportError(token, $"{token.Text} must be declared as a subroutine.");
                    } else
                    {
                        _symbolTable.AddSubroutineReference(token);
                    }
                }
            }

            List<Expression> args = new();
            while (!NextTokenIs(typeof(RParenToken)))
            {
                if (args.Count > 0)
                {
                    ConsumeToken(typeof(CommaToken));
                }

                Expression arg;
                if (NextTokenIs(typeof(MatToken)))
                {
                    Token matrix = ConsumeIdToken();
                    if (!IsMatrix(matrix))
                    {
                        _parseErrors.ReportError(matrix, $"The identifier {matrix.Text} must be dimensioned.");
                    }
                    arg = new IdExpression(matrix, IdentifierType.Reference);
                } else
                {
                    arg = ParseExpr();
                    if (arg is IdExpression)
                    {
                        ((IdExpression)arg).IdentifierType = IdentifierType.Assignment;
                  
                    }
                }

                args.Add(arg);
            }
            return new FuncExpression { Args = args, Function = new IdExpression(token, IdentifierType.Function) };
        }

        Expression ParseMatrixIndexExpression(Token token)
        {
            if (!NextTokenIs(typeof(LParenToken)))
            {
                throw Error(token, $"{token.Text} is a matrix and therefore requires subscript/s");
            }

            Expression col = ParseExpr();
            Expression row = null;
            if (NextTokenIs(typeof(CommaToken))){
                row = ParseExpr();
            }
            ConsumeToken(typeof(RParenToken));

            _symbolTable.AddVariableReference(token);

            return new MatrixIndexExpression { Col = col, Row = row, 
                Name = new IdExpression(token, IdentifierType.Reference) };
        }

        Token PeekNextToken(int lookAhead = 0)
        {
            if (_nextTokenIndex + lookAhead < _tokens.Count)
            {
                return _tokens[_nextTokenIndex + lookAhead];
            }

            return _tokens.Last();
        }

        Token GetNextToken()
        {
            if (_nextTokenIndex < _tokens.Count)
            {
                return _tokens[_nextTokenIndex++];
            }
            throw Error(_tokens.Last(),"Unexpected end of program.");
        }

        bool NextTokenIs(out Token token, params Type[] validTokens)
        {
            token = PeekNextToken();
            if (token == null) return false;
            if (validTokens.Any(x =>  x.IsAssignableFrom(_nextToken.GetType())))
            {
                _nextTokenIndex++;
                return true;
            }
            else
            {
                return false;
            }
        }

        Token ConsumeToken(string errMsg = null, bool optional = false, params Type[] expected)
        {
            Token token = PeekNextToken();
            if (!expected.Any(t => t == token.GetType()))
            {
                if (optional)
                {
                    return null;
                }

                if (errMsg == null)
                {
                    errMsg = $"Expected {expected}";
                }
                throw Error(token, errMsg);
            }
            return _tokens[_nextTokenIndex++];
        }

        Token ConsumeToken(Type expected, string errMsg = null, bool optional = false)
        {
            Token token = PeekNextToken();
            if (token.GetType() != expected)
            {
                if (optional)
                {
                    return null;
                }

                if (errMsg == null)
                {
                    errMsg = $"Expected {expected}";
                }
                throw Error(token, errMsg);
            }
            return _tokens[_nextTokenIndex++];
        }

        Token ConsumeIdToken(string message = "Identifier expected.", bool addIdentifierToSymbolTable = true, bool optional = false)
        {
            Token token = PeekNextToken();
            if (token is not IdentifierToken)
            {
                if (optional)
                {
                    return null;
                }
                else
                {
                    throw Error(token, message);
                }
            }
            if (addIdentifierToSymbolTable) _symbolTable.AddVariableReference(token);
            return _tokens[_nextTokenIndex++];
        }

        bool NextTokenIs(params Type[] validTokens)
        {
            return NextTokenIs(out Token token, validTokens);
        }

        bool IsAtEnd()
        {
            return PeekNextToken() is EofToken;
        }

        bool IsProgramEnd()
        {
            while (NextTokenIs(typeof(NewLineToken), typeof(SemiColonToken))) ;
            return PeekNextToken()  is EofToken; 
        }

        bool IsStatementEnd()
        {
            return PeekNextToken() is EofToken || PeekNextToken()  is NewLineToken || PeekNextToken()  is SemiColonToken;
        }

        int GetLineNo()
        {
            return PeekNextToken()?.LineNo ?? _tokens.Last().LineNo;
        }

        int GetLineCol()
        {
            return PeekNextToken()?.StartCol ?? _tokens.Last().LineNo;
        }

        bool IsMatrix(Token token)
        {
            return _symbolTable.IsMatrix(token);
        }

        void ConsumeSemiColonsUntilEndOfLine()
        {
            while (NextTokenIs(typeof(SemiColonToken)));
            if (!NextTokenIs(typeof(NewLineToken)))
            {
                throw Error(_nextToken, "New Line expected.");
            }
        }

        ParseException Error(Token token, string message)
        {
            return new ParseException(token, message);
        }

        public void CheckIfJumpLabelsAreDefined()
        {
            foreach(var label in _symbolTable.LabelReferences)
            {
               if (!_symbolTable.IsLabelDeclared(label))
                {
                    _parseErrors.ReportError(label, $"The label {label.Text} has not been defined.");
                }      
            }
        }
    }
}
