using System.Collections.Generic;


namespace BasicPlusParser {
    public static class BuiltinFunctions  {

        static readonly HashSet<string> _builtInFunctions = new() {
            "len","xlate","char","field","not","assigned","unassigned","fieldcount","mod","time","date","int","delete","insert",
            "sum","dcount","index","indexc","count", "trim", "trimb","trimf","inlist","status","str","num","fmt", "quote", "iconv", "oconv", "matunparse", "abs", "alpha",
            "bitor", "col2", "space", "timedate", "get_status"
        };

        public static bool IsBuiltInFunction(string function) {
            return _builtInFunctions.Contains(function.ToLower());
        }
    }
}
