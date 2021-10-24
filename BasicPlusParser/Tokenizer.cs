using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BasicPlusParser.Tokens;
using System.Linq;


namespace BasicPlusParser
{
    public class Tokenizer
    {

        private int _pos = 0;
        private List<Tokens.Token> _tokens = new List<Tokens.Token>();
        private string _origText;
        Tokens.Token _prevToken => _tokens.Count > 0 ? _tokens.Last() : null;

        static Regex _numRegEx = new Regex(@"^[0-9]+(\.[0-9]*)?",RegexOptions.IgnoreCase);
        static Regex _idRegEx = new Regex(@"^[a-zA-Z_@][a-zA-Z_$0-9@\.]*", RegexOptions.IgnoreCase);
        static Regex _whiteSpaceRegEx = new Regex(@"\s", RegexOptions.IgnoreCase);


        public Tokenizer(string text)
        {
            _origText = text;
        }


        bool TryMatch(Regex regEx, string text, out Match match)
        {
            match = regEx.Match(text);
            return match.Success;
        }


        bool TryMatch(string target, string text, out string match)
        {
            match = null;
            if (text.StartsWith(target,StringComparison.OrdinalIgnoreCase))
            {
                match = text.Substring(0, target.Length);
                return true;
            }
            else
            {
                return false;
            } 
        }

        bool TryMatchString(string text, out StringToken token)
        {
            token = null;
            if (!(text.StartsWith("'") || text.StartsWith("\"")))
            {
                return false;
            }

            int pos = 1;
            char delim = text[0];
            List<char> chars = new List<char>();
            chars.Add(delim);
            while (pos < text.Length)
            {
                char chr = text[pos];
                if (chr == delim)
                {
                    chars.Add(delim);
                    token = new StringToken { Text = new String(chars.ToArray()), Delim = delim };
                    return true;
                }
                if (chr == '\r')
                {
                    if (chars.Count > 1 && chars[chars.Count - 1] == '|')
                    {
                        chars.RemoveAt(chars.Count - 1);
                        pos += 2;
                        continue;
                    }
                    else
                    {
                        throw new InvalidOperationException("Sring contanis new line char.");
                    }
                }
                chars.Add(chr);
                pos += 1;
            }
            throw new InvalidOperationException("String is not delimited.");
        }

        bool TryMatchMultiLineComment(string text, out CommentToken token)
        {
            token = new CommentToken();
            if (text.StartsWith("/*"))
            {
                int index;
                if ( text.Length >= 4 && (index = text.Substring(1).IndexOf("*/")) > 0)
                {
                    token.Text = text.Substring(0, index + 3);
                    return true;
                } else
                {
                    throw new InvalidOperationException("Comment not delimited with */");
                }
            }
            return false;
        }

        bool TryMatchSingleLineComment(string text, out CommentToken token)
        {
            // This will try each test.
            // The longest match that appearst highest in this function will be chosen.


            token = new CommentToken();
            if (text.StartsWith("//") || text.StartsWith("!") || text.StartsWith("*"))
            {
                List<char> comment = new List<char>();
                foreach (char chr in text)
                {
                    if (chr == '\r')
                    {
                        break;
                    }
                    comment.Add(chr);
                }
                token.Text = new string(comment.ToArray()); ;
                return true;
                
            }
            return false;
        }
        bool TryMatchCommonName(string text, out CommonNameToken token)
        {
            token = new CommonNameToken();
            List<char> name = new List<char>();
            int pos = 1;
            if (text.StartsWith('/') && _prevToken is CommonToken)
            {
                name.Add('/');
                if (text.Length > 1 && text[1] == '/')
                {
                    name.Add('/');
                    pos += 1;
                }

                while(pos < text.Length)
                {
                    char chr = text[pos];

                    if (chr == '\r')
                    {
                        throw new InvalidOperationException("New line not allowed in common block.");
                    }

                    if (chr == '/')
                    {
                        string end = new string (text.Substring(pos).TakeWhile(x => x == '/').ToArray());
                        token.Text = new string(name.ToArray()) + end;
                        return true;
                    }

                    pos += 1;
                    name.Add(chr);
                }
            }


            return false;
        }

        IEnumerable<Token> GetMatchingTokens(string text)
        {
            Match match;
            string found;

            // The //, * and ! comments must be the first thing on a line.
            if (_prevToken is NewLineToken || _prevToken is SemiColonToken || _prevToken == null)
            {
                if (TryMatchSingleLineComment(text, out CommentToken singleLineCommentToken)) yield return singleLineCommentToken;
            }
            if (TryMatchMultiLineComment(text, out CommentToken multieLineCommentToken)) yield return multieLineCommentToken;

            // Operators
            if (TryMatch("<=", text, out found)) yield return new LteToken { Text = found };
            if (TryMatch("and", text, out found)) yield return new AndToken { Text = found };
            if (TryMatch("or", text, out found)) yield return new OrToken { Text = found };
            if (TryMatch("!=", text, out found)) yield return new ExcalmEqToken { Text = found };
            if (TryMatch("#", text, out found)) yield return new HashTagToken { Text = found };
            if (TryMatch("+", text, out found)) yield return new PlusToken { Text = found };
            if (TryMatch("-", text, out found)) yield return new MinusToken { Text = found };
            if (TryMatch("/", text, out found)) yield return new SlashToken { Text = found };
            if (TryMatch("*", text, out found)) yield return new StarToken { Text = found };
            if (TryMatch("<", text, out found)) yield return new LAngleBracketToken { Text = found };
            if (TryMatch(">", text, out found)) yield return new RAngleBracketToken { Text = found };
            if (TryMatch(",", text, out found)) yield return new CommaToken { Text = found };
            if (TryMatch("]", text, out found)) yield return new RSqrBracketToken { Text = found };
            if (TryMatch("[", text, out found)) yield return new LSqrBracketToken { Text = found };
            if (TryMatch(")", text, out found)) yield return new RParenToken { Text = found };
            if (TryMatch("(", text, out found)) yield return new LParenToken { Text = found };
            if (TryMatch("=", text, out found)) yield return new EqualToken { Text = found };
            if (TryMatch(";", text, out found)) yield return new SemiColonToken { Text = found };
            if (TryMatch(":", text, out found)) yield return new ColonToken { Text = found };
            if (TryMatch("+=", text, out found)) yield return new PlusEqualToken { Text = found };
            if (TryMatch("/=", text, out found)) yield return new SlashEqualToken { Text = found };
            if (TryMatch("*=", text, out found)) yield return new StarEqualToken { Text = found };
            if (TryMatch(":=", text, out found)) yield return new ColonEqualToken { Text = found };
            if (TryMatch("-=", text, out found)) yield return new MinusEqualToken { Text = found };
            if (TryMatch("{", text, out found)) yield return new LCurlyToken { Text = found };
            if (TryMatch("}", text, out found)) yield return new RCurlyToken { Text = found };
            if (TryMatch("**", text, out found)) yield return new PowerToken { Text = found };
            if (TryMatch("***", text, out found)) yield return new MultiValueMullToken { Text = found };
            if (TryMatch("///", text, out found)) yield return new MultiValueDivToken { Text = found };
            if (TryMatch("+++", text, out found)) yield return new MultiValueAddToken { Text = found };
            if (TryMatch("---", text, out found)) yield return new MultiValueSubToken { Text = found };
            if (TryMatch(":::", text, out found)) yield return new MultiValueConcatToken { Text = found };
            if (TryMatch("ge", text, out found)) yield return new GeToken { Text = found };
            if (TryMatch("ne", text, out found)) yield return new NeToken { Text = found };
            if (TryMatch("lt", text, out found)) yield return new LtToken { Text = found };
            if (TryMatch("le", text, out found)) yield return new LeToken { Text = found };
            if (TryMatch("gt", text, out found)) yield return new GtToken { Text = found };
            if (TryMatch("eq", text, out found)) yield return new EqToken { Text = found };
            if (TryMatch("_eqc", text, out found)) yield return new EqcToken { Text = found };
            if (TryMatch("_neq", text, out found)) yield return new NecToken { Text = found };
            if (TryMatch("_ltc", text, out found)) yield return new LtcToken { Text = found };
            if (TryMatch("_leq", text, out found)) yield return new LecToken { Text = found };
            if (TryMatch("_gtc", text, out found)) yield return new GtcToken { Text = found };
            if (TryMatch("_gec", text, out found)) yield return new GecToken { Text = found };
            if (TryMatch("_eqx", text, out found)) yield return new EqxToken { Text = found };
            if (TryMatch("_nex", text, out found)) yield return new NexToken { Text = found };
            if (TryMatch("_ltx", text, out found)) yield return new LtxToken { Text = found };
            if (TryMatch("_gtx", text, out found)) yield return new GtxToken { Text = found };
            if (TryMatch("_lex", text, out found)) yield return new LexToken { Text = found };
            if (TryMatch("_gex", text, out found)) yield return new GexToken { Text = found };
            
            // Keywords
            if (TryMatch("loop",text, out found)) yield return new LoopToken { Text = found };
            if (TryMatch("repeat", text, out found)) yield return new RepeatToken { Text = found };
            if (TryMatch("if", text, out found)) yield return new IfToken { Text = found };
            if (TryMatch("then", text, out found)) yield return new ThenToken { Text = found };
            if (TryMatch("function", text, out found)) yield return new FunctionToken { Text = found };
            if (TryMatch("subroutine", text, out found)) yield return new SubroutineToken { Text = found };
            if (TryMatch("end", text, out found)) yield return new EndToken { Text = found };
            if (TryMatch("else", text, out found)) yield return new ElseToken { Text = found };
            if (TryMatch("begin", text, out found)) yield return new BeginToken { Text = found };
            if (TryMatch("case", text, out found)) yield return new CaseToken { Text = found };
            if (TryMatch("return", text, out found)) yield return new ReturnToken { Text = found };
            if (TryMatch("for", text, out found)) yield return new ForToken { Text = found };
            if (TryMatch("next", text, out found)) yield return new NextToken { Text = found };
            if (TryMatch("while", text, out found)) yield return new WhileToken { Text = found };
            if (TryMatch("until", text, out found)) yield return new UntilToken { Text = found };
            if (TryMatch("gosub", text, out found)) yield return new GosubToken { Text = found };
            if (TryMatch("to", text, out found)) yield return new ToToken { Text = found };
            if (TryMatch("equ", text, out found)) yield return new EquToken { Text = found };
            if (TryMatch("swap", text, out found)) yield return new SwapToken { Text = found };
            if (TryMatch("in", text, out found)) yield return new InToken { Text = found };
            if (TryMatch("with", text, out found)) yield return new WithToken { Text = found };
            if (TryMatch("convert", text, out found)) yield return new ConvertToken { Text = found };
            if (TryMatch("step", text, out found)) yield return new StepToken { Text = found };
            if (TryMatch("$insert", text, out found)) yield return new InsertToken { Text = found };
            if (TryMatch("declare", text, out found)) yield return new DeclareToken { Text = found };
            if (TryMatch("call", text, out found)) yield return new CallToken { Text = found };
            if (TryMatch("remove", text, out found)) yield return new RemoveToken { Text = found };
            if (TryMatch("from", text, out found)) yield return new FromToken { Text = found };
            if (TryMatch("at", text, out found)) yield return new AtToken { Text = found };
            if (TryMatch("setting", text, out found)) yield return new SettingToken { Text = found };
            if (TryMatch("mat", text, out found)) yield return new MatToken { Text = found };
            if (TryMatch("locate", text, out found)) yield return new LocateToken { Text = found };
            if (TryMatch("using", text, out found)) yield return new UsingToken { Text = found };
            if (TryMatch("null", text, out found)) yield return new NullToken { Text = found };
            if (TryMatch("read", text, out found)) yield return new ReadToken { Text = found };
            if (TryMatch("write", text, out found)) yield return new WriteToken { Text = found };
            if (TryMatch("delete", text, out found)) yield return new DeleteToken { Text = found };
            if (TryMatch("lock", text, out found)) yield return new LockToken { Text = found };
            if (TryMatch("unlock", text, out found)) yield return new UnlockToken { Text = found };
            if (TryMatch("open", text, out found)) yield return new OpenToken { Text = found };
            if (TryMatch("debug", text, out found)) yield return new DebugToken { Text = found };
            if (TryMatch("cursor", text, out found)) yield return new CursorToken { Text = found };
            if (TryMatch("on", text, out found)) yield return new OnToken { Text = found };
            if (TryMatch("clearselect", text, out found)) yield return new ClearSelectToken { Text = found };
            if (TryMatch("readnext", text, out found)) yield return new ReadNextToken { Text = found };
            if (TryMatch("do", text, out found)) yield return new DoToken { Text = found };
            if (TryMatch("all", text, out found)) yield return new AllToken { Text = found };
            if (TryMatch("goto", text, out found)) yield return new GoToToken { Text = found };
            if (TryMatch("transfer", text, out found)) yield return new TransferToken { Text = found };
            if (TryMatch("matread", text, out found)) yield return new MatReadToken { Text = found };
            if (TryMatch("matwrite", text, out found)) yield return new MatWriteToken { Text = found };
            if (TryMatch("oswrite", text, out found)) yield return new OsWriteToken { Text = found };
            if (TryMatch("osread", text, out found)) yield return new OsReadToken { Text = found };
            if (TryMatch("dimension", text, out found)) yield return new DimensionToken { Text = found };
            if (TryMatch("dim", text, out found)) yield return new DimensionToken { Text = found };
            if (TryMatch("#pragma", text, out found)) yield return new PragmaToken { Text = found };
            if (TryMatch("output", text, out found)) yield return new OutputToken { Text = found };
            if (TryMatch("precomp", text, out found)) yield return new PreCompToken { Text = found };
            if (TryMatch("common", text, out found)) yield return new CommonToken { Text = found };
            if (TryMatch("freecommon", text, out found)) yield return new FreeCommonToken { Text = found };
            if (TryMatch("initrnd", text, out found)) yield return new InitRndToken { Text = found };
            if (TryMatch("by", text, out found)) yield return new ByToken { Text = found };
            if (TryMatch("select", text, out found)) yield return new SelectToken { Text = found };
            if (TryMatch("equate", text, out found)) yield return new EquToken { Text = found };
            if (TryMatch("garbagecollect", text, out found)) yield return new GarbageCollectToken { Text = found };
            if (TryMatch("flush", text, out found)) yield return new FlushToken { Text = found };
            if (TryMatch("readv", text, out found)) yield return new ReadVToken { Text = found };
            if (TryMatch("reado", text, out found)) yield return new ReadOToken { Text = found };
            if (TryMatch("matparse", text, out found)) yield return new MatParseToken { Text = found };
            if (TryMatch("into", text, out found)) yield return new IntoToken { Text = found };
            if (TryMatch("match", text, out found)) yield return new MatchesToken { Text = found };
            if (TryMatch("matches", text, out found)) yield return new MatchesToken { Text = found };
            if (TryMatch("initdir", text, out found)) yield return new InitDirToken { Text = found };
            if (TryMatch("writev", text, out found)) yield return new WriteVToken { Text = found };
            if (TryMatch("compile", text, out found)) yield return new CompileToken { Text = found };
            if (TryMatch("osopen", text, out found)) yield return new OsOpenToken { Text = found };
            if (TryMatch("osdelete", text, out found)) yield return new OsDeleteToken { Text = found };
            if (TryMatch("osclose", text, out found)) yield return new OsCloseToken { Text = found };
            if (TryMatch("osbread", text, out found)) yield return new OsBReadToken { Text = found };
            if (TryMatch("osbwrite", text, out found)) yield return new OsBWriteToken { Text = found };
            if (TryMatch("length", text, out found)) yield return new LengthToken { Text = found };
            if (TryMatch("bremove", text, out found)) yield return new BRemoveToken { Text = found };

            if (TryMatchCommonName(text, out CommonNameToken commonNameToken)) yield return commonNameToken;

            if (TryMatch(_numRegEx, text, out match)) yield return new NumberToken { Text = match.Value };

            if (TryMatch(_idRegEx, text, out match)) yield return new IdentifierToken { Text = match.Value };

            if (TryMatchString(text, out StringToken strToken)) yield return strToken;
            
            if (TryMatch("\r\n", text, out found)) yield return new NewLineToken { Text = found };

            if (TryMatch(_whiteSpaceRegEx, text, out match)) yield return new WhiteSpaceToken { Text = match.Value };
        }


        int GetLineNo()
        {
            int end = Math.Min(_pos, _origText.Length);
            return _origText.Substring(0, end).Count(x => x == '\r') + 1;
        }

        public List<Token> Tokenise()
        {
            while (_pos < _origText.Length)
            {
                int maxLength = 0;
                Token matchedToken = null;
                foreach (Token candidate in GetMatchingTokens(_origText[_pos..]))
                {
                    if (candidate.Text.Length > maxLength)
                    {
                        maxLength = candidate.Text.Length;
                        matchedToken = candidate;
                    }
                }
                if (matchedToken != null)
                {
                    matchedToken.LineNo = GetLineNo();
                    matchedToken.Pos = _pos;
     
                    if ((_prevToken is IfToken || _prevToken is ReturnToken) && matchedToken is WhiteSpaceToken)
                    {
                        // Basic + allows keywords to be function names.
                        // However, if the keyword is followed by space,
                        // it does not allow the keyword to be a function name.
                        // This logic enforces that absurd rule.
                        // Though anyone who does this is an idiot.
                        _prevToken.DisallowFunction = true;
                    }

                    if (matchedToken is WhiteSpaceToken || matchedToken is CommentToken ||
                        (matchedToken is NewLineToken && _prevToken is NewLineToken))
                    {
                        // We don't what junk white spaces, newlines and comments in the token list.
                        _pos += maxLength;
                        continue;
                    }
                  
                    _tokens.Add(matchedToken);
                    _pos += maxLength;
                } else
                {
                    throw new InvalidOperationException($"Failed to match token at pos {_pos}, line {GetLineNo()}.");
                }
             
            }
            _tokens.Add(new EofToken { LineNo = GetLineNo(), Pos = _pos });
            return _tokens;
        }
    }
}
