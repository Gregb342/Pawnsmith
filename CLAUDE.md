# CLAUDE.md — méthode de travail

Ce fichier condense les règles contraignantes du projet à l'usage d'un assistant
de code. Il ne remplace pas les documents de référence, il y renvoie :

- [`docs/pawnsmith-bible.md`](docs/pawnsmith-bible.md) — **à lire en premier.**
  Vision, glossaire contraignant (chapitre 2), modèle de données, architecture,
  modèle de menace, et **journal des décisions (chapitre 11), qui fait foi**.
- [`docs/pawnsmith-cahier-des-charges-t1.md`](docs/pawnsmith-cahier-des-charges-t1.md)
  — fondations (partie A) et première tranche (partie B).

En cas de contradiction entre ce fichier et l'un des deux documents, **ce sont
les documents qui gagnent**, et il faut le signaler.

---

## 1. Méthode de travail (chapitre 0 du cahier des charges)

Le porteur du projet **relit intégralement tout le code produit**, tranche par
tranche. Toute la méthode découle de cette contrainte.

- **Travailler par petites tâches successives**, chacune close par un commit
  atteignable en une relecture. Ne pas produire une tranche entière d'un seul
  jet, ni un commit monolithique.
- **Expliciter les choix non évidents** en commentaire ou en message de commit,
  en particulier **les conversions d'unités et les calculs géométriques**.
- **Aucun code implicite ou magique** : pas de génération automatique de
  mapping, pas de convention cachée, pas d'abstraction introduite « au cas où ».
- **Quand une information manque, s'arrêter et demander** plutôt que de choisir
  une valeur plausible. Une valeur physique inventée coûte une impression papier
  à détecter — et se détecte au ciseau, pas au test.
- **Le vocabulaire du chapitre 2 de la bible est contraignant** et repris tel
  quel dans les noms de types : `Gabarit`, `Candidat`, `Planche`, `Taille`,
  `Geometrie`. Toute divergence est un défaut.
- **Ne pas anticiper les tranches à venir.** Les ports du chapitre 7 de la bible
  sont une intention de conception, pas du code à écrire aujourd'hui.

## 2. Politique de dépendances (A.2)

**La licence d'une dépendance est un critère de conception, au même titre que
ses fonctionnalités.** Le projet est open source et destiné à être repris ; une
dépendance dont le modèle change impose une dette à tous ses utilisateurs aval.

Règle générale : **toute nouvelle dépendance doit être justifiée** dans le
message du commit qui l'introduit, et ajoutée à
[`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) **dans le même commit**.
En cas de doute entre une dépendance et vingt lignes de code, écrire les vingt
lignes.

### Interdits explicites

| Paquet | Motif | Remplacement |
|---|---|---|
| **QuestPDF** | Licence commerciale « source-available », non approuvée OSI. Secteur public et sociétés cotées exclus quel que soit leur chiffre d'affaires. | **PDFsharp** (MIT), DEC-019 |
| **FluentAssertions ≥ 8** | Passé sous licence propriétaire Xceed en janvier 2025 ; usage commercial payant. La 7.x reste libre, mais épingler une version majeure pour raison de licence est une dette gratuite sur un projet neuf. | **Shouldly** |
| **AutoMapper** | Modèle commercial, et surtout mapping invisible en relecture — ce qui contredit frontalement DEC-021 et DEC-027. | Mapping manuel par méthodes d'extension `ToDto()` |

### Choix d'outillage arrêtés (A.1)

.NET 10 (LTS) · React + TypeScript outillé par Vite · Node 22 LTS ·
xUnit + Shouldly · PDFsharp · Serilog · licence MIT.

## 3. Règle de dépendance entre projets (A.3)

```
Domain  ←  Application  ←  Infrastructure  ←  Api
```

`Domain` ne référence **rien** : ni projet, ni paquet NuGet. `Application`
référence `Domain`. `Infrastructure` référence `Application` et `Domain`. `Api`
référence tout. **Aucune flèche en sens inverse, jamais.**

`tools/Pawnsmith.Cli` est un harnais **jetable, non livré**, exclu de l'image
Docker (B.7). Ne rien y mettre qui ressemble à de la logique.

## 4. Conventions (A.8)

- **Conventional Commits** : `feat:`, `fix:`, `docs:`, `test:`, `chore:`,
  `refactor:`, plus `build:` et `ci:`.
- **Versionnement sémantique**, à partir de `0.1.0`.
- Messages de commit et **commentaires de code en anglais** ; **documentation
  fonctionnelle en français**.
- `var` autorisé **uniquement** quand le type est apparent à droite
  (`.editorconfig`, sévérité `warning`, donc erreur de compilation via
  `TreatWarningsAsErrors`).
- **Aucune chaîne en dur**, front comme back, dès le squelette (chapitre 10 de
  la bible). Front : catalogues JSON `react-i18next`. Back : `.resx`.
  L'API renvoie des **codes d'erreur**, jamais des messages traduits.

## 5. Valeurs physiques

**Aucune valeur physique ne doit apparaître comme constante dans le code.**
Toutes vivent dans [`config/calibration.json`](config/calibration.json) (B.2).
Elles sont **provisoires** et seront remplacées après un tirage papier de
contrôle ; ce remplacement ne doit demander aucune modification de code.

Piège à ne jamais confondre : `gridFootprintMm` (emprise sur la grille de jeu) et
`pawnHeightMm` (hauteur visuelle debout) sont **deux dimensions indépendantes**.
Ne jamais déduire l'une de l'autre.

## 6. Vérifications avant commit

```bash
dotnet build Pawnsmith.sln
dotnet test Pawnsmith.sln
cd src/Pawnsmith.Web && npm run build
docker build -t pawnsmith .
```

Et vérifier qu'aucun `bin/`, `obj/` ou `node_modules/` n'est suivi par git.

## 7. État actuel

Les **fondations (partie A)** sont en place. Les projets .NET sont **vides** :
aucune logique métier nulle part, seulement le strict minimum pour compiler.
La partie B (tranche T1 — moteur de mise en page et rendu PDF) **n'est pas
commencée**.
