using Simplex.Modelo;
using Simplex.Diagnostico;

namespace Simplex.Solver;

/// <summary>
/// Método das DUAS FASES para problemas com restrições &gt;= ou = (plus 1 — minimização).
/// O PDF pede explicitamente NÃO usar Big-M; este método é a alternativa correta.
///
/// Fase 1: minimizar Σ a_i (soma das artificiais) para encontrar uma base viável.
///   • Equivale a maximizar −Σ a_i, logo Cj = −1 para artificiais, 0 para as demais.
///   • Ao fim, se Z_fase1 &lt; 0 (tolerância), o problema é INVIÁVEL.
///
/// Fase 2: restaura o Cj original (da Fase 2) e entrega o tableau ao SimplexSolver
///         para continuar a otimização normal.
/// </summary>
public sealed class DuasFases
{
    private readonly TextWriter _saida;

    public DuasFases(TextWriter saida) => _saida = saida;

    /// <summary>
    /// Executa a Fase 1 e devolve o tableau pronto para a Fase 2.
    ///
    /// Após o retorno, use DetectorAcidentes.EhInviavel() para verificar
    /// se a solução encontrada é de fato viável.
    /// </summary>
    public Tableau ExecutarFase1(ProblemaLinear problema)
    {
        // Monta o tableau com o Cj original (Fase 2) já embutido.
        Tableau tab = Tableau.APartirDe(problema);

        // Se não há artificiais, o tableau já é viável — retorna direto.
        if (tab.IndicesArtificiais.Length == 0)
            return tab;

        // ── Configura o objetivo da Fase 1 ──────────────────────────────────
        // Salva os Cj da Fase 2 para restaurar depois.
        double[] cjFase2 = (double[])tab.CustosObjetivo.Clone();

        // Fase 1: max(−Σ a_i)  →  Cj = 0 para variáveis reais, −1 para artificiais.
        Array.Clear(tab.CustosObjetivo, 0, tab.NumColunas);
        foreach (int ai in tab.IndicesArtificiais)
            tab.CustosObjetivo[ai] = -1.0;

        // Atualiza Cb para refletir o novo Cj (artificiais na base têm Cb = −1).
        for (int i = 0; i < tab.NumLinhas; i++)
            tab.CustosBase[i] = tab.CustosObjetivo[tab.Base[i]];

        _saida.WriteLine("\n=== FASE 1: minimizar variáveis artificiais ===");
        tab.Imprimir(_saida, 0);

        // ── Loop SIMPLEX da Fase 1 ───────────────────────────────────────────
        for (int iter = 1; iter <= 1000; iter++)
        {
            int col = SeletorPivo.EscolherColuna(tab);
            if (col < 0) break; // ótimo da Fase 1 atingido

            // Ilimitado na Fase 1 não deveria ocorrer num problema bem formado,
            // mas tratamos por segurança.
            if (DetectorAcidentes.EhIlimitado(tab, col)) break;

            int lin = SeletorPivo.EscolherLinha(tab, col);
            if (lin < 0) break;

            SimplexSolver.Pivotear(tab, lin, col);
            tab.Imprimir(_saida, iter);
        }

        // ── Restaura Cj da Fase 2 ────────────────────────────────────────────
        Array.Copy(cjFase2, tab.CustosObjetivo, tab.NumColunas);

        // Atualiza Cb para refletir o Cj original.
        for (int i = 0; i < tab.NumLinhas; i++)
            tab.CustosBase[i] = tab.CustosObjetivo[tab.Base[i]];

        return tab;
    }
}
