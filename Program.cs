using Simplex.Modelo;
using Simplex.Solver;
using Simplex.UI;

// Ponto de entrada. Apenas orquestra: lê → resolve → exibe.
// A lógica do método SIMPLEX fica em Solver/ (a implementar).

var ui = new InterfaceConsole();

Console.WriteLine("Solver SIMPLEX — Otimização de Sistemas Lineares");
Console.WriteLine("================================================");

ProblemaLinear problema = ui.LerProblema();

var solver = new SimplexSolver(Console.Out);
ResultadoSimplex resultado = solver.Resolver(problema);

ui.ExibirResultado(resultado);
