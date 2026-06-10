using Simplex.Modelo;

namespace Simplex.UI;

/// <summary>
/// Problemas de teste prontos (os mesmos do README), para uso rápido no menu inicial.
/// </summary>
public static class ProblemasExemplo
{
    public static readonly IReadOnlyList<(string Descricao, ProblemaLinear Problema)> Lista = new[]
    {
        ("Max 3x1+5x2; x1<=4; 2x2<=12; 3x1+2x2<=18  (Z=36)",
            Criar(TipoObjetivo.Maximizar, new[] { 3.0, 5 },
                (new[] { 1.0, 0 }, Operador.MenorIgual, 4.0),
                (new[] { 0.0, 2 }, Operador.MenorIgual, 12.0),
                (new[] { 3.0, 2 }, Operador.MenorIgual, 18.0))),

        ("Max 5x1+4x2; 6x1+4x2<=24; x1+2x2<=6  (Z=21)",
            Criar(TipoObjetivo.Maximizar, new[] { 5.0, 4 },
                (new[] { 6.0, 4 }, Operador.MenorIgual, 24.0),
                (new[] { 1.0, 2 }, Operador.MenorIgual, 6.0))),

        ("Max 2x1+x2; x1-x2<=10; 2x1-x2<=40  (Ilimitado)",
            Criar(TipoObjetivo.Maximizar, new[] { 2.0, 1 },
                (new[] { 1.0, -1 }, Operador.MenorIgual, 10.0),
                (new[] { 2.0, -1 }, Operador.MenorIgual, 40.0))),

        ("Max 3x1+2x2; 2x1+x2<=2; 3x1+4x2>=12  (Inviável)",
            Criar(TipoObjetivo.Maximizar, new[] { 3.0, 2 },
                (new[] { 2.0, 1 }, Operador.MenorIgual, 2.0),
                (new[] { 3.0, 4 }, Operador.MaiorIgual, 12.0))),

        ("Max 3x1+9x2; x1+4x2<=8; x1+2x2<=4  (Z=18, degeneração)",
            Criar(TipoObjetivo.Maximizar, new[] { 3.0, 9 },
                (new[] { 1.0, 4 }, Operador.MenorIgual, 8.0),
                (new[] { 1.0, 2 }, Operador.MenorIgual, 4.0))),

        ("Max 2x1+4x2; x1+2x2<=5; x1+x2<=4  (Z=10, ótimos alternados)",
            Criar(TipoObjetivo.Maximizar, new[] { 2.0, 4 },
                (new[] { 1.0, 2 }, Operador.MenorIgual, 5.0),
                (new[] { 1.0, 1 }, Operador.MenorIgual, 4.0))),

        ("Min 4x1+x2; 3x1+x2=3; 4x1+3x2>=6; x1+2x2<=4  (C=3.4, duas fases)",
            Criar(TipoObjetivo.Minimizar, new[] { 4.0, 1 },
                (new[] { 3.0, 1 }, Operador.Igual, 3.0),
                (new[] { 4.0, 3 }, Operador.MaiorIgual, 6.0),
                (new[] { 1.0, 2 }, Operador.MenorIgual, 4.0))),

        ("Min 2x1+3x2; x1+x2>=4; x1+2x2>=6  (C=10, duas fases)",
            Criar(TipoObjetivo.Minimizar, new[] { 2.0, 3 },
                (new[] { 1.0, 1 }, Operador.MaiorIgual, 4.0),
                (new[] { 1.0, 2 }, Operador.MaiorIgual, 6.0))),
    };

    private static ProblemaLinear Criar(
        TipoObjetivo tipo,
        double[] objetivo,
        params (double[] Coef, Operador Op, double Rhs)[] restricoes)
    {
        return new ProblemaLinear(
            tipo,
            objetivo,
            restricoes.Select(r => new Restricao(r.Coef, r.Op, r.Rhs)).ToList());
    }
}
