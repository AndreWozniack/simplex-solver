namespace Simplex.Modelo;

/// <summary>Tipo de otimização do problema.</summary>
public enum TipoObjetivo
{
    Maximizar,
    Minimizar
}

/// <summary>Operador relacional de uma restrição.</summary>
public enum Operador
{
    MenorIgual,  // <=
    MaiorIgual,  // >=
    Igual        // =
}

/// <summary>Resultado final/parcial da execução do SIMPLEX.</summary>
public enum StatusSolucao
{
    Otimo,            // solução ótima encontrada
    Ilimitado,        // sem fronteira (unbounded)
    Inviavel,         // sem região viável (infeasible)
    Degenerado,       // ótimo atingido porém com degeneração detectada
    OtimosAlternados, // existe mais de uma solução ótima (plus)
    Interrompido      // parada por acidente matemático / limite de iterações
}
