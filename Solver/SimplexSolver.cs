using Simplex.Modelo;
using Simplex.Diagnostico;

namespace Simplex.Solver;

/// <summary>
/// Loop principal do método SIMPLEX.
///
/// Algoritmo (convenção Cj−Zj, maximização):
///   1. Monta o tableau inicial (Tableau.APartirDe).
///   2. Se há artificiais → executa Fase 1 (DuasFases); verifica viabilidade.
///   3. Loop:
///        col = SeletorPivo.EscolherColuna(t)        → −1 se ótimo
///        DetectorAcidentes.EhIlimitado(t, col)      → para se ilimitado
///        lin = SeletorPivo.EscolherLinha(t, col)    → teste da razão θ
///        DetectorAcidentes.TemDegeneracao(t, col)   → marca flag
///        Pivotear(t, lin, col)                       → elimina Gauss-Jordan
///        t.Imprimir(...)                             → exibe iteração (requisito 3)
///   4. Extrai Z, x1..xn, detecta ótimos alternados, monta ResultadoSimplex.
///
/// Minimização: coeficientes negados em APartirDe; Z final é negado de volta.
/// </summary>
public sealed class SimplexSolver
{
    private readonly TextWriter _saida;
    private readonly int        _maxIteracoes;

    public SimplexSolver(TextWriter saida, int maxIteracoes = 1000)
    {
        _saida        = saida;
        _maxIteracoes = maxIteracoes;
    }

    // ── ponto de entrada público ───────────────────────────────────────────────

    /// Resolve o problema e devolve o ResultadoSimplex.
    public ResultadoSimplex Resolver(ProblemaLinear problema)
    {
        Tableau tab;
        int     iteracoes       = 0;
        bool    houveDegeneracao = false;

        // ── Fase 1 (quando necessário) ─────────────────────────────────────────
        // A decisão é tomada pelo TABLEAU (IndicesArtificiais), e não pelos operadores
        // originais do problema: a normalização de b < 0 em Tableau.APartirDe pode
        // transformar <= em >= e criar artificiais que não existiam no modelo digitado.
        var df = new DuasFases(_saida);
        tab = df.ExecutarFase1(problema); // retorna direto se não há artificiais

        if (tab.IndicesArtificiais.Length > 0)
        {
            if (DetectorAcidentes.EhInviavel(tab))
                return new ResultadoSimplex
                {
                    Status        = StatusSolucao.Inviavel,
                    SolucaoFinal  = true,
                    Observacao    = "Problema inviável: ao fim da Fase 1 há variável artificial " +
                                    "na base com valor positivo (região viável vazia)."
                };

            _saida.WriteLine("\n=== FASE 2: otimizar função objetivo original ===");
        }

        tab.Imprimir(_saida, 0);

        // ── Loop principal SIMPLEX (Fase 2 ou problema puro de maximização) ────
        bool otimoAtingido = false;

        while (iteracoes < _maxIteracoes)
        {
            // Passo 1 — escolhe a coluna pivô (critério Cj−Zj máximo positivo)
            int col = SeletorPivo.EscolherColuna(tab);
            if (col < 0)
            {
                otimoAtingido = true;
                break; // todas Cj−Zj ≤ 0 → ótimo
            }

            // Passo 2 — verifica se o problema é ilimitado nessa direção
            if (DetectorAcidentes.EhIlimitado(tab, col))
                return new ResultadoSimplex
                {
                    Status           = StatusSolucao.Ilimitado,
                    NumeroIteracoes  = iteracoes,
                    SolucaoFinal     = true,
                    Observacao       = $"Problema sem fronteira: coluna pivô (coluna {col + 1}) " +
                                       "não tem elemento positivo no teste da razão θ."
                };

            // Passo 3 — escolhe a linha pivô (menor θ positivo)
            int lin = SeletorPivo.EscolherLinha(tab, col);

            // Passo 4 — verifica degeneração antes de pivotear
            if (DetectorAcidentes.TemDegeneracao(tab, col))
                houveDegeneracao = true;

            // Passo 5 — pivoteamento (eliminação de Gauss-Jordan)
            Pivotear(tab, lin, col);
            iteracoes++;

            // Exibe o tableau após cada iteração (requisito 3)
            tab.Imprimir(_saida, iteracoes);
        }

        // Limite de iterações atingido sem convergência
        if (!otimoAtingido)
            return new ResultadoSimplex
            {
                Status          = StatusSolucao.Interrompido,
                NumeroIteracoes = iteracoes,
                SolucaoFinal    = false,
                Observacao      = $"Interrompido após {iteracoes} iterações (limite máximo atingido)."
            };

        // ── Extrai a solução ───────────────────────────────────────────────────

        // Z: desfaz a negação feita para minimização
        double z = tab.ValorObjetivoAtual();
        if (problema.Tipo == TipoObjetivo.Minimizar) z = -z;

        // Valores de x1..xn: variáveis não-básicas ficam com 0
        var valores = new double[problema.NumeroVariaveis];
        for (int i = 0; i < tab.NumLinhas; i++)
            if (tab.Base[i] < problema.NumeroVariaveis)
                valores[tab.Base[i]] = tab.Rhs[i];

        // Plus 2 — verifica ótimos alternados
        bool temAlternados = DetectorAcidentes.TemOtimosAlternados(tab);

        // Define status final
        StatusSolucao status;
        string?       observacao;

        if (temAlternados)
        {
            status     = StatusSolucao.OtimosAlternados;
            observacao = "Existem ótimos alternados: variável não-básica com Cj−Zj = 0 " +
                         "na tabela ótima indica outra solução com o mesmo valor de Z.";
        }
        else if (houveDegeneracao)
        {
            status     = StatusSolucao.Degenerado;
            observacao = "Degeneração detectada: empate no teste da razão θ ou variável " +
                         "básica com valor zero durante a resolução.";
        }
        else
        {
            status     = StatusSolucao.Otimo;
            observacao = null;
        }

        return new ResultadoSimplex
        {
            Status           = status,
            ValorObjetivo    = z,
            ValoresVariaveis = valores,
            NumeroIteracoes  = iteracoes,
            SolucaoFinal     = true,
            Observacao       = observacao
        };
    }

    // ── pivoteamento (Gauss-Jordan) ────────────────────────────────────────────

    /// <summary>
    /// Operação de pivoteamento na posição (linhaPivo, colunaPivo).
    ///
    /// 1. Divide todos os elementos da linha pivô pelo elemento pivô
    ///    → elemento pivô vira 1 (linha normalizada).
    /// 2. Para cada outra linha i: subtrai (Matriz[i, colunaPivo] × linha normalizada)
    ///    → zera o elemento na coluna pivô das outras linhas (eliminação de Gauss-Jordan).
    /// 3. Atualiza Base[linhaPivo] = colunaPivo e CustosBase[linhaPivo] = Cj[colunaPivo].
    /// </summary>
    internal static void Pivotear(Tableau tableau, int linhaPivo, int colunaPivo)
    {
        int m   = tableau.NumLinhas;
        int n   = tableau.NumColunas;
        double ep = tableau.Matriz[linhaPivo, colunaPivo]; // elemento pivô

        // Passo 1 — normaliza a linha pivô (divide pelo elemento pivô)
        for (int j = 0; j < n; j++)
            tableau.Matriz[linhaPivo, j] /= ep;
        tableau.Rhs[linhaPivo] /= ep;

        // Passo 2 — zera a coluna pivô nas demais linhas
        for (int i = 0; i < m; i++)
        {
            if (i == linhaPivo) continue;

            double fator = tableau.Matriz[i, colunaPivo];
            if (Math.Abs(fator) < 1e-14) continue; // já é zero, pula

            for (int j = 0; j < n; j++)
                tableau.Matriz[i, j] -= fator * tableau.Matriz[linhaPivo, j];
            tableau.Rhs[i] -= fator * tableau.Rhs[linhaPivo];
        }

        // Passo 3 — atualiza a base e o custo base da linha pivô
        tableau.Base[linhaPivo]       = colunaPivo;
        tableau.CustosBase[linhaPivo] = tableau.CustosObjetivo[colunaPivo];
    }
}
