# `config/`

Trois fichiers de données, tous lus par l'application et jamais écrits par
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
| `subjectHead` | La tête de la clause. **Exactement deux jetons** y sont admis, `{race}` et `{characterClass}`, et les deux sont obligatoires. Un jeton inconnu fait rejeter le fichier en le nommant — `{taille}` ne partira jamais au modèle en texte littéral. |
| `optionalOrder` | Les clés d'`optionalParameters` dont les fragments viennent en premier, dans cet ordre. Les clés absentes de la liste suivent, en ordre ordinal. |
| `unknownValueFragment` | Ce qu'on dit d'une valeur que le catalogue ne connaît pas. Seul jeton admis : `{value}`. |

La taille n'est **pas** un jeton, et c'est voulu : un pion `Large` est grand
parce que sa cellule est grande, pas parce que le prompt l'a dit (§D.5.2).

Ce fichier a un grand rayon d'explosion — une faute y déforme la clause de
**tous** les gabarits. Il s'édite rarement, et avec soin (DEC-065).

## `catalog.{univers}.json`

Le **vocabulaire** d'un univers : les clés des paramètres optionnels, les
valeurs que chacune peut prendre, et ce que chaque valeur dit dans un prompt
(§D.4 du cahier T3).

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
- une clé ou une valeur répétée, ou un fragment vide, fait rejeter le fichier.

**Le catalogue ne valide jamais un projet.** Un gabarit qui porte une valeur
inconnue du catalogue se charge, se compose et s'exporte ; la valeur est
insérée telle quelle et un diagnostic le signale (DEC-056, §D.4.4). C'est ce
qui permet de partager un projet avec quelqu'un dont le catalogue diffère.

Le contenu livré est un **point de départ**, à enrichir à l'usage. C'est du
contenu, pas du code : l'améliorer ne demande aucune recompilation (DEC-010).
