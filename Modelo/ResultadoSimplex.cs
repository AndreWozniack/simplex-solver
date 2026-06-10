namespace Simplex.Modelo;

/// <summary>
/// Resposta do solver (requisito 4): valor ótimo de Z/C, valores das variáveis,
/// status e número de iterações. Marca claramente se é solução final ou intermediária.
/// </summary>
public sealed class ResultadoSimplex
{
    public StatusSolucao Status { get; init; }

    /// <summary>Valor ótimo de Z (max) ou C (min). Nulo se não houver solução válida.</summary>
    public double? ValorObjetivo { get; init; }

    /// <summary>Valores das variáveis de decisão (x1..xn) na solução encontrada.</summary>
    public double[] ValoresVariaveis { get; init; } = Array.Empty<double>();

    /// <summary>Número de iterações executadas (requisito 3).</summary>
    public int NumeroIteracoes { get; init; }

    /// <summary>True se a solução é final; false se intermediária/interrompida por acidente.</summary>
    public bool SolucaoFinal { get; init; }

    /// <summary>Mensagem livre para diagnósticos (degeneração, redundância, etc.).</summary>
    public string? Observacao { get; init; }
}
