# Pawnsmith — Cahier des charges : tranche T2

| | |
|---|---|
| **Version** | 1.1 |
| **Date** | 2 septembre 2026 |
| **Document parent** | `pawnsmith-bible.md` v0.10 — à lire en premier, chapitre 3 en particulier |
| **Document frère** | `pawnsmith-cahier-des-charges-t1.md` — partie B, dont ce document reprend la forme |
| **Portée** | Modèle de projet, sérialisation, chargement, sauvegarde, export et import d'archives |

> **Changements depuis la v1.0** — Sept points relevés à la revue. DEC-055 (aucun champ n'est verrouillé après création ; `SaveAsync` garde sa signature). DEC-056 (une donnée de projet n'est jamais rejetée par une donnée de machine : la validation relationnelle devient un diagnostic — corrige une incohérence interne qui rendait non ouvrable une archive cohérente venue d'une autre calibration). DEC-057 (les bornes de C.9.3 sont un record d'options, pas un fichier de configuration ; le fichier arrive en T6). DEC-058 (`<Version>` dans `Directory.Build.props`, T2 vaut **0.3.0** et non 0.2.0). DEC-059 (le CLI prend des sous-commandes, celle de T1 devient `sheet`). Précisions : ordre des clés d'`optionalParameters` (tri ordinal), échappement des caractères non ASCII, et deux paragraphes assumant que les fonctions de C.5 n'ont aucun appelant en T2. Nouvelle section C.17. Le compte de tests passe de 52 à **54**.

> **Deux corrections apportées à la fusion, absentes du document de révisions.** Le diagnostic relationnel de C.4.4 est **conditionné à `geometry == TabAndSocket`** : les deux autres géométries n'ont pas d'onglet, la surcharge n'y est jamais lue, et un projet en `NoSupport` aurait porté un avertissement permanent que rien ne peut lever. Et le test 53 place l'exception dans la bonne couche : c'est le **calcul de la planche** qui lève, dans le contour de découpe, et non la construction de la calibration effective — cette dernière est une simple fusion de champs et ne valide rien, sans quoi DEC-056 réintroduirait la duplication qu'elle vient de supprimer.

> **Comment lire ce document.** Il ne redécrit pas les entités : le **chapitre 3 de la bible** fait foi sur ce qu'elles contiennent et sur pourquoi. Ce document décide ce que la bible laisse ouvert — la forme exacte du fichier, l'identité d'un projet, le comportement de compatibilité, le contenu des archives, et la dépendance du désalignement à des clauses que T2 ne possède pas.
>
> Les sections sont numérotées **C.x** pour que les renvois croisés avec la partie B de T1 (B.x) restent lisibles quand les deux documents sont ouverts côte à côte.

> **Quatorze décisions structurantes sont prises ici** — neuf à la rédaction de la v1.0, cinq de plus à sa revue — et redescendent en fiches DEC dans le chapitre 11 de la bible avant toute implémentation. Elles sont listées en **C.15**. Trois d'entre elles demandent un arbitrage explicite du porteur : DEC-049 et MEN-008/MEN-009 modifient le chapitre 3 ou le chapitre 9, DEC-052 ferme une question laissée ouverte au §15.6.

> **Six contradictions ont été relevées** entre les documents de référence, dont deux dans le cahier des charges T1 lui-même. Elles sont en **C.16**. Aucune n'est bloquante pour T2.

---

## C.0 Consignes de travail

Celles du §0 de T1 s'appliquent intégralement et ne sont pas répétées. Trois s'appliquent avec une force particulière ici :

- **Petites tâches, un commit relisible chacune.** T2 est une tranche large en surface et faible en profondeur : beaucoup de champs, peu d'algorithmes. C'est exactement la configuration où un gros commit devient irrelisible sans être difficile.
- **Aucun code implicite.** Le mapping est manuel (DEC-021), les conventions de nommage sont explicites, aucun attribut de sérialisation ne vit sur une entité de domaine (A.3).
- **Quand une information manque, s'arrêter et demander.** La règle des valeurs physiques (« ne jamais inventer ») ne concerne pas T2 : aucune valeur mesurée n'y entre. En revanche T2 introduit des **bornes de ressources** (C.9.3) qui, elles, s'arbitrent et se documentent — elles ne se mesurent pas au réglet, et les choisir maintenant est légitime.

---

## C.1 Objectif et périmètre

**Entrée** : rien, ou un dossier de projet existant, ou une archive.
**Sortie** : un dossier de projet en clair, lisible, diffable, et une archive ZIP qui peut être envoyée à quelqu'un sans réflexion préalable.

### Dans le périmètre

- Les types de domaine du chapitre 3 : `Project`, `Style`, `Blueprint`, `Candidate`, et les énumérations associées.
- Le schéma de `project.json`, version 1, champ par champ.
- Les lecteurs et écrivains, dans `Pawnsmith.Infrastructure`, avec leurs propres types de document (motif de T1, repris tel quel).
- L'implémentation de `IProjectRepository` (chapitre 7 de la bible) : `LoadAsync`, `SaveAsync`, `ExportArchiveAsync`, `ImportArchiveAsync`.
- La règle d'assemblage du prompt résolu et le calcul du désalignement, comme règles de **domaine pur** (C.5).
- Les surcharges de calibration par projet (C.4).
- Le durcissement des deux surfaces d'archive : MEN-001 à l'import, MEN-006 à l'export, et deux menaces nouvelles (C.10).
- Un **point d'entrée en ligne de commande**, en dernière tâche de la tranche, spécifié en **C.17** (DEC-059).

### Hors périmètre — ne rien écrire de tout cela

Points de terminaison d'API, interface, composeur de prompts, catalogue, client ComfyUI, détourage, moteur de mise en page (il existe déjà, en T1), migration de schéma, création d'un fichier de configuration (DEC-057), verrouillage concurrent, journalisation structurée au-delà de ce que la solution porte déjà.

> **Deux tentations à nommer avant qu'elles ne se présentent.** La première est d'écrire un mécanisme de migration « puisqu'on y est » : il n'y a qu'une version de schéma, donc rien à migrer, et un moteur de migration sans migration est une abstraction introduite au cas où (§0 de T1). La seconde est de faire produire au dépôt une classe `Catalog` parce que `optionalParameters` en évoque une : le catalogue est en T3 et la question D du chapitre 16 de la bible — global ou embarqué — n'est pas tranchée. T2 stocke des paires clé/valeur et ne les valide contre rien.

---

## C.2 Vocabulaire et identifiants

DEC-037 s'applique sans exception : **les types, les membres, les clés de fichier et les valeurs d'énumération sont en anglais.** La table de correspondance de DEC-037 fait foi. Rappel des identifiants que T2 introduit ou consomme :

| Glossaire (ch. 2) | Type / clé |
|---|---|
| Projet | `Project` |
| Style | `Style` |
| Gabarit | `Blueprint` |
| Candidat | `Candidate` |
| Taille | `Size` — `Small`, `Medium`, `Large`, `Huge`, `Gargantuan` |
| Géométrie | `Geometry` — `FoldedTent`, `TabAndSocket`, `NoSupport` |
| Univers | `Universe` — `Fantasy` |
| Clause sujet | `subjectClause` |
| Clause style | `styleClause` |
| Clause cadrage | `framingClause` |
| Prompt résolu | `ResolvedPrompt` — **fonction**, pas champ |
| Désaligné | `Misalignment` / `IsMisaligned` — **calculé**, pas champ |

Deux points de nommage non évidents, à expliciter dans le code plutôt qu'à subir :

- **`class` est un mot réservé en C#.** Le champ « classe » du gabarit s'appelle donc `characterClass`, dans le type comme dans le JSON. Écrire `@class` pour sauver la symétrie serait pire : un identifiant échappé se lit mal et se cherche mal.
- **`Geometry` compte trois valeurs**, pas deux. DEC-039 a ajouté `NoSupport` ; le tableau du §3.1 de la bible n'a pas été mis à jour (voir C.16). Le schéma de T2 en accepte trois.

---

## C.3 Le fichier projet

### C.3.1 Arborescence et nom de fichier

```
{racineDesProjets}/
└── {nomDeDossier}/
    ├── project.json
    ├── images/
    │   ├── {candidateId}-pair.png
    │   ├── {candidateId}-front.png
    │   └── {candidateId}-back.png
    └── exports/
        └── {quelqueChose}.pdf
```

**Le fichier s'appelle `project.json`, pas `projet.json`.** Le §3.2 de la bible écrit encore le nom en français ; c'est un reliquat d'avant DEC-037, qui a placé les clés de fichier du côté anglais. Un nom de fichier français contenant des clés anglaises est exactement l'écart d'une ligne à l'autre que DEC-037 supprime. Décision à consigner (**DEC-046**).

`images/` et `exports/` sont les **deux seuls** sous-dossiers reconnus. Cette clôture n'est pas cosmétique : c'est elle qui rend la liste blanche d'export (C.8.2) exacte et testable. Un dossier inconnu dans un dossier de projet n'empêche pas le chargement, mais il ne part jamais dans une archive.

`{racineDesProjets}` est `/app/data/projects` en conteneur (A.6), configurable. **Aucun chemin absolu de cette racine n'apparaît jamais dans `project.json`.**

### C.3.2 Ce qui identifie un projet

**Question tranchée : un champ `projectId`, opaque et immuable. Le nom du dossier n'a aucune sémantique, et `name` n'est ni unique ni stable.**

| Candidat à l'identité | Verdict | Motif |
|---|---|---|
| Le nom du dossier | Non | DEC-011 fait du dossier un **dossier ordinaire**, versionnable et sauvegardable. Un dossier ordinaire se renomme, se duplique, se restaure sous un autre nom. Faire du nom l'identité, c'est interdire tout cela sans l'écrire nulle part. |
| Le champ `name` | Non | C'est un libellé d'affichage. Deux projets peuvent légitimement s'appeler « Donjon de test ». |
| Un `projectId` | **Oui** | UUID v4, généré à la création, jamais recalculé, jamais dérivé du nom. |

Formuler la décision à l'endroit : **`projectId` n'est pas une fonctionnalité, c'est l'absence d'une contrainte.** Il ne sert, en T2, ni à indexer ni à dédupliquer — il sert à ce que renommer un dossier ne crée pas un projet différent. Ses consommateurs réels viendront plus tard : corrélation de journaux (chapitre 8), et la question « ai-je déjà ce projet ? » à l'import, qui est un comportement d'interface (T6).

**Conséquence explicite, pour qu'elle ne soit pas ajoutée en silence :** T2 **ne vérifie pas** l'unicité de `projectId` dans la racine des projets. Importer deux fois la même archive produit deux dossiers portant le même `projectId`, et c'est acceptable — c'est une copie, pas une corruption. Détecter le doublon et proposer « remplacer / garder les deux » est un dialogue, donc T6.

**Le nom de dossier à la création** est dérivé de `name` par translittération, et c'est une surface d'attaque, pas une commodité (C.10, MEN-009) :

- caractères conservés : `a-z`, `0-9`, `-` ; tout le reste est translittéré puis remplacé par `-` ; les `-` consécutifs sont fusionnés ; les `-` de tête et de queue sont retirés ;
- longueur bornée à 64 caractères ;
- un résultat vide donne `project` ;
- noms réservés Windows rejetés (`CON`, `PRN`, `AUX`, `NUL`, `COM1`–`COM9`, `LPT1`–`LPT9`), points et espaces finaux interdits ;
- collision résolue par suffixe `-2`, `-3`, … ;
- le chemin résultant est **vérifié comme étant strictement sous la racine des projets** avant toute écriture.

### C.3.3 Règles de forme du fichier

Ces règles existent parce que DEC-011 justifie la persistance en clair par un mot précis : **diffable**. Un fichier illisible en `git diff` ne tient pas cette promesse.

| Règle | Valeur | Motif |
|---|---|---|
| Encodage | UTF-8 **sans BOM** | Un BOM casse la comparaison octet à octet et surprend les outils Unix |
| Fins de ligne | `\n` | Un projet traverse les machines |
| Indentation | 2 espaces, JSON indenté | Diff lisible |
| Ordre des membres | Celui des tables de C.3.4, **fixe** | Deux sauvegardes du même projet doivent produire le même fichier |
| Nombres décimaux | Culture **invariante**, séparateur `.`, format aller-retour | Une session `fr-FR` écrivant `12,0` produit un projet illisible ailleurs. C'est le défaut .NET le plus banal de cette tranche, et il est invisible tant qu'on développe en anglais |
| Ordre des clés d'un dictionnaire | Tri par clé, comparaison **ordinale** | Voir la note |
| Caractères non ASCII | Écrits **littéralement**, non échappés en `\uXXXX` | Voir la note |
| Horodatages | ISO 8601 **UTC**, suffixe `Z`, précision seconde | Un projet n'a pas de fuseau |
| Booléens, énumérations | Chaînes littérales pour les énumérations (`"Medium"`), jamais d'entier ordinal | Un ordinal se décale silencieusement quand on insère une valeur |
| Écriture | Fichier temporaire dans le **même dossier**, puis remplacement atomique | Le dossier du projet est la seule copie de l'utilisateur. Une écriture interrompue ne doit jamais laisser un `project.json` tronqué |

> **Pourquoi le tri des clés, et pourquoi ordinal.** `optionalParameters` est la seule table libre du schéma, et elle n'apparaît dans aucune des tables de C.3.4 qui fixent l'ordre des membres. L'ordre d'énumération d'un dictionnaire .NET n'étant pas garanti, le test 19 — deux sauvegardes identiques octet pour octet — n'était pas satisfaisable de façon déterministe. Le tri le rend déterministe sans rien perdre : ces clés ne portent aucun ordre sémantique, contrairement à `blueprints` et `candidates` dont C.3.4 impose explicitement l'inverse.
>
> La comparaison doit être **ordinale**, jamais dépendante de la culture. Un tri culturel varie avec la culture du processus et avec la version d'ICU installée : deux machines produiraient deux fichiers différents pour le même projet, et le défaut se manifesterait à l'intégration continue plutôt qu'en développement. C'est le même piège que l'écriture des décimales, une ligne plus haut dans cette table.
>
> **La règle générale, pour les collections à venir :** une collection de ce fichier est soit **explicitement ordonnée** — l'ordre d'insertion est significatif et n'est jamais trié —, soit **explicitement triée** par clé en ordinal. Il n'y a pas de troisième cas, et une nouvelle collection doit choisir son camp dans le schéma, pas dans le code.

> **Sur l'échappement.** `System.Text.Json` échappe par défaut tout ce qui sort de l'ASCII : « Griffe Noire » passe, mais un accent devient `\u00e9`. C'est déterministe, donc le test 19 ne s'en plaint pas — mais cela détruit la promesse de DEC-011, qui justifie le format en clair par le fait qu'il soit lisible et diffable. L'échappement doit donc être relâché. Ce durcissement par défaut existe pour le cas où du JSON est **inséré dans une page HTML ou un script** ; `project.json` est écrit sur disque et relu par notre propre désérialiseur, jamais inliné dans une page. Le relâcher ici est sans conséquence. **Ce qui doit rester vrai en T6** : si l'API sert un jour du contenu de projet dans une page, l'échappement de ce contexte est la responsabilité de ce contexte, et il ne doit rien attendre de l'encodeur choisi ici.

**L'écriture est idempotente** : sauvegarder deux fois un projet inchangé produit deux fichiers identiques octet pour octet, `modifiedAt` mis à part. C'est vérifiable par un test, et c'est ce qui rend un `git diff` de projet exploitable.

### C.3.4 Schéma, version 1

#### Racine

| Clé | Type | Obligatoire | Notes |
|---|---|---|---|
| `versionSchema` | entier | oui | Vaut `1`. Voir C.6 |
| `projectId` | chaîne | oui | UUID v4, minuscules, forme canonique à tirets |
| `name` | chaîne | oui | 1 à 120 caractères après `Trim`, non vide |
| `universe` | chaîne | oui | `Fantasy` |
| `geometry` | chaîne | oui | `FoldedTent` \| `TabAndSocket` \| `NoSupport` |
| `paperFormat` | chaîne | oui | Clé existant dans `paperFormats` de la calibration. **Non validée à la lecture** — voir la note ci-dessous |
| `style` | objet | oui | Voir plus bas |
| `calibrationOverrides` | objet | oui | Toujours écrit, membres à `null` si absence de surcharge. Voir C.4 |
| `blueprints` | tableau | oui | Peut être vide. **L'ordre est significatif** |
| `createdAt` | chaîne | oui | ISO 8601 UTC |
| `modifiedAt` | chaîne | oui | ISO 8601 UTC, réécrit à chaque sauvegarde |

> **Pourquoi `paperFormat` n'est pas validé contre la calibration à la lecture.** Le fichier de calibration est une donnée de machine, le projet est une donnée d'utilisateur. Un projet créé sur une machine dont la calibration déclare `A4Paysage` (DEC-036) doit pouvoir s'ouvrir sur une machine qui ne l'a pas — pour être corrigé, pas pour planter. Le format inconnu est signalé comme diagnostic à la lecture (C.7.3) et devient une erreur au moment où quelqu'un demande une planche, c'est-à-dire dans le moteur de T1, qui la lève déjà.

> **L'ordre de `blueprints` est fonctionnel, pas cosmétique.** Le §B.5.1 de T1 pagine les groupes de taille « dans l'ordre des tailles tel qu'il apparaît dans le manifeste ». Le manifeste sera produit à partir du projet ; l'ordre des gabarits détermine donc l'ordre des pages du PDF. Ne jamais trier à la lecture, ne jamais réordonner à l'écriture.

#### `style`

Objet de valeur, sans identifiant : un projet a exactement un style, et un style n'existe pas hors de son projet.

| Clé | Type | Obligatoire | Notes |
|---|---|---|---|
| `name` | chaîne | oui | Peut être vide |
| `styleClause` | chaîne | oui | Peut être vide. Normalisée (C.5.3). En anglais (DEC-037) |
| `negativeClause` | chaîne | oui | Peut être vide |
| `palette` | chaîne | oui | Peut être vide |

#### `blueprints[]`

| Clé | Type | Obligatoire | Notes |
|---|---|---|---|
| `id` | chaîne | oui | UUID v4 |
| `race` | chaîne | oui | Non vide après `Trim` |
| `characterClass` | chaîne | oui | Non vide après `Trim` |
| `size` | chaîne | oui | `Small` \| `Medium` \| `Large` \| `Huge` \| `Gargantuan` |
| `optionalParameters` | objet | oui | `chaîne → chaîne`. Peut être vide. Clés non validées en T2 |
| `details` | chaîne | oui | Peut être vide |
| `subjectClause` | chaîne | oui | Peut être vide. **Stockée et éditable** (DEC-028). Normalisée (C.5.3) |
| `quantity` | entier | oui | ≥ 1 |
| `candidates` | tableau | oui | Peut être vide. Ordre significatif (affichage) |
| `electedCandidateId` | chaîne ou `null` | oui | Doit désigner un candidat **de ce gabarit** |

> **`optionalParameters` : une clé absente signifie « non contraint »**, jamais « absent de l'illustration » (DEC-024). Le modèle ne peut pas exprimer la différence, et c'est voulu : la distinction est portée par la formulation de l'interface (§15.3), pas par une troisième valeur.

#### `blueprints[].candidates[]`

| Clé | Type | Obligatoire | Notes |
|---|---|---|---|
| `id` | chaîne | oui | UUID v4 |
| `seed` | chaîne | oui | Entier non signé 64 bits **écrit en chaîne décimale**. Voir la note |
| `framingClauseUsed` | chaîne | oui | Clause de cadrage figée à la génération |
| `subjectClauseUsed` | chaîne | oui | Clause sujet figée à la génération |
| `styleClauseUsed` | chaîne | oui | Clause style figée à la génération |
| `status` | chaîne | oui | `Draft` \| `Valid` \| `Rejected` |
| `pairedImageFile` | chaîne ou `null` | oui | Image jumelée brute, conservée pour diagnostic |
| `frontImageFile` | chaîne ou `null` | oui | PNG détouré |
| `backImageFile` | chaîne ou `null` | oui | PNG détouré |
| `generatedAt` | chaîne | oui | ISO 8601 UTC |

> **Pourquoi `seed` est une chaîne.** Les graines de ComfyUI vont jusqu'à 2⁶⁴−1. Un nombre JSON est lu par `JSON.parse` comme un flottant IEEE-754 : au-delà de 2⁵³ la valeur est **silencieusement arrondie**. Le front est en React (DEC-018), donc la graine passera par `JSON.parse` en T6. Une graine arrondie est une génération irreproductible, et le défaut ne se voit qu'au moment où l'on rejoue une graine — c'est-à-dire jamais pendant les tests. La chaîne décimale coûte une conversion et supprime le cas.

> **Les trois clauses figées remplacent le champ unique `promptUtilise` du §3.1.** C'est un changement du chapitre 3, argumenté en C.5.2, à consigner en **DEC-049**.

> **`frontImageFile` et `backImageFile` sont nullables**, parce que le cycle de vie l'impose : entre la génération (T4) et le détourage (T5), un candidat possède une image jumelée et aucun détourage. Refuser le `null` obligerait T4 à écrire des chemins vers des fichiers inexistants, ce qui est pire. Le couplage entre `status` et la présence des fichiers est une **règle de gestion**, donc question C du chapitre 16 de la bible, donc T3 — T2 ne l'invente pas.

### C.3.5 Les chemins d'images

**Question tranchée : relatifs au dossier du projet, séparateur `/`, obligatoirement sous `images/`.**

La portabilité est la raison d'être de DEC-011 ; un chemin absolu la supprime entièrement. Mais ce n'est pas le seul motif, et le second est plus intéressant :

1. **Portabilité.** Un `C:\Users\gregoire\...` rend l'archive inutilisable ailleurs. Un `/app/data/projects/...` la rend inutilisable hors conteneur.
2. **Fuite d'information.** Le chapitre 8 interdit aux journaux de voyager avec une archive, et cite explicitement « des chemins absolus » parmi ce qu'ils contiennent. Un chemin absolu dans `project.json` ferait entrer par la porte ce que MEN-006 fait sortir par la fenêtre : le nom de compte de l'utilisateur, sa distribution, l'arborescence de sa machine.
3. **Surface d'attaque.** Un chemin lu dans un fichier et concaténé à une racine est la définition de MEN-002. Un projet peut arriver d'une archive tierce (donc hostile) ou d'une édition à la main.

Règles de validation, appliquées **à la lecture** et **avant tout accès disque** :

- le chemin est relatif, sans racine, sans lettre de lecteur, sans préfixe UNC ;
- le seul séparateur admis est `/` ; un `\` fait rejeter le fichier (c'est un séparateur sur une plateforme et un caractère de nom valide sur l'autre — l'ambiguïté se tranche, elle ne se devine pas) ;
- aucun segment `.` ni `..` ;
- le premier segment est `images` ;
- le chemin absolu résolu est **préfixé par le chemin absolu du dossier du projet**, vérifié après résolution des liens, exactement comme MEN-001 l'exige à l'import.

Tout écart fait rejeter le chargement avec `PROJECT_PATH_ESCAPE`, sans qu'aucun fichier n'ait été ouvert.

> **Alternative écartée : ne pas stocker les chemins et les dériver de `candidateId`.** Le §3.2 de la bible suggère une convention (`{idCandidat}-recto.png`), et l'on pourrait s'en contenter — un chemin dérivable est un chemin qu'on ne persiste pas, ce qui flatte le critère d'acceptation de T2. Deux raisons de ne pas le faire. D'abord, EVO-010 (import d'images externes) apportera des fichiers dont Pawnsmith n'a pas choisi le nom, et la convention casse ce jour-là — dans un schéma déjà figé. Ensuite, une convention de nommage implicite est du code magique : elle ne se voit pas en relecture, elle ne s'exprime pas dans le type, et elle produit une erreur de fichier introuvable au lieu d'une erreur de validation. Le §0 de T1 l'interdit.
>
> **Cela ne contredit pas le critère « aucune valeur dérivée n'est sérialisée ».** Ce critère vise les valeurs **calculées à partir d'autres données du modèle** — `resolvedPrompt`, `misaligned` — qui mentent dès la première modification manquée. L'emplacement d'un fichier n'est pas calculé : c'est un fait externe constaté.

### C.3.6 Où la garantie « aucune valeur dérivée » est portée

Elle n'est pas portée par une consigne, ni par un test de bonne volonté, mais **par les types**, comme DEC-028 porte le verrouillage des clauses par la signature du port.

Le motif est celui de T1 (A.3) : le domaine ne référence rien, donc les entités ne portent aucun attribut de sérialisation. L'Infrastructure définit ses propres types de document — `ProjectDocument`, `StyleDocument`, `BlueprintDocument`, `CandidateDocument` — en `record` immuables, et deux jeux de méthodes d'extension explicites, `ToDocument()` et `ToDomain()` (DEC-021, jamais AutoMapper).

**`CandidateDocument` n'a ni membre `resolvedPrompt` ni membre `misaligned`.** Il n'y a donc rien à oublier de ne pas écrire : ce n'est pas exprimable. Le test n° 14 vérifie l'absence textuelle de ces clés dans le fichier produit, mais il vérifie une propriété déjà garantie par la construction — c'est un filet, pas le mécanisme.

Le sens inverse mérite d'être dit aussi : `ToDomain()` **ne recalcule rien**. Il construit des entités à partir du document, un point c'est tout. Le prompt résolu et le désalignement se calculent quand on les demande, à partir de l'entité et des clauses courantes (C.5), et jamais au chargement.

### C.3.7 Exemple complet

```json
{
  "versionSchema": 1,
  "projectId": "8f1a3c2e-5b47-4d90-a1e6-72c9f0d4b833",
  "name": "Donjon de la Griffe Noire",
  "universe": "Fantasy",
  "geometry": "TabAndSocket",
  "paperFormat": "A4",
  "style": {
    "name": "Encre et lavis",
    "styleClause": "clean digital illustration, ink outlines with muted watercolour wash, restrained earth palette, flat even lighting",
    "negativeClause": "photorealistic, 3d render, text, watermark, signature",
    "palette": "muted earth tones, desaturated greens and browns"
  },
  "calibrationOverrides": {
    "tabWidthMm": 11.5,
    "tabHeightMm": null
  },
  "blueprints": [
    {
      "id": "2d6b1f04-9c33-4a71-8e52-0b7d61a9c418",
      "race": "goblin",
      "characterClass": "skirmisher",
      "size": "Medium",
      "optionalParameters": {
        "weapon": "short spear",
        "armour": "leather scraps"
      },
      "details": "one ear torn, bone fetish tied to the belt",
      "subjectClause": "a goblin skirmisher, wielding a short spear held vertically against the body, wearing leather scraps, one ear torn, a bone fetish tied to the belt",
      "quantity": 6,
      "candidates": [
        {
          "id": "b4c7e910-2f88-4d16-9a03-5e1c8b72d055",
          "seed": "10428836719284460113",
          "framingClauseUsed": "Character rotation sheet for a miniature reference: front view on the left and back view on the right, both figures on the same ground line, full body, feet touching the bottom edge, compact narrow silhouette, flat uniform pale grey background, no cast shadow, crisp clean outlines suitable for automatic cutout.",
          "subjectClauseUsed": "a goblin skirmisher, wielding a short spear held vertically against the body, wearing leather scraps, one ear torn, a bone fetish tied to the belt",
          "styleClauseUsed": "clean digital illustration, ink outlines with muted watercolour wash, restrained earth palette, flat even lighting",
          "status": "Valid",
          "pairedImageFile": "images/b4c7e910-2f88-4d16-9a03-5e1c8b72d055-pair.png",
          "frontImageFile": "images/b4c7e910-2f88-4d16-9a03-5e1c8b72d055-front.png",
          "backImageFile": "images/b4c7e910-2f88-4d16-9a03-5e1c8b72d055-back.png",
          "generatedAt": "2026-09-01T14:22:07Z"
        },
        {
          "id": "0a19d5c3-7b64-4e28-b0f7-3c2a91e8d740",
          "seed": "42",
          "framingClauseUsed": "Character rotation sheet for a miniature reference: front view on the left and back view on the right, both figures on the same ground line, full body, feet touching the bottom edge, compact narrow silhouette, flat uniform pale grey background, no cast shadow, crisp clean outlines suitable for automatic cutout.",
          "subjectClauseUsed": "a goblin skirmisher, wielding a short spear, wearing leather scraps",
          "styleClauseUsed": "clean digital illustration, ink outlines with muted watercolour wash, restrained earth palette, flat even lighting",
          "status": "Rejected",
          "pairedImageFile": "images/0a19d5c3-7b64-4e28-b0f7-3c2a91e8d740-pair.png",
          "frontImageFile": "images/0a19d5c3-7b64-4e28-b0f7-3c2a91e8d740-front.png",
          "backImageFile": "images/0a19d5c3-7b64-4e28-b0f7-3c2a91e8d740-back.png",
          "generatedAt": "2026-09-01T14:19:41Z"
        }
      ],
      "electedCandidateId": "b4c7e910-2f88-4d16-9a03-5e1c8b72d055"
    },
    {
      "id": "6e3f8a25-14bd-4c07-9f81-a5d206e3b9c1",
      "race": "ogre",
      "characterClass": "bruiser",
      "size": "Large",
      "optionalParameters": {},
      "details": "",
      "subjectClause": "an ogre bruiser, bare-chested, holding a crude club vertically against the body",
      "quantity": 1,
      "candidates": [],
      "electedCandidateId": null
    }
  ],
  "createdAt": "2026-08-31T09:12:00Z",
  "modifiedAt": "2026-09-01T14:22:11Z"
}
```

Ce que cet exemple montre volontairement : un candidat **désaligné** (le second gabarit n'est pas concerné, mais le candidat `Rejected` du premier a une `subjectClauseUsed` plus courte que la `subjectClause` actuelle — il est donc désaligné, et le calcul le dira sans que rien ne soit stocké) ; un projet avec **une seule surcharge sur deux** ; un gabarit **sans aucun candidat**, qui est un état parfaitement normal.

---

## C.4 Surcharges de calibration par projet

### C.4.1 Ce qui est surchargeable, et pourquoi la liste est close

DEC-040 nomme trois catégories de valeur physique et n'en rend qu'une modifiable : le **matériel possédé**. La liste des surcharges de T2 est donc exactement celle-là.

| Valeur | Catégorie DEC-040 | Surchargeable par projet |
|---|---|---|
| `tabWidthMm` | Matériel possédé | **Oui** |
| `tabHeightMm` | Matériel possédé | **Oui** |
| `flapHeightMm` | Mesure d'imprimante et de papier | Non |
| `gutterMm` | Préférence d'usage | **Non** — voir C.4.2 |
| `silhouetteMarginMm` | Entre les deux (§15.6) | Non — voir la note |
| `pageMarginMm`, `calibrationZoneHeightMm`, `scaleCorrectionFactor` | Mesure d'imprimante | Non |
| `pawnHeightMm`, `gridFootprintMm` | Table de référence | Non |

**La liste est close, et `calibrationOverrides` n'est pas un dictionnaire.** Un `overrides: { "n'importe quelle clé": valeur }` serait plus court à écrire et strictement pire : il rendrait surchargeable tout ce que la calibration contient, y compris `scaleCorrectionFactor`, dont DEC-040 explique précisément pourquoi il doit rester verrouillé. Une convention implicite qui ouvre par défaut est l'inverse de ce que §0 de T1 demande. Deux membres nommés, deux entrées dans le schéma, deux validations.

> **`silhouetteMarginMm` reste dans la calibration.** Le §15.6 le classe « entre les deux » sans trancher. L'argument qui le fait pencher : depuis DEC-042, la marge de silhouette n'absorbe plus une incertitude de cadrage — le cadrage est contraint à la source — elle absorbe une imprécision de découpe. C'est une propriété de la main et du ciseau, pas du projet. Même raisonnement que `gutterMm` ci-dessous, et même conclusion.

### C.4.2 `gutterMm` : la question du §15.6 est fermée par la négative

**Décision : `gutterMm` ne devient pas un réglage de projet. Il reste dans `calibration.json`.**

La question était posée comme si la réponse allait de soi — la valeur est une préférence, les préférences vont dans le projet. Elle ne va pas de soi, et le critère de DEC-040 dit le contraire de ce que la formulation suggère.

DEC-040 ne classe pas « préférence contre mesure ». Il classe selon **qui détermine la valeur**, et il rend modifiable ce qui est déterminé par un objet que l'utilisateur possède et remplace : la fente de ses socles. `gutterMm` est déterminé par la dextérité de l'utilisateur au ciseau. Or Pawnsmith est **mono-utilisateur** (§1.5) : une propriété de l'utilisateur est, dans cette application, une propriété globale. La mettre au niveau du projet oblige la même personne à ressaisir la même valeur dans chaque projet, et produit à terme des projets qui divergent sur un réglage qui n'avait aucune raison de varier.

Le contre-argument existe et il faut le dire : une planche de vingt `Small` se découpe plus confortablement avec une gouttière généreuse qu'une planche d'un `Gargantuan`, donc la valeur idéale dépend un peu du contenu. Il est réel mais mince, et surtout il ne désigne pas le projet comme bon niveau — il désignerait la **taille**, ce qui est une autre décision, plus coûteuse, et sans demande.

Ce que la décision coûte si elle est fausse : rien d'irréversible. Ajouter une surcharge à `calibrationOverrides` est une extension de schéma, donc un `versionSchema` de plus. Retirer une surcharge déjà utilisée par des projets existants serait une régression. La décision est prise dans le sens qui se rattrape.

À consigner en **DEC-052**, qui ferme la question ouverte du §15.6 de la bible.

### C.4.3 Résolution des valeurs effectives

**Le domaine ne voit jamais une surcharge. Il voit des valeurs résolues.**

C'est le pendant exact du « le rendu ne décide de rien » du §B.6 de T1. La résolution se fait en **un seul endroit**, dans `Pawnsmith.Application`, avant tout appel au moteur de mise en page :

```
CalibrationEffective = Calibration ∪ Project.CalibrationOverrides
```

- membre à `null` → valeur de la calibration ;
- membre renseigné → valeur du projet ;
- tout le reste de la calibration est recopié à l'identique.

Le moteur de T1 reçoit une calibration effective et **sa signature ne change pas**. Aucune ligne de T1 n'est modifiée par cette tranche. C'est le résultat qu'on cherche : T2 ajoute un niveau de configuration sans toucher au code qui la consomme, parce que le §B.2 avait déjà imposé que ce code lise les valeurs sans les connaître.

> **Conséquence à rendre visible, pas à découvrir.** DEC-040 le dit déjà : `tabHeightMm` entre dans la hauteur de cellule du §B.5.2, donc **la capacité d'une page dépend désormais du projet**. Deux projets identiques avec des socles différents ne tiennent pas le même nombre de pions par page. Ce n'est pas un défaut, mais l'indicateur de capacité de T6 (§15.4) doit être calculé sur la calibration **effective**, jamais sur le fichier de calibration.

### C.4.4 Validation d'une surcharge

DEC-056 scinde la validation en deux classes, et les surcharges tombent des deux côtés.

**Intrinsèque — bloquante partout.** Une surcharge renseignée doit être un nombre fini et strictement positif. Une valeur nulle, négative, infinie, `NaN` ou non numérique est rejetée par `PROJECT_OVERRIDE_INVALID`, avec un message nommant la valeur fautive, à la sauvegarde comme au chargement comme à l'import. Elle est fausse en elle-même, sur toutes les machines.

**Relationnelle — bloquante nulle part dans T2.** La contrainte `tabWidthMm ≤ pawnWidthMm` de chaque taille utilisée confronte une donnée de projet à une donnée de machine. Elle produit un **diagnostic** au chargement, et rien de plus.

> **Le diagnostic n'est émis que si `geometry` vaut `TabAndSocket`.** C'est la seule géométrie qui possède un onglet : `FoldedTent` pose un volet sur toute la largeur du pion, et `NoSupport` ne pose rien du tout (DEC-039). Dans ces deux cas, `tabWidthMm` n'est jamais lu, le contrôle du domaine n'est jamais atteint, et un projet en `NoSupport` porterait donc un avertissement **permanent que rien ne peut lever** — pas même en corrigeant la valeur, puisqu'aucune valeur ne serait bonne. C'est exactement le faux positif que le §C.5.4 désigne comme le mode de défaillance à éviter : un avertissement qui se déclenche à tort finit par être ignoré, ce qui revient à ne pas l'avoir.

Le diagnostic devient une erreur au moment où quelqu'un demande une planche en `TabAndSocket` : c'est le **calcul de la planche** qui échoue, dans la construction du contour de découpe, avec l'exception nommant la valeur fautive qu'impose la convention 5 de DEC-038.

> **Où l'exception vit, et pourquoi pas ailleurs.** Elle est **déjà écrite**, dans le contour de découpe de T1, et elle n'a pas à être dupliquée ici. La construction de la calibration effective (C.4.3) est une **simple fusion de champs : elle ne valide rien**. Lui confier la vérification reviendrait à réintroduire, un cran plus haut, la duplication que DEC-056 vient précisément de supprimer — la vérité « l'onglet ne peut pas être plus large que le pion » doit rester écrite à un seul endroit, celui où DEC-038 l'a mise.

Le motif est celui de `paperFormat` (C.3.4) et des images manquantes (C.7.2), et il est désormais le même dans les trois cas : **un projet s'ouvre pour être corrigé, pas pour planter.** La v1.0 rejetait la surcharge incompatible, ce qui rendait non ouvrable une archive parfaitement cohérente venue de quelqu'un dont les socles ont une autre fente — c'est-à-dire le scénario même que le profil `Share` existe pour rendre agréable.

Aucune borne haute n'est imposée sur `tabHeightMm` : une valeur absurde produit une capacité nulle, que le moteur de T1 signale déjà en nommant la taille, le format et la géométrie.

**Conséquence sur le contrat, inchangée :** valider intrinsèquement ne demande pas la calibration, mais produire les diagnostics relationnels et construire la calibration effective la demandent. `LoadAsync(path, calibration, ct)` reste la signature retenue (DEC-053).

---

## C.5 Prompt résolu et désalignement — la dépendance arbitrée

### C.5.1 Ce que T2 possède réellement

La question posée était : T2 a besoin du prompt résolu sans posséder deux de ses trois clauses. **Cette formulation est inexacte, et la correction change la taille du problème.**

| Clause | Où elle vit | T2 la possède ? |
|---|---|---|
| Cadrage | Template de workflow ComfyUI (DEC-029, ch. 6) | **Non** — T4 |
| Sujet | Champ `subjectClause` du gabarit (DEC-028) | **Oui** — c'est un champ que T2 persiste |
| Style | Champ `styleClause` du style de projet | **Oui** |

Ce qui relève de T3 n'est pas la clause sujet mais le **composeur**, c'est-à-dire la fonction qui produit sa valeur initiale à partir de la race, de la classe et du catalogue. Une fois produite, elle est stockée, éditable, et elle ne se régénère pas toute seule (§3.1). T2 la lit et l'écrit sans avoir besoin de savoir la fabriquer.

**Il manque donc une clause, pas deux.** Et cette clause est une constante d'application, pas une donnée de projet.

### C.5.2 Décision

**Option A est retenue, dans une forme plus étroite que celle proposée.**

Ce que T2 écrit :

1. une fonction pure de domaine `ResolvePrompt(framingClause, subjectClause, styleClause) → string` ;
2. une fonction pure de domaine qui compare les clauses figées d'un candidat aux clauses courantes et rend l'**ensemble des clauses désalignées** ;
3. rien d'autre.

Ce que T2 **n'écrit pas**, et c'est la restriction qui compte : **aucun port, aucune interface, aucun fournisseur de clause de cadrage.** Formulée comme « T2 prend les deux clauses comme des données fournies », l'option A invite à créer un `IFramingClauseProvider` pour matérialiser le « fourni ». Ce serait une abstraction introduite au cas où, interdite par le §0 de T1, et elle vivrait dans le domaine, qui ne référence rien. La clause de cadrage est un **paramètre de fonction**. Le câblage qui va la chercher dans le template de workflow est un problème de T4, et il n'existe pas avant.

Pourquoi pas l'option B (reporter à T3) : parce que DEC-030 fait du désalignement le mécanisme de sécurité central, et qu'un mécanisme de sécurité qui n'est pas testable à la tranche où le modèle est figé n'est testé nulle part. Le point décisif est ailleurs, cependant : **le schéma doit de toute façon décider ce que le candidat fige**, et cette décision est irréversible une fois des projets écrits. Reporter le calcul n'aurait pas reporté la décision de schéma — il l'aurait seulement prise sans y penser.

Pourquoi pas l'option C (remonter la clause cadrage dans le projet) : elle contredit DEC-029 frontalement, et pour un gain nul. DEC-029 fonde son choix sur le fait que modifier le cadrage produit un **défaut fonctionnel**. Le stocker par projet, c'est le rendre modifiable par projet, donc le rendre atteignable par une interface un jour, donc rouvrir ce que DEC-028 et DEC-029 ferment à deux reprises.

**En revanche, le candidat fige ses trois clauses séparément, et non le prompt assemblé.** C'est un écart au §3.1 de la bible, qui prévoit un champ unique `promptUtilise`. Trois arguments :

- **Attribution.** DEC-030 justifie le désalignement par le fait qu'il « désigne **quels** candidats sont concernés » plutôt que d'avertir dans le vide. Le même argument vaut d'un cran plus bas : savoir *quelle clause* a bougé est ce qui rend l'information actionnable. Avec une chaîne unique, l'interface ne peut dire que « quelque chose a changé ».
- **Le piège du cadrage.** La clause de cadrage est éditable — c'est le point d'extension expert de DEC-029. Un utilisateur qui ajuste son template de workflow désaligne **toute sa bibliothèque, dans tous ses projets, d'un coup**. C'est le comportement correct, mais avec une chaîne unique il est incompréhensible et sans recours. Avec trois clauses, l'interface dit « le cadrage a changé », et l'utilisateur sait qu'il vient de le faire.
- **Cohérence avec le critère d'acceptation.** Le prompt assemblé devient une valeur **dérivée** des trois clauses figées, donc non persistée — ce qui aligne le candidat sur la règle qui gouverne déjà le gabarit.

Ce que cet écart coûte, et il faut le nommer : le §3.1 justifie `promptUtilise` par « comprendre a posteriori pourquoi un candidat diffère ». Une **copie littérale** de ce qui est parti sur le réseau est un meilleur enregistrement médico-légal qu'une reconstruction. La contrepartie est donc réelle, et elle impose une contrainte à T4 (C.5.5). Elle est acceptée parce que le désalignement est un mécanisme vivant, consulté à chaque écran, tandis que la relecture forensique d'un prompt est un usage rare et que la reconstruction reste exacte tant que la contrainte de T4 tient.

**Ces deux fonctions n'auront aucun appelant en T2, en dehors des tests, et c'est délibéré.** T2 ne génère rien : la clause de cadrage vit dans le template de workflow ComfyUI, que seul T4 sait lire. Les deux fonctions sont donc livrées inertes, et se relisent comme un oubli si l'on ne sait pas qu'elles attendent T4.

C'est le prix de l'option A dans sa forme étroite, et il est plus bas que celui des deux façons de le supprimer. Créer un port pour aller chercher la clause donnerait un appelant à `ResolvePrompt` — au prix d'une abstraction sans implémentation, dans un domaine qui ne référence rien. Persister le prompt assemblé donnerait un appelant à l'écrivain — au prix du critère d'acceptation de la tranche. Une fonction pure sans appelant ne coûte rien à l'exécution, rien à la maintenance, et se teste intégralement ; c'est le seul des trois coûts qui soit nul.

À la relecture, la tentation sera de les passer `internal` ou de les supprimer comme code mort. Elles restent **publiques et dans `Pawnsmith.Domain`**, parce que c'est là qu'elles vivront quand T4 les appellera, et que les déplacer alors serait un remaniement gratuit.

À consigner en **DEC-049**, qui supersède la forme du champ `promptUtilise` du §3.1 sans toucher à son intention.

### C.5.3 Règle d'assemblage

```
ResolvePrompt(framing, subject, style) =
    Join("\n", [Normalize(framing), Normalize(subject), Normalize(style)]
               .Where(c => c.Length > 0))
```

Avec `Normalize(c)` = remplacer `\r\n` et `\r` par `\n`, puis `Trim()`.

Quatre points, chacun pour une raison précise :

- **L'ordre est cadrage, sujet, style, et il est fixe.** C'est celui du §4.1 et de DEC-028. Le changer change tous les prompts.
- **Le séparateur est un `\n` unique, jamais `\r\n`.** Un séparateur dépendant de la plateforme ferait qu'un projet sauvegardé sous Windows et rouvert sous Linux produirait un prompt résolu différent, donc **un désalignement global et fantôme**. C'est le genre de défaut qui ne se voit ni au test ni à la relecture, et qui détruit la confiance dans le seul mécanisme de sécurité du produit.
- **Les clauses vides sont omises**, pas jointes. Un style vide ne doit pas produire un prompt se terminant par un saut de ligne. Conséquence assumée : l'assemblage n'est pas injectif — un style vide et un style absent donnent le même prompt. C'est correct : ils désignent la même chose.
- **La normalisation a lieu à l'assemblage ET à l'écriture.** Une clause stockée est déjà normalisée ; la validation de sauvegarde le vérifie. `Normalize` est idempotente, donc l'appliquer deux fois ne coûte rien et supprime la question de savoir où elle a lieu.

### C.5.4 Règle de comparaison

```
MisalignedClauses(candidate, blueprint, style, framingClause) =
    { Framing  si Normalize(candidate.framingClauseUsed) ≠ Normalize(framingClause) }
  ∪ { Subject  si Normalize(candidate.subjectClauseUsed) ≠ Normalize(blueprint.subjectClause) }
  ∪ { Style    si Normalize(candidate.styleClauseUsed)   ≠ Normalize(style.styleClause) }

IsMisaligned(...) = MisalignedClauses(...) ≠ ∅
```

**La comparaison est ordinale, sensible à la casse, caractère par caractère.** Aucune tolérance : ni insensibilité à la casse, ni écrasement des espaces multiples, ni normalisation Unicode.

Le raisonnement est celui d'un mécanisme de sécurité, et il a deux côtés. Une comparaison **trop tolérante** déclare « aligné » un candidat qui ne l'est pas : le mécanisme échoue silencieusement, exactement dans le sens dangereux. Une comparaison **trop stricte** crie au loup, et un avertissement qui se déclenche à tort finit par être ignoré, ce qui revient à ne pas l'avoir. La sortie de cette tension n'est pas une comparaison intelligente — c'est de **normaliser une seule fois, tôt, de manière visible**, et de comparer ensuite bêtement. C'est ce que fait `Normalize` à l'écriture.

> **`Trim()` est le seul assouplissement, et il est assumé.** Une clause qui ne diffère que par une espace finale est déclarée alignée. C'est correct : cette espace ne change rien au prompt envoyé, puisque l'assemblage la supprime aussi. Une différence de casse ou une espace **interne**, en revanche, désaligne — un modèle de diffusion n'y est pas indifférent.

### C.5.5 Ce que cela impose à T3 et T4

Deux contraintes, écrites ici pour qu'elles ne soient pas découvertes plus tard :

- **T4 envoie exactement `ResolvePrompt(framing, subject, style)`**, et fige les trois mêmes clauses sur le candidat. Si T4 transforme le prompt après assemblage — substitution de jeton supplémentaire, troncature, échappement — alors la reconstruction n'est plus fidèle et DEC-049 doit être rouverte. Ce n'est pas une interdiction de transformer : c'est l'obligation de le signaler.
- **Toute évolution de la règle d'assemblage désaligne tous les candidats de tous les projets, immédiatement.** L'ordre des clauses et le séparateur sont donc une **surface de compatibilité**, au même titre qu'un schéma. Les modifier demande une fiche DEC, pas un commit de confort.

### C.5.6 Ce que le désalignement n'est pas

Repris de DEC-030 parce que c'est l'erreur naturelle à cet endroit, et parce qu'un cahier des charges qui l'omet la laisse se produire :

- **`Misalignment` n'est pas une valeur de `status`.** Les deux axes sont indépendants. Un candidat `Valid` désaligné reste `Valid`. Le test n° 8 verrouille ce point.
- **Le calcul ne modifie rien.** Il ne réécrit pas le statut, ne dé-élit pas un candidat, ne touche pas au fichier.
- **Il n'existe pas de « réalignement partiel ».** Connaître l'ensemble des clauses désalignées sert à *expliquer*, jamais à recopier sélectivement une clause courante sur un candidat : un candidat est une image produite sous un prompt donné, et réécrire son prompt sans régénérer l'image est précisément le mensonge que DEC-030 empêche.
- **Changer `geometry`, `paperFormat` ou `quantity` ne désaligne rien** : ce sont des paramètres de rendu (DEC-030, DEC-004). Le test n° 9 le vérifie.
- **Ce que l'export fait d'un candidat élu mais désaligné n'est pas tranché**, et ne l'est pas ici : c'est un comportement d'export, donc T6, et la question C du chapitre 16 de la bible. La seule obligation que T2 doit tenir est que la réponse reste **calculable hors ligne** : le désalignement se détermine à partir du projet et de la clause de cadrage, sans contacter le générateur ni relire une image. Le schéma de C.3.4 le garantit.

---

## C.6 `versionSchema` et compatibilité

**Question tranchée : rejet, jamais de migration implicite.** T1 rejette (lecteur de manifeste, lecteur de calibration) et T2 fait de même — mais par un raisonnement propre, pas par mimétisme.

### C.6.1 Version plus récente que celle supportée

Rejet net, `PROJECT_SCHEMA_TOO_RECENT`, sans aucune lecture partielle.

Le motif n'est pas la prudence, c'est la **destruction de données**. Un lecteur v1 ouvrant un projet v2 ignorerait les champs qu'il ne connaît pas ; l'écrivain v1, lui, écrit ce que son type de document contient, c'est-à-dire *sans* ces champs. Ouvrir puis sauvegarder amputerait donc silencieusement le projet de tout ce que la v2 avait ajouté. Un aller-retour à travers un lecteur trop ancien est une perte, et elle est invisible jusqu'au moment où l'on rouvre le projet avec la version récente.

### C.6.2 Version plus ancienne

Il n'existe qu'une version. Le cas est donc vide en v1, et **rien n'est écrit pour lui** — pas de moteur de migration, pas d'interface `IProjectMigration`, pas de dossier `Migrations/`.

Ce qui est décidé, c'est la **politique** du jour où le cas se présentera : la migration sera une opération **explicite, demandée, versionnée pas à pas, et précédée d'une copie de sauvegarde du dossier**. Jamais une conversion silencieuse au chargement. Motif : une migration silencieuse réussit sur la machine du développeur et se découvre chez l'utilisateur, sur son unique copie.

### C.6.3 Champs inconnus à une version connue

**Rejet**, `PROJECT_INVALID`, en nommant le champ (`JsonUnmappedMemberHandling.Disallow`).

C'est l'inverse de ce que fait la calibration, et la différence est intentionnelle :

| Fichier | Écrit par | Champ inconnu | Motif |
|---|---|---|---|
| `calibration.json` | L'utilisateur, à la main | **Accepté et ignoré** (bloc `paper`, §B.2) | C'est un fichier de réglage qu'on annote ; le bloc `paper` existe précisément pour porter une information que le code ne lit pas |
| `project.json` | L'application, exclusivement | **Rejeté** | Un champ inconnu à une version connue signifie soit une corruption, soit une modification manuelle dont l'intention ne peut pas être honorée — et l'ignorer le ferait disparaître à la sauvegarde suivante |

La règle qui découle des deux lignes ci-dessus, et qu'il faut écrire une fois : **tout ajout de champ à `project.json` incrémente `versionSchema`.** Il n'existe pas d'ajout « compatible ».

À consigner en **DEC-048**.

---

## C.7 Chargement et sauvegarde

### C.7.1 Ordre des opérations au chargement

L'ordre n'est pas une préférence de style : chaque étape suppose que la précédente a réussi, et une inversion ouvre une surface.

1. Vérifier que le chemin du dossier est **sous la racine des projets**, après résolution.
2. Lire `project.json`. Refuser un fichier dépassant une taille plafond (C.9.3) **avant** de le désérialiser.
3. Désérialiser en `ProjectDocument`, membres inconnus interdits.
4. Vérifier `versionSchema` (C.6).
5. Valider **structurellement** : champs obligatoires, formats d'UUID, énumérations connues, `quantity ≥ 1`, `electedCandidateId` existant **dans ce gabarit**, horodatages analysables.
6. Valider **les chemins** de tous les fichiers d'image (C.3.5). Aucun accès disque avant cette étape.
7. Valider **intrinsèquement** les surcharges : nombres finis et strictement positifs (C.4.4).
8. Construire les entités de domaine (`ToDomain()`).
9. Collecter les **diagnostics**, qui n'échouent pas le chargement : fichiers référencés absents du disque, `paperFormat` inconnu de la calibration, surcharge incompatible avec la calibration locale — ce dernier en géométrie `TabAndSocket` seulement (C.4.4).

Les étapes 1 à 7 échouent le chargement. L'étape 9 ne l'échoue jamais, et la distinction entre les deux est celle de DEC-056 : ce que le fichier contredit tout seul bloque, ce que seule la machine locale contredit informe.

### C.7.2 Pourquoi un fichier d'image manquant n'échoue pas le chargement

C'est un écart au manifeste de T1 (§B.3, « fichiers image présents et lisibles » est une condition de validité) et il est délibéré. Les deux fichiers n'ont pas le même contrat :

- le **manifeste** est l'entrée d'un rendu ; une image manquante rend le rendu impossible, donc l'échec est la bonne réponse, immédiatement ;
- le **projet** est un espace de travail ; une image manquante rend un candidat inutilisable, pas le projet. Refuser d'ouvrir le projet le rendrait **irréparable**, puisque la seule façon de retirer le candidat fautif serait d'éditer le JSON à la main.

Le chargement rend donc la liste des fichiers manquants à l'appelant, qui en fait ce qu'il veut — un avertissement en T6, une erreur au moment d'exporter une planche.

### C.7.3 Sauvegarde

1. Valider le projet **avant d'écrire quoi que ce soit** : mêmes règles qu'aux étapes 5 à 7 du chargement. Une entité invalide n'est jamais sérialisée.
2. Vérifier que toutes les clauses stockées sont normalisées (C.5.3).
3. Mettre `modifiedAt` à l'instant courant, en UTC.
4. Sérialiser vers un fichier temporaire, **dans le dossier du projet**.
5. Remplacer atomiquement `project.json` par ce fichier.

Pas de fichier `.bak`, pas d'historique de versions : la sauvegarde de l'utilisateur, c'est l'archive (C.8), et ajouter une seconde forme de sauvegarde en concurrence avec elle produirait deux mécanismes à moitié fiables au lieu d'un.

> **Pas de verrou.** Deux écritures simultanées du même projet ne sont pas gérées en T2, parce qu'il n'y a en T2 aucun appelant concurrent : le seul consommateur est un test ou un point d'entrée en ligne de commande. La concurrence apparaît avec l'API et deux onglets de navigateur, c'est-à-dire en T6. À traiter là-bas, pas ici, et à ne pas oublier : le remplacement atomique protège de la **corruption**, pas de la **perte** — le dernier écrivain gagne.

---

## C.8 Export d'archive

### C.8.1 Deux profils, et pourquoi ce n'est pas une complication gratuite

MEN-006 interdit les secrets et les journaux. La question posée porte sur ce que la menace ne couvre pas : `exports/` et les images jumelées brutes.

| | Images jumelées | `exports/*.pdf` |
|---|---|---|
| Poids | 1 à 2 Mo par candidat, **candidats rejetés compris** | Quelques centaines de ko à quelques Mo |
| Reproductible ? | Non — c'est la sortie brute d'une génération non déterministe à l'identique | **Oui**, entièrement, à partir du projet et de la calibration |
| Utilité pour un tiers | Nulle : le §4 les conserve « pour diagnostic » | Nulle : il les régénérera |
| Utilité pour soi | Réelle : diagnostiquer une découpe ou un détourage raté | Faible |

Deux usages différents, donc deux profils :

| Profil | Usage | Contenu |
|---|---|---|
| `Backup` | Se sauvegarder soi-même, déménager de machine | Tout ce que la liste blanche autorise, `exports/` et jumelées comprises |
| `Share` | Envoyer le projet à quelqu'un | `project.json` filtré, images détourées des candidats conservés, rien d'autre |

**Le profil `Share` filtre `project.json`, il ne se contente pas d'omettre des fichiers.** C'est le point qui mérite d'être écrit : omettre les fichiers en laissant les références produirait une archive dont le projet pointe vers des images absentes, donc un état « à moitié cassé » que l'import devrait tolérer, donc une tolérance qui se propagerait ensuite partout. L'invariant est plus simple et plus fort :

> **Une archive contient toujours un `project.json` cohérent avec les fichiers qu'elle contient. Jamais de référence pendante, dans aucun profil.**

Règle de filtrage du profil `Share` :

- tous les gabarits sont conservés, y compris ceux sans candidat ;
- les candidats de statut `Rejected` sont retirés, ainsi que leurs fichiers ;
- les candidats `Draft` et `Valid` sont conservés — un projet partagé en cours d'arbitrage a besoin de ses brouillons, c'est même souvent la raison du partage ;
- `pairedImageFile` est mis à `null` sur tous les candidats conservés, et les fichiers correspondants sont exclus ;
- `exports/` est exclu en entier ;
- si `electedCandidateId` désignait un candidat retiré — cas impossible sous les règles de gestion attendues, mais le fichier peut avoir été édité — l'export **échoue** plutôt que de produire une référence pendante.

**Conséquence sur le critère d'acceptation de T2.** « Aller-retour export/import sans perte » vaut pour le profil `Backup`, intégralement et à l'octet près. Pour `Share`, le critère devient : aller-retour sans perte de ce qui n'a pas été délibérément retiré, et projet importé **cohérent et rendable**.

### C.8.2 Le contenu se décide par liste blanche

**L'archive n'est jamais « le dossier zippé ».** Elle est construite en énumérant ce qui est autorisé :

| Entrée | `Backup` | `Share` |
|---|---|---|
| `archive.json` | oui | oui |
| `project.json` | tel quel | filtré |
| `images/*.png` référencé par le `project.json` de l'archive | oui | oui |
| `images/*` non référencé | **non** | non |
| `exports/*.pdf` | oui | non |
| Tout le reste | **non** | **non** |

Le motif est directement MEN-006, et il transforme une bonne pratique en propriété structurelle. Une exportation par copie du dossier emporterait ce que l'utilisateur y aura déposé : un `.env` égaré, un `notes.txt`, un `.git/` complet avec l'historique, un dossier `logs/` créé à la main malgré DEC-022. La liste blanche rend le test de MEN-006 **exhaustif au lieu d'être exemplaire** : on ne vérifie plus qu'un secret nommé est absent, on vérifie qu'aucune entrée hors liste n'est présente.

Le rejet des images non référencées ferme au passage un chemin de contamination discret : un fichier orphelin dans `images/` — reste d'un candidat supprimé, ou fichier déposé là — n'a aucune raison de voyager.

### C.8.3 `archive.json`

```json
{
  "archiveVersion": 1,
  "profile": "Share",
  "createdAt": "2026-09-01T16:04:12Z",
  "producedBy": "Pawnsmith 0.3.0"
}
```

Quatre champs, et il faut justifier chacun d'exister plutôt que de laisser `project.json` faire seul le travail :

- **`archiveVersion`** permet à l'import de rejeter un ZIP quelconque **avant d'extraire quoi que ce soit**, ce que MEN-001 exige (valider avant d'écrire) ;
- **`profile`** dit au destinataire ce qu'il tient. Sans lui, on ne peut pas distinguer un `Share` d'un `Backup` dont l'auteur n'avait jamais généré de PDF ni conservé de jumelée. La différence compte : réexporter en `Backup` un projet issu d'un `Share` produirait une archive qui prétend être complète et ne l'est pas ;
- **`createdAt`** et **`producedBy`** sont l'information minimale d'un support de sauvegarde. Elles ne dupliquent rien de `project.json`. `producedBy` est lu **par réflexion sur l'assembly**, jamais écrit en littéral : c'est le seul endroit qui ne peut pas diverger du binaire réellement produit. Le numéro vient de `<Version>` dans `Directory.Build.props` (DEC-058), et la révision de source doit être exclue de la chaîne — le suffixe `+3f9a1c…` que le SDK ajoute par défaut désignerait un commit dans un fichier destiné à être lu par un humain.

`archive.json` ne contient **ni `projectId` ni `name`** : ce serait une duplication, donc une occasion de divergence.

### C.8.4 Ce que l'export refuse

- **Un lien symbolique**, où que ce soit dans le dossier du projet : l'export échoue, il ne l'ignore pas. Un lien vers `/etc` ou vers le dossier des journaux transforme l'export en exfiltration, avec le consentement apparent de l'utilisateur puisqu'il a lui-même demandé le partage. C'est le jumeau exact de MEN-001, du côté sortant, et il n'est écrit nulle part (C.10, **MEN-008**).
- **Un fichier référencé mais absent du disque** : l'archive serait incohérente. L'export échoue en nommant le fichier. C'est l'endroit où le diagnostic « toléré » du chargement (C.7.2) devient bloquant, et c'est cohérent : ouvrir un projet troué est acceptable, en distribuer un ne l'est pas.
- **Une écriture de l'archive à l'intérieur d'un dossier de projet** : refusée. Sinon la sauvegarde suivante contiendrait la précédente, puis les deux, et ainsi de suite.

### C.8.5 Forme de l'archive

- Format **ZIP** nu, extension `.zip`. Pas d'extension propriétaire : la portabilité de DEC-011 suppose que n'importe qui puisse ouvrir l'archive avec l'outil de son système, y compris dans dix ans sans Pawnsmith.
- Entrées en **chemins relatifs POSIX**, sans dossier racine englobant. La destination est choisie à l'import, pas subie ; et l'ambiguïté classique « un dossier racine ou plusieurs » disparaît.
- `archive.json` est la **première entrée**, pour être lisible en flux sans parcourir l'archive entière.
- Nom de fichier proposé : `{nomDeDossier}-{profil}-{aaaammjjHHmm}.zip`, en minuscules. Le nom est une commodité ; il ne porte aucune information que l'archive ne contient pas.
- Aucun chiffrement, aucun mot de passe : MEN-006 rend l'archive partageable par construction, pas par protection.

---

## C.9 Import d'archive

### C.9.1 Ordre de validation

**Tout est validé avant qu'un seul octet ne soit écrit à la destination finale.** L'ordre suit MEN-001 et il n'est pas réarrangeable.

1. Ouvrir l'archive en lecture, sans extraire.
2. Vérifier les **bornes de ressources** (C.9.3) : nombre d'entrées, taille décompressée totale annoncée, ratio de compression, profondeur et longueur des chemins.
3. Lire `archive.json` et vérifier `archiveVersion`.
4. Parcourir l'inventaire des entrées et **rejeter globalement** l'archive si l'une d'elles :
   - est un chemin absolu, contient une lettre de lecteur, un préfixe UNC ou un séparateur `\` ;
   - contient un segment `..` ou `.` ;
   - n'est pas une entrée de fichier ordinaire (lien symbolique, périphérique, entrée de dossier avec attributs inattendus) ;
   - est chiffrée ;
   - **duplique le nom d'une autre entrée** — c'est le contournement classique : le validateur voit la première, l'extracteur écrit la seconde ;
   - **entre en collision de casse** avec une autre (`a.png` et `A.png`) — inoffensif sous Linux, écrasement silencieux sous Windows et macOS ;
   - n'appartient pas à la liste blanche de C.8.2.
5. Lire `project.json` **depuis l'archive, en mémoire**, et lui appliquer les validations **bloquantes** de C.7.1, étapes 3 à 7, `versionSchema` compris. Les diagnostics de l'étape 9 sont collectés et rendus à l'appelant, mais **ne font pas échouer l'import** : une archive cohérente produite sur une autre calibration s'importe (DEC-056).
6. Vérifier la **cohérence interne** : tout fichier référencé par le `project.json` de l'archive est présent dans l'archive, et tout fichier d'image présent est référencé.
7. Extraire dans un dossier **temporaire, sur le même volume que la destination**. Pour chaque entrée, résoudre le chemin absolu et vérifier qu'il est préfixé par celui du dossier temporaire — la vérification d'inventaire de l'étape 4 ne dispense pas de celle-ci, elle la double.
8. Renommer le dossier temporaire vers la destination.

**Le rejet est global** (MEN-001 : « rejet global de l'archive sinon »). Une archive n'est jamais importée partiellement, et un échec à n'importe quelle étape supprime le dossier temporaire. La destination n'est créée qu'à l'étape 8, ce qui rend l'import atomique du point de vue de l'utilisateur : ou bien le projet est là et complet, ou bien il n'y a rien.

### C.9.2 Destination et collisions

| Cas | Comportement |
|---|---|
| Le dossier de destination **existe** (même vide) | **Rejet**, `IMPORT_DESTINATION_EXISTS`. Le dossier n'est pas touché |
| La destination est **hors de la racine des projets** | Rejet |
| Un projet portant le **même `projectId`** existe déjà ailleurs | **Import accepté**, `projectId` **préservé** |
| L'archive porte un **`versionSchema` plus récent** | Rejet avant extraction, `PROJECT_SCHEMA_TOO_RECENT` |

**Jamais de fusion.** Fusionner deux dossiers de projet — que faire de deux gabarits de même identifiant aux clauses différentes ? de deux candidats élus ? — est une machine à perdre des données, et ce serait décider en silence de règles de gestion que personne n'a écrites. Le refus est franc et l'appelant propose un autre nom ; en T2 il n'y a pas d'appelant humain, donc pas de dialogue à écrire.

**Le `projectId` est préservé, pas régénéré.** Le cas dominant est la restauration de sa propre sauvegarde, où changer l'identité serait faux. Le cas du doublon est réel mais c'est une **copie**, ce qui n'est pas une corruption : deux dossiers, deux projets utilisables. Trancher entre « remplacer » et « garder les deux » demande de poser la question à quelqu'un, donc T6.

### C.9.3 Bornes de ressources

MEN-005 borne les images ; l'archive est une **seconde surface de décompression** que rien ne bornait. Ces valeurs ne sont pas des mesures physiques : elles s'arbitrent, et les fixer maintenant est légitime — il serait faux de les marquer `À CALIBRER`.

**Où elles vivent (DEC-057) :** un record d'options passé en paramètre au dépôt, dont les valeurs par défaut sont déclarées en un seul endroit nommé. Pas de fichier. `calibration.json` est réservé aux valeurs physiques, et T2 n'a ni API ni hôte pour lire un fichier de configuration — en créer un aujourd'hui reviendrait à écrire un fichier que rien ne lit. Le fichier arrive en T6.

| Borne | Valeur proposée | Motif |
|---|---|---|
| Nombre d'entrées | 10 000 | Un projet de 200 candidats en fait 600 |
| Taille décompressée totale | 4 Gio | Une sauvegarde `Backup` d'un gros projet peut être volumineuse ; c'est une borne de sécurité, pas de confort |
| Ratio de compression, global et par entrée | 100:1 | Un PNG ne se comprime pas ; un ratio élevé signale une bombe |
| Taille de `project.json` | 32 Mio | Lu en mémoire avant validation |
| Profondeur de chemin | 3 segments | La liste blanche n'en autorise que deux |
| Longueur d'un chemin d'entrée | 255 caractères | |

Le dépassement d'une borne est un rejet global, avec un code d'erreur distinct de `ARCHIVE_REJECTED` pour que la cause soit lisible.

---

## C.10 Deux menaces nouvelles

Le chapitre 9 de la bible déduit les menaces de l'architecture. T2 introduit deux surfaces qu'il ne couvre pas. À ajouter au tableau du chapitre 9.

| Réf. | Menace | Vecteur | Contre-mesure |
|---|---|---|---|
| **MEN-008** | **Exfiltration par lien symbolique à l'export** | Un lien symbolique déposé dans le dossier du projet, pointant hors du dossier — vers le volume des journaux, vers `/etc`, vers un dossier personnel. L'export le suit et le met dans une archive que l'utilisateur envoie lui-même | Ne jamais suivre un lien ; l'export **échoue** en le nommant plutôt que de l'ignorer silencieusement. Doublé par la liste blanche de C.8.2, qui n'autorise que des `.png` et des `.pdf` référencés |
| **MEN-009** | **Traversée de chemin par le nom de projet** | `name` est une chaîne libre venue de l'utilisateur ou d'une archive tierce, et sert à fabriquer un nom de dossier. `../../logs` en est un | Translittération vers une liste blanche de caractères (C.3.2), noms réservés exclus, et vérification que le chemin résolu est sous la racine des projets **avant** création. Jamais de concaténation directe, comme MEN-002 l'exige déjà pour les journaux |

> MEN-008 mérite un mot de plus. C'est le miroir exact de MEN-001 : le zip slip fait entrer un fichier là où il ne devrait pas, le lien symbolique fait **sortir** un fichier de là où il devrait rester. Les deux se traitent par la même primitive — résoudre le chemin absolu et vérifier le préfixe — et il serait dommage de n'en implémenter qu'une moitié.

---

## C.11 Codes d'erreur

Le chapitre 10 impose que l'API renvoie des codes, jamais du texte traduit. T2 n'a pas d'API, mais les erreurs naissent ici : elles portent un code dès maintenant, sinon T6 devra les inventer après coup à partir de messages.

| Code | Situation |
|---|---|
| `PROJECT_SCHEMA_TOO_RECENT` | `versionSchema` supérieur à la version supportée |
| `PROJECT_INVALID` | Champ obligatoire absent, champ inconnu, énumération inconnue, `quantity < 1`, `electedCandidateId` inconnu ou étranger au gabarit, horodatage illisible |
| `PROJECT_PATH_ESCAPE` | Chemin d'image absolu, hors `images/`, contenant `..` ou `\` |
| `PROJECT_OVERRIDE_INVALID` | Surcharge de calibration **intrinsèquement** fausse : nulle, négative, infinie, `NaN`, non numérique. Une surcharge valide mais incompatible avec la calibration locale n'est pas une erreur, c'est un diagnostic (DEC-056) |
| `PROJECT_NOT_FOUND` | Dossier ou `project.json` absent |
| `PROJECT_TOO_LARGE` | `project.json` dépassant la borne de C.9.3 |
| `ARCHIVE_REJECTED` | Toute violation de C.9.1 étapes 3 à 6 — le code est volontairement unique, une archive rejetée ne détaille pas ce qui l'a trahie |
| `ARCHIVE_LIMIT_EXCEEDED` | Dépassement d'une borne de ressources (C.9.3) |
| `ARCHIVE_EXPORT_FAILED` | Lien symbolique rencontré, fichier référencé absent, destination interdite |
| `IMPORT_DESTINATION_EXISTS` | Le dossier de destination existe déjà |

> **`ARCHIVE_REJECTED` ne dit pas pourquoi, et c'est délibéré.** Le message de journal, lui, est précis (chapitre 8). Détailler à l'appelant qu'une entrée précise a échoué au contrôle de casse est une information utile à qui construit l'archive malveillante et à personne d'autre. Ce raisonnement ne vaut **que** pour l'archive, dont le contenu peut être hostile : `PROJECT_INVALID` nomme le champ fautif, parce qu'un projet mal formé est presque toujours le sien.

> **Un diagnostic n'est pas un code d'erreur.** Les diagnostics de C.7.1 étape 9 sont rendus avec le projet chargé, pas à la place de celui-ci. Ils auront besoin d'une représentation propre en T6 — ce n'est pas la même chose qu'un échec, et les faire transiter par la liste ci-dessus les transformerait en échecs.

---

## C.12 Tests attendus

Sur le modèle du §B.8 de T1 : numérotés, pour que « terminé » se vérifie en cochant.

### Domaine — tests unitaires, sans mock, sans système de fichiers

1. Assemblage : ordre cadrage → sujet → style, séparateur `\n` unique, clauses `Trim`ées.
2. Assemblage : une clause vide est omise, et ne laisse ni séparateur en trop ni espace de tête.
3. Assemblage : des clauses contenant `\r\n` produisent le même résultat que les mêmes clauses en `\n`.
4. Désalignement faux quand les trois clauses figées égalent les trois clauses courantes.
5. Désalignement vrai sur une clause style différente, et l'ensemble rendu contient **exactement** `Style`.
6. Idem pour la clause sujet.
7. Idem pour la clause cadrage — c'est le cas de l'utilisateur qui édite son template de workflow (DEC-029).
8. **Un candidat `Valid` désaligné reste `Valid`** : le calcul ne modifie aucun statut. C'est le piège nommé par DEC-030.
9. **Changer `geometry`, `paperFormat` ou `quantity` ne désaligne aucun candidat.**
10. Deux clauses ne différant que par une espace finale sont alignées ; une différence de casse ou une espace interne désaligne.
11. Résolution des surcharges : membre `null` → valeur de calibration ; membre renseigné → valeur du projet ; le reste de la calibration est inchangé.
12. Une surcharge intrinsèquement fausse — nulle, négative, `NaN` — est rejetée par `PROJECT_OVERRIDE_INVALID` en nommant la valeur, à la sauvegarde comme au chargement.
13. Une surcharge `tabHeightMm` modifie la capacité de page calculée par le moteur de T1 pour la même taille et le même format — **verrouille le couplage annoncé par DEC-040**.
14. Le nom de dossier dérivé d'un `name` contenant `../`, des caractères non ASCII, un nom réservé Windows et une longueur excessive est sûr, non vide, et borné (MEN-009).

### Infrastructure — sérialisation

15. Aller-retour domaine → document → JSON → document → domaine : égalité par valeur sur l'intégralité du modèle, candidats et surcharges compris.
16. **Le JSON produit ne contient aucune clé `resolvedPrompt` ni `misaligned`** — assertion textuelle sur le fichier. Critère d'acceptation de T2.
17. Écriture sous `CurrentCulture` forcée à `fr-FR` : les décimales sont écrites avec un point et relues correctement.
18. UTF-8 sans BOM ; un `name` accentué et un `details` contenant des emoji survivent à l'aller-retour.
19. Deux sauvegardes successives d'un projet inchangé produisent des fichiers identiques octet pour octet, `modifiedAt` mis à part — y compris lorsque le projet porte un `optionalParameters` non vide construit dans un ordre d'insertion différent.
20. `seed` est écrit comme une chaîne, et `18446744073709551615` est relu sans perte.
21. Horodatages écrits en UTC avec suffixe `Z` et relus identiques avec un fuseau de processus décalé.
22. Écriture atomique : un échec simulé pendant l'écriture laisse le `project.json` précédent intact et lisible.
23. L'ordre des gabarits et celui des candidats sont préservés à l'aller-retour, sans tri.
54. Les clés d'`optionalParameters` sont écrites triées en ordinal, quel que soit leur ordre d'insertion et quelle que soit la `CurrentCulture` du processus ; et les caractères non ASCII sont écrits littéralement, non échappés.

### Infrastructure — validation au chargement

24. `versionSchema` à `2` → `PROJECT_SCHEMA_TOO_RECENT`, aucune écriture, aucune lecture partielle.
25. Champ inconnu à `versionSchema` 1 → `PROJECT_INVALID` nommant le champ.
26. Champ obligatoire absent → `PROJECT_INVALID` nommant le champ.
27. `quantity` à `0` → rejet.
28. `electedCandidateId` inconnu, ou désignant un candidat d'un **autre** gabarit → rejet.
29. Chemin d'image absolu, contenant `..`, contenant `\`, ou hors de `images/` → `PROJECT_PATH_ESCAPE`, **et aucun fichier n'a été ouvert**.
30. Un fichier d'image référencé mais absent du disque → le chargement **réussit** et le rapporte en diagnostic (C.7.2).
31. Un `paperFormat` absent de la calibration → chargement réussi, diagnostic émis.
32. Un dossier inconnu (`notes/`) dans le dossier du projet n'empêche pas le chargement.
53. Un projet en `TabAndSocket` dont la surcharge `tabWidthMm` dépasse le `pawnWidthMm` d'une taille utilisée **se charge**, émet un diagnostic le nommant, et **s'importe** depuis une archive (DEC-056). Le **calcul de la planche** à partir de ce projet lève ensuite avec le message de DEC-038 — c'est le contour de découpe qui refuse, et non la construction de la calibration effective, qui ne valide rien. Le même projet en `NoSupport` se charge **sans aucun diagnostic** (C.4.4).

### Infrastructure — export

33. Profil `Backup` : l'archive contient `exports/`, les images jumelées, et tous les candidats, `Rejected` compris.
34. Profil `Share` : ni `exports/`, ni image jumelée ; `pairedImageFile` vaut `null` pour tous les candidats du `project.json` de l'archive.
35. Profil `Share` : les candidats `Rejected` et leurs fichiers sont absents, les `Draft` et `Valid` sont présents, et **aucune référence n'est pendante**.
36. **MEN-006** : un `secret.env`, un dossier `logs/`, un `.git/` et une image orpheline non référencée sont déposés dans le dossier du projet ; **aucun des deux profils ne les emporte**. Le test énumère les entrées de l'archive et vérifie qu'elles appartiennent toutes à la liste blanche — il ne cherche pas un nom de fichier connu.
37. **MEN-008** : un lien symbolique dans le dossier du projet fait échouer l'export avec `ARCHIVE_EXPORT_FAILED` ; aucune archive partielle n'est laissée sur le disque.
38. Un fichier référencé mais absent du disque fait échouer l'export en le nommant.
39. Toutes les entrées d'une archive sont des chemins relatifs POSIX, sans `..`, sans doublon, et `archive.json` est la première.
40. Écrire une archive dans un dossier de projet est refusé.

### Infrastructure — import

41. **Aller-retour `Backup`** : export puis import produit un projet égal au projet source, fichiers compris, comparés par empreinte. *Critère d'acceptation de T2.*
42. **Aller-retour `Share`** : le projet importé est égal au source privé des éléments délibérément retirés, et reste cohérent.
43. **MEN-001** : une entrée `../../evil.txt` → `ARCHIVE_REJECTED`, et **aucun fichier n'est écrit**, ni dans la destination ni ailleurs.
44. Entrée en chemin absolu, avec lettre de lecteur, ou avec séparateur `\` → rejet global.
45. Deux entrées de même nom → rejet global. Deux entrées différant seulement par la casse → rejet global.
46. Entrée chiffrée → rejet global.
47. Archive dépassant le nombre d'entrées, la taille décompressée ou le ratio de compression → `ARCHIVE_LIMIT_EXCEEDED`, sans écriture.
48. `versionSchema` plus récent dans l'archive → rejet **avant** extraction.
49. `project.json` de l'archive référençant un fichier absent de l'archive → rejet. Fichier présent dans l'archive et non référencé → rejet.
50. Destination existante, même vide → `IMPORT_DESTINATION_EXISTS`, dossier inchangé.
51. Échec en cours d'extraction → aucune destination créée, dossier temporaire supprimé.
52. Import d'une archive dont le `projectId` existe déjà dans un autre dossier → succès, `projectId` préservé, les deux projets se chargent.

> **Les tests 53 et 54 sont numérotés à la suite plutôt qu'insérés**, pour ne décaler aucun numéro déjà cité ailleurs. Le test 53 appartient à « validation au chargement », le 54 à « sérialisation » ; ils sont écrits dans leur section, avec leur numéro.

> **Aucun test n'est ajouté pour le CLI de C.17.** Comme celui de B.7, il est jetable, non livré et sans tests. Ce qu'il produit est vérifié par les tests du dépôt qu'il appelle.

---

## C.13 Critères d'acceptation

T2 est terminée quand :

- [ ] Les **54** tests de C.12 passent.
- [ ] Aller-retour export/import en profil `Backup` **sans perte**, vérifié par empreinte de fichier.
- [ ] Aller-retour en profil `Share` **cohérent** : aucune référence pendante, projet rendable.
- [ ] **MEN-001** couvert par un test d'archive malveillante (tests 43 à 46).
- [ ] **MEN-006** couvert par un test **exhaustif** sur la liste blanche, pas par la recherche d'un nom de fichier (test 36).
- [ ] **MEN-008** et **MEN-009** couverts (tests 37 et 14), et ajoutés au chapitre 9 de la bible.
- [ ] `versionSchema` présent, et un schéma plus récent rejeté sans lecture partielle.
- [ ] **Aucune valeur dérivée n'est sérialisée** : `resolvedPrompt` et `misaligned` sont absents du fichier **et** absents des types de document.
- [ ] Aucun attribut de sérialisation sur un type de `Pawnsmith.Domain`, et `Pawnsmith.Domain.csproj` ne référence toujours rien.
- [ ] Le mapping domaine ↔ document est manuel et explicite dans les deux sens (DEC-021).
- [ ] Aucune valeur de calibration en dur ; les bornes de C.9.3 sont des **paramètres**, leurs valeurs par défaut sont déclarées en un seul endroit, et **aucune n'apparaît en littéral dans le code qui l'applique** (DEC-057).
- [ ] `Directory.Build.props` porte `<Version>0.3.0</Version>`, et `producedBy` est lu par réflexion, sans révision de source (DEC-058).
- [ ] Le CLI de C.17 produit un projet, l'exporte dans les deux profils et le réimporte, **sans contenir de logique** (DEC-059).
- [ ] Aucun champ de projet n'est verrouillé à la sauvegarde, et `SaveAsync` n'a pas reçu de paramètre d'état antérieur (DEC-055).
- [ ] Les fiches DEC de C.15 sont écrites dans le chapitre 11 de la bible.
- [ ] L'intégration continue est verte.
- [ ] Le code est relu intégralement (DEC-027).

---

## C.14 Ce que T2 ne fait pas

Chaque ligne découle d'une décision déjà prise, et chacune est naturelle à ajouter.

| T2… | Parce que |
|---|---|
| …n'expose aucun point de terminaison | T6 |
| …ne compose aucune clause sujet | T3. T2 stocke la clause, il ne sait pas la fabriquer |
| …ne définit aucune entité `Catalog` | T3, et la question D n'est pas tranchée |
| …n'écrit aucun moteur de migration | Une seule version de schéma (C.6.2) |
| …ne définit aucun port de clause de cadrage | T4. En T2 c'est un paramètre de fonction (C.5.2) |
| …ne gère aucune concurrence | Aucun appelant concurrent avant T6 |
| …ne tranche pas le sort d'un candidat élu mais désaligné à l'export | T6, question C. T2 garantit seulement que la réponse est calculable (C.5.6) |
| …ne tranche pas ce que devient l'ancien élu quand on en élit un nouveau | T3, question B. T2 vérifie l'intégrité référentielle, pas la règle de gestion |
| …ne modifie pas une ligne de T1 | La calibration effective se résout en amont (C.4.3) |
| …ne crée aucun fichier de configuration | DEC-057. Le fichier arrive avec l'hôte qui le lit, en T6 |
| …ne verrouille aucun champ après création | DEC-030, confirmé par DEC-055. Une règle de transition appartiendrait à un cas d'usage, pas au dépôt |
| …ne rejette aucun projet au motif de la calibration locale | DEC-056 |

---

## C.15 Décisions à consigner dans la bible

Neuf fiches, à écrire au chapitre 11 avant l'implémentation. Trois demandent un arbitrage explicite du porteur : elles sont marquées ⚠.

| Réf. | Objet | Section |
|---|---|---|
| **DEC-046** | Le fichier projet s'appelle `project.json` ; les noms de fichiers suivent DEC-037 comme les clés | C.3.1 |
| **DEC-047** | L'identité d'un projet est un `projectId` opaque ; le nom du dossier n'a aucune sémantique ; l'unicité n'est pas contrôlée en v1 | C.3.2 |
| **DEC-048** | `versionSchema` inconnu : rejet, jamais de migration implicite. Champ inconnu rejeté dans `project.json`, à l'inverse de `calibration.json` | C.6 |
| ⚠ **DEC-049** | Le candidat fige ses **trois clauses**, pas le prompt assemblé. **Supersède la forme du champ `promptUtilise` du §3.1** | C.5.2 |
| **DEC-050** | Deux profils d'archive, `Backup` et `Share` ; contenu décidé par liste blanche ; cohérence interne comme invariant | C.8 |
| **DEC-051** | Import atomique, rejet global, refus si la destination existe, jamais de fusion, `projectId` préservé | C.9 |
| ⚠ **DEC-052** | `gutterMm` **reste dans la calibration**. **Ferme la question ouverte du §15.6** | C.4.2 |
| **DEC-053** | Surcharges de projet : liste close à `tabWidthMm` et `tabHeightMm`, membres nullables explicites, résolution unique en Application | C.4 |
| ⚠ **MEN-008 / MEN-009** | Deux menaces à ajouter au chapitre 9 : exfiltration par lien symbolique à l'export, traversée de chemin par le nom de projet | C.10 |

Cinq fiches supplémentaires sont issues de la revue de la v1.0 : **DEC-055** à **DEC-059**. **DEC-056 supersède la clause de double validation de DEC-053**, qui n'aura jamais été en vigueur : les deux fiches se déposent au chapitre 11 dans le même mouvement.

---

## C.16 Contradictions relevées dans les documents

Signalées plutôt que corrigées en silence, comme les instructions du projet l'exigent. Aucune n'empêche de démarrer T2.

1. **`Geometry` a trois valeurs, le §3.1 en liste deux.** DEC-039 a ajouté `NoSupport` ; le tableau des entités n'a pas suivi. Le schéma de T2 en accepte trois, ce qui est la lecture correcte, mais la bible doit être corrigée.

2. **Le §0 du cahier des charges T1 contredit DEC-037 dans le même document.** Il écrit encore : « Le vocabulaire du chapitre 2 de la bible est contraignant : `Gabarit`, `Candidat`, `Planche`, `Taille`, `Geometrie`. Il est repris tel quel dans les noms de types. » DEC-037 supersède exactement cette clause. Le changelog de la v1.4 dit que le renommage a été appliqué « dans le texte comme dans les blocs JSON du §B.2 et du §B.3 » — le §0 a été oublié. C'est la contradiction la plus coûteuse de la liste, puisque c'est la première chose que lit un assistant de code.

3. **Les instructions du projet reprennent la même clause périmée** : « Vocabulaire contraignant, repris tel quel dans le code : Gabarit, Candidat, Planche, Taille, Géométrie, Style, Job. » À reformuler en « contraignant comme concept, table de correspondance de DEC-037 pour la graphie ».

4. **Version du cahier des charges T1.** La consigne de séance annonce « v1.3 dans `docs/` » ; la base de connaissance porte la **v1.4**. Si `docs/` est réellement en v1.3, le miroir est périmé d'une version.

5. **L'en-tête du cahier des charges T1 v1.4 annonce « Document parent : `pawnsmith-bible.md` v0.3 »** alors que la bible est en v0.9. Champ non mis à jour depuis six révisions.

6. **`pawnsmith-etat-du-projet.md` est périmé de manière significative.** Il date du 29 août, annonce bible v0.3 et cahier des charges v1.2, donne T0a comme « à faire maintenant » alors que DEC-043 la déclare menée et concluante, T1 comme « non démarrée » alors qu'elle est écrite, testée et poussée, et T2 comme « non spécifiée ». Il liste par ailleurs le générateur comme « FLUX Krea », que DEC-043 corrige explicitement en Krea 2 Turbo. C'est le document dont les instructions du projet disent qu'il doit être mis à jour « à chaque séance » ; il l'est le moins. **Traité à la fusion de la v1.1** : son §4 devient le chapitre 16 de la bible, seule partie qui n'existait nulle part ailleurs, et le document est supprimé.

7. **Le §15.1 de la bible verrouille quatre champs que DEC-030 a rendus modifiables.** Il écrit « univers, style, géométrie, format de papier : les quatre derniers sont verrouillés après création (DEC-001, DEC-006, DEC-025) », en citant une fiche partiellement supersédée sur ce point, une fiche dont DEC-030 dit explicitement qu'elle ne pose plus ce verrou, et une fiche qui n'en a jamais posé — DEC-025 dit seulement que le champ existe. Traité par DEC-055.

8. **Le §15.3 de la bible affirme que « changer de style implique de dupliquer le projet ».** C'est le régime d'avant DEC-030, qui rend le style modifiable et remplace le verrou par le désalignement. La phrase voisine — « ils sont verrouillés au niveau du projet » — reste vraie mais avec un autre sens que celui que le mot suggère aujourd'hui : ce sont des propriétés de **projet** et non de **gabarit**, ce que DEC-006 continue de garantir. Deux corrections de rédaction, aucune décision nouvelle.

---

## C.17 Point d'entrée en ligne de commande

`tools/Pawnsmith.Cli` — **jetable, non livré, exclu de l'image Docker, sans tests**, exactement comme le §B.7 l'a posé. Écrit en **dernière tâche de la tranche**, quand tout le reste est relu.

Sa raison d'être est celle de DEC-027 : une tranche sans sortie observable se relit uniquement à travers ses tests, et cinquante-quatre tests ne montrent pas ce que montre une archive `Share` ouverte dans un explorateur de fichiers.

La commande de T1 devient une sous-commande explicite (DEC-059) :

```
pawnsmith-cli sheet --manifest ./manifeste.json --calibration ./config/calibration.json --out ./planche.pdf
```

Quatre sous-commandes s'y ajoutent :

| Sous-commande | Arguments | Effet |
|---|---|---|
| `project new` | `--root`, `--name`, `--geometry`, `--paper-format`, `--calibration` | Crée un dossier de projet valide et minimal, et affiche le chemin obtenu |
| `project check` | `--path`, `--calibration` | Charge le projet, affiche les erreurs de validation avec leur **code**, et les diagnostics séparément |
| `project export` | `--path`, `--profile Backup\|Share`, `--out`, `--calibration` | Écrit l'archive et affiche la liste de ses entrées |
| `project import` | `--archive`, `--root`, `--name`, `--calibration` | Importe et affiche le chemin obtenu, ou le code d'erreur |

**Aucune logique.** Le CLI lit ses arguments, appelle le dépôt, écrit le résultat, affiche les erreurs lisiblement. Toute règle qui apparaîtrait ici est une règle qui manque à l'Application.

Deux choix méritent d'être explicités. **`project check` affiche les erreurs et les diagnostics séparément**, parce que c'est exactement la distinction que DEC-056 introduit et qu'elle doit être visible plutôt que décrite. Et **`--calibration` est demandée par toutes les sous-commandes de projet**, y compris `check` : la validation relationnelle et la construction de la calibration effective en dépendent (DEC-053).

> **Le renommage de la commande de T1 et la mise à jour du protocole T0 se font dans le même commit** (DEC-059). Le §B.7 du cahier T1 et le protocole T0 décrivent aujourd'hui l'ancienne invocation ; les corriger avant que le code n'existe ferait décrire par la documentation une commande qu'aucun binaire n'accepte, ce qui est le défaut exact que la fiche cherche à éviter.

---

## Annexe — Ce qui vient après, et qu'il ne faut pas anticiper

T3 apportera le composeur, le catalogue et les règles de gestion (questions A à D du chapitre 16 de la bible) ; T4 le client ComfyUI, le template de workflow et donc la clause de cadrage réelle ; T5 le détourage ; T6 l'API, l'interface, et les deux arbitrages que T2 lui renvoie explicitement — le doublon de `projectId` à l'import, et le sort d'un candidat élu mais désaligné à l'export.

**Ne rien construire pour ces tranches.** En particulier : aucun port de clause de cadrage, aucun type `Catalog`, aucun mécanisme de migration, aucun verrou de concurrence. Les seuls contrats à implémenter en T2 sont `IProjectRepository` et les deux fonctions de domaine de C.5.
