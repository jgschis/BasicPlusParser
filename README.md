This is an error tolerant parser for the Basic + language (of Open Insight). The parser creates a syntax tree from Basic + source code. The parser is capable of detecting unreachable code and unassigned variables. It also reports  syntax/semantic/usage errors. 


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
The parser also contains an implementation of the language server protocol, which means you can use the parser in any IDE that implements the language server protocol client, for example Visual Studio Code:
![image](https://user-images.githubusercontent.com/87922814/175839994-065ceeab-476c-4ef5-abf8-ed7ba597f07d.png)

Features
----------
* Reports errors/warnings with code as you type. 
* Syntax highlighting.
* Unreachable code analysis that can  detect when a statement will never run.
* Smart unassigned variable analysis that takes into account if statements and gosubs. If you look at the screenshot above, you can see that d is definitively assigned in the then branch of the if statement but not definitively assigned outside of it.
* Placing your cursor over a symbol will tell you info about the symbol. In the case of an equate, it tells you the value of the equate.
* Code range folding/unfolding
* Pressing f12 on a label, equate, matrix or common variable will take you to where it is defined.
* View all references of a label, equate, matrix, common varialbe and function/subroutine.



TODO
-----
* Change the tokenizer so that you can add several tokens at once.
* Make the parser capable of reading INSERT files.
* Integrate the parser with OI.
* Add code completion.
* Add code formatting.
* The unassigned variable analyser can't handle the case when a label spans several blocks (i.e., when a label is in the middle of a for loop). 99.999% of code does not contain such labels, but there are some weird programs created decades ago that do ... Anyway, there is a way to make the unassiagned variable analyser handle this case.
