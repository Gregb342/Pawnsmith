# Pawnsmith — Cahier des charges : Fondations et tranche T1

| | |
|---|---|
| **Version** | 1.0 |
| **Date** | 26 août 2026 |
| **Document parent** | `pawnsmith-bible.md` — à lire en premier |
| **Portée** | Squelette du dépôt, chaîne de compilation, puis moteur de mise en page et rendu PDF |

---

## 0. Consignes de travail

Ce document est destiné à un assistant de code. Le porteur du projet **relit intégralement tout le code produit**, tranche par tranche. Cela impose une méthode :

- Travailler par **petites tâches successives**, chacune close par un commit atteignable en une relecture. Ne pas produire l'ensemble de T1 d'un seul jet.
- **Expliciter les choix non évidents** en commentaire ou en message de commit, en particulier les conversions d'unités et les calculs géométriques.
- **Aucun code implicite ou magique** : pas de génération automatique de mapping, pas de conventions cachées, pas d'abstraction introduite « au cas où ».
- Quand une information manque dans ce document, **s'arrêter et demander** plutôt que de choisir une valeur plausible. Les valeurs physiques inventées coûtent une impression papier à détecter.
- Le vocabulaire du chapitre 2 de la bible est contraignant : `Gabarit`, `Candidat`, `Planche`, `Taille`, `Geometrie`. Il est repris tel quel dans les noms de types.

---

## PARTIE A — FONDATIONS

## A.1 Plateformes et outillage

| Élément | Choix | Motif |
|---|---|---|
| Runtime .NET | **.NET 10 (LTS)** | Support long terme |
| Front | **React + TypeScript**, outillé par **Vite** | Écosystème, DEC-018 |
| Node | **22 LTS** | |
| Tests | **xUnit** + **Shouldly** | Voir A.2 |
| Rendu PDF | **PDFsharp** (MIT) | DEC-019 |
| Journalisation | **Serilog** (Apache 2.0) | Chapitre 8 de la bible |
| Licence du projet | **MIT** | |

## A.2 Politique de dépendances

**La licence d'une dépendance est un critère de conception, au même titre que ses fonctionnalités.** Ce projet est open source et destiné à être repris ; une dépendance dont le modèle change impose une dette à tous ses utilisateurs aval.

Interdits explicites, avec leur motif :

| Paquet | Motif |
|---|---|
| **QuestPDF** | Licence commerciale « source-available », non approuvée OSI. Secteur public et sociétés cotées exclus quel que soit leur chiffre d'affaires. |
| **FluentAssertions** ≥ 8 | Passé sous licence propriétaire Xceed en janvier 2025 ; usage commercial payant. La 7.x reste libre mais épingler une version majeure pour raison de licence est une dette gratuite sur un projet neuf. Utiliser **Shouldly**. |
| **AutoMapper** | Modèle commercial, et surtout : mapping invisible en relecture, ce qui contredit DEC-021 et DEC-027. Mapping manuel par méthodes d'extension. |

Règle générale : **toute nouvelle dépendance doit être justifiée** dans le message de commit qui l'introduit, et ajoutée à `THIRD-PARTY-NOTICES.md` dans le même commit. En cas de doute entre une dépendance et vingt lignes de code, écrire les vingt lignes.

## A.3 Structure du dépôt

```
pawnsmith/
├── .github/workflows/ci.yml
├── config/
│   └── calibration.json              # valeurs physiques, voir B.2
├── docs/
│   ├── pawnsmith-bible.md
│   └── cahier-des-charges-t1.md
├── src/
│   ├── Pawnsmith.Domain/             # pur, aucune dépendance externe
│   ├── Pawnsmith.Application/        # cas d'usage, ports
│   ├── Pawnsmith.Infrastructure/     # PDFsharp, système de fichiers, Serilog
│   ├── Pawnsmith.Api/                # ASP.NET Core, sert aussi le front compilé
│   └── Pawnsmith.Web/                # front React (squelette en T1)
├── tests/
│   ├── Pawnsmith.Domain.Tests/
│   └── Pawnsmith.Infrastructure.Tests/
├── tools/
│   └── Pawnsmith.Cli/                # JETABLE — non livré, voir B.7
├── .editorconfig
├── .gitignore
├── Directory.Build.props
├── Dockerfile
├── LICENSE                            # MIT
├── README.md
├── THIRD-PARTY-NOTICES.md
└── Pawnsmith.sln
```

**Règle de dépendance, à respecter strictement** : `Domain` ne référence rien. `Application` référence `Domain`. `Infrastructure` référence `Application` et `Domain`. `Api` référence tout. Aucune flèche en sens inverse, jamais.

## A.4 Réglages de compilation

`Directory.Build.props`, appliqué à tous les projets :

- `<Nullable>enable</Nullable>`
- `<ImplicitUsings>enable</ImplicitUsings>`
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- `<LangVersion>latest</LangVersion>`
- `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`

`.editorconfig` avec les conventions C# par défaut de Microsoft, `var` autorisé uniquement quand le type est apparent à droite.

## A.5 Squelette du front

Objectif de T1 : **prouver la chaîne de compilation et le câblage de la localisation**, rien de plus.

- Application Vite + React + TypeScript.
- `react-i18next` configuré avec deux catalogues, `fr` et `en`, chacun dans un fichier JSON distinct.
- Une seule page affichant le nom du produit et un sélecteur de langue fonctionnel.
- **Aucune chaîne en dur, dès ce squelette.** C'est l'habitude qu'on installe, pas la fonctionnalité.
- Aucun appel à l'API. Aucun composant métier.

## A.6 Conteneurisation

`Dockerfile` en plusieurs étapes, opérationnel dès T1 :

1. Étape Node : installation des dépendances et compilation de `Pawnsmith.Web`.
2. Étape SDK .NET : restauration, compilation et publication de `Pawnsmith.Api`.
3. Étape finale sur l'image runtime ASP.NET : copie du binaire publié et du `dist` du front dans `wwwroot`.

L'API sert les fichiers statiques du front. Même origine, donc **pas de configuration CORS**.

Deux volumes déclarés : `/app/data/projects` et `/app/data/logs` (DEC-022).

Le `README.md` documente la forme canonique de lancement : `docker run -p 127.0.0.1:8080:8080 …`, avec la mention explicite que l'application **n'a pas d'authentification** et ne doit pas être publiée sur toutes les interfaces (MEN-004).

## A.7 Intégration continue

`.github/workflows/ci.yml`, déclenché à chaque poussée et sur les demandes de fusion :

1. Compilation du front.
2. Compilation de la solution .NET.
3. Exécution de tous les tests.

Rien d'autre à ce stade. Pas de publication, pas d'analyse, pas de couverture.

## A.8 Conventions

- **Conventional Commits** (`feat:`, `fix:`, `docs:`, `test:`, `chore:`, `refactor:`).
- **Versionnement sémantique**, à partir de `0.1.0`.
- Messages de commit et commentaires de code **en anglais** ; documentation fonctionnelle **en français**.
- `README.md` contient : objectif du projet, prérequis, lancement en développement hors conteneur, lancement des tests, lancement en conteneur, licence.

---

## PARTIE B — TRANCHE T1 : MOTEUR DE MISE EN PAGE ET RENDU PDF

## B.1 Objectif et périmètre

**Entrée** : un manifeste JSON et un dossier de PNG à fond transparent, déjà détourés, fournis à la main.
**Sortie** : un fichier PDF calibré, prêt à imprimer, découper, plier et monter.

### Dans le périmètre

- Le domaine géométrique : tailles, géométries, calcul de grille, pagination, positionnement.
- Le rendu PDF via PDFsharp.
- La lecture du manifeste et du fichier de calibration.
- Un point d'entrée en ligne de commande jetable pour produire le PDF (B.7).

### Hors périmètre — ne rien écrire de tout cela

Génération d'images, détourage, appels HTTP, modèle de projet complet, catalogue, composition de prompts, points de terminaison d'API, interface au-delà du squelette A.5, persistance de projet.

> **Note de conception.** L'entrée de T1 — des PNG fournis à la main — préfigure l'import d'images externes, retenu comme évolution ultérieure (EVO). Le code ne doit rien faire qui l'empêche, mais ne doit rien construire pour l'anticiper non plus.

## B.2 Fichier de calibration

Toutes les valeurs physiques vivent dans `config/calibration.json`. **Aucune de ces valeurs ne doit apparaître comme constante dans le code.** Elles seront remplacées après un tirage papier de contrôle ; le code ne doit pas avoir à changer.

```json
{
  "versionSchema": 1,
  "sizes": {
    "Moyenne":     { "gridFootprintMm": 25.4,  "pawnWidthMm": 25.4,  "pawnHeightMm": 50.0 },
    "Grande":      { "gridFootprintMm": 50.8,  "pawnWidthMm": 50.8,  "pawnHeightMm": 75.0 },
    "TresGrande":  { "gridFootprintMm": 76.2,  "pawnWidthMm": 76.2,  "pawnHeightMm": 100.0 },
    "Gigantesque": { "gridFootprintMm": 101.6, "pawnWidthMm": 101.6, "pawnHeightMm": 125.0 }
  },
  "geometry": {
    "tentePliee":  { "flapHeightMm": 8.0 },
    "pionASocle":  { "tabWidthMm": 12.0, "tabHeightMm": 10.0 }
  },
  "layout": {
    "pageMarginMm": 10.0,
    "gutterMm": 3.0,
    "silhouetteMarginMm": 1.5,
    "calibrationZoneHeightMm": 14.0
  },
  "print": {
    "scaleCorrectionFactor": 1.0
  },
  "strokes": {
    "cutWidthMm": 0.25,
    "foldWidthMm": 0.25,
    "colorHex": "#B0B0B0",
    "foldDashPatternMm": [2.0, 2.0]
  },
  "paperFormats": {
    "A4":     { "widthMm": 210.0, "heightMm": 297.0 },
    "Letter": { "widthMm": 216.0, "heightMm": 279.0 }
  }
}
```

> ⚠️ **Toutes les valeurs ci-dessus sont provisoires**, à l'exception des dimensions de papier et des emprises de grille. Elles seront mesurées lors d'un tirage de contrôle.

**Piège à ne pas confondre** : `gridFootprintMm` est l'emprise du pion sur la grille de jeu ; `pawnHeightMm` est sa hauteur visuelle debout. Ce sont **deux dimensions indépendantes**. Ne jamais déduire l'une de l'autre.

## B.3 Manifeste d'entrée

```json
{
  "versionSchema": 1,
  "geometry": "PionASocle",
  "paperFormat": "A4",
  "culture": "fr-FR",
  "imagesDirectory": "./images",
  "items": [
    {
      "name": "gobelin-lancier",
      "size": "Moyenne",
      "quantity": 6,
      "rectoFile": "gobelin-lancier-recto.png",
      "versoFile": "gobelin-lancier-verso.png"
    },
    {
      "name": "ogre",
      "size": "Grande",
      "quantity": 1,
      "rectoFile": "ogre-recto.png",
      "versoFile": "ogre-verso.png"
    }
  ]
}
```

`geometry` vaut `TentePliee` ou `PionASocle`, et s'applique à tout le manifeste (DEC-001).

**Validation à l'ouverture, avec message d'erreur explicite** : fichiers image présents et lisibles, taille référencée existant dans la calibration, quantité ≥ 1, format de papier connu, `versionSchema` reconnu.

## B.4 Géométrie de l'unité dépliée

C'est le cœur de la tranche. À implémenter dans `Pawnsmith.Domain`, sans aucune dépendance.

### B.4.1 Structure verticale

Une **unité dépliée** est la découpe complète d'une figurine, avant pliage. De haut en bas sur la page :

| Bande | Hauteur | Contenu |
|---|---|---|
| Appendice verso | `appendixHeight` | Miroir de l'appendice recto |
| Image verso | `pawnHeightMm` | Image du verso, **tournée à 180°** |
| — **ligne de pliage** — | 0 | Frontière entre verso et recto |
| Image recto | `pawnHeightMm` | Image du recto, à l'endroit |
| Appendice recto | `appendixHeight` | Volets ou onglet selon la géométrie |

**Hauteur totale dépliée** = `2 × (pawnHeightMm + appendixHeight)`.

La ligne de pliage se situe donc exactement au **sommet de l'image recto**, c'est-à-dire au-dessus de la tête du personnage. Après pliage, le verso se rabat derrière le recto et se retrouve à l'endroit. **Omettre la rotation de 180° du verso produit un personnage tête en bas** : c'est l'erreur la plus probable de cette tranche, et elle doit être couverte par un test.

### B.4.2 Appendice selon la géométrie

**`TentePliee`** — `appendixHeight = flapHeightMm`. L'appendice occupe toute la largeur du pion. Deux lignes de pliage supplémentaires sont tracées, à la frontière entre chaque image et son appendice — c'est là que les volets se replient vers l'extérieur pour former la base.

**`PionASocle`** — `appendixHeight = tabHeightMm`. L'appendice est un onglet rectangulaire de largeur `tabWidthMm`, **centré horizontalement**. Pas de ligne de pliage supplémentaire : l'onglet est solidaire de la figurine et coulisse dans le socle.

### B.4.3 Contour de découpe

Le contour est un polygone fermé, symétrique par rapport à l'axe vertical de l'unité et par rapport à la ligne de pliage.

- Pour `TentePliee` : un simple rectangle de `pawnWidthMm` par la hauteur totale dépliée.
- Pour `PionASocle` : un rectangle de `pawnWidthMm` sur la hauteur des deux images, prolongé en haut et en bas par un onglet de `tabWidthMm` de large et `tabHeightMm` de haut, centré.

Le domaine expose ce polygone en coordonnées millimétriques relatives à l'unité. **Le rendu ne le recalcule pas** : il le reçoit et le trace.

### B.4.4 Placement des images

Pour chaque image, recto comme verso :

1. Boîte disponible : `(pawnWidthMm - 2 × silhouetteMarginMm)` de large, `(pawnHeightMm - silhouetteMarginMm)` de haut.
2. Mise à l'échelle **en conservant le rapport d'aspect** de l'image source.
3. Alignement **centré horizontalement, calé sur la ligne des pieds** — jamais centré verticalement.

La ligne des pieds est la frontière entre l'image et son appendice. La marge de silhouette s'applique donc sur les côtés et au sommet, pas sous les pieds.

## B.5 Mise en page de la planche

### B.5.1 Regroupement et pagination

1. Regrouper les éléments du manifeste **par taille** (DEC-005).
2. Pour chaque groupe, développer les quantités : un élément de quantité 6 produit 6 cellules identiques.
3. Remplir les pages du groupe, puis passer au groupe suivant.
4. Le PDF final concatène toutes les pages de tous les groupes, dans l'ordre des tailles tel qu'il apparaît dans le manifeste.

Une page ne contient **jamais** deux tailles différentes.

### B.5.2 Calcul de capacité

```
largeurUtile  = pageWidth  - 2 × pageMarginMm
hauteurUtile  = pageHeight - 2 × pageMarginMm - calibrationZoneHeightMm

largeurCellule = pawnWidthMm
hauteurCellule = 2 × (pawnHeightMm + appendixHeight)

colonnes = floor((largeurUtile + gutterMm) / (largeurCellule + gutterMm))
lignes   = floor((hauteurUtile + gutterMm) / (hauteurCellule + gutterMm))
capacite = colonnes × lignes
```

La grille est **centrée horizontalement** dans la largeur utile, et **calée en haut** de la hauteur utile.

Remplissage de gauche à droite, puis de haut en bas. Une quantité qui dépasse la fin d'une ligne continue naturellement sur la ligne suivante, puis sur la page suivante.

**Si `capacite` vaut zéro** — cellule plus grande que la zone utile — lever une erreur explicite nommant la taille en cause. Ne pas produire de page vide.

### B.5.3 Gouttières

`gutterMm` est l'espace **entre deux contours de découpe voisins**. Chaque unité conserve son propre contour ; les traits ne sont pas mutualisés. Une valeur de `0` produit des contours jointifs, ce qui est admis mais laisse moins de marge au ciseau.

### B.5.4 Repères d'impression

Tracés sur chaque page, non désactivables (DEC-017) :

| Repère | Spécification |
|---|---|
| **Trait de calibration** | Segment horizontal de **100,0 mm exactement**, centré dans la zone de calibration en bas de page, avec un repère vertical à chaque extrémité. Légendé par une chaîne localisée. |
| **Traits de coupe** | Le polygone de contour de chaque unité, en trait plein, épaisseur `cutWidthMm`, couleur `colorHex`. |
| **Lignes de pliage** | Traits discontinus selon `foldDashPatternMm`, épaisseur `foldWidthMm`, même couleur. Une ligne par pli : une pour toutes les géométries, trois pour `TentePliee`. |

### B.5.5 Facteur de correction d'échelle

`scaleCorrectionFactor` est appliqué **à l'ensemble du contenu de la page, trait de calibration compris**.

C'est volontaire et il faut le comprendre : si l'imprimante réduit de 2 %, on agrandit le PDF de 2 %, et le trait tracé à 102 mm dans le PDF ressort à 100 mm sur le papier. Le trait reste donc le juge de paix, quelle que soit la correction. Exclure le trait de la correction rendrait la mesure ininterprétable.

## B.6 Rendu PDF

- Implémentation de `ISheetRenderer` dans `Pawnsmith.Infrastructure`, via PDFsharp.
- **Le rendu ne décide de rien.** Il reçoit du domaine une structure de mise en page entièrement résolue — positions, dimensions, polygones, tous en millimètres — et se contente de tracer.
- **Toutes les conversions millimètres vers points sont centralisées dans une seule fonction.** Aucune conversion dispersée dans le code de dessin. C'est la source d'erreur la plus classique de ce genre de travail.
- Les PNG sont dessinés avec leur canal alpha préservé.
- Métadonnées PDF : titre, producteur, date de création.
- **Aucune mise à l'échelle automatique à l'impression** : les dimensions de page sont fixées explicitement en points.

### Localisation du PDF

`RenderAsync` reçoit une `CultureInfo` (DEC-023). Les chaînes portées sur la planche vivent dans des `.resx`, avec `fr` et `en` renseignés dès T1 :

- Légende de calibration : « 100 mm — si ce trait ne mesure pas 100 mm, l'impression n'est pas à l'échelle 100 % ».
- Étiquette de page indiquant la taille des pions et le numéro de page.

## B.7 Point d'entrée en ligne de commande

`tools/Pawnsmith.Cli` — **jetable, non livré, exclu de l'image Docker.** Sa seule raison d'être est de permettre un tirage papier avant l'existence de l'interface.

```
pawnsmith-cli --manifest ./manifeste.json --calibration ./config/calibration.json --out ./planche.pdf
```

Ne pas y mettre de logique. Il lit, appelle le cas d'usage, écrit le fichier, affiche les erreurs de validation lisiblement.

## B.8 Tests attendus

**Domaine — tests unitaires, sans mock, sans système de fichiers :**

1. Capacité calculée pour chaque combinaison taille × format de papier × géométrie.
2. Capacité nulle correctement détectée et signalée.
3. Développement des quantités : un élément de quantité 6 produit bien 6 cellules.
4. Pagination : un groupe dépassant la capacité produit le bon nombre de pages.
5. Regroupement : deux tailles dans un manifeste ne partagent jamais une page.
6. **Le verso est tourné à 180° et positionné au-dessus de la ligne de pliage.**
7. La ligne de pliage tombe exactement au sommet de l'image recto.
8. Hauteur totale dépliée conforme à la formule, pour les deux géométries.
9. Contour de découpe : rectangle pour `TentePliee`, polygone à onglet centré pour `PionASocle`.
10. Placement d'image : rapport d'aspect conservé, calage sur la ligne des pieds, marge de silhouette respectée.
11. Grille centrée horizontalement.
12. Facteur de correction appliqué au trait de calibration comme au reste.

**Infrastructure — tests d'intégration :**

13. Un manifeste minimal produit un PDF ouvrable et non vide.
14. Le nombre de pages du PDF correspond à la pagination calculée.
15. Manifeste invalide : erreur explicite, pas d'exception non gérée.
16. Image manquante sur le disque : erreur nommant le fichier absent.

## B.9 Critères d'acceptation

T1 est terminée quand, **planche imprimée en main** :

- [ ] Le trait de calibration mesure 100 mm ± 0,5 à la règle.
- [ ] Les pions découpés selon le contour tiennent dans leur socle (géométrie `PionASocle`).
- [ ] Les pions en tente tiennent debout seuls (géométrie `TentePliee`).
- [ ] Après pliage, le verso est à l'endroit.
- [ ] Aucune silhouette n'est rognée par le trait de coupe.
- [ ] Le PDF s'ouvre correctement et le nombre de pages est celui attendu.
- [ ] Tous les tests de B.8 passent.
- [ ] L'intégration continue est verte.
- [ ] Le squelette front compile, s'affiche, et bascule de langue.
- [ ] `docker run` démarre l'application et sert le front.
- [ ] Aucune valeur physique n'est codée en dur dans le code source.

---

## Annexe — Ce qui viendra après, et qu'il ne faut pas anticiper

T2 introduira le modèle de projet complet et la persistance ; T3 la composition de prompts ; T4 le client de génération ; T5 le détourage ; T6 l'API et l'interface ; T7 l'observabilité et le durcissement.

**Ne rien construire pour ces tranches.** Les ports définis au chapitre 7 de la bible existent comme intention de conception, pas comme code à écrire aujourd'hui. Le seul contrat à implémenter en T1 est `ISheetRenderer`.
