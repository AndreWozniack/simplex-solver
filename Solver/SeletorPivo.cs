using Simplex.Modelo;

namespace Simplex.Solver;

/// <summary>
/// Regras de seleção de pivô na convenção de aula (Cj−Zj e θ).
/// Separado do SimplexSolver para facilitar alteração durante a defesa
/// (ex.: trocar pela regra de Bland para anti-ciclagem).
/// </summary>
public static class SeletorPivo
{
    // Tolerância numérica: evita que erros de ponto flutuante façam o loop não convergir.
    private const double Tolerancia = 1e-9;

    /// <summary>
    /// Escolhe a COLUNA PIVÔ (variável que ENTRA na base).
    ///
    /// Critério de maximização: entra a coluna com maior Cj−Zj POSITIVO.
    /// Retorna −1 quando todas Cj−Zj ≤ 0 → condição de ÓTIMO atingido.
    ///
    /// Nota: itera somente até NumColunasReais — variáveis artificiais
    /// nunca entram como coluna pivô (elas só saem da base na Fase 1).
    /// </summary>
    public static int EscolherColuna(Tableau tableau)
    {
        int    melhor    = -1;
        double maiorCjZj = Tolerancia; // só entra se estritamente acima da tolerância

        for (int j = 0; j < tableau.NumColunasReais; j++)
        {
            double cjzj = tableau.CjMenosZj(j);
            if (cjzj > maiorCjZj)
            {
                maiorCjZj = cjzj;
                melhor    = j;
            }
        }

        return melhor; // −1 ⟺ ótimo
    }

    /// <summary>
    /// Escolhe a LINHA PIVÔ (variável que SAI da base) pelo teste da razão θ.
    ///
    ///   θ_i = Rhs[i] / Matriz[i, colunaPivo]   (apenas para Matriz[i, colunaPivo] > 0)
    ///
    /// A linha com menor θ positivo é escolhida; o cruzamento linha×coluna
    /// é o elemento pivô ("e.p.").
    ///
    /// Retorna −1 quando nenhum coeficiente da coluna é positivo → problema ILIMITADO.
    /// Empate no menor θ é sinal de DEGENERAÇÃO (verificado em DetectorAcidentes).
    /// </summary>
    public static int EscolherLinha(Tableau tableau, int colunaPivo)
    {
        int    melhor     = -1;
        double menorTheta = double.MaxValue;

        for (int i = 0; i < tableau.NumLinhas; i++)
        {
            double elem = tableau.Matriz[i, colunaPivo];
            if (elem <= Tolerancia) continue; // ignora zero e negativos

            double theta = tableau.Rhs[i] / elem;
            if (theta < menorTheta - Tolerancia)
            {
                menorTheta = theta;
                melhor     = i;
            }
        }

        return melhor; // −1 ⟺ ilimitado
    }
}
