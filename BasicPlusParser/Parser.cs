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
        public const int MAX_PARAMS = 500;

        int _nextTokenIndex = 0;
        List<Token> _tokens = new();
        Token _nextToken => _nextTokenIndex < _tokens.Count ? _tokens[_nextTokenIndex] : null;
        Token _prevToken => _nextTokenIndex > 0 ? _tokens[_nextTokenIndex - 1] : null;
        Dictionary<string, (List<Statement>, int pos)> _labels = new();
        Dictionary<string, Matrix> _matricies = new();
        ParseErrors _parseErrors = new();

        public Parser(string text)
        {
            Tokenizer tokenizer = new(text, _parseErrors);
            _tokens = tokenizer.Tokenise();
        }

        public OiProgram Parse()
        {
            OiProgram program = ParseProgramDeclaration();
            program.Statements = ParseStmts(() => IsAtEnd());
            program.Labels = _labels;
            program.Errors = _parseErrors;
            return program;
        }

        OiProgram ParseProgramDeclaration()
        {
            ProgramType programType;
            ConsumeToken(typeof(CompileToken), optional: true);
            Token progType = ConsumeToken("Program type missing. Must be either function or subroutine.", false, typeof(SubroutineToken), typeof(FunctionToken));
            if (progType is FunctionToken)
            {
                programType = ProgramType.Function;
            }
            else
            {
                programType = ProgramType.Subroutine;
            }


            Token progName = ConsumeIdToken("Program must have a name.");
            ConsumeToken(typeof(LParenToken));
            List<Token> args = new();
            while (!NextTokenIs(typeof(RParenToken)))
            {
                if (args.Count > 0)
                {
                    ConsumeToken(typeof(CommaToken));
                }
                Token arg = ConsumeIdToken();
                args.Add(arg);
            }
            ConsumeToken(typeof(NewLineToken));
            return new OiProgram(programType, progName.Text, args);
        }

        public List<Statement> ParseStmts(Func<bool> stop, bool inLoop = false)
        {
            return ParseStmts(_ => stop(), inLoop: inLoop);
        }

        public List<Statement> ParseStmts(Func<List<Statement>, bool> stop, bool inLoop = false)
        {
            List<Statement> statements = new();
            while (!stop(statements))
            {
                try
                {
                    if (statements.Count >= 1) ConsumeStatementSeparator();

                    if (stop(statements)) break;

                    int lineNo = GetLineNo();
                    statements.Add(ParseStmt(inLoop: inLoop));
                    AnnotateStmt(statements, lineNo);
                }
                catch
                {
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
                if (!IsMatrix(token) && NextTokenIs(typeof(EqualToken)))
                {
                    return ParseAssignmentStmt(token);

                }
                else if (!IsMatrix(token) && NextTokenIs(typeof(LAngleBracketToken)))
                {
                    return ParseAngleAssignmentStmt(token);
                }
                else if (!IsMatrix(token) && token.DisallowFunction == false && NextTokenIs(typeof(LSqrBracketToken)))
                {
                    return ParseSquareBracketArrayAssignmentStmt(token);
                }
                else if (NextTokenIs(typeof(ColonToken)))
                {
                    return ParseInternalSubStmt(token);
                }
                else if (!IsMatrix(token) && NextTokenIs(typeof(PlusEqualToken)))
                {
                    return ParsePlusAssignmentStmt(token);
                }
                else if (!IsMatrix(token) && NextTokenIs(typeof(MinusEqualToken)))
                {
                    return ParseMinusAssignmentStmt(token);
                }
                else if (!IsMatrix(token) && NextTokenIs(typeof(SlashEqualToken)))
                {
                    return ParseDivideAssignmentStmt(token);
                }
                else if (!IsMatrix(token) && NextTokenIs(typeof(StarEqualToken)))
                {
                    return ParseMulAssignmentStmt(token);
                }
                else if (NextTokenIs(typeof(ColonEqualToken)))
                {
                    return ParseConcatAssignmentStmt(token);
                }
                else if (!token.DisallowFunction && !IsMatrix(token) && NextTokenIs(typeof(LParenToken)))
                {
                    return ParseFunctionCallStmt(token);
                }
                else if (IsMatrix(token) && NextTokenIs(typeof(LParenToken)))
                {
                    return ParseMatrixAssignmentStmt(token);
                }
                else if (token is IfToken)
                {
                    return ParseIfStmt();
                }
                else if (token is BeginToken)
                {
                    return ParseCaseStmt();
                }
                else if (token is ReturnToken)
                {
                    return ParseReturnStmt();
                }
                else if (token is ForToken)
                {
                    return ParseForLoopStmt();
                }
                else if (token is LoopToken)
                {
                    return ParseLoopRepeatStmt();
                }
                else if (token is GosubToken)
                {
                    return ParseGosubStmt();
                }
                else if (inLoop == true && token is WhileToken)
                {
                    return ParseWhileStmt();
                }
                else if (inLoop == true && token is UntilToken)
                {
                    return ParseUntilStmt();
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
            }
            else if (token is PragmaToken)
            {
                return ParsePragmaStmt();
            }
            else if (token is EndToken)
            {
                return ParseEndStmt();
            }
            else if (token is SemiColonToken)
            {
                return new EmptyStatement();
            }
            throw Error(GetLineNo(), $"Unmatched token {token}.");
        }

        void ConsumeStatementSeparator()
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

            throw Error(GetLineNo(), "Semicolon or newline expected.");
        }

        void AnnotateStmt(List<Statement> statements, int lineNo)
        {
            Statement statement = statements.Last();
            statement.LineNo = lineNo;
            switch (statement)
            {
                case
                    InternalSubStatement s:
                    _labels.Add(s.Label.Name, (statements, statements.Count));
                    break;
            }
        }

        void Sync(Func<List<Statement>, bool> stop, List<Statement> statements)
        {
            // When an error occurs, lets start parsing again after the next new line or semicolon.
            while (!stop(statements) && PeekNextToken() is not NewLineToken && PeekNextToken() is not SemiColonToken && PeekNextToken() is not EofToken)
            {
                _nextTokenIndex += 1;
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

            if (NextTokenIs(typeof(ThenToken)))
            {
                hasThen = true;
                if (NextTokenIs(typeof(NewLineToken)))
                {
                    thenBlock = ParseStmts(_ =>PeekNextToken() is EndToken || IsAtEnd());
                    ConsumeToken(typeof(EndToken));
                }
                else
                {
                    thenBlock = ParseStmts(_ => PeekNextToken() is ElseToken || PeekNextToken() is NewLineToken
                        || IsAtEnd() || (PeekNextToken() is SemiColonToken && PeekNextToken(1) is NewLineToken));
                }
            }

            if (NextTokenIs(typeof(ElseToken)))
            {
                hasElse = true;
                if (NextTokenIs(typeof(NewLineToken)) || IsAtEnd())
                {
                    elseBlock = ParseStmts(() => PeekNextToken() is EndToken || IsAtEnd());
                    ConsumeToken(typeof(EndToken));
                }
                else
                {
                    elseBlock = ParseStmts(() => PeekNextToken() is NewLineToken || IsAtEnd() ||
                        (PeekNextToken() is SemiColonToken && PeekNextToken(1) is NewLineToken));
                }
            }

            if (!(hasElse || hasThen) && optional == false)
            {
                throw Error(GetLineNo(), "Then or else block expected.");
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
                Var = new IdExpression(var.Text)
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
                Variable = new IdExpression(var.Text,IdentifierType.Assignment)
            };
        }

        AssignmentStatement ParseAssignmentStmt(Token token)
        {
            Expression expr = ParseExpr();
            return new AssignmentStatement { Value = expr, Variable = new IdExpression(token.Text,IdentifierType.Assignment) };
        }


        Statement ParseDeclareStmt()
        {
            List<IdExpression> functions = new List<IdExpression>();
            ProgramType pType;
            Token funcType = ConsumeToken("Expected subroutine or function.", false, typeof(SubroutineToken), typeof(FunctionToken));
            if (funcType is SubroutineToken)
            {
                pType = ProgramType.Subroutine;
            } 
            else 
            {
                pType = ProgramType.Function;
            }

            do
            {
                Token func = ConsumeIdToken();
                functions.Add(new IdExpression(func.Text, IdentifierType.Function));
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
                Handle = new IdExpression(handle.Text),
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
            Token label = ConsumeIdToken();
            return new GoToStatement
            {
                Label = new IdExpression(label.Text, IdentifierType.Label)
            };
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
                    Pos = new IdExpression(pos.Text),
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
                    Start = new IdExpression(pos.Text),
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
                Token label = ConsumeIdToken();
                labels.Add(new IdExpression(label.Text));
   
            } while (NextTokenIs(typeof(CommaToken)));

            if (isGosub)
            {
                return new OnGosubStatement
                {
                    Index = index,
                    Labels = labels
                };
            } else
            {
                return new OnGotoStatement
                {
                    Index = index,
                    Labels = labels
                };
            }
        }

        Statement ParseIfStmt()
        {
            Expression cond = ParseExpr();

            (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock();

            return new IfStatement
            {
                Condition = cond,
                Then = thenBlock,
                Else = elseBlock
            };
        }

        Case ParseCase()
        {
            Expression cond = ParseExpr();
            ConsumeToken(typeof(NewLineToken));
            List<Statement> statements = 
                ParseStmts(() => PeekNextToken() is  CaseToken || PeekNextToken() is EndToken);
            return new Case
            {
                Condition = cond,
                Statements = statements
            };
        }

        Statement ParseCaseStmt()
        {
            ConsumeToken(typeof(CaseToken));
            ConsumeToken(typeof(NewLineToken));
            List<Case> cases = new();
            while (NextTokenIs(typeof(CaseToken)))
            {
                cases.Add(ParseCase());
            }
            ConsumeToken(typeof(EndToken));
            ConsumeToken(typeof(CaseToken));
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
            while (indexes.Count < 4 && NextTokenIs(typeof(CommaToken)));
            ConsumeToken(typeof(RAngleBracketToken));
            ConsumeToken(typeof(EqualToken));
            Expression expr = ParseExpr();
            return new AngleArrayAssignmentStatement
            {
                Indexes = indexes,
                Variable = new IdExpression(token.Text, IdentifierType.Reference),
                Value = expr
            };
        }

        Statement ParseWhileStmt()
        {
            Expression cond = ParseExpr();
            ConsumeToken(typeof(DoToken),optional:true);
            return new WhileStatement
            {
                Condition = cond
            };
        }

        Statement ParseUntilStmt()
        {
            Expression cond = ParseExpr();
            ConsumeToken(typeof(DoToken),optional:true);
            return new UntilStatement
            {
                Condition = cond
            };
        }

        Statement ParseEquStatement(Token token)
        {
            Token var = ConsumeIdToken();
            ConsumeToken(typeof(ToToken));
            Expression val = ParseExpr();
            return new EquStatemnet
            {
                Variable = new IdExpression(var.Text),
                Value = val
            };
        }

        Statement ParseFunctionCallStmt(Token token)
        {
            _nextTokenIndex -= 2;
            FuncExpression funcExpr = (FuncExpression) ParseExpr();
            return new FunctionCallStatement
            {
                Expr = funcExpr
            };
        }

        Statement ParseLoopRepeatStmt()
        {
            List<Statement> statements = new();
            ConsumeToken(typeof(NewLineToken), optional: true);
            statements = ParseStmts(() => PeekNextToken() is RepeatToken || IsAtEnd(), inLoop: true);
            ConsumeToken(typeof(RepeatToken));
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
                _parseErrors.ReportError(GetLineNo(), $"The identifier {matrix.Text} must be dimensioned.");
            }

            Expression value = null;
            IdExpression otherMatrix = null;
            ConsumeToken(typeof(EqualToken));
            if (NextTokenIs(typeof(MatToken))) {

                Token otherMatrixToken = ConsumeIdToken();
                if (!IsMatrix(otherMatrixToken))
                {
                    _parseErrors.ReportError(GetLineNo(), $"The identifier {otherMatrixToken.Text} must be dimensioned.");
                }
                otherMatrix = new IdExpression(otherMatrixToken.Text, IdentifierType.Reference);
            }
            else
            {
                value = ParseExpr();
            }
           
            return new MatStatement
            {
                Variable = new IdExpression(matrix.Text),
                Value = value,
                OtherMatrix = otherMatrix
            };
        }

        public Statement ParseForLoopStmt()
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

            ConsumeToken(typeof(NewLineToken),optional:true);
            statements = ParseStmts(() => PeekNextToken() is NextToken || IsAtEnd() , inLoop: true);
            ConsumeToken(typeof(NextToken));

            if (!(PeekNextToken() is NewLineToken || PeekNextToken() is EofToken))
            {
                // Basic + allows you to put an expression after the next keyword. But it has
                // no significance, so we just eat it.
                ParseExpr();
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
                Value = new IdExpression(value?.Text),
                Variable = new IdExpression( variable.Text),
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
                Variable = new IdExpression(var.Text),
                From = from,
                Pos = new IdExpression(pos.Text),
                Flag = new IdExpression(flag.Text)
            };

        }

        public Statement ParseSquareBracketArrayAssignmentStmt(Token token)
        {
            List<Expression> indexes = new();
            do
            {
                indexes.Add(ParseExpr());
            }
            while (indexes.Count < 2 && NextTokenIs(typeof(CommaToken)));
            ConsumeToken(typeof(RSqrBracketToken));
            ConsumeToken(typeof(EqualToken));
            Expression expr = ParseExpr();
            return new SquareBracketArrayAssignmentStatement
            {
                Indexes = indexes,
                Variable = new IdExpression(token.Text, IdentifierType.Reference),
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
                Variable = new IdExpression(variable.Text),
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
            return new InternalSubStatement
            {
                Label = new IdExpression(token.Text,IdentifierType.Label),
               // Statements = statements
            };
        }

        Statement ParseGosubStmt()
        {
            Token label = ConsumeIdToken();
            return new GosubStatement
            {
                Label = new IdExpression(label.Text, IdentifierType.Label)
            };
        }

        Statement ParsePlusAssignmentStmt(Token token)
        {
            Expression expr = ParseExpr();
            return new PlusAssignmentStatement
            {
                Value = expr,
                Variable = new IdExpression(token.Text, IdentifierType.Reference)
            };
        }

        Statement ParseInsertStmt()
        {
            Token insert = ConsumeIdToken();
            return new InsertStatement
            {
                Name = new IdExpression(insert.Text, IdentifierType.Insert)
            };
        }

        Statement ParseMinusAssignmentStmt(Token token)
        {
            Expression expr = ParseExpr();
            return new MinusAssignmentStatement
            {
                Value = expr,
                Variable = new IdExpression(token.Text,IdentifierType.Reference)
            };
        }

        Statement ParseDivideAssignmentStmt(Token token)
        {
            Expression expr = ParseExpr();
            return new DivideAssignmentStatement
            {
                Value = expr,
                Variable = new IdExpression(token.Text, IdentifierType.Reference)
            };
        }
        Statement ParseMulAssignmentStmt(Token token)
        {
            Expression expr = ParseExpr();
            return new MulAssignmentStatement
            {
                Value = expr,
                Name = new IdExpression(token.Text,IdentifierType.Reference)
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
            return new ConcatAssignmentStatement
            {
                Value = expr,
                Variable = new IdExpression(token.Text, IdentifierType.Reference)
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
            Expression funcExpr = ParseExpr();
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
                From = new IdExpression(from.Text, IdentifierType.Reference),
                To = new IdExpression(to.Text, IdentifierType.Reference)
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
                Variable = new IdExpression(variable.Text),
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
                Token matVar = ConsumeIdToken();
                ConsumeToken(typeof(LParenToken));
                Expression row = ParseExpr();
                Expression col = null;
                if (NextTokenIs(typeof(CommaToken)))
                {
                    col = ParseExpr();
                }
                ConsumeToken(typeof(RParenToken));
                Matrix matrix = new Matrix(matVar.Text, col, row);
                matricies.Add(matrix);
                _matricies.Add(matVar.Text.ToLower(),matrix);

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

            Token commonBlockId = ConsumeIdToken();

            for(int i = 1; i <= nSlashes;i++)
            {
                ConsumeToken(typeof(SlashToken));
            }

            List<IdExpression> globalVars = new();
            do
            {
                Token name = ConsumeIdToken();
                globalVars.Add(new IdExpression(name.Text));

            } while (NextTokenIs(typeof(CommaToken)));

            return new CommonStatement
            {
                CommonName = new IdExpression(commonBlockId.Text),
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
                Variable = new IdExpression(variable.Text),
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
                Variable = new IdExpression(variable.Text),
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
                Matrix = new IdExpression(matrixVar.Text),
                Variable = new IdExpression(variable.Text)
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
                Variable = new IdExpression(label.Text,identifierType: IdentifierType.Reference)
            };
        }

        Statement ParseOsCloseStmt()
        {
            Token variable = ConsumeIdToken();

            return new OsCloseStatement
            {
                FileVar = new IdExpression(variable.Text, IdentifierType.Reference)
            };
        }

        Statement ParseEndStmt()
        {
            if (!IsProgramEnd())
            {
                throw new InvalidOperationException("End statement used but the program is not done.");
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
                Variable = new IdExpression(fileVar.Text, IdentifierType.Reference)
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
                FileVariable = new IdExpression(fileVar.Text, IdentifierType.Reference), 
                Length = length,
                Variable = new IdExpression(variable.Text)
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
                Variable = new IdExpression(variable.Text),
                From = from,
                Pos = new IdExpression(pos.Text),
                Flag = new IdExpression(flag.Text)
            };
        }

        Statement ParseMatrixAssignmentStmt (Token token)
        {
            Expression row = ParseExpr();
            Expression col = null;
            if (NextTokenIs(typeof(CommaToken)))
            {
                col = ParseExpr();
            }
            ConsumeToken(typeof(RParenToken));
            ConsumeToken(typeof(EqualToken));
            Expression value = ParseExpr();

            return new MatAssignmentStatement
            {
                Value = value,
                Col = col,
                Row = row,
                Variable = new IdExpression(token.Text, IdentifierType.Assignment)
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
                Variable = new IdExpression(variable.Text),
                Else = elseBlock,
                Then = thenBlock
            };
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
                typeof(NexToken),typeof(LtxToken),typeof(GtxToken),typeof(LexToken),typeof(GexToken)))
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
                else if (optoken is ExcalmEqToken || optoken is HashTagToken || optoken is NeToken || optoken is NexToken || optoken is NecToken )
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
            Expression expr = ParseAtom();
            while (NextTokenIs(out Token optoken, typeof(PowerToken)))
            {
                Expression right = ParseAtom();
                expr = new PowerExpression { Left = expr, Right = right, Operator = optoken.Text };
            }
            return expr;
        }

        Expression ParseAtom()
        {
            Expression expr;
            Token token = GetNextToken();

            if (token is IdentifierToken )
            {
                if (NextTokenIs(typeof(LParenToken)))
                {
                    if (IsMatrix(token)){
                        expr = ParseMatrixIndexExpression(token);
                    }
                    else if (token.DisallowFunction == false)
                    {
                        expr = ParseFunc(token);
                    }
                    else
                    {
                        throw Error(GetLineNo(), "Expression expected.");
                    }
                }
                else if (token is IfToken)
                {
                    expr = ParseIfExpression(token);
                }
                else
                {
                    expr = new IdExpression(token.Text, IdentifierType.Reference);
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
            else if (token is MinusToken) {
                expr = new NegateExpression { Operator = token.Text, Argument = ParseAtom() };
            }
            else if (token is NullToken)
            {
                expr = new NullExpression();
            }
            else if (token is LSqrBracketToken)
            {
                expr = ParseArrayInitExpression(token);
            }
            else
            {
                throw Error(GetLineNo(),"Expression expected.");
            }

            while (NextTokenIs(out Token optoken, typeof(LAngleBracketToken), typeof(LSqrBracketToken)))
            {
                if (optoken is LAngleBracketToken)
                {
                    Expression arrayExpr = ParseAngleArray(token, expr);
                    if (arrayExpr == null)
                    {
                        // Not an array so break out.
                        break;
                    } else
                    {
                        expr = arrayExpr;
                    }
                }
                else
                {
                    expr = ParseSqrBracketArray(token, expr);
                }
            }
            return expr;
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
            if (NextTokenIs(typeof(ElseToken)))
            {
                elseBlock = ParseExpr();
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

            } while (indexes.Count < 4 && NextTokenIs(typeof(CommaToken)));

            if (PeekNextToken() is RAngleBracketToken)
            {
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

            } while (indexes.Count < 2 && NextTokenIs(typeof(CommaToken)));
            ConsumeToken(typeof(RSqrBracketToken));
            return new SqrArrExpression { Indexes = indexes, Source = expr };
        }

        Expression ParseFunc(Token token)
        {
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
                        _parseErrors.ReportError(GetLineNo(), $"The identifier {matrix.Text} must be dimensioned.");
                    }
                    arg = new IdExpression(matrix.Text, IdentifierType.Reference);
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
            return new FuncExpression { Args = args, Function = new IdExpression(token.Text, IdentifierType.Function) };
        }

        Expression ParseMatrixIndexExpression(Token token)
        {
            Expression row = ParseExpr();
            Expression col = null;
            if (NextTokenIs(typeof(CommaToken))){
                col = ParseExpr();
            }
            ConsumeToken(typeof(RParenToken));

            return new MatrixIndexExpression { Col = col, Row = row, 
                Name = new IdExpression(token.Text, IdentifierType.Reference) };
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
            throw Error(GetLineNo(),"Unexpected end of program.");
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
                throw Error(_nextToken.LineNo, errMsg);
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
                throw Error(_nextToken.LineNo, errMsg);
            }
            return _tokens[_nextTokenIndex++];
        }

        Token ConsumeIdToken(string message = "Identifier expected.")
        {
            Token token = PeekNextToken();
            if (token.GetType() != typeof(IdentifierToken))
            {
                _parseErrors.ReportError(_nextToken.LineNo, message);
                throw new ParseException();
            }
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

        bool IsMatrix(Token token)
        {
            return _matricies.ContainsKey(token.Text.ToLower());
        }

        ParseException Error(int lineNo, string message)
        {
            _parseErrors.ReportError(lineNo, message);
            return new ParseException();
        }
    }
}
