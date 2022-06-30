using System;
using System.Collections.Generic;
using BasicPlusParser.Tokens;
using System.Linq;

namespace BasicPlusParser
{
    public class Tokenizer
    {
        readonly string _source;
        int _pos = 0;
        int _col = 0;
        int _lineNo = 1;
        List<Token> _tokens = new();
        List<Token> _commentTokens = new();
        Token _prevToken => _tokens.Count > 0 ? _tokens.Last() : null;
        readonly ParseErrors _tokenErrors;

        public Tokenizer(string text, ParseErrors error = null)
        {
            _tokenErrors = error ?? new ParseErrors();
            _source = text;
        }

        public TokenizerOutput Tokenise()
        {
            while (!IsAtEnd())
            {
                int startPos = _pos;
                int startLineNo = _lineNo;
                int startCol = _col;
                Token matchedToken = GetNextToken(_pos);
                if (matchedToken != null)
                {
                    matchedToken.LineNo = startLineNo;
                    matchedToken.Pos = startPos;
                    matchedToken.StartCol = startCol;
                    matchedToken.EndCol = _col;
                    matchedToken.EndLineNo = _lineNo;

                    if (matchedToken is WhiteSpaceToken && (_prevToken is IfToken || _prevToken is ReturnToken ||
                        _prevToken is WhileToken || _prevToken is UntilToken))
                    {
                        // Basic + allows some keywords to be function names.
                        // However, if the keyword is followed by space,
                        // it does not allow the keyword to be a function name.
                        _prevToken.DisallowFunction = true;
                    }

                    if (matchedToken is WhiteSpaceToken)
                    {
                        continue;
                    }
                    else if (matchedToken is NewLineToken && _prevToken is NewLineToken)
                    {
                        continue;
                    }
                    else if (matchedToken is CommentToken)
                    {
                        _commentTokens.Add(matchedToken);
                    }
                    else
                    {
                        _tokens.Add(matchedToken);
                    }
                }
            }
       
            _tokens.Add(new EofToken { LineNo = _lineNo, Pos = _source.Length, StartCol = _col, EndCol = _col, EndLineNo = _lineNo });
            
            return new TokenizerOutput
            {
                Tokens = _tokens,
                CommentTokens = _commentTokens
            };
        }

        public Token GetNextToken(int start)
        {
            char character = Advance();
            switch (character)
            {
                case '=':
                    return new EqualToken { Text = character.ToString() };
                case '"':
                    return ScanStringLiteral('"');
                case '\'':
                    return ScanStringLiteral('\'');
                case '<':
                    if (Match("=")) return new LteToken { Text = "<=" };
                    else return new LAngleBracketToken { Text = character.ToString() };
                case '#':
                    if (Match("pragma")) return new PragmaToken { Text = "#pragma" };
                    else return new HashTagToken { Text = character.ToString() };
                case '+':
                    if (Match("++")) return new MultiValueAddToken { Text = "+++" };
                    else if (Match('=')) return new PlusEqualToken { Text = "+=" };
                    else return new PlusToken { Text = character.ToString() };
                case '-':
                    if (Match("--")) return new MultiValueSubToken { Text = "---" };
                    else if (Match("=")) return new MinusEqualToken { Text = "-=" };
                    else return new MinusToken { Text = character.ToString() };
                case '/':
                    if (StartOfStmt() && Match("/")) return ScanSingleLineComment();
                    else if (Match("//")) return new MultiValueDivToken { Text = "///" };
                    else if (Match("=")) return new SlashEqualToken { Text = "/=" };
                    else if (Match("*")) return ScanMultiLineComment();
                    else return new SlashToken { Text = character.ToString() };
                case '*':
                    if (StartOfStmt()) return ScanSingleLineComment();
                    else if (Match("=")) return new StarEqualToken { Text = "*=" };
                    else if (Match("**")) return new MultiValueMullToken { Text = "***" };
                    else if (Match("*")) return new PowerToken { Text = "**" };
                    else return new StarToken { Text = character.ToString() };
                case '!':
                    if (Match("=")) return new ExcalmEqToken { Text = "!=" };
                    else if (StartOfStmt()) return ScanSingleLineComment();
                    else return new ExclamToken { Text = character.ToString() };
                case ':':
                    if (Match("::")) return new MultiValueConcatToken { Text = ":::" };
                    else if (Match("=")) return new ColonEqualToken { Text = ":=" };
                    else return new ColonToken { Text = character.ToString() };
                case '>':
                    return new RAngleBracketToken { Text = character.ToString() };
                case ',':
                    return new CommaToken { Text = character.ToString() };
                case ']':
                    return new RSqrBracketToken { Text = character.ToString() };
                case '[':
                    return new LSqrBracketToken { Text = character.ToString() };
                case ')':
                    return new RParenToken { Text = character.ToString() };
                case '(':
                    return new LParenToken { Text = character.ToString() };
                case ';':
                    return new SemiColonToken { Text = character.ToString() };
                case '{':
                    return new LCurlyToken { Text = character.ToString() };
                case '}':
                    return new RCurlyToken { Text = character.ToString() };
                case '$' when Match("insert"):
                    return new InsertToken { Text = "$insert" };
                case '\\':
                    return ScanNumber();
                case '.':
                    return ScanNumber();
                case '@':
                    return ScanSystemVariableOrAtOperator();
                case '\r':
                    Match('\n');
                    IncrementLineNo();
                    while (Match("\r\n"))
                    {
                        IncrementLineNo();
                    }
                    return new NewLineToken { Text = _source[start.._pos] };
                case ' ':
                case '\t':
                    while (Match(' ') || Match('\t')) ;
                    return new WhiteSpaceToken { Text = _source[start.._pos] };
                default:
                    if (IsIdentifierOrKeyWord(character))
                    {
                        return ScanIdentifierOrKeyword();
                    }
                    else if (IsNumber(character))
                    {
                        return ScanNumber();
                    }
                    else
                    {
                        _tokenErrors.ReportError(_lineNo, $"Unmatched character.",_col,_col);
                        return null;
                    }
            }
        }

        bool IsIdentifierOrKeyWord(char chr)
        {
            return (chr >= 'a' && chr <= 'z') ||
                    (chr >= 'A' && chr <= 'Z');
        }

        bool IsNumber(char chr, bool hexAllowed = false)
        {
            return chr >= '0' && chr <= '9' ||
                (hexAllowed && (chr >= 'a' && chr <= 'f') || (chr >= 'A' && chr <= 'F'));
        }

        bool StartOfStmt()
        {
            return (_prevToken is NewLineToken || _prevToken is SemiColonToken || _prevToken == null);
        }

        CommentToken ScanSingleLineComment()
        {
            int start = _pos -1;
            while (Peek() != '\r' && !IsAtEnd())
            {
                char chr = Advance();
            }
            return new CommentToken { Text = _source[start.._pos] };
        }

        CommentToken ScanMultiLineComment()
        {
            int start = _pos-2;
            while (!IsAtEnd())
            {
                char chr = Advance();
                if (chr == '\r') IncrementLineNo();
                if (chr == '*' && Match('/')) {
                    return new CommentToken { Text = _source[start.._pos] };
                }
            }

            _tokenErrors.ReportError(_lineNo, "Multiline comment must be delimited with */",_col,_col);
            return null;
        }

        StringToken ScanStringLiteral(char delim)
        {
            int start = _pos - 1;
            List<char> chars = new();
            while (Peek() != delim && !IsAtEnd())
            {
                char chr = Advance();

                if (chr == '\r')
                {
                    if (chars.Count > 1 && chars.Last() == '|')
                    {
                        chars.RemoveAt(chars.Count - 1);
                        Advance();
                        IncrementLineNo();
                        continue;
                    }
                    else
                    {
                        _tokenErrors.ReportError(_lineNo, $"String must be enclosed by {delim}.",_col,_col);
                        break;
                    }
                }
                chars.Add(chr);
            }

            if (!Match(delim))
            {
                _tokenErrors.ReportError(_lineNo, $"String must be enclosed by {delim}.",_col,_col);
            }


            string str = new String(chars.ToArray());
            return new StringToken { Str = str, Delim = delim , Text = _source[start.._pos]};
        }


        Token ScanSystemVariableOrAtOperator()
        {
            int origPos = _pos;
            int origCol = _col;

            int start = _pos - 1;
            while (IsIdentifierOrKeyWord(Peek()) || IsNumber(Peek()) || Peek() == '_' || Peek() == '@' || Peek() == '.' || Peek() == '$')
            {
                Advance();
            }

            string atOperatorOrSystemVariable = _source[start.._pos];

            switch (atOperatorOrSystemVariable.ToLower())
            {
                case "@cursors":
                case "@list_active":
                case "@pri_file":
                case "@query_dict":
                case "@reccount":
                case "@reduction_done":
                case "@rn_counter":
                case "@admin":
                case "@appid":
                case "@appinfo":
                case "@dbid":
                case "@environ_set":
                case "@files.system":
                case "@files_sysdict":
                case "@files_sysenv":
                case "@files_sysobj":
                case "@files_sysptrs":
                case "@lower_case":
                case "lptrhigh":
                case "@lptrwide":
                case "@mdiactive":
                case "@mdiframe":
                case "@printmode":
                case "@station":
                case "@system_tables":
                case "@tables":
                case "@upper_case":
                case "@username":
                case "@volumes":
                case "@window":
                case "@default_stops":
                case "@index.time":
                case "@hfactive":
                case "@page":
                case "@rm":
                case "@fm":
                case "@vm":
                case "@svm":
                case "@tm":
                case "@stm":
                case "@file.error":
                case "@file_error":
                case "@file_error_mode":
                case "@ans":
                case "@dict":
                case "@id":
                case "@mv":
                case "@record":
                case "@user0":
                case "@user1":
                case "@user2":
                case "@user3":
                case "@user4":
                case "@recur0":
                case "@recur1":
                case "@recur2":
                case "@recur3":
                case "@recur4":
                    return new SystemVariableToken { Text = atOperatorOrSystemVariable };
                default:
                    // We're not dealing with a system variabe, so let's return the at operator and put the token position to after the at operator.
                    _pos = origPos;
                    _col = origCol;
                    return new AtOperatorToken { Text = atOperatorOrSystemVariable };
           }
        }

        Token ScanIdentifierOrKeyword()
        {
            int start = _pos - 1;
            while (IsIdentifierOrKeyWord(Peek()) || IsNumber(Peek()) || Peek() == '_'  || Peek() == '@' || Peek() == '.' || Peek() == '$')
            {
                Advance();
            }

            string idOrKeyword = _source[start.._pos];

            return idOrKeyword.ToLower() switch
            {
                "loop" => new LoopToken { Text = idOrKeyword },
                "repeat" => new RepeatToken { Text = idOrKeyword },
                "if" => new IfToken { Text = idOrKeyword },
                "then" => new ThenToken { Text = idOrKeyword },
                "function" => new FunctionToken { Text = idOrKeyword },
                "subroutine" => new SubroutineToken { Text = idOrKeyword },
                "end" => new EndToken { Text = idOrKeyword },
                "else" => new ElseToken { Text = idOrKeyword },
                "begin" => new BeginToken { Text = idOrKeyword },
                "case" => new CaseToken { Text = idOrKeyword },
                "return" => new ReturnToken { Text = idOrKeyword },
                "for" => new ForToken { Text = idOrKeyword },
                "next" => new NextToken { Text = idOrKeyword },
                "while" => new WhileToken { Text = idOrKeyword },
                "until" => new UntilToken { Text = idOrKeyword },
                "gosub" => new GosubToken { Text = idOrKeyword },
                "to" => new ToToken { Text = idOrKeyword },
                "equ" => new EquToken { Text = idOrKeyword },
                "swap" => new SwapToken { Text = idOrKeyword },
                "in" => new InToken { Text = idOrKeyword },
                "with" => new WithToken { Text = idOrKeyword },
                "convert" => new ConvertToken { Text = idOrKeyword },
                "step" => new StepToken { Text = idOrKeyword },
                "declare" => new DeclareToken { Text = idOrKeyword },
                "call" => new CallToken { Text = idOrKeyword },
                "remove" => new RemoveToken { Text = idOrKeyword },
                "from" => new FromToken { Text = idOrKeyword },
                "at" => new AtToken { Text = idOrKeyword },
                "setting" => new SettingToken { Text = idOrKeyword },
                "mat" => new MatToken { Text = idOrKeyword },
                "locate" => new LocateToken { Text = idOrKeyword },
                "using" => new UsingToken { Text = idOrKeyword },
                "null" => new NullToken { Text = idOrKeyword },
                "read" => new ReadToken { Text = idOrKeyword },
                "write" => new WriteToken { Text = idOrKeyword },
                "delete" => new DeleteToken { Text = idOrKeyword },
                "lock" => new LockToken { Text = idOrKeyword },
                "unlock" => new UnlockToken { Text = idOrKeyword },
                "open" => new OpenToken { Text = idOrKeyword },
                "debug" => new DebugToken { Text = idOrKeyword },
                "cursor" => new CursorToken { Text = idOrKeyword },
                "on" => new OnToken { Text = idOrKeyword },
                "clearselect" => new ClearSelectToken { Text = idOrKeyword },
                "readnext" => new ReadNextToken { Text = idOrKeyword },
                "do" => new DoToken { Text = idOrKeyword },
                "all" => new AllToken { Text = idOrKeyword },
                "goto" => new GoToToken { Text = idOrKeyword },
                "transfer" => new TransferToken { Text = idOrKeyword },
                "matread" => new MatReadToken { Text = idOrKeyword },
                "matwrite" => new MatWriteToken { Text = idOrKeyword },
                "oswrite" => new OsWriteToken { Text = idOrKeyword },
                "osread" => new OsReadToken { Text = idOrKeyword },
                "dimension" => new DimensionToken { Text = idOrKeyword },
                "dim" => new DimensionToken { Text = idOrKeyword },
                "output" => new OutputToken { Text = idOrKeyword },
                "precomp" => new PreCompToken { Text = idOrKeyword },
                "common" => new CommonToken { Text = idOrKeyword },
                "freecommon" => new FreeCommonToken { Text = idOrKeyword },
                "initrnd" => new InitRndToken { Text = idOrKeyword },
                "by" => new ByToken { Text = idOrKeyword },
                "select" => new SelectToken { Text = idOrKeyword },
                "equate" => new EquToken { Text = idOrKeyword },
                "garbagecollect" => new GarbageCollectToken { Text = idOrKeyword },
                "flush" => new FlushToken { Text = idOrKeyword },
                "readv" => new ReadVToken { Text = idOrKeyword },
                "reado" => new ReadOToken { Text = idOrKeyword },
                "matparse" => new MatParseToken { Text = idOrKeyword },
                "into" => new IntoToken { Text = idOrKeyword },
                "matches" => new MatchesToken { Text = idOrKeyword },
                "initdir" => new InitDirToken { Text = idOrKeyword },
                "writev" => new WriteVToken { Text = idOrKeyword },
                "compile" => new CompileToken { Text = idOrKeyword },
                "osopen" => new OsOpenToken { Text = idOrKeyword },
                "osdelete" => new OsDeleteToken { Text = idOrKeyword },
                "osclose" => new OsCloseToken { Text = idOrKeyword },
                "osbread" => new OsBReadToken { Text = idOrKeyword },
                "osbwrite" => new OsBWriteToken { Text = idOrKeyword },
                "length" => new LengthToken { Text = idOrKeyword },
                "bremove" => new BRemoveToken { Text = idOrKeyword },
                "insert" => new InsertDeclarationToken { Text = idOrKeyword },
                "ge" => new GeToken { Text = idOrKeyword },
                "ne" => new NeToken { Text = idOrKeyword },
                "lt" => new LtToken { Text = idOrKeyword },
                "le" => new LeToken { Text = idOrKeyword },
                "gt" => new GtToken { Text = idOrKeyword },
                "eq" => new EqToken { Text = idOrKeyword },
                "_eqc" => new EqcToken { Text = idOrKeyword },
                "_nec" => new NecToken { Text = idOrKeyword },
                "_ltc" => new LtcToken { Text = idOrKeyword },
                "_lec" => new LecToken { Text = idOrKeyword },
                "_gtc" => new GtcToken { Text = idOrKeyword },
                "_gec" => new GecToken { Text = idOrKeyword },
                "_eqx" => new EqxToken { Text = idOrKeyword },
                "_nex" => new NextToken { Text = idOrKeyword },
                "_ltx" => new LtxToken { Text = idOrKeyword },
                "_gtx" => new GtxToken { Text = idOrKeyword },
                "_lex" => new LexToken { Text = idOrKeyword },
                "_gex" => new GexToken { Text = idOrKeyword },
                "and" => new AndToken { Text = idOrKeyword },
                "or" => new OrToken { Text = idOrKeyword },
                _ => new IdentifierToken { Text = idOrKeyword }      
            };
        }

        NumberToken ScanNumber()
        {
            int start = _pos - 1;
            bool hexAllowed = (_source[start] == '0' && Match("x") || _source[start] == '\\');
            bool dotAllowed = _source[start] != '.';

            while (!IsAtEnd())
            {
                char chr = Peek();
                if (chr == '.')
                {
                    if (dotAllowed)
                    {
                        dotAllowed = false;
                    } else
                    {
                        _tokenErrors.ReportError(_lineNo, "Number contains more than 1 decimal point.",_col,_col);
                        return null;
                    }
                } else
                {
                    if (!IsNumber(chr,hexAllowed:hexAllowed))
                    {
                        break;
                    }
                }
                Advance();
            }

            if (_source[start] == '\\' && !Match('\\'))
            {
                _tokenErrors.ReportError(_lineNo, "Number must end in \\",_col,_col);
                return null;
            }

            string number = _source[start.._pos];

            if (number.Last() == '.')
            {
                _tokenErrors.ReportError(_lineNo, "Invalid decimal point.",_col,_col);
                return null;
            }

            return new NumberToken { Text = number };
        }

        bool IsAtEnd()
        {
            return _pos >= _source.Length;
        }

        bool Match(char expected)
        {
            if (IsAtEnd()) return false;
            if (_source[_pos] != expected) return false;

            IncrementPos();
            return true;
        }

        char Advance()
        {
            char c = _source[_pos];
            IncrementPos();
            return c;
        }

        bool Match(string expected)
        {
            if (IsAtEnd()) return false;
            if (_source.Substring(_pos).StartsWith(expected, StringComparison.OrdinalIgnoreCase))
            {
                IncrementPos(expected.Length);
                return true;
            }
            return false;
        }

        char Peek()
        {
            if (_pos >= _source.Length) return '\0';
            return _source[_pos];
        }

        void IncrementLineNo()
        {
            _lineNo++;
            _col = 0;
        }

        int IncrementPos(int amount = 1)
        {
            _pos += amount;
            _col += amount;
            return _pos;
        }
    }
}
