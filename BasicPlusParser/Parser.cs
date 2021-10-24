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
        public const int MAX_PARAMS = 25;

        int _nextTokenIndex = 0;
        List<Token> _tokens = new();
        Token _nextToken => _nextTokenIndex < _tokens.Count ? _tokens[_nextTokenIndex] : null;
         

        public Parser(string text)
        {
            Tokenizer tokenizer = new(text);
            _tokens = tokenizer.Tokenise();
        }

        public OiProgram Parse()
        {
            OiProgram program = ParseProgramDeclaration();
            program.Statements = ParseStmts(()=> NextTokenIs(typeof(EofToken)));
            return program;
        }

        OiProgram ParseProgramDeclaration()
        {
            ProgramType programType;
            NextTokenIs(typeof(CompileToken));
            if (NextTokenIs(typeof(FunctionToken)))
            {
                programType = ProgramType.Function;
            }
            else if (NextTokenIs(typeof(SubroutineToken)))
            {
                programType = ProgramType.Subroutine;
            }
            else
            {
                throw new InvalidOperationException("Program must start with function or subroutine.");
            }

            if (!NextTokenIs(out Token progName, typeof(IdentifierToken)))
            {
                throw new InvalidOperationException("Program must have a name.");
            }

            ExpectToken(typeof(LParenToken));
            List<Token> args = new List<Token>();
            while (!NextTokenIs(typeof(RParenToken)))
            {
                Token arg = GetNextToken();
                if (arg is not IdentifierToken)
                {
                    throw new InvalidOperationException("Invalid parameter");
                }
                args.Add(arg);
                NextTokenIs(typeof(CommaToken));
            }
            ExpectToken(typeof(NewLineToken));
            return new OiProgram(programType, progName.Text, args);
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
            ExpectToken(typeof(CommaToken));
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
            ExpectToken(typeof(CommaToken));
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
            ExpectToken(typeof(CommaToken));
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
            List<Statement> elseBlock = new List<Statement>();
            List<Statement> thenBlock = new List<Statement>();
            bool hasThen = false;
            bool hasElse = false;

            if (NextTokenIs(typeof(ThenToken)))
            {
                Func<bool> stop;
                if (NextTokenIs(typeof(NewLineToken)))
                {
                    stop = () => NextTokenIs(typeof(EndToken));
                }
                else
                {
                    stop = () => PeekNextToken() is ElseToken || PeekNextToken() is NewLineToken
                        || PeekNextToken() is EofToken;
                }

                thenBlock = ParseStmts(stop);
                hasThen = true;
            }

            if (NextTokenIs(typeof(ElseToken)))
            {

                Func<bool> stop;
                if (NextTokenIs(typeof(NewLineToken)))
                {
                    stop = () => NextTokenIs(typeof(EndToken));
                }
                else
                {
                    stop = () => PeekNextToken() is NewLineToken || PeekNextToken() is EofToken;
                }
                elseBlock = ParseStmts(stop);
                hasElse = true;
            }

            if (!(hasElse || hasThen) && optional == false)
            {
                throw new InvalidOperationException("If statement requires either a then or else block.");
            }

            return (thenBlock, elseBlock);
        }


        Statement ParseMatWriteStmt()
        {
            Expression expr = ParseExpr();
            if (!NextTokenIs(typeof(OnToken), typeof(ToToken)))
            {
                throw new InvalidOperationException("Expected on or to");
            }
            Expression handle = ParseExpr();
            ExpectToken(typeof(CommaToken));
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
            if (!NextTokenIs(typeof(OnToken), typeof(ToToken))){
                throw new InvalidOperationException("Expected on or to");
            }
            Expression handle = ParseExpr();
            ExpectToken(typeof(CommaToken));
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
            if (!NextTokenIs(out Token var, typeof(IdentifierToken)))
            {
                throw new InvalidOperationException("Identifier expected.");
            }
            ExpectToken(typeof(FromToken));
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
            ExpectToken(typeof(CommaToken));
            Expression key = ParseExpr();

            (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock();

            return new MatReadStatement
            {
                Cursor = cursor,
                Else = elseBlock,
                Handle = handle,
                Key = key,
                Then = thenBlock,
                Var = var.Text
            };
        }



        Statement ParseReadStmt()
        {
            if (!NextTokenIs(out Token var, typeof(IdentifierToken))){
                throw new InvalidOperationException("Identifier expected.");
            }
            ExpectToken(typeof(FromToken));
            Expression handle = null;
            Expression cursor = null;
            if (NextTokenIs(typeof(CursorToken))){
                cursor = ParseExpr();
            }
            else
            {
                handle = ParseExpr();
            }
            ExpectToken(typeof(CommaToken));
            Expression key = ParseExpr();

            (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock();

            return new ReadStatement
            {
                Cursor = cursor,
                Else = elseBlock,
                Handle = handle,
                Key = key,
                Then = thenBlock,
                Var = var.Text
            };
        }

        AssignmentStatement ParseAssignmentStmt(Token token)
        {
            Expression expr = ParseExpr();
            return new AssignmentStatement { Expr = expr, Name = token.Text };
        }


        Statement ParseDeclareStmt()
        {
            List<string> functions = new List<String>();
            ProgramType pType;
            if (NextTokenIs(typeof(SubroutineToken)))
            {
                pType = ProgramType.Subroutine;
            } 
            else if (NextTokenIs(typeof(FunctionToken)))
            {
                pType = ProgramType.Subroutine;
            }
            else
            {
                throw new InvalidOperationException("Expected subroutine or function.");
            }

            while (NextTokenIs(out Token token, typeof(IdentifierToken)))
            {
                functions.Add(token.Text);
                NextTokenIs(typeof(CommaToken));
            }   
            return new DeclareStatement
            {
                PType = pType,
                Functions = functions
            };
        }


        Statement ParseOpenStmt()
        {
            Expression table = ParseExpr();
            ExpectToken(typeof(ToToken));
            if (!NextTokenIs(out Token handle,typeof(IdentifierToken)))
            {
                throw new InvalidOperationException("Identifier expected.");
            }

           (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock();

            return new OpenStatement
            {
                Else = elseBlock,
                Handle = handle.Text,
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
            if (!NextTokenIs(out Token label, typeof(IdentifierToken))){
                throw new InvalidOperationException("Expected identifier.");
            }

            return new GoToStatement
            {
                Label = label.Text
            };
        }

        Statement ParseLocateStmt()
        {
            bool isBy = false;
            Expression seq = null;

            Expression needle = ParseExpr();
            ExpectToken(typeof(InToken));
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
            ExpectToken(typeof(SettingToken));
            if (!NextTokenIs(out Token pos, typeof(IdentifierToken)))
            {
                throw new InvalidOperationException("Expected identifeir.");
            }

            (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock();
            
            if (isBy)
            {
                return new LocateByStatement
                {
                    Needle = needle,
                    Haystack = haystack,
                    Delim = delim,
                    Else = elseBlock,
                    Pos = pos.Text,
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
                    Start = pos.Text,
                    Then = thenBlock
                };
            } 
        }

        Statement ParseJumpStmt()
        {
            bool isGosub = false;

            Expression index = ParseExpr();

            if (NextTokenIs(typeof(GosubToken))){
                isGosub = true;
            } else if (!NextTokenIs(typeof(GoToToken)))
            {
                throw new InvalidOperationException("Expected gosub or goto.");
            }

            List<String> labels = new List<string>();

            do
            {
                if (NextTokenIs(out Token label, typeof(IdentifierToken))){
                    labels.Add(label.Text);
                }
                else
                {
                    throw new InvalidOperationException("expected identifier.");
                }
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
                ThenBlock = thenBlock,
                ElseBlock = elseBlock
            };
        }

        public Case ParseCase()
        {
            Expression cond = ParseExpr();
            ExpectStatementEnd();
            List<Statement> statements = 
                ParseStmts(() => PeekNextToken() is  CaseToken || PeekNextToken() is EndToken);
            return new Case
            {
                Condition = cond,
                Statements = statements
            };
        }

        public Statement ParseCaseStmt()
        {
            ExpectToken(typeof(CaseToken));
            ExpectToken(typeof(NewLineToken));
            List<Case> cases = new List<Case>();
            while (NextTokenIs(typeof(CaseToken)))
            {
                cases.Add(ParseCase());
            }
            ExpectToken(typeof(EndToken));
            ExpectToken(typeof(CaseToken));
            return new CaseStmt
            {
                Cases = cases
            };
        }

        public Statement ParseAngleAssignmentStmt(Token token)
        {
            List<Expression> indexes = new List<Expression>();
            do
            {
                indexes.Add(ParseExpr(takeGt: true));
            }
            while (indexes.Count < 4 && NextTokenIs(typeof(CommaToken)));
            ExpectToken(typeof(RAngleBracketToken));
            ExpectToken(typeof(EqualToken));
            Expression expr = ParseExpr();
            return new AngleArrayAssignmentStatement
            {
                Indexes = indexes,
                Name = token.Text,
                Expr = expr
            };
        }

        Statement ParseWhileStmt()
        {
            Expression cond = ParseExpr();
            NextTokenIs(typeof(DoToken));
            return new WhileStatement
            {
                Condition = cond
            };
        }

        Statement ParseUntilStmt()
        {
            Expression cond = ParseExpr();
            NextTokenIs(typeof(DoToken));
            return new UntilStatement
            {
                Condition = cond
            };
        }

        Statement ParseEquStatement(Token token)
        {
            Expression id = ParseExpr();
            ExpectToken(typeof(ToToken));
            Expression val = ParseExpr();
            return new EquStatemnet
            {
                Id = id,
                Val = val
            };
        }


        Statement ParseFunctionCallStmt(Token token)
        {
            _nextTokenIndex -= 2;
            Expression funEx = ParseExpr();
            return new FunctionCallStatement
            {
                Expr = funEx
            };
        }

        Statement ParseLoopRepeatStmt()
        {
            List<Statement> statements = new List<Statement>();
            NextTokenIs(typeof(NewLineToken));
            statements = ParseStmts(() => NextTokenIs(typeof(RepeatToken)), inLoop: true);
            return new LoopRepeatStatement
            {
                Statements = statements
            };
        }

        public Statement ParseMatAssignmentStmt()
        {
            if (!NextTokenIs(out Token matrix, typeof(IdentifierToken)))
            {
                throw new InvalidOperationException("Expected identifier.");
            }

            ExpectToken(typeof(EqualToken));
            Expression expr = null;
            Token otherMatrix = null;
            expr = ParseExpr();
           // if (expectTerminator) ExpectStatementEnd();
            return new MatAssignmentStatement
            {
                Name = matrix as IdentifierToken,
                Expr = expr,
                OtherMatrix = otherMatrix as IdentifierToken
            };
        }

        public Statement ParseForLoopStmt()
        {
            List<Statement> statements = new List<Statement>();
            Token startId = GetNextToken();
            if (startId is not IdentifierToken)
            {
                throw new InvalidOperationException("Identifier expected.");
            }
            ExpectToken(typeof(EqualToken));

            AssignmentStatement index = ParseAssignmentStmt(startId);
            ExpectToken(typeof(ToToken));
            Expression end = ParseExpr();
            Expression step = null;


            if (NextTokenIs(typeof(StepToken))) {
                step = ParseExpr();
            }

            if (!NextTokenIs(typeof(NextToken))){
                ExpectStatementEnd();
                statements = ParseStmts(() => NextTokenIs(typeof(NextToken)), inLoop: true);

                if (!(PeekNextToken() is NewLineToken || PeekNextToken() is EofToken))
                {
                    // Ignore whatever junk is after the next statement...
                    ParseExpr();
                }
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

            if (!NextTokenIs(out Token variable, typeof(IdentifierToken))) {
                throw new InvalidOperationException("Expected identifier.");
            }

            if (NextTokenIs(typeof(CommaToken)))
            {
                if (!NextTokenIs(out value, typeof(IdentifierToken)))
                {
                    throw new InvalidOperationException("Expected identifier.");
                }
            }

            if (NextTokenIs( typeof(UsingToken)))
            {
                cursor = ParseExpr();
            }

            (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock();

            return new ReadNextStatement
            {
                Cursor = cursor,
                Value = value?.Text,
                Variable = variable.Text,
                Else = elseBlock,
                Then = thenBlock
            };

        }

        Statement ParseRemoveStmt()
        {
            if (!NextTokenIs(out Token var, typeof(IdentifierToken))){
                throw new InvalidOperationException("Expected identifier.");
            }
            ExpectToken(typeof(FromToken));
            Expression from = ParseExpr();
            ExpectToken(typeof(AtToken));
            if (!NextTokenIs(out Token pos, typeof(IdentifierToken)))
            {
                throw new InvalidOperationException("Expected identifier.");
            }
            ExpectToken(typeof(SettingToken));
            if (!NextTokenIs(out Token flag, typeof(IdentifierToken)))
            {
                throw new InvalidOperationException("Expected identifier.");
            }
            return new RemoveStatement
            {
                Var = var.Text,
                From = from,
                Pos = pos.Text,
                Flag = flag.Text
            };

        }

        public Statement ParseSquareBracketArrayAssignmentStmt(Token token)
        {
            List<Expression> indexes = new List<Expression>();
            do
            {
                indexes.Add(ParseExpr());
            }
            while (indexes.Count < 2 && NextTokenIs(typeof(CommaToken)));
            ExpectToken(typeof(RSqrBracketToken));
            ExpectToken(typeof(EqualToken));
            Expression expr = ParseExpr();
            return new SquareBracketArrayAssignmentStatement
            {
                Indexes = indexes,
                Name = token.Text,
                Expr = expr
            };
        }

        Statement ParseReturnStmt()
        {
            Expression expr = ParseExpr(optional: true);
            return new ReturnStatement
            {
                Expr = expr
            };
        }


        public Statement ParseSwapStmt()
        {
            Expression old_val = ParseExpr();
            ExpectToken(typeof(WithToken));
            Expression new_val = ParseExpr();
            ExpectToken(typeof(InToken));
            if (!NextTokenIs(out Token token, typeof(IdentifierToken))){
                throw new InvalidOperationException("Expected idenftier");
            }
            return new SwapStatement
            {
                Name = token.Text,
                New = new_val,
                Old = old_val
            };
        }

        public Statement ParseInternalSubStmt(Token token)
        {
            ExpectStatementEnd();
            List<Statement> statements = ParseStmts(x => x.Count > 0 &&  x.Last() is ReturnStatement 
                || PeekNextToken() is EofToken);   
            return new InternalSubStatement
            {
                Name = token.Text,
                Statements = statements
            };
        }

        Statement ParseGosubStmt()
        {
            Token token = GetNextToken();
            if (token is IdentifierToken)
            {
                return new GosubStatement
                {
                    Name = token.Text
                };
            } else
            {
                throw new InvalidOperationException("Identifer expected.");
            }
        }

        Statement ParsePlusAssignmentStmt(Token token)
        {

            Expression expr = ParseExpr();
            return new PlusAssignmentStatement
            {
                Expr = expr,
                Name = token.Text
            };
        }

        Statement ParseInsertStmt()
        {
            if (NextTokenIs( out Token token, typeof(IdentifierToken)))
            {
                return new InsertStatement
                {
                    Name = token.Text
                };
            }
            else
            {
                throw new InvalidOperationException("Identifed expiected.");
            }
        }

        Statement ParseMinusAssignmentStmt(Token token)
        {
            Expression expr = ParseExpr();
            return new MinusAssignmentStatement
            {
                Expr = expr,
                Name = token.Text
            };
        }

        Statement ParseDivideAssignmentStmt(Token token)
        {

            Expression expr = ParseExpr();
            return new DivideAssignmentStatement
            {
                Expr = expr,
                Name = token.Text
            };
        }
        Statement ParseMulAssignmentStmt(Token token)
        {

            Expression expr = ParseExpr();
            return new MulAssignmentStatement
            {
                Expr = expr,
                Name = token.Text
            };
        }

       

        Statement ParseConvertStmt()
        {
            Expression from = ParseExpr();
            ExpectToken(typeof(ToToken));
            Expression to = ParseExpr();
            ExpectToken(typeof(InToken));
            Expression in_part = ParseExpr();
           
            return new ConvertStatement
            {
                From = from,
                To = to,
                In = in_part
            };
        }



        Statement ParseConcatAssignmentStmt(Token token)
        {
            Expression expr = ParseExpr();
            return new ConcatAssignmentStatement
            {
                Expr = expr,
                Name = token.Text
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
                expr = funcExpr
            };
        }


        public List<Statement> ParseStmts(Func<bool> stop, bool inLoop = false)
        {
            Func<List<Statement>, bool> sto = (_) => stop();
            return ParseStmts(sto, inLoop: inLoop);
        }

        public List<Statement> ParseStmts( Func<List<Statement>, bool> stop, bool inLoop = false)
        {
            List<Statement> statements = new List<Statement>();
            while (_nextTokenIndex < _tokens.Count)
            {
                if (stop(statements))
                {
                    break;
                }

    
                if (statements.Count() >= 1)
                {
                    ExpectStatementEnd();
                }


                if (stop(statements))
                {
                    break;
                }

                int lineNo = GetLineNo();
                statements.Add(ParseStmt(inLoop: inLoop));
                statements.Last().LineNo = lineNo;
            }
            return statements;
        }

        int GetLineNo()
        {
            return PeekNextToken().LineNo;
        }

        Statement ParseTransferStmt()
        {
            if (!NextTokenIs(out Token from, typeof(IdentifierToken)))
            {
                throw new InvalidOperationException("Identifier Expected");
            }
            ExpectToken(typeof(ToToken));
            if (!NextTokenIs(out Token to, typeof(IdentifierToken)))
            {
                throw new InvalidOperationException("Identifier Expected");

            }

            return new TransferStatement
            {
                From = from.Text,
                To = to.Text
            };
        }


        Statement ParseOsReadStmt()
        {
            if (!NextTokenIs(out Token var, typeof(IdentifierToken))){
                throw new InvalidOperationException("Identifier expiected.");
            }
            ExpectToken(typeof(FromToken));
            Expression filePath = ParseExpr();
            (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock();

            return new OsReadStatement
            {
                Variable = var.Text,
                ElseBlock = elseBlock,
                FilePath = filePath,
                ThenBlock = thenBlock
            };
        }

        Statement ParseDimStmt(Token token)
        {
            List<Matrix> matricies = new();

            do
            {
                if (!NextTokenIs(out Token matVar, typeof(IdentifierToken)))
                {
                    throw new InvalidOperationException("Expected identifier");
                }
                ExpectToken(typeof(LParenToken));
                Expression row = ParseExpr();
                Expression col = null;
                if (NextTokenIs(typeof(CommaToken)))
                {
                    col = ParseExpr();
                }
                ExpectToken(typeof(RParenToken));
                matricies.Add(new Matrix { Col = col, Row = row, Name = matVar.Text });

            } while (NextTokenIs(typeof(CommaToken)));

            return new DimStatement
            {
                matricies = matricies
            };
        }

        Statement ParsePragmaStmt()
        {
            PragmaOption opt;
            if (NextTokenIs(typeof(OutputToken))){
                opt = PragmaOption.Output;
            } else if (NextTokenIs(typeof(PreCompToken)))
            {
                opt = PragmaOption.PreComp;
            }
            else
            {
                throw new InvalidOperationException("Expected PRECOMP or OUTPUT");
            }

            if (!NextTokenIs(out Token option, typeof(IdentifierToken))){
                throw new InvalidOperationException("Expected identifier.");
            }

            return new PragmaStatement
            {
                Keyword = opt,
                Option = option.Text
            };
        }

        Statement ParseOsWriteStmt()
        {
            Expression expr = ParseExpr();
            if (!NextTokenIs(typeof(ToToken), typeof(OnToken))){
                throw new InvalidOperationException("To or On keyword expected");
            }
            Expression location = ParseExpr();
            return new OsWriteStatement
            {
                Expr = expr,
                Location = location
            };
        }

        Statement ParseCommonStmt()
        {
            if (!NextTokenIs(out Token CommonNameToken, typeof(CommonNameToken))){
                throw new InvalidOperationException("Expected common block name.");
            }

            List<string> globalVars = new();

            do
            {
                if (!NextTokenIs(out Token name, typeof(IdentifierToken))){
                    throw new InvalidOperationException("identifier expected.");
                }
                globalVars.Add(name.Text);

            } while (NextTokenIs(typeof(CommaToken)));

            return new CommonStatement
            {
                CommonName = CommonNameToken.Text,
                GlovalVars = globalVars
            };
        }


        Statement ParseInitRndStmt()
        {
            Expression expr = ParseExpr();

            return new InitRndStatement
            {
                Expr = expr
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
            if (!NextTokenIs(out Token variable, typeof(IdentifierToken)))
            {
                throw new InvalidOperationException("Identified Expected");
            }
            ExpectToken(typeof(FromToken));
            Expression cursor = null;
            Expression tableVar = null;
            if (NextTokenIs(typeof(CursorToken)))
            {
                cursor = ParseExpr();
            } else
            {
                tableVar = ParseExpr();
            }
            ExpectToken(typeof(CommaToken));
            Expression key = ParseExpr();
         
            (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock();
            return new ReadOStatement
            {
                Key = key,
                TableVar = tableVar,
                Variable = variable.Text,
                Else = elseBlock,
                Then = thenBlock,
                Cursor = cursor
            };
        }



        Statement ParseReadVStmt()
        {
            if (!NextTokenIs(out Token variable, typeof(IdentifierToken))){
                throw new InvalidOperationException("Identified Expected");
            }
            ExpectToken(typeof(FromToken));
            Expression tableVar = ParseExpr();
            ExpectToken(typeof(CommaToken));
            Expression key = ParseExpr();
            ExpectToken(typeof(CommaToken));
            Expression col = ParseExpr();

            (List<Statement> thenBlock,List<Statement> elseBlock)  = ParseThenElseBlock();
            return new ReadVStatement
            {
                Column  = col,
                Key = key,
                TableVar = tableVar,
                Variable = variable.Text,
                Else = elseBlock,
                Then = thenBlock
            };
        }

        Statement ParseMatParseStmt()
        {

            if (!NextTokenIs(out Token variable, typeof(IdentifierToken))){
                throw new InvalidOperationException("Identifier expecited");
            }
            ExpectToken(typeof(IntoToken));
            if (!NextTokenIs(out Token matrixVar, typeof(IdentifierToken)))
            {
                throw new InvalidOperationException("Identifier expecited");
            }
            Expression delim = null;
            if (NextTokenIs(typeof(UsingToken)))
            {
                delim = ParseExpr();
            }


            return new MatParseStatement
            {
                Delim = delim,
                Matrix = matrixVar.Text,
                Variable = variable.Text
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
            if (!NextTokenIs(typeof(OnToken), typeof(ToToken)))
            {
                throw new InvalidOperationException("Expected on or to");
            }
            Expression handle = ParseExpr();
            ExpectToken(typeof(CommaToken));
            Expression key = ParseExpr();
            ExpectToken(typeof(CommaToken));
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
            if(!NextTokenIs(out Token label, typeof(IdentifierToken))){
                throw new InvalidOperationException("Expected identifier");
            }


            return new FreeCommonStatement
            {
                Label = label.Text
            };
        }

        Statement ParseOsCloseStmt()
        {
            if(!NextTokenIs(out Token variable, typeof(IdentifierToken)))
            {
                throw new InvalidOperationException("Expected identifier.");
            }

            return new OsCloseStatement
            {
                FileVar = variable.Text
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
            ExpectToken(typeof(OnToken),typeof(ToToken));
            if(!NextTokenIs(out Token fileVar, typeof(IdentifierToken)))
            {
                throw new InvalidOperationException("expected identifier");
            }
            ExpectToken(typeof(AtToken));
            Expression byt = ParseExpr();
            return new OsBWriteStatement
            {
                Byte = byt,
                Expr = expr,
                FileVar = fileVar.Text
            };
        }

        Statement ParseOsBReadStmt()
        {
            if (!NextTokenIs(out Token variable, typeof(IdentifierToken)))
            {
                throw new InvalidOperationException("expected identifier");
            }
            ExpectToken(typeof(FromToken));
            if (!NextTokenIs(out Token fileVar, typeof(IdentifierToken)))
            {
                throw new InvalidOperationException("expected identifier");
            }
            ExpectToken(typeof(AtToken));
            Expression byt = ParseExpr();
            ExpectToken(typeof(LengthToken));
            Expression length = ParseExpr();
            return new OsBreadStatement
            {
                Byte = byt,
                FileVar = fileVar.Text,
                Length = length,
                Variable = variable.Text
            };
        }

        Statement ParseBRemoveStmt()
        {
            if (!NextTokenIs(out Token var, typeof(IdentifierToken)))
            {
                throw new InvalidOperationException("Expected identifier.");
            }
            ExpectToken(typeof(FromToken));
            Expression from = ParseExpr();
            ExpectToken(typeof(AtToken));
            if (!NextTokenIs(out Token pos, typeof(IdentifierToken)))
            {
                throw new InvalidOperationException("Expected identifier.");
            }
            ExpectToken(typeof(SettingToken));
            if (!NextTokenIs(out Token flag, typeof(IdentifierToken)))
            {
                throw new InvalidOperationException("Expected identifier.");
            }
            return new BRemoveStatement
            {
                Var = var.Text,
                From = from,
                Pos = pos.Text,
                Flag = flag.Text
            };


        }

        public Statement ParseStmt(bool inLoop = false)
        {
            Token token = GetNextToken();
            if (token is IdentifierToken)
            {
                if (NextTokenIs(typeof(EqualToken)))
                {
                    return ParseAssignmentStmt(token);

                } else if (NextTokenIs(typeof(LAngleBracketToken))) {

                    return ParseAngleAssignmentStmt(token);
                }
                else if ( token.DisallowFunction == false && NextTokenIs(typeof(LSqrBracketToken)))
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
                else if (token.DisallowFunction == false && NextTokenIs(typeof(LParenToken)))
                {
                    return ParseFunctionCallStmt(token);
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
                    return ParseMatAssignmentStmt();
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
                    return ParseJumpStmt();
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
            } else if (token is PragmaToken)
            {
                return ParsePragmaStmt();
            } 
            else if (token is EndToken)
            {
                return ParseEndStmt();
            }
            throw new InvalidOperationException($"Not a valid statement {token}");
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
            ExpectToken(typeof(ToToken));
            if(!NextTokenIs(out Token variable, typeof(IdentifierToken))){
                throw new InvalidOperationException("Expected identifier.");
            }

            (List<Statement> thenBlock, List<Statement> elseBlock) = ParseThenElseBlock();

            return new OsOpenStatement
            {
                FilePath = filePath,
                Variable = variable.Text,
                ElseBlock = elseBlock,
                ThenBlock = thenBlock
            };
        }

        Expression ParseExpr(bool takeGt = false, bool optional = false)
        {
            if (IsStatementEnd() && optional)
            {
                return null;
            }

            int tokenIndex = _nextTokenIndex;
            Expression expr = null;
            
            try
            {
                expr = ParseLogExpr(takeGt: takeGt);
            }
            catch
            {
                if (tokenIndex + 1== _nextTokenIndex  && optional)
                {
                    _nextTokenIndex = tokenIndex;
                    return null;
                }
                else
                {
                    throw;
                }
            }

            Token optoken;
            while (NextTokenIs(out optoken, typeof(AndToken), typeof(OrToken), typeof(MatchesToken)))
            {
                Expression right = this.ParseLogExpr(takeGt: takeGt);


                if (optoken is OrToken)
                {
                    expr = new OrExpression(optoken, expr, right);
                }
                else if (optoken is MatchesToken)
                {
                    expr = new MatchesExpression(optoken, expr, right);
                }
                else
                {
                    expr = new AndExpression(optoken, expr, right);
                }
            }
            return expr;
        }


        Expression ParseLogExpr(bool takeGt = false)
        {
            Expression expr = ParseConcatExpr();
            Token optoken;
            while (NextTokenIs( out optoken, typeof(LAngleBracketToken), typeof(RAngleBracketToken), typeof(EqualToken), typeof(ExcalmEqToken),
                typeof(HashTagToken), typeof(GeToken), typeof(LteToken),typeof(EqToken),typeof(NeToken),
                typeof(LtToken),typeof(LeToken),typeof(GtToken),typeof(EqcToken),typeof(NecToken),
                typeof(LtcToken),typeof(LecToken),typeof(GtcToken),typeof(GecToken),typeof(EqxToken),
                typeof(NexToken),typeof(LtxToken),typeof(GtxToken),typeof(LexToken),typeof(GexToken)))
            {
                if (optoken is RAngleBracketToken && takeGt)
                {
                    _nextTokenIndex -= 1;
                    return expr;
                }
                Expression right;

                if  (optoken is LAngleBracketToken && NextTokenIs(typeof(RAngleBracketToken))){
                    right = ParseConcatExpr();
                    expr = new NotEqExpression(optoken, expr, right);
                    continue;
                }

                if (optoken is RAngleBracketToken && NextTokenIs(typeof(EqualToken)))
                {
                    right = ParseConcatExpr();
                    expr = new GtEqExpression(optoken, expr, right);
                    continue;
                }

                right = ParseConcatExpr();
                
                if (optoken is LAngleBracketToken || optoken is LtToken || optoken is LteToken || optoken is LtxToken)
                {
                  expr = new LtExpression(optoken, expr, right);
                }
                else if (optoken is RAngleBracketToken || optoken is GtToken || optoken is GtxToken || optoken is GtcToken)
                {
                    expr = new GtExpression(optoken, expr, right);
                }
                else if (optoken is ExcalmEqToken || optoken is HashTagToken || optoken is NeToken || optoken is NexToken || optoken is NecToken )
                {
                    expr = new NotEqExpression(optoken, expr, right);
                }
                else if (optoken is LteToken || optoken is LeToken || optoken is LexToken || optoken is LecToken)
                {
                    expr = new LtEqExpression(optoken, expr, right);
                }
                else if (optoken is GeToken || optoken is GecToken || optoken is GexToken)
                {
                    expr = new GtEqExpression(optoken, expr, right);
                }
                else
                {
                    expr = new EqExpression(optoken, expr, right);
                }
            }
            return expr;
        }

        Expression ParseConcatExpr()
        {
            Expression expr = ParseAddExpr();
            Token optoken;
            while (NextTokenIs(out optoken, typeof(ColonToken), typeof(MultiValueConcatToken)))
            {
                Expression right = this.ParseAddExpr();

                if (optoken is MultiValueConcatToken)
                {
                    expr = new MultiValueConcatExpression(optoken, expr, right);

                }
                else
                {
                    expr = new ConcatExpression(optoken, expr, right);
                }
            }
            return expr;
        }


        Expression ParseAddExpr()
        {
            Expression expr = ParseMulExpr();
            Token optoken;
            while (NextTokenIs(out optoken, typeof(PlusToken), typeof(MinusToken),
                typeof(MultiValueSubToken),typeof(MultiValueAddToken))) {
                Expression right = this.ParseMulExpr();
                if (optoken is PlusToken)
                {
                    expr = new AddExpression(optoken, expr, right);
                }
                else if(optoken is MultiValueAddToken)
                {
                    expr = new MultiValueAddExpression(optoken, expr, right);
                }
                else if(optoken is MultiValueSubToken)
                {
                    expr = new MultiValueSubExpression(optoken, expr, right);
                }
                else
                {
                    expr = new SubExpression(optoken, expr, right);
                }
            }
            return expr;
        }

        Expression ParseArrayInitExpression(Token token)
        {
            List<Expression> indexes = new List<Expression>();
            do
            {
                indexes.Add(ParseExpr());

            } while (NextTokenIs(typeof(CommaToken)));
            ExpectToken(typeof(RSqrBracketToken));
            return new ArrayInitExpression(token, indexes.ToArray());


        }


        Expression ParseMulExpr()
        {
            Expression expr = ParsePowerExpr();
            Token optoken;
            while (NextTokenIs(out optoken, typeof(StarToken), typeof(SlashToken),
                typeof(MultiValueMullToken),typeof(MultiValueDivToken))) {
                Expression right = ParsePowerExpr();
                if (optoken is StarToken)
                {
                    expr = new MulExpression(optoken, expr, right);
                }
                else if (optoken is MultiValueDivToken)
                {
                    expr = new MultiValueDivExpression(optoken, expr, right);
                }
                else if (optoken is MultiValueMullToken)
                {
                    expr = new MultiValueMullExpression(optoken, expr, right);
                }
                else
                {
                    expr = new DivExpression(optoken, expr, right);
                }
            }
            return expr;
        }

        Expression ParsePowerExpr()
        {
            Expression expr = ParseAtom();
            Token optoken;
            while (NextTokenIs(out optoken, typeof(PowerToken)))
            {
                Expression right = ParseAtom();
                expr = new PowerExpression(optoken, expr, right);
            }
            return expr;
        }

        Expression ParseIfExpression(Token token)
        {
            Expression thenBlock = null;
            Expression elseBlock = null;
            Expression cond = ParseExpr();
            if (NextTokenIs(typeof(ThenToken))){
                thenBlock = ParseExpr();
            }
            if (NextTokenIs(typeof(ElseToken))){
                elseBlock = ParseExpr();
            }
            return new IfExpression(token, cond, thenBlock, elseBlock);
        }



        Expression ParseAngleArray(Token token, Expression baseExpr)
        {

            if (PeekNextToken() is RAngleBracketToken)
            {
                _nextTokenIndex -= 1;
                return null;
            }


            int tokenPos = _nextTokenIndex - 1;
            List<Expression> indexes = new List<Expression>();
            do
            {
                indexes.Add(ParseExpr(takeGt: true));

            } while (indexes.Count < 4 && NextTokenIs(typeof(CommaToken)));

            if (PeekNextToken() is RAngleBracketToken)
            {
                _nextTokenIndex += 1;
                indexes.Insert(0, baseExpr);
                return new AngleArrExpression(token, indexes.ToArray());
            }
            else
            {
                // We assumed that we had an array, but we do not.
                _nextTokenIndex = tokenPos;
                return null;
            }
        }


        Expression ParseSqrBracketArray(Token token, Expression expr)
        {
            List<Expression> indexes = new List<Expression>();
            do
            {
                indexes.Add(ParseExpr());

            } while (indexes.Count < 2 && NextTokenIs(typeof(CommaToken)));
            ExpectToken(typeof(RSqrBracketToken));
            indexes.Insert(0, expr);
            return new SqrArrExpression(token, indexes.ToArray());
        }

        Expression ParseFunc(Token token)
        {
            List<Expression> args = new List<Expression>();
            while(!NextTokenIs(typeof(RParenToken)) && args.Count < MAX_PARAMS)
            {
                args.Add(ParseExpr());
                NextTokenIs(typeof(CommaToken));

            } 
            return new FuncExpression(token, args.ToArray());
        }

        Expression ParseAtom()
        {
            Expression expr;
            NextTokenIs(typeof(MatToken));
            Token token = GetNextToken();

            if (token is IdentifierToken && token.DisallowFunction == false)
            {
                if (NextTokenIs(typeof(LParenToken)))
                {
                    expr = ParseFunc(token);
                }
                else
                {
                    expr = new IdExpression(token);
                }

            }
            else if(token is IfToken)
            {
                expr = ParseIfExpression(token);
            }
            else if (token is NumberToken)
            {
                expr = new NumExpression(token);
            }

            else if (token is StringToken)
            {
                expr = new StringExpression(token);
            }
            else if (token is LParenToken)
            {
                expr = ParseExpr();
                ExpectToken(typeof(RParenToken));
            }
            else if (token is LCurlyToken)
            {
                expr = ParseExpr();
                ExpectToken(typeof(RCurlyToken));
                expr = new CurlyExpression(token, expr);
            }
            else if (token is MinusToken) {
                expr = new NegateExpression(token, ParseAtom());
            }
            else if (token is NullToken)
            {
                expr = new NullExpression(token);
            }
            else if (token is LSqrBracketToken)
            {
                expr = ParseArrayInitExpression(token);
            }
            else
            {
                throw new InvalidOperationException("Expected identifier, number, function, or parenthesized expression.");
            }

            while (NextTokenIs(out Token optoken, typeof(LAngleBracketToken), typeof(LSqrBracketToken)))
            {
                Expression arrayExpr = null;
                if (optoken is LAngleBracketToken)
                {
                    arrayExpr = ParseAngleArray(token, expr);
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


        Token PeekNextToken()
        {
            if (_nextTokenIndex < _tokens.Count)
            {
                return _tokens[_nextTokenIndex];
            }
            return null;
        }

        Token GetNextToken()
        {
            if (_nextTokenIndex < _tokens.Count)
            {
                return _tokens[_nextTokenIndex++];
            }
            throw new InvalidOperationException("Unexpected end of program.");
        }


        Token ExpectToken(Type expectedToken)
        {
            Token token = PeekNextToken();
            if (token.GetType() != expectedToken)
            {
                throw new InvalidOperationException($"Expected {expectedToken}");    
            }
            return _tokens[_nextTokenIndex++];
        }

        Token ExpectToken(params Type[] validTokens)
        {
            Token token = PeekNextToken();
            if(!validTokens.Any(x=>x == token.GetType()))
            {
                throw new InvalidOperationException($"Expected {validTokens}");

            }
            return _tokens[_nextTokenIndex++];
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

        bool NextTokenIs(params Type[] validTokens)
        {
            return NextTokenIs(out Token token, validTokens);
        }

        void ExpectStatementEnd()
        {
            if (PeekNextToken() is EofToken)
            {
                return;
            }

            if (NextTokenIs(typeof(SemiColonToken))){
                if (NextTokenIs(typeof(NewLineToken))) {
                }

            } else if (NextTokenIs(typeof(NewLineToken)))
            {
                //Good
            } else
            {
                throw new InvalidOperationException("Statement not terminated properly");
            }

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
    }
}
