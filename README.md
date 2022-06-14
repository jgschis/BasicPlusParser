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
![image](https://user-images.githubusercontent.com/87922814/170830212-fc152117-a2ac-44e5-b1ec-db8c385c0346.png)


+There are actaully 2 issues with the unassigned variable analyser. The first is that if will loop infinitely if you have recursive gosubs. The second is that that analyser can't handle a label that spans 2 or more blocks (i.e., a label that is in two loop-repeat statements. There is a way to fix both problems. 
