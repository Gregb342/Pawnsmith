# CLAUDE.md — méthode de travail

Ce fichier condense les règles contraignantes du projet à l'usage d'un assistant
de code. Il ne remplace pas les documents de référence, il y renvoie :

- [`docs/pawnsmith-bible.md`](docs/pawnsmith-bible.md) — **à lire en premier.**
  Vision, glossaire contraignant (chapitre 2), modèle de données, architecture,
  modèle de menace, et **journal des décisions (chapitre 11), qui fait foi**.
- [`docs/pawnsmith-cahier-des-charges-t1.md`](docs/pawnsmith-cahier-des-charges-t1.md)
  — fondations (partie A) et première tranche (partie B).
- [`docs/pawnsmith-protocole-t0.md`](docs/pawnsmith-protocole-t0.md) — protocole
  de calibration physique, scindé en T0a (test décisif, sans impression) et T0b
  (mesures papier, **après** le code de T1, avec le CLI de B.7). DEC-033.

**`docs/` EST la base de connaissance du projet**, et non un miroir d'une source
extérieure. Elle fait foi, et c'est ici qu'on la fait évoluer : conception et
code se font désormais dans le même dépôt.

Deux conséquences à ne pas séparer. Ces documents se **modifient** quand une
décision est prise — les laisser diverger du code est un défaut. Et la règle
du §1 s'y applique **intégralement** : proposer, montrer le diff, attendre la
validation. Ce serait le pire endroit où la relâcher, puisque c'est le document
qui arbitre tous les autres.

En pratique : une décision se consigne en **fiche au chapitre 11 de la bible**,
on n'édite jamais une fiche existante, on en ajoute une qui la supersède. Le
numéro de version du document et sa ligne de changelog se mettent à jour dans
le même commit.

En cas de contradiction entre ce fichier et l'un de ces documents, **ce sont
les documents qui gagnent**, et il faut le signaler.

---

## 1. Méthode de travail (chapitre 0 du cahier des charges)

Le porteur du projet **relit intégralement tout le code produit**, tranche par
tranche. Toute la méthode découle de cette contrainte.

### 🛑 Aucun commit sans relecture et validation préalables

**Règle absolue, qui prime sur tout le reste de ce fichier.** Le porteur relit et
valide le code **avant** qu'il soit committé. Pas après.

Le cycle est donc : écrire → **s'arrêter** → présenter le travail et expliquer
les choix non évidents → **attendre le feu vert** → committer ce qui a été
validé, et cela seulement.

Cela vaut aussi pour la tâche suivante : ne pas enchaîner sur du code qui
dépendrait de code non encore validé.

**Le but n'est pas d'obtenir un historique propre, c'est que le porteur
comprenne le projet au fur et à mesure qu'il se conçoit.** Un lot de commits
déjà faits transforme la relecture en audit après coup : il constate au lieu de
décider. C'est exactement le mécanisme de sécurité retenu en DEC-027.

Corollaire à ne pas mal lire : « je relis commit par commit » signifie « je relis
**avant** le commit ». Le découpage en petites tâches ci-dessous **sert** cette
relecture, il ne l'autorise pas à être sautée.

### Le reste de la méthode

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

Le critère n'est pas seulement juridique : **la chaîne de dépendances est une
surface de traitement de données personnelles.** Une dépendance de test
s'exécute sur le poste du développeur exactement comme une dépendance de
production s'exécute sur le serveur.

Règle générale : **toute nouvelle dépendance doit être justifiée** dans le
message du commit qui l'introduit, et ajoutée à
[`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) **dans le même commit**.
En cas de doute entre une dépendance et vingt lignes de code, écrire les vingt
lignes.

`THIRD-PARTY-NOTICES.md` est un **inventaire, pas une feuille de route** : il
liste uniquement ce que le dépôt référence effectivement, donc ce qui est
réellement distribué. Y inscrire un paquet « prévu » est un défaut — un
repreneur qui l'audite y trouverait des licences absentes de l'arbre de
dépendances et cesserait de faire confiance au fichier entier. Les intentions
d'outillage vivent en A.1. **PDFsharp et Serilog n'y entrent qu'au commit qui
les référence pour de bon.**

### Interdits explicites

| Paquet | Motif | Remplacement |
|---|---|---|
| **QuestPDF** | Licence commerciale « source-available », non approuvée OSI. Secteur public et sociétés cotées exclus quel que soit leur chiffre d'affaires. | **PDFsharp** (MIT), DEC-019 |
| **FluentAssertions ≥ 8** | Passé sous licence propriétaire Xceed en janvier 2025 ; usage commercial payant. La 7.x reste libre, mais épingler une version majeure pour raison de licence est une dette gratuite sur un projet neuf. | **Shouldly** |
| **AutoMapper** | Modèle commercial, et surtout mapping invisible en relecture — ce qui contredit frontalement DEC-021 et DEC-027. | Mapping manuel par méthodes d'extension `ToDto()` |
| **Moq** | A embarqué en août 2023, **dans une version mineure**, un composant extrayant l'adresse e-mail du développeur depuis sa configuration Git pour l'envoyer à un service tiers — sans consentement ni mention dans les notes de version. Retiré depuis, mais le paquet a démontré qu'il pouvait embarquer de la collecte par surprise, jusque sur le poste des contributeurs. | **NSubstitute** (BSD) |

> **Pourquoi Moq figure ici alors que le domaine se teste sans mock.** Le
> domaine, oui (DEC-020). Mais T4 (client HTTP ComfyUI), T5 (runtime ONNX) et
> T7 auront besoin de doubles de test, et Moq est le paquet que tout assistant
> de code proposera par réflexe. La règle doit être écrite **avant** qu'on en
> ait besoin, pas après.

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
Le code **lit** ces valeurs, il ne les connaît pas.

Deux statuts à ne pas mélanger : les `gridFootprintMm` sont des **faits
documentés** (1, 1, 2, 3 et 4 pouces — chapitre 14 de la bible), les
`pawnHeightMm` sont des **marqueurs provisoires** qui seront arbitrés en T0b.
Aucune des deux catégories ne doit être durcie dans le code, et le remplacement
des secondes ne doit demander aucune modification.

Trois pièges, tous rencontrés pour de vrai :

1. `gridFootprintMm` (emprise sur la grille de jeu) et `pawnHeightMm` (hauteur
   visuelle debout) sont **deux dimensions indépendantes**. Ne jamais déduire
   l'une de l'autre.
2. `Petite` et `Moyenne` ont volontairement la **même emprise** de 25,4 mm —
   dans les règles de jeu, Small et Medium occupent tous deux une case de
   5 pieds. Seule la hauteur les distingue. **Ne pas « corriger » cette
   redondance apparente** (DEC-031). Conséquence : elles ne peuvent jamais
   partager une page, leurs hauteurs de cellule différant.
3. Une hauteur de pion est **bornée par le papier** : `2 × (pawnHeightMm +
   appendice) ≤ hauteurUtile`, soit environ 112 mm si US Letter doit rester
   utilisable (§B.5.6, DEC-032). La calibration v1.1 portait 125 mm pour
   `Gigantesque`, ce qui donnait une capacité de page **nulle sur A4 comme sur
   Letter**. Toute nouvelle hauteur se vérifie contre ce plafond.

## 6. Vérifications avant commit

Dans cet ordre. La dernière ligne n'est pas une formalité : c'est elle qui
autorise le commit.

```bash
dotnet build Pawnsmith.sln
dotnet test Pawnsmith.sln
cd src/Pawnsmith.Web && npm run build
docker build -t pawnsmith .
```

Vérifier qu'aucun `bin/`, `obj/` ou `node_modules/` n'est suivi par git.

Puis **présenter le travail au porteur et attendre sa validation** (§1). Une
chaîne verte prouve que le code compile, pas qu'il est le bon.

## 7. État actuel

Les **fondations (partie A) sont closes**, A.1 à A.8, dernier critère compris :
l'intégration continue a tourné au vert sur `main`. Les projets .NET sont
**vides** — aucune logique métier nulle part, seulement le strict minimum pour
compiler, plus deux tests fumigènes qui donnent à la CI quelque chose à
exécuter. Ne pas les supprimer tant qu'aucun vrai test ne les remplace.

Documents de référence en vigueur : bible **v0.3**, cahier des charges **v1.2**,
protocole T0 **v1.1**.

**La partie B (tranche T1 — moteur de mise en page et rendu PDF) n'est pas
commencée, et ne démarre que sur instruction explicite du porteur.** Cela veut
dire : aucun type de domaine, aucune référence de projet ajoutée, aucun paquet
NuGet installé.

T0a (test décisif du générateur, sans code) reste à mener. T0b (mesures papier)
vient **après** le code de T1, dont elle utilise le CLI (DEC-033) : T1 s'écrit
donc avec les valeurs provisoires, et c'est normal.
