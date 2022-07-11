namespace BasicPlusParser
{
    public class Region 
    {
        public int StartLineNo { get; set; }
        public int EndLineNo { get; set; }
        public int StartCharPos { get; set; }
        public int EndCharPos { get; set; }

        public Region(int startLineNo, int startCharPos, int entLineNo, int endCharPos)
        {
            StartLineNo = startLineNo;
            StartCharPos = startCharPos;
            EndLineNo = entLineNo;
            EndCharPos = endCharPos;
        }
    }
}
