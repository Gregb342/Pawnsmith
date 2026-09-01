# Third-party notices

Pawnsmith est distribué sous licence MIT (voir `LICENSE`). Il utilise les
composants tiers listés ci-dessous, qui restent soumis à leurs propres licences.

Ce fichier est tenu à jour **dans le commit qui introduit la dépendance**
(règle A.2 du cahier des charges). Une dépendance absente de cette liste est un
défaut, pas un oubli bénin.

Rappel de la politique de dépendances (A.2) : la licence d'une dépendance est un
critère de conception, et la chaîne de dépendances est une surface de traitement
de données personnelles. Sont explicitement écartés **QuestPDF** (licence
commerciale « source-available », non approuvée OSI), **FluentAssertions ≥ 8**
(passé sous licence propriétaire Xceed en janvier 2025), **AutoMapper**
(modèle commercial, et mapping invisible en relecture — DEC-021, DEC-027) et
**Moq** (a embarqué en août 2023, dans une version mineure, un composant
extrayant l'adresse e-mail du développeur depuis sa configuration Git pour
l'envoyer à un service tiers ; utiliser **NSubstitute**).

---

## .NET — production

| Paquet | Version | Licence | Rôle |
|---|---|---|---|
| [PDFsharp](https://github.com/empira/PDFsharp) | 6.2.4 | MIT | Rendu PDF, référencé par `Pawnsmith.Infrastructure` (A.1, DEC-019) |
| Microsoft.Extensions.DependencyInjection.Abstractions | 8.0.2 | MIT | Dépendance transitive de PDFsharp |
| Microsoft.Extensions.Logging.Abstractions | 8.0.3 | MIT | Dépendance transitive de PDFsharp |
| System.Security.Cryptography.Pkcs | 8.0.1 | MIT | Dépendance transitive de PDFsharp (signature de PDF) |

## Polices embarquées

| Ressource | Licence | Rôle |
|---|---|---|
| [DejaVu Sans](https://dejavu-fonts.github.io/) 2.37 — `src/Pawnsmith.Infrastructure/Fonts/DejaVuSans.ttf` | Bitstream Vera / DejaVu (libre, texte intégral dans `Fonts/DejaVuSans-LICENSE.txt`) | Unique police de la planche : légende de calibration et étiquette de page |

> **Pourquoi une police est un sujet de licence.** Une police utilisée dans un
> PDF y est **embarquée**, donc redistribuée avec chaque planche produite. Le
> choix engage tous les utilisateurs aval, pas seulement ce dépôt. La licence
> Bitstream Vera autorise explicitement la redistribution et l'incorporation
> dans des documents.
>
> Elle est **embarquée dans l'assembly** plutôt que cherchée sur la machine :
> PDFsharp 6 n'accède pas aux polices système, et l'image runtime ASP.NET n'en
> contient aucune. C'est aussi ce qui garantit un rendu identique sous Windows,
> en conteneur et en intégration continue.

## .NET — tests uniquement

| Paquet | Version | Licence | Rôle |
|---|---|---|---|
| [xunit](https://github.com/xunit/xunit) | 2.9.3 | Apache-2.0 | Cadre de test (A.1) |
| [xunit.runner.visualstudio](https://github.com/xunit/visualstudio.xunit) | 3.1.4 | Apache-2.0 | Adaptateur de découverte des tests pour `dotnet test` |
| [Shouldly](https://github.com/shouldly/shouldly) | 4.3.0 | BSD-3-Clause | Bibliothèque d'assertions, retenue à la place de FluentAssertions (A.2) |
| [Microsoft.NET.Test.Sdk](https://github.com/microsoft/vstest) | 17.14.1 | MIT | Hôte d'exécution des tests |

---

## Front — `src/Pawnsmith.Web`

| Paquet | Version | Licence | Rôle |
|---|---|---|---|
| [react](https://github.com/facebook/react) | 19.2.x | MIT | Bibliothèque d'interface (DEC-018) |
| [react-dom](https://github.com/facebook/react) | 19.2.x | MIT | Rendu DOM de React |
| [i18next](https://github.com/i18next/i18next) | 25.x | MIT | Moteur de localisation, socle de `react-i18next` |
| [react-i18next](https://github.com/i18next/react-i18next) | 16.x | MIT | Liaison React de i18next (A.5, chapitre 10 de la bible) |
| [vite](https://github.com/vitejs/vite) | 7.x | MIT | Outillage de compilation du front (A.1) |
| [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react) | 5.x | MIT | Prise en charge de JSX et du rafraîchissement à chaud |
| [typescript](https://github.com/microsoft/TypeScript) | 5.9.x | Apache-2.0 | Compilateur TypeScript (A.1) |
| [@types/react](https://github.com/DefinitelyTyped/DefinitelyTyped) | 19.2.x | MIT | Définitions de types pour React |
| [@types/react-dom](https://github.com/DefinitelyTyped/DefinitelyTyped) | 19.2.x | MIT | Définitions de types pour React DOM |

---

## Images de base des conteneurs

| Image | Licence |
|---|---|
| `mcr.microsoft.com/dotnet/sdk:10.0` | MIT (composants .NET), voir la licence de l'image |
| `mcr.microsoft.com/dotnet/aspnet:10.0` | MIT (composants .NET), voir la licence de l'image |
| `node:22-alpine` | MIT (Node.js), voir la licence de l'image |
