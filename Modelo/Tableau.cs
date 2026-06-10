using System.Globalization;

namespace Simplex.Modelo;

/// <summary>
/// Estrutura central do SIMPLEX tabular, na convenção usada em aula (Zj e Cj−Zj).
///
/// Layout das colunas:
///   [0, nVars)                      → variáveis de decisão  (x1..xn)
///   [nVars, nVars+nFolgas)          → variáveis de folga    (s1..) para restrições &lt;=
///   [nVars+nF, nVars+nF+nExcessos)  → variáveis de excesso  (e1..) para restrições &gt;=
///   [nVars+nF+nExc, ...)            → variáveis artificiais (a1..) para &gt;= e =
///
/// NumColunasReais = nVars + nFolgas + nExcessos  →  artificiais nunca entram como coluna pivô.
/// </summary>
public sealed class Tableau
{
    // ── estado mutável (alterado a cada pivoteamento) ─────────────────────────
    public double[,] Matriz         { get; }   // coeficientes [linha, coluna]
    public double[]  Rhs            { get; }   // lado direito b[i]
    public double[]  CustosObjetivo { get; }   // linha Cj (trocada entre Fase 1 e Fase 2)
    public int[]     Base           { get; }   // Base[i] = índice da coluna básica da linha i
    public double[]  CustosBase     { get; }   // Cb[i] = CustosObjetivo[Base[i]]

    // ── metainformação (fixada na construção) ─────────────────────────────────
    public int      NumVariaveisDecisao { get; }   // nVars — para extrair x1..xn da solução
    public int[]    IndicesArtificiais  { get; }   // índices de colunas artificiais
    public string[] NomesColunas        { get; }   // rótulos para impressão
    public int      NumColunasReais     { get; }   // limite superior da seleção de pivô

    public int NumLinhas  => Rhs.Length;
    public int NumColunas => CustosObjetivo.Length;

    // ── construtores ──────────────────────────────────────────────────────────

    /// Construtor completo — usado por APartirDe e DuasFases.
    public Tableau(
        double[,] matriz, double[] rhs, double[] custosObjetivo,
        int[] baseInicial, double[] custosBase,
        int numVariaveisDecisao, int[] indicesArtificiais,
        string[] nomesColunas, int numColunasReais)
    {
        Matriz              = matriz;
        Rhs                 = rhs;
        CustosObjetivo      = custosObjetivo;
        Base                = baseInicial;
        CustosBase          = custosBase;
        NumVariaveisDecisao = numVariaveisDecisao;
        IndicesArtificiais  = indicesArtificiais;
        NomesColunas        = nomesColunas;
        NumColunasReais     = numColunasReais;
    }

    /// Construtor compatível com a assinatura original (sem metainformação extra).
    public Tableau(
        double[,] matriz, double[] rhs, double[] custosObjetivo,
        int[] baseInicial, double[] custosBase)
        : this(matriz, rhs, custosObjetivo, baseInicial, custosBase,
               custosObjetivo.Length, Array.Empty<int>(),
               Enumerable.Range(0, custosObjetivo.Length)
                          .Select(i => $"x{i + 1}").ToArray(),
               custosObjetivo.Length)
    { }

    // ── métodos derivados (calculados a partir do estado) ─────────────────────

    /// Zj de uma coluna = Σ_i ( CustosBase[i] × Matriz[i, coluna] ).
    public double Zj(int coluna)
    {
        double z = 0.0;
        for (int i = 0; i < NumLinhas; i++)
            z += CustosBase[i] * Matriz[i, coluna];
        return z;
    }

    /// Cj − Zj de uma coluna: critério de entrada no SIMPLEX de maximização.
    /// Ótimo quando todas Cj-Zj ≤ 0.
    public double CjMenosZj(int coluna)
        => CustosObjetivo[coluna] - Zj(coluna);

    /// Valor atual de Z = Σ_i ( CustosBase[i] × Rhs[i] ).
    public double ValorObjetivoAtual()
    {
        double z = 0.0;
        for (int i = 0; i < NumLinhas; i++)
            z += CustosBase[i] * Rhs[i];
        return z;
    }

    // ── fábrica ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Constrói o tableau inicial na forma padrão a partir do problema.
    ///
    ///   &lt;=  : adiciona variável de FOLGA (+1)                       → entra na base, Cj = 0
    ///   &gt;=  : subtrai EXCESSO (−1) e adiciona ARTIFICIAL (+1)       → artificial entra na base
    ///   =   : adiciona ARTIFICIAL (+1)                               → entra na base
    ///
    /// Para minimização, os Cj das variáveis de decisão são negados
    /// (converte min f em max −f; o resultado final é negado de volta em SimplexSolver).
    /// </summary>
    public static Tableau APartirDe(ProblemaLinear problema)
    {
        int n = problema.NumeroVariaveis;
        int m = problema.Restricoes.Count;

        // Normalização: b < 0 → multiplica a restrição por −1 e inverte o operador.
        // Sem isso, uma restrição como −x1 <= −2 deixaria a base inicial inviável
        // e o solver poderia declarar "ótimo" num ponto fora da região viável.
        IReadOnlyList<Restricao> rs = problema.Restricoes
            .Select(r => r.LadoDireito >= 0 ? r
                : new Restricao(
                    r.Coeficientes.Select(c => -c).ToArray(),
                    r.Operador switch
                    {
                        Operador.MenorIgual => Operador.MaiorIgual,
                        Operador.MaiorIgual => Operador.MenorIgual,
                        _                   => Operador.Igual
                    },
                    -r.LadoDireito))
            .ToList();

        int nFolgas   = rs.Count(r => r.Operador == Operador.MenorIgual);
        int nExcessos = rs.Count(r => r.Operador == Operador.MaiorIgual);
        int nArtif    = rs.Count(r => r.Operador != Operador.MenorIgual);

        int colFolga   = n;
        int colExcesso = n + nFolgas;
        int colArtif   = n + nFolgas + nExcessos;
        int nCols      = colArtif + nArtif;
        int nColsReais = colArtif;   // artificiais excluídas da seleção de pivô

        double[,] mat   = new double[m, nCols];
        double[]  rhs   = new double[m];
        double[]  cj    = new double[nCols];
        int[]     base_ = new int[m];
        double[]  cb    = new double[m];
        string[]  nomes = new string[nCols];

        // Cj das variáveis de decisão (negado para minimização → converte em max)
        double sinal = problema.Tipo == TipoObjetivo.Maximizar ? 1.0 : -1.0;
        for (int j = 0; j < n; j++)
        {
            cj[j]    = sinal * problema.CoeficientesObjetivo[j];
            nomes[j] = $"x{j + 1}";
        }
        // Cj de folgas, excessos e artificiais = 0

        var indArtif = new List<int>();
        int iFolga = 0, iExcesso = 0, iArtif = 0;

        for (int i = 0; i < m; i++)
        {
            var r = rs[i];
            rhs[i] = r.LadoDireito;

            for (int j = 0; j < n; j++)
                mat[i, j] = r.Coeficientes[j];

            switch (r.Operador)
            {
                case Operador.MenorIgual:
                {
                    int c = colFolga + iFolga;
                    mat[i, c] = 1.0;
                    nomes[c]  = $"s{iFolga + 1}";
                    base_[i]  = c;
                    cb[i]     = 0.0;
                    iFolga++;
                    break;
                }
                case Operador.MaiorIgual:
                {
                    int cE = colExcesso + iExcesso;
                    mat[i, cE] = -1.0;
                    nomes[cE]  = $"e{iExcesso + 1}";
                    iExcesso++;

                    int cA = colArtif + iArtif;
                    mat[i, cA] = 1.0;
                    nomes[cA]  = $"a{iArtif + 1}";
                    indArtif.Add(cA);
                    base_[i] = cA;
                    cb[i]    = 0.0;
                    iArtif++;
                    break;
                }
                case Operador.Igual:
                {
                    int cA = colArtif + iArtif;
                    mat[i, cA] = 1.0;
                    nomes[cA]  = $"a{iArtif + 1}";
                    indArtif.Add(cA);
                    base_[i] = cA;
                    cb[i]    = 0.0;
                    iArtif++;
                    break;
                }
            }
        }

        return new Tableau(mat, rhs, cj, base_, cb,
                           n, indArtif.ToArray(), nomes, nColsReais);
    }

    // ── impressão (requisito 3) ────────────────────────────────────────────────

    /// Imprime o tableau completo: cabeçalho Cj, linhas Base/Cb/coeficientes/b, Zj e Cj−Zj.
    public void Imprimir(TextWriter saida, int iteracao = -1)
    {
        const int W = 9;

        // Formata um número com largura fixa; anula valores perto de zero.
        static string F(double v) =>
            Math.Abs(v) < 1e-9
                ? "0".PadLeft(W)
                : v.ToString("0.####", CultureInfo.InvariantCulture).PadLeft(W);

        if (iteracao >= 0)
            saida.WriteLine($"\n--- Iteração {iteracao} ---");

        int largura = 6 + W + 2 + W + (W + 2) * NumColunas;

        // Nomes das colunas
        saida.Write($"{"Var",-6} {"Cb",W}  {"b",W}");
        for (int j = 0; j < NumColunas; j++)
            saida.Write($"  {NomesColunas[j].PadLeft(W)}");
        saida.WriteLine();

        // Linha Cj
        saida.Write($"{"Cj→",-6} {"",W}  {"",W}");
        for (int j = 0; j < NumColunas; j++)
            saida.Write($"  {F(CustosObjetivo[j])}");
        saida.WriteLine();

        saida.WriteLine(new string('-', largura));

        // Linhas da base
        for (int i = 0; i < NumLinhas; i++)
        {
            saida.Write($"{NomesColunas[Base[i]],-6} {F(CustosBase[i])}  {F(Rhs[i])}");
            for (int j = 0; j < NumColunas; j++)
                saida.Write($"  {F(Matriz[i, j])}");
            saida.WriteLine();
        }

        saida.WriteLine(new string('-', largura));

        // Linha Zj
        saida.Write($"{"Zj",-6} {"",W}  {F(ValorObjetivoAtual())}");
        for (int j = 0; j < NumColunas; j++)
            saida.Write($"  {F(Zj(j))}");
        saida.WriteLine();

        // Linha Cj − Zj
        saida.Write($"{"Cj-Zj",-6} {"",W}  {"",W}");
        for (int j = 0; j < NumColunas; j++)
            saida.Write($"  {F(CjMenosZj(j))}");
        saida.WriteLine();
    }

}
