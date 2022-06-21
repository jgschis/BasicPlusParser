This is a parser for the Basic + language (of Open Insight). The parser creates a syntax tree out of a file of Basic + source code. The parser is also capable of detecting unreachable code and unassigned variables.+


``` csharp
string path = "...";
string sourceCode = File.ReadAllText(path);
     
Parser parser = new Parser(sourceCode);
OiProgram program = parser.Parse();

UnreachableCodeAnalyser analyser = new UnreachableCodeAnalyser(program);
analyser.Analyse();
```
The parser also contains an implementation of the language server protocol, which means you can use the parser in any IDE that implements the language server protocol, for example Visual Studio Code:
![image](https://user-images.githubusercontent.com/87922814/174413924-5ae55d50-889d-4a5a-a9c1-e763c4c46e46.png)


+ The unassigned variable analyser can't handle the case when a label spans several blocks. 99.999% of code does not contain such labels, but there are some weird programs created decades ago that do ... Anyway, there is a way to make the unassiagned variable analyser handle this case.
