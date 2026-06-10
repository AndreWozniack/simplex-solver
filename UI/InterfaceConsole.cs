using System.Globalization;
using Simplex.Modelo;

namespace Simplex.UI;

/// <summary>
/// Interface com o usuário via console (requisito 2). Responsável apenas por
/// LER o problema e EXIBIR resultados — nenhuma lógica do SIMPLEX aqui.
/// </summary>
public sealed class InterfaceConsole
{
    /// <summary>Lê um problema linear: menu de exemplos prontos ou entrada manual.</summary>
    public ProblemaLinear LerProblema()
    {
        Console.WriteLine("=== Entrada do problema de Programação Linear ===");
        Console.WriteLine("Problemas de teste prontos:");
        for (int i = 0; i < ProblemasExemplo.Lista.Count; i++)
            Console.WriteLine($"  [{i + 1}] {ProblemasExemplo.Lista[i].Descricao}");
        Console.WriteLine($"  [{ProblemasExemplo.Lista.Count + 1}] Entrada manual");

        int opcao = LerInteiro($"Opção [1-{ProblemasExemplo.Lista.Count + 1}]: ",
            minimo: 1, maximo: ProblemasExemplo.Lista.Count + 1);

        if (opcao <= ProblemasExemplo.Lista.Count)
            return ProblemasExemplo.Lista[opcao - 1].Problema;

        return LerProblemaManual();
    }

    /// <summary>
    /// Entrada manual em linha única:
    ///   Objetivo:    max 3 5
    ///   Restrição:   1 0 &lt;= 4   (linha vazia encerra)
    /// O número de variáveis é inferido da quantidade de coeficientes do objetivo.
    /// </summary>
    private static ProblemaLinear LerProblemaManual()
    {
        Console.WriteLine();
        Console.WriteLine("Objetivo: \"max\" ou \"min\" seguido dos coeficientes. Ex.: max 3 5");
        (TipoObjetivo tipo, double[] objetivo) = LerObjetivo();
        int nVars = objetivo.Length;

        Console.WriteLine($"Restrições ({nVars} coeficientes, operador <=, >= ou =, lado direito). Ex.: 3 2 <= 18");
        Console.WriteLine("Linha vazia encerra.");
        var restricoes = new List<Restricao>();
        while (true)
        {
            Console.Write($"Restrição {restricoes.Count + 1}: ");
            string? linha = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(linha))
            {
                if (restricoes.Count > 0) break;
                Console.WriteLine("Informe ao menos uma restrição.");
                continue;
            }

            if (TentarParsearRestricao(linha, nVars, out Restricao? r, out string erro))
                restricoes.Add(r!);
            else
                Console.WriteLine($"Linha inválida: {erro}");
        }

        return new ProblemaLinear(tipo, objetivo, restricoes);
    }

    private static (TipoObjetivo, double[]) LerObjetivo()
    {
        while (true)
        {
            Console.Write("Objetivo: ");
            string[] partes = (Console.ReadLine() ?? "")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (partes.Length < 2)
            {
                Console.WriteLine("Linha inválida: informe \"max\" ou \"min\" e ao menos um coeficiente.");
                continue;
            }

            TipoObjetivo tipo;
            switch (partes[0].ToLowerInvariant())
            {
                case "max": tipo = TipoObjetivo.Maximizar; break;
                case "min": tipo = TipoObjetivo.Minimizar; break;
                default:
                    Console.WriteLine($"Linha inválida: esperado \"max\" ou \"min\", recebido \"{partes[0]}\".");
                    continue;
            }

            if (TentarParsearNumeros(partes[1..], out double[] coef))
                return (tipo, coef);

            Console.WriteLine("Linha inválida: coeficiente não numérico.");
        }
    }

    private static bool TentarParsearRestricao(
        string linha, int nVars, out Restricao? restricao, out string erro)
    {
        restricao = null;

        string[] partes = linha
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        int posOp = Array.FindIndex(partes, p => p is "<=" or ">=" or "=" or "==");
        if (posOp < 0)
        {
            erro = "operador <=, >= ou = não encontrado.";
            return false;
        }
        if (posOp != nVars)
        {
            erro = $"esperados {nVars} coeficientes antes do operador, encontrados {posOp}.";
            return false;
        }
        if (partes.Length != posOp + 2)
        {
            erro = "esperado exatamente um valor após o operador (lado direito b).";
            return false;
        }

        if (!TentarParsearNumeros(partes[..posOp], out double[] coef) ||
            !TentarParsearNumeros(partes[(posOp + 1)..], out double[] rhs))
        {
            erro = "valor não numérico.";
            return false;
        }

        Operador op = partes[posOp] switch
        {
            "<=" => Operador.MenorIgual,
            ">=" => Operador.MaiorIgual,
            _ => Operador.Igual
        };

        restricao = new Restricao(coef, op, rhs[0]);
        erro = "";
        return true;
    }

    private static bool TentarParsearNumeros(string[] tokens, out double[] valores)
    {
        valores = new double[tokens.Length];
        for (int i = 0; i < tokens.Length; i++)
        {
            string s = tokens[i].Replace(',', '.');
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out valores[i]))
                return false;
        }
        return true;
    }

    /// <summary>Exibe o resultado final/intermediário (requisito 4).</summary>
    public void ExibirResultado(ResultadoSimplex resultado)
    {
        Console.WriteLine();
        Console.WriteLine("=== Resultado ===");
        Console.WriteLine($"Status: {resultado.Status}");
        Console.WriteLine($"Solução final? {(resultado.SolucaoFinal ? "Sim" : "Não (intermediária)")}");
        Console.WriteLine($"Iterações: {resultado.NumeroIteracoes}");

        if (resultado.ValorObjetivo is double z)
            Console.WriteLine($"Valor ótimo (Z/C): {z.ToString("0.####", CultureInfo.InvariantCulture)}");

        for (int i = 0; i < resultado.ValoresVariaveis.Length; i++)
            Console.WriteLine($"  x{i + 1} = {resultado.ValoresVariaveis[i].ToString("0.####", CultureInfo.InvariantCulture)}");

        if (!string.IsNullOrWhiteSpace(resultado.Observacao))
            Console.WriteLine($"Observação: {resultado.Observacao}");
    }

    // ---- helpers de leitura ----

    private static int LerInteiro(string prompt, int minimo, int maximo)
    {
        while (true)
        {
            Console.Write(prompt);
            if (int.TryParse(Console.ReadLine(), out int v) && v >= minimo && v <= maximo)
                return v;
            Console.WriteLine($"Informe um inteiro entre {minimo} e {maximo}.");
        }
    }
}
