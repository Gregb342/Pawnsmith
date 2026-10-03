# Pawnsmith — Cahier des charges : tranche T4

| | |
|---|---|
| **Version** | 1.0 |
| **Date** | 3 octobre 2026 |
| **Document parent** | `pawnsmith-bible.md` v0.15 — à lire en premier, chapitres 4, 6.4, 7, 9 et 16 en particulier |
| **Documents frères** | `pawnsmith-cahier-des-charges-t1.md` (partie B), `-t2.md` (partie C) et `-t3.md` (partie D), dont ce document reprend la forme |
| **Portée** | Client du générateur ComfyUI, template de workflow, machine à états du `Job`, production des candidats d'un lot, règle de découpe de l'image jumelée |

> **Régime d'écriture.** Ce document a été écrit et tranché **sans arbitrage du porteur**, dans le régime « itère sans attendre, tranche et consigne » ouvert pour T4 et l'API de T6. Chaque décision prise sans son arbitrage est une fiche DEC du chapitre 11, et la liste est reprise au §8 de `CLAUDE.md`, sous « Décisions prises sans toi, à relire en premier ». Rien ici n'est définitif au sens où une relecture ne pourrait pas le rouvrir.

> **Comment lire ce document.** Il ne redécrit pas les entités : le **chapitre 3 de la bible** fait foi sur ce qu'un candidat contient, et T2 a figé ce schéma. Il ne redécrit pas l'assemblage du prompt : `ResolvedPrompt.From` existe depuis T2 (§C.5), et ce document l'**appelle**, il ne le réécrit pas. Il décide ce que les tranches précédentes ont explicitement laissé à T4 — la machine à états du `Job` (DEC-072), le template de workflow (DEC-073), la contrainte de fidélité imposée par DEC-049 — et ce que le chapitre 12 lui confie : génération jumelée et découpe.
>
> Les sections sont numérotées **E.x**, à la suite des parties B (T1), C (T2) et D (T3).

> **Deux questions ouvertes du chapitre 16 sont fermées ici**, et une troisième est scindée. B (machine à états du `Job`) et F (template de workflow) se ferment ; G (valeurs non fonctionnelles) perd sa moitié « génération », la moitié « détourage » restant à T5. Le détail est en **E.15**.

---

## E.0 Consignes de travail

Celles du §0 de T1, du §C.0 de T2 et du §D.0 de T3 s'appliquent intégralement. Trois s'appliquent ici avec une force particulière.

- **Le générateur est une entrée non fiable.** Tout ce qui revient de ComfyUI — corps JSON, noms de fichiers, octets d'image — se lit comme une archive tierce : borné **avant** d'être lu, vérifié **avant** d'être écrit, et jamais concaténé à un chemin local.
- **Aucune substitution textuelle.** Le template de workflow est un graphe JSON et il se modifie **comme un graphe**, nœud par nœud. Un `string.Replace` sur le texte JSON casserait le document dès qu'un prompt contient un guillemet, et — c'est le piège qui a déjà coûté un correctif en T3 — re-balaierait une valeur substituée à la recherche du jeton suivant.
- **Ne pas anticiper les tranches à venir.** T4 produit une **image jumelée brute** par candidat, et s'arrête là. Le détourage, et donc les fichiers `front` et `back`, sont à T5. L'API qui hébergera les `Job` en arrière-plan est à T6.

---

## E.1 Objectif et périmètre

**Entrée** : un gabarit d'un projet, un nombre de candidats à produire, la clause de cadrage du template de workflow.
**Sortie** : autant de candidats que le générateur en a produit, chacun **sauvegardé dans le projet dès qu'il existe**, avec son image jumelée sur disque et ses trois clauses figées ; et un `Job` terminal qui dit comment le lot s'est fini.

### Dans le périmètre

- La **machine à états du `Job`** (E.3, question B).
- Le **lot** : ce qu'il fige, l'ordre de ses opérations, la conservation de ce qui est déjà produit (E.4).
- Le **template de workflow ComfyUI** : schéma, jetons, lecture, substitution (E.5, question F).
- Ce qui part au générateur, et la **fidélité exigée par DEC-049** (E.6).
- Le **port `IImageGenerator`** et son adaptateur ComfyUI, sur l'API HTTP de ComfyUI (E.7).
- La **règle de découpe** de l'image jumelée (E.8).
- Les **bornes** de la génération, moitié T4 de la question G (E.9).
- Les surfaces de menace que T4 ouvre (E.10).
- Un **point d'entrée en ligne de commande**, en dernière tâche (E.18).

### Hors périmètre — ne rien écrire de tout cela

Détourage, fichiers `front` et `back`, runtime ONNX, plafonds de T5. Points de terminaison d'API et hébergement des `Job` en arrière-plan (T6). Journalisation (T7). Seconde implémentation de `IImageGenerator` (EVO-002). Suivi de progression par WebSocket.

> **Trois tentations à nommer avant qu'elles ne se présentent.** La première est de **découper les pixels** de l'image jumelée ici : E.8 montre pourquoi la règle de découpe est à T4 et son exécution à T5. La deuxième est de **persister le `Job`** dans `project.json` pour qu'un lot survive à un redémarrage : E.3.4 montre qu'il n'y a rien à faire survivre, puisque chaque candidat est déjà sauvegardé. La troisième est d'**écrire `IPawnPairProducer`** parce que le chapitre 7 le dessine : E.7.1 montre que T0a a retiré la raison d'être de ce port.

---

## E.2 Vocabulaire et identifiants

DEC-037 s'applique sans exception. Identifiants que T4 introduit :

| Concept | Type / clé |
|---|---|
| Job | `Job` — états `JobState` : `Queued`, `Running`, `Completed`, `Failed`, `Cancelled` |
| Lot | `GenerationBatch` |
| Générateur | `IImageGenerator` |
| Image générée | `GeneratedImage` |
| Template de workflow | `WorkflowTemplate`, fichier `workflow.comfyui.json` |
| Découpe | `PairSplit` |

Un **lot** n'est pas un terme du glossaire, et n'en devient pas un : c'est le contenu d'un `Job` de génération — N graines, un seul prompt. Le chapitre 2 définit déjà le `Job` comme « unité d'exécution asynchrone traçable (génération d'un lot, détourage, export) » ; le mot était donc employé sans être défini, et il l'est ici.

---

## E.3 La machine à états du `Job` — question B

DEC-072 a fait descendre cette question en T4, au motif que « les états d'un travail dépendent de ce que le générateur sait réellement rendre — une file interrogeable, un identifiant de tâche, une annulation qui aboutit ou non ». Le client existe maintenant, et l'API de ComfyUI répond aux trois : elle rend un identifiant de tâche (`prompt_id`), sa file est interrogeable (`/queue`, `/history`), et une annulation aboutit — en retirant la tâche de la file si elle attend, en interrompant l'exécution si elle tourne.

### E.3.1 Cinq états, deux transitions de départ

```
            Start            Complete
  Queued ──────────► Running ─────────► Completed
    │                  │  │
    │ Cancel           │  │ Fail
    ▼                  │  ▼
  Cancelled ◄──────────┘  Failed
             Cancel
```

| De | Vers | Quand |
|---|---|---|
| `Queued` | `Running` | Le lot commence. |
| `Queued` | `Cancelled` | Annulé avant d'avoir commencé. Aucun candidat. |
| `Running` | `Completed` | Toutes les graines ont produit un candidat. |
| `Running` | `Failed` | Le lot s'arrête sur une erreur. **Les candidats déjà produits restent.** |
| `Running` | `Cancelled` | L'utilisateur annule. **Les candidats déjà produits restent.** |

**Les trois états terminaux le sont vraiment** : aucune transition n'en sort. Toute autre transition est une erreur de programmation, levée immédiatement, et un test parcourt les vingt-cinq couples possibles pour le vérifier.

Pendant `Running`, le `Job` compte ce qu'il a produit : la liste des identifiants de candidats sauvegardés. `Completed` exige que cette liste ait la longueur demandée ; `Failed` porte un **code d'erreur** et un message ; `Cancelled` ne porte rien d'autre que ce qui a été produit.

### E.3.2 Pourquoi pas d'état « partiellement réussi »

Un lot de cinq dont le troisième échoue est `Failed`, avec deux candidats produits. La tentation est de créer un sixième état — « terminé avec erreurs » — et elle est écartée : **l'information est déjà là**, dans le couple (état, nombre produit), et un état de plus doublerait chaque `switch` d'interface sans rien dire de neuf. Ce qui compte pour l'utilisateur est que les deux candidats sont dans son projet ; le `Job` dit seulement pourquoi il n'y en a pas cinq.

### E.3.3 Un échec arrête le lot

**Décision : le premier échec arrête le lot. On ne passe pas à la graine suivante.**

Les échecs d'un générateur local sont presque toujours **systémiques** : ComfyUI arrêté, modèle absent, mémoire graphique épuisée, workflow refusé. Continuer reproduirait la même erreur N fois, et une erreur par délai d'attente coûte la durée du délai — dix minutes par graine (E.9). Un lot de vingt sur un générateur tombé occuperait trois heures pour produire vingt fois le même message.

Le cas où continuer aurait servi — une image ratée isolée au milieu d'un lot sain — n'existe pas ici : une image que le modèle rate reste une image, et c'est l'utilisateur qui la juge (§D.3.2), pas le client.

### E.3.4 Le `Job` vit en mémoire, et rien ne le persiste

**Décision : un `Job` n'est jamais écrit sur disque, ni dans `project.json` ni ailleurs. Un redémarrage le perd, et c'est sans conséquence.**

Trois raisons, dont la première suffit.

- **Il n'y a rien à faire survivre.** Le critère d'acceptation de T4 — « un lot interrompu conserve les candidats déjà produits » — est tenu par E.4.2, qui sauvegarde chaque candidat dès qu'il existe. Après un redémarrage, le projet contient exactement ce qui a été produit ; le `Job` ne disait rien de plus.
- **Le persister coûterait un `versionSchema`.** DEC-048 : il n'existe pas d'ajout compatible à `project.json`. Un champ `jobs` ferait passer le schéma en version 2 pour une donnée qui n'a de sens que pendant que le processus tourne.
- **Il n'y aurait rien à reprendre.** Un `Job` relu après redémarrage pointerait vers un `prompt_id` de ComfyUI dont l'état est inconnu — exécuté, perdu, ou exécuté et dont le fichier est sur le disque de ComfyUI sans que personne l'ait rapatrié. Reprendre un lot est **relancer un lot**, et l'utilisateur le fait en un clic.

Conséquence assumée : une tâche que ComfyUI exécutait au moment d'un redémarrage de Pawnsmith termine chez ComfyUI, et son image reste dans le dossier de sortie de ComfyUI, que Pawnsmith ne lit jamais. C'est du disque perdu chez le générateur, pas une incohérence chez Pawnsmith.

À consigner en **DEC-074**, qui ferme la question B du chapitre 16.

---

## E.4 Le lot

### E.4.1 Un lot, un prompt, N graines

**Décision : un lot fige ses trois clauses une seule fois, au démarrage. Toutes ses graines partent avec le même prompt.**

Un lot est la réponse à « montre-moi N variantes de ce gabarit ». Les graines sont ce qui varie ; le prompt est ce qui reste. Si l'utilisateur édite la clause sujet pendant que le lot tourne, les candidats suivants sont encore produits sous l'ancienne clause, ils la figent, et ils sont **désalignés** dès leur arrivée — ce qui est exactement vrai, et exactement ce que DEC-030 existe pour dire. L'inverse — relire la clause à chaque graine — produirait un lot dont les candidats ne répondent pas à la même question, sans qu'aucun écran ne puisse le montrer.

Les **graines sont choisies par l'appelant**, et le lot les reçoit en liste. Le lot ne tire aucun nombre au hasard : rejouer une graine précise est un usage légitime, et un cas d'usage qui tirerait ses graines lui-même ne serait pas testable sans substituer son générateur aléatoire. Un utilitaire de l'Application, `RandomSeeds.Draw(count)`, tire des graines uniformes sur `[0, 2^63)` pour qui n'en a pas de préférées.

> **Pourquoi 2^63 et pas 2^64.** Le §3.1 type la graine en `ulong` parce que ComfyUI accepte jusqu'à 2^64 − 1, et ce choix ne bouge pas : une graine arrivant d'ailleurs garde toute sa plage. Le tirage, lui, se contente de 63 bits, parce que c'est ce que `RandomNumberGenerator` rend sans manipulation de bits — et neuf milliards de milliards de graines suffisent.

### E.4.2 Chaque candidat est sauvegardé dès qu'il existe

**Décision : pour chaque graine, dans cet ordre — générer, relire le projet, écrire l'image, ajouter le candidat, sauvegarder. Le candidat suivant ne commence qu'après.**

```
for each seed:
    image   = generator.Generate(prompt, negative, seed)      // long : 30 s à plusieurs minutes
    project = repository.Load(projectDirectory)                // relu, pas gardé en mémoire
    blueprint must still exist, else Failed(BLUEPRINT_NOT_FOUND)
    path    = repository.WritePairedImage(candidateId, image)  // images/{candidateId}-pair.png
    project = project + candidate(seed, frozen clauses, Draft, path)
    repository.Save(projectDirectory, project)
```

Trois points, chacun pour une raison précise.

- **Le projet est relu à chaque graine, jamais gardé en mémoire pendant le lot.** Un lot dure de quelques minutes à une heure ; un projet gardé en mémoire tout ce temps écraserait, à chaque sauvegarde, tout ce que l'utilisateur aura modifié entre-temps — une clause éditée, un autre gabarit ajouté, un candidat élu. Relire avant d'écrire réduit la fenêtre de concurrence à l'intervalle entre la lecture et l'écriture, soit quelques millisecondes au lieu de la durée du lot. **Ce n'est pas un verrou**, et DEC-062 le rappelle : deux écritures simultanées restent non gérées, le dernier écrivain gagne. L'API de T6 sérialisera les écritures d'un même projet ; T4 fait en sorte qu'il n'y ait presque rien à sérialiser.
- **L'image est écrite avant le candidat qui la référence**, jamais après. L'ordre inverse laisserait, pendant un instant ou pour toujours si le processus tombe, un `project.json` pointant vers un fichier absent — la référence pendante que DEC-050 interdit dans une archive et que personne ne veut dans un projet. L'ordre retenu laisse au pire un **fichier orphelin**, et DEC-070 a déjà jugé qu'un orphelin est inoffensif.
- **Une fois l'image reçue, sa sauvegarde n'est plus annulable.** L'annulation porte sur le **lot**, pas sur l'image qui vient de coûter quarante secondes de carte graphique. L'écriture et la sauvegarde reçoivent donc un jeton d'annulation neutre ; l'annulation est observée juste après, avant la graine suivante.

Si le gabarit a disparu à la relecture — supprimé pendant le lot —, le lot s'arrête en `Failed` avec `BLUEPRINT_NOT_FOUND`, et l'image reçue n'est **pas écrite**. Rien ne la référencerait.

À consigner en **DEC-075**.

### E.4.3 Ce qu'un candidat produit contient

| Champ | Valeur |
|---|---|
| `id` | Nouvel identifiant, tiré à la réception de l'image |
| `seed` | La graine du lot |
| `framingClauseUsed`, `subjectClauseUsed`, `styleClauseUsed` | Les trois clauses **figées au démarrage du lot**, normalisées |
| `status` | `Draft` — l'utilisateur n'a encore rien jugé |
| `pairedImageFile` | `images/{id}-pair.png`, nom choisi par Pawnsmith et jamais par le générateur |
| `frontImageFile`, `backImageFile` | `null` — T5 |
| `generatedAt` | L'instant de réception, en UTC, tronqué à la seconde comme tout horodatage du fichier (C.3.3) |

Le candidat est ajouté **en dernier** dans la liste du gabarit, et rien d'autre ne bouge : ni l'élu, ni le statut d'aucun autre candidat, ni la clause sujet (DEC-068). Un candidat neuf n'est jamais désaligné à sa création — ses clauses figées sont les clauses courantes —, et un test le vérifie.

### E.4.4 Ce qui est refusé avant que le lot ne commence

Deux refus, levés **avant** qu'aucun `Job` n'existe et qu'aucun appel ne parte : un lot vide ou plus grand que la borne de MEN-007 (`BATCH_SIZE_INVALID`, E.9), et un gabarit inconnu (`BLUEPRINT_NOT_FOUND`, existant depuis T3). Une requête mal formée n'est pas un travail qui échoue : c'est un travail qui n'a jamais existé.

---

## E.5 Le template de workflow — question F

ComfyUI n'accepte pas un prompt mais un **graphe de workflow** au format JSON (§6.4 de la bible). L'application stocke un template de ce graphe et y substitue ce qui varie. DEC-029 en fait le **seul point d'accès** à la clause de cadrage ; DEC-073 en a laissé le schéma à T4.

### E.5.1 Le fichier

Un fichier par générateur, lu et jamais écrit par l'application : `config/workflow.comfyui.json`.

```json
{
  "versionSchema": 1,
  "framingClause": [
    "Character rotation sheet for a miniature reference: the exact same character",
    "drawn twice in one single image, front view on the left and back view on the right, ..."
  ],
  "outputNodeId": "9",
  "workflow": {
    "3": {
      "class_type": "KSampler",
      "inputs": { "seed": "{{SEED}}", "steps": 8, "cfg": 1.0, "...": "..." }
    },
    "6": { "class_type": "CLIPTextEncode", "inputs": { "text": "{{POSITIVE}}", "clip": ["11", 0] } },
    "7": { "class_type": "CLIPTextEncode", "inputs": { "text": "{{NEGATIVE}}", "clip": ["11", 0] } },
    "9": { "class_type": "SaveImage", "inputs": { "filename_prefix": "pawnsmith", "images": ["8", 0] } }
  }
}
```

| Clé | Type | Obligatoire | Notes |
|---|---|---|---|
| `versionSchema` | entier | oui | Vaut `1`. Rejet d'une version plus récente, comme en C.6 |
| `framingClause` | tableau de chaînes | oui | Les lignes de la clause de cadrage, **jointes par `\n`** puis normalisées. Non vide après normalisation |
| `outputNodeId` | chaîne | oui | Identifiant, dans `workflow`, du nœud dont l'image est rapatriée |
| `workflow` | objet | oui | Le graphe au **format API** de ComfyUI, tel qu'exporté, avec ses jetons |

**`framingClause` est un tableau de lignes, pas une chaîne**, parce que c'est un texte de plusieurs centaines de caractères qu'un utilisateur averti réécrit à la main. Une chaîne JSON d'un seul tenant ne se relit pas ; un tableau se relit ligne à ligne et se diffe. La jointure par `\n` est celle de l'assemblage (§C.5.3), si bien qu'un saut de ligne écrit dans le fichier arrive au modèle à l'identique.

Le régime de lecture est celui de `calibration.json` et des fichiers de T3 : commentaires et virgules finales tolérés, membre inconnu **ignoré** au niveau du fichier. Le graphe, lui, est transmis tel quel : Pawnsmith n'en interprète que les jetons et le nœud de sortie, et c'est **ComfyUI qui valide le reste** — un graphe faux est refusé par le générateur avec `GENERATOR_REJECTED`, au premier lot, avec son propre message.

> **Format API, pas format interface.** ComfyUI exporte un workflow sous deux formes. Le format de l'interface porte des tableaux `nodes` et `links` ; le format API est un objet indexé par identifiant de nœud, et c'est le seul que `/prompt` accepte. Un graphe portant `nodes` et `links` est refusé à la lecture avec un message qui dit quoi faire — « exporter avec *Export (API)* » —, parce que c'est l'erreur la plus probable du premier essai et qu'elle se découvrirait sinon sous la forme d'un refus opaque du générateur.

### E.5.2 Les jetons : une liste close, et un jeton est une valeur entière

| Jeton | Remplacé par | Type JSON produit | Occurrences |
|---|---|---|---|
| `{{POSITIVE}}` | Le prompt résolu, `ResolvedPrompt.From(cadrage, sujet, style)` | chaîne | **exactement une** |
| `{{SEED}}` | La graine du candidat | nombre | **exactement une** |
| `{{NEGATIVE}}` | La clause négative du style du projet | chaîne | **zéro ou une** |

Trois règles, chacune fermant un défaut précis.

- **Un jeton occupe une valeur de chaîne entière.** `"text": "{{POSITIVE}}"` est un jeton ; `"text": "{{POSITIVE}}, masterpiece"` est **refusé** à la lecture. Un jeton noyé dans un texte ferait envoyer au modèle autre chose que le prompt résolu, et DEC-049 exige que ce qui part soit **exactement** l'assemblage des trois clauses figées. L'écrire dans le template serait une transformation silencieuse du prompt ; la refuser à la lecture la rend impossible.
- **`{{POSITIVE}}` et `{{SEED}}` apparaissent exactement une fois.** Zéro, et le prompt ou la graine ne partent pas — une génération dont on ne peut pas dire ce qu'elle a reçu. Deux, et le graphe porte deux encodages du prompt ou deux échantillonneurs dont on ne sait lequel produit l'image rapatriée. `{{NEGATIVE}}` peut manquer : avec Krea 2 Turbo à CFG 1,0, le prompt négatif est sans effet (DEC-043), et un workflow qui n'a pas de nœud négatif est légitime.
- **Tout autre `{{…}}` est refusé en le nommant** (`WORKFLOW_UNKNOWN_TOKEN`), comme un jeton inconnu du template de prompt en T3 (DEC-073). Écrire `{{STEPS}}` en croyant que Pawnsmith le remplit doit échouer au chargement, pas partir au générateur en texte littéral.

**Pourquoi `{{WIDTH}}` et `{{HEIGHT}}` ne sont pas des jetons**, alors que le §6.4 de la bible les annonçait. Les dimensions de l'image sont un **réglage du workflow**, que l'utilisateur écrit directement dans son graphe, et Pawnsmith n'a aucune raison de les imposer : la découpe (E.8) et le détourage lisent les dimensions **de l'image reçue**, jamais celles qui ont été demandées. Un jeton de dimension obligerait à porter la valeur à deux endroits — un champ du fichier et un nœud du graphe —, c'est-à-dire à créer exactement la divergence qu'un fichier unique évite. La liste du §6.4 est corrigée par **DEC-076**.

### E.5.3 La substitution se fait sur le graphe, jamais sur le texte

```
Substitute(template, prompt, negative, seed) =
    graph = DeepClone(template.Workflow)
    for each token position recorded at load time:
        graph[position] = JsonValue(prompt | negative | seed)
    return graph
```

- **Les positions des jetons sont relevées au chargement**, une fois, sur le template. La substitution remplace les nœuds situés à ces positions, et seulement eux. Elle **ne parcourt jamais** une valeur qu'elle vient d'insérer : un prompt contenant littéralement `{{SEED}}` part littéralement, et la graine ne s'y glisse pas. C'est la leçon du correctif de `TemplateToken.Substitute` en T3, appliquée d'emblée.
- **Le graphe est cloné à chaque génération.** Le template lu au démarrage n'est jamais modifié ; deux générations successives ne peuvent pas se contaminer.
- **Le prompt est inséré comme une valeur JSON**, et c'est le sérialiseur qui l'échappe. Un guillemet, une barre oblique inverse, un saut de ligne ou un caractère accentué deviennent `"`, `\\`, `\n`, `é` dans le texte transmis, et ComfyUI les décode à l'identique. **L'échappement n'est pas une transformation au sens de DEC-049** : c'est l'encodage du transport, et le texte que le modèle reçoit est octet pour octet celui que le candidat fige. Un test le vérifie avec un prompt qui contient tous ces caractères à la fois.

### E.5.4 Le fichier livré est un exemple, et il le dit

Le workflow réel du porteur n'est pas dans le dépôt, et ne doit pas y être : il dépend des noms de fichiers de modèles de **sa** machine. Le dépôt livre **`config/workflow.comfyui.example.json`**, construit d'après les paramètres consignés dans DEC-043 — Krea 2 Turbo, encodeur Qwen3-VL, euler / simple, 8 étapes, CFG 1,0, 1216 × 832 —, et marqué comme exemple **trois fois** : par son nom de fichier, par un champ `_readme` en tête (ignoré à la lecture), et dans `config/README.md`.

**Ce fichier n'a jamais été soumis à un ComfyUI réel.** Les noms de types de nœuds y sont plausibles, pas vérifiés. Le chemin attendu est : exporter son propre workflow avec *Export (API)*, y remplacer les trois valeurs par les trois jetons, l'envelopper dans le schéma de E.5.1, l'enregistrer sous `config/workflow.comfyui.json`. L'application **ne retombe jamais sur l'exemple** : un fichier absent est une configuration absente, pas une invitation à deviner.

### E.5.5 L'ordre des clauses a bougé par rapport à T0a, et c'est à vérifier

Le prompt de référence de DEC-043 plaçait la ligne `Subject: {SUJET}` **au milieu** du texte de cadrage. L'assemblage de T2, lui, est fixé — cadrage, sujet, style, dans cet ordre (§C.5.3) — et c'est une surface de compatibilité sous fiche. Le cadrage de l'exemple porte donc tout le texte de référence d'un seul tenant, et le sujet vient **après**.

L'exemple termine sa clause de cadrage par la ligne `Subject:`, si bien que le sujet arrive sur la ligne suivante, étiqueté comme dans T0a. Ce n'est pas pour autant le prompt que T0a a validé : tout le texte qui suivait le sujet le précède désormais. L'effet de l'ordre sur un encodeur Qwen3-VL est probablement faible, mais il n'est pas mesuré. **À vérifier au premier lot réel**, sur un des trois sujets de T0a, avant de juger quoi que ce soit d'autre. Si l'ordre dégrade le résultat, la réponse n'est pas de changer l'assemblage — ce qui désalignerait tout — mais de réécrire le texte de cadrage pour qu'il se lise bien suivi du sujet.

À consigner en **DEC-076**, qui ferme la question F du chapitre 16.

---

## E.6 Ce qui part au générateur, et ce qui est figé

| Ce qui part | D'où | Figé sur le candidat ? |
|---|---|---|
| `{{POSITIVE}}` | `ResolvedPrompt.From(cadrage, sujet, style)`, les trois clauses figées au démarrage du lot | **Oui**, clause par clause (DEC-049) |
| `{{SEED}}` | La graine du lot | **Oui**, `seed` |
| `{{NEGATIVE}}` | `style.negativeClause`, normalisée | **Non** |
| Le reste du graphe | Le template de workflow | **Non** |

**La contrainte de DEC-049 est tenue à la lettre** : T4 n'applique au prompt résolu aucune substitution, troncature ou réécriture. Ce qui part est `ResolvedPrompt.From` des trois clauses que le candidat fige, et un test le vérifie **sur ce que le faux serveur a reçu**, pas sur ce que le client croyait envoyer.

**La clause négative n'est pas figée, et c'est délibéré.** DEC-028 définit le prompt résolu comme l'assemblage de **trois** clauses, et la négative n'en est pas une : elle n'entre pas dans le désalignement. La figer ajouterait un quatrième champ au candidat, donc un `versionSchema` de plus (DEC-048), pour une valeur que le modèle en usage **ignore** — à CFG 1,0, le guidage négatif est neutralisé. Si un modèle futur la rendait signifiante, la décision se rouvrira avec une mesure sous les yeux.

Le **reste du graphe** n'est pas figé non plus : changer le nombre d'étapes ou le modèle dans le workflow ne désaligne rien. C'est la limite du mécanisme, et elle était déjà écrite : DEC-049 fige des clauses de prompt, pas un environnement d'exécution. Seule la clause de cadrage, parce qu'elle vit dans ce fichier et entre dans le prompt, désaligne quand on la touche — c'est le « piège du cadrage » que DEC-049 a nommé.

À consigner en **DEC-077**.

---

## E.7 Le port et son adaptateur ComfyUI

### E.7.1 Un seul port : `IImageGenerator`

```csharp
public interface IImageGenerator
{
    Task<GeneratorAvailability> CheckAsync(CancellationToken cancellationToken);
    Task<GeneratedImage> GenerateAsync(GenerationRequest request, CancellationToken cancellationToken);
}

public sealed record GenerationRequest(string Prompt, string NegativePrompt, ulong Seed);
public sealed record GeneratedImage(byte[] Png, int WidthPx, int HeightPx);
public enum GeneratorAvailability { Available, Unreachable, Unhealthy }
```

**`CheckAsync` ne lève jamais pour un générateur absent.** Le critère d'acceptation du chapitre 12 le demande en propres termes : « générateur injoignable géré comme un état normal ». Un générateur éteint est l'état ordinaire d'un poste où ComfyUI n'est pas lancé ; c'est une valeur de retour, pas une exception. `Unhealthy` désigne un serveur qui répond, mais pas comme ComfyUI — une page d'erreur, un autre service sur le même port.

**`GenerateAsync` lève une `GeneratorException` portant un code** (E.11). Le cas d'usage la rattrape et en fait l'état `Failed` du `Job` : vu de l'extérieur du lot, un générateur injoignable n'est **toujours pas** une exception — c'est un `Job` terminé avec un code.

**`IPawnPairProducer` n'est pas écrit.** Le chapitre 7 le dessine comme « point de substitution du choix DEC-003 », et DEC-003 l'invoquait au cas où T0a montrerait que le modèle ne sait pas produire une planche de rotation — il aurait alors fallu une implémentation dégradée, deux générations à graine partagée. **T0a a montré le contraire** (DEC-043). Le port protégeait d'un risque qui ne s'est pas réalisé ; l'écrire aujourd'hui serait une interface à implémentation unique, pour toujours ou jusqu'à EVO-009. Et EVO-009 — le passage par la 3D — ne produirait **pas** d'image jumelée : la signature dessinée aujourd'hui serait fausse ce jour-là, et le schéma du candidat avec elle. La production du couple est donc un **cas d'usage** de l'Application, appuyé sur `IImageGenerator`, et c'est ce port qui reste le point de substitution du fournisseur (EVO-002).

À consigner en **DEC-078**, qui supersède `IPawnPairProducer` au chapitre 7 et la dernière phrase du §4.2.

### E.7.2 Le protocole, tel que ComfyUI le parle

Pas de WebSocket. Le suivi se fait par **interrogation périodique** de l'historique : c'est quatre appels HTTP ordinaires, testables contre un faux serveur écrit à la main, et la granularité d'une seconde est sans importance pour une génération de quarante.

| # | Appel | Réponse attendue | Ce qu'on en fait |
|---|---|---|---|
| 1 | `POST /prompt` — `{ "prompt": graphe, "client_id": … }` | `200` et `{ "prompt_id": "…" }` | `4xx` → `GENERATOR_REJECTED` ; `5xx` → `GENERATOR_FAILED` |
| 2 | `GET /history/{prompt_id}`, toutes les `PollInterval` | `{}` tant que rien n'est fini ; puis `{ "{prompt_id}": { "status": …, "outputs": … } }` | `status.status_str == "error"` → `GENERATOR_FAILED` |
| 3 | Lecture de `outputs[outputNodeId].images` | **exactement une** image | zéro ou plusieurs → `GENERATOR_OUTPUT_INVALID` |
| 4 | `GET /view?filename=…&subfolder=…&type=…` | les octets du PNG | bornés et vérifiés, E.9 |
| — | `GET /system_stats` | `200` et un objet JSON | c'est `CheckAsync` |

**Le nom de fichier rendu par ComfyUI ne sert qu'à le lui redemander.** Il voyage dans la chaîne de requête de `/view`, encodé, et n'est **jamais** utilisé comme chemin local : le fichier du projet s'appelle `{candidateId}-pair.png`, nom choisi par Pawnsmith. Un générateur hostile qui répondrait `../../etc/passwd` obtiendrait au pire de se faire redemander ce nom par HTTP. C'est MEN-002 appliqué à une entrée qui vient du réseau.

**Une seule image exactement.** Un workflow dont le nœud de sortie rend plusieurs images — taille de lot latente supérieure à 1 — est refusé à la génération plutôt que tronqué à la première. Garder la première jetterait le travail des autres en silence ; les garder toutes ferait d'une graine plusieurs candidats, ce que le modèle ne sait pas représenter.

### E.7.3 L'annulation va jusqu'au générateur

Une annulation pendant `GenerateAsync` fait deux appels **au mieux**, avant de rendre la main :

1. `POST /queue` avec `{ "delete": [prompt_id] }` — retire la tâche si elle attend encore son tour ;
2. `POST /interrupt` avec `{ "prompt_id": prompt_id }` — interrompt l'exécution si c'est elle qui tourne.

Les mêmes deux appels partent quand c'est le **délai de génération** qui expire : une tâche que Pawnsmith a abandonnée n'a pas plus de raison d'occuper la carte graphique qu'une tâche annulée.

**Au mieux** veut dire : avec un délai propre et court — celui de l'état de santé, `CheckTimeout`, ces appels étant aussi petits —, indépendant du jeton annulé — sinon les deux appels seraient annulés avant de partir —, et sans que leur échec masque l'annulation. Sans eux, un lot annulé côté Pawnsmith continuerait d'occuper la carte graphique pendant toute sa génération en cours, et l'utilisateur relançant aussitôt un lot attendrait sans comprendre.

> **Une limite de version, à connaître.** Les versions anciennes de ComfyUI ignorent le corps de `/interrupt` et interrompent **l'exécution en cours, quelle qu'elle soit**. Sur un poste mono-utilisateur, c'est presque toujours la tâche de Pawnsmith ; si l'utilisateur génère en même temps depuis l'interface de ComfyUI, il peut perdre la sienne. Le risque est accepté et documenté, pas contourné.

### E.7.4 Le client HTTP

- **Aucune redirection n'est suivie.** Une réponse `3xx` est un échec. Suivre une redirection rendrait inutile toute validation de l'adresse du générateur : le serveur validé désignerait lui-même la cible suivante (E.10).
- **Aucun proxy n'est utilisé**, même si l'environnement en déclare un. Le générateur est local ou sur le réseau domestique (DEC-007) ; faire transiter les prompts par un proxy, c'est les envoyer à un tiers.
- **Chaque requête a son délai** (`RequestTimeout`), et la génération entière a le sien (`GenerationTimeout`). Le premier attrape un serveur qui ne répond plus ; le second, un serveur qui répond « pas encore » pour toujours.
- **Le corps de chaque réponse est lu avec une borne**, et la borne est vérifiée pendant la lecture, pas après : un `Content-Length` absent ou mensonger ne doit pas suffire à faire tenir un gigaoctet en mémoire.

---

## E.8 La découpe — décidée en T4, exécutée en T5

### E.8.1 La règle

```
PairSplit.Of(widthPx, heightPx):
    half  = floor(widthPx / 2)
    front = rectangle(x = 0,               y = 0, width = half, height = heightPx)
    back  = rectangle(x = widthPx − half,  y = 0, width = half, height = heightPx)
```

- **La vue de face est à gauche, la vue de dos à droite.** C'est ce que la clause de cadrage demande (« front view on the left and back view on the right », DEC-043), et ce que T0a a obtenu sur trois sujets sur trois. La règle ne cherche pas à le détecter : elle le **suppose**, parce que c'est le cadrage qui le garantit, et une détection qui se tromperait inverserait le recto et le verso d'un pion sans que rien ne le signale.
- **La découpe est un partage vertical au milieu exact.** Pas de recherche du « vrai » milieu entre les deux silhouettes : T0a a mesuré 0 à 1,4 % d'écart d'alignement entre les deux vues, et le détourage de T5 retire de toute façon le fond de part et d'autre.
- **Une largeur impaire perd la colonne du milieu**, et les deux moitiés ont la même largeur. L'autre choix — donner le pixel en trop à l'une des deux — ferait différer d'un pixel la largeur des deux vues d'un même personnage ; DEC-041 vient précisément d'établir que le couple partage une échelle unique, et deux sources de largeurs différentes y entreraient avec un biais qu'aucune image ne justifie.
- **Une image de moins de deux pixels de large n'est pas découpable**, et elle est refusée à la réception (`GENERATOR_OUTPUT_INVALID`).

### E.8.2 Pourquoi les pixels ne sont pas découpés ici

Le chapitre 12 range « découpe » dans T4. La **règle** y est ; son **exécution**, non. Quatre raisons, dans l'ordre de leur poids.

1. **Le schéma n'a pas de place pour des moitiés non détourées.** Le candidat porte trois fichiers : l'image jumelée, et deux PNG **à fond transparent** (§3.1). Écrire les moitiés brutes dans `frontImageFile` et `backImageFile` les rendrait **élisibles** — DEC-071 n'exige que leur présence — et une planche imprimerait deux rectangles de fond gris. Leur donner deux champs à elles coûterait un `versionSchema` (DEC-048), pour des fichiers que personne ne consomme.
2. **Le seul consommateur des moitiés est le détourage**, qui décode les pixels de toute façon pour les donner au modèle de segmentation. Les découper en T4 pour les relire en T5 ferait un aller-retour disque par candidat, sans lecteur entre les deux.
3. **Découper des pixels suppose de décoder un PNG.** C'est une bibliothèque d'image — donc un **choix de licence**, que le porteur s'est réservé avec celui du modèle de détourage de T5 — ou trois cents lignes de décodeur écrites à la main, probablement remplacées par cette bibliothèque deux semaines plus tard.
4. **La règle, elle, n'attend rien.** Elle est géométrique, pure, et elle se teste sans une seule image. L'écrire en T4 fixe la réponse à la question que T5 se poserait, à l'endroit où le cadrage qui la garantit est écrit.

T4 utilise la règle à la réception, pour refuser une image non découpable. T5 l'appellera pour découper.

> **Ce que T5 doit savoir, et qui naît ici.** ComfyUI inscrit le **graphe et le prompt** dans les métadonnées de chaque PNG qu'il enregistre — des blocs `tEXt` nommés `prompt` et `workflow`. L'image jumelée les porte donc, et c'est sans conséquence : le profil `Share` la retire de l'archive (DEC-050). Les moitiés détourées, elles, **partent** dans une archive `Share`. Elles devront être **réencodées** — ce que tout passage par un modèle de segmentation fait naturellement —, et jamais produites par une copie de blocs qui emporterait les métadonnées de l'image source.

À consigner en **DEC-079**.

---

## E.9 Les bornes — moitié T4 de la question G

Ces valeurs **s'arbitrent, elles ne se mesurent pas** (DEC-057) : la règle « les valeurs physiques ne s'inventent jamais » ne s'y applique pas, et elles ne sont pas marquées `À CALIBRER`. Elles vivent dans deux **records d'options**, déclarées une fois chacune, et n'apparaissent en littéral dans aucun code qui les applique.

| Borne | Valeur | Où | Motif |
|---|---|---|---|
| `MaxBatchSize` | **20** | `GenerationOptions` (Application) | MEN-007. Vingt générations de quarante secondes font un quart d'heure, ce qu'un utilisateur lance en connaissance de cause. Au-delà, un clic malheureux occupe la carte graphique pour la soirée |
| `GenerationTimeout` | **10 min** | `ComfyUiOptions` (Infrastructure) | Une génération mesure 34 à 42 s (DEC-043), mais le **premier** lot après le démarrage de ComfyUI charge un modèle de 12 milliards de paramètres. Dix minutes couvrent un chargement lent sans laisser un générateur bloqué occuper un lot indéfiniment |
| `RequestTimeout` | **30 s** | idem | Délai d'**un** appel HTTP. Aucune réponse de ComfyUI n'est longue à produire : l'attente est dans la file, pas dans l'appel |
| `CheckTimeout` | **5 s** | idem | Un état de santé qui met plus de cinq secondes à répondre est une réponse |
| `PollInterval` | **1 s** | idem | Granularité du suivi. Une seconde de retard sur quarante est invisible ; cent requêtes par seconde ne le seraient pas pour ComfyUI |
| `MaxImageBytes` | **64 Mio** | idem | Un PNG de 1216 × 832 en RGB pèse 1 à 2 Mo (DEC-050). Quarante fois cette taille couvre une résolution quadruple sans laisser un serveur hostile remplir la mémoire |
| `MaxImageDimensionPx` | **8192** | idem | MEN-005, première couche : vérifiée sur l'en-tête, **avant** tout décodage. Une image de 8192 × 8192 décodée en RGBA pèse déjà 256 Mio |
| `MaxResponseJsonBytes` | **16 Mio** | idem | L'historique d'une tâche contient le graphe entier ; c'est le plus gros JSON que ComfyUI renvoie, et il dépasse rarement quelques centaines de kilo-octets |

La moitié restante de la question G — dimensions maximales en entrée du détourage, durée acceptable d'un détourage sur processeur — appartient à T5.

À consigner en **DEC-080**, qui scinde la question G.

---

## E.10 Sécurité — ce que T4 ouvre

Le chapitre 9 se revoit **à chaque tranche qui ouvre une surface** (DEC-054). T4 en ouvre deux : une adresse de serveur fournie par l'utilisateur, et des octets arrivant de ce serveur.

### E.10.1 MEN-003 : l'adresse du générateur

Le chapitre 9 prescrit « liste blanche de schémas et de ports ». **La moitié « ports » est écartée, et la menace est traitée à la racine.**

**Décision : l'adresse du générateur est un réglage de déploiement — fichier de configuration ou variable d'environnement —, jamais une valeur modifiable par l'API.** Une SSRF suppose qu'un attaquant **choisisse** l'adresse que le serveur appelle. Une adresse que seul l'opérateur écrit, dans un fichier de son propre poste, n'est choisie par personne d'autre ; filtrer des ports que l'opérateur a lui-même écrits ne protège de rien, et casserait le jour où ComfyUI tourne sur un port non standard.

Ce qui est vérifié, et pourquoi chaque règle existe :

| Règle | Motif |
|---|---|
| URI absolue, schéma `http` ou `https` | Ferme `file:`, `ftp:` et les schémas que le client HTTP saurait peut-être ouvrir |
| Pas d'identifiants dans l'URI (`user:pass@`) | Un secret dans une adresse finit dans un journal (MEN-006, chapitre 8) |
| Pas de requête ni de fragment | Les appels ajoutent leur propre chemin et leur propre requête ; une adresse qui en porte déjà serait concaténée de travers |
| **Redirections jamais suivies** (E.7.4) | Sans cela, toute validation de l'adresse est contournée par le serveur validé lui-même |
| **Proxy jamais utilisé** (E.7.4) | Les prompts ne sortent pas du réseau domestique |

Une adresse refusée l'est avec `GENERATOR_URL_INVALID`, au démarrage, pas au premier lot.

À consigner en **DEC-081**, qui corrige la contre-mesure de MEN-003 au chapitre 9.

### E.10.2 MEN-005 : ce qui arrive du générateur

La première couche de MEN-005 est tenue en T4, la seconde reste à T5.

- **Avant d'écrire une image**, le client vérifie la signature PNG, la présence du bloc `IHDR` à sa place, et les dimensions qu'il déclare contre `MaxImageDimensionPx`. Rien n'est décodé : ce sont vingt-quatre octets lus à un décalage fixe, comme `FileImageSizeReader` le fait depuis T1.
- **Avant de lire un corps**, la borne de taille est armée ; elle est vérifiée pendant la lecture.
- **Rien de ce qui est refusé n'est écrit.** Une image refusée ne laisse aucun fichier, pas même pour diagnostic : le critère « l'image jumelée brute est conservée » vaut pour une image **valide**, que l'utilisateur juge. Conserver un fichier hostile pour diagnostic, c'est l'avoir accepté.

La seconde couche — plafonds vérifiés avant **décodage** par le détourage — est celle de T5, qui sera le premier code de Pawnsmith à décoder une image.

---

## E.11 Codes d'erreur

Ajoutés à ceux du §C.11 et du §D.10. Rendus par l'API, jamais traduits (chapitre 10).

| Code | Levé par | Quand |
|---|---|---|
| `GENERATOR_UNREACHABLE` | Infrastructure | Connexion refusée, nom introuvable, connexion coupée |
| `GENERATOR_TIMEOUT` | Infrastructure | Un appel dépasse `RequestTimeout`, ou la génération dépasse `GenerationTimeout` |
| `GENERATOR_REJECTED` | Infrastructure | `POST /prompt` répond `4xx` : graphe invalide, nœud inconnu, modèle absent |
| `GENERATOR_FAILED` | Infrastructure | L'exécution se termine en erreur, ou le générateur répond `5xx` ou `3xx` |
| `GENERATOR_OUTPUT_INVALID` | Infrastructure | Réponse JSON illisible ou trop grosse ; zéro ou plusieurs images ; image non PNG, trop lourde, trop grande, ou non découpable |
| `WORKFLOW_INVALID` | Infrastructure | Fichier de workflow malformé, champ absent, jeton manquant ou répété, jeton noyé dans un texte, nœud de sortie absent, format interface au lieu du format API |
| `WORKFLOW_UNKNOWN_TOKEN` | Infrastructure | Un `{{…}}` hors de la liste close, nommé |
| `WORKFLOW_SCHEMA_TOO_RECENT` | Infrastructure | `versionSchema` du workflow supérieur à 1 |
| `GENERATOR_URL_INVALID` | Infrastructure | Adresse du générateur refusée par E.10.1 |
| `BATCH_SIZE_INVALID` | Application | Lot vide, ou plus grand que `MaxBatchSize` |

Les cinq `GENERATOR_*` du haut sont aussi les **codes d'échec d'un `Job`** : le cas d'usage les recopie dans l'état `Failed`. S'y ajoutent `BLUEPRINT_NOT_FOUND` (gabarit supprimé pendant le lot) et `JOB_UNEXPECTED_ERROR`, réservé à ce qu'aucun code ne décrit — une écriture disque impossible, par exemple. Ce dernier existe parce qu'un lot tourne en arrière-plan à partir de T6, et qu'un `Job` qui disparaîtrait sur une exception non prévue laisserait l'interface attendre pour toujours.

Une exception venue d'une couche que l'Application ne connaît pas — un `PROJECT_INVALID` levé par la relecture du projet, par exemple — **garde son code** dans l'état `Failed`. Pour cela, toutes les exceptions codées du dépôt implémentent une interface d'Application à une propriété, `ICodedException.WireCode`. C'est la seule façon pour l'Application de lire le code d'une exception d'Infrastructure sans référencer l'Infrastructure (A.3), et l'API de T6 s'en servira pour la même raison.

---

## E.12 Tests attendus

Quarante-deux tests, groupés par sujet. Les numéros sont stables et servent de référence dans les messages de commit.

### Machine à états du `Job` (1 à 6)

| # | Ce qu'il vérifie |
|---|---|
| 1 | Un `Job` naît `Queued`, sans rien de produit |
| 2 | Les cinq transitions de E.3.1 sont acceptées |
| 3 | Les vingt autres couples (état, transition) sont refusés ; les trois états terminaux n'ont aucune sortie |
| 4 | Un candidat ne s'enregistre qu'en `Running`, et dans l'ordre de production |
| 5 | `Completed` exige que tout ce qui était demandé ait été produit |
| 6 | `Failed` porte un code et un message ; `Cancelled` n'en porte pas |

### Découpe (7 à 9)

| # | Ce qu'il vérifie |
|---|---|
| 7 | Largeur paire : deux moitiés égales, la face à gauche |
| 8 | Largeur impaire : la colonne du milieu est perdue, les deux moitiés restent égales |
| 9 | Une largeur inférieure à 2 est refusée |

### Template de workflow (10 à 19)

| # | Ce qu'il vérifie |
|---|---|
| 10 | Un fichier valide se lit ; la clause de cadrage est ses lignes jointes par `\n`, normalisées |
| 11 | `versionSchema` supérieur à 1 est rejeté par `WORKFLOW_SCHEMA_TOO_RECENT` |
| 12 | `{{POSITIVE}}` absent, ou présent deux fois, est rejeté |
| 13 | `{{SEED}}` absent, ou présent deux fois, est rejeté |
| 14 | `{{NEGATIVE}}` absent est accepté ; présent deux fois, rejeté |
| 15 | Un jeton inconnu est rejeté par `WORKFLOW_UNKNOWN_TOKEN`, **en le nommant** |
| 16 | Un jeton noyé dans un texte plus long est rejeté |
| 17 | Un nœud de sortie absent du graphe, une clause de cadrage vide, un graphe au format interface sont rejetés |
| 18 | La substitution transmet un prompt contenant guillemets, barres obliques inverses, sauts de ligne, accents **et** le texte littéral `{{SEED}}`, à l'identique ; la graine part en nombre |
| 19 | Deux substitutions successives ne se contaminent pas : le template n'est jamais modifié |

### Client ComfyUI, contre un faux serveur (20 à 31)

| # | Ce qu'il vérifie |
|---|---|
| 20 | `CheckAsync` rend `Available` sur un serveur qui répond |
| 21 | `CheckAsync` rend `Unreachable` sur un port fermé, **sans lever** |
| 22 | `GenerateAsync` soumet le graphe substitué, interroge l'historique, rapatrie l'image du nœud de sortie |
| 23 | Port fermé : `GENERATOR_UNREACHABLE` |
| 24 | `/prompt` en `400` : `GENERATOR_REJECTED` |
| 25 | Historique en erreur d'exécution : `GENERATOR_FAILED` |
| 26 | Aucun résultat dans le délai : `GENERATOR_TIMEOUT` |
| 27 | Zéro image, deux images, ou des octets qui ne sont pas un PNG : `GENERATOR_OUTPUT_INVALID` |
| 28 | Une image plus lourde que la borne, ou plus grande que la borne de dimensions, est refusée **sur son en-tête** |
| 29 | L'annulation retire la tâche de la file et l'interrompt **par son identifiant**, puis rend la main |
| 30 | Une redirection n'est pas suivie |
| 31 | Une adresse `ftp:`, porteuse d'identifiants, ou porteuse d'une requête est refusée par `GENERATOR_URL_INVALID` |

### Lot (32 à 42)

| # | Ce qu'il vérifie |
|---|---|
| 32 | Un lot de trois produit trois candidats `Draft`, dans l'ordre des graines, et finit `Completed` |
| 33 | Les clauses figées sont les clauses courantes ; un candidat neuf **n'est pas désaligné** |
| 34 | Ce que le générateur a reçu est exactement `ResolvedPrompt.From` des trois clauses figées (DEC-049) ; la négative est celle du style |
| 35 | Un échec au troisième candidat finit `Failed` avec son code, et **les deux premiers sont dans le projet relu** |
| 36 | Une annulation après le premier candidat finit `Cancelled`, avec ce seul candidat, et aucune image de trop |
| 37 | Un générateur injoignable finit `Failed` avec `GENERATOR_UNREACHABLE`, sans lever et sans rien écrire |
| 38 | Un gabarit supprimé pendant le lot finit `Failed` avec `BLUEPRINT_NOT_FOUND`, **sans fichier orphelin** |
| 39 | Un lot vide ou trop grand est refusé par `BATCH_SIZE_INVALID` avant le moindre appel |
| 40 | Une modification du projet faite **pendant** le lot n'est pas écrasée par la sauvegarde du candidat suivant |
| 41 | L'élu et les statuts des candidats existants ne bougent pas |
| 42 | De bout en bout, contre le faux serveur et le vrai dépôt : `project.json` se relit, l'image sur disque est octet pour octet celle que le serveur a rendue |

---

## E.13 Critères d'acceptation

T4 est terminée quand :

- [ ] Les **42** tests de E.12 passent.
- [ ] **Générateur injoignable géré comme un état normal** : `CheckAsync` ne lève pas, et un lot sur un générateur éteint finit `Failed` sans exception (tests 21 et 37).
- [ ] **Un lot interrompu conserve les candidats déjà produits**, par échec comme par annulation (tests 35 et 36).
- [ ] **L'image jumelée brute est conservée pour diagnostic**, sous un nom choisi par Pawnsmith (tests 32 et 42).
- [ ] Ce qui part au générateur est **exactement** `ResolvedPrompt.From` des trois clauses figées (test 34, DEC-049).
- [ ] Aucune substitution textuelle sur le JSON du workflow ; un prompt contenant un jeton littéral part littéral (test 18).
- [ ] Aucune borne de E.9 en littéral dans le code qui l'applique.
- [ ] `versionSchema` de `project.json` **reste à 1** : T4 n'ajoute aucun champ au schéma.
- [ ] `Pawnsmith.Domain.csproj` ne référence toujours rien ; **aucune dépendance NuGet ajoutée**.
- [ ] Aucun binaire commité : les images de test sont fabriquées par le code des tests.
- [ ] Le CLI lance un lot et en affiche les transitions, **sans contenir de logique**.
- [ ] Les fiches DEC de E.15 sont écrites au chapitre 11 de la bible.
- [ ] L'intégration continue est verte.
- [ ] Le code est relu intégralement (DEC-027).

---

## E.14 Ce que T4 ne fait pas

| T4… | Parce que |
|---|---|
| …ne découpe pas les pixels de l'image jumelée | E.8.2, DEC-079 — T5 |
| …n'écrit ni `frontImageFile` ni `backImageFile` | T5 ; DEC-071 les rendrait élisibles |
| …ne persiste aucun `Job` | E.3.4, DEC-074 |
| …n'héberge aucun `Job` en arrière-plan, ni file d'attente | T6 — c'est l'hôte de l'API qui en a besoin |
| …n'écrit pas `IPawnPairProducer` | E.7.1, DEC-078 |
| …ne fige ni la clause négative ni le graphe | E.6, DEC-077 |
| …ne suit pas la progression par WebSocket | E.7.2 |
| …ne laisse pas l'API modifier l'adresse du générateur | E.10.1, DEC-081 |
| …ne journalise rien | T7 |
| …n'ajoute aucun champ à `project.json` | DEC-048 |

---

## E.15 Décisions à consigner dans la bible

Huit fiches, écrites au chapitre 11 **avant l'implémentation, en un seul commit de documentation**, comme T2 et T3.

| Réf. | Objet | Section |
|---|---|---|
| **DEC-074** | Machine à états du `Job` : cinq états, trois terminaux, un échec arrête le lot, le `Job` vit en mémoire. **Ferme la question B** | E.3 |
| **DEC-075** | Un lot fige un prompt et N graines ; chaque candidat est sauvegardé dès qu'il existe, par relecture, écriture de l'image, puis du projet | E.4 |
| **DEC-076** | Le template de workflow : schéma, trois jetons en liste close, un jeton est une valeur entière, substitution sur le graphe. **Ferme la question F** ; corrige la liste de jetons du §6.4 | E.5 |
| **DEC-077** | Ce qui part est exactement le prompt résolu ; la clause négative et le graphe ne sont pas figés | E.6 |
| **DEC-078** | `IImageGenerator` est le seul port ; `IPawnPairProducer` n'est pas écrit. **Supersède le chapitre 7 et le §4.2** sur ce point | E.7.1 |
| **DEC-079** | La découpe est décidée en T4 et exécutée en T5 ; la face est à gauche, une largeur impaire perd sa colonne du milieu | E.8 |
| **DEC-080** | Les bornes de la génération, en records d'options. **Scinde la question G** | E.9 |
| **DEC-081** | MEN-003 : l'adresse du générateur est un réglage de déploiement ; ni redirection, ni proxy, ni liste blanche de ports | E.10.1 |

---

## E.16 Corrections à apporter aux documents

Signalées plutôt que corrigées en silence.

1. **Le §6.4 de la bible liste cinq jetons**, dont `{{WIDTH}}` et `{{HEIGHT}}`. DEC-076 n'en garde que trois ; le §6.4 doit y renvoyer.
2. **Le chapitre 7 dessine `IPawnPairProducer`** et une signature d'`IImageGenerator` rendant un `RawImage`. DEC-078 retire le premier ; le second rend un `GeneratedImage` portant ses dimensions, lues sur l'en-tête.
3. **Le §4.2 dit que la génération jumelée « est isolée derrière un port unique (`IPawnPairProducer`) »**. Elle est désormais un cas d'usage appuyé sur `IImageGenerator` (DEC-078).
4. **La contre-mesure de MEN-003** au chapitre 9 prescrit une liste blanche de ports. DEC-081 la remplace.
5. **Le chapitre 16** : la ligne B passe dans les questions refermées (DEC-074), la ligne F aussi (DEC-076), la ligne G perd sa moitié génération (DEC-080).

---

## E.17 Découpage en tâches

Dix tâches, un commit chacune, dans cet ordre. Chaque tâche laisse la solution compilable et la suite de tests verte.

| # | Tâche | Tests |
|---|---|---|
| 1 | `Job` et `JobState` (domaine), et `<Version>0.5.0</Version>` (DEC-058) | 1 à 6 |
| 2 | `PairSplit` (domaine) | 7 à 9 |
| 3 | `WorkflowTemplate` et son lecteur, codes de configuration, `config/workflow.comfyui.example.json` | 10 à 19 |
| 4 | Port `IImageGenerator`, `GeneratorException`, `ICodedException` ; options et adresse ; `CheckAsync` ; le faux serveur | 20, 21, 31 |
| 5 | `GenerateAsync` : soumission, interrogation, rapatriement, bornes | 22 à 28, 30 |
| 6 | Annulation jusqu'au générateur | 29 |
| 7 | `IProjectRepository.WritePairedImageAsync` | — (couvert par 42, plus ses propres tests de chemin) |
| 8 | Cas d'usage du lot, `RandomSeeds`, `GenerationOptions` | 32 à 41 |
| 9 | Test de bout en bout | 42 |
| 10 | CLI `generator check` et `candidate generate`, documentation | — |

---

## E.18 Point d'entrée en ligne de commande

`tools/Pawnsmith.Cli` — jetable, non livré, exclu de l'image Docker, sans tests, comme en B.7, C.17 et D.16.

| Sous-commande | Arguments | Effet |
|---|---|---|
| `generator check` | `--workflow`, `--generator-url` | Lit le workflow, affiche la clause de cadrage et l'état du générateur |
| `candidate generate` | `--path`, `--id`, `--count` ou `--seed`…, `--workflow`, `--generator-url`, `--calibration` | Lance un lot ; affiche chaque transition du `Job` ; `Ctrl+C` annule proprement |

**Aucune logique.** Le CLI lit ses arguments, construit l'adaptateur et le cas d'usage, et affiche les transitions qu'il reçoit. `Ctrl+C` annule le jeton : c'est la seule façon de voir, sans écrire un test, qu'un lot interrompu laisse ses candidats dans le projet.

---

## Annexe — Ce qui vient après, et qu'il ne faut pas anticiper

T5 exécutera la découpe de E.8 et détourera les deux moitiés, en réencodant sans métadonnées. T6 hébergera les `Job` en arrière-plan, sérialisera les écritures d'un même projet, et exposera l'état du générateur.

**Ne rien construire pour ces tranches.** En particulier : aucune file de `Job`, aucun registre, aucun décodeur de PNG.
