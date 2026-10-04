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
du §2 s'y applique **intégralement** : proposer, montrer le diff, attendre la
validation. Ce serait le pire endroit où la relâcher, puisque c'est le document
qui arbitre tous les autres.

En pratique : une décision se consigne en **fiche au chapitre 11 de la bible**,
on n'édite jamais une fiche existante, on en ajoute une qui la supersède. Le
numéro de version du document et sa ligne de changelog se mettent à jour dans
le même commit.

En cas de contradiction entre ce fichier et l'un de ces documents, **ce sont
les documents qui gagnent**, et il faut le signaler.

---

## 1. À qui tu parles, et comment

### L'interlocuteur

Grégoire, développeur .NET **junior** (deux ans, en reconversion). Il ne code
pas lui-même sur ce projet : **tu produis le code, il le relit intégralement**
(DEC-027). C'est donc lui qui doit comprendre, pas toi qui dois aller vite.

Deux conséquences directes :

- Tout ce que tu écris doit être lisible par quelqu'un qui découvre la
  bibliothèque, le pattern ou l'API **en même temps que le diff**. C'est la
  même exigence que « aucun code implicite ou magique » au §2, appliquée à la
  prose.
- Quand tu introduis une notion nouvelle — un paquet, un pattern, un terme, une
  méthode que tu es seul à connaître — tu l'expliques en une ou deux phrases,
  avec **le pourquoi de ce choix-là plutôt qu'un autre**. Pas de renvoi sec vers
  la documentation officielle.

Langage simple, une idée par phrase. Le vocabulaire technique est le bienvenu,
mais il est défini la première fois qu'il apparaît. Tu adaptes le vocabulaire,
**jamais la profondeur d'analyse**.

### Le format de réponse

Chaque réponse suit ces trois blocs, dans cet ordre.

**1. En bref** — cinq lignes maximum. Ce que tu as fait, où tu en es, ce qui
coince. C'est le seul bloc garanti d'être lu en entier.

**2. Détail** — tout le reste : choix techniques, compromis, explications, ce que
tu as vérifié. Aussi long qu'il le faut, mais **rien d'indispensable ici**.

**3. Ce que j'attends de toi** — une à trois puces concrètes : une question à
trancher, un diff à relire, une valeur à mesurer. Si tu n'attends rien, dis-le.

Règles de remplissage, non négociables :

- Un blocage, un doute, une erreur ou une question ouverte va **toujours dans
  « En bref »**, jamais dans le Détail. Le résumé sert à comprendre vite, pas à
  faire passer une mauvaise nouvelle en douceur.
- **Pas de mur de code dans la réponse.** Le diff se lit dans le diff. Tu cites
  au plus quelques lignes, celles qui portent le point que tu expliques.
- Pas de récapitulatif de ce que Grégoire vient de dire. Il le sait.

Exemple de forme attendue :

```
## En bref

Le calcul de capacité de page est écrit et testé (7 tests verts). J'ai buté
sur un point : la formule de B.5.2 ne dit pas si la gouttière compte après
la dernière colonne. J'ai supposé que non — à confirmer avant que je committe.

## Détail

La formule floor((largeurUtile + gutter) / (largeurCellule + gutter)) ajoute
une gouttière fictive au numérateur pour compenser celle qu'on ne trace pas à
droite de la dernière cellule. C'est un classique du calcul de grille, mais ce
n'est écrit nulle part dans la spec, donc je le signale plutôt que de le
supposer acquis.

## Ce que j'attends de toi

- Confirmer l'hypothèse sur la gouttière de fin de ligne.
- Relire le diff de PageCapacity.cs (42 lignes).
```

### Le ton

Sympathique, positif, motivant. Tu dis ce qui avance, tu ne noies pas une
réussite sous les réserves.

**Mais un ton positif n'est pas un accord automatique.** Quand un choix te
paraît faux, coûteux, ou qu'il casse quelque chose ailleurs, tu le dis
franchement et tu argumentes. Tu proposes une alternative si tu en as une. Tu ne
cèdes pas par politesse : tu cèdes quand l'argument d'en face est meilleur, et
tu dis pourquoi.

À faire : « Ça marche, mais X va poser problème, parce que Y. Alternative : Z. »

À éviter : « Excellente idée ! », « Tu as parfaitement raison », et toute
formule qui valide avant d'avoir réfléchi. Un accord automatique ne sert à rien
à quelqu'un qui apprend en relisant.

## 2. Méthode de travail (chapitre 0 du cahier des charges)

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

- **Proposer le découpage avant de coder.** À l'ouverture d'une tranche, tu
  présentes ta liste de tâches et tu attends la validation.
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
- **Le vocabulaire du chapitre 2 de la bible est contraignant en tant que
  concept**, pas en tant que graphie. Un terme désigne une seule chose, partout ;
  toute divergence de sens est un défaut. L'identifiant de code de chaque terme
  est donné par la table de **DEC-037** : `Blueprint`, `Candidate`, `Sheet`,
  `Size`, `Geometry`.
- **Trois langues, trois registres** (DEC-037). Le **code** — types, membres
  d'énumération, méthodes, journaux, et les clés de `calibration.json` comme du
  manifeste — est en **anglais**. L'**interface** est traduite par les
  catalogues `fr` et `en`, sans jamais de chaîne en dur. Les **prompts**,
  préenregistrés comme générés, sont en **anglais** : les modèles de diffusion
  sont entraînés sur des légendes anglaises, et un prompt français rend moins
  fidèlement.
- **Ne pas anticiper les tranches à venir.** Les ports du chapitre 7 de la bible
  sont une intention de conception, pas du code à écrire aujourd'hui.

## 3. Politique de dépendances (A.2)

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

## 4. Règle de dépendance entre projets (A.3)

```
Domain  ←  Application  ←  Infrastructure  ←  Api
```

`Domain` ne référence **rien** : ni projet, ni paquet NuGet. `Application`
référence `Domain`. `Infrastructure` référence `Application` et `Domain`. `Api`
référence tout. **Aucune flèche en sens inverse, jamais.**

`tools/Pawnsmith.Cli` est un harnais **jetable, non livré**, exclu de l'image
Docker (B.7). Ne rien y mettre qui ressemble à de la logique.

## 5. Conventions (A.8)

- **Conventional Commits** : `feat:`, `fix:`, `docs:`, `test:`, `chore:`,
  `refactor:`, plus `build:` et `ci:`.
- **Versionnement sémantique**, à partir de `0.1.0`.
- Messages de commit et **commentaires de code en anglais** ; **documentation
  fonctionnelle et échanges avec le porteur en français**.
- `var` autorisé **uniquement** quand le type est apparent à droite
  (`.editorconfig`, sévérité `warning`, donc erreur de compilation via
  `TreatWarningsAsErrors`).
- **Aucune chaîne en dur**, front comme back, dès le squelette (chapitre 10 de
  la bible). Front : catalogues JSON `react-i18next`. Back : `.resx`.
  L'API renvoie des **codes d'erreur**, jamais des messages traduits.

## 6. Valeurs physiques

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
2. `Small` et `Medium` ont volontairement la **même emprise** de 25,4 mm —
   dans les règles de jeu, Small et Medium occupent tous deux une case de
   5 pieds. Seule la hauteur les distingue. **Ne pas « corriger » cette
   redondance apparente** (DEC-031). Conséquence : elles ne peuvent jamais
   partager une page, leurs hauteurs de cellule différant.
3. Une hauteur de pion est **bornée par le papier** : `2 × (pawnHeightMm +
   appendice) ≤ hauteurUtile`, soit environ 112 mm si US Letter doit rester
   utilisable (§B.5.6, DEC-032). La calibration v1.1 portait 125 mm pour
   `Gargantuan`, ce qui donnait une capacité de page **nulle sur A4 comme sur
   Letter**. Toute nouvelle hauteur se vérifie contre ce plafond.

## 7. Vérifications avant commit

Dans cet ordre. La dernière ligne n'est pas une formalité : c'est elle qui
autorise le commit.

```bash
dotnet build Pawnsmith.sln
dotnet test Pawnsmith.sln
cd src/Pawnsmith.Web && npm run lint && npm run build
docker build -t pawnsmith .
```

Vérifier qu'aucun `bin/`, `obj/` ou `node_modules/` n'est suivi par git.

Puis **présenter le travail au porteur et attendre sa validation** (§2), au
format du §1. Une chaîne verte prouve que le code compile, pas qu'il est le bon.

## 8. État actuel

Les **fondations (partie A) sont closes**, A.1 à A.8, dernier critère compris :
l'intégration continue a tourné au vert sur `main`.

Documents de référence en vigueur : bible **v0.18**, cahier des charges T1
**v1.7**, cahier des charges T2 **v1.1**, cahier des charges T3 **v1.1**,
**cahier des charges T4 v1.1**, **cahier des charges T5 v1.1**, **cahier des
charges T6 v1.1** (l'API seule), **cahier des charges T7 v1.1**, protocole T0
**v1.4**.

### Décisions prises sans toi, à relire en premier

**T4, l'API de T6 et T7 ont été spécifiées et écrites sans ton arbitrage**,
dans le régime que tu as ouvert pour cette session : une branche par tranche,
un commit par tâche, et « quand un choix est ouvert, tranche et consigne ».
Chaque décision est une fiche DEC (DEC-074 à DEC-081 pour T4, DEC-082 à
DEC-089 pour T6, DEC-090 à DEC-097 pour T7) et revient dans le message du
commit où elle est née.

**Les trois tranches sont fusionnées dans `main`** le 3 octobre 2026, par trois
PR empilées (Gregb342/Pawnsmith#1, #2, #3), chacune avec la CI verte. Tu m'as
délégué ces fusions. Elles sont faites par **commits de fusion**, pas par
écrasement : chaque commit de tâche garde son identité, et la relecture commit
par commit reste possible. **Cette relecture intégrale n'est pas faite** — c'est
ce qui reste avant de clore T4, l'API de T6 et T7, comme pour T3. Les branches
de tranche n'ont pas été supprimées.

#### Pour T4 — branche `claude/charming-babbage-uuft5v`

La plus engageante d'abord.

1. **Le `Job` vit en mémoire et n'est jamais persisté** (DEC-074). Cinq états,
   trois terminaux, un échec arrête le lot. Rien n'est perdu par un
   redémarrage, parce que chaque candidat est sauvegardé dès qu'il existe.
2. **Chaque candidat est sauvegardé dès qu'il existe, en relisant le projet
   à chaque graine** (DEC-075). C'est ce qui tient « un lot interrompu
   conserve ses candidats », et ce qui évite d'écraser une modification faite
   pendant le lot. Ce n'est pas un verrou : l'API de T6 devra sérialiser les
   écritures d'un même projet.
3. **`IPawnPairProducer` n'est pas écrit** (DEC-078). T0a a retiré le risque
   qu'il couvrait ; `IImageGenerator` est le seul port. Supersède le
   chapitre 7 et le §4.2 de la bible sur ce point.
4. **La découpe est décidée en T4, exécutée en T5** (DEC-079). Face à gauche,
   colonne du milieu perdue sur une largeur impaire. Les pixels ne sont pas
   découpés : le schéma n'a pas de place pour des moitiés non détourées, et
   décoder un PNG est un choix de bibliothèque qui t'appartient avec T5.
5. **`{{WIDTH}}` et `{{HEIGHT}}` ne sont plus des jetons** (DEC-076). Trois
   jetons seulement, et un jeton est une valeur entière : un
   `"{{POSITIVE}}, masterpiece"` est refusé au chargement, parce qu'il ferait
   partir autre chose que le prompt que le candidat fige (DEC-049).
6. **La clause négative n'est pas figée sur le candidat** (DEC-077). Elle
   n'est pas une des trois clauses, et à CFG 1,0 le modèle l'ignore.
7. **MEN-003 se traite en interdisant à l'API de modifier l'adresse du
   générateur**, pas par une liste blanche de ports (DEC-081). Ni redirection
   suivie, ni proxy.
8. **Les bornes** (DEC-080) : 20 candidats par lot, 10 min par génération,
   30 s par appel, 64 Mio et 8192 px par image, 16 Mio par réponse JSON.
9. **Le délai de génération retire aussi la tâche chez ComfyUI**, pas
   seulement l'annulation (tâche 6) : une tâche abandonnée n'a pas à occuper
   la carte graphique.
10. **L'exemple de workflow finit sa clause de cadrage par `Subject:`** (tâche
    3), pour que le sujet arrive étiqueté comme dans T0a. L'ordre reste
    différent de T0a : le sujet vient désormais après **tout** le cadrage.
    **À vérifier au premier lot réel**, c'est le risque le plus concret de la
    tranche (§E.5.5).
11. **`ICodedException`**, une interface d'Application à une propriété, est
    implémentée par toutes les exceptions codées du dépôt. Sans elle, un
    `PROJECT_INVALID` levé pendant un lot finirait en `JOB_UNEXPECTED_ERROR`.
12. **Les graines sont tirées par `Random.Shared`**, pas par une source
    cryptographique : une graine est écrite en clair dans `project.json`.

#### Pour l'API de T6 — branche `claude/t6-api`, partie de T4

1. **Question C fermée : l'export d'un élu désaligné passe outre, et le
   dit** (DEC-082). Le PDF sort ; le rapport de planche liste l'élu et les
   clauses qui ont bougé, et dit quand le désalignement est **inconnu** (pas
   de workflow configuré). Ni blocage, ni confirmation.
2. **Une erreur d'API rend `{ "code" }` et rien d'autre** (DEC-084). Le
   message n'est jamais renvoyé : les messages du code contiennent des chemins
   absolus. Contrepartie : `PROJECT_INVALID` ne dit plus quel champ ; si le
   front en a besoin, ce sera un champ structuré.
3. **Un projet s'adresse par son nom de dossier canonique** (DEC-083), jamais
   par son `projectId`, qui n'est pas unique par décision (DEC-047).
4. **Un seul lot tourne à la fois**, validé avant la file, registre en
   mémoire borné à cent jobs terminés (DEC-085). `CandidateGeneration` est
   scindé en `QueueAsync` / `RunAsync` ; les clauses se figent au démarrage
   du lot, pas à sa mise en file.
5. **Une porte d'écriture par projet** (DEC-086), dans l'Application, que
   franchissent toutes les routes qui écrivent et chaque candidat d'un lot.
6. **La configuration passe par `appsettings.json` et l'environnement**
   (DEC-087) ; un générateur mal configuré n'empêche pas de démarrer, une
   calibration illisible si.
7. **MEN-010 entre au chapitre 9** (DEC-089) : contrôle d'`Origin` sur toute
   requête qui écrit, `AllowedHosts` restreint aux noms locaux — **posé par
   le code** quand le réglage manque, pas seulement par `appsettings.json`.
8. **Une image n'est servie que si un candidat la référence** (DEC-088).
9. **Deux réponses n'ont pas la forme `{ code }`** : le `400` d'un `Host`
   refusé et le `413` d'un corps trop gros, tous deux produits par le serveur
   lui-même. Assumé et écrit au §G.3.
10. **Tout corps de requête est borné à 1 Mio**, l'import seul montant à
    1 Gio — sans quoi une liste de cent millions de graines tenait en mémoire
    avant d'être refusée (MEN-007).
11. **Version `0.7.0`, pas `0.6.0`** : DEC-058 attache un mineur à une
    tranche, et `0.6.0` reste celui de T5, qui n'est pas écrite.
12. **Les tests de l'API démarrent l'hôte réel** sur `127.0.0.1` et un port
    libre, sans `Microsoft.AspNetCore.Mvc.Testing` : un paquet de moins.
13. **Une route `/api` inconnue répond `ROUTE_NOT_FOUND`**, jamais la page
    du front avec un `200`.

#### Pour T7 — branche `claude/t7-observability`, partie de l'API de T6

1. **La revue du chapitre 9 a trouvé un trou réel, et je l'ai bouché dans du
   code de T1** (DEC-095). Le rendu de la planche fait **décoder** chaque
   élu par PDFsharp ; un PNG importé de quelques octets peut annoncer
   60 000 × 60 000 pixels, soit quatorze gigaoctets, à une requête HTTP de
   distance depuis T6. `FileImageSizeReader` refuse désormais un côté de plus
   de **8 192 pixels**, sur l'en-tête, avant tout décodage — la borne même du
   générateur de T4. Son commentaire renvoyait les plafonds à T5 ; la planche
   décode aujourd'hui.
2. **Seuls les bords journalisent** (DEC-090) : démarrage, intergiciel
   d'erreurs, travailleur des lots. Le domaine et l'Application n'ont reçu
   aucune dépendance. L'identifiant de job est poussé par
   `LogContext.PushProperty` **au point d'appel** du cas d'usage, dans le
   travailleur : l'Application ne voit pas Serilog.
3. **Trois paquets, tous Apache-2.0**, licences lues dans le `LICENSE` de
   chaque dépôt et dans le `.nuspec` publié : `Serilog`,
   `Serilog.Sinks.File`, `Serilog.Extensions.Logging`. Écartés :
   `Serilog.Formatting.Compact` (le cœur a un formateur JSON) et
   `Serilog.AspNetCore` (console, configuration par réflexion, débogage).
4. **Une ligne JSON par événement, fichiers `.ndjson`**, un par jour et un de
   plus à chaque 50 Mio, **31 fichiers gardés** — environ 1,5 Gio au plus
   (DEC-091). Sans passage de taille, le puits **cesse d'écrire** à la limite.
5. **Désactiver la journalisation coupe les fichiers, pas la console**
   d'ASP.NET, qui garde son réglage standard (DEC-091).
6. **Aucun prompt au journal** (DEC-092), ce qui **précise le chapitre 8**,
   qui les citait. Le candidat fige déjà ses clauses ; une copie au journal
   serait du texte d'utilisateur de plus, plus dur à effacer. Exception
   connue, voir plus bas.
7. **En conteneur, l'avertissement de MEN-004 sort à chaque démarrage**
   (DEC-093), et le dit : un conteneur écoute forcément partout et ne voit
   pas comment son port est publié. C'est écrit comme risque accepté.
8. **Le visualiseur rend des lignes brutes**, en chaînes, au plus 4 Mio lus
   depuis la fin, 500 lignes par défaut et 5 000 au plus ; tout nom hors liste
   blanche rend `LOG_NOT_FOUND`, sans dire s'il existe (DEC-094). **L'API lit
   l'infrastructure directement**, sans port : il n'y a aucune règle à porter.
9. **MEN-011, falsification de journal, entre au chapitre 9** (DEC-096) —
   tenue par le format JSON, éprouvée par un test.
10. **La revue couvre MEN-001 à MEN-011**, pas seulement les sept que le
    chapitre 12 demandait ; chaque ligne nomme son test ou son risque
    accepté (§H.7.1, DEC-097).
11. **Deux défauts de T4 et T6 corrigés en T7**, parce que la journalisation
    les rendait réels : un générateur dont le workflow était refusé gardait
    son adresse **sans l'avoir vérifiée**, donc avec ses identifiants, et
    l'API la montrait déjà ; et `GeneratorAddress` répétait le texte brut
    dans la plupart de ses refus — `user:secret@hôte` sans schéma, ou un
    jeton en requête, finissaient dans le message, que le démarrage écrit
    désormais au journal.
12. **Un démarrage impossible écrit une ligne `Fatal`** : le journal est
    construit avant toute lecture de fichier.
13. **Version `0.8.0`**, comme DEC-058 l'annonçait pour T7.

#### Pour T5 — branche `claude/t5-cutout`, partie de `main`

**T5 est fusionnée dans `main`** le 4 octobre 2026, par
Gregb342/Pawnsmith#5, en commit de fusion, CI verte. Tu m'as délégué cette
fusion. **La relecture intégrale reste à faire**, comme pour T4, l'API de T6
et T7. La branche part de `main` après la fusion des trois PR (`eb18ef4`).
Les deux choix que tu t'étais
réservés, **tu les as tranchés** le 4 octobre : détourage **sans modèle**
(DEC-098) et PNG **écrits à la main** (DEC-099). Le reste est tranché sans
toi, DEC-100 à DEC-104.

1. **Le détourage n'a jamais vu une vraie image de ComfyUI.** Toutes les
   images de test sont des scènes fabriquées en code, à fond uni bruité et
   bande de sol. C'est le risque le plus concret de la tranche : **les
   valeurs de `CutoutOptions` sont arbitrées, pas mesurées** (DEC-100), et
   se règlent avec `cutout --pair` sur les planches de T0a.
2. **L'algorithme** (DEC-100) : couleur du fond = médiane des bords haut,
   gauche et droit (pas le bas, où sont les pieds) ; refus si moins de 60 %
   de ces bords lui ressemblent ; bande de sol retirée par le bas, 5 % de la
   hauteur au plus ; diffusion 4-connexe depuis les bords, tolérance 24 ;
   trous enclos retirés à tolérance moitié, 12, s'ils dépassent 0,05 % de la
   moitié ; bord adouci sur un pixel ; recadrage serré sur le sujet.
3. **Le lot détoure avant de sauvegarder, hors de la porte d'écriture**
   (DEC-101). Un détourage raté **n'arrête pas le lot** : le candidat est
   sauvé sans ses détourages, et l'échec est rangé sur
   `Job.CutoutFailures`, montré par l'API, le CLI et le journal. Seul un
   `CutoutException` est rattrapé ; toute autre exception arrête le lot
   comme avant.
4. **Le port reçoit l'image jumelée entière et rend deux PNG** (DEC-102) :
   la découpe de DEC-079 et le détourage sont faits ensemble, par
   l'adaptateur. Supersède la signature du chapitre 7.
5. **Cinq codes, tous en `422`** (DEC-103) : `CUTOUT_IMAGE_INVALID`,
   `CUTOUT_IMAGE_TOO_LARGE`, `CUTOUT_BACKGROUND_NOT_UNIFORM`,
   `CUTOUT_SUBJECT_NOT_FOUND`, `CANDIDATE_NO_PAIRED_IMAGE`. La moitié
   détourage de la question G est fermée.
6. **Version `0.9.0`, pas `0.6.0`** (DEC-104) : le dépôt était en `0.8.0`, et
   un numéro de version ne recule pas. `0.6.0` n'existera jamais.
7. **Le détourage à la demande est synchrone, dans la requête** :
   `POST …/candidates/{id}/cutout`. Il remplace un détourage existant et ne
   touche ni le statut ni l'élection.
8. **Les détourages sont réencodés depuis les pixels** : ils ne portent que
   `IHDR`, `IDAT`, `IEND`. Le graphe et le prompt que ComfyUI inscrit dans
   l'image jumelée ne passent pas (MEN-006).
9. **Le décodeur lit un sous-ensemble** : 8 bits, RGB ou RGBA, non entrelacé.
   C'est ce que ComfyUI écrit ; tout le reste est refusé avec
   `CUTOUT_IMAGE_INVALID`, jamais deviné. Côté refusé sur les 24 premiers
   octets, CRC vérifié par bloc, taille décompressée exacte.
10. **L'image jumelée est bornée à 64 Mio**, la borne même du générateur —
    né de ma relecture, voir plus bas.
11. **Le paramètre `IBackgroundRemover` de `CandidateGeneration` est
    obligatoire**, pas optionnel : un lot sans détourage n'a plus de sens
    depuis T5, et un défaut silencieux l'aurait caché.
12. **Deux commandes de CLI** : `candidate cutout` sur un candidat de projet,
    et `cutout --pair` sur un fichier quelconque, hors de tout projet.

**La tranche T1 (moteur de mise en page et rendu PDF) est écrite**, ses onze
tâches committées, plus quatre décisions nées de son usage sur de vraies
illustrations.

| # | Tâche | État |
|---|---|---|
| 1 | Vocabulaire du domaine et valeurs de calibration | ✅ |
| 2 | Unité dépliée : bandes, hauteur totale, ligne de pliage, appendice | ✅ |
| 3 | Contour de découpe | ✅ |
| 4 | Placement des images, verso à 180° | ✅ |
| 5 | Capacité et grille | ✅ |
| 6 | Regroupement, quantités, pagination | ✅ |
| 7 | Modèle de planche résolu, repères, facteur d'échelle | ✅ |
| 8 | Port `ISheetRenderer` et cas d'usage | ✅ |
| 9 | Lecture du manifeste et de la calibration | ✅ |
| 10 | Rendu PDFsharp et `.resx` | ✅ |
| 11 | CLI de B.7 | ✅ |

| Décision | Objet | Implémentée ? |
|---|---|---|
| DEC-039 | Géométrie `NoSupport`, rien sous les pieds | ✅ |
| DEC-040 | Cotes d'onglet réglables par l'utilisateur | ✅ — écrit en T2, tâche 3 (`EffectiveCalibration`) |
| DEC-041 | Le couple recto/verso partage une échelle | ✅ |
| DEC-042 | La clause de cadrage impose la pose | Partiellement — le **signalement** est fait ; la contrainte de pose voyage avec chaque objet du **catalogue** (DEC-064) ; la clause de cadrage vit dans le workflow (T4, DEC-076), mais le dépôt n'en livre qu'un **exemple** |

T1 a livré **154 tests**, les 19 du §B.8 couverts. Une seule dépendance de
production : PDFsharp 6.2.4 (MIT). La police embarquée est DejaVu Sans, sous
licence libre autorisant l'incorporation dans un document — point important,
puisqu'une police utilisée dans un PDF y est redistribuée.

### T2 — code terminé, 15 tâches sur 15

**La tranche T2 (modèle de projet et persistance) est spécifiée** par
[`docs/pawnsmith-cahier-des-charges-t2.md`](docs/pawnsmith-cahier-des-charges-t2.md)
v1.1 : schéma de `project.json`, chargement, sauvegarde, export et import
d'archives, **54 tests** attendus.

Le découpage a été proposé en 11 tâches et validé ; trois d'entre elles se sont
révélées trop grosses pour tenir en une relecture et ont été coupées en deux
(7a/7b, 9a/9b et 10a/10b), et une quinzième s'est ajoutée — le port
`IProjectRepository`, que le §C.1 met dans le périmètre et qu'aucune tâche du
découpage ne portait. **Les quinze sont écrites.**

| # | Tâche | État |
|---|---|---|
| 1 | Types de domaine, et `<Version>0.3.0</Version>` | ✅ |
| 2 | Assemblage du prompt et désalignement | ✅ |
| 3 | Calibration effective (fusion des surcharges) | ✅ |
| 4 | Nom de dossier sûr (MEN-009) | ✅ |
| 5 | Types de document et mapping manuel | ✅ |
| 6 | Écriture de `project.json` (règles de forme de C.3.3) | ✅ |
| 7a | Codes d'erreur, record d'options, validation intrinsèque | ✅ |
| 7b | Lecteur : les neuf étapes de C.7.1 et les diagnostics | ✅ |
| 8 | Sauvegarde (C.7.3) | ✅ |
| 9a | Profils d'archive, `archive.json`, filtrage `Share` | ✅ |
| 9b | Export : liste blanche, liens symboliques, forme du ZIP | ✅ |
| 10a | Inspection de l'archive, sans rien extraire (C.9.1, étapes 1 à 6) | ✅ |
| 10b | Extraction, échange atomique, destination (C.9.1, étapes 7-8 ; C.9.2) | ✅ |
| 11 | CLI de C.17, et les corrections de documentation groupées | ✅ |
| 12 | `IProjectRepository` assemblé, et DEC-062 | ✅ |

**Les 54 tests de C.12 sont couverts.** Le dépôt porte **418 tests verts** au
total, et l'intégration continue est verte sur `main`.

**Des seize critères d'acceptation de C.13, quinze sont tenus. Le seizième est
la relecture intégrale, et c'est la seule chose qui reste avant de clore T2.**
Les fiches DEC de C.15 sont déposées — elles l'ont été au fil des tâches, dans
le commit du code qu'elles décidaient, et non en fin de tranche : bible v0.10
pour DEC-046 à DEC-059, v0.11 et v0.12 pour DEC-060 et DEC-061, v0.13 pour
DEC-062. MEN-008 et MEN-009 sont au chapitre 9.

Le code de T2 vit dans `src/Pawnsmith.Infrastructure/Projects/`, sauf la fusion
des surcharges (`src/Pawnsmith.Application/PhysicalValues/`) et les types de
domaine (`src/Pawnsmith.Domain/Projects/` et `Prompts/`).

#### Ce que l'import a donné, et les deux limites connues

L'import est coupé à la frontière que MEN-001 trace lui-même — « tout est validé
avant qu'un octet ne soit écrit ». `ArchiveInspector` est le côté validé : il
**ne reçoit aucune destination et ne possède aucun chemin d'écriture**, donc
« rien n'est écrit » n'est pas une règle à tenir, c'est une propriété du type.
`ProjectImporter` est le côté qui écrit : dossier temporaire voisin de la
destination, puis un seul `Directory.Move`.

`ZipCentralDirectory` lit **un champ du format ZIP à la main**, et c'est le seul
endroit du dépôt qui fait ça. Motif mesuré, pas supposé : `System.IO.Compression`
n'expose pas le bit « entrée chiffrée » **et ne le lit jamais lui-même**. Une
archive chiffrée n'est donc pas refusée par le cadre, elle est extraite en
chiffré, en silence. Ne rien laisser grossir dans ce fichier.

Trois choses non couvertes, à connaître plutôt qu'à redécouvrir :

- **La branche Zip64** de la lecture du répertoire central. L'éprouver demande
  une archive de plus de 4 Gio ou de 65 535 entrées.
- **Les deux contrôles doublés que DEC-051 impose à l'extraction** — le nom
  d'entrée revu, le chemin résolu revérifié. Ils sont inatteignables tant que
  l'inspection refuse les doublons ; on les garde parce que la fiche les demande
  et qu'ils coûtent deux comparaisons.
- **L'atomicité du `Directory.Move`** elle-même, comme l'échange atomique de la
  tâche 8. Le test montre que c'est un déplacement et non une copie, pas qu'il
  est indivisible.

Un trou assumé, mesuré lui aussi : une entrée qui **sous-déclare** sa taille
arrive **tronquée en silence**, le cadre ne vérifiant pas le CRC. Ça ne casse
aucun invariant de C.8 et une image abîmée échoue bruyamment en T1 et en T5.
Fermer ça demanderait un contrôle CRC32 par entrée, que C.9 ne demande pas.

#### Le piège C# qui a coûté deux tests

**Un `record` compare un membre de type interface par référence.** `Project`
porte ses gabarits dans un `IReadOnlyList`, donc deux projets au contenu
rigoureusement identique dans deux listes de types concrets différents sont
« différents » pour `==`. Les tests d'import comparent les **documents
sérialisés**, ce qui est plus fort : ça couvre tout le modèle d'un coup, ordre
des collections compris, et le test 19 garantit déjà que l'écriture est
déterministe.

#### Ce que le CLI a demandé en plus

`project new` ne pouvait pas exister sans deux briques qui manquaient, et qui
n'avaient pas leur place dans un harnais sans logique :

- **`NewProject.Create`** (domaine) — ce qu'est un projet vide est un fait sur
  le modèle, pas sur qui le crée. L'instant est un paramètre, le domaine ne lit
  pas d'horloge.
- **`ProjectCreator`** (infrastructure) — la moitié de C.3.2 que
  `ProjectFolderName` laissait dehors : suffixe de collision `-2`, `-3`, et
  vérification que le chemin résolu est bien sous la racine. Cinq tests le
  couvrent, bien que C.12 n'en numérote aucun : c'est de la vraie logique.

**Une collision se suffixe à la création et se refuse à l'import**, et l'écart
est voulu. Créer un second « Donjon » est ordinaire. Importer sur un dossier
existant ne l'est pas : quelque chose est déjà là.

#### Le port, arrivé en dernier

`IProjectRepository` est dans le périmètre au §C.1, et **aucune des onze tâches
du découpage ne le portait** — l'oubli s'est vu en repassant sur les critères
d'acceptation. Il est assemblé maintenant, en façade des cinq types qui
existaient déjà : `ProjectCreator`, `ProjectReader`, `ProjectSaver`,
`ProjectExporter`, `ProjectImporter`. **Il ne décide de rien.**

Cinq types plutôt qu'une classe, et c'est délibéré : le lecteur est les neuf
étapes de C.7.1, l'import les huit de C.9.1, et les réunir aurait donné une
classe de mille lignes que personne ne relit d'une traite — le défaut exact que
DEC-027 existe pour empêcher.

Deux traversées de frontière valent d'être connues, parce que la règle de
dépendance interdit à `Application` de connaître `Infrastructure` : le profil
d'archive est **une seconde énumération** côté Application, avec un mapping
manuel de quatre lignes ; et le type de diagnostic traverse **en tant que nom**,
pas en tant qu'énumération miroir. Le second est un compromis assumé — on perd
l'aide du compilateur au loin — et il tient tant que les seuls consommateurs
sont un CLI et un test. T6 voudra une représentation plus riche de toute façon.

**DEC-062** consigne au passage la décision prise oralement en tâche 8 : dépôt
sans état, chaque opération reçoit son chemin. Elle **supersède les signatures
du chapitre 7 de la bible**, qui supposaient qu'un projet sache où il habite —
ce que DEC-047 interdit.

#### Les décisions de T2

Seize fiches sont nées de cette spécification, de sa revue et de son écriture :
**DEC-046 à DEC-061**, plus **MEN-008** et **MEN-009** au chapitre 9. Les huit
qui changent quelque chose à ce qui était écrit avant :

| Décision | Ce qu'elle change |
|---|---|
| DEC-046 | Le fichier projet s'appelle `project.json`, pas `projet.json` |
| DEC-049 | Le candidat fige ses **trois clauses**, pas le prompt assemblé. Supersède la forme du champ `promptUtilise` du §3.1 |
| DEC-053 | `LoadAsync` prend la calibration en paramètre ; les surcharges de projet sont une **liste close** de deux membres, résolues en Application |
| DEC-055 | Aucun champ de projet n'est verrouillé après création ; `SaveAsync` ne reçoit **jamais** l'état antérieur |
| DEC-056 | Une donnée de projet n'est jamais rejetée par une donnée de machine. **Supersède la double validation de DEC-053** |
| DEC-057 | Les bornes de ressources sont un record d'options, pas un fichier de configuration ; le fichier arrive en T6 |
| DEC-058 | `<Version>` vit dans `Directory.Build.props` ; une tranche livrée vaut un mineur, donc **T2 est `0.3.0`** |
| DEC-060 / DEC-061 | Le filigrane est écarté et le sujet de l'attribution est clos. Rien à écrire, aucune tâche ouverte |

**DEC-059 a renommé l'invocation du CLI** en `pawnsmith-cli sheet …`. C'est fait,
en tâche 11, et les quatre documents qui décrivaient l'ancienne commande ont été
corrigés **dans le même commit que le code** : le §B.7 et le §A.3 du cahier T1
(v1.7), le protocole T0 (v1.4), ce fichier et le README.

#### Trois points ouverts, à connaître avant de reprendre

**Le test MEN-008 est dégradé sous Windows.** Y créer un lien symbolique demande
le privilège `SeCreateSymbolicLink`, absent du poste du porteur. Le test détecte
la capacité, et à défaut n'exerce que la seconde couche de la contre-mesure — la
liste blanche — en l'écrivant dans sa sortie. **C'est la CI Ubuntu qui certifie
cette menace.**

**Une mutation survit sur l'échange atomique.** Remplacer le fichier temporaire
et `File.Replace` par une écriture en place ne fait tomber aucun test : éprouver
l'atomicité demanderait d'interrompre le processus au milieu de l'échange, ce
qu'un test unitaire ne sait pas faire. Tout ce qui entoure l'échange est
couvert ; l'atomicité elle-même repose sur la lecture de dix lignes. Le porteur
l'a accepté en connaissance de cause.

**Le manifeste de T1 porte des clés en français.** `rectoFile` et `versoFile`,
dans le §B.3 comme dans `ManifestReader`. Le code est donc conforme à sa spec,
et c'est **la spec qui contredit DEC-037**, laquelle place les clés de fichier du
côté anglais. Découvert en éprouvant le CLI de la tâche 11, signalé plutôt que
corrigé en silence : renommer casserait le format du manifeste et les fichiers
de tirage de T0b. À trancher — probablement avec une montée de `versionSchema`,
et probablement pas avant T0b.

#### Ce qui a bougé hors de T2 pendant T2

- **Les sources sont rangées en dossiers thématiques** à l'intérieur de chaque
  projet, namespaces alignés sur les chemins (§A.3 du cahier T1, v1.7). Le
  domaine se range par sujet, l'infrastructure par technologie d'adaptateur.
  Un dossier ne peut pas porter le nom d'un type qu'il contient — c'est une
  erreur de compilation, d'où `PhysicalValues` plutôt que `Calibration`.
- **`tests/Pawnsmith.Application.Tests`** est né : la couche n'en avait pas.
- **ESLint** est branché sur le front et tourne dans la CI, avant la
  construction.
- **Un défaut de localisation a été corrigé** : les libellés du PDF retombaient
  en silence sur des valeurs anglaises si la ressource ne se résolvait pas.
  `SheetStrings` lève désormais, et quatre tests le gardent.

### T3 — code terminé, 11 tâches sur 11, fusionnée dans `main`

**La tranche T3 (composition de la clause sujet, catalogue, règles de gestion)
est spécifiée** par
[`docs/pawnsmith-cahier-des-charges-t3.md`](docs/pawnsmith-cahier-des-charges-t3.md)
v1.1, et **écrite en entier**, un commit par tâche, fusionnée dans `main` en
avance rapide et poussée. Le porteur a donné son feu vert à la spec et au
découpage avec la consigne « itère jusqu'à une version testable, je relis
après », puis « valide et pousse tout » — c'est la seule tranche écrite dans ce
régime, et **la relecture intégrale reste à faire, commit par commit**. Les
quatorze commits de T3 se relisent d'une traite avec
`git log --oneline 2ac311d..HEAD`.

Le découpage a été proposé en 11 tâches ; une paire a été fusionnée à
l'écriture (2+3, la règle de composition et son type de retour). Dix commits
de code, deux de documentation, **deux correctifs nés de ma propre relecture**
(voir plus bas).

| # | Tâche | État |
|---|---|---|
| 1 | Types `Catalog` et `PromptTemplate`, et `<Version>0.4.0</Version>` | ✅ |
| 2+3 | `SubjectClause.Compose` : règle, repli, diagnostics | ✅ |
| 4 | `IPromptComposer` réduit à `ComposeSubject`, `TemplatePromptComposer` | ✅ |
| 5 | `CatalogReader`, codes d'erreur, `config/catalog.fantasy.json` | ✅ |
| 6 | `PromptTemplateReader`, `config/prompt-template.fantasy.json` | ✅ |
| 7 | `BlueprintEditor` : recomposition déduite (DEC-067) | ✅ |
| 8 | `CandidateElection` : deux détourages exigés, statut intact | ✅ |
| 9 | `BlueprintRemoval` + `ProjectImageFiles` : tout part, fichiers compris | ✅ |
| 10 | `ProjectSheetRequestBuilder` : gabarit sans élu ignoré et nommé | ✅ |
| 11 | CLI `blueprint …` et `project sheet`, documentation | ✅ |

**Les 32 tests de D.11 sont couverts**, et quelques-uns de plus. Le dépôt porte
**518 tests verts**, en Debug comme en Release. La chaîne complète a été éprouvée au CLI sur un projet
jetable : composition avec valeur inconnue et diagnostic, recomposition après
changement de race, édition manuelle puis non-recomposition, élection, planche
rendue avec gabarit sans élu nommé, suppression emportant ses trois fichiers.

Le code de T3 vit dans `src/Pawnsmith.Domain/Prompts/` (types, composition),
`src/Pawnsmith.Domain/Sheets/ProjectSheetRequest.cs` (le pont vers T1),
`src/Pawnsmith.Application/Blueprints/` (les trois cas d'usage) et
`Prompts/` (le composeur), `src/Pawnsmith.Infrastructure/Prompts/` (les deux
lecteurs) et `Projects/ProjectImageFiles.cs`.

#### Deux défauts que j'ai trouvés en me relisant, corrigés

Ils sont de la même famille — du code à moi dont le test ne prouvait pas ce
qu'il prétendait — et ce sont les deux premiers commits à relire :

- **`ProjectImageFiles.Delete` validait chaque chemin dans la boucle de
  suppression.** Une liste `[légitime, échappant]` aurait donc supprimé le
  premier fichier avant de refuser le second, et une suppression ne se
  rejoue pas. Le test passait **uniquement parce qu'il rangeait le chemin
  fautif en premier** : un test qui flatte le code au lieu de l'éprouver.
  Les deux boucles sont désormais séparées — tout est approuvé, puis tout
  part — ce qui est la règle de MEN-001 appliquée à une suppression.
- **`TemplateToken.Substitute` enchaînait des `string.Replace`.** Une valeur
  substituée était donc re-balayée par le jeton suivant : un gabarit dont la
  race vaut littéralement `{characterClass}` sortait avec sa classe imprimée
  deux fois. Personne ne tape ça exprès, mais race et classe sont du texte
  libre d'utilisateur, et le comportement est invisible en relecture. La
  substitution se fait maintenant en **une seule passe**.

#### Les décisions prises en écrivant, à relire en premier

Le porteur a demandé de trancher plutôt que d'attendre. Voici ce qui a été
tranché sans lui, chaque point signalé dans le commit où il est né :

- **Le port rend un record, pas une chaîne.** `ComposeSubject` rend
  `ComposedSubject(Clause, Diagnostics)`. La v1.0 de la spec écrivait `string`,
  ce qui laissait les diagnostics de D.6.3 sans endroit où aller. Corrigé dans
  la spec avant le premier commit, et consigné dans DEC-066.
- **Une valeur optionnelle vide est « non contrainte »**, comme une clé
  absente : ni fragment, ni diagnostic. Un avertissement sur une chaîne vide
  serait du bruit que personne ne peut traiter.
- **Clés et valeurs du catalogue sont comparées en ordinal, casse comprise.**
  `Weapon` n'est pas `weapon`. C'est la règle du projet pour toute chaîne
  libre ; une comparaison tolérante ici ferait obéir la clause composée à une
  règle que le désalignement ne partage pas.
- **Le fichier catalogue est un tableau ordonné, pas un objet.** L'unicité des
  clés est vérifiée à la main dans `Catalog.Create` plutôt qu'obtenue du
  format, parce que la composition lit l'ordre du fichier en repli.
- **Les fichiers de données ont le régime de `calibration.json`** :
  commentaires et virgules finales tolérés, champ inconnu ignoré. Les fichiers
  livrés restent en JSON strict.
- **Deux codes d'erreur de plus qu'en v1.0** : `BLUEPRINT_NOT_FOUND` et
  `CANDIDATE_NOT_FOUND`, levés par l'Application. Une opération sur un
  identifiant inconnu devait refuser avec un code, pas avec une exception de
  programmation.
- **Le composeur v1 tient un seul univers**, pas un dictionnaire d'univers.
  Il vérifie que le template et le catalogue sont du même univers, et que le
  projet aussi. Un second univers décidera de la forme, le cas sous les yeux.
- **La validité des champs d'un gabarit** (race non vide, quantité ≥ 1) n'est
  pas revérifiée par l'éditeur : c'est le sauveur qui la porte, et il nomme le
  champ. L'éditeur ne vérifie que ce qui lui appartient — l'identifiant.
- **`Unelect` existe**, symétrique d'`Elect`, pour qu'aucun appelant n'ait à
  faire un `with` sur un gabarit pour défaire une élection. D.8 ne le
  demandait pas ; T6 en aura besoin.
- **Le nom d'un item de planche est « race classe »**, sans identifiant. Deux
  gabarits peuvent le partager, ce qui ne brouille qu'un message.
- **`project sheet` prend `--culture`, optionnelle, `en` par défaut.** Le
  manifeste de T1 portait la culture ; un projet ne la porte pas.
- **Le test du manifeste d'archive épinglait `0.3.0`** et a été passé à
  `0.4.0` dans le commit de la tâche 7, alors qu'il appartenait à la tâche 1.
  Défaut de découpage, signalé dans le message du commit.

#### Ce qui n'est pas couvert, à connaître plutôt qu'à redécouvrir

- **`docker build` n'a jamais tourné sur T3, et je ne peux pas le lancer.** Le
  service Windows `com.docker.service` est **arrêté**, et le démarrer demande
  une élévation que l'assistant n'a pas : `Start-Service` répond « Cannot open
  'com.docker.service' service ». Docker Desktop a été lancé, ses processus
  tournent, mais sans le service le pipe n'existe pas. **C'est à Grégoire de
  faire tourner `docker build -t pawnsmith .` une fois.**
  Ce qui a été fait à la place, et qui couvre l'essentiel : l'image construit
  avec `--configuration Release`, donc `dotnet publish src/Pawnsmith.Api -c
  Release` **et** toute la suite de tests **en Release** ont été passés au
  vert. Et le `Dockerfile` copie `config/` **en bloc** (`COPY config/
  ./config/`) et les quatre projets `src/` un par un — T3 n'ajoute aucun
  `.csproj`, donc rien n'y échappe. Le risque résiduel est celui d'un
  environnement d'image, pas celui de cette tranche.
- **Le chemin `UNIVERSE_MISMATCH` est inatteignable** tant que `Universe` n'a
  qu'un membre : déclarer un univers inconnu donne `*_INVALID`, pas un
  mismatch. Le code existe, le test le note, il s'exercera avec EVO-004.
- **Deux raisons de saut de `ProjectSheetRequestBuilder` sont inatteignables
  par le lecteur** — `ElectedCandidateNotFound` et `ElectedCandidateNotCutOut`
  — parce que la validation intrinsèque de T2 et `CandidateElection` les
  refusent en amont. Elles restent parce qu'un projet en mémoire peut les
  produire, et qu'un saut nommé vaut mieux qu'un fichier introuvable.
- **La spec a été suivie à la lettre sur un point discutable** : D.6.2 place
  les détails en dernier et les fragments inconnus avant, en ordinal. Un
  utilisateur qui coche cinq valeurs hors catalogue obtient cinq mots nus,
  triés alphabétiquement, au milieu de sa clause. C'est correct et ce n'est pas
  beau ; le catalogue livré est là pour que ça n'arrive pas souvent.

#### Ce que le CLI a demandé en plus

`--param` se répète, ce que l'analyseur d'arguments de T2 ne savait pas faire :
il ne gardait que la dernière valeur d'une option. Il garde désormais toutes les
valeurs, dans l'ordre ; `Required` et `Optional` lisent la dernière, `All` les
rend toutes. Aucune autre sous-commande n'y a vu de différence.

### T4 — code terminé, 10 tâches sur 10, fusionnée dans `main`

**La tranche T4 (client du générateur et production de couples) est
spécifiée** par
[`docs/pawnsmith-cahier-des-charges-t4.md`](docs/pawnsmith-cahier-des-charges-t4.md)
v1.1 **et écrite en entier**, sur la branche `claude/charming-babbage-uuft5v`,
partie de `main` (`d753728`). Un commit de documentation (spec et fiches),
puis un commit par tâche. Elle se relit d'une traite avec
`git log --oneline d753728..`.

| # | Tâche | État |
|---|---|---|
| 1 | `Job` et `JobState`, et `<Version>0.5.0</Version>` | ✅ |
| 2 | `PairSplit` (règle de découpe) | ✅ |
| 3 | `WorkflowTemplate`, son lecteur, l'exemple de workflow | ✅ |
| 4 | Port `IImageGenerator`, `ICodedException`, adresse, `CheckAsync`, faux serveur | ✅ |
| 5 | `GenerateAsync` : soumission, interrogation, rapatriement, bornes | ✅ |
| 6 | Annulation jusqu'au générateur | ✅ |
| 7 | `IProjectRepository.WritePairedImageAsync` | ✅ |
| 8 | Cas d'usage du lot `CandidateGeneration` | ✅ |
| 9 | Test de bout en bout | ✅ |
| 10 | CLI `generator check` et `candidate generate`, documentation | ✅ |

**Les 42 tests de E.12 sont couverts**, et une trentaine de plus. Le dépôt
porte **690 tests verts**. Aucune dépendance ajoutée : le faux serveur
ComfyUI est un serveur HTTP écrit à la main sur une socket, les images de test
sont des PNG fabriqués en code (zlib du framework, CRC écrit à la main), et
les faux d'Application sont deux classes lisibles. Aucun binaire commité.

Le code vit dans `src/Pawnsmith.Domain/Jobs/` et `Generation/`,
`src/Pawnsmith.Application/Generation/` et `Ports/IImageGenerator.cs`,
`src/Pawnsmith.Infrastructure/Generation/` et `Imaging/PngHeader.cs`.

**La chaîne a été éprouvée au CLI** contre un faux ComfyUI en Python (dans le
bac à sable de la session, pas dans le dépôt) : lot nominal, générateur
injoignable (`Failed`, code `GENERATOR_UNREACHABLE`, rien d'écrit), lot trop
grand (`BATCH_SIZE_INVALID`), et **Ctrl+C au milieu d'un lot de trois** — le
job finit `Cancelled` avec un candidat conservé, et le faux serveur a bien
reçu le retrait de file et l'interruption ciblée.

**`docker build` a enfin tourné**, dans le conteneur de cette session, sur
T3 puis sur T4 : l'image se construit et répond `200` sur `/`. Il a fallu
injecter le certificat du proxy réseau de la session dans une **copie
temporaire** du `Dockerfile` ; celui du dépôt n'a pas changé. La note de T3
ci-dessus (« `docker build` n'a jamais tourné ») est donc levée.

#### Ce que ma relecture de T4 a trouvé, et corrigé

- **Une réponse JSON de forme inattendue échappait au code d'erreur.**
  L'indexeur de `JsonNode` lève quand le nœud n'est pas un objet, ou quand
  l'objet répète une clé. Un générateur qui répondait `"status": "error"` en
  chaîne, ou `{ "x": "finished" }`, produisait donc une exception imprévue —
  que le lot aurait rangée en `JOB_UNEXPECTED_ERROR` — ou, pire, dix minutes
  d'attente avant un faux `GENERATOR_TIMEOUT`. Toute lecture d'une réponse
  passe désormais par une méthode `Child` qui rend `null` ou un refus codé ;
  neuf formes hostiles sont testées, et huit échouaient avant le correctif.
- **Trois messages formataient une durée selon la culture du processus**
  (`0,2 s` sous `fr-FR`). Ce n'était pas une comparaison, mais un message
  anglais se formate de façon invariante ; corrigé.
- **`config/workflow.comfyui.json` est ignoré par git** : c'est le workflow
  exporté de ta machine, qui nomme ses fichiers de modèles. Seul l'exemple
  appartient au dépôt.

#### Ce qui n'est pas couvert, à connaître plutôt qu'à redécouvrir

- **Une annulation pendant `POST /prompt`** ne peut pas retirer la tâche,
  dont elle ne connaît pas encore l'identifiant (§E.7.3). Fenêtre de quelques
  millisecondes.
- **Aucun ComfyUI réel n'a été appelé.** Le workflow livré est un exemple
  construit d'après DEC-043 ; les types de nœuds y sont plausibles, pas
  vérifiés. **Le vrai `config/workflow.comfyui.json` est à fabriquer chez
  toi**, avec *Export (API)*, en suivant `config/README.md`.
- **L'interruption ciblée** (`/interrupt` avec `prompt_id`) dépend de la
  version de ComfyUI ; les anciennes interrompent ce qui tourne, quoi que ce
  soit. Risque accepté et écrit (§E.7.3).
- **`FileImageSizeReader` (T1) et `PngHeader` (T4) lisent les mêmes 24
  octets**, chacun de son côté. Laissé tel quel pour ne pas toucher du code de
  T1 non encore validé sur papier ; dix lignes de doublon.
- **Les métadonnées PNG** : ComfyUI inscrit le graphe et le prompt dans
  chaque image. L'image jumelée les garde, sans conséquence puisque `Share` la
  retire. **T5 devra réencoder** les moitiés détourées (DEC-079).

### T6, première partie : l'API — code terminé, 10 tâches sur 10, fusionnée dans `main`

**L'API de T6 est spécifiée** par
[`docs/pawnsmith-cahier-des-charges-t6.md`](docs/pawnsmith-cahier-des-charges-t6.md)
v1.1 **et écrite**, sur la branche `claude/t6-api`, partie de la branche de
T4. **Le front n'est pas commencé** : c'est la seconde partie de T6, hors de
ce que tu as demandé.

| # | Tâche | État |
|---|---|---|
| 1 | `CandidateJudgement`, `ProjectWriteGate`, `CandidateGeneration` scindé, `0.7.0` | ✅ |
| 2 | Dépôt : liste, suppression d'images, ouverture d'une image | ✅ |
| 3 | `ProjectSheet` : rapport, PDF, question C | ✅ |
| 4 | Hôte, erreurs, garde d'origine et d'hôte, harnais de tests | ✅ |
| 5 | Projets | ✅ |
| 6 | Gabarits, candidats, images | ✅ |
| 7 | File des lots | ✅ |
| 8 | Planche | ✅ |
| 9 | Archives | ✅ |
| 10 | Documentation | ✅ |

**Les 29 tests de G.13 sont couverts**, et une centaine de plus. Le dépôt
porte **860 tests verts**. Aucune dépendance ajoutée. `docker build` passe,
et l'image démarrée répond : projet créé, listé, front servi, `Host` étranger
refusé.

Le code vit dans `src/Pawnsmith.Api/` (`Hosting/`, `Errors/`, `Contracts/`,
`Endpoints/`, `Jobs/`), plus `src/Pawnsmith.Application/Projects/`,
`Sheets/ProjectSheet.cs` et `Blueprints/CandidateJudgement.cs`. Les tests
sont dans `tests/Pawnsmith.Api.Tests/`, nouveau projet.

#### Ce que ma relecture de T6 a trouvé, et corrigé

- **Le travailleur des lots pouvait mourir.** Un job annulé en file, puis
  oublié par le registre (borné à cent jobs terminés) avant que le
  travailleur n'atteigne son identifiant, faisait lever `JOB_NOT_FOUND` dans
  la boucle : plus aucun lot n'aurait tourné jusqu'au redémarrage. Corrigé,
  et testé au niveau du registre.
- **Le filtrage d'hôtes ne tenait qu'à `appsettings.json`.** Trouvé par un
  test : avec une racine de contenu ailleurs, le fichier n'était pas lu et
  n'importe quel `Host` passait. La valeur restrictive est désormais posée
  par le code.

#### Ce qui n'est pas couvert, à connaître

- **Aucun point de terminaison ne supprime un projet** : aucun cas d'usage
  ne le fait. À écrire avec le front, s'il le demande.
- **Le doublon de `projectId` à l'import n'est pas arbitré** : l'API
  l'expose dans la liste, le front posera la question (T2 l'avait renvoyé à
  T6, et c'est sa partie front).
- **Le template de T3 écrit « a orc »** : sa tête est `a {race}
  {characterClass}`. Contenu de `config/`, à reprendre avec le catalogue au
  premier lot réel.

### T7 — code terminé, 5 tâches sur 5, fusionnée dans `main`

**T7 est spécifiée** par
[`docs/pawnsmith-cahier-des-charges-t7.md`](docs/pawnsmith-cahier-des-charges-t7.md)
v1.1 **et écrite**, sur la branche `claude/t7-observability`, partie de
`claude/t6-api`. Le visualiseur est écrit **côté API** ; son écran appartient
au front de T6.

| # | Tâche | État |
|---|---|---|
| 1 | Journalisation Serilog, trois paquets, `0.8.0` | ✅ |
| 2 | Démarrage, erreurs de requête, lots et `JobId`, MEN-004 | ✅ |
| 3 | Visualiseur : `LogDirectory`, deux routes, `LOG_NOT_FOUND` | ✅ |
| 4 | MEN-005 sur la planche | ✅ |
| 5 | Documentation, chapitre 9 tenu | ✅ |

**Les 20 tests de H.8 sont couverts**, et une vingtaine de plus. Le dépôt
porte **928 tests verts**, en Debug comme en Release. Chaque test de
sécurité neuf a été éprouvé par mutation : retirer la rétention, la poussée
du `JobId`, l'exclusion des liens, `[0-9]` au profit de `\d`, la borne
d'image, la vérification de l'adresse ou le filtrage des niveaux fait tomber
au moins un test. `docker build` passe, et le conteneur démarré écrit ses
journaux dans `/app/data/logs`, avertit selon MEN-004 et les sert par l'API.

Le code vit dans `src/Pawnsmith.Infrastructure/Logging/`,
`src/Pawnsmith.Api/Hosting/` (`StartupReport`, `ListeningAddresses`),
`Endpoints/LogEndpoints.cs`, et `Imaging/FileImageSizeReader.cs` pour la
borne.

#### Ce que ma relecture de T7 a trouvé, et corrigé

- **Les fichiers recevaient tous les niveaux, `Debug` et `Verbose`
  compris** — trouvé en faisant tourner le conteneur, pas par les tests :
  26 Ko pour une seule requête. L'extension `AddSerilog` du paquet ajoute un
  filtre propre à son fournisseur, et ce filtre l'emporte sur
  `Logging:LogLevel`. Le fournisseur est désormais enregistré à la main, et
  un test vérifie ce qui **n'est pas** écrit.
- **Les refus d'adresse du générateur répétaient leurs secrets** — le
  point 11 ci-dessus. Cinq des six cas du nouveau test fuyaient sur l'ancien
  code.
- **La longueur du fichier lu par le visualiseur était lue deux fois** ; un
  fichier allongé entre les deux lectures aurait pu dépasser la fenêtre de
  4 Mio.

#### Ce qui n'est pas couvert, à connaître

- **Un refus de ComfyUI cite le début de sa réponse**, et ComfyUI peut y
  reprendre une valeur du workflow : un **fragment de prompt peut atteindre
  le journal** par un lot en échec. Ce n'est pas exprès, et c'est la seule
  voie connue ; filtrer le texte d'un tiers serait fragile.
- **Quelqu'un qui peut écrire dans le dossier des journaux** peut y déposer
  un lien dur, ou échanger un fichier entre l'énumération et l'ouverture.
  Les liens symboliques sont écartés, pas ceux-là. Écrire dans ce dossier,
  c'est déjà tenir la machine.
- **Les fichiers de journal prennent les droits du processus** : lisibles par
  qui lit le volume, comme les projets.
- **La rotation quotidienne n'est pas éprouvée** — un test ne peut pas
  attendre minuit. La rétention l'est, par passage de taille, et c'est le
  même mécanisme du puits.
- **Une planche de nombreuses images à la borne** tient autant d'images
  décodées, 256 Mio chacune. Risque accepté (§H.7.2).
- **En conteneur, l'avertissement de MEN-004 ne peut pas savoir** comment le
  port est publié ; il le rappelle à chaque démarrage.

### T5 — code terminé, 7 tâches sur 7, fusionnée dans `main`

**T5 est spécifiée** par
[`docs/pawnsmith-cahier-des-charges-t5.md`](docs/pawnsmith-cahier-des-charges-t5.md)
v1.1 **et écrite**, sur la branche `claude/t5-cutout`, partie de `main`
(`eb18ef4`). Elle se relit d'une traite avec `git log --oneline eb18ef4..`.

| # | Tâche | État |
|---|---|---|
| 1 | PNG lus et écrits à la main, `0.9.0` | ✅ |
| 2 | `UniformBackground` : l'algorithme | ✅ |
| 3 | Port `IBackgroundRemover`, adaptateur, écriture des détourages | ✅ |
| 4 | Le lot détoure ; `CandidateCutout` à la demande | ✅ |
| 5 | API et CLI ; échecs du lot montrés et journalisés | ✅ |
| 6 | Bout en bout : générer, détourer, élire, imprimer | ✅ |
| 7 | Documentation, revue du chapitre 9 (§F.13) | ✅ |

**Les tests de F.9 sont couverts.** Le dépôt porte **999 tests verts**.
Aucune dépendance ajoutée. Chaque étape de l'algorithme et chaque contrôle
du décodeur a été éprouvé par mutation. La planche tirée du test de bout en
bout a été regardée : fonds transparents, recto en place, verso à 180°.

Le code vit dans `src/Pawnsmith.Infrastructure/Imaging/` (`RgbaImage`,
`Crc32`, `PngDecoder`, `PngEncoder`), `Cutout/`, et
`src/Pawnsmith.Application/Generation/CandidateCutout.cs`.

#### Ce que ma relecture de T5 a trouvé, et corrigé

- **Le détourage à la demande lisait l'image jumelée sans borne.** Une
  archive importée peut porter une entrée de plusieurs gigaoctets (C.9.3), et
  le décodeur ne regarde l'en-tête qu'une fois le fichier entier en mémoire.
  La lecture s'arrête désormais, et refuse, dès qu'elle passe 64 Mio ;
  l'adaptateur refait le contrôle avant de décoder.

#### Ce qui n'est pas couvert, à connaître

- **Aucune vraie image n'a été détourée** — le point 1 ci-dessus. À faire
  chez toi, sur les planches de T0a :
  `$CLI cutout --pair "refs/gen comfyui krea2/<fichier>.png" --out ./detourage`.
- **Un vêtement gris qui touche le bord de l'image est mangé** : la diffusion
  part des bords, et ne distingue pas un gris de fond d'un gris de tissu qui
  lui est connexe. La clause de cadrage demande un sujet entier, centré.
- **Les ombres portées sont gardées**, sauf la bande de sol qui couvre toute
  la largeur.
- **Les pixels transparents gardent leur couleur grise** : un halo léger peut
  apparaître si un lecteur interpole sans tenir compte de l'alpha.
- **Un détourage à la borne de 8 192 pixels** tient quelques centaines de
  mégaoctets le temps du calcul. Borné, et risque accepté (§F.13).

### Ce que le mode cloud ne peut pas tester — à faire sur ton poste

Les sessions dans le cloud n'ont ni ComfyUI, ni carte graphique, ni les
planches de T0a (`refs/` n'est pas versionné), ni imprimante. Tout ce qui
suit a été remplacé par des faux ou des images fabriquées en code, et reste
**à éprouver de retour sur ton ordinateur** :

- **Un lot réel contre ComfyUI** (T4) : ton `config/workflow.comfyui.json`
  exporté, et l'ordre de la clause de cadrage avant `Subject:` (§E.5.5).
- **Le détourage sur de vraies images** (T5) : `cutout --pair` sur les
  planches de `refs/gen comfyui krea2/`, puis réglage de `CutoutOptions`
  si le fond, la bande de sol ou le contour ne sortent pas bien.
- **La chaîne entière dans l'interface**, quand le front de T6 existera :
  générer, détourer, élire, tirer la planche.
- **T0b**, planche imprimée en main.

### Comment faire tourner les choses

**Produire une planche** — c'est le livrable réel de T1 :

```bash
dotnet run --project tools/Pawnsmith.Cli -- sheet --manifest ./manifeste.json --calibration ./config/calibration.json --out ./planche.pdf
```

Ajouter `--debug` imprime « tête » et « pieds » dans chaque panneau. Diagnostic
seulement, jamais sur une planche destinée au ciseau.

**Manipuler un projet** — les quatre sous-commandes de C.17. Toutes demandent
`--calibration`, `check` compris (DEC-053).

```bash
CLI="dotnet run --project tools/Pawnsmith.Cli --"

$CLI project new    --root ./data/projects --name "Donjon" --geometry TabAndSocket --paper-format A4 --calibration ./config/calibration.json
$CLI project check  --path ./data/projects/donjon --calibration ./config/calibration.json
$CLI project export --path ./data/projects/donjon --profile Share --out ./data/archives --calibration ./config/calibration.json
$CLI project import --archive ./data/archives/donjon-share-…zip --root ./data/projects --name "Donjon restauré" --calibration ./config/calibration.json
```

**Composer des gabarits et tirer la planche d'un projet** — les sous-commandes
de T3. `add` et `edit` demandent le template et le catalogue de l'univers.

```bash
CFG="--template ./config/prompt-template.fantasy.json --catalog ./config/catalog.fantasy.json --calibration ./config/calibration.json"

$CLI blueprint add    --path ./data/projects/donjon --race goblin --class skirmisher --size Medium --param weapon=spear --details "one ear torn" --quantity 6 $CFG
$CLI blueprint edit   --path ./data/projects/donjon --id <guid> --race orc --class skirmisher --size Medium --param weapon=axe $CFG
$CLI blueprint clause --path ./data/projects/donjon --id <guid> --clause "..." --calibration ./config/calibration.json
$CLI blueprint elect  --path ./data/projects/donjon --id <guid> --candidate <guid> --calibration ./config/calibration.json
$CLI blueprint remove --path ./data/projects/donjon --id <guid> --calibration ./config/calibration.json
$CLI project sheet    --path ./data/projects/donjon --out ./planche.pdf --calibration ./config/calibration.json
```

**Générer des candidats** — les sous-commandes de T4. Il faut un ComfyUI qui
tourne, et **ton** workflow exporté (le dépôt n'a qu'un exemple). `Ctrl+C`
annule le lot ; ce qui a été produit reste dans le projet.

```bash
GEN="--workflow ./config/workflow.comfyui.json --generator-url http://127.0.0.1:8188"

$CLI generator check    $GEN
$CLI candidate generate --path ./data/projects/donjon --id <guid> --count 4 $GEN --calibration ./config/calibration.json
$CLI candidate generate --path ./data/projects/donjon --id <guid> --seed 42 --seed 43 $GEN --calibration ./config/calibration.json
```

**Détourer** (T5) — un candidat existant, ou une image jumelée quelconque,
hors de tout projet. La seconde forme sert à régler `CutoutOptions` sur de
vraies images.

```bash
$CLI candidate cutout --path ./data/projects/donjon --id <guid> --candidate <guid> --calibration ./config/calibration.json
$CLI cutout           --pair ./paire.png --out ./detourage
```

**Lancer l'application** — l'API de T6 est complète ; le front n'est encore
qu'une coquille. L'API s'essaie en `curl` (voir le README).

```bash
docker build -t pawnsmith . && docker run --rm -p 127.0.0.1:8080:8080 -v pawnsmith-logs:/app/data/logs pawnsmith
```

**Lire les journaux** (T7) — une ligne JSON par événement, dans
`/app/data/logs` en conteneur, `data/logs/` sous `dotnet run`.

```bash
curl -s http://127.0.0.1:8080/api/logs
curl -s "http://127.0.0.1:8080/api/logs/pawnsmith-20261003.ndjson?lines=50"
```

### Ce qui reste avant de clore T1

Les critères d'acceptation du §B.9 se cochent **planche imprimée en main**, donc
après T0b.

**Le filigrane est tranché et le sujet est clos** (DEC-060, DEC-061) : aucune
marque sur les planches, ni cachée ni visible. Licence MIT confirmée. Rien à
écrire, aucune tâche T1 ouverte à ce titre.

### T0a et T0b

**T0a** (test décisif de DEC-003) **est menée et concluante** — DEC-043. Le
modèle local effectue une **rotation réelle** du personnage et non un miroir de
la vue de face, ce qui est exactement ce qu'exigeait DEC-002 et la propriété la
plus difficile à obtenir. La génération jumelée est confirmée, T4 peut être
spécifiée sur cette base, et le prompt de référence est consigné dans la fiche.

Deux points de vigilance en découlent, à traiter dans leur tranche et pas avant :
une **bande de sol de 1 %** collée aux pieds, qui risque de survivre au
détourage et de devenir la ligne des pieds (**T5**) ; et une **adhérence
imparfaite à l'équipement demandé** — une hache réclamée, deux dagues obtenues
(**T3**).

À ne pas croire sur parole ailleurs : le modèle installé est **Krea 2 Turbo**,
pas FLUX Krea comme l'annonçaient les versions antérieures du protocole. Les
LoRA et ControlNet FLUX sont **incompatibles**.

Les trois planches de T0a sont dans `refs/gen comfyui krea2/`, non versionnées
comme le reste de `refs/`. Elles sont la matière première des gabarits de T0b.

**T0b** (mesures papier) vient **après** le code de T1, dont elle utilise le CLI
(DEC-033), avec plusieurs fichiers de calibration variantes passés à
`--calibration`. T1 s'écrit donc avec les valeurs provisoires, et c'est normal.
