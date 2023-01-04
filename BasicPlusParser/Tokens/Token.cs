namespace BasicPlusParser.Tokens
{
    public abstract class Token
    {
        public int LineNo { get; set; }
        public string Text { get; set; }
        public int Pos { get; set; }
        public int StartCol { get; set; }
        public int EndCol { get; set; }
        public bool DisallowFunction = false;
        // This is used by the langauge server protocol to implement highlighting.
        public virtual string LsClass { get; set; } = "";
        public int EndLineNo { get; set; }
        public string FileName {get;set;}
    }
}
