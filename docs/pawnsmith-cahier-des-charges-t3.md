# Pawnsmith — Cahier des charges : tranche T3

| | |
|---|---|
| **Version** | 1.0 |
| **Date** | 9 septembre 2026 |
| **Document parent** | `pawnsmith-bible.md` v0.13 — à lire en premier, chapitres 3, 4 et 16 en particulier |
| **Documents frères** | `pawnsmith-cahier-des-charges-t1.md` (partie B) et `-t2.md` (partie C), dont ce document reprend la forme |
| **Portée** | Parcours utilisateur, catalogue, fichiers de templates de prompts, composeur de clause sujet, règles de gestion |

> **Comment lire ce document.** Il ne redécrit pas les entités : le **chapitre 3 de la bible** fait foi sur ce qu'elles contiennent. Il ne redécrit pas non plus l'assemblage du prompt ni le désalignement : le **§C.5 de T2** les a spécifiés et le code existe. Ce document décide ce que ces deux-là laissent ouvert — d'où vient la clause sujet, quand elle se recompose, et les règles de gestion que le chapitre 16 réservait à cette tranche.
>
> Les sections sont numérotées **D.x**, à la suite des parties B (T1) et C (T2), pour que les renvois croisés restent lisibles quand plusieurs documents sont ouverts côte à côte.

> **Les quatre questions ouvertes que le chapitre 16 réservait à T3 sont tranchées ici** — A (parcours utilisateur), B (machines à états), C (règles de gestion) et D (entité Catalogue). Deux d'entre elles se scindent plutôt que de se fermer : la moitié « machine à états du `Job` » de B descend en T4, et une sous-question de C appartenait déjà à T6. Le détail est en **D.14**.

> **Onze fiches sont à consigner au chapitre 11 de la bible** — DEC-063 à DEC-073 — dont deux superposent des documents existants : DEC-066 supersède la signature d'`IPromptComposer` du chapitre 7, et DEC-072 déplace l'échéance de la question B au chapitre 16.

---

## D.0 Consignes de travail

Celles du §0 de T1 et du §C.0 de T2 s'appliquent intégralement et ne sont pas répétées. Trois s'appliquent avec une force particulière ici.

- **Aucun code implicite.** T3 est la tranche du texte généré, et le texte invite à la magie : substitution implicite de jetons, tri « naturel », repli silencieux. Chaque règle de ce document nomme son ordre, son séparateur et son cas de repli.
- **Ne pas anticiper les tranches à venir.** T3 ne génère aucune image et n'en détoure aucune. Il produit une chaîne de caractères et il s'arrête là. Le `Job`, le client HTTP et le template de workflow ComfyUI sont hors périmètre — voir D.9.
- **Petites tâches, un commit relisible chacune.** T3 est moins large que T2 mais plus profonde : la composition et la recomposition sont de vrais algorithmes, et c'est là que la relecture doit être lente.

---

## D.1 Objectif et périmètre

**Entrée** : un gabarit — race, classe, taille, paramètres optionnels, détails — et l'univers du projet.
**Sortie** : une clause sujet en anglais, déterministe, stockée sur le gabarit, et que l'utilisateur peut ensuite éditer.

### Dans le périmètre

- Le **parcours utilisateur** de bout en bout, comme document de conception (D.3, question A).
- Le **catalogue** : entité, fichier, schéma, chargement (D.4, question D).
- Les **fichiers de templates de prompts par univers** : schéma et chargement (D.5).
- Le **composeur** : le port `IPromptComposer` réduit à sa seule méthode substituable, et son implémentation par template (D.6).
- La **règle de recomposition** de la clause sujet (D.7).
- Les **règles de gestion** que le chapitre 16 réservait à cette tranche (D.8, question C).
- Un **point d'entrée en ligne de commande**, en dernière tâche, comme en B.7 et en C.17.

### Hors périmètre — ne rien écrire de tout cela

Points de terminaison d'API, interface, client ComfyUI, template de workflow, machine à états du `Job`, génération, découpe, détourage, second adaptateur de `IPromptComposer` (EVO-001), migration de schéma.

> **Trois tentations à nommer avant qu'elles ne se présentent.** La première est d'écrire l'assemblage du prompt : **il existe déjà**, en `ResolvedPrompt.From`, écrit et testé en T2. Le réécrire ici serait dupliquer la surface de compatibilité que le §C.5.5 met précisément sous fiche. La deuxième est de faire lire au composeur le template de workflow ComfyUI pour y prendre la clause de cadrage : ce fichier est à T4, et le §C.5.2 a explicitement refusé de créer un port pour aller le chercher. La troisième est d'ajouter un champ au gabarit pour savoir si la clause a été éditée — D.7 montre qu'on peut s'en passer, et DEC-048 rappelle que ce champ coûterait un `versionSchema` de plus.

---

## D.2 Vocabulaire et identifiants

DEC-037 s'applique sans exception. Rappel des identifiants que T3 introduit :

| Glossaire (ch. 2) | Type / clé |
|---|---|
| Catalogue | `Catalog` |
| Entrée de catalogue | `CatalogEntry` |
| Clé de paramètre optionnel | `parameterKey` |
| Fragment de phrase | `fragment` |
| Template de prompt | `PromptTemplate` |
| Composeur | `IPromptComposer` — **`ComposeSubject` seule**, voir D.6.1 |

Les **valeurs** du catalogue et les **fragments** sont en anglais, comme tout ce qui entre dans un prompt (DEC-037). Les **libellés** affichés à l'utilisateur sont des clés de traduction, jamais les valeurs elles-mêmes — un catalogue est du contenu de prompt, pas du texte d'interface.

---

## D.3 Le parcours utilisateur — question A

Le chapitre 16 posait cette question en notant qu'elle « révèle la moitié des règles de gestion sans avoir à les chercher ». C'est vérifié : le parcours ci-dessous a rendu deux sous-questions de la question C sans objet et en a fait surgir une que personne n'avait écrite (D.7).

### D.3.1 Le chemin nominal

| # | Étape | Ce que l'utilisateur fait | Tranche |
|---|---|---|---|
| 1 | **Projet** | Nomme le projet, choisit l'univers, la géométrie et le format de papier ; renseigne le style — nom, clause style, clause négative, palette. | T2 |
| 2 | **Gabarits** | Ajoute un gabarit : race, classe et taille obligatoires ; coche des paramètres optionnels dans le catalogue ; écrit des détails libres ; fixe une quantité. **Le composeur produit la clause sujet**, affichée et éditable ; le prompt résolu s'affiche en lecture seule. | **T3** |
| 3 | **Génération** | Lance un lot pour un gabarit : N candidats, une graine par candidat. | T4 |
| 4 | **Validation** | Compare les candidats, statue sur chacun, en élit un. | T4, T5, T6 |
| 5 | **Mise en page** | Regarde les planches calculées, groupées par taille, et la capacité de chaque page. | T1, T6 |
| 6 | **Impression** | Exporte le PDF dans la culture voulue. | T6 |

**T3 ne touche que l'étape 2**, et cela borne la tranche bien plus nettement que le chapitre 12 ne le fait.

### D.3.2 Ce que le parcours a rendu sans discussion

- **La navigation n'est pas un assistant** (§15.1) : on revient à l'étape 2 après avoir généré, on change un paramètre, et cela **désaligne** les candidats existants au lieu d'être interdit (DEC-030). Le mécanisme existe déjà et n'a rien à apprendre de T3.
- **Une planche partielle est un usage normal.** On élit six gobelins et on tire la page pendant que l'ogre se génère. Toute règle qui bloquerait l'export au motif qu'un gabarit n'a pas d'élu casserait ce parcours ; c'est ce qui tranche D.8.1.
- **L'utilisateur juge sur ce qu'il voit.** À l'étape 4, il voit l'image jumelée bien avant qu'un détourage n'existe. Une règle qui interdirait de statuer avant détourage lui interdirait de juger une image qu'il a sous les yeux ; c'est ce qui tranche D.8.3.

### D.3.3 Ce que le parcours a fait surgir

À l'étape 2, l'utilisateur peut **revenir sur un gabarit et changer un champ**. Le §3.1 de la bible dit que la clause sujet est « produite par le composeur à partir des champs ci-dessus » — donc dérivée — et, deux lignes plus bas, qu'elle est « stockée et éditable » et « ne se régénère pas toute seule **après édition** ». Le cas d'un gabarit **jamais édité** dont on change un champ n'est écrit nulle part. Sans règle, un gabarit affiche `orc` et demande un gobelin au modèle. C'est l'objet de D.7.

> **Question A est fermée par cette section.** Le chapitre 16 doit déplacer sa ligne A dans le tableau des questions refermées, en renvoyant ici.

---

## D.4 Le catalogue — question D

### D.4.1 Global, en fichier de données

**Décision : le catalogue est global à l'application, livré en fichier de données, un fichier par univers, éditable par l'utilisateur. Il n'est jamais embarqué dans un projet.**

Le critère est celui que DEC-052 a déjà appliqué à `gutterMm` : **qui détermine la valeur ?** Le vocabulaire d'équipement d'un univers est déterminé par l'univers, pas par le projet. Et Pawnsmith est mono-utilisateur (§1.5) : une propriété de l'univers est ici une propriété globale. L'embarquer par projet obligerait la même personne à ressaisir « hache d'armes » dans chaque projet, et produirait à terme des projets qui divergent sur un vocabulaire qui n'avait aucune raison de varier.

C'est aussi ce que DEC-010 fait déjà des templates de prompts — « un fichier par univers, livré avec l'application, éditable par l'utilisateur ». Le catalogue suit le même régime parce qu'il a la même nature.

Le contre-argument existe et il faut le dire : un projet partagé en profil `Share` arrive chez quelqu'un dont le catalogue ne contient peut-être pas les mêmes entrées. **Ce n'est pas un problème, et D.4.4 explique pourquoi** — la clause sujet est stockée sur le gabarit, pas recomposée à l'ouverture. Le destinataire lit exactement le texte que l'expéditeur a envoyé.

À consigner en **DEC-063**, qui ferme la question D du chapitre 16.

### D.4.2 Une entrée de catalogue est un fragment de phrase, pas un mot

**C'est la décision la moins évidente de cette tranche, et elle vient d'une mesure.**

DEC-043 a relevé, sur le premier sujet de T0a, que la consigne d'équipement a été ignorée : `a large battle axe` demandée, deux dagues obtenues. La fiche note que « cela compte dès que l'équipement viendra d'un catalogue : un utilisateur qui coche « hache » attend une hache ».

T3 ne peut pas rendre un modèle de diffusion obéissant. Il peut en revanche cesser de lui donner un mot isolé. Le prompt de référence de DEC-043 ne dit pas `spear` : il dit *une lance courte tenue verticalement contre le corps*. La contrainte de pose y est **collée à l'objet**, et la fiche mesure que cette contrainte « a tenu sur les trois sujets ».

**Décision : une entrée de catalogue porte un `fragment`, c'est-à-dire un groupe de mots complet, prêt à être inséré dans la clause sujet.** Pas un nom nu.

| | Ce qui serait écrit | Ce qui est écrit |
|---|---|---|
| Clé | `weapon` | `weapon` |
| Valeur | `axe` | `axe` |
| Fragment | *(déduit : « axe »)* | `wielding a large battle axe held vertically against the body` |

Deux bénéfices pour un seul champ. L'adhérence s'améliore, parce qu'une périphrase pèse plus qu'un mot dans un encodeur de texte. Et la **contrainte de pose compacte de DEC-042 se trouve portée par chaque objet qui pourrait élargir la silhouette**, au lieu d'être une phrase générale qui parle d'objets que le modèle ne sait pas encore qu'il va dessiner.

> **Ce que cette décision coûte, et pourquoi c'est acceptable.** Un catalogue devient plus long à écrire : chaque entrée demande une phrase anglaise, pas un mot. C'est réel. Mais c'est du contenu, pas du code, et c'est exactement le genre de fichier que DEC-010 rend éditable pour qu'il s'améliore à l'usage. L'alternative — dériver le fragment du mot par un patron du type `wielding a {value}` — recrée une convention implicite, interdite par le §0 de T1, et ne saurait produire ni `wearing`, ni `held vertically against the body`.

À consigner en **DEC-064**.

### D.4.3 Deux fichiers, pas un

**Décision : le catalogue et le template de prompt d'un univers sont deux fichiers distincts.**

Ils ont la même portée — l'univers — et la tentation de les réunir est réelle. L'argument qui les sépare est celui de DEC-029, appliqué un cran plus bas : **ils n'ont pas le même rayon d'explosion.**

| | Template de prompt | Catalogue |
|---|---|---|
| Ce qu'on y touche | La **structure** de la phrase | Le **vocabulaire** |
| Une erreur produit | Une clause sujet malformée pour **tous** les gabarits | Un fragment fautif pour **une** valeur |
| Qui l'édite | Un utilisateur averti, rarement | L'utilisateur ordinaire, souvent |

Réunir les deux mettrait la structure de phrase à portée de main de quelqu'un qui venait ajouter « hallebarde ». C'est la même logique qui garde la clause de cadrage hors de l'interface.

À consigner en **DEC-065**.

### D.4.4 Ce que le catalogue ne fait pas

**Le catalogue ne valide jamais un projet.** Un gabarit dont `optionalParameters` porte une clé ou une valeur que le catalogue ne connaît pas est **parfaitement valide** : il se charge, il s'affiche, il s'exporte.

C'est **DEC-056** appliqué tel quel : une donnée de projet n'est jamais rejetée par une donnée de machine. Le catalogue est une donnée de machine — il est global, il est éditable, il diffère d'une installation à l'autre. Le projet est une donnée d'utilisateur. Un projet reçu de quelqu'un dont le catalogue est plus riche doit **s'ouvrir pour être corrigé, pas planter**.

Conséquences, à ne pas découvrir plus tard :

- Le §C.3.4 de T2 écrit que les clés d'`optionalParameters` sont « non validées en T2 ». **Elles ne le sont pas davantage en T3.** T3 ajoute un catalogue, il n'ajoute pas une validation.
- Une valeur inconnue du catalogue à la **composition** produit un fragment de repli et un **diagnostic**, jamais une erreur. La règle exacte est en D.6.3.
- Le profil `Share` reste sans réflexion préalable, comme le §3.2 de la bible l'exige : la clause sujet voyage **stockée**, et le destinataire lit le texte de l'expéditeur même si son propre catalogue est vide.

### D.4.5 Schéma du fichier catalogue, version 1

Un fichier par univers, nommé d'après lui : `config/catalog.fantasy.json`.

```json
{
  "versionSchema": 1,
  "universe": "Fantasy",
  "parameters": [
    {
      "key": "weapon",
      "entries": [
        { "value": "axe",   "fragment": "wielding a large battle axe held vertically against the body" },
        { "value": "spear", "fragment": "wielding a short spear held vertically against the body" }
      ]
    },
    {
      "key": "armour",
      "entries": [
        { "value": "leather", "fragment": "wearing leather scraps" },
        { "value": "mail",    "fragment": "wearing a mail hauberk" }
      ]
    }
  ]
}
```

| Clé | Type | Obligatoire | Notes |
|---|---|---|---|
| `versionSchema` | entier | oui | Vaut `1`. Rejet d'une version plus récente, comme en C.6 |
| `universe` | chaîne | oui | Doit correspondre au nom du fichier et à une valeur de `Universe` |
| `parameters` | tableau | oui | Peut être vide. **L'ordre est significatif** — voir D.6.2 |
| `parameters[].key` | chaîne | oui | Non vide, unique dans le fichier. C'est la clé d'`optionalParameters` |
| `parameters[].entries` | tableau | oui | Peut être vide |
| `entries[].value` | chaîne | oui | Non vide, unique pour cette clé. C'est la valeur d'`optionalParameters` |
| `entries[].fragment` | chaîne | oui | Non vide. Groupe de mots anglais, inséré tel quel (DEC-064) |

Les règles de forme de C.3.3 s'appliquent — UTF-8 sans BOM, `\n`, caractères non ASCII littéraux — à ceci près que **ce fichier n'est jamais écrit par l'application**. Il est lu, jamais produit ; les règles de déterminisme d'écriture ne le concernent donc pas.

> **`parameters` est un tableau et non un objet, et c'est délibéré.** Un objet aurait donné des clés uniques gratuitement, au prix de l'ordre — que D.6.2 utilise pour ordonner les fragments dans la phrase. L'unicité est donc vérifiée à la lecture, explicitement, plutôt que déléguée au format. C'est la règle générale de C.3.3 : une collection choisit son camp — ordonnée ou triée — dans le schéma, jamais dans le code. Celle-ci est **ordonnée**.

---

## D.5 Les fichiers de templates de prompts

### D.5.1 La question F se scinde

Le chapitre 16 groupe sous la question F « le template de workflow ComfyUI et ses jetons » et « les fichiers de templates de prompts par univers », et fixe l'échéance à T4.

**Ces deux fichiers n'ont pas la même échéance.** Le template de workflow porte la clause de cadrage, que seul T4 sait lire (DEC-029, §C.5.2) — il reste en T4. Le fichier de templates de prompts, lui, est **ce que le composeur de T3 lit pour exister** : sans lui, `ComposeSubject` n'a pas de patron de phrase.

**Décision : la question F est scindée. Le schéma des fichiers de templates de prompts descend en T3 ; le template de workflow ComfyUI reste en T4.**

À consigner en **DEC-073**.

### D.5.2 Schéma du fichier template, version 1

Un fichier par univers : `config/prompt-template.fantasy.json`.

```json
{
  "versionSchema": 1,
  "universe": "Fantasy",
  "subjectHead": "a {race} {characterClass}",
  "optionalOrder": ["weapon", "armour", "clothing"],
  "unknownValueFragment": "{value}"
}
```

| Clé | Type | Obligatoire | Notes |
|---|---|---|---|
| `versionSchema` | entier | oui | Vaut `1` |
| `universe` | chaîne | oui | Comme pour le catalogue |
| `subjectHead` | chaîne | oui | Patron de tête. Jetons admis : `{race}`, `{characterClass}`. **Les deux sont obligatoires** — voir la note |
| `optionalOrder` | tableau | oui | Peut être vide. Ordre de sortie des fragments optionnels (D.6.2) |
| `unknownValueFragment` | chaîne | oui | Patron de repli. Seul jeton admis : `{value}` |

> **Les jetons sont une liste close, et un jeton inconnu fait rejeter le fichier.** Écrire `{taille}` dans `subjectHead` ne doit pas produire une clause contenant littéralement « {taille} » : cela partirait au modèle sans que rien ne le signale, et se découvrirait sur une illustration ratée. La liste des jetons est donc énumérée dans le code, et le chargement échoue en nommant le jeton fautif — convention 5 de DEC-038.

> **Pourquoi la taille n'est pas un jeton.** `Size` est une clé de regroupement en pages (§3.1), pas une propriété visuelle du personnage. Un pion `Large` est grand parce que la cellule est grande, pas parce que le prompt a dit « large ». Mettre la taille dans la clause sujet demanderait au modèle de dessiner un ogre plus grand **dans son propre cadre**, ce qui n'a pas de sens : chaque illustration remplit sa planche de la même façon. C'est le piège « emprise et hauteur sont indépendantes » du §3.1, transposé au texte.

---

## D.6 Le composeur

### D.6.1 Le port se réduit à `ComposeSubject`

Le chapitre 7 de la bible donne à `IPromptComposer` deux méthodes :

```csharp
public interface IPromptComposer
{
    string ComposeSubject(Gabarit gabarit, Univers univers);
    string Assemble(string clauseSujet, Style style);
}
```

**`Assemble` n'a plus lieu d'être.** T2 a écrit l'assemblage en fonction pure de domaine, `ResolvedPrompt.From(framing, subject, style)`, publique, testée, et dont le §C.5.5 fait une surface de compatibilité sous fiche.

Le motif n'est pas d'éviter une duplication — c'est ce qu'est un port. **Un port existe pour qu'on puisse en substituer l'implémentation.** Or EVO-001, qui est la seule seconde implémentation prévue, dit que le composeur par modèle de langage « ne réécrit **que la clause sujet** ; les clauses style et cadrage lui restent inaccessibles ». L'assemblage n'aura donc **jamais** de seconde implémentation : le mettre derrière une interface, c'est promettre une substitution que la conception interdit.

**Décision : `IPromptComposer` ne porte que `ComposeSubject`.**

```csharp
public interface IPromptComposer
{
    ComposedSubject ComposeSubject(Blueprint blueprint, Universe universe);
}

public sealed record ComposedSubject(string Clause, IReadOnlyList<CompositionDiagnostic> Diagnostics);
```

> **Pourquoi un record et non une chaîne.** D.6.3 impose qu'une valeur inconnue du catalogue produise un diagnostic **à côté** de la clause, jamais dedans. Une méthode rendant `string` n'aurait nulle part où le mettre, et le composeur devrait soit le taire, soit le lever en exception — les deux étant ce que DEC-056 interdit. Le composeur par modèle de langage d'EVO-001 rendra simplement une liste vide.

Le verrouillage de DEC-028 est **renforcé**, pas affaibli : aucune méthode de ce port ne reçoit ni ne rend une clause de style ou de cadrage. La garantie « le niveau du gabarit ne peut pas atteindre les clauses verrouillées » est désormais portée par une interface qui ne les mentionne pas du tout.

À consigner en **DEC-066**, qui supersède la signature du chapitre 7 — comme DEC-062 l'a fait pour `IProjectRepository`.

### D.6.2 Règle de composition

```
ComposeSubject(blueprint, template, catalog) =
    Normalize(
      Join(", ", [ Head(blueprint, template) ]
                 ++ OptionalFragments(blueprint, template, catalog)
                 ++ [ blueprint.Details ]
                 .Where(fragment => fragment.Length > 0)))
```

Avec :

- **`Head`** = `template.SubjectHead`, ses jetons `{race}` et `{characterClass}` remplacés par les champs du gabarit, chacun `Trim()`é.
- **`OptionalFragments`** = pour chaque clé de `blueprint.OptionalParameters`, le fragment que le catalogue associe au couple (clé, valeur), ou le repli de D.6.3.
- **`Normalize`** = celle de `ResolvedPrompt`, déjà écrite : fins de ligne réduites à `\n`, puis `Trim()`.

Quatre points, chacun pour une raison précise.

- **Le séparateur est `", "`.** C'est la forme du sujet de référence de DEC-043 — un groupe nominal à virgules, pas des phrases. Le changer change toutes les clauses sujet composées ensuite.
- **L'ordre des fragments optionnels est celui d'`optionalOrder`**, et les clés absentes de cette liste viennent après, **triées en ordinal**. Deux règles explicites plutôt qu'une convention. Un tri purement alphabétique donnerait « wearing…, wielding… » là où la prose anglaise veut l'inverse ; un ordre purement déclaré laisserait une clé oubliée sortir n'importe où, et le déterminisme est un critère d'acceptation de la tranche.
- **Le tri de repli est ordinal, jamais culturel.** Même piège qu'en C.3.3 : un tri culturel varie avec la culture du processus et la version d'ICU, et deux machines composeraient deux clauses différentes pour le même gabarit. Le défaut se manifesterait à l'intégration continue plutôt qu'en développement.
- **Les fragments vides sont omis**, pas joints. Un gabarit sans détails ne produit pas une clause finissant par « , ».

**La composition est déterministe** : mêmes gabarit, template et catalogue, même chaîne, sur toute machine et dans toute culture. C'est le premier critère d'acceptation du chapitre 12, et le test 1 le verrouille.

### D.6.3 Une valeur que le catalogue ne connaît pas

Elle **ne fait jamais échouer la composition** (D.4.4). Elle produit :

1. le fragment de repli du template, `unknownValueFragment`, son jeton `{value}` remplacé par la valeur brute ;
2. un **diagnostic** nommant la clé et la valeur.

Le diagnostic remonte au cas d'usage, qui le rend à l'appelant à côté de la clause composée. Il n'est ni une exception, ni une chaîne insérée dans le prompt.

> **Pourquoi un repli plutôt qu'un silence.** Omettre purement et simplement la valeur inconnue serait pire que de l'insérer mal : l'utilisateur a coché « hallebarde », et une clause qui n'en porte aucune trace produit une illustration sans hallebarde **sans que rien ne l'explique**. Le repli littéral donne au moins le mot au modèle, et le diagnostic dit à l'utilisateur que son catalogue ne connaît pas cette valeur — ce qu'il peut corriger, puisque le fichier est éditable.

### D.6.4 Ce que le composeur ne fait pas

| Le composeur… | Parce que |
|---|---|
| …ne lit ni la clause style ni la clause cadrage | DEC-028, DEC-029, et sa signature ne les mentionne pas (D.6.1) |
| …n'assemble pas le prompt | `ResolvedPrompt.From` le fait déjà (§C.5) |
| …ne valide pas `optionalParameters` contre le catalogue | DEC-056, D.4.4 |
| …n'appelle aucun modèle de langage | DEC-009. C'est EVO-001, et il ne réécrira que la clause sujet |
| …ne connaît pas la taille du gabarit | D.5.2 |
| …n'écrit rien sur le gabarit | Il rend une chaîne. Qui la stocke, et quand, est en D.7 |

---

## D.7 La recomposition — la règle que le parcours a fait surgir

### D.7.1 Le problème, en trois faits qui se contredisent presque

1. Le §3.1 dit que la clause sujet est « **produite par le composeur** à partir des champs ci-dessus » — donc dérivée.
2. Le même §3.1 dit qu'elle est « **stockée et éditable** » et qu'elle « ne se régénère pas toute seule **après édition** » — donc figée.
3. DEC-028 verrouille la seule chose qui compte vraiment : **l'édition de l'utilisateur ne doit jamais être écrasée.**

Le trou est le gabarit **jamais édité** dont on change un champ. Sans règle, il affiche `orc` et demande un gobelin.

### D.7.2 Décision

**La clause sujet se recompose tant qu'elle n'a pas été éditée à la main. L'édition n'est pas stockée : elle se déduit.**

```
OnBlueprintFieldsChanged(before, after, template, catalog):
    if blueprint.SubjectClause == ComposeSubject(before, template, catalog):
        after.SubjectClause = ComposeSubject(after, template, catalog)
    else:
        after.SubjectClause is left untouched
```

La clause stockée est comparée à celle que le composeur aurait produite **pour les anciens champs**. Identiques, personne n'y a touché : on recompose. Différentes, l'utilisateur l'a éditée : on n'y touche pas.

Trois propriétés qui font préférer cette forme à un booléen stocké.

- **Aucun champ ajouté, donc `versionSchema` reste à 1.** DEC-048 pose qu'« il n'existe pas d'ajout compatible » : un booléen `subjectClauseEdited` ferait passer le schéma de projet en version 2, sur un schéma que T2 vient de figer et dont aucun autre besoin ne demande la montée.
- **C'est le réflexe du projet.** `promptResolu` et `desaligne` sont calculés et jamais persistés, pour la raison que le §3.2 énonce : « une valeur calculée qu'on persiste devient une valeur qui ment dès la première modification manquée ». Un drapeau d'édition est exactement une telle valeur — il ment dès qu'on restaure un `project.json` à la main, ce que DEC-011 encourage.
- **Le repli est du bon côté.** Si le template ou le catalogue changent, la comparaison échoue et la clause est traitée comme éditée : on ne touche à rien. Se tromper coûte une recomposition manquée, jamais un texte d'utilisateur écrasé.

**La comparaison est ordinale, caractère par caractère**, sur les deux chaînes normalisées — même règle que le désalignement en C.5.4, et pour le même motif.

À consigner en **DEC-067**.

### D.7.3 Où cette règle vit, et où elle ne vit pas

**Dans un cas d'usage de `Pawnsmith.Application`, jamais dans le dépôt.**

DEC-055 le dit en propres termes : `SaveAsync` ne reçoit pas l'état antérieur et ne doit jamais en recevoir, parce qu'« une telle règle appartiendrait à un cas d'usage, pas au dépôt ». La recomposition a précisément besoin des champs d'avant. C'est donc le cas d'usage — qui tient l'avant et l'après — qui recompose, puis remet au dépôt un projet déjà à jour.

> **Conséquence sur le CLI.** Une sous-commande qui modifie un gabarit doit passer par ce cas d'usage, jamais par `SaveAsync` directement. C'est la seule façon dont un harnais « sans logique » (§C.17) peut exercer une règle de gestion sans la réimplémenter.

---

## D.8 Les règles de gestion — question C

Le chapitre 16 en listait six. **Deux sont sans objet**, et il faut le dire avant de traiter les quatre autres.

| Sous-question | Statut |
|---|---|
| Export avec un candidat élu mais **désaligné** | **Déjà attribuée à T6** par le §C.5.6 de T2. Comportement d'export, pas règle de modèle |
| **Quantité dépassant la capacité** de page | **Sans objet.** T1 pagine déjà : `Pagination.Plan` étale les copies sur autant de pages qu'il faut. Seule une capacité **nulle** est une erreur, et elle est déjà levée en nommant la taille. Le chapitre 16 a été écrit avant que T1 n'existe |

### D.8.1 Un gabarit sans candidat élu est ignoré, et signalé

**Décision : il ne produit aucune cellule ; un groupe de taille dont aucun gabarit n'a d'élu ne produit aucune page. La mise en page rend un diagnostic nommant les gabarits sautés.**

Les sous-questions 1 et 5 du chapitre 16 sont la même règle vue des deux bouts, et se répondent ensemble.

Bloquer l'export casserait le parcours de D.3.2 : imprimer une planche partielle pendant qu'un gabarit se génère est un usage normal, pas une erreur. Mais l'ignorer **silencieusement** ferait disparaître de la planche un gabarit déclaré avec une quantité de six, sans que rien ne le dise — l'utilisateur le découvrirait feuille en main.

Le diagnostic est la sortie, et ce n'est pas une invention : **c'est le motif que DEC-056 a installé** pour `paperFormat` inconnu et pour la surcharge d'onglet incompatible. On n'empêche rien, on dit ce qu'on a fait.

**Jamais de page vide.** Une page vide n'est pas un signalement, c'est du papier perdu.

À consigner en **DEC-069**.

### D.8.2 Supprimer un gabarit emporte ses candidats et leurs fichiers

**Décision : la suppression d'un gabarit supprime ses candidats et les fichiers image de ceux-ci, que l'un d'eux soit élu ou non. Aucun refus, aucune étape préalable de dé-élection.**

Le motif est qu'un fichier que **plus rien ne référence** n'est pas une sauvegarde : il est invisible depuis l'application, inconsultable autrement qu'au gestionnaire de fichiers, et il alourdit chaque archive `Backup` pour toujours. Conserver les PNG donnerait l'illusion d'un filet sans en être un.

Refuser tant qu'un candidat est élu protégerait d'un geste malheureux, au prix d'une étape qui n'explique pas pourquoi elle existe — et que l'utilisateur qui veut vraiment supprimer exécuterait machinalement.

Ce qui rend la décision tenable est ailleurs, et il faut le nommer : **un projet est un dossier ordinaire** (DEC-011), versionnable et sauvegardable, et le profil `Backup` existe précisément pour ça. Le filet est là, il est explicite, et il ne dépend pas de fichiers orphelins.

> **Ce que cela impose au code.** La suppression touche le disque **après** avoir touché le modèle, et elle ne supprime que des fichiers que le gabarit supprimé référençait. Elle ne balaie jamais `images/` à la recherche de ce qui n'est plus référencé : un tel balayage supprimerait aussi les fichiers d'un projet à demi écrit, et transformerait une opération locale en opération globale.

À consigner en **DEC-070**.

### D.8.3 Le statut n'exige rien ; l'élection exige les deux détourages

**Décision : un candidat peut porter n'importe quel statut sans posséder ses détourages. En revanche, seul un candidat possédant `frontImageFile` **et** `backImageFile` peut être élu.**

Les deux axes restent indépendants, comme le §3.1 l'impose déjà pour le désalignement. Le statut est le **jugement** de l'utilisateur, et D.3.2 rappelle qu'il juge sur l'image jumelée, bien avant qu'un détourage n'existe : lui interdire de valider ce qu'il a sous les yeux serait absurde, et créerait une dépendance de T3 vers T5.

L'élection, elle, n'est pas un jugement : c'est **la désignation de ce que la planche va consommer**. Or la planche consomme deux PNG détourés, et rien d'autre. La contrainte se pose donc là, une seule fois, à l'endroit où elle sert.

> **Ce que cela évite.** Sans cette règle, un gabarit peut avoir un élu sans détourage, et le défaut ressort à la mise en page sous la forme d'un fichier introuvable — une erreur technique là où il fallait une règle métier, et à l'étape 5 alors que la faute a été commise à l'étape 4.

Le code d'erreur est `CANDIDATE_NOT_CUT_OUT`.

À consigner en **DEC-071**.

### D.8.4 L'ancien élu ne change pas de statut

**Décision : élire un candidat fait simplement pointer `electedCandidateId` ailleurs. Le statut de l'ancien élu n'est pas modifié.**

C'est la moitié restante de la question B. L'élection et le statut sont deux axes, et les fusionner est exactement le piège que le §3.1 nomme pour le désalignement : cela rendrait impossible de distinguer « rejeté par l'utilisateur » de « simplement pas retenu cette fois ».

Un candidat `Valid` qu'on cesse d'élire **reste `Valid`** : l'utilisateur l'a jugé bon, et en préférer un autre ne le rend pas mauvais. Le repasser à `Draft` effacerait un jugement qu'il avait porté, sans qu'il l'ait demandé.

À consigner en **DEC-068**.

---

## D.9 Ce que la question B laisse à T4

La machine à états du `Job` — en file, en cours, échoué, annulé — **n'est pas tranchée ici**.

Le `Job` est l'objet de la génération, et T3 ne génère rien. En fixer les états sans le client ComfyUI reviendrait à concevoir pour une tranche à venir, ce que le §0 de T1 interdit en propres termes et que l'annexe de T2 répète. Les états d'un travail dépendent de ce que le générateur sait réellement rendre — une file interrogeable, un identifiant de tâche, une annulation qui aboutit ou non — et rien de tout cela n'est connu avant T4.

**Décision : la question B est scindée. Sa première moitié est fermée par D.8.4 ; sa seconde moitié change d'échéance et passe à T4.**

À consigner en **DEC-072**, qui modifie la ligne B du chapitre 16.

---

## D.10 Codes d'erreur

Ajoutés à ceux du §C.11. Ils sont rendus par l'API, jamais traduits (chapitre 10).

| Code | Quand |
|---|---|
| `CATALOG_INVALID` | Fichier catalogue malformé, clé ou valeur dupliquée, fragment vide |
| `CATALOG_SCHEMA_TOO_RECENT` | `versionSchema` du catalogue supérieur à 1 |
| `TEMPLATE_INVALID` | Fichier template malformé, ou champ obligatoire absent |
| `TEMPLATE_UNKNOWN_TOKEN` | Jeton inconnu dans `subjectHead` ou `unknownValueFragment`, nommé |
| `TEMPLATE_SCHEMA_TOO_RECENT` | `versionSchema` du template supérieur à 1 |
| `UNIVERSE_MISMATCH` | Le champ `universe` du fichier ne correspond pas à celui attendu |
| `BLUEPRINT_NOT_FOUND` | Une opération de gabarit désigne un identifiant que le projet ne porte pas |
| `CANDIDATE_NOT_FOUND` | Une élection désigne un candidat que le gabarit ne porte pas |
| `CANDIDATE_NOT_CUT_OUT` | Tentative d'élection d'un candidat sans ses deux détourages (D.8.3) |

Les six premiers sont levés par l'Infrastructure, à la lecture des fichiers. Les trois derniers sont levés par l'**Application**, là où la règle vit : ils ne parlent d'aucun fichier. Les deux `*_NOT_FOUND` n'étaient pas dans la v1.0 de ce document ; l'écriture du cas d'usage les a demandés, une opération sur un identifiant inconnu devant refuser avec un code et non avec une exception de programmation.

Les valeurs inconnues du catalogue ne figurent pas ici : ce sont des **diagnostics**, pas des erreurs (D.6.3).

---

## D.11 Tests attendus

Trente-deux tests, groupés par sujet. Les numéros sont stables et servent de référence dans les messages de commit, comme en B.8 et C.12.

### Composition (1 à 9)

| # | Ce qu'il vérifie |
|---|---|
| 1 | Deux compositions du même gabarit donnent la **même chaîne**, octet pour octet |
| 2 | La composition est identique sous culture `fr-FR` et sous culture invariante |
| 3 | `{race}` et `{characterClass}` sont substitués ; les champs sont `Trim()`és |
| 4 | Les fragments optionnels sortent dans l'ordre d'`optionalOrder` |
| 5 | Une clé absente d'`optionalOrder` sort **après** les autres, en ordre ordinal |
| 6 | Un gabarit sans paramètre optionnel et sans détails compose la seule tête |
| 7 | Les détails viennent en **dernier** |
| 8 | Un fragment vide est omis, et la clause ne finit jamais par un séparateur |
| 9 | La clause composée est normalisée : ni `\r\n`, ni espace de tête ou de queue |

### Catalogue et repli (10 à 16)

| # | Ce qu'il vérifie |
|---|---|
| 10 | Un couple (clé, valeur) connu produit le **fragment**, pas la valeur |
| 11 | Une **valeur** inconnue produit le repli `unknownValueFragment` et **un diagnostic** |
| 12 | Une **clé** inconnue produit le même repli et le même diagnostic |
| 13 | Une valeur inconnue **ne lève jamais** d'exception (DEC-056) |
| 14 | Un catalogue vide compose quand même la tête et les détails |
| 15 | Une clé dupliquée dans le fichier est rejetée par `CATALOG_INVALID`, en la nommant |
| 16 | Une valeur dupliquée pour une même clé est rejetée de même |

### Chargement des fichiers (17 à 22)

| # | Ce qu'il vérifie |
|---|---|
| 17 | `versionSchema` supérieur à 1 rejeté sans lecture partielle, des deux fichiers |
| 18 | Un jeton inconnu dans `subjectHead` est rejeté en **nommant le jeton** |
| 19 | `subjectHead` sans `{race}` ou sans `{characterClass}` est rejeté |
| 20 | Un `universe` ne correspondant pas à celui attendu est rejeté |
| 21 | Un `fragment` vide est rejeté |
| 22 | L'ordre de `parameters` est **préservé** à la lecture, jamais trié |

### Recomposition (23 à 28)

| # | Ce qu'il vérifie |
|---|---|
| 23 | Changer la race d'un gabarit **non édité** recompose la clause |
| 24 | Changer la race d'un gabarit **édité** laisse la clause intacte |
| 25 | Une clause éditée puis **remise à l'identique** de la composition est traitée comme non éditée — conséquence assumée de la déduction, et elle est correcte |
| 26 | Un catalogue modifié fait échouer la comparaison, donc **rien n'est écrasé** |
| 27 | La recomposition est déclenchée par le **cas d'usage**, pas par `SaveAsync` |
| 28 | Le désalignement des candidats existants est recalculé correctement après recomposition |

### Règles de gestion (29 à 32)

| # | Ce qu'il vérifie |
|---|---|
| 29 | Un gabarit sans élu ne produit aucune cellule et **un diagnostic le nomme** |
| 30 | Un groupe de taille sans aucun élu ne produit **aucune page** |
| 31 | Élire un candidat sans détourage échoue par `CANDIDATE_NOT_CUT_OUT` |
| 32 | Élire un second candidat laisse le **statut** du premier inchangé |

---

## D.12 Critères d'acceptation

T3 est terminée quand :

- [ ] Les **32** tests de D.11 passent.
- [ ] **Composition déterministe** : même entrée, même sortie, sous toute culture (tests 1 et 2).
- [ ] Les clauses **style et cadrage sont inatteignables** depuis le niveau du gabarit, et la signature d'`IPromptComposer` le rend structurellement vrai — aucune de ses méthodes ne les mentionne (DEC-066).
- [ ] Le **désalignement est correctement calculé** après édition d'une clause sujet, d'un style ou d'un univers (test 28).
- [ ] Aucune valeur de catalogue ni de template **en dur dans le code** ; les deux fichiers sont lus, jamais connus.
- [ ] Un projet portant une valeur inconnue du catalogue **s'ouvre, se compose et s'exporte** (test 13, DEC-056).
- [ ] `versionSchema` de `project.json` **reste à 1** : T3 n'ajoute aucun champ au schéma de projet (DEC-067).
- [ ] `Pawnsmith.Domain.csproj` ne référence toujours rien.
- [ ] La règle de recomposition vit dans un **cas d'usage**, et `SaveAsync` n'a reçu aucun paramètre d'état antérieur (test 27, DEC-055).
- [ ] Le CLI compose une clause sujet et affiche ses diagnostics, **sans contenir de logique**.
- [ ] Les fiches DEC de D.14 sont écrites au chapitre 11 de la bible.
- [ ] L'intégration continue est verte.
- [ ] Le code est relu intégralement (DEC-027).

---

## D.13 Ce que T3 ne fait pas

| T3… | Parce que |
|---|---|
| …n'assemble pas le prompt | `ResolvedPrompt.From` existe depuis T2 |
| …ne lit pas le template de workflow ComfyUI | T4, DEC-029, DEC-073 |
| …ne définit aucune machine à états du `Job` | T4, DEC-072 |
| …n'appelle aucun modèle de langage | DEC-009 ; c'est EVO-001 |
| …ne valide pas `optionalParameters` contre le catalogue | DEC-056, D.4.4 |
| …n'ajoute aucun champ à `project.json` | DEC-067, et DEC-048 en donne le prix |
| …ne tranche pas le sort d'un candidat élu mais désaligné à l'export | T6, §C.5.6 |
| …n'expose aucun point de terminaison ni écran | T6 |
| …ne modifie aucune ligne de T1 | Le composeur produit du texte ; la planche n'en voit rien |

---

## D.14 Décisions à consigner dans la bible

Onze fiches, à écrire au chapitre 11 **avant l'implémentation, en un seul commit de documentation** — comme T2 l'a fait pour DEC-046 à DEC-059. Une décision prise pendant l'écriture du code, s'il en naît une, se dépose dans le commit de ce code, comme DEC-062.

| Réf. | Objet | Section |
|---|---|---|
| **DEC-063** | Le catalogue est global, en fichier de données, un fichier par univers. **Ferme la question D** | D.4.1 |
| **DEC-064** | Une entrée de catalogue porte un **fragment de phrase**, pas un mot ; la contrainte de pose de DEC-042 voyage avec l'objet | D.4.2 |
| **DEC-065** | Catalogue et template sont **deux fichiers distincts**, parce qu'ils n'ont pas le même rayon d'explosion | D.4.3 |
| **DEC-066** | `IPromptComposer` se réduit à `ComposeSubject`. **Supersède la signature du chapitre 7** | D.6.1 |
| **DEC-067** | La clause sujet se recompose tant qu'elle n'a pas été éditée, et **l'édition se déduit** au lieu d'être stockée ; `versionSchema` reste à 1 | D.7.2 |
| **DEC-068** | L'élection et le statut sont deux axes ; l'ancien élu ne change pas de statut. **Ferme la moitié restante de la question B** | D.8.4 |
| **DEC-069** | Un gabarit sans élu est ignoré **et signalé** ; jamais de page vide | D.8.1 |
| **DEC-070** | Supprimer un gabarit emporte ses candidats et leurs fichiers | D.8.2 |
| **DEC-071** | Le statut n'exige aucun fichier ; **l'élection exige les deux détourages** | D.8.3 |
| **DEC-072** | La machine à états du `Job` descend en T4. **Scinde la question B** | D.9 |
| **DEC-073** | La question F est scindée : le schéma des fichiers de templates est T3, le workflow ComfyUI reste T4 | D.5.1 |

**La question A est fermée par D.3** et n'a pas de fiche : un parcours est une description, pas une décision. Sa ligne au chapitre 16 passe dans le tableau des questions refermées, en renvoyant à cette section.

---

## D.15 Corrections à apporter aux documents

Signalées plutôt que corrigées en silence.

1. **Le chapitre 16 a été écrit avant que T1 n'existe.** Sa question C demande de trancher « quantité dépassant la capacité de page » ; `Pagination.Plan` pagine déjà, et `PageCapacityException` traite déjà le seul cas réellement fautif. La sous-question disparaît.

2. **Le chapitre 7 écrit encore `IPromptComposer` en franglais** — `ComposeSubject(Gabarit gabarit, Univers univers)` — ce que DEC-037 supersède depuis la v0.5 de la bible. Le renommage y avait été appliqué à `IProjectRepository` mais pas à ce port. Corrigé par DEC-066, qui réécrit la signature de toute façon.

3. **Le §15.1 range l'étape « Gabarits » en T3 et l'étape « Génération » en T4-T5**, ce qui est correct, mais le §15.3 décrit un panneau de paramètres qui suppose le catalogue chargé sans jamais dire d'où il vient. La réponse est désormais D.4 ; le §15.3 doit y renvoyer.

4. **Le §3.1 laisse le trou de D.7.1** — « produite par le composeur » et « stockée et éditable » cohabitent sans que le cas du gabarit jamais édité soit traité. DEC-067 le comble ; la ligne `clauseSujet` du tableau des gabarits doit y renvoyer.

---

## Annexe — Ce qui vient après, et qu'il ne faut pas anticiper

T4 apportera le client ComfyUI, le template de workflow, la clause de cadrage réelle et la machine à états du `Job` ; T5 le détourage et les bornes de ressources de la question G ; T6 l'API, l'interface, et les trois arbitrages qui lui sont explicitement renvoyés — le doublon de `projectId` à l'import (T2), le sort d'un candidat élu mais désaligné à l'export (T2), et la formulation des paramètres optionnels que le §15.3 désigne comme « un travail d'interface à part entière ».

**Ne rien construire pour ces tranches.** En particulier : aucun type `Job`, aucun client HTTP, aucun lecteur de workflow, aucun second adaptateur de `IPromptComposer`.
