namespace Simplex.Modelo;

/// <summary>
/// Uma restrição linear: (coeficientes . variáveis) [operador] ladoDireito.
/// Ex.: 2x1 + 3x2 &lt;= 12  →  Coeficientes={2,3}, Operador=MenorIgual, LadoDireito=12.
/// </summary>
public sealed class Restricao
{
    public double[] Coeficientes { get; }
    public Operador Operador { get; }
    public double LadoDireito { get; }

    public Restricao(double[] coeficientes, Operador operador, double ladoDireito)
    {
        Coeficientes = coeficientes ?? throw new ArgumentNullException(nameof(coeficientes));
        Operador = operador;
        LadoDireito = ladoDireito;
    }

    public override string ToString()
    {
        string termos = string.Join(" + ",
            Coeficientes.Select((c, i) => $"{c}x{i + 1}"));
        string op = Operador switch
        {
            Operador.MenorIgual => "<=",
            Operador.MaiorIgual => ">=",
            _ => "="
        };
        return $"{termos} {op} {LadoDireito}";
    }
}
