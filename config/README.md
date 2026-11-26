# `config/`

Cinq fichiers de données, tous lus par l'application et jamais écrits par
elle. Ils se modifient à la main. Le lecteur tolère les commentaires `//` et
les virgules finales, et **ignore les champs qu'il ne connaît pas** — on peut
les annoter sans les casser (§C.6.3). Les fichiers livrés restent en JSON
strict, sans commentaire, pour ne dépendre d'aucun outil.

## `calibration.json`

Toutes les valeurs physiques du projet vivent ici (B.2 du cahier des charges).
**Aucune de ces valeurs ne doit apparaître comme constante dans le code source.**

> ⚠️ **Les valeurs actuelles sont provisoires**, à l'exception des dimensions de
> papier et des emprises de grille (`gridFootprintMm`). Elles seront remplacées
> par les mesures d'un tirage papier de contrôle (tranche T0, §5.6 de la bible).
> Le remplacement ne doit demander aucune modification de code.

**Piège à ne pas confondre** : `gridFootprintMm` est l'emprise du pion sur la
grille de jeu ; `pawnHeightMm` est sa hauteur visuelle debout. Ce sont deux
dimensions **indépendantes**. Ne jamais déduire l'une de l'autre.

## `prompt-template.{univers}.json`

La **structure** de la clause sujet d'un univers (§D.5 du cahier T3). Un fichier
par univers ; `fantasy` seul en v1.

| Clé | Rôle |
|---|---|
| `subjectHead` | La tête de la clause. **Exactement deux jetons** y sont admis, `{race}` et `{characterClass}`, et les deux sont obligatoires. Un jeton inconnu fait rejeter le fichier en le nommant — `{taille}` ne partira jamais au modèle en texte littéral. Chaque jeton reçoit le **fragment** de la valeur quand le catalogue a une liste pour ce champ : la tête livrée, `{race} {characterClass}`, donne « an orc warrior » (DEC-106). |
| `optionalOrder` | Les clés d'`optionalParameters` dont les fragments viennent en premier, dans cet ordre. Les clés absentes de la liste suivent, en ordre ordinal. |
| `unknownValueFragment` | Ce qu'on dit d'une valeur que le catalogue ne connaît pas. Seul jeton admis : `{value}`. |

La taille n'est **pas** un jeton, et c'est voulu : un pion `Large` est grand
parce que sa cellule est grande, pas parce que le prompt l'a dit (§D.5.2).

Ce fichier a un grand rayon d'explosion — une faute y déforme la clause de
**tous** les gabarits. Il s'édite rarement, et avec soin (DEC-065).

## `catalog.{univers}.json`

Le **vocabulaire** d'un univers : les listes de la race, de la classe et des
paramètres optionnels, les valeurs que chacune peut prendre, ce que chaque
valeur dit dans un prompt (§D.4 du cahier T3) et **comment elle s'affiche**
dans chaque langue de l'interface (DEC-106). `versionSchema` vaut `2`.

```json
{ "key": "weapon", "labels": { "en": "Weapon", "fr": "Arme" }, "entries": [
  { "value": "spear", "labels": { "en": "short spear", "fr": "lance courte" },
    "fragment": "wielding a short spear held vertically against the body" } ] }
```

Deux clés sont réservées aux champs obligatoires : `race` et `characterClass`.
Leurs fragments portent l'article — `"an orc"`, `"a goblin"` — pour que la tête
n'ait pas à le deviner.

**Chaque valeur porte un `fragment`, un groupe de mots complet inséré tel quel
dans la clause** — pas un mot nu (DEC-064). `weapon: axe` ne donne pas « axe »
mais « wielding a large battle axe held vertically flat against the body ».
Tout ce qui pourrait élargir la silhouette — arme, bouclier, cape — porte sa
propre contrainte de pose compacte, celle que DEC-042 exige et que T0a a
mesurée comme efficace.

Règles :

- les fragments sont en **anglais**, comme tout ce qui entre dans un prompt
  (DEC-037) ;
- l'ordre de `parameters` compte : c'est l'ordre de repli des fragments quand
  `optionalOrder` ne liste pas une clé ;
- clés et valeurs sont comparées **exactement**, casse comprise ;
- une clé ou une valeur répétée, un fragment vide, ou un libellé manquant pour
  `en` ou `fr`, fait rejeter le fichier.

Les objets que l'utilisateur ajoute depuis l'interface ne vont **pas** dans ce
fichier : ils vont dans son catalogue personnel, sous le dossier utilisateur
(DEC-107).

**Le catalogue ne valide jamais un projet.** Un gabarit qui porte une valeur
inconnue du catalogue se charge, se compose et s'exporte ; la valeur est
insérée telle quelle et un diagnostic le signale (DEC-056, §D.4.4). C'est ce
qui permet de partager un projet avec quelqu'un dont le catalogue diffère.

Le contenu livré est un **point de départ**, à enrichir à l'usage. C'est du
contenu, pas du code : l'améliorer ne demande aucune recompilation (DEC-010).

## `styles.{univers}.json`

La **bibliothèque de styles** livrée (DEC-110) : des points de départ que
l'utilisateur choisit à l'étape Projet. Choisir un style le **copie** dans le
projet ; modifier ce fichier ne change aucun projet existant.

| Clé | Rôle |
|---|---|
| `versionSchema` | Vaut `1`. |
| `styles[].id` | Identifiant stable, unique. |
| `styles[].names` | Le nom affiché, **un par langue** de l'interface (`en`, `fr`). |
| `styles[].styleClause` | La clause de style, en anglais. Une palette se dit ici : il n'y a pas d'autre champ pour elle. |
| `styles[].negativeClause` | La clause négative ; peut être vide. Avec Krea 2 Turbo à CFG 1,0, elle est sans effet (DEC-077). |

Les styles que l'utilisateur enregistre depuis l'interface vont dans le dossier
utilisateur, au même format, jamais ici.

## Le dossier utilisateur — `data/user/`

Ce qui n'est **pas** dans `config/` : les fichiers que l'application écrit
pour l'utilisateur, depuis l'interface (§I.4.2 du cahier T6 front). Réglage
`Pawnsmith:UserDirectory`, `data/user` par défaut ; en conteneur, le volume
`/app/data/user`. Aucune archive de projet ne les emporte.

| Fichier | Contenu |
|---|---|
| `catalog.{univers}.json` | Les objets ajoutés au catalogue (DEC-107), au format du catalogue livré |
| `styles.{univers}.json` | Les styles enregistrés (DEC-110), au format de la bibliothèque livrée |
| `generator.json` | L'adresse du générateur choisie dans l'interface (DEC-108). **Elle l'emporte sur `Pawnsmith:Generator:Url`** ; supprimer ce fichier rend la main à la configuration |

## `workflow.comfyui.json` — et son exemple

Le **graphe de workflow ComfyUI** que l'application soumet pour chaque candidat,
et la **clause de cadrage** qui part avec (§E.5 du cahier T4, DEC-076). C'est le
seul endroit où la clause de cadrage se modifie (DEC-029).

> ⚠️ **Le dépôt ne livre qu'un exemple, `workflow.comfyui.example.json`, à
> remplacer par le workflow exporté de ta machine.** Il est construit d'après
> les paramètres de DEC-043 et **n'a jamais été soumis à un ComfyUI réel** : les
> types de nœuds y sont plausibles, pas vérifiés. L'application ne retombe jamais
> dessus ; sans `workflow.comfyui.json`, la génération n'est pas configurée.

Pour fabriquer le vrai fichier :

1. dans ComfyUI, ouvrir le workflow validé en T0a, et l'exporter avec
   **Export (API)** — pas l'export ordinaire, dont le format (`nodes`, `links`)
   est refusé ;
2. remplacer trois valeurs par trois jetons : le texte du prompt positif par
   `"{{POSITIVE}}"`, la graine de l'échantillonneur par `"{{SEED}}"`, et, s'il y
   en a un, le texte du prompt négatif par `"{{NEGATIVE}}"` ;
3. envelopper le graphe dans le schéma ci-dessous, et enregistrer sous
   `config/workflow.comfyui.json`.

| Clé | Rôle |
|---|---|
| `versionSchema` | Vaut `1`. |
| `framingClause` | La clause de cadrage, **en tableau de lignes**, jointes par un saut de ligne. Elle part en tête de chaque prompt, avant le sujet et le style. |
| `outputNodeId` | L'identifiant du nœud dont l'image est rapatriée — en général le `SaveImage`. Il doit rendre **une seule** image. |
| `workflow` | Le graphe exporté, avec ses jetons. |

Règles des jetons, toutes vérifiées au démarrage :

- **trois jetons seulement**. `{{POSITIVE}}` et `{{SEED}}` exactement une fois,
  `{{NEGATIVE}}` au plus une fois. Tout autre `{{…}}` fait refuser le fichier en
  le nommant ;
- **un jeton est une valeur entière** : `"text": "{{POSITIVE}}"` et non
  `"text": "{{POSITIVE}}, masterpiece"`. Ce qui part au modèle doit être
  exactement le prompt que le candidat retient (DEC-049) ;
- **les dimensions ne sont pas des jetons** : on les écrit dans le graphe,
  directement. La découpe lit celles de l'image reçue.

**Modifier `framingClause` désaligne tous les candidats de tous les projets** :
c'est le comportement correct, le cadrage a changé (DEC-049). Changer le nombre
d'étapes ou le modèle dans le graphe, en revanche, ne désaligne rien (DEC-077).
