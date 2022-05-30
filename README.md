This is a parser for the Basic + language (of Open Insight). The parser creates a syntax tree out of a file of Basic + source code. The parser is also capable of detecting unreachable code and unassigned variables.


``` csharp
string path = "...";
string sourceCode = File.ReadAllText(path);
     
Parser parser = new Parser(sourceCode);
OiProgram program = parser.Parse();

UnreachableCodeAnalyser analyser = new UnreachableCodeAnalyser(program);
analyser.Analyse();
```
The parser also contains an implementation of the language server protocol, which means you can use the parser in an IDE, for example Visual Studio Code:
![image](https://user-images.githubusercontent.com/87922814/170830212-fc152117-a2ac-44e5-b1ec-db8c385c0346.png)
