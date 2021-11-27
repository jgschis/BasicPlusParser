This is a parser for the Basic + language (of Open Insight). The parser creates a syntax tree out of a file of Basic + source code. The parser is also capable of detecting unreachable code and unassigned variables.


``` csharp
string path = "...";
string sourceCode = File.ReadAllText(path);
     
Parser parser = new Parser(sourceCode);
OiProgram program = parser.Parse();

UnreachableCodeAnalyser analyser = new UnreachableCodeAnalyser(program);
analyser.Analyse();
```
