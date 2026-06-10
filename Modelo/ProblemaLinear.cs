namespace Simplex.Modelo;

/// <summary>
/// Modelo de domínio puro de um problema de programação linear.
/// Não contém lógica do SIMPLEX — apenas os dados do problema fornecidos pelo usuário.
/// </summary>
public sealed class ProblemaLinear
{
    /// <summary>Maximizar ou minimizar.</summary>
    public TipoObjetivo Tipo { get; }

    /// <summary>Coeficientes da função objetivo (c). Tamanho = número de variáveis de decisão.</summary>
    public double[] CoeficientesObjetivo { get; }

    /// <summary>Lista de restrições do modelo.</summary>
    public IReadOnlyList<Restricao> Restricoes { get; }

    /// <summary>Número de variáveis de decisão (x1..xn).</summary>
    public int NumeroVariaveis => CoeficientesObjetivo.Length;

    public ProblemaLinear(
        TipoObjetivo tipo,
        double[] coeficientesObjetivo,
        IReadOnlyList<Restricao> restricoes)
    {
        Tipo = tipo;
        CoeficientesObjetivo = coeficientesObjetivo
            ?? throw new ArgumentNullException(nameof(coeficientesObjetivo));
        Restricoes = restricoes
            ?? throw new ArgumentNullException(nameof(restricoes));

        // TODO(validação): garantir que toda restrição tenha NumeroVariaveis coeficientes.
    }
}
