This is a parser for the Basic + language (of Open Insight). The parser creates a syntax tree out of a file of Basic + source code. The parser is also capable of detecting unreachable code and unassigned variables.


``` csharp
string path = "...";
string sourceCode = File.ReadAllText(path);
     
Parser parser = new Parser(sourceCode);
Procedure program = parser.Parse();

UnreachableCodeAnalyser analyser = new UnreachableCodeAnalyser(program);
analyser.Analyse();
UnassignedVariableAnalyser uva = new UnassignedVariableAnalyser(program);
uva.Analyse();
```
The parser also contains an implementation of the language server protocol, which means you can use the parser in any IDE that implements the language server protocol, for example Visual Studio Code:
![image](https://user-images.githubusercontent.com/87922814/175839994-065ceeab-476c-4ef5-abf8-ed7ba597f07d.png)



TODO
-----
* Make the parser aware of the various "System Variables"
* Make the parser capable of reading INSERT files.
* Integrate the parser with OI.
* Add code completion.
* Add code formatting.
* The unassigned variable analyser can't handle the case when a label spans several blocks (i.e., when a label is in the middle of a for loop). 99.999% of code does not contain such labels, but there are some weird programs created decades ago that do ... Anyway, there is a way to make the unassiagned variable analyser handle this case.
