# Solver SIMPLEX — Otimização de Sistemas Lineares

Programa que resolve problemas de Programação Linear pelo método SIMPLEX (PUCPR — Eng. Computação).

## Estrutura

```
Program.cs                       Ponto de entrada (lê → resolve → exibe)
Modelo/
  Enums.cs                       TipoObjetivo, Operador, StatusSolucao
  Restricao.cs                   Uma restrição linear
  ProblemaLinear.cs              Modelo de domínio do problema
  Tableau.cs                     Tabela SIMPLEX (estado) + forma padrão + normalização b<0
  ResultadoSimplex.cs            Resposta: Z, variáveis, status, iterações
Solver/
  SimplexSolver.cs               Loop principal + pivoteamento Gauss-Jordan
  SeletorPivo.cs                 Escolha de coluna (Cj−Zj máx.) e linha (menor θ)
  DuasFases.cs                   Fase 1: min Σ artificiais (sem Big-M)
Diagnostico/
  DetectorAcidentes.cs           Degeneração / inviável / ilimitado / ótimos alternados
UI/
  InterfaceConsole.cs            Entrada e saída pelo console
  ProblemasExemplo.cs            Problemas de teste prontos (menu inicial)
```

## Mapa requisitos → onde implementar

| Requisito | Local |
|-----------|-------|
| 1. Maximização | `SimplexSolver.Resolver` |
| 2. Interface + tutorial | `UI/InterfaceConsole`, `TUTORIAL.txt` |
| 3. Nº iterações + estado da tabela | `SimplexSolver` + `Tableau.Imprimir` |
| 4. Z/C ótimo + variáveis | `ResultadoSimplex` |
| 5. Degeneração | `DetectorAcidentes.TemDegeneracao` |
| 6. Inviável | `DetectorAcidentes.EhInviavel` |
| 7. Ilimitado | `DetectorAcidentes.EhIlimitado` |
| + Minimização (duas fases) | `DuasFases` |
| + Ótimos alternados | `DetectorAcidentes.TemOtimosAlternados` |

## Executar

```bash
dotnet run
```

## Problemas de teste (resultados conhecidos)

| Problema | Esperado |
|----------|----------|
| Max 3x1+5x2; x1<=4; 2x2<=12; 3x1+2x2<=18 | Z=36, x=(2,6), 2 iterações |
| Max 5x1+4x2; 6x1+4x2<=24; x1+2x2<=6 | Z=21, x=(3, 1.5) |
| Max 2x1+x2; x1−x2<=10; 2x1−x2<=40 | Ilimitado |
| Max 3x1+2x2; 2x1+x2<=2; 3x1+4x2>=12 | Inviável |
| Max 3x1+9x2; x1+4x2<=8; x1+2x2<=4 | Z=18, degeneração |
| Max 2x1+4x2; x1+2x2<=5; x1+x2<=4 | Z=10, ótimos alternados |
| Min 4x1+x2; 3x1+x2=3; 4x1+3x2>=6; x1+2x2<=4 | C=3.4, x=(0.4, 1.8) — duas fases |
| Min 2x1+3x2; x1+x2>=4; x1+2x2>=6 | C=10, x=(2,2) — duas fases |
# simplex-solver
