﻿using System;
using System.Collections.Generic;
using BasicPlusParser.Tokens;
using System.Linq;

namespace BasicPlusParser
{
    public class Tokenizer
    {
        readonly string _source;
        readonly string _fileName;

        int _pos = 0;
        int _col = 0;
        int _lineNo = 1;

        List<Token> _tokens = new();
        List<Token> _trivialTokens = new();
        Token _prevToken => _tokens.Any() ? _tokens.Last() : null;
        Token _prevTrivialToken => _trivialTokens.Any() ? _trivialTokens.Last() : null;
        readonly ParseErrors _tokenErrors = new();

        public Tokenizer(string text, string fileName)
        {
            _source = text;
            _fileName = fileName;
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
                    matchedToken.FileName = _fileName;

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
                    else if (matchedToken is NewLineToken && _prevTrivialToken is PipeToken) 
                    {
                        _trivialTokens.Add(matchedToken);
                    }
                    else if (matchedToken is CommentToken || matchedToken is PipeToken)
                    {
                        _trivialTokens.Add(matchedToken);
                    }
                    else
                    {
                        _tokens.Add(matchedToken);
                    }
                }
            }

            _tokens.Add(new EofToken { LineNo = _lineNo, Pos = _source.Length, StartCol = _col, EndCol = _col, EndLineNo = _lineNo, FileName = _fileName });

            return new TokenizerOutput
            {
                Tokens = _tokens,
                TrivalTokens = _trivialTokens,
                TokenErrors = _tokenErrors
            };
        }

        public Token GetNextToken(int start)
        {
            char character = Advance();
            switch (character)
            {
                case '=':
                    if (Match("=", ignoreWhitespace: true)) return new DoubleEqualToken { Text = _source[start.._pos] };
                    return new EqualToken { Text = _source[start.._pos] };
                case '"':
                case '\'':
                    return ScanStringLiteral(_source[start]);
                case '<':
                    if (Match("=", ignoreWhitespace: true)) return new LteToken { Text = _source[start.._pos] };
                    else return new LAngleBracketToken { Text = _source[start.._pos] };
                case '#':
                    if (Match("pragma")) return new PragmaToken { Text = _source[start.._pos] };
                    else return new HashTagToken { Text = _source[start.._pos] };
                case '+':
                    if (Match("++", ignoreWhitespace:true)) return new MultiValueAddToken { Text = _source[start.._pos] };
                    else if (Match("=", ignoreWhitespace: true)) return new PlusEqualToken { Text = _source[start.._pos] };
                    else return new PlusToken { Text = _source[start.._pos] };
                case '-':
                    if (Match("--", ignoreWhitespace: true)) return new MultiValueSubToken { Text = _source[start.._pos] };
                    else if (Match("=", ignoreWhitespace: true)) return new MinusEqualToken { Text = _source[start.._pos] };
                    else return new MinusToken { Text = _source[start.._pos] };
                case '/':
                    if (StartOfStmt() && Match("/")) return ScanSingleLineComment();
                    else if (Match("//", ignoreWhitespace: true)) return new MultiValueDivToken { Text = _source[start.._pos] };
                    else if (Match("*")) return ScanMultiLineComment();
                    else return new SlashToken { Text = _source[start.._pos] };
                case '*':
                    if (StartOfStmt()) return ScanSingleLineComment();
                    else if (Match("**", ignoreWhitespace: true)) return new MultiValueMullToken { Text = _source[start.._pos] };
                    else if (Match("*", ignoreWhitespace: true)) return new PowerToken { Text = _source[start.._pos] };
                    else return new StarToken { Text = _source[start.._pos] };
                case '!':
                    if (StartOfStmt()) return ScanSingleLineComment();
                    else if (Match("=", ignoreWhitespace: true)) return new ExcalmEqToken { Text = _source[start.._pos] };
                    else return new ExclamToken { Text = _source[start.._pos] };
                case ':':
                    if (Match("::", ignoreWhitespace: true)) return new MultiValueConcatToken { Text = _source[start.._pos] };
                    else if (Match("=", ignoreWhitespace: true)) return new ColonEqualToken { Text = _source[start.._pos] };
                    else return new ColonToken { Text = _source[start.._pos] };
                case '>':
                    return new RAngleBracketToken { Text = _source[start.._pos] };
                case ',':
                    return new CommaToken { Text = _source[start.._pos] };
                case ']':
                    return new RSqrBracketToken { Text = _source[start.._pos] };
                case '[':
                    return new LSqrBracketToken { Text = _source[start.._pos] };
                case ')':
                    return new RParenToken { Text = _source[start.._pos] };
                case '(':
                    return new LParenToken { Text = _source[start.._pos] };
                case ';':
                    return new SemiColonToken { Text = _source[start.._pos] };
                case '{':
                    return new LCurlyToken { Text = _source[start.._pos] };
                case '}':
                    return new RCurlyToken { Text = _source[start.._pos] };
                case '$' when Match("insert"):
                    return new InsertToken { Text = _source[start.._pos] };
                case '\\':
                    return ScanNumber();
                case '.':
                    return ScanNumber();      
                case '@':
                    return ScanSystemVariableOrAtOperator();
                case '\r':
                    Match("\n");
                    IncrementLineNo();
                    while (Match("\r\n"))
                    {
                        IncrementLineNo();
                    }
                    return new NewLineToken { Text = _source[start.._pos] };
                case ' ':
                case '\t':
                case '\f':
                case '\v':
                    while (Match(" ") || Match("\t") || Match("\f") || Match("\v"));
                    return new WhiteSpaceToken { Text = _source[start.._pos] };
                case '|':
                    return new PipeToken{ Text = _source[start.._pos] };
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
                        //_tokenErrors.ReportError(_lineNo, $"Unmatched character: {character}",_col,_col, _fileName);
                        return new CatchAllToken { Text = _source[start.._pos] };
                    }
            }
        }

        bool IsIdentifierOrKeyWord(char chr) {

            return (chr >= 'a' && chr <= 'z') ||
                   (chr >= 'A' && chr <= 'Z') ||
                   chr == '_';
        }

        bool IsNumber(char chr, bool hexAllowed = false)
        {
            return chr >= '0' && chr <= '9' ||
                (hexAllowed && (chr >= 'a' && chr <= 'f') || (chr >= 'A' && chr <= 'F'))
                || chr == '.';
        }

        bool StartOfStmt()
        {
            return (_prevToken is NewLineToken || _prevToken is SemiColonToken || _prevToken == null);
        }

        CommentToken ScanSingleLineComment()
        {
            int start = _pos -1;
            while (!IsAtEnd() && Peek() != '\r')
            {
                Advance();
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
                if (chr == '*' && Match("/")) {
                    return new CommentToken { Text = _source[start.._pos] };
                }
            }

            return new CommentToken { Text = _source[start.._pos] };
        }
        

        StringToken ScanStringLiteral(char delim)
        {
            int start = _pos - 1;
            List<char> chars = new();
            while (Peek() != delim && !IsAtEnd())
            {
                char chr = Peek();

                if (chr == '\r')
                {
                    if (chars.Count > 0 && chars.Last() == '|')
                    {
                        chars.RemoveAt(chars.Count - 1);
                        Advance();
                        Advance();
                        IncrementLineNo();
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
               
                chars.Add(Advance());
            }

            if (!Match(delim.ToString()))
            {
                _tokenErrors.ReportError(_lineNo, $"String must be enclosed by {delim}.",_col,_col, _fileName);
            }


            string str = new(chars.ToArray());
            return new StringToken { Str = str, Delim = delim , Text = _source[start.._pos]};
        }

        Token ScanSystemVariableOrAtOperator()
        {
            int origPos = _pos;
            int origCol = _col;
            int origLineNo = _lineNo;

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
                case "@lower.case":
                case "@upper.case":
                case "@list.active":
                    return new SystemVariableToken { Text = atOperatorOrSystemVariable };
                default:
                    // We're not dealing with a system variabe, so let's return the at operator and put the token position to after the at operator.
                    _pos = origPos;
                    _col = origCol;
                    _lineNo = origLineNo;
                    return new AtOperatorToken { Text = atOperatorOrSystemVariable };
           }
        }

        Token ScanIdentifierOrKeyword()
        {
            int start = _pos - 1;
            int startCol = _col;
            while (IsIdentifierOrKeyWord(Peek()) || IsNumber(Peek()) || Peek() == '@' || Peek() == '.' || Peek() == '$' || Peek() == '%')
            {
                Advance();
            }

            string idOrKeyword = _source[start.._pos];

            if (idOrKeyword[0] == '_') {
                switch (idOrKeyword.ToLower()) {
                    case "_eqc":
                        return new EqcToken { Text = idOrKeyword };
                    case "_nec":
                        return new NecToken { Text = idOrKeyword };
                    case "_ltc":
                        return new LtcToken { Text = idOrKeyword };
                    case "_lec":
                        return new LecToken { Text = idOrKeyword };
                    case "_gtc":
                        return new GtcToken { Text = idOrKeyword };
                    case "_eqx":
                        return new EqxToken { Text = idOrKeyword };
                    case "_nex":
                        return new NexToken { Text = idOrKeyword };
                    case "_ltx":
                        return new LtxToken { Text = idOrKeyword };
                    case "_gtx":
                        return new GtxToken { Text = idOrKeyword };
                    case "_lex":
                        return new LexToken { Text = idOrKeyword };
                    case "_gex":
                        return new GexToken { Text = idOrKeyword };
                    default:
                        _tokenErrors.ReportError(_lineNo, $"A variable name cannot start with _.", startCol, _col, _fileName);
                        return new IdentifierToken { Text = idOrKeyword };

                }
            } else {
                return idOrKeyword.ToLower() switch {
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
                    "abort" => new AbortToken { Text = idOrKeyword },
                    "ge" => new GeToken { Text = idOrKeyword },
                    "ne" => new NeToken { Text = idOrKeyword },
                    "lt" => new LtToken { Text = idOrKeyword },
                    "le" => new LeToken { Text = idOrKeyword },
                    "gt" => new GtToken { Text = idOrKeyword },
                    "eq" => new EqToken { Text = idOrKeyword },
                    "and" => new AndToken { Text = idOrKeyword },
                    "or" => new OrToken { Text = idOrKeyword },
                    _ => new IdentifierToken { Text = idOrKeyword }
                };
            }
        }

        NumberToken ScanNumber()
        {
            int startPos = _pos - 1;
            int startCol = _col - 1;

            bool isHexLiteral = (_source[startPos] == '0' && Match("x") || _source[startPos] == '\\');

            while (!IsAtEnd())
            {
                char chr = Peek();
               
                if (!IsNumber(chr,hexAllowed: isHexLiteral))
                {
                    break;
                }
                
                Advance();
            }

            if (isHexLiteral) Match("\\");
           
            string number = _source[startPos.._pos];

            if (isHexLiteral)
            {
                if (number.Contains('.'))
                {
                    _tokenErrors.ReportError(_lineNo, "Hex literal cannot contain a decimal point.", startCol, _col, _fileName);
                }
                else if (number.StartsWith('\\') && (number.Last() != '\\' || number.Length == 1))
                {
                    _tokenErrors.ReportError(_lineNo, "Hex literal must be terminated by \\.", startCol, _col, _fileName);
                }
                else if ((number.StartsWith("0x",StringComparison.OrdinalIgnoreCase)  || number.StartsWith('\\') )&& number.Length < 3)
                {
                    _tokenErrors.ReportError(_lineNo, "Invalid hex literal.", startCol, _col, _fileName);
                }
                
            }
            else if ((number.Last() == '.' || number.Count(x => x == '.') > 1))
            {
                _tokenErrors.ReportError(_lineNo, "The number contains an invalid deciaml point.", startCol, _col, _fileName);
            }

            return new NumberToken { Text = number };
        }

        bool IsAtEnd()
        {
            return _pos >= _source.Length;
        }

        char Advance()
        {
            char c = _source[_pos];
            IncrementPos();
            return c;
        }

        bool Match(string expected, bool ignoreWhitespace = false)
        {
            if (IsAtEnd()) return false;

            int startPos = _pos;
            int startCol = _col;

            foreach (var chr in expected) {
                while (ignoreWhitespace && Peek() == ' ') { Advance(); };
                if (IsAtEnd() || !Peek().ToString().Equals(chr.ToString(), StringComparison.OrdinalIgnoreCase)) {
                    // We looked ahead but failed to find the expected string, so let's restore and move on.
                    _pos = startPos;
                    _col = startCol;
                    return false;
                } else {
                    IncrementPos();
                }
            }
            // If we got here, then we found the expected string.
            return true;
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
