# Third-party notices

Pawnsmith est distribué sous licence MIT (voir `LICENSE`). Il utilise les
composants tiers listés ci-dessous, qui restent soumis à leurs propres licences.

Ce fichier est tenu à jour **dans le commit qui introduit la dépendance**
(règle A.2 du cahier des charges). Une dépendance absente de cette liste est un
défaut, pas un oubli bénin.

Rappel de la politique de dépendances (A.2) : la licence d'une dépendance est un
critère de conception. Sont explicitement écartés **QuestPDF** (licence
commerciale « source-available », non approuvée OSI), **FluentAssertions ≥ 8**
(passé sous licence propriétaire Xceed en janvier 2025) et **AutoMapper**
(modèle commercial, et mapping invisible en relecture — DEC-021, DEC-027).

---

## .NET — tests uniquement

Aucun projet de `src/` ni de `tools/` ne référence de paquet NuGet à ce stade.

| Paquet | Version | Licence | Rôle |
|---|---|---|---|
| [xunit](https://github.com/xunit/xunit) | 2.9.3 | Apache-2.0 | Cadre de test (A.1) |
| [xunit.runner.visualstudio](https://github.com/xunit/visualstudio.xunit) | 3.1.4 | Apache-2.0 | Adaptateur de découverte des tests pour `dotnet test` |
| [Shouldly](https://github.com/shouldly/shouldly) | 4.3.0 | BSD-3-Clause | Bibliothèque d'assertions, retenue à la place de FluentAssertions (A.2) |
| [Microsoft.NET.Test.Sdk](https://github.com/microsoft/vstest) | 17.14.1 | MIT | Hôte d'exécution des tests |

---

## Dépendances prévues, pas encore introduites

Annoncées en A.1 mais absentes du code tant qu'aucun code ne les utilise. Elles
seront ajoutées à la table ci-dessus par le commit qui les référence.

| Paquet | Licence | Tranche |
|---|---|---|
| PDFsharp | MIT | T1 — rendu PDF (DEC-019) |
| Serilog | Apache-2.0 | T7 — journalisation (chapitre 8) |
