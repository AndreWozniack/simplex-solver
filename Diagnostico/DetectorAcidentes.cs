using Simplex.Modelo;

namespace Simplex.Diagnostico;

/// <summary>
/// Detecção dos "acidentes" do SIMPLEX (requisitos 5, 6, 7 + plus 2).
/// Cada método inspeciona o tableau e devolve um diagnóstico booleano.
/// </summary>
public static class DetectorAcidentes
{
    private const double Tolerancia = 1e-9;

    /// <summary>
    /// Requisito 5 — DEGENERAÇÃO.
    ///
    /// Detecta dois casos:
    ///   (a) Empate no menor θ durante o teste da razão para a colunaPivo
    ///       (ex.: 8/2 = 4 e 12/3 = 4 → duas linhas empatadas).
    ///   (b) Variável básica com Rhs ≈ 0 em qualquer linha (degeneração estrutural).
    ///
    /// Ambas as situações indicam que o próximo passo pode não avançar o objetivo.
    /// </summary>
    public static bool TemDegeneracao(Tableau tableau, int colunaPivo)
    {
        // Caso (b): alguma variável básica já tem valor zero
        for (int i = 0; i < tableau.NumLinhas; i++)
            if (Math.Abs(tableau.Rhs[i]) < Tolerancia)
                return true;

        // Caso (a): empate no menor θ para a coluna pivô escolhida
        double menorTheta  = double.MaxValue;
        int    contEmpate  = 0;

        for (int i = 0; i < tableau.NumLinhas; i++)
        {
            double elem = tableau.Matriz[i, colunaPivo];
            if (elem <= Tolerancia) continue;

            double theta = tableau.Rhs[i] / elem;

            if (theta < menorTheta - Tolerancia)
            {
                menorTheta = theta;
                contEmpate = 1;
            }
            else if (Math.Abs(theta - menorTheta) <= Tolerancia)
            {
                contEmpate++;
            }
        }

        return contEmpate > 1;
    }

    /// <summary>
    /// Requisito 7 — PROBLEMA SEM FRONTEIRA (ilimitado).
    ///
    /// Uma coluna pivô foi escolhida (Cj−Zj > 0), mas nenhum coeficiente
    /// dessa coluna é positivo → o teste da razão θ não tem candidato,
    /// ou seja, a variável pode crescer indefinidamente sem violar nenhuma restrição.
    /// </summary>
    public static bool EhIlimitado(Tableau tableau, int colunaPivo)
    {
        for (int i = 0; i < tableau.NumLinhas; i++)
            if (tableau.Matriz[i, colunaPivo] > Tolerancia)
                return false; // existe pelo menos um limitante

        return true; // nenhum coeficiente positivo → ilimitado
    }

    /// <summary>
    /// Requisito 6 — PROBLEMA INVIÁVEL.
    ///
    /// Chamado após o término da Fase 1 do método das duas fases.
    /// O problema é inviável se alguma variável artificial permanece na base
    /// com valor (Rhs) estritamente positivo → a soma das artificiais > 0
    /// e não é possível atingir uma solução viável.
    /// </summary>
    public static bool EhInviavel(Tableau tableau)
    {
        var artificiais = new HashSet<int>(tableau.IndicesArtificiais);

        for (int i = 0; i < tableau.NumLinhas; i++)
            if (artificiais.Contains(tableau.Base[i]) && tableau.Rhs[i] > Tolerancia)
                return true;

        return false;
    }

    /// <summary>
    /// Plus 2 — ÓTIMOS ALTERNADOS.
    ///
    /// Na tabela ÓTIMA (todas Cj−Zj ≤ 0), uma variável NÃO-BÁSICA com Cj−Zj = 0
    /// indica que pivotear nessa direção geraria outra solução com o mesmo valor de Z.
    /// Verifica apenas as colunas reais (exclui artificiais).
    /// </summary>
    public static bool TemOtimosAlternados(Tableau tableau)
    {
        var emBase = new HashSet<int>(tableau.Base);

        for (int j = 0; j < tableau.NumColunasReais; j++)
        {
            if (emBase.Contains(j)) continue; // variável básica — ignorar

            if (Math.Abs(tableau.CjMenosZj(j)) < Tolerancia)
                return true; // não-básica com Cj−Zj = 0 → ótimo alternado
        }

        return false;
    }
}
