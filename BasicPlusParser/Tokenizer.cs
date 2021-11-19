using System;
using System.Collections.Generic;
using BasicPlusParser.Tokens;
using System.Linq;

namespace BasicPlusParser
{
    public class Tokenizer
    {
        int _pos = 0;
        readonly List<Token> _tokens = new();
        readonly string _source;
        Token _prevToken => _tokens.Count > 0 ? _tokens.Last() : null;
        readonly ParseErrors _tokenErrors;
        int _col;
        int _lineNo = 1;

        bool IsAtEnd()
        {
            return _pos >= _source.Length;
        }

        bool Match(char expected)
        {
            if (IsAtEnd()) return false;
            if (_source[_pos] != expected) return false;

            _pos++;
            return true;
        }

        char Advance() {
            return _source[_pos++];
        }

        bool Match(string expected)
        {
            if (IsAtEnd()) return false;
            if (_source.Substring(_pos).StartsWith(expected, StringComparison.OrdinalIgnoreCase)) {
                _pos += expected.Length;
                return true;
            }
            return false;

        }

        char Peek()
        {
            if (_pos >= _source.Length) return '\0';
            return _source[_pos];
        }

        public Tokenizer(string text, ParseErrors error)
        {
            _tokenErrors = error;
            _source = text;
        }

        public List<Token> Tokenise()
        {
            while (!IsAtEnd())
            {
                int start = _pos;
                int startLineNo = _lineNo;
                Token matchedToken = GetNextToken(start);
                if (matchedToken != null)
                {
                    matchedToken.LineNo = startLineNo;
                    matchedToken.Pos = start;
                    //matchedToken.Col = _col;

                    if (matchedToken is WhiteSpaceToken && (_prevToken is IfToken || _prevToken is ReturnToken ||
                        _prevToken is WhileToken || _prevToken is UntilToken))
                    {
                        // Basic + allows keywords to be function names.
                        // However, if the keyword is followed by space,
                        // it does not allow the keyword to be a function name.
                        _prevToken.DisallowFunction = true;
                    }

                    if (matchedToken is WhiteSpaceToken || matchedToken is CommentToken ||
                        (matchedToken is NewLineToken && _prevToken is NewLineToken))
                    {
                        // We don't what junk white spaces, newlines and comments in the token list.
                        continue;
                    }
                    _tokens.Add(matchedToken);
                }
            }
            _tokens.Add(new EofToken { LineNo = _lineNo, Pos = _pos });
            return _tokens;
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
                    if (Match("//")) return new MultiValueDivToken { Text = "///" };
                    else if (Match("=")) return new SlashEqualToken { Text = "/=" };
                    else if (StartOfStmt() && Match("/")) return ScanSingleLineComment();
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
                case '\r':
                    Match('\n');
                    _lineNo++;
                    while (Match("\r\n"))
                    {
                        _lineNo++;
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
                        _tokenErrors.ReportError(_lineNo, $"Unmatched character: {_source[_pos]}");
                        return null;
                    }
            }
        }

        bool IsIdentifierOrKeyWord(char chr)
        {
            return (chr >= 'a' && chr <= 'z') ||
                    (chr >= 'A' && chr <= 'Z') ||
                    chr == '_' || chr == '@';
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
            int start = _pos;
            while (Peek() != '\r' && !IsAtEnd())
            {
                char chr = Advance();
            }
            return new CommentToken { Text = _source[start.._pos] };
        }

        CommentToken ScanMultiLineComment()
        {
            int start = _pos;
            while (!IsAtEnd())
            {
                char chr = Advance();
                if (chr == '\r') _lineNo++;
                if (chr == '*' && Match('/')) {
                    return new CommentToken { Text = _source[start..(_pos - 2)] };
                }
            }

            _tokenErrors.ReportError(_lineNo, "Multiline comment must be delimited with */");
            return null;
        }

        StringToken ScanStringLiteral(char delim)
        {
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
                        _lineNo += 1;
                        continue;
                    }
                    else
                    {
                        _tokenErrors.ReportError(_lineNo, $"String must be terminated by {delim}.");
                        return null;
                    }
                }
                chars.Add(chr);
            }

            if (IsAtEnd())
            {
                _tokenErrors.ReportError(_lineNo, $"String must be terminated by {delim}.");
                return null;
            }

            // Eat the quote.
            Advance();
            return new StringToken { Text = new String(chars.ToArray()), Delim = delim };
        }

        Token ScanIdentifierOrKeyword()
        {
            int start = _pos - 1;
            while (IsIdentifierOrKeyWord(Peek()) || IsNumber(Peek()) || Peek() == '.' || Peek() == '$')
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
                _ => new IdentifierToken { Text = idOrKeyword },
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
                        _tokenErrors.ReportError(_lineNo, "Number contains more than 1 decimal point.");
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
                _tokenErrors.ReportError(_lineNo, "Number must end in \\");
                return null;
            }

            string number = _source[start.._pos];

            if (number.Last() == '.')
            {
                _tokenErrors.ReportError(_lineNo, "Invalid decimal point.");
                return null;
            }

            return new NumberToken { Text = number };
        }
    }
}
