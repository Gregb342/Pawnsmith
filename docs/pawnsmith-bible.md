# Pawnsmith — Bible du projet
 
| | |
|---|---|
| **Nom de code** | Pawnsmith |
| **Version du document** | 0.19 |
| **Date** | 4 octobre 2026 |
| **Statut** | Brouillon — évolutif |
| **Porteur** | Grégoire |
| **Licence visée** | Open source, permissive (MIT recommandé) |
 
> **Comment lire ce document.** Il est vivant. Le chapitre 11 (journal des décisions) fait foi : quand une décision change, on ajoute une fiche, on ne réécrit pas l'ancienne. Les valeurs marquées `À CALIBRER` sont volontairement absentes tant que la tranche T0 n'a pas été menée — ne pas les inventer.
 
> **Changements depuis la v0.18** — Revue de la maquette du front par le porteur, et spécification de la seconde partie de T6 (`pawnsmith-cahier-des-charges-t6-front.md` v1.0). **DEC-105 à DEC-112, décidées par le porteur** : les libellés d'écran (Proposition, Retenue, Prompt modifié, ComfyUI connecté) ; le catalogue traduit, race et classe devenues des listes, la tête du template prenant leurs fragments ; le catalogue personnel, fait d'objets complets ; l'adresse du générateur réglée depuis l'interface (**supersède DEC-081** sur ce point) ; la clause sujet verrouillée par défaut ; la bibliothèque de styles copiés, la palette retirée de l'interface ; l'enregistrement automatique ; l'univers et le style figés à la première proposition, et la duplication (**supersède DEC-030 et DEC-055** pour ces deux champs). **DEC-113 et DEC-114** : l'aperçu de la planche est le PDF ; le front sans dépendance nouvelle, en `0.10.0`. Notes ajoutées aux §2, §12, §15.1, §15.5 et au chapitre 16, où la question E est fermée.

> **Changements depuis la v0.17** — Spécification de la tranche T5 (`pawnsmith-cahier-des-charges-t5.md` v1.0). **DEC-098 et DEC-099, décidées par le porteur** : le détourage se fait **sans modèle**, par diffusion sur le fond uni que la clause de cadrage exige (**supersède DEC-008** sur le modèle ONNX : les poids disponibles portent la condition non commerciale de leurs données d'entraînement), et les PNG se lisent et s'écrivent à la main, sur le sous-ensemble qu'écrit ComfyUI. **DEC-100 à DEC-104** : l'algorithme et ses valeurs ; le détourage dans le lot, avant la sauvegarde, sans que son échec arrête le lot ; la signature du port (**supersède le chapitre 7**) ; cinq codes, qui **ferment la question G** ; T5 en `0.9.0` (**supersède DEC-058** sur ce point).

> **Changements depuis la v0.16** — Spécification de la tranche T7 (`pawnsmith-cahier-des-charges-t7.md` v1.0), écrite sans arbitrage du porteur. **DEC-090 à DEC-097** : seuls les bords journalisent, Serilog derrière `ILogger<T>`, l'identifiant de job poussé au point d'appel du cas d'usage ; une ligne JSON par événement, rotation par jour et par taille, rétention par nombre de fichiers ; ni prompt ni corps de requête au journal (**précise le chapitre 8**) ; l'avertissement de MEN-004 et ce qu'un conteneur ne peut pas savoir ; le visualiseur par liste blanche d'énumération ; **MEN-005 étendu à la planche**, trou trouvé par la revue ; **MEN-011** — falsification de journal — entre au chapitre 9 ; et la revue elle-même, dont chaque ligne nomme son test ou son risque accepté.

> **Changements depuis la v0.15** — Spécification de la première partie de T6, l'API (`pawnsmith-cahier-des-charges-t6.md` v1.0), écrite sans arbitrage du porteur. **DEC-082 à DEC-089** : l'export d'un élu désaligné passe outre et le signale (**ferme la question C**) ; un projet s'adresse par son nom de dossier canonique ; une erreur d'API rend un code et rien d'autre, les messages portant des chemins absolus ; les lots sont validés avant la file et un seul tourne à la fois ; les écritures d'un projet passent par une porte par dossier ; la configuration passe par `appsettings.json` et l'environnement ; une image n'est servie que si un candidat la référence ; **MEN-010** — requêtes intersites et rebinding DNS — entre au chapitre 9. La question E est fermée pour sa partie serveur.
 
> **Changements depuis la v0.14** — Spécification de la tranche T4 (`pawnsmith-cahier-des-charges-t4.md` v1.0), écrite sans arbitrage du porteur, dans le régime « tranche et consigne » ouvert pour cette session. **DEC-074 à DEC-081** : machine à états du `Job` en cinq états, en mémoire seulement (**ferme la question B**) ; un lot fige un prompt pour N graines et sauvegarde chaque candidat dès qu'il existe ; schéma du template de workflow, trois jetons en liste close dont aucun de dimension (**ferme la question F**, corrige le §6.4) ; ce qui part au générateur est exactement le prompt résolu, la clause négative n'est pas figée ; `IPawnPairProducer` n'est pas écrit (**supersède le chapitre 7 et le §4.2** sur ce point) ; la découpe est décidée en T4 et exécutée en T5 ; bornes de la génération en records d'options (**scinde la question G**) ; l'adresse du générateur est un réglage de déploiement, ce qui remplace la liste blanche de ports de MEN-003.
 
> **Changements depuis la v0.13** — Spécification de la tranche T3 (`pawnsmith-cahier-des-charges-t3.md` v1.0). **DEC-063 à DEC-073** : catalogue global en fichier de données, un par univers (**ferme la question D**) ; une entrée de catalogue est un fragment de phrase et non un mot, en réponse à l'adhérence imparfaite mesurée par DEC-043 ; catalogue et template en deux fichiers ; `IPromptComposer` réduit à `ComposeSubject` (**supersède la signature du chapitre 7**) ; la clause sujet se recompose tant qu'elle n'a pas été éditée, l'édition étant déduite et non stockée ; élection et statut en deux axes (**ferme la première moitié de la question B**) ; gabarit sans élu ignoré et signalé ; suppression d'un gabarit emportant ses fichiers ; élection exigeant les deux détourages ; machine à états du `Job` reportée à T4 (**scinde la question B**) ; schéma des fichiers de templates descendu en T3 (**scinde la question F**). La question A est fermée par le parcours utilisateur du §D.3. Corrections : le chapitre 7 écrivait encore `IPromptComposer` en franglais ; le §3.1 laissait ouvert le cas du gabarit jamais édité ; le §15.3 supposait un catalogue sans dire d'où il venait ; la question C du chapitre 16 demandait de trancher une sous-question que T1 avait déjà réglée.
 
> **Changements depuis la v0.12** — **DEC-062**, écrite à l'assemblage d'`IProjectRepository` en fin de T2 : le dépôt de projet est sans état et chaque opération reçoit son chemin, ce qui **supersède les signatures esquissées au chapitre 7**. La décision avait été prise pendant la tâche 8 et laissée hors des fiches, faute de port à qui l'appliquer.
 
> **Changements depuis la v0.11** — DEC-061 : la mention visible que DEC-060 prévoyait n'est pas écrite non plus. Le sujet de l'attribution est clos en entier.
 
> **Changements depuis la v0.10** — DEC-060 : le filigrane invisible est écarté, la licence MIT est confirmée, et l'attribution passe par une mention visible et par le nom. C'était le dernier sujet réservé de T1.
 
> **Changements depuis la v0.9** — Spécification de la tranche T2, puis sa revue. **DEC-046 à DEC-054** : noms de fichiers en anglais comme leurs clés, identité d'un projet par identifiant opaque, rejet d'un schéma inconnu sans migration implicite, figement des **trois clauses** sur le candidat à la place du prompt assemblé (supersède la forme du champ `promptUtilise` du §3.1), deux profils d'archive à contenu décidé par liste blanche, import global et atomique sans fusion, `gutterMm` maintenu dans la calibration (**ferme la question ouverte du §15.6**), surcharges de projet en liste close résolues hors du domaine, et deux menaces nouvelles au chapitre 9 — MEN-008 (exfiltration par lien symbolique à l'export) et MEN-009 (traversée de chemin par le nom de projet). **DEC-055 à DEC-059**, nées de la revue de cette spécification : aucun champ de projet n'est verrouillé après création, une donnée de projet n'est jamais rejetée par une donnée de machine (**supersède la clause de double validation de DEC-053**, qui n'aura jamais été en vigueur), un fichier de configuration se crée avec le composant qui le lit, une tranche livrée vaut un mineur, et le CLI jetable prend des sous-commandes. Corrections de texte : `Geometry` comptait encore deux valeurs au §1.3, au chapitre 2, au §3.1, au §5.2 et au §5.3 alors que DEC-039 en a ajouté une troisième ; le §15.1 et le §15.3 décrivaient encore un verrouillage que DEC-030 avait levé ; et le chapitre 12 annonçait encore l'ordre des tranches d'avant DEC-044. Ajout du **chapitre 16**, qui reprend les neuf questions ouvertes du §4 de `pawnsmith-etat-du-projet.md` — document supprimé, périmé partout ailleurs, mais seul porteur de ces questions que trois fiches neuves citent.
 
> **Changements depuis la v0.8** — DEC-044 (T0b reportée ; T1 reste ouverte, écrite et testée mais non validée ; le travail continue sur T2). DEC-045 (l'étape 0 de T0b se juge en rapport avec le trait de calibration, pas en millimètres absolus — sans quoi on conclurait à un bug de T1 pour une propriété de l'imprimante). Correction au §15.3 : le prompt résolu y était décrit comme stocké et éditable, ce que DEC-028 contredit.
 
> **Changements depuis la v0.7** — DEC-043 : T0a est menée et concluante. DEC-003 tient — le modèle effectue une rotation réelle du personnage et non un miroir. Le prompt de référence est consigné. Correction : le modèle installé est Krea 2 Turbo et non FLUX Krea, ce qui rend les LoRA et ControlNet FLUX incompatibles.
 
> **Changements depuis la v0.6** — DEC-042 (la clause de cadrage impose la pose ; mesures à l'appui, les huit illustrations d'essai étaient toutes limitées par leur largeur). DEC-039 (troisième géométrie `NoSupport`, sans appendice). DEC-040 (les cotes de l'onglet sont réglables, et la troisième catégorie de valeur physique est nommée). DEC-041 (le recto et le verso partagent une échelle unique — défaut mesuré jusqu'à 4,5 mm d'écart sur des illustrations réelles).
 
> **Changements depuis la v0.5** — DEC-038 : les cinq conventions géométriques du domaine, posées pendant les tâches 2 et 3 de T1 et absentes de tout document jusqu'ici — sens de l'axe vertical, coordonnées relatives à l'unité, fermeture implicite des polygones, millimètre partout, rejet des incohérences internes de la calibration.
 
> **Changements depuis la v0.4** — DEC-037 : l'anglais devient la langue du code, des journaux, des clés de fichiers et des prompts ; le français reste celle de l'interface traduite et de ces documents. La clause « repris tel quel » du chapitre 2 est superseedée et la table de correspondance des termes est fixée. Renommage appliqué dans tout le document **sauf au chapitre 11**, dont les fiches sont des enregistrements datés.
 
> **Changements depuis la v0.3** — Ajout du **chapitre 15**, structure de l'interface : navigation, anatomie de l'écran de mise en page, panneau de paramètres à deux niveaux, indicateur de capacité, et la liste de ce que l'interface ne fait pas. DEC-034 (la coquille d'une maquette exploratoire est retenue, son contenu est rejeté). DEC-035 (les marges de page restent uniformes ; le gain abandonné est chiffré). DEC-036 (le paysage est une entrée de configuration, pas une bascule ; son intérêt dépend de la taille). §5.4 et critères de T6 renvoyés vers le chapitre 15.
 
> **Changements depuis la v0.2** — Ajout du **chapitre 14**, table de référence des grilles de jeu et des tailles de créature, sourcée. DEC-031 (cinq tailles nommées d'après les règles, emprise et hauteur découplées ; rejet de l'échelle S/M/L/XL/XXL). DEC-032 (la loi de progression des hauteurs est une décision de conception, bornée par le format de papier — la valeur provisoire de `Gargantuan` rendait la capacité de page nulle). DEC-033 (T0 scindée en T0a et T0b ; le CLI de T1 remplace le script de gabarit jetable). EVO-011 (taille Minuscule). Tableau des tailles du §3.1 étendu, §5.6 borné, chapitre 12 réordonné.
 
> **Changements depuis la v0.1** — DEC-028, DEC-029 et DEC-030 ajoutées (composition du prompt en trois clauses, désalignement à la place du verrouillage). Suppression des « cotes de l'encoche », résidu de la piste d'impression externe abandonnée. Ajout de la tranche Fondations au chapitre 12. Ajout d'EVO-010 (import d'images externes), décidé mais jamais consigné. Vocabulaire du chapitre 2 étendu : clause sujet, clause style, clause cadrage, désaligné.
 
---
 
## 1. Vision et périmètre
 
### 1.1 Le problème
 
Les figurines de jeu de rôle en carton 2D (« pions », « standees ») sont la solution la plus accessible pour matérialiser des créatures sur une carte de bataille. Trois briques existent aujourd'hui, mais aucune chaîne ne les relie :
 
- des modèles de diffusion capables de produire l'illustration d'un personnage ;
- des modèles de segmentation capables de détourer une image ;
- des outils de mise en page rudimentaires, qui n'acceptent qu'une image à la fois.
L'assemblage est manuel, fastidieux, et surtout il ne garantit aucune **cohérence visuelle** entre les figurines d'une même planche — ce qui est le critère de qualité principal du résultat imprimé.
 
### 1.2 Proposition de valeur
 
Pawnsmith est une application web auto-hébergée qui produit, à partir de paramètres de haut niveau (race, classe, équipement), une **planche PDF calibrée pour l'impression domestique**, dont toutes les figurines partagent un style visuel imposé structurellement.
 
### 1.3 Dans le périmètre de la v1
 
- Univers fantasy uniquement.
- Génération d'images via un modèle de diffusion **local**, piloté par l'API HTTP de ComfyUI.
- Composition de prompts déterministe par templates.
- Production du couple recto/verso par génération jumelée.
- Détourage local et systématique.
- **Trois géométries** de pion : tente pliée, pion à onglet avec socle, et pion sans aucun support (DEC-039).
- Mise en page en grille uniforme, une taille de pion par page, plusieurs pages par projet.
- **Cinq tailles** calées sur les emprises de la grille de jeu : Small, Medium, Large, Huge, Gargantuan (DEC-031).
- Export PDF, formats A4 et US Letter.
- Sauvegarde et rechargement de projets, export et import d'archives.
- Interface bilingue français / anglais.
### 1.4 Hors périmètre de la v1 (différé, voir chapitre 13)
 
- Fournisseur d'images distant (API en ligne).
- Composition de prompts assistée par modèle de langage.
- Import d'images externes déjà détourées.
- Univers autres que fantasy.
- Troisième géométrie (pièces séparées collées sur âme carton).
- Mélange de plusieurs tailles de pions sur une même page.
- **Taille Minuscule** (emprise de 12,7 mm), dont la faisabilité physique n'est pas établie — voir EVO-011.
- Grilles hexagonales.
- Langues au-delà du français et de l'anglais.
### 1.5 Non-objectifs permanents
 
Ces points ne sont pas « plus tard », ils sont **hors sujet**. Les inscrire ici évite d'y revenir tous les trois mois.
 
- Pawnsmith ne fournit ni ne distribue de modèle de diffusion. Il fournit l'interface pour s'y brancher ; l'obtention, l'installation et la conformité du modèle relèvent de l'utilisateur.
- Pawnsmith n'est pas multi-utilisateur : pas de comptes, pas d'authentification, pas de cloisonnement. C'est une application mono-utilisateur auto-hébergée.
- Pawnsmith ne produit pas de modèles 3D et ne commande pas d'impression auprès d'un prestataire.
- Pawnsmith n'est pas un éditeur d'images. Le retouchage se fait ailleurs.
---
 
## 2. Glossaire
 
Ce vocabulaire est contraignant en tant que **concept** : un terme désigne une seule chose, partout, et toute divergence de sens est un défaut. Il ne l'est plus en tant que **graphie** : depuis DEC-037, le code, les journaux et les prompts sont en anglais, et l'identifiant de chaque terme est donné par la table de correspondance de cette fiche. Le français reste la langue de ce document et du catalogue d'interface `fr`.
 
| Terme | Définition |
|---|---|
| **Projet** | Unité de travail persistante. Contient un style, une géométrie, un univers, un ensemble de gabarits, et produit une ou plusieurs planches. |
| **Univers** | Registre esthétique global du projet (fantasy en v1). Détermine quel jeu de templates de prompts est utilisé. |
| **Style** | Ensemble (rendu, palette, formulation littérale) défini au niveau du projet, jamais au niveau du gabarit. Garantit la cohérence visuelle. |
| **Géométrie** | Mode de construction physique du pion — `FoldedTent`, `TabAndSocket` ou `NoSupport` (DEC-039). Uniforme pour tout le projet. |
| **Gabarit** | *Ce que l'utilisateur veut* : une créature définie par ses paramètres (race, classe, taille, équipement, détails), sa quantité, et sa clause sujet. Persistant et stable. |
| **Candidat** | *Ce que le modèle a produit* : une tentative concrète pour un gabarit, identifiée par une graine. Un gabarit peut avoir N candidats ; un seul est élu. |
| **Clause cadrage** | Segment de prompt constant, jamais exposé dans l'interface. Impose ce qui rend l'image découpable et détourable. Voir §4.1 et DEC-029. |
| **Clause sujet** | Segment de prompt propre au gabarit, produit par le composeur à partir de ses paramètres. **Seul segment éditable par l'utilisateur.** |
| **Clause style** | Segment de prompt propre au projet, appliqué identiquement à tous les gabarits. |
| **Prompt résolu** | Assemblage des trois clauses. Valeur **dérivée** : ni stockée sur le gabarit, ni éditable. |
| **Désaligné** | État **calculé** d'un candidat dont **au moins une des trois clauses figées** diffère de la clause courante correspondante. Orthogonal au statut. Voir DEC-030 et DEC-049. |
| **Couple recto/verso** | Paire d'images indissociable attachée à un candidat : la vue de face et la vue de dos du même personnage. La validation porte sur le couple, jamais sur une face isolée. |
| **Taille** | Catégorie de créature, nommée d'après les règles de jeu (Small, Medium, Large, Huge, Gargantuan). Porte **deux dimensions indépendantes** : l'emprise sur la grille et la hauteur du pion. Sert de clé de regroupement en pages. Voir DEC-031 et le chapitre 14. |
| **Emprise** | Côté du carré occupé par la créature sur la grille de jeu, en millimètres. Fait documenté, issu des règles (chapitre 14). |
| **Planche** | Une page PDF, contenant les pions d'une seule taille, disposés en grille uniforme, avec les repères d'impression. |
| **Catalogue** | Listes de valeurs proposées dans l'interface pour les paramètres d'un gabarit (armes, armures, etc.). Éditable par l'utilisateur. |
| **Job** | Unité d'exécution asynchrone traçable (génération d'un lot, détourage, export). Porte un identifiant propagé dans toute la journalisation. |

*Depuis DEC-105 :* trois concepts ont un **libellé d'écran** distinct de leur nom dans ce glossaire, parce que la revue de la maquette a montré que les mots ne passaient pas. Un *candidat* s'affiche « Proposition » (« Proposal »), un candidat *élu* « Retenue » (« Kept »), un candidat *désaligné* « Prompt modifié depuis » (« Prompt changed since »). Les concepts ne changent pas ; la table de DEC-105 fait foi pour les libellés.
 
---
 
## 3. Modèle de données
 
### 3.1 Entités
 
**Projet** — racine d'agrégat.
 
| Champ | Type | Notes |
|---|---|---|
| `versionSchema` | entier | Obligatoire dès la v1. Tout ajout de champ l'incrémente : il n'existe pas d'ajout compatible (DEC-048). |
| `projectId` | identifiant | UUID v4, généré à la création, **immuable**, jamais dérivé du nom. Le nom du dossier n'a aucune sémantique (DEC-047). |
| `nom` | texte | |
| `univers` | énumération | `Fantasy` en v1. Champ présent pour l'extension. Modifiable ; désaligne les candidats existants (DEC-030). |
| `style` | Style | Modifiable ; désaligne les candidats existants (DEC-030). |
| `geometrie` | énumération | `FoldedTent` \| `TabAndSocket` \| `NoSupport` (DEC-039). Modifiable sans conséquence sur les candidats : paramètre de rendu. |
| `formatPapier` | FormatPapier | Référence vers une entrée du catalogue de formats. Modifiable sans conséquence sur les candidats. |
| `calibrationOverrides` | objet | Liste **close** : `tabWidthMm` et `tabHeightMm`, membres nullables toujours écrits, `null` valant « valeur de la calibration ». Ce n'est pas un dictionnaire (DEC-053). |
| `gabarits` | liste de Gabarit | |
| `creeLe`, `modifieLe` | horodatage | |
 
**Style** — propriété de projet, jamais de gabarit.
 
| Champ | Type | Notes |
|---|---|---|
| `nom` | texte | |
| `clauseStyle` | texte | Chaîne littérale injectée dans chaque prompt résolu. **Jamais réécrite au niveau du gabarit, ni par un modèle de langage.** Modifiable au niveau du projet (DEC-030). |
| `clauseNegative` | texte | Prompt négatif commun. |
| `palette` | texte | Descripteur libre, intégré à la clause style. |
 
**Gabarit**
 
| Champ | Type | Notes |
|---|---|---|
| `id` | identifiant | |
| `race` | texte | Obligatoire. |
| `classe` | texte | Obligatoire. |
| `taille` | Taille | Obligatoire. Clé de regroupement en pages. |
| `parametresOptionnels` | dictionnaire | Clés du catalogue (arme, armure, vêtement, couleur…). Une clé absente signifie « non contraint », **pas** « absent de l'illustration ». |
| `details` | texte | Champ libre, intégré à la clause sujet lors de sa composition. |
| `clauseSujet` | texte | Produite par le composeur à partir des champs ci-dessus. **Stockée et éditable.** Seul segment du prompt que l'utilisateur peut modifier (DEC-028). Ne se régénère pas toute seule après édition — et **tant qu'elle n'a pas été éditée, elle suit les champs** ; l'édition se déduit par comparaison, elle n'est pas stockée (DEC-067). |
| `promptResolu` | *(dérivé)* | `clauseCadrage + clauseSujet + clauseStyle`. **Non persisté, non éditable.** Affiché en lecture seule. |
| `quantite` | entier ≥ 1 | Nombre d'exemplaires du même pion sur la planche. |
| `candidats` | liste de Candidat | |
| `idCandidatElu` | identifiant nullable | |
 
**Candidat**
 
| Champ | Type | Notes |
|---|---|---|
| `id` | identifiant | |
| `graine` | entier | |
| `framingClauseUsed` | texte | Clause cadrage figée au moment de la génération. |
| `subjectClauseUsed` | texte | Clause sujet figée au moment de la génération. |
| `styleClauseUsed` | texte | Clause style figée au moment de la génération. |
| `statut` | énumération | `Brouillon` \| `Valide` \| `Rejete` |
| `desaligne` | *(calculé)* | Vrai si l'ensemble des clauses désalignées n'est pas vide, clause par clause (DEC-049). **Jamais persisté.** |
| `fichierJumelee` | chemin | Image brute contenant les deux vues, conservée pour diagnostic. |
| `fichierRectoDetoure` | chemin | PNG à fond transparent. |
| `fichierVersoDetoure` | chemin | PNG à fond transparent. |
| `genereLe` | horodatage | |
 
> **Le candidat fige ses trois clauses, pas le prompt assemblé** (DEC-049, qui supersède la forme du champ unique `promptUtilise` sans toucher à son intention). Le prompt réellement envoyé est une valeur **dérivée** de ces trois champs, donc ni persistée ni sérialisée — au même titre que `promptResolu` sur le gabarit. Le désalignement se calcule **clause par clause**, ce qui permet de dire *laquelle* a bougé plutôt que « quelque chose a changé ».
 
> **Piège de conception.** `desaligne` n'est **pas** une valeur de `statut`. Un candidat `Valide` peut devenir désaligné sans cesser d'être validé : ce sont deux axes indépendants. Les fusionner en une seule énumération est l'erreur naturelle à cet endroit, et elle rend impossible de distinguer « rejeté par l'utilisateur » de « produit sous un style qui n'est plus celui du projet ».
 
**Taille** — table de référence, valeurs par défaut surchargeables dans les réglages. Les emprises sont des **faits documentés** (chapitre 14) ; les hauteurs sont des **décisions de conception** bornées par le papier (DEC-032).
 
| Identifiant | Libellé du catalogue `fr` | Emprise grille | Largeur pion | Hauteur pion |
|---|---|---|---|---|
| `Small` | Petite | 1 × 1 case | 25,4 mm | `À CALIBRER` |
| `Medium` | Moyenne | 1 × 1 case | 25,4 mm | `À CALIBRER` |
| `Large` | Grande | 2 × 2 cases | 50,8 mm | `À CALIBRER` |
| `Huge` | Très Grande | 3 × 3 cases | 76,2 mm | `À CALIBRER` |
| `Gargantuan` | Gigantesque | 4 × 4 cases | 101,6 mm | `À CALIBRER` |
 
> **Piège à ne pas manquer** : l'emprise sur la grille et la hauteur visuelle du pion sont **deux dimensions indépendantes**. Un humanoïde de taille Medium occupe une case de 25,4 mm mais mesure environ le double en hauteur. Ne pas déduire l'une de l'autre.
 
> **Small et Medium partagent la même emprise.** C'est conforme aux règles : Small et Medium occupent tous deux une case de 5 pieds. Ce qui les distingue est la hauteur du pion, et rien d'autre. Conséquence directe et assumée (DEC-031) : puisque la hauteur détermine la hauteur de cellule, une planche de `Small` ne peut jamais partager sa page avec des `Medium`. Un seul gnome dans un projet coûte donc une page entière. EVO-005 (mélange de tailles par shelf packing) est la seule résolution propre, et vient de gagner en valeur.
 
**FormatPapier**
 
| Nom | Largeur | Hauteur |
|---|---|---|
| A4 | 210 mm | 297 mm |
| US Letter | 216 mm | 279 mm |
 
Le moteur de mise en page ne connaît **que des millimètres**. Aucun format n'est codé en dur : ajouter un format est l'ajout d'une entrée de configuration.
 
### 3.2 Persistance sur disque
 
Un projet est un **dossier en clair**, jamais une base de données.
 
```
mon-projet/
├── project.json           # toutes les entités ci-dessus
├── images/
│   ├── {idCandidat}-jumelee.png
│   ├── {idCandidat}-recto.png
│   └── {idCandidat}-verso.png
└── exports/
    └── mon-projet-moyenne.pdf
```
 
Justification : versionnable, sauvegardable, diffable, et lisible dans plusieurs années même si l'application ne démarre plus.
 
Les valeurs dérivées — `promptResolu`, `desaligne` — ne sont **pas** sérialisées. Une valeur calculée qu'on persiste devient une valeur qui ment dès la première modification manquée.
 
Le fichier s'appelle **`project.json`**, et non `projet.json` : tout ce que l'application nomme comme un contrat est en anglais, au même titre que ses clés (DEC-046). Ce que l'**utilisateur** nomme — le dossier du projet, dérivé de son `nom` — reste dans sa langue à lui.
 
L'export produit une archive ZIP en **deux profils** : `Backup`, pour se sauvegarder soi-même, et `Share`, pour envoyer le projet à quelqu'un (DEC-050). Dans les deux cas, le contenu est décidé par **liste blanche** — on énumère ce qui est autorisé, on ne zippe jamais le dossier. **L'archive ne contient jamais de secret, ni de journal.** Un projet doit pouvoir être partagé sans réflexion préalable — cette propriété est un invariant, pas une bonne pratique, et la liste blanche est ce qui la rend structurelle plutôt que déclarative.
 
---
 
## 4. Pipeline de production
 
Chaque étape est un port distinct (chapitre 7). Le découplage est la propriété centrale du système : **la production d'images ne sait rien de la mise en page, et réciproquement.**
 
| # | Étape | Entrée | Sortie | Mode d'échec |
|---|---|---|---|---|
| 1 | Composition du prompt | Gabarit + Style + clause cadrage | Prompt résolu (assemblage de trois clauses) | Aucun (déterministe) |
| 2 | Génération jumelée | Prompt + graine | Une image contenant vue de face et vue de dos côte à côte | Générateur injoignable, délai dépassé, modèle en erreur |
| 3 | Découpe | Image jumelée | Deux images indépendantes | Partage vertical incorrect si le modèle n'a pas respecté le cadrage |
| 4 | Détourage | Deux images | Deux PNG à fond transparent | Image malformée, dimensions hors bornes |
| 5 | Validation | Couple recto/verso | Candidat élu | Aucun (action utilisateur) |
| 6 | Mise en page | Gabarits élus + format + géométrie | Modèle de planche (positions en mm) | Capacité de page dépassée ou nulle |
| 7 | Rendu PDF | Modèle de planche | Fichier PDF | Écriture disque |
 
### 4.1 Les trois clauses
 
Le prompt résolu est l'assemblage ordonné de trois segments, de portées différentes :
 
| Clause | Portée | Éditable ? |
|---|---|---|
| **Cadrage** | Constante de l'application | Non — jamais exposée (DEC-029) |
| **Sujet** | Le gabarit | **Oui**, seul segment modifiable (DEC-028) |
| **Style** | Le projet | Au niveau du projet uniquement (DEC-030) |
 
La **clause de cadrage** impose : planche de rotation, vue de face et vue de dos, corps entier, pieds au bord inférieur, fond uni, aucun élément coupé, ratio portrait.
 
Cette clause n'est pas une préférence esthétique : c'est ce qui rend l'étape 3 découpable et l'étape 4 fiable. Un fond de forêt se détoure mal ; un personnage cadré à mi-cuisse est inutilisable. La modifier produit un **défaut fonctionnel**, pas un choix de goût — d'où son absence totale de l'interface. L'utilisateur avancé qui veut un autre cadrage passe par le template de workflow ComfyUI, qui est un fichier de configuration.
 
### 4.2 Pourquoi la génération jumelée
 
Deux générations indépendantes du même personnage ne produisent pas le même personnage. Les modèles de diffusion n'ont pas de permanence d'objet : l'arme change de forme, la cape disparaît, la palette dérive. La cohérence recto/verso est **structurellement garantie** si les deux vues sont dessinées dans la même passe.
 
Contrepartie assumée : chaque vue n'occupe que la moitié de la résolution générée.
 
Cette étape est isolée derrière un port unique (`IPawnPairProducer`). Si la calibration T0a montre que le modèle local ne produit pas de planche de rotation exploitable, on substitue une implémentation dégradée — deux générations indépendantes à graine partagée — **sans toucher au reste du système**.

*Depuis DEC-078 :* T0a a montré que le modèle produit une planche de rotation exploitable (DEC-043), et l'implémentation dégradée n'est pas écrite. La génération jumelée est un **cas d'usage** de l'Application appuyé sur le port `IImageGenerator`, qui reste le point de substitution du fournisseur ; `IPawnPairProducer` n'existe pas.
 
---
 
## 5. Géométries, mise en page et impression
 
### 5.1 Contrainte physique fondatrice
 
Aucune imprimante domestique n'atteint un repérage recto-verso suffisant pour un pion de 25 mm. En conséquence, **les deux faces sont imprimées sur la même face du papier**, puis pliées et collées. L'épaisseur double obtenue est aussi ce qui donne sa rigidité au pion.
 
### 5.2 Les trois géométries
 
**Tente pliée** — les deux vues sont réunies par une ligne de pliage haute. Sous la ligne des pieds, des volets se replient vers l'extérieur, ou le pion tient en V inversé. Aucun socle nécessaire.
 
**Pion à onglet et socle** — même construction, mais un onglet rectangulaire dépasse sous la ligne des pieds et coulisse dans un socle du commerce.
 
**Sans support** — rien sous la ligne des pieds : le contour se réduit au rectangle des deux images, et il ne reste que le pli principal (DEC-039). Le pion ne tient pas debout tout seul, et c'est le but : il est destiné à qui veut le coller sur son propre socle, le pincer, ou poser les figures à plat. L'appendice étant nul, la cellule est plus courte et la page en porte davantage.
 
La seule différence entre les trois est **ce qui est ajouté sous la ligne des pieds, et de combien la hauteur dépliée s'allonge**. Tout le reste est commun. L'abstraction correspondante est donc minimale : une fonction qui décrit l'appendice inférieur et la hauteur totale.
 
### 5.3 Règle de placement du verso
 
Pour les trois géométries : **le verso est placé au-dessus de la ligne de pliage, tourné à 180°.** Omettre la rotation produit un personnage tête en bas après pliage. Cette règle est vérifiée par un test unitaire.
 
### 5.4 Algorithme de mise en page
 
Toutes les figurines d'une page ont la même taille, donc la page est une **grille uniforme**. Pas de bin packing.
 
1. Regrouper les gabarits élus par taille.
2. Pour chaque groupe, émettre une ou plusieurs pages.
3. Capacité d'une page = `floor((largeurUtile) / largeurCellule) × floor((hauteurUtile) / hauteurCellule)`.
4. Chaque gabarit occupe `quantite` cellules consécutives.
5. Le PDF final concatène toutes les pages de tous les groupes.

L'interface expose la capacité restante de la page courante — information réellement utile lors de la composition. Ce qu'elle affiche exactement, et pourquoi un taux de remplissage en pourcentage n'y suffit pas, est précisé au §15.4.
 
**Une capacité nulle est un cas normal, pas une anomalie improbable** : il suffit qu'une hauteur de pion dépasse le plafond du §5.7. Elle doit produire une erreur explicite nommant la taille en cause, et un test dédié.
 
### 5.5 Repères d'impression (obligatoires, non désactivables en v1)
 
| Repère | Rôle |
|---|---|
| **Trait de calibration** | Segment de 100,0 mm exactement, légendé. Seule protection contre une impression hors échelle. |
| **Traits de coupe** | Contour de découpe, trait fin gris clair, invisible sur le pion découpé. |
| **Ligne de pliage** | Trait discontinu à la position du pli haut. |
 
Le texte porté sur la planche est localisé : la requête d'export transporte la langue voulue (chapitre 10).
 
### 5.6 Paramètres à calibrer (tranche T0b)
 
Ces valeurs ne doivent **pas** être devinées. Elles sortent d'un tirage papier réel.
 
| Paramètre | Unité | Statut |
|---|---|---|
| Grammage papier retenu | g/m² | `À CALIBRER` |
| Facteur de correction d'échelle de l'imprimante | ratio | `À CALIBRER` |
| Hauteur du pion pour la taille Medium | mm | `À CALIBRER` |
| Loi de progression des hauteurs des autres tailles | multiplicateurs | `À CALIBRER` (voir DEC-032) |
| Largeur et hauteur de l'onglet | mm | `À CALIBRER` |
| Hauteur des volets de tente | mm | `À CALIBRER` |
| Marge de sécurité autour de la silhouette | mm | `À CALIBRER` |
 
> **Note.** La v0.1 listait une entrée « cotes de l'encoche (ouverture / extrémité) ». C'était un résidu de la piste d'impression chez un prestataire externe, abandonnée très tôt. La géométrie `TabAndSocket` n'a pas d'encoche : elle a un onglet rectangulaire qui coulisse dans un socle du commerce. Aucun champ correspondant n'existe dans `calibration.json` et le protocole T0 n'en mesure aucune.
 
### 5.7 Plafond de hauteur imposé par le papier
 
Une unité dépliée occupe `2 × (hauteurPion + hauteurAppendice)`. Elle doit tenir dans la hauteur utile de la page :
 
```
hauteurUtile = hauteurPage − 2 × margePage − hauteurZoneCalibration
2 × (hauteurPion + hauteurAppendice) ≤ hauteurUtile
```
 
Avec les valeurs de mise en page actuelles (marge 10 mm, zone de calibration 14 mm) :
 
| Format | Hauteur utile | Plafond de `hauteurPion` (onglet 10 mm) | Plafond (volet 8 mm) |
|---|---|---|---|
| A4 (297 mm) | 263 mm | **121,5 mm** | 123,5 mm |
| US Letter (279 mm) | 245 mm | **112,5 mm** | 114,5 mm |
 
**Le plafond contraignant est celui d'US Letter : environ 112 mm.** Toute hauteur de pion supérieure produit une capacité nulle et rend la taille inutilisable sur ce format. C'est la borne dure de DEC-032, et elle doit être couverte par un test.
 
---
 
## 6. Architecture
 
### 6.1 Découpage
 
Architecture hexagonale allégée. **Quatre projets, pas sept.**
 
| Projet | Contenu | Dépendances |
|---|---|---|
| `Pawnsmith.Domain` | Géométries, tailles, calcul de grille, conversions millimètres/points, règles de validation. Pur, sans effet de bord. | Aucune |
| `Pawnsmith.Application` | Cas d'usage, orchestration des jobs, définition des ports. | Domain |
| `Pawnsmith.Infrastructure` | Client ComfyUI, runtime ONNX, PDFsharp, système de fichiers, Serilog. | Application, Domain |
| `Pawnsmith.Api` | ASP.NET Core, points de terminaison, DTO, injection de dépendances. Sert aussi le front compilé. | Toutes |
 
Le choix de l'hexagonal est ici justifié par les faits, pas par principe : le domaine est authentiquement pur (des mathématiques testables sans mock) et l'infrastructure est authentiquement remplaçable (ComfyUI aujourd'hui, une API demain).
 
### 6.2 Principes de code
 
- **DTO en `record`**, immuables, à égalité par valeur.
- **Une frontière, un jeu de DTO** — celle de l'API. Pas de cascade de représentations intermédiaires du même concept.
- **Mapping manuel explicite** via méthodes d'extension `ToDto()`. Pas d'AutoMapper : le mapping automatique casse à l'exécution et reste invisible à la relecture, ce qui contredit frontalement le mode de travail retenu (DEC-027).
- **Minimiser les dépendances NuGet.** Chaque paquet ajouté doit être justifié dans le journal des décisions.
- **Méthodes courtes**, découpées en méthodes privées nommées. La lisibilité prime sur la concision.
- Le domaine est couvert par des **tests unitaires sans mock** ; les adaptateurs par des **tests d'intégration**.
### 6.3 Front et packaging
 
- Front **React** + `react-i18next`.
- API ASP.NET Core.
- **Un seul conteneur**, construit en plusieurs étapes : compilation Node du front, compilation .NET de l'API, copie du `dist` dans le `wwwroot`. Même origine, donc pas de CORS ; déploiement en un `docker run`.
- Deux volumes distincts : `/app/data/projects` et `/app/data/logs`.
### 6.4 Une remarque sur le générateur d'images
 
L'API de ComfyUI est **HTTP, y compris en local**. Le port `IImageGenerator` est donc un client HTTP dès le premier jour — il n'existe jamais de cas « processus embarqué » qui polluerait l'abstraction.
 
ComfyUI n'accepte pas un prompt mais un **graphe de workflow JSON**. L'application stocke donc un *template de workflow* comportant des jetons nommés (`{{POSITIVE}}`, `{{NEGATIVE}}`, `{{SEED}}`, `{{WIDTH}}`, `{{HEIGHT}}`) qu'elle substitue avant envoi. Ce template est un **fichier de configuration**, pas du code : un utilisateur dont le workflow diffère peut l'adapter sans recompiler. C'est aussi le seul point d'accès à la clause de cadrage (DEC-029).

*Depuis DEC-076 :* les jetons sont **trois**, en liste close — `{{POSITIVE}}` et `{{SEED}}` exactement une fois, `{{NEGATIVE}}` au plus une fois. `{{WIDTH}}` et `{{HEIGHT}}` ne sont pas des jetons : les dimensions sont un réglage du workflow, et la découpe lit celles de l'image reçue. Un jeton occupe une valeur de chaîne entière, et la substitution se fait sur le graphe, jamais sur le texte. Le schéma du fichier est au §E.5 du cahier T4.
 
---
 
## 7. Contrats des ports
 
Signatures indicatives, à affiner à l'implémentation.
 
```csharp
// Disponibilité + génération. L'indisponibilité est un état normal, pas une exception :
// CheckAsync rend une valeur, et un lot sur un générateur éteint finit Failed (DEC-074).
// Le prompt reçu part tel quel : c'est ResolvedPrompt.From des trois clauses que le
// candidat fige (DEC-049, DEC-077). Les dimensions rendues sont lues sur l'en-tête PNG.
public interface IImageGenerator
{
    Task<GeneratorAvailability> CheckAsync(CancellationToken ct);
    Task<GeneratedImage> GenerateAsync(GenerationRequest request, CancellationToken ct);
}
 
// IPawnPairProducer n'existe pas (DEC-078) : T0a a retiré le risque qu'il couvrait, et
// la production du couple est un cas d'usage appuyé sur IImageGenerator.
 
// Détourage. Superséde par DEC-102 : le port reçoit l'image jumelée et rend
// deux PNG détourés ; DEC-098 retire le modèle ONNX et son fournisseur d'exécution.
public interface IBackgroundRemover
{
    Task<CutoutPair> CutOutPairAsync(byte[] pairedPng, CancellationToken ct);
}
 
// Une seule méthode, et c'est le point (DEC-066). ComposeSubject produit la clause
// sujet initiale, que l'utilisateur peut ensuite éditer, avec les diagnostics de
// composition — une valeur inconnue du catalogue, par exemple (DEC-063).
// L'assemblage du prompt n'est PAS ici : c'est ResolvedPrompt.From, fonction pure
// de domaine écrite en T2, qui n'aura jamais de seconde implémentation (EVO-001
// ne réécrit que la clause sujet). Aucune signature ne reçoit ni ne rend une clause
// cadrage ou une clause style : le verrouillage de DEC-028 est porté par le type.
public interface IPromptComposer
{
    ComposedSubject ComposeSubject(Blueprint blueprint, Universe universe);
}
 
// LoadAsync reçoit la calibration : valider une surcharge d'onglet suppose de la
// comparer à pawnWidthMm, et produire les diagnostics relationnels la demande aussi
// (DEC-053, DEC-056).
// SaveAsync ne reçoit PAS l'état antérieur, et ne doit jamais en recevoir : aucun
// champ n'est verrouillé après création, donc le dépôt n'a aucune transition à
// arbitrer. Une telle règle appartiendrait à un cas d'usage (DEC-055).
// Sans état : chaque opération reçoit l'endroit où travailler (DEC-062).
// Un projet ne porte jamais son propre emplacement — DEC-047.
public interface IProjectRepository
{
    Task<CreatedProjectResult> CreateAsync(string name, Universe universe, Geometry geometry,
                                           string paperFormatName, CancellationToken ct);
    Task<LoadedProjectResult> LoadAsync(string projectDirectory, Calibration calibration, CancellationToken ct);
    Task<Project> SaveAsync(string projectDirectory, Project project, CancellationToken ct);
    Task<string> ExportArchiveAsync(string projectDirectory, ArchiveProfileKind profile,
                                    string destinationDirectory, CancellationToken ct);
    Task<ImportedProjectResult> ImportArchiveAsync(string archivePath, string name,
                                                   Calibration calibration, CancellationToken ct);
}
 
// Reçoit un modèle de planche déjà calculé par le domaine. Ne décide de rien.
public interface ISheetRenderer
{
    Task<byte[]> RenderAsync(SheetLayout layout, CultureInfo culture, CancellationToken ct);
}
```
 
---
 
## 8. Journalisation et observabilité
 
- **Serilog**, sortie **JSON structurée** (pas du texte : la destination Graylog doit être branchable sans travail de parsing).
- **Rotation quotidienne** et rétention configurable par nombre de fichiers. Sans rétention, le volume croît indéfiniment.
- Destination par défaut : le volume `/app/data/logs`, **jamais le dossier du projet**. Les journaux contiennent des chemins absolus, l'URL du générateur et des messages d'erreur qui nomment des projets ; ils ne doivent pas partir avec une archive de projet. Ils ne contiennent **pas de prompt** : DEC-092 l'écarte, le candidat figeant déjà ses clauses.
- Chaque **Job** porte un identifiant, poussé une seule fois en entrée du cas d'usage via `LogContext.PushProperty`. Il n'est jamais passé en paramètre de méthode en méthode. L'Application ne voyant pas Serilog, l'entrée du cas d'usage est **son point d'appel**, dans l'API (DEC-090).
- L'interface expose un visualiseur de journaux dans la section configuration. Il lit **uniquement** dans le répertoire de journaux, par liste blanche de noms de fichiers (voir MEN-002).
- La journalisation est désactivable par configuration.
- Seuls les **bords** journalisent — démarrage, intergiciel d'erreurs, travailleur des lots ; le domaine et l'Application n'écrivent aucun journal (DEC-090). Format, fichiers et rétention : DEC-091. Le visualiseur : DEC-094.
---
 
## 9. Sécurité — modèle de menace
 
Les menaces sont déduites de l'architecture, non d'une liste générique. Chaque entrée est traçable vers une décision de conception.
 
| Réf. | Menace | Vecteur | Contre-mesure |
|---|---|---|---|
| MEN-001 | **Zip Slip** | Archive importée contenant une entrée `../../` (conséquence directe de DEC-011) | Résoudre le chemin absolu de chaque entrée et vérifier qu'il est bien préfixé par le dossier de destination **avant** écriture. Rejet global de l'archive sinon. |
| MEN-002 | **Traversée de chemin** | Visualiseur de journaux avec nom de fichier en paramètre | Liste blanche de noms. Jamais de concaténation de chemin depuis une entrée utilisateur. **Depuis DEC-094** : le dossier est énuméré, seuls les fichiers ordinaires au nom du motif sont retenus, et c'est le chemin de l'énumération qui est ouvert. |
| MEN-003 | **SSRF** | L'URL du générateur est fournie par l'utilisateur et appelée par le serveur | **Depuis DEC-081** : l'adresse est un réglage de déploiement, jamais modifiable par l'API. Schémas `http` et `https` seulement, ni identifiants, ni requête, ni fragment ; redirections jamais suivies ; proxy jamais utilisé. Pas de liste blanche de ports, qui ne protégerait de rien. Hypothèse de déploiement en réseau de confiance documentée. **Depuis DEC-108** : l'interface règle l'adresse, sous les mêmes règles de forme ; l'écriture n'est acceptée que de la même origine et d'un `Host` local (MEN-010). |
| MEN-004 | **Exposition réseau** | Application sans authentification publiée sur toutes les interfaces par Docker | Documenter `-p 127.0.0.1:8080:8080` comme forme canonique. Avertissement au démarrage si l'écoute n'est pas locale. **Risque accepté** (DEC-093) : un conteneur ne voit pas comment son port est publié ; l'avertissement y est toujours émis, et c'est à l'opérateur de vérifier. |
| MEN-005 | **Entrée image non fiable** | Bombe de décompression, dimensions extrêmes, fichier malformé, décodés par le pipeline de détourage — **et par le rendu de la planche**, qui décode chaque élu (DEC-095) | Plafonds de taille et de dimensions vérifiés **avant** décodage. Échec propre du job, pas d'arrêt du processus. Sur la planche : 8 192 pixels de côté, lus sur l'en-tête. Au détourage (DEC-099) : même borne sur l'en-tête, CRC vérifié, taille décompressée exactement celle que l'en-tête annonce. |
| MEN-006 | **Fuite de secret** | Clé d'API ou identifiants sérialisés dans `project.json` puis partagés | Secrets exclusivement en variables d'environnement. Aucun champ de secret dans le modèle de projet. Test automatisé vérifiant l'absence de secret dans l'export. |
| MEN-007 | **Consommation de ressources** | Lot de génération de taille non bornée | Plafond configurable du nombre de candidats par lot. Annulation coopérative des jobs. |
| MEN-008 | **Exfiltration par lien symbolique à l'export** | Un lien symbolique déposé dans le dossier d'un projet et pointant hors de celui-ci — volume des journaux, dossier personnel, `/etc`. L'export le suit et le place dans une archive que l'utilisateur envoie lui-même | Ne jamais suivre un lien : résoudre le chemin absolu et vérifier le préfixe, comme MEN-001 à l'import. L'export **échoue** en nommant le lien plutôt que de l'ignorer. Doublé par la liste blanche de DEC-050, qui n'autorise que des `.png` et des `.pdf` référencés |
| MEN-009 | **Traversée de chemin par le nom de projet** | `name` est une chaîne libre, issue de l'utilisateur ou d'une archive tierce, et sert à fabriquer le nom du dossier de projet | Translittération vers une liste blanche de caractères, longueur bornée, noms réservés Windows exclus, points et espaces finaux interdits. Vérification que le chemin résolu est sous la racine des projets **avant** toute création. Jamais de concaténation directe, comme l'exige déjà MEN-002 |
| MEN-010 | **Requête intersite et rebinding DNS** | L'API n'a pas d'authentification (§1.5) et le navigateur de l'utilisateur est sur la boucle locale : une page tierce peut lui faire envoyer un formulaire `POST` vers l'API, ou faire résoudre son propre domaine vers `127.0.0.1` pour devenir de même origine | Une requête autre que `GET`/`HEAD` portant un `Origin` différent de l'hôte est refusée (`CROSS_ORIGIN_REFUSED`). `AllowedHosts` restreint par défaut aux noms locaux, ce qui refuse le `Host` d'un domaine rebindé. Aucun en-tête CORS émis. Voir DEC-089 |
| MEN-011 | **Falsification de journal** | Un message d'erreur nomme un dossier de projet, dont le nom est un texte libre, venu au besoin d'une archive tierce ; un saut de ligne dans un journal texte fabrique un faux événement | Tenue par le format : une ligne JSON par événement, toute valeur échappée. Le visualiseur rend chaque ligne comme une chaîne, sans l'interpréter. Voir DEC-096 |
 
---
 
## 10. Localisation
 
- Deux langues en v1 : **français** et **anglais**. Aucune chaîne en dur, nulle part.
- Front : `react-i18next`, un fichier JSON par langue.
- Back : `.resx` et `IStringLocalizer`, réservés à ce que le serveur produit réellement.
- **L'API renvoie des codes d'erreur, pas des messages traduits** (`GENERATOR_UNREACHABLE`, `SHEET_CAPACITY_EXCEEDED`, `ARCHIVE_REJECTED`…). L'API reste agnostique de la langue et les traductions vivent en un seul endroit.
- **Le PDF contient du texte** (mention de calibration, étiquettes). La requête d'export transporte donc la culture cible.
- Ne pas coder en dur les formats de date et de nombre. Prévoir que les chaînes traduites changent de largeur.
- Les noms de tailles sont des **clés de traduction**, jamais des chaînes affichées telles quelles. L'identifiant est anglais et sans accent (DEC-037) ; le libellé vient du catalogue : `Huge` s'affiche « Très Grande » en français et « Huge » en anglais.
---
 
## 11. Journal des décisions
 
Format : contexte implicite, choix, conséquence. On n'édite pas une fiche : on en ajoute une nouvelle qui supersède.
 
**DEC-001 — Géométrie double, verrouillée au projet.**
Choix : `TentePliee` et `PionASocle`, paramétrables, mais fixées à la création du projet.
Conséquence : pas de mélange de géométries sur une planche ; l'abstraction se réduit à l'appendice sous la ligne des pieds.
*Partiellement superséde par DEC-030 : le verrouillage après création tombe, le reste demeure.*
 
**DEC-002 — Le verso représente le même personnage retourné.**
Choix : deux vues distinctes du même sujet, pas un miroir du recto.
Conséquence : contrainte la plus forte du projet ; justifie DEC-003.
 
**DEC-003 — Production du couple par génération jumelée puis découpe.**
Choix : une seule génération produit une planche de rotation contenant les deux vues, découpée immédiatement.
Conséquence : cohérence garantie par construction ; résolution divisée par deux. Isolé derrière `IPawnPairProducer` pour substitution.
 
**DEC-004 — Découplage production d'images / mise en page.**
Choix : après découpe, le système ne manipule que deux images indépendantes.
Conséquence : le choix de génération n'impose aucune contrainte sur le placement. Lève l'objection initiale sur DEC-003.
 
**DEC-005 — Une seule taille de pion par page, N pages par projet.**
Choix : grille uniforme par page ; regroupement par taille au moment de la mise en page.
Conséquence : pas de bin packing ; le projet reste l'unité de cohérence stylistique.
 
**DEC-006 — Style verrouillé au niveau projet.**
Choix : rendu, palette et formulation figés à la création, non surchargeables par gabarit.
Conséquence : seule garantie *structurelle* de cohérence visuelle. Changer de style implique de dupliquer le projet.
*Partiellement superséde par DEC-030 : le style reste une propriété de projet et n'est jamais surchargeable par gabarit — c'est le figement à la création qui tombe.*
 
**DEC-007 — Fournisseur d'images local en v1.**
Choix : ComfyUI via son API HTTP, plutôt qu'une API en ligne.
Conséquence : accès aux LoRA, ce dont dépend la faisabilité de DEC-003. Dépendance à un poste équipé.
 
**DEC-008 — Détourage local et systématique.**
Choix : modèle ONNX embarqué, jamais délégué au fournisseur d'images.
Conséquence : comportement identique quel que soit le fournisseur, gratuit, hors ligne. Fournisseur d'exécution configurable.
 
**DEC-009 — Composition de prompt déterministe ; modèle de langage différé.**
Choix : `TemplatePromptComposer` seul en v1, derrière `IPromptComposer`.
Conséquence : pas de seconde dépendance modèle, pas de contention VRAM, pas de variance introduite là où DEC-006 l'interdit. Voir §13.
 
**DEC-010 — Templates de prompts en fichiers de données.**
Choix : un fichier par univers, livré avec l'application, éditable par l'utilisateur.
Conséquence : ajouter un univers n'est pas une recompilation.
 
**DEC-011 — Persistance en dossier clair + export ZIP.**
Choix : `projet.json` et un dossier `images/`, pas de base de données.
Conséquence : portable et pérenne. Introduit MEN-001.
 
**DEC-012 — Secrets hors du fichier projet.**
Choix : variables d'environnement uniquement.
Conséquence : invariant « un projet est partageable sans réflexion ». Voir MEN-006.
 
**DEC-013 — Quantité par gabarit.**
Choix : un gabarit porte un nombre d'exemplaires ; une seule génération, N impressions.
Conséquence : couvre le cas majoritaire (les figurants interchangeables) et divise le coût de génération.
 
**DEC-014 — Validation candidat par candidat.**
Choix : un seul mode de validation en v1.
Conséquence : interface plus simple ; la validation en lot est différée.
 
**DEC-015 — Tailles prédéfinies calées sur la grille de jeu.**
Choix : Moyenne / Grande / Très Grande / Gigantesque, valeurs en mm surchargeables dans les réglages.
Conséquence : compatibilité avec les tapis standards, sans fermer la porte aux grilles de 3 cm.
*Étendue par DEC-031 : ajout de la taille Petite, et adossement explicite des emprises à la table de référence du chapitre 14.*
 
**DEC-016 — A4 et US Letter fournis, dimensions pilotées en millimètres.**
Choix : aucun format codé en dur ; le moteur prend une largeur et une hauteur.
Conséquence : ajouter un format est une entrée de configuration. Rappel : Letter est plus large de 6 mm et plus court de 18 mm ; une planche A4 imprimée sur Letter perd sa dernière rangée.
 
**DEC-017 — Repères d'impression obligatoires.**
Choix : calibration, coupe et pliage non désactivables en v1.
Conséquence : protection contre le tirage hors échelle, au prix d'un peu de surface.
 
**DEC-018 — Front React, API ASP.NET, conteneur unique.**
Choix : React pour l'écosystème et la couverture par les assistants de code ; build multi-étapes.
Conséquence : deux chaînes de compilation, une seule image, pas de CORS.
 
**DEC-019 — PDFsharp plutôt que QuestPDF.**
Choix : PDFsharp, sous licence MIT.
Conséquence : le projet et **ses utilisateurs aval** restent libres. QuestPDF est désormais une licence commerciale « source-available », non approuvée OSI, dont sont exclus le secteur public et les sociétés cotées quel que soit leur chiffre d'affaires — une obligation qu'un utilisateur aval n'aurait pas vue venir. Techniquement, le besoin est du placement d'images à des coordonnées précises, pas un moteur de flux de document.
 
**DEC-020 — Architecture hexagonale allégée, quatre projets.**
Choix : Domain / Application / Infrastructure / Api.
Conséquence : domaine testable sans mock ; adaptateurs substituables. Risque surveillé : la multiplication des mappings.
 
**DEC-021 — DTO en record, mapping manuel, pas d'AutoMapper.**
Choix : mapping explicite en méthodes d'extension.
Conséquence : plus verbeux, mais entièrement visible en relecture — ce qui est le mécanisme de sécurité choisi en DEC-027.
 
**DEC-022 — Journaux en volume dédié.**
Choix : `/app/data/logs`, séparé du volume des projets.
Conséquence : aucune fuite de prompt, de chemin ou d'URL via une archive partagée.
 
**DEC-023 — Localisation dès la v1, codes d'erreur côté API.**
Choix : français et anglais ; l'API ne renvoie pas de texte traduit.
Conséquence : traductions centralisées côté front ; la requête d'export PDF transporte la culture.
 
**DEC-024 — Paramétrage d'unité mixte.**
Choix : trois champs obligatoires (race, classe, taille), champs optionnels cochables issus d'un catalogue éditable, champ de détails libre, prompt final éditable.
Conséquence : un champ décoché signifie « non contraint », pas « absent ». Formulation à soigner dans l'interface.
*Précisé par DEC-028 : ce qui est éditable est la clause sujet, pas le prompt entier.*
 
**DEC-025 — Univers en paramètre global, fantasy seul en v1.**
Choix : le champ existe, un seul jeu de templates est livré.
Conséquence : extension future sans migration de schéma.
 
**DEC-026 — Nom de code : Pawnsmith.**
Choix : retenu contre *Simulacra* et *Standee*.
Conséquence : préfixe de tous les espaces de noms et nom du dépôt. Décision volontairement prise tôt.
 
**DEC-027 — Périmètre de compréhension du porteur.**
Choix : le code est produit par assistance, mais **intégralement relu**, tranche par tranche, avec compréhension approfondie de la couche ONNX.
Conséquence : contraint le style de code vers l'explicite (DEC-021) et impose un découpage en petites tâches relisibles (chapitre 12).
 
**DEC-028 — Le prompt est composé de trois clauses ; seule la clause sujet est éditable.**
Choix : le composeur assemble `clauseCadrage + clauseSujet + clauseStyle`. Le gabarit stocke `clauseSujet`, modifiable. `promptResolu` devient une valeur dérivée, recalculée, affichée en lecture seule.
Conséquence : précise DEC-024, dont la formulation « prompt final éditable » rendait les clauses verrouillées atteignables par un simple champ texte libre — la garantie de DEC-006 n'était donc que nominale. Le verrouillage est désormais porté par la signature de `IPromptComposer` : aucune méthode ne permet de fournir une clause cadrage ou style depuis le niveau du gabarit.
 
**DEC-029 — La clause cadrage n'est jamais exposée dans l'interface.**
Choix : elle reste une constante de l'application. Son point d'extension est le template de workflow ComfyUI, fichier de configuration éditable sur disque.
Conséquence : la clause cadrage garantit la découpe verticale (étape 3) et la fiabilité du détourage (étape 4). La modifier produit un défaut fonctionnel, pas un choix esthétique — ce n'est donc pas un réglage utilisateur. L'échappatoire experte existe déjà, elle est auto-sélective, et elle ne coûte aucune ligne de code.
 
**DEC-030 — Le désalignement remplace le verrouillage.**
Choix : `univers`, `style`, `geometrie` et `formatPapier` sont modifiables après création. Un candidat est **désaligné** lorsque son `promptUtilise` figé diffère du prompt résolu que produirait le composeur aujourd'hui. L'état est **calculé** à la volée, jamais stocké.
Conséquence : supersède le verrouillage après création de DEC-006 et de DEC-001 ; le reste de DEC-001 (deux géométries, aucun mélange sur une planche) demeure. Un avertissement au moment de l'édition aurait été un mécanisme de consentement, pas de sécurité : il arrive quand l'utilisateur est motivé, et le problème n'apparaît qu'à l'export. Le désalignement, lui, est visible exactement là où il compte, désigne **quels** candidats sont concernés, et n'est pas destructif. Changer de géométrie ou de format ne désaligne rien : ce sont des paramètres de rendu (DEC-004). Reste à trancher : ce que l'export fait d'un candidat élu mais désaligné.
 
**DEC-031 — Cinq tailles nommées d'après les règles ; l'échelle S/M/L/XL/XXL est écartée.**
Choix : les tailles s'appellent `Petite`, `Moyenne`, `Grande`, `TresGrande`, `Gigantesque`, et correspondent aux catégories Small, Medium, Large, Huge, Gargantuan des règles. Leurs emprises sont celles du chapitre 14 : 25,4 / 25,4 / 50,8 / 76,2 / 101,6 mm. `Minuscule` (Tiny, 12,7 mm) est écartée de la v1 et devient EVO-011.
Conséquence : une échelle de type S/M/L/XL/XXL était impraticable parce que **Small et Medium occupent la même case de 5 pieds**. Une échelle à cinq crans calée sur les emprises produit donc soit deux crans identiques, soit une emprise Small inventée qui n'existe dans aucun corpus de règles — ce qui viole la règle « les valeurs physiques ne s'inventent jamais ». Ce qui distingue réellement ces deux catégories est la **hauteur**, dimension que le modèle sépare déjà de l'emprise (§3.1). Reprendre le vocabulaire des règles évite en outre d'imposer aux joueurs une taxonomie parallèle à celle de leur table.
Contrepartie assumée : `Petite` et `Moyenne` partageant l'emprise mais pas la hauteur, elles ont des hauteurs de cellule différentes et **ne peuvent pas partager une page** sous DEC-005. Un unique gnome coûte une page. C'est acceptable en v1 et c'est la meilleure justification d'EVO-005.
Étend DEC-015.
 
**DEC-032 — La progression des hauteurs est une décision de conception, bornée par le papier.**
Choix : `hauteurPion` n'est dérivée d'aucune règle de jeu. Seule la hauteur de `Moyenne` est mesurée en T0b ; les autres s'en déduisent par une loi de progression **monotone, documentée et arbitrée**, dont la seule contrainte dure est le plafond du §5.7 : `2 × (hauteurPion + hauteurAppendice) ≤ hauteurUtile`, soit environ **112 mm** de hauteur de pion si US Letter doit rester utilisable.
Conséquence : trois choses cessent d'être implicites. **Un**, les règles de jeu ne définissent que l'espace occupé au sol, jamais la taille des créatures — aucune source ne peut fournir ces hauteurs, et il est vain d'en chercher une. **Deux**, à l'échelle vraie de la grille (1 pouce pour 5 pieds, soit environ 1:60), un humanoïde de 1,80 m mesurerait 30,5 mm ; les valeurs provisoires actuelles surdimensionnent donc d'un facteur ~1,6 pour la lisibilité à un mètre, et c'est délibéré. **Trois**, la progression proportionnelle aux emprises (50 / 100 / 150 / 200 mm) est physiquement impossible : `Gigantesque` dépasserait la feuille du double. L'instruction du protocole T0 « déduire les autres tailles par proportion » était fausse et est corrigée.
Défaut constaté et corrigé par cette fiche : la valeur provisoire `Gigantesque.pawnHeightMm = 125` donnait `2 × (125 + 10) = 270 mm > 263 mm` de hauteur utile A4 — **capacité nulle sur les deux formats**. Le cahier des charges T1 en fait désormais un test de non-régression.
 
**DEC-033 — T0 est scindée ; le CLI de T1 produit les gabarits de calibration.**
Choix : **T0a** (verdict DEC-003, aucun code, aucune impression) passe avant tout. **T0b** (mesures physiques) passe **après** le code de T1 et utilise le CLI jetable de B.7 pour tirer ses gabarits, avec des fichiers de calibration variantes passés en `--calibration`. Le « script Python de gabarit » que le protocole T0 posait en prérequis est supprimé.
Conséquence : ce script aurait dû tracer des pions à hauteur, volet et marge variables sur une planche A4 calibrée — c'est-à-dire réimplémenter le moteur de T1 en jetable, sans test, pour l'abandonner trois jours plus tard. Le CLI de B.7 est déjà défini pour exactement cet usage (« permettre un tirage papier avant l'existence de l'interface »). L'ordre devient : Fondations → T0a → T1 (code) → T0b → T1 (validation B.9). T1 reste écrivable sans aucune valeur mesurée, puisque B.2 impose au code de les lire et jamais de les connaître ; ce sont ses **critères d'acceptation** qui attendent T0b, pas son code.
Risque identifié : un défaut de géométrie dans T1 contaminerait les gabarits de T0b, et l'on mesurerait sur du faux. Mitigation : l'étape 2 de T0b (le trait de 100 mm) est aussi le test du moteur ; vérifier au réglet le trait de calibration **et** la hauteur totale dépliée d'une cellule avant de découper quoi que ce soit.
 
**DEC-034 — La coquille de l'interface est retenue d'une maquette exploratoire ; son contenu est rejeté.**
Choix : d'une maquette produite le 29 août 2026 par un modèle de langage disposant de très peu de contexte projet, retenir quatre choses — la navigation en cinq étapes calquée sur le pipeline du chapitre 4, l'aperçu de planche comme pièce maîtresse de l'écran de mise en page, le panneau de paramètres séparant obligatoires et optionnels, et l'indicateur de capacité de page. Écarter tout le reste. Le résultat est le chapitre 15.
Conséquence : c'est le **rejet** qui est la partie utile de cette fiche, pas l'adoption. La maquette proposait des comptes utilisateurs et un partage (contre §1.5), une planche de jetons ronds au lieu d'unités dépliées, quarante-huit figurines sur un A4 là où la géométrie en autorise douze en taille Moyenne, un mélange de tailles sur une même page (contre DEC-005), des repères d'impression absents et désactivables (contre DEC-017), un placement libre à la souris (contre le fait que la planche est calculée), et `race` rangée parmi les champs optionnels avec `classe` absente (contre DEC-024). Sans cette fiche, la même maquette ressort dans six mois et les mêmes erreurs sont réintroduites une par une, chacune paraissant raisonnable isolément.
Enseignement de méthode, qui vaut au-delà de cette maquette : l'essentiel de ces écarts vient de ce qu'elle a été produite **sans le glossaire du chapitre 2 ni les fiches DEC-001, 005, 006, 017 et 024**. Une maquette n'est pas un document d'entrée, c'est un document de sortie.

**DEC-035 — Les marges de page restent uniformes.**
Choix : un seul `pageMarginMm`, appliqué aux quatre côtés. La formule du §B.5.2 conserve `pageWidth − 2 × pageMarginMm`. Les marges indépendantes par côté sont écartées de la v1.
Conséquence : on renonce sciemment à de la capacité. Les imprimantes domestiques ont rarement une zone non imprimable symétrique — beaucoup acceptent 3 à 5 mm sur trois côtés mais exigent 12 à 15 mm en bas, à cause de l'entraînement du papier. Une marge uniforme doit donc prendre **le pire des quatre côtés** et gaspille la différence sur les trois autres. Le gain abandonné est mesurable : sur A4, le passage de 6 à 7 colonnes en `Petite` et `Moyenne` se joue à **7,1 mm** de marge latérale, soit **+2 pions par page** pour une imprimante qui tolérerait 5 mm sur les côtés.
Motif du choix malgré ce gain : quatre marges doublent la surface d'erreur d'un calcul géométrique **relu à la main** (DEC-027), pour un bénéfice borné à deux tailles sur cinq et dans une plage étroite. Et la décision est réversible dans le bon sens — passer d'une marge à quatre est une extension, l'inverse serait une régression. À réexaminer si T0b mesure une asymétrie forte sur l'imprimante retenue : ce sera alors une décision fondée sur une mesure et non sur une hypothèse.

**DEC-036 — Le paysage est une entrée de configuration, pas une bascule d'interface.**
Choix : le paysage s'obtient en ajoutant une entrée à `paperFormats`, par exemple `"A4Paysage": { "widthMm": 297.0, "heightMm": 210.0 }`. Aucune bascule d'orientation dans l'interface, et **aucune sélection automatique** de l'orientation la plus capacitaire. Aucune entrée paysage n'est livrée en v1 ; le format existe si on l'écrit.
Conséquence : coût nul, DEC-016 le permettait déjà — le moteur ne connaît qu'une largeur et une hauteur en millimètres. Surtout, cela évite de traiter le paysage comme un gain général, ce qu'il n'est pas : il fait **perdre une rangée entière** (hauteur utile de 263 à 176 mm sur A4) et gagner des colonnes, donc son intérêt dépend de la taille.

| Taille | A4 portrait | A4 paysage | |
|---|---|---|---|
| `Petite` | 6 × 2 = **12** | 9 × 1 = 9 | portrait |
| `Moyenne` | 6 × 2 = **12** | 9 × 1 = 9 | portrait |
| `Grande` | 3 × 1 = 3 | 5 × 1 = **5** | **paysage, +67 %** |
| `TresGrande` | 2 × 1 = **2** | 3 × 0 = **0** | paysage inutilisable |
| `Gigantesque` | 1 × 1 = **1** | 2 × 0 = **0** | paysage inutilisable |

La dégradation est propre et déjà spécifiée : un format paysage choisi avec des `TresGrande` produit une capacité nulle, donc l'erreur explicite du §B.5.2 nommant la taille en cause. Une sélection automatique de l'orientation serait en revanche un **choix implicite du moteur**, ce que le chapitre 0 du cahier des charges interdit. Si le gain sur `Grande` se révèle utile à l'usage, ce sera une EVO avec sa fiche, pas un comportement qui apparaît tout seul.

**DEC-037 — L'anglais est la langue du code et des prompts ; le français est celle de l'interface et des documents.**
Choix : trois registres, séparés une fois pour toutes. **Le code** — types, membres d'énumération, méthodes, journaux, et les clés de `calibration.json` comme du manifeste — est en anglais. **L'interface** est traduite, par les catalogues `fr` et `en` (chapitre 10) ; aucune chaîne affichée n'est écrite en dur, quelle que soit sa langue. **Les prompts**, préenregistrés comme générés, sont en anglais. Cette fiche supersède la clause « repris tel quel dans le code » du chapitre 2.

| Glossaire (ch. 2) | Identifiant de code |
|---|---|
| Projet | `Project` |
| Univers | `Universe` |
| Style | `Style` |
| Géométrie | `Geometry` — valeurs `FoldedTent`, `TabAndSocket` |
| Gabarit | `Blueprint` |
| Candidat | `Candidate` |
| Couple recto/verso | `PawnPair` |
| Taille | `Size` — valeurs `Small`, `Medium`, `Large`, `Huge`, `Gargantuan` |
| Planche | `Sheet` |
| Catalogue | `Catalog` |
| Job | `Job` |

Conséquence : on tranche une incohérence qui existait déjà, plutôt que d'en créer une. Le chapitre 7 écrit `ISheetRenderer`, `SheetLayout`, `PawnPair`, `RawImage` et `IProjectRepository` en anglais depuis le premier jour, et une signature comme `Compose(Gabarit gabarit, Style style, Univers univers)` mélangeait deux langues dans la même ligne.
Sur les tailles, le gain est réel et pas seulement cosmétique : DEC-031 établit que les cinq catégories **sont** Small, Medium, Large, Huge et Gargantuan dans les règles du jeu. `Medium` n'est donc pas la traduction de `Moyenne`, c'est le nom d'origine, et l'on supprime une couche de traduction au lieu d'en ajouter une.
Sur les prompts, le motif est technique : les modèles de diffusion sont entraînés très majoritairement sur des légendes anglaises, et un prompt français produit des résultats moins fidèles. Conséquence à assumer dans l'interface : dans une session en français, la clause de style et le prompt résolu éditable restent en anglais. Cela se dit à l'utilisateur, cela ne se découvre pas.
Les clés de fichier suivent le code plutôt que l'inverse. Un `calibration.json` en français lu par un code en anglais imposerait une couche de correspondance permanente entre ce qu'on lit dans le fichier et ce qu'on lit dans le code — exactement le genre d'écart qui coûte cher en relecture (DEC-027).
Portée du renommage : **le chapitre 11 n'est pas touché.** Les fiches sont des enregistrements datés, et ce document interdit d'en modifier une. DEC-015 continue donc de dire « Moyenne / Grande / Très Grande / Gigantesque », et c'est correct : c'est ce qui a été décidé ce jour-là.

**DEC-038 — Conventions géométriques du domaine.**
Choix : cinq conventions, posées ensemble parce qu'elles découlent toutes de la même chose — le domaine raisonne en millimètres, relativement à une unité, et ne sait rien de la page ni du PDF.

| # | Convention |
|---|---|
| 1 | **L'origine est le coin supérieur gauche de l'unité.** X croît vers la droite, **Y croît vers le bas**. |
| 2 | **Les coordonnées sont relatives à l'unité**, jamais à la page. Placer les unités sur une page est un calcul distinct, qui vient après. |
| 3 | **Un polygone est implicitement fermé** : le dernier sommet rejoint le premier, et le premier n'est pas répété à la fin. |
| 4 | **Tout est en millimètres dans le domaine.** La conversion en points est centralisée dans une seule fonction du rendu (§B.6) et n'existe nulle part ailleurs. |
| 5 | **Une incohérence interne de la calibration est rejetée à la construction**, avec une exception nommant la valeur fautive. |

Conséquence, convention par convention. **Le sens de l'axe (1)** n'est écrit dans aucun document : le §B.4.1 décrit les bandes « de haut en bas » sans dire où est l'origine. Le choix suit l'ordre de lecture de la spécification, de sorte que le code se parcoure dans l'ordre du document. La convention mathématique inverse, Y vers le haut, obligerait à retourner l'axe quelque part entre le domaine et le rendu — et une inversion de signe dans un calcul géométrique ne se voit pas au test, elle se voit à l'impression.
**Les coordonnées relatives (2)** sont ce qui permet à une unité d'être calculée une fois et placée N fois sur une page, sans recalcul par cellule.
**La fermeture implicite (3)** évite qu'un tracé émette un segment de longueur nulle. C'est une convention arbitraire — l'inverse se défendrait — mais elle doit être écrite quelque part, sinon la moitié du code fermera le polygone et l'autre moitié le supposera fermé.
**Le millimètre (4)** est la reprise de l'exigence du §B.6, énoncée ici comme une propriété du domaine et pas seulement une consigne de rendu : aucun type du domaine ne porte de point, de pixel ou de pouce.
**Le rejet des incohérences (5)** couvre ce que la liste de validation du §B.3 ne couvre pas. Cette liste porte sur le **manifeste** — fichiers présents, taille connue, quantité ≥ 1 — et pas sur la cohérence interne de la **calibration**. Premier cas rencontré : un onglet plus large que le pion produit une abscisse d'onglet négative, donc un contour retourné sur lui-même, valide en apparence, tracé, imprimé, et découvert au ciseau. Le cas est impossible avec les valeurs actuelles, mais `calibration.json` est précisément le fichier qu'on éditera à la main pendant T0b en faisant varier ces valeurs-là.
Portée : ces conventions engagent `Pawnsmith.Domain` et tout ce qui le consomme. Elles ne sont pas négociables tranche par tranche ; les changer est une nouvelle fiche.

**DEC-039 — Une troisième géométrie, sans aucun support.**
Choix : ajouter `NoSupport` à `Geometry`. L'appendice a une hauteur nulle, le contour se réduit au rectangle des deux images, et il ne reste que le pli principal. Supersède le mot « double » de DEC-001, dont tout le reste demeure.
Conséquence : couvre le cas de l'utilisateur qui ne veut que la découpe — pour coller le pion sur son propre socle, le pincer dans une attache, ou simplement disposer des figures à plat. Le §5.2 réduisait déjà la différence entre géométries à « ce qui est ajouté sous la ligne des pieds » : l'absence d'ajout en est une valeur légitime, et le modèle l'accueille sans nouvelle abstraction.
Effet secondaire, plus étroit qu'il n'y paraît : sans appendice, la cellule mesure `2 × hauteurPion` au lieu de `2 × (hauteurPion + appendice)`. Avec les hauteurs provisoires, cela ne fait gagner une rangée que dans **un seul cas — `Small` sur A4, qui passe de 12 à 18 pions**. Partout ailleurs la capacité est identique : la cellule raccourcit de 16 à 20 mm, ce qui ne suffit jamais à laisser passer une rangée de plus. Le raccourcissement ne peut en revanche jamais faire perdre de capacité, et c'est verrouillé par un test. T0b déplacera probablement cette frontière.
Ce n'est pas EVO-003, qui décrit une autre troisième géométrie : deux pièces séparées collées sur une âme carton, avec repères d'alignement. Celle-là reste différée.

**DEC-040 — Les cotes de l'onglet sont réglables par l'utilisateur.**
Choix : `tabWidthMm` et `tabHeightMm` cessent d'être des valeurs de calibration figées et deviennent modifiables. Elles gardent une valeur par défaut dans `calibration.json` ; leur surcharge par projet relève du schéma de T2.
Conséquence : cette fiche existe surtout pour nommer une **troisième catégorie de valeur physique**, que le projet confondait jusqu'ici avec les deux autres.

| Catégorie | Exemples | Qui la détermine | Modifiable ? |
|---|---|---|---|
| Mesure d'imprimante | `pageMarginMm`, `scaleCorrectionFactor` | La machine, en T0b | Non — elle se mesure |
| Préférence d'usage | `gutterMm` | La dextérité au ciseau | Ouvert, voir §15.6 |
| **Matériel possédé** | **`tabWidthMm`, `tabHeightMm`** | **La fente des socles du commerce** | **Oui** |

La distinction n'est pas rhétorique. Une marge de page mal réglée produit des pions rognés sans que rien ne le signale, d'où son verrouillage. La largeur de l'onglet, elle, est la cote d'un objet que l'utilisateur tient en main et qui change avec la marque de socle qu'il achète : la verrouiller obligerait à recalibrer le projet entier pour une raison qui n'a rien à voir avec l'impression.
À ne pas perdre de vue : `tabHeightMm` entre dans la hauteur de cellule du §B.5.2. Changer de socles change donc la capacité des pages, et une planche qui tenait en 12 pions peut en tenir 10. C'est correct, et cela doit être visible dans l'interface plutôt que découvert à l'export.

**DEC-041 — Le recto et le verso d'un même pion partagent une seule échelle.**
Choix : les deux images d'un couple sont mises à l'échelle par un facteur unique, calculé pour que les deux tiennent dans la boîte. Le §B.4.4, qui décrit le placement image par image, est corrigé en conséquence.
Défaut constaté qui motive cette fiche, et il est physique, pas esthétique : le §B.4.4 traite chaque image indépendamment, donc deux vues d'un même personnage n'ayant pas exactement le même encombrement en pixels sortent à des hauteurs différentes. Mesuré sur des illustrations réelles, en `Medium` et en `Large` :

| Personnage | Recto | Verso | Écart |
|---|---|---|---|
| orc lancier | 20,5 mm | 22,7 mm | 2,2 mm |
| orc marteau | 35,8 mm | 32,7 mm | 3,1 mm |
| orc fléau | 36,1 mm | 38,2 mm | 2,1 mm |
| troll massue | 70,0 mm | 65,5 mm | **4,5 mm** |

Conséquence : après pliage, la face arrière dépasse la face avant de plusieurs millimètres, et le pion n'est pas symétrique. C'est exactement le genre de défaut que la bible signale comme invisible au test et visible au ciseau. Le couple recto/verso est déclaré indissociable au chapitre 2 — la mise à l'échelle doit l'être aussi.

**DEC-042 — La clause de cadrage impose la pose ; la largeur du pion ne bouge pas.**
Choix : contraindre l'image à la source plutôt que la boîte qui l'accueille. La clause de cadrage du §4.1 gagne une exigence de **pose** — silhouette entière, armes et bras compris, tenant dans un cadre portrait d'au moins deux fois plus haut que large, aucun membre ni aucune arme n'élargissant la silhouette. `pawnWidthMm` reste égal à l'emprise de grille, et la règle de mise à l'échelle du §B.4.4 est inchangée.
Contexte mesuré, sur des illustrations réelles en `Medium` (boîte de 22,4 × 48,5 mm) : **les huit images étaient limitées par leur largeur, aucune par sa hauteur**. La hauteur imprimée allait de 20,5 à 38,2 mm pour une hauteur disponible de 48,5 — soit un rapport de **1,87** entre deux pions de taille pourtant identique, et 20 à 58 % de hauteur perdue sur chacun.
Conséquence, et c'est ce qui motive le choix : deux autres leviers existaient, et aucun ne réglait la cause. **Élargir `pawnWidthMm`** aurait coûté une colonne par page — six au lieu de sept en `Medium` sur US Letter — sans sauver le cas qui a déclenché le constat, un orc tenant sa lance à l'horizontale sur toute la largeur de l'image : aucune largeur raisonnable ne le rattrape. **Corriger après coup** n'a pas de sens quand la cause est en amont. Une pose contrainte, elle, rend le problème structurellement absent, et donne au passage ce qui manquait le plus : **des créatures de même espèce sortant toutes à la même hauteur**, ce qui est le critère de qualité visuelle d'une planche.
Contrepartie assumée : le catalogue de poses se restreint. Une figurine de 25 mm vue à un mètre n'a de toute façon pas besoin d'une pose dynamique, et le §4.1 rappelle que cette clause n'est pas une préférence esthétique mais ce qui rend l'étape suivante fiable.
**La clause ne couvre pas tout, et l'écart doit être signalé.** Elle gouverne ce que le générateur produit, pas ce qui entre par ailleurs : les images déjà en main, et plus tard l'import d'images externes (EVO-010), échappent à toute clause. Le moteur conserve donc sa règle — l'image entre dans sa boîte quoi qu'il arrive — mais **signale toute image dont la largeur est le facteur limitant**, en nommant l'élément et la hauteur réellement obtenue. Une incohérence invisible devient une information sur laquelle agir.
Relève de T3 pour la clause, et de T1 pour le signalement. La clause reste inaccessible depuis l'interface (DEC-029).

**DEC-043 — T0a est concluante : DEC-003 tient, et le modèle n'est pas celui que l'on croyait.**
Choix : le test décisif du protocole T0a a été mené le 1er septembre 2026 sur l'instance locale. **Verdict concluant** — la génération jumelée est retenue, DEC-003 et DEC-004 restent en vigueur, et T4 peut être spécifiée sur cette base. Le prompt de référence ci-dessous devient la matière première des templates de T3 (DEC-010).

Résultat mesuré sur trois sujets, une seule génération chacun, sans tri ni relance : deux sujets à 7 critères sur 8, un à 5. Le seuil était de 6 sur 8 pour deux sujets sur trois. Les écarts d'alignement et d'échelle entre les deux vues sont de 0 à 10 pixels sur 832, soit **0 à 1,4 %** — c'est ce qu'on pouvait redouter le plus, et cela ne pose aucun problème.

**Le résultat qui emporte la décision n'est pas dans la grille.** Sur le mage, le bâton tenu de la main gauche apparaît à droite de l'image en vue de face et à gauche en vue de dos ; sur l'éclaireur, l'arc et le carquois basculent de la même façon. **Le modèle effectue une rotation réelle du personnage, pas un miroir de la vue de face.** C'est exactement ce qu'exige DEC-002, et c'est la propriété la plus difficile à obtenir : un modèle produisant deux belles vues avec l'arme du mauvais côté aurait donné un verdict trompeur, cohérent à l'œil et faux au montage.

Le prompt de référence, avec un seul emplacement variable :

```text
Character rotation sheet for a miniature reference: the exact same character
drawn twice in one single image, front view on the left and back view on the
right, side by side in one horizontal row, both figures resting on the same
horizontal ground line, exactly the same height and exactly the same scale,
each view occupying a tall narrow portrait panel.
Subject: {SUJET}
Both views are the identical character: identical armour, identical clothing,
identical colour palette, identical weapon, identical proportions. The right
figure is that same character seen strictly from behind.
Full body from head to feet, the feet touching the bottom edge of the image,
nothing cropped.
Compact narrow silhouette: strict straight standing pose, arms hanging straight
down and held close against the sides, any weapon held vertically flat against
the body, any cape or cloak hanging straight down against the back. Nothing
extends sideways beyond the shoulders: no outstretched arms, no horizontal
weapon, no spread or flowing cape.
Flat uniform pale grey background, completely plain and even, no scenery, no
ground plane, no cast shadow, no drop shadow, soft even diffuse lighting, crisp
clean outlines suitable for automatic cutout. Clean digital illustration, clear
readable shapes.
```

La contrainte de pose compacte, ajoutée au titre de DEC-042, a tenu sur les trois sujets : aucun bras tendu, aucune arme à l'horizontale, aucun élargissement de silhouette. Le défaut que DEC-042 vient corriger ne s'est pas reproduit une seule fois.

| Paramètre | Valeur |
|---|---|
| Modèle | `krea2_turbo_fp8_scaled.safetensors` — **Krea 2 Turbo**, 12 B, fp8 |
| Encodeur de texte | `qwen3vl_4b_fp8_scaled.safetensors` |
| VAE | `qwen_image_vae.safetensors` |
| LoRA | aucun |
| Échantillonneur / ordonnanceur | euler / simple |
| Étapes, CFG, denoise | 8 · 1.0 · 1.0 |
| Dimensions | 1216 × 832, soit ≈ 608 × 832 par vue |
| Durée | 34 à 42 s par planche |

**Correction de documentation que cette fiche entérine.** Le protocole T0 posait « ComfyUI opérationnel avec **FLUX Krea** ». C'est faux : le modèle installé est **Krea 2 Turbo**, d'architecture différente, avec un encodeur Qwen3-VL et un régime de 8 étapes à CFG 1 dont on ne sort pas — augmenter les étapes dégrade le résultat, c'est mesuré. Conséquence à ne pas découvrir plus tard : **les LoRA et les ControlNet de l'écosystème FLUX sont incompatibles.** Toute piste d'amélioration passant par un LoRA de planche de personnage ou un ControlNet de pose devra être cherchée dans l'écosystème Krea 2, pas FLUX.

Deux points de vigilance, à traiter dans leur tranche et pas avant.

**Pour T5 — la bande de sol touche les pieds.** Le modèle ajoute systématiquement une bande de sol de 8 à 10 pixels sur 832, soit environ 1 % de l'image, malgré une consigne explicite l'interdisant. Le fond lui-même est propre : gris uniforme à 99 % sur le meilleur sujet, 83 % sur le moins bon. Ce n'est donc pas un problème de fond, mais d'adjacence : cette bande touche les semelles, et un détoureur qui la conserverait comme partie de la silhouette produirait un pion debout sur une barre brune. Comme le moteur cale l'image sur son bord bas, **cette barre deviendrait la ligne des pieds** et décollerait le personnage de son socle. À vérifier sur les premières sorties du détourage.

**Pour T3 — l'adhérence à l'équipement est imparfaite.** Le sujet 1 demandait `a large battle axe` ; l'orc tient deux dagues. Les deux vues sont cohérentes entre elles, donc la génération jumelée n'est pas en cause, mais la consigne d'équipement a été ignorée. Cela compte dès que l'équipement viendra d'un catalogue : un utilisateur qui coche « hache » attend une hache.

Un dernier constat, à surveiller plutôt qu'à corriger : sur le sujet 1, une cape couvre tout le dos alors que rien ne la laisse deviner de face. C'est le seul échec non systématique du test. Sur un pion de 25 mm l'effet est nul, mais le mécanisme — l'invention d'un élément dorsal — produirait une incohérence visible sur un personnage dont le dos porte quelque chose de structurant.

Les trois planches produites sont conservées dans `refs/gen comfyui krea2/`, non versionnées comme le reste du dossier. Elles servent de matière première aux gabarits de T0b.

**DEC-044 — T0b est reportée ; T1 reste ouverte et le travail continue sur T2.**
Choix : la calibration physique T0b est repoussée à une date non fixée. L'ordre de DEC-033 devient **Fondations → T0a → T1 (code) → T2 → … → T0b → T1 (validation)**. T1 n'est pas close pour autant : elle est **écrite et testée, non validée**.
Conséquence : le report ne bloque qu'une seule chose, la clôture formelle de T1, dont les critères du §B.9 se cochent planche imprimée en main. Il ne bloque **ni T2, ni T3, ni T4, ni T5** : aucune de ces tranches ne touche à une valeur physique. Le code s'en accommode par construction, puisque le §B.2 impose au moteur de **lire** ces valeurs et de ne jamais les connaître — remplacer une hauteur de pion ne demandera pas une ligne de code.
Risque assumé, et il faut le nommer : l'étape 0 de T0b existe pour attraper un défaut de géométrie dans T1, et on construira donc T2 et T3 sur un moteur jamais confronté au papier. L'exposition reste faible pour deux raisons — T2 et T3 ne consomment rien de la géométrie de T1, et celle-ci est couverte par des tests de symétrie et des vérifications par mutation. Si un défaut subsiste, il portera sur des **valeurs**, pas sur du code, et sa correction restera confinée à T1.
Ce que cette fiche ne fait pas : elle ne dispense pas de T0b. Tant qu'elle n'est pas menée, aucune planche ne peut être déclarée conforme, et les valeurs de `config/calibration.json` restent des marqueurs.

**DEC-045 — L'étape 0 de T0b se juge en rapport, pas en millimètres absolus.**
Choix : le contrôle du moteur de l'étape 0 compare les dimensions relevées **au trait de calibration mesuré sur la même feuille**, et non à leur valeur nominale en millimètres. L'ordre des étapes est corrigé en conséquence : mesurer le trait d'abord, contrôler le moteur ensuite.
Défaut corrigé : le protocole demandait de vérifier qu'une cellule mesure `2 × (hauteur + appendice) × facteur`, et concluait qu'un écart au-delà de la tolérance du réglet « arrête T0b, c'est un bug de T1 ». Or le facteur n'est pas encore connu à ce moment — c'est l'étape suivante qui le mesure — et la première planche est nécessairement tirée avec un facteur de 1,0. Une imprimante réduisant de 2 % ferait mesurer 117,6 mm à une cellule de 120, soit 2,4 mm d'écart, très au-delà du réglet. **On aurait conclu à un bug de T1 pour une propriété de l'imprimante.**
Conséquence : le contrôle redevient valide sans rien mesurer de plus, parce que le trait et la cellule subissent exactement la même réduction. C'est précisément ce que garantissait le §B.5.5 en décidant que le facteur d'échelle s'applique **aussi** au trait de calibration — une décision prise pour la lisibilité de la mesure, et qui sert ici une seconde fois.

**DEC-046 — Les noms de fichiers produits par l'application suivent DEC-037, comme leurs clés.**
Choix : le fichier de projet s'appelle **`project.json`** et non `projet.json`. La règle est générale : tout artefact que l'application produit ou lit comme un contrat — nom de fichier, nom de dossier, clé — est en anglais. Le français reste la langue de l'interface traduite et de ces documents. Supersède le nom de fichier du §3.2.
Conséquence : DEC-037 avait tranché les clés et oublié les noms. Un `projet.json` contenant `versionSchema`, `paperFormat` et `blueprints` reproduit exactement l'écart d'une ligne à l'autre que cette fiche avait supprimé ailleurs — `Compose(Gabarit gabarit, Style style)` en était l'exemple. Le coût du changement est nul : aucun projet n'existe encore sur aucun disque, et les sous-dossiers `images/` et `exports/` étaient déjà anglais.
Portée, et la limite est nette : cela concerne ce que **l'application** nomme, pas ce que **l'utilisateur** nomme. Le nom d'un dossier de projet est dérivé du `name` que l'utilisateur a saisi ; il sera français s'il écrit en français, et c'est correct — c'est une donnée, pas un identifiant de code. `calibration.json` et son contenu ne bougent pas, ils étaient déjà conformes.

**DEC-047 — L'identité d'un projet est un identifiant opaque ; le nom du dossier n'a aucune sémantique.**
Choix : `Project` porte un champ `projectId`, UUID v4, généré à la création, **immuable**, jamais dérivé du nom. L'application n'analyse jamais le nom du dossier. `name` est un libellé d'affichage : ni unique, ni stable. L'unicité de `projectId` **n'est pas contrôlée** en v1.
Conséquence : il faut formuler la décision à l'endroit, sinon elle paraît gratuite. `projectId` n'est pas une fonctionnalité, c'est **l'absence d'une contrainte**. DEC-011 justifie la persistance en dossier clair par trois mots — versionnable, sauvegardable, diffable — et les deux premiers supposent qu'on renomme un dossier, qu'on le duplique, qu'on le restaure ailleurs sous un autre nom. Faire du nom du dossier l'identité aurait interdit ces trois gestes sans que la contrainte soit écrite nulle part, et le symptôme aurait été une restauration de sauvegarde silencieusement traitée comme un projet différent.
Contrepartie assumée, et il faut la dire : **en T2, ce champ n'a aucun consommateur.** Ses usages réels arrivent plus tard — la corrélation de journaux du chapitre 8, et la question « ai-je déjà ce projet ? » posée à l'import. On accepte donc d'écrire un champ dont l'utilité est différée, ce que ce projet refuse d'ordinaire. La justification tient parce que l'identité est ce qu'on ne peut pas ajouter après coup : des projets écrits sans identifiant n'en gagnent pas un rétroactivement.
Ce que cette fiche ne tranche pas : le comportement quand deux dossiers portent le même `projectId`. Ce n'est pas une corruption mais une **copie**, et les deux projets restent utilisables. Trancher entre « remplacer » et « garder les deux » suppose de poser la question à quelqu'un, donc une interface, donc T6.

**DEC-048 — Un schéma inconnu est rejeté, jamais migré en silence ; il n'existe pas d'ajout compatible.**
Choix : trois règles pour `project.json`. **Un**, un `versionSchema` supérieur à la version supportée est rejeté sans aucune lecture partielle. **Deux**, un `versionSchema` inférieur est un cas vide en v1 — aucune ligne de code n'est écrite pour lui ; la politique est posée dès maintenant : une migration sera explicite, demandée, versionnée pas à pas, et précédée d'une copie de sauvegarde du dossier, jamais une conversion silencieuse au chargement. **Trois**, un champ inconnu à une version connue est rejeté en le nommant.
Conséquence : le motif du rejet n'est pas la prudence, c'est la **destruction de données**. Un lecteur v1 ouvrant un projet v2 ignorerait les champs qu'il ne connaît pas ; l'écrivain v1, lui, écrit ce que son type de document contient, c'est-à-dire sans ces champs. Ouvrir puis sauvegarder amputerait donc le projet de tout ce que la v2 avait ajouté, et la perte ne se découvrirait qu'en rouvrant le projet avec la version récente.
Le contraste avec `calibration.json` est intentionnel et vaut d'être écrit, parce qu'il paraît incohérent au premier regard : la calibration **accepte et ignore** un bloc inconnu — c'est même la raison d'être du bloc `paper` du §B.2. Les deux fichiers n'ont pas le même contrat. La calibration est écrite à la main et s'annote ; le projet est écrit exclusivement par l'application, et un champ inconnu y signifie soit une corruption, soit une modification manuelle dont l'intention ne peut pas être honorée — l'ignorer la ferait disparaître à la sauvegarde suivante.
Règle qui en découle, et qui est la partie la plus utile de cette fiche : **tout ajout de champ à `project.json` incrémente `versionSchema`.** Il n'existe pas d'ajout compatible.

**DEC-049 — Le candidat fige ses trois clauses, pas le prompt assemblé.**
Choix : le champ unique `promptUtilise` du §3.1 est remplacé par trois champs — `framingClauseUsed`, `subjectClauseUsed`, `styleClauseUsed`. Le prompt assemblé devient une valeur **dérivée**, donc non persistée. Le désalignement se calcule **clause par clause** et rend l'ensemble des clauses désalignées ; `desaligne` reste le fait que cet ensemble ne soit pas vide. Supersède la **forme** du champ, pas son intention.
Conséquence, en trois points. **Un — l'attribution.** DEC-030 justifie le désalignement par le fait qu'il « désigne **quels** candidats sont concernés » plutôt que d'avertir dans le vide ; le même argument vaut d'un cran plus bas, et savoir *quelle clause* a bougé est ce qui rend l'information actionnable. Avec une chaîne unique, l'interface ne peut dire que « quelque chose a changé ». **Deux — le piège du cadrage**, et c'est lui qui emporte la décision. La clause de cadrage est éditable : c'est l'échappatoire experte de DEC-029, le template de workflow ComfyUI. Un utilisateur qui l'ajuste désaligne **toute sa bibliothèque, dans tous ses projets, d'un coup**. C'est le comportement correct — le cadrage a changé, les images n'ont plus été produites sous les mêmes règles — mais avec une chaîne unique il est incompréhensible et sans recours. Avec trois clauses, l'interface dit « le cadrage a changé », et l'utilisateur sait qu'il vient de le faire. **Trois — la cohérence.** Le prompt assemblé rejoint `promptResolu` et `desaligne` du côté des valeurs dérivées, et le critère « aucune valeur dérivée n'est sérialisée » cesse d'avoir une exception qui n'était justifiée nulle part.
Contrepartie assumée, réelle : le §3.1 justifiait `promptUtilise` par « comprendre a posteriori pourquoi un candidat diffère ». Une **copie littérale** de ce qui est parti sur le réseau est un meilleur enregistrement qu'une reconstruction. On l'échange contre un mécanisme consulté à chaque écran, tandis que la relecture forensique d'un prompt est un usage rare.
Contrainte imposée à T4, qui est le prix de cette contrepartie : **T4 envoie exactement l'assemblage des trois clauses qu'il fige.** S'il transforme le prompt après assemblage — substitution supplémentaire, troncature, échappement — la reconstruction n'est plus fidèle et cette fiche doit être rouverte. Ce n'est pas une interdiction de transformer, c'est l'obligation de le signaler.
Deux corollaires à ne pas manquer. La **règle d'assemblage** — ordre cadrage/sujet/style, séparateur `\n` unique et jamais `\r\n`, clauses normalisées et jointes en omettant les vides — devient une **surface de compatibilité** au même titre qu'un schéma : la modifier désaligne tous les candidats de tous les projets, immédiatement, et demande donc une fiche. Et il n'existe **pas de réalignement partiel** : connaître la clause fautive sert à expliquer, jamais à recopier une clause courante sur un candidat, ce qui serait précisément le mensonge que DEC-030 empêche.

**DEC-050 — Deux profils d'archive, et le contenu se décide par liste blanche.**
Choix : l'export produit un **`Backup`** (tout ce que la liste blanche autorise, `exports/` et images jumelées comprises) ou un **`Share`** (`project.json` filtré, images détourées des candidats conservés, rien d'autre). Dans les deux cas l'archive est construite en **énumérant ce qui est autorisé**, jamais en zippant le dossier. Invariant : **une archive contient toujours un `project.json` cohérent avec les fichiers qu'elle contient ; jamais de référence pendante, dans aucun profil.**
Conséquence : MEN-006 cesse d'être une bonne pratique pour devenir une **propriété structurelle**. Une exportation par copie du dossier emporterait ce que l'utilisateur y aura déposé — un `.env` égaré, un `.git/` complet avec son historique, un dossier `logs/` créé à la main malgré DEC-022. Le test change de nature avec elle : on ne vérifie plus qu'un secret nommé est absent, on vérifie qu'**aucune entrée hors liste** n'est présente. Un test exemplaire devient un test exhaustif.
Ce qui motive les deux profils, chiffré : une image jumelée pèse 1 à 2 Mo par candidat, **candidats rejetés compris**, et n'a d'utilité que pour diagnostiquer sa propre découpe ; les PDF de `exports/` sont entièrement reproductibles à partir du projet et de la calibration. Ni l'un ni l'autre n'a de valeur pour un destinataire, et les deux ont une valeur pour soi.
Le point non évident, et c'est là que la fiche gagne son existence : **le profil `Share` filtre `project.json`, il ne se contente pas d'omettre des fichiers.** Omettre les fichiers en laissant les références produirait une archive dont le projet pointe vers des images absentes, donc un état à moitié cassé que l'import devrait tolérer, donc une tolérance qui se propagerait ensuite partout. Le filtrage retire les candidats `Rejected` et leurs fichiers, met `pairedImageFile` à `null`, conserve les `Draft` — un projet partagé en cours d'arbitrage a besoin de ses brouillons, c'est même souvent la raison du partage.
Précision au critère d'acceptation de T2 : « aller-retour export/import sans perte » vaut pour **`Backup`**, à l'octet près. Pour `Share`, le critère devient : sans perte de ce qui n'a pas été délibérément retiré, et projet importé cohérent et rendable.

**DEC-051 — L'import est global, atomique, et ne fusionne jamais.**
Choix : l'archive est **entièrement validée avant qu'un octet ne soit écrit** ; l'extraction a lieu dans un dossier temporaire sur le même volume, renommé vers la destination en dernière étape ; une destination existante, même vide, fait échouer l'import ; il n'y a **aucune fusion** ; le `projectId` de l'archive est **préservé**.
Conséquence : l'import devient atomique du point de vue de l'utilisateur — ou bien le projet est là et complet, ou bien il n'y a rien. C'est la seule forme acceptable quand la destination est le dossier de travail de quelqu'un.
Le refus de fusionner n'est pas de la paresse. Fusionner deux dossiers de projet demande de répondre à des questions que personne n'a écrites : que faire de deux gabarits de même identifiant aux clauses différentes ? de deux candidats élus ? Y répondre dans le code d'import, c'est décider en silence de règles de gestion qui appartiennent à la question C de l'état du projet.
Le `projectId` est préservé et non régénéré parce que le cas dominant est la **restauration de sa propre sauvegarde**, où changer l'identité serait faux. Le cas du doublon est traité par DEC-047 : c'est une copie, pas une corruption.
Cette fiche introduit une contre-mesure que le chapitre 9 n'avait pas : MEN-005 borne les **images**, et l'archive est une **seconde surface de décompression** que rien ne bornait. Nombre d'entrées, taille décompressée totale, ratio de compression global et par entrée, profondeur et longueur des chemins sont plafonnés, en configuration. Ces valeurs s'**arbitrent**, elles ne se mesurent pas : la règle « les valeurs physiques ne s'inventent jamais » ne s'y applique pas, et il serait faux de les marquer `À CALIBRER`.
Détail d'implémentation qui n'en est pas un : la vérification du préfixe de chemin est faite **deux fois**, sur l'inventaire des entrées puis sur chaque chemin résolu au moment d'écrire. MEN-001 est satisfait par la première ; la seconde existe parce qu'une archive peut contenir deux entrées de même nom, ou deux entrées ne différant que par la casse, et que le validateur voit alors une chose et l'extracteur en écrit une autre.

**DEC-052 — `gutterMm` reste dans la calibration ; la question du §15.6 est fermée par la négative.**
Choix : `gutterMm` **ne devient pas** un réglage de projet. `silhouetteMarginMm` non plus. Les seules valeurs surchargeables par projet restent celles que DEC-040 a désignées.
Conséquence : la question du §15.6 était posée comme si la réponse allait de soi — la valeur est une préférence, les préférences vont dans le projet — et le critère de DEC-040 dit le contraire de ce que la formulation suggère. DEC-040 ne classe pas « préférence contre mesure » : il classe selon **qui détermine la valeur**, et il rend modifiable ce qui est déterminé par un objet que l'utilisateur possède et remplace, la fente de ses socles. `gutterMm` est déterminé par sa dextérité au ciseau. Or Pawnsmith est **mono-utilisateur** (§1.5) : une propriété de l'utilisateur est, dans cette application, une propriété globale. La placer au niveau du projet oblige la même personne à ressaisir la même valeur dans chaque projet, et produit à terme des projets qui divergent sur un réglage qui n'avait aucune raison de varier.
Le contre-argument existe et il faut le nommer : une planche de vingt `Small` se découpe plus confortablement avec une gouttière généreuse qu'une planche d'un seul `Gargantuan`, donc la valeur idéale dépend un peu du contenu. Il est réel mais mince, et surtout il ne désigne pas le projet comme bon niveau — il désignerait la **taille**, ce qui est une autre décision, plus coûteuse, et que personne ne demande.
`silhouetteMarginMm`, que le §15.6 classait « entre les deux », bascule du même côté pour une raison qui lui est propre : depuis DEC-042 elle n'absorbe plus une incertitude de cadrage — le cadrage est contraint à la source — mais une imprécision de découpe. C'est encore une propriété de la main.
La décision est prise dans le sens qui se rattrape : ajouter une surcharge est une extension de schéma, donc un `versionSchema` de plus ; retirer une surcharge déjà utilisée par des projets existants serait une régression.
Ce que cette fiche ne ferme pas : `calibration.json` mélange aujourd'hui des mesures d'imprimante et des préférences d'usage, ce qui reste une imprécision de rangement. La scinder un jour est une possibilité, pas un besoin, et certainement pas un préalable à T2.

**DEC-053 — Les surcharges de projet forment une liste close, et le domaine ne les voit jamais.**
Choix : `Project` porte un objet `calibrationOverrides` contenant **exactement** `tabWidthMm` et `tabHeightMm`, membres nullables, toujours écrits, `null` signifiant « valeur de la calibration ». Ce n'est **pas un dictionnaire**. La fusion `calibration ∪ surcharges` a lieu en **un seul endroit**, dans `Pawnsmith.Application`, avant tout appel au moteur ; le domaine reçoit une calibration **effective** et ignore l'existence des surcharges. Une surcharge est validée à la sauvegarde **et** au chargement.
Conséquence : c'est la mise en œuvre de DEC-040, et la partie qui mérite d'être écrite est ce qui a été écarté. Un `overrides` à clés libres aurait été plus court et strictement pire : il rendrait surchargeable tout ce que la calibration contient, `scaleCorrectionFactor` compris, dont DEC-040 explique précisément pourquoi il doit rester verrouillé. Une convention implicite qui ouvre par défaut est l'inverse de ce que le §0 du cahier des charges demande.
La résolution en un seul endroit est le pendant exact du « le rendu ne décide de rien » du §B.6 : **T1 n'est pas modifiée d'une ligne** par cette fiche, parce que le §B.2 avait déjà imposé au moteur de lire les valeurs sans les connaître. C'est le résultat qu'on cherchait en écrivant B.2, et il se vérifie ici.
Conséquence à rendre visible plutôt qu'à découvrir, déjà annoncée par DEC-040 : **la capacité d'une page dépend désormais du projet.** Deux projets identiques avec des socles différents ne tiennent pas le même nombre de pions par page. L'indicateur de capacité du §15.4 doit donc être calculé sur la calibration **effective**, jamais sur le fichier de calibration.
La validation double n'est pas de la ceinture et bretelles : le fichier peut avoir été édité à la main ou provenir d'une archive tierce. Un onglet plus large que le pion produit une abscisse négative, donc un contour retourné sur lui-même, valide en apparence, tracé, imprimé, et découvert au ciseau — c'est le cas nommé par la convention 5 de DEC-038, et une surcharge de projet est exactement le chemin par lequel il arrive maintenant en production.
Effet de bord sur un contrat : valider `tabWidthMm` suppose de le comparer à `pawnWidthMm`, donc le lecteur de projet a besoin de la calibration. La signature indicative du chapitre 7 devient `LoadAsync(path, calibration, ct)`.

**DEC-054 — Deux menaces ajoutées : exfiltration par lien symbolique, traversée de chemin par le nom de projet.**
Choix : ajouter **MEN-008** et **MEN-009** au tableau du chapitre 9, avec leurs contre-mesures. L'export **échoue** en nommant un lien symbolique rencontré, il ne l'ignore pas. Le nom de dossier d'un projet est produit par translittération vers une liste blanche de caractères, bornée en longueur, excluant les noms réservés de Windows, et le chemin résolu est vérifié comme étant sous la racine des projets **avant** création.
Conséquence : le chapitre 9 annonce des menaces « déduites de l'architecture, non d'une liste générique ». C'est sa force, et c'est aussi pourquoi ces deux-là manquaient : la persistance n'existait que comme intention quand il a été écrit, et une menace se déduit d'un code qui existe. L'oubli est structurel, pas un défaut de vigilance — ce qui veut dire qu'il se reproduira à chaque tranche qui introduit une surface, et que la revue du chapitre 9 prévue en T7 doit être faite **par tranche** plutôt qu'une seule fois à la fin.
**MEN-008 est le miroir exact de MEN-001.** Le zip slip fait entrer un fichier là où il ne devrait pas ; le lien symbolique fait **sortir** un fichier de là où il devrait rester — vers le volume des journaux que DEC-022 avait justement isolé, vers un dossier personnel, vers `/etc`. Le vecteur est d'autant plus efficace que la victime envoie l'archive elle-même, en toute confiance, puisque MEN-006 lui a promis qu'un projet est partageable sans réflexion préalable. Les deux menaces se traitent avec la même primitive — résoudre le chemin absolu et vérifier le préfixe — et n'en implémenter qu'une moitié serait une occasion manquée.
**MEN-009 est MEN-002 avec une autre entrée.** MEN-002 interdit de concaténer un chemin depuis une entrée utilisateur pour le visualiseur de journaux ; `name` est une chaîne libre, venue de l'utilisateur ou d'une archive tierce, et elle sert à fabriquer un nom de dossier. `../../logs` en est un nom valide.

**DEC-055 — Aucun champ de projet n'est verrouillé après création ; la sauvegarde ignore l'état antérieur.**
Choix : `universe`, `style`, `geometry` et `paperFormat` sont modifiables après création. Aucune tranche ne porte de verrou, ni T2, ni T3, ni T6. `IProjectRepository.SaveAsync(Project project, CancellationToken ct)` **conserve sa signature** : le dépôt n'a jamais besoin de l'état chargé.
Conséquence : la question posée était de savoir si le verrou tient. Il est mort depuis DEC-030, dont le texte nomme **les quatre champs** et pas seulement le style. La recommandation qui le faisait tomber pour `style` et le renvoyait en règle de gestion de T3/T6 pour les trois autres lit DEC-030 trop étroitement — il ne reste aucune règle à différer, parce qu'il ne reste rien à verrouiller. Le §3.1 est d'ailleurs déjà conforme : les quatre lignes portent « Modifiable ». Le seul texte périmé est le §15.1, qui invoque trois fiches à l'appui d'un verrou que la première a rendu partiel, que la deuxième ne pose plus, et que la troisième n'a jamais posé — DEC-025 dit seulement que le champ `univers` existe et qu'un seul jeu de templates est livré.
Ce que le §15.1 réintroduit sans le vouloir est plus sérieux qu'une imprécision de citation : un verrou est un **mécanisme de consentement**, et DEC-030 l'a écarté explicitement, au motif qu'un avertissement « arrive quand l'utilisateur est motivé, et le problème n'apparaît qu'à l'export ». Laisser le §15.1 en l'état, c'est autoriser une interface future à rétablir le mécanisme que la fiche a rejeté, en se réclamant de la bible.
Conséquence sur le contrat, et c'est la partie qui engage du code. Un verrou aurait obligé la sauvegarde à comparer au projet **tel qu'il a été chargé**, donc à recevoir l'état antérieur, donc à faire du dépôt le gardien d'une règle de transition. Le dépôt reste un dépôt : il écrit ce qu'on lui donne, après validation intrinsèque. Si une tranche ultérieure veut une règle de transition — demander confirmation avant un changement de géométrie, refuser de changer d'univers quand des candidats existent — elle appartient à un **cas d'usage de l'Application**, qui tient les deux états, et jamais au dépôt. C'est la même frontière que « le rendu ne décide de rien ».
Deux choses restent vraies et ne doivent pas être confondues avec un verrou : `universe` n'a qu'une valeur en v1 (DEC-025), donc en pratique il ne bouge pas ; et le style demeure une propriété de **projet**, jamais surchargeable par gabarit — cette moitié de DEC-006 tient toujours.

**DEC-056 — Une donnée de projet n'est jamais rejetée par une donnée de machine.**
Choix : la validation d'un projet se scinde en deux classes. La validation **intrinsèque** porte sur ce que le fichier peut contredire tout seul : types, énumérations connues, nombres finis et strictement positifs, intégrité référentielle, forme des chemins. Elle est **bloquante** partout — chargement, sauvegarde, import. La validation **relationnelle** confronte le projet à l'environnement local : `tabWidthMm ≤ pawnWidthMm`, `paperFormat` connu de la calibration, fichier d'image présent sur le disque. Elle n'est bloquante **nulle part** dans T2 : elle produit un diagnostic, et devient une erreur au moment où quelqu'un demande une planche, là où le moteur de T1 la lève déjà (DEC-038, convention 5).
Supersède la clause de double validation de DEC-053.
Conséquence : le cahier T2 traitait trois fois le même cas et le tranchait deux fois dans un sens, une fois dans l'autre. Format de papier inconnu → diagnostic, avec l'argument écrit : « le fichier de calibration est une donnée de machine, le projet est une donnée d'utilisateur ». Fichier d'image manquant → diagnostic, avec l'argument écrit : « refuser d'ouvrir le projet le rendrait irréparable ». Surcharge d'onglet incompatible → rejet. Le troisième cas était le plus dommageable des trois, puisqu'il rendait **non ouvrable une archive parfaitement cohérente** venue de quelqu'un dont les socles ont une autre fente — c'est-à-dire exactement le scénario que DEC-050 existe pour rendre agréable.
Ce que la fiche préserve : une surcharge nulle, négative, non finie ou non numérique reste rejetée par `PROJECT_OVERRIDE_INVALID`. Elle est fausse en elle-même, sur toutes les machines, et aucune calibration ne la rend sensée.
L'argument qui a failli l'emporter dans l'autre sens, parce qu'il faut le nommer pour ne pas le reprendre dans six mois : « ne jamais écrire sur disque un projet qui ne peut pas être rendu ». C'est une bonne règle qui ne survit pas à sa propre généralisation — un `paperFormat` inconnu rend un projet tout aussi irrendable, et personne ne propose d'en bloquer la sauvegarde. Une règle qui ne s'applique qu'à un cas sur trois n'est pas une règle, c'est une préférence.
Bénéfice de bord, non négligeable en relecture : la vérité « l'onglet ne peut pas être plus large que le pion » n'est plus écrite qu'à **un seul endroit**, la construction du domaine, où DEC-038 l'a mise. Le lecteur de projet cesse d'en porter une copie, avec son message et son risque de divergence.
Portée au-delà de T2, et c'est ce qui justifie une fiche plutôt qu'une correction : la règle vaut pour tout ce qui viendra confronter un projet à son environnement — une clé d'`optionalParameters` absente du catalogue en T3, un template de workflow différent de celui qui a produit un candidat en T4. **Un projet s'ouvre pour être corrigé, pas pour planter.**

**DEC-057 — Un fichier de configuration se crée avec le composant qui le lit, jamais avant.**
Choix : en T2, la racine des projets et les six bornes de ressources de l'import sont un **record d'options** passé en paramètre au dépôt, dont les valeurs par défaut sont déclarées en un seul endroit nommé. Aucun troisième fichier de configuration n'est créé. Le fichier arrive en T6, avec l'hôte ASP.NET capable de le lire.
Conséquence : le critère d'acceptation de T2 disait « les bornes sont en configuration » sans dire dans quoi, et n'était donc pas vérifiable. `calibration.json` est le mauvais réceptacle — il est réservé aux valeurs physiques, et y ranger un plafond de ratio de compression brouillerait la seule distinction que ce fichier porte, celle que DEC-040 a mis une fiche entière à établir. Créer `limits.json` aujourd'hui reviendrait à écrire un fichier que rien ne lit, une liaison de configuration sans hôte, et un chemin de recherche de fichier à durcir : trois abstractions au cas où pour zéro consommateur.
Le critère devient testable : les bornes sont des **paramètres**, leurs valeurs par défaut sont déclarées en un seul endroit, et **aucune n'apparaît en littéral dans le code qui l'applique**.
Nuance à ne pas gommer : la racine des projets et les bornes ne sont pas de même nature. La racine est une décision de déploiement, déjà matérialisée par le volume `/app/data/projects` du §A.6 ; les bornes sont des limites de sécurité, qui relèvent du chapitre 9. Elles voyagent ensemble en T2 par commodité, et rien n'obligera à ce qu'elles partagent un fichier en T6.
La règle générale posée ici se reposera à l'identique en T3 (catalogue), T4 (template de workflow) et T5 (bornes de détourage) : elle est écrite une fois pour les trois.

**DEC-058 — Une tranche livrée vaut un mineur ; la version vit dans `Directory.Build.props`.**
Choix : `<Version>` est déclarée dans `Directory.Build.props` et incrémentée d'un mineur **au premier commit de chaque tranche**. Fondations 0.1.0, T1 0.2.0, **T2 0.3.0**, jusqu'à T7 en 0.8.0 ; `1.0.0` est atteint quand T7 est close et T0b menée. Le numéro est lu par réflexion sur l'assembly, jamais recopié dans une constante.
Conséquence : le §A.8 imposait le versionnement sémantique « à partir de 0.1.0 » sans dire quand incrémenter, et le dépôt n'a en réalité aucun numéro. `archive.json` promettait donc un `producedBy` sans source, ce qui est le genre de champ qui finit rempli par une chaîne littérale que personne ne pense à mettre à jour.
**Correction de la valeur proposée : T2 n'est pas 0.2.0, c'est 0.3.0.** Les Fondations sont livrées et T1 est écrite, testée et poussée (DEC-044) ; elles occupent 0.1.0 et 0.2.0. Le décalage n'est pas anecdotique : un binaire produit pendant T2 et estampillé 0.2.0 serait indiscernable de celui de T1, ce qui est précisément ce à quoi sert un numéro de version.
Sur l'incrémentation au **premier** commit d'une tranche plutôt qu'au dernier : c'est ce qui permet à toute archive produite pendant la tranche d'annoncer le chantier auquel elle appartient. Le numéro dit « ce binaire relève de T2 », pas « T2 est validée ». La validation est portée par les critères d'acceptation, et DEC-044 rappelle qu'une tranche peut être entièrement écrite sans être close.
Piège .NET à ne pas manquer, avec une conséquence de confidentialité : par défaut le SDK ajoute la révision de source à l'`AssemblyInformationalVersion`, qui sort sous la forme `0.3.0+3f9a1c…`. Ce suffixe partirait dans chaque `archive.json` et désignerait un commit — inoffensif sur un dépôt public, moins sur un dépôt privé dont la visibilité n'est toujours pas confirmée (question H de l'état du projet), et de toute façon du bruit dans un fichier destiné à être lu par un humain. Désactiver `IncludeSourceRevisionInInformationalVersion`.

**DEC-059 — Le CLI jetable prend des sous-commandes ; celle de T1 devient `sheet`.**
Choix : `tools/Pawnsmith.Cli` gagne quatre sous-commandes — `project new`, `project check`, `project export`, `project import` — et l'invocation de T1 devient explicite : `pawnsmith-cli sheet --manifest … --calibration … --out …`. La contrainte du §B.7 est reprise sans changement : **aucune logique, uniquement du câblage**, outil jetable, non livré, exclu de l'image Docker, sans tests. Le protocole T0 doit être mis à jour dans le même commit.
Conséquence : DEC-027 fait relire l'intégralité du code au porteur, et une tranche sans sortie observable se relit uniquement à travers ses tests. Cinquante-quatre tests ne montrent pas la même chose qu'un vrai `project.json` ouvert dans un éditeur, ni qu'une archive `Share` listée dans un explorateur de fichiers, où l'on **voit** ce qui a été retiré. C'est le service que le CLI de B.7 a rendu à T1, et il n'y a pas de raison de le refuser à la tranche dont le produit est précisément constitué de fichiers destinés à être lus par des humains.
`project check` mérite sa place à côté des trois autres : la moitié de T2 est faite de **refus**, et c'est le seul moyen de voir un message de rejet sans écrire un test.
Sur le renommage plutôt que la préservation de la forme de B.7, parce que c'était le choix par défaut et qu'il est écarté : garder l'ancienne invocation intacte tout en ajoutant des sous-commandes imposerait de les distinguer sur la **forme du premier argument** — un tiret ou pas. C'est une convention implicite, invisible en relecture, exactement ce que le §0 interdit, et elle coûterait plus cher à relire que la ligne de protocole qu'elle prétend épargner. Le renommage est possible aujourd'hui parce que T0b n'a pas encore été menée (DEC-044) : aucune séance de calibration n'a pris cette commande en main, aucune planche imprimée ne la mentionne. Le coût est une ligne de documentation, payée le jour où elle est encore gratuite.
Pourquoi cette fiche existe alors que le point relève de l'outillage : parce qu'elle modifie une procédure écrite dans un **autre document**, exécutée à la main, avec du papier et de l'encre en jeu. Une commande périmée dans un protocole de calibration ne se découvre pas à la compilation ; elle se découvre pendant la séance, quand la ramette est déjà ouverte.

**DEC-060 — Pas de filigrane invisible ; la licence reste MIT et l'attribution passe par le visible.**
Choix : aucune marque cachée n'est ajoutée aux planches — ni granulation stéganographique dans l'image, ni métadonnée, ni calque invisible. La licence **MIT est confirmée**. L'attribution repose sur deux choses seulement : **le nom**, que la licence ne cède pas, et une **mention visible** dans la zone de calibration de la planche, localisée comme le reste du texte imprimé.
Question posée : une application distribuée gratuitement en open source n'empêche concrètement personne d'en faire commerce ; un filigrane invisible — une granulation minuscule, vérifiable comme un code-barres — permettrait-il de reconnaître une planche issue de l'outil gratuit ?
Conséquence, et c'est la première chose à voir : **MIT autorise l'usage commercial, c'est sa raison d'être.** Le dispositif détecterait donc un usage que le projet permet explicitement. Avant de construire un détecteur, il fallait décider à quoi l'on s'oppose — être privé du crédit, ou être concurrencé. Seule la première réponse est retenue, et elle ne demande aucun filigrane.
L'argument interne qui tranche : le §A.2 **écarte QuestPDF** au motif que sa licence « source-available » exclut l'usage commercial et n'est pas approuvée OSI. Restreindre l'usage commercial de Pawnsmith reviendrait à adopter chez soi exactement le modèle qu'on refuse chez une dépendance. Ce n'est pas interdit, mais cela ne peut pas se faire sans rouvrir A.2 — et personne ne le demande.
Trois raisons techniques, cohérentes entre elles. **La sortie d'un outil n'est pas couverte par la licence de l'outil** : un PDF produit par Pawnsmith est le document de son utilisateur, et aucune licence sur le code ne le rend non commercial. **Le dispositif ne marquerait que les gens honnêtes** : le code est public, retirer la marque est un fork et vingt lignes, si bien que les seules planches marquées seraient celles produites par un binaire non modifié. **La réalité physique l'interdit de toute façon** : le livrable est un pion de carton de 25 mm, imprimé chez soi, découpé aux ciseaux. Tout ce qui est hors du contour part à la poubelle par conception, tout ce qui est dedans salit l'illustration qui est le critère de qualité du produit, et le tramage d'une imprimante domestique sur du 250 g/m² détruit toute micro-granulation — c'est le canal même dont T0b existe pour établir qu'on ne lui fait pas confiance à 2 % près.
Ce que la fiche évite au passage, et qui aurait été une régression : une marque portant quoi que ce soit d'unique par installation serait un **identifiant de traçage** dans une application sans compte ni authentification (§1.5), dont MEN-006 promet que les archives se partagent sans réflexion préalable. DEC-058 venait précisément de retirer la révision de source d'`archive.json` pour cette raison.
Ce que cette fiche ne ferme pas : si l'objectif devenait réellement d'interdire l'usage commercial, la réponse serait une **licence** qui le dit — au prix de cesser d'être open source au sens de l'OSI, et de contredire A.2. C'est une décision de modèle, pas une technique, et elle demanderait sa propre fiche.
La mention visible relève de **T1**, puisqu'elle touche le rendu, et se pose dans la zone de calibration qui porte déjà du texte localisé. Elle n'ajoute rien sur les pions eux-mêmes : cette zone est découpée avec le reste.

**DEC-061 — La mention visible n'est pas écrite non plus ; le sujet de l'attribution est clos.**
Choix : la mention « produit avec Pawnsmith » que DEC-060 plaçait dans la zone de calibration **n'est pas écrite**. Aucune marque, ni cachée ni visible, n'est ajoutée aux planches. Supersède la seule clause de DEC-060 qui engageait du code ; tout le reste de cette fiche demeure, licence MIT comprise.
Conséquence : il ne reste **aucune tâche T1 ouverte** au titre de l'attribution, et le §8 des instructions du projet cesse d'en annoncer une. L'attribution repose sur le nom et sur le dépôt public, ce qui est ce que la licence garantit déjà sans que le rendu ait à s'en mêler.
Sur la brièveté de cette fiche, qui contraste avec DEC-060 : l'analyse est faite et elle tient, elle n'est simplement pas suivie sur son dernier point. Le porteur a tranché que la planche n'a pas à porter de mention du tout. Ce qui aurait été coûteux, c'est de laisser la bible promettre un texte que personne n'écrira.

**DEC-062 — Le dépôt de projet est sans état ; chaque opération reçoit son chemin.**
Choix : les cinq méthodes d'`IProjectRepository` prennent le dossier en paramètre — ou, pour l'import, le **nom** dont le dossier est dérivé. Le dépôt ne retient aucun projet « courant » et ne lit jamais un emplacement sur un projet. Supersède la signature `SaveAsync(Project, CancellationToken)` esquissée au chapitre 7, ainsi que celles de ses trois voisines.
Conséquence : la signature du chapitre 7 supposait qu'un projet sache où il habite. **DEC-047 l'interdit** : le nom du dossier n'a aucune sémantique, un dossier ordinaire se renomme, se duplique et se restaure ailleurs sous un autre nom. Un projet qui porterait son emplacement ferait du renommage d'un dossier un changement de projet — exactement ce que `projectId` existe pour empêcher. La contradiction était donc interne au chapitre 7, et elle se tranche du côté de la fiche.
Un dépôt sans état a un second effet, qui n'était pas le motif mais qui compte : **il n'y a rien à invalider.** Un dépôt qui aurait mémorisé le projet courant aurait dû décider quoi en faire à l'import, à l'export, et quand un dossier disparaît sous ses pieds — trois questions que personne n'a posées et dont les réponses se seraient écrites en silence.
Ce que la fiche ne fait pas : elle ne dit rien de la concurrence. Deux écritures simultanées du même dossier restent non gérées, comme le §C.7.3 l'écrit, et le dernier écrivain gagne. L'absence d'état n'est pas un verrou et ne prétend pas en tenir lieu.
Portée : la décision avait été prise pendant l'écriture de la tâche 8 de T2 et laissée hors des fiches, faute de port à qui l'appliquer. Elle est consignée au moment où le port est assemblé — le dernier moment où elle pouvait l'être sans être reconstituée d'après le code.

**DEC-063 — Le catalogue est global, en fichier de données, un fichier par univers.**
Choix : le catalogue des paramètres optionnels — arme, armure, vêtement… — est une donnée de l'**application**, livrée en `config/catalog.{univers}.json`, éditable par l'utilisateur, et jamais embarquée dans un projet. **Ferme la question D du chapitre 16.**
Conséquence : le critère est celui de DEC-052 — *qui détermine la valeur*. Le vocabulaire d'équipement est déterminé par l'univers, pas par le projet, et Pawnsmith est mono-utilisateur (§1.5) : une propriété de l'univers est ici une propriété globale. L'embarquer par projet obligerait la même personne à ressaisir « hache d'armes » partout, et produirait des projets divergeant sur un vocabulaire qui n'avait aucune raison de varier. C'est le régime que DEC-010 donne déjà aux templates de prompts, parce que les deux ont la même nature.
Le contre-argument — un projet `Share` arrive chez quelqu'un dont le catalogue diffère — ne tient pas, et c'est DEC-056 qui le dit : le catalogue est une donnée de machine, il ne rejette jamais un projet. La clause sujet voyage **stockée** sur le gabarit, et le destinataire lit le texte de l'expéditeur même si son propre catalogue est vide. Une valeur inconnue produit un fragment de repli et un diagnostic, jamais une erreur. Voir §D.4 du cahier T3.

**DEC-064 — Une entrée de catalogue porte un fragment de phrase, pas un mot.**
Choix : chaque entrée associe à un couple (clé, valeur) un `fragment` — un groupe de mots anglais complet, inséré tel quel dans la clause sujet. `weapon: axe` ne donne pas « axe » mais « wielding a large battle axe held vertically against the body ».
Conséquence : la décision vient d'une mesure. DEC-043 a relevé, sur le premier sujet de T0a, une **adhérence imparfaite à l'équipement** — `a large battle axe` demandée, deux dagues obtenues. T3 ne peut pas rendre un modèle obéissant ; il peut cesser de lui donner un mot isolé. Le prompt de référence de DEC-043 ne dit pas `spear` mais *une lance courte tenue verticalement contre le corps*, et la fiche mesure que cette contrainte de pose « a tenu sur les trois sujets ». Deux bénéfices pour un seul champ : une périphrase pèse plus qu'un mot dans un encodeur de texte, et **la contrainte de pose compacte de DEC-042 se trouve portée par chaque objet qui pourrait élargir la silhouette**, au lieu d'être une phrase générale parlant d'objets que le modèle ne sait pas encore qu'il va dessiner.
Ce que cela coûte : un catalogue plus long à écrire, une phrase par entrée. C'est du contenu, pas du code, et c'est le fichier que DEC-010 rend éditable pour qu'il s'améliore à l'usage. L'alternative — dériver le fragment du mot par un patron `wielding a {value}` — recrée une convention implicite et ne saurait produire ni `wearing`, ni la contrainte de pose.

**DEC-065 — Catalogue et template de prompt sont deux fichiers distincts.**
Choix : un univers a deux fichiers, `catalog.{univers}.json` et `prompt-template.{univers}.json`, et non un seul.
Conséquence : ils ont la même portée, et la tentation de les réunir est réelle. Ce qui les sépare est l'argument de DEC-029 appliqué un cran plus bas — **ils n'ont pas le même rayon d'explosion**. Le template porte la *structure* de la phrase ; une faute y produit une clause malformée pour tous les gabarits. Le catalogue porte le *vocabulaire* ; une faute y touche une valeur. L'un s'édite rarement et par quelqu'un d'averti, l'autre souvent et par l'utilisateur ordinaire. Les réunir mettrait la structure de phrase à portée de main de qui venait ajouter « hallebarde ».

**DEC-066 — `IPromptComposer` se réduit à `ComposeSubject` ; l'assemblage n'est pas un port.**
Choix : le port ne porte plus qu'une méthode, `ComposeSubject(Blueprint, Universe)`, qui rend la clause **et** ses diagnostics. La méthode `Assemble` du chapitre 7 est supprimée. **Supersède la signature d'`IPromptComposer` esquissée au chapitre 7**, comme DEC-062 l'a fait pour `IProjectRepository`.
Conséquence : T2 a écrit l'assemblage en fonction pure de domaine, `ResolvedPrompt.From`, dont le §C.5.5 fait une surface de compatibilité sous fiche. Le motif de la réduction n'est pas d'éviter une duplication — c'est ce qu'est un port : **une interface existe pour qu'on puisse en substituer l'implémentation.** Or EVO-001, seule seconde implémentation prévue, « ne réécrit que la clause sujet ; les clauses style et cadrage lui restent inaccessibles ». L'assemblage n'aura donc jamais de seconde implémentation, et le mettre derrière une interface promettrait une substitution que la conception interdit.
Le verrouillage de DEC-028 en sort **renforcé** : aucune méthode du port ne reçoit ni ne rend une clause de style ou de cadrage. La garantie est portée par une interface qui ne les mentionne pas du tout.
Sur le type de retour, un record plutôt qu'une chaîne : une valeur inconnue du catalogue doit produire un diagnostic à côté de la clause (DEC-063). Une méthode rendant `string` n'aurait nulle part où le mettre, et devrait soit le taire, soit lever — les deux étant ce que DEC-056 interdit.

**DEC-067 — La clause sujet se recompose tant qu'elle n'a pas été éditée, et l'édition se déduit au lieu d'être stockée.**
Choix : quand un champ de composition d'un gabarit change — race, classe, paramètre optionnel, détails — la clause stockée est comparée à celle que le composeur produirait **pour les anciens champs**. Identiques, personne n'y a touché : on recompose avec les nouveaux. Différentes, l'utilisateur l'a éditée : on n'y touche pas. Aucun champ n'est ajouté au gabarit ; `versionSchema` de `project.json` reste à 1. La règle vit dans un cas d'usage d'Application, jamais dans le dépôt (DEC-055).
Conséquence : le §3.1 laissait un trou. Il dit que la clause est « produite par le composeur » — donc dérivée — et « stockée et éditable », ne se régénérant pas « après édition » — donc figée. Le gabarit **jamais édité** dont on change un champ n'était écrit nulle part ; sans règle, il affiche `orc` et demande un gobelin au modèle. Le parcours utilisateur du §D.3 l'a fait surgir.
Trois propriétés font préférer la déduction à un booléen stocké. **Aucun champ ajouté**, alors que DEC-048 pose qu'il n'existe pas d'ajout compatible : un `subjectClauseEdited` ferait passer le schéma en version 2, sur un schéma que T2 vient de figer. **C'est le réflexe du projet** : `promptResolu` et `desaligne` sont calculés et jamais persistés, parce qu'une valeur calculée qu'on persiste ment dès la première modification manquée — un drapeau d'édition ment dès qu'on restaure un `project.json` à la main. **Le repli est du bon côté** : si le template ou le catalogue changent, la comparaison échoue et la clause est traitée comme éditée. Se tromper coûte une recomposition manquée, jamais un texte d'utilisateur écrasé.
La comparaison est ordinale, sur les deux chaînes normalisées — même règle que le désalignement, pour le même motif.

**DEC-068 — L'élection et le statut sont deux axes ; l'ancien élu ne change pas de statut.**
Choix : élire un candidat fait pointer `electedCandidateId` ailleurs, et rien d'autre. Le statut de l'ancien élu n'est pas modifié. **Ferme la première moitié de la question B du chapitre 16.**
Conséquence : fusionner l'élection et le jugement est le piège que le §3.1 nomme déjà pour le désalignement — cela rendrait impossible de distinguer « rejeté par l'utilisateur » de « simplement pas retenu cette fois ». Un candidat `Valid` qu'on cesse d'élire **reste `Valid`** : l'utilisateur l'a jugé bon, et en préférer un autre ne le rend pas mauvais. Le repasser à `Draft` effacerait un jugement qu'il avait porté, sans qu'il l'ait demandé.

**DEC-069 — Un gabarit sans candidat élu est ignoré à la mise en page, et signalé ; jamais de page vide.**
Choix : un gabarit sans élu ne produit aucune cellule, et un groupe de taille dont aucun gabarit n'a d'élu ne produit aucune page. La mise en page rend un **diagnostic** nommant les gabarits sautés. L'export n'est jamais bloqué pour ce motif.
Conséquence : le parcours utilisateur du §D.3 montre qu'une planche partielle est un usage normal — on élit six gobelins et on tire la page pendant que l'ogre se génère. Bloquer casserait ce parcours. Mais ignorer **silencieusement** ferait disparaître un gabarit déclaré avec une quantité de six sans que rien ne le dise ; l'utilisateur le découvrirait feuille en main. Le diagnostic est le motif que DEC-056 a installé pour `paperFormat` et pour la surcharge d'onglet : on n'empêche rien, on dit ce qu'on a fait. Une page vide n'est pas un signalement, c'est du papier perdu.
Deux sous-questions de C se ferment ici, parce qu'elles étaient la même règle vue des deux bouts. Une troisième — « quantité dépassant la capacité de page » — est **sans objet** : `Pagination.Plan` de T1 pagine déjà, et seule une capacité nulle est une erreur, déjà levée en nommant la taille. Le chapitre 16 a été écrit avant que T1 n'existe.

**DEC-070 — Supprimer un gabarit emporte ses candidats et leurs fichiers image.**
Choix : la suppression d'un gabarit supprime ses candidats et les PNG que ceux-ci référencent, qu'un candidat soit élu ou non. Aucun refus, aucune étape préalable de dé-élection. Le disque est touché **après** le modèle, et seuls les fichiers que le gabarit supprimé référençait sont retirés — jamais un balayage d'`images/`.
Conséquence : un fichier que plus rien ne référence n'est pas une sauvegarde. Il est invisible depuis l'application, inconsultable autrement qu'au gestionnaire de fichiers, et il alourdit chaque archive `Backup` pour toujours. Conserver les PNG donnerait l'illusion d'un filet sans en être un. Ce qui rend la décision tenable : **un projet est un dossier ordinaire** (DEC-011), et le profil `Backup` existe précisément pour ça — le filet est là, il est explicite, et il ne dépend pas de fichiers orphelins.
Refuser tant qu'un candidat est élu aurait protégé d'un geste malheureux, au prix d'une étape qui n'explique pas pourquoi elle existe, et que l'utilisateur décidé exécute machinalement.

**DEC-071 — Le statut d'un candidat n'exige aucun fichier ; l'élection exige les deux détourages.**
Choix : un candidat peut porter n'importe quel statut sans posséder ses détourages. Seul un candidat possédant `frontImageFile` **et** `backImageFile` peut être élu ; sinon, `CANDIDATE_NOT_CUT_OUT`.
Conséquence : le statut est le **jugement** de l'utilisateur, et il juge sur l'image jumelée, bien avant qu'un détourage n'existe (§D.3). Lui interdire de valider ce qu'il a sous les yeux serait absurde et créerait une dépendance de T3 vers T5. L'élection n'est pas un jugement : c'est la désignation de ce que la planche va consommer, et la planche consomme deux PNG détourés. La contrainte se pose là, une seule fois, à l'endroit où elle sert. Sans elle, le défaut ressortirait à la mise en page sous la forme d'un fichier introuvable — une erreur technique là où il fallait une règle métier, et à l'étape 5 pour une faute commise à l'étape 4.
Ferme la sixième sous-question de C, que le code de T2 renvoyait explicitement à cette tranche.

**DEC-072 — La machine à états du `Job` descend en T4 ; la question B est scindée.**
Choix : les états d'un travail de génération — en file, en cours, échoué, annulé — ne sont pas tranchés en T3. La question B du chapitre 16 est scindée : sa première moitié est fermée par DEC-068, sa seconde change d'échéance et passe **avant T4**.
Conséquence : le `Job` est l'objet de la génération, et T3 ne génère rien. En fixer les états sans le client ComfyUI reviendrait à concevoir pour une tranche à venir, ce que le §0 de T1 interdit. Les états dépendent de ce que le générateur sait réellement rendre — une file interrogeable, un identifiant de tâche, une annulation qui aboutit ou non — et rien de tout cela n'est connu avant T4. La première moitié, elle, touchait le modèle que T2 a écrit, et était bien à sa place.

**DEC-073 — La question F est scindée : le schéma des fichiers de templates est T3, le workflow ComfyUI reste T4.**
Choix : le fichier de templates de prompts par univers — `prompt-template.{univers}.json`, son schéma et ses jetons — est spécifié et lu en **T3**. Le template de workflow ComfyUI et ses jetons restent en T4. La ligne F du chapitre 16 est corrigée en conséquence.
Conséquence : le chapitre 16 groupait les deux sous une même échéance, T4. Ils ne l'ont pas : le template de workflow porte la clause de cadrage que seul T4 sait lire (DEC-029, §C.5.2), tandis que le fichier de templates de prompts est **ce que le composeur de T3 lit pour exister** — sans lui, `ComposeSubject` n'a pas de patron de phrase. Les jetons de ce fichier forment une liste close, énumérée dans le code, et un jeton inconnu fait rejeter le fichier en le nommant : écrire `{taille}` ne doit pas produire une clause contenant littéralement « {taille} », qui partirait au modèle sans que rien ne le signale.

**DEC-074 — Machine à états du `Job` : cinq états, un échec arrête le lot, et rien ne le persiste.**
Choix : un `Job` passe de `Queued` à `Running`, puis à l'un des trois états terminaux `Completed`, `Failed` ou `Cancelled` ; il peut aussi passer directement de `Queued` à `Cancelled`. Aucune autre transition n'existe, et aucune ne sort d'un état terminal. `Failed` porte un code d'erreur et un message ; pendant `Running`, le `Job` compte les candidats qu'il a produits. Le premier échec arrête le lot. Le `Job` vit en mémoire et n'est jamais écrit sur disque. **Ferme la question B du chapitre 16.**
Conséquence : DEC-072 avait renvoyé la question à T4 parce que les états dépendaient de ce que le générateur sait rendre ; ComfyUI rend un identifiant de tâche, une file interrogeable et une annulation qui aboutit, ce qui suffit à cinq états. Aucun état « partiellement réussi » : le couple (état, nombre produit) dit déjà tout, et un sixième état doublerait chaque `switch` d'interface sans rien apprendre de neuf.
L'arrêt au premier échec vient de la nature des échecs d'un générateur local — ComfyUI arrêté, modèle absent, mémoire graphique épuisée, workflow refusé. Ils sont systémiques : continuer reproduirait la même erreur à chaque graine, et une erreur par délai d'attente coûte dix minutes par graine.
Ne rien persister n'est pas une économie, c'est l'absence de besoin : chaque candidat est sauvegardé dès qu'il existe (DEC-075), donc après un redémarrage le projet contient exactement ce qui a été produit, et le `Job` ne disait rien de plus. Le persister coûterait un `versionSchema` (DEC-048) pour une donnée qui n'a de sens que pendant que le processus tourne, et il n'y aurait rien à reprendre : un `prompt_id` relu après redémarrage désigne une tâche dont l'état est inconnu. Reprendre un lot, c'est le relancer.

**DEC-075 — Un lot fige un prompt pour N graines, et chaque candidat est sauvegardé dès qu'il existe.**
Choix : un lot fige ses trois clauses une fois, au démarrage, et toutes ses graines partent avec le même prompt. Les graines sont choisies par l'appelant. Pour chaque graine, dans cet ordre : générer, **relire** le projet, écrire l'image jumelée, ajouter le candidat en `Draft`, sauvegarder. Une fois l'image reçue, son écriture et la sauvegarde ne sont plus annulables ; l'annulation est observée avant la graine suivante. Si le gabarit a disparu à la relecture, le lot finit `Failed` avec `BLUEPRINT_NOT_FOUND` et l'image n'est pas écrite.
Conséquence : c'est ce qui tient le critère « un lot interrompu conserve les candidats déjà produits », par échec comme par annulation. Trois choix l'accompagnent. **Relire à chaque graine** : un lot dure jusqu'à une heure, et un projet gardé en mémoire tout ce temps écraserait à chaque sauvegarde ce que l'utilisateur a modifié entre-temps ; relire ramène la fenêtre de concurrence à quelques millisecondes. Ce n'est pas un verrou — DEC-062 le rappelle —, et l'API de T6 sérialisera les écritures d'un même projet. **L'image avant le candidat** : l'ordre inverse laisserait, au moins un instant, un `project.json` pointant vers un fichier absent ; l'ordre retenu laisse au pire un orphelin, que DEC-070 a jugé inoffensif. **Un seul prompt par lot** : un candidat produit après une édition de la clause sujet fige l'ancienne et naît désaligné, ce qui est exactement vrai ; relire la clause à chaque graine donnerait un lot dont les candidats ne répondent pas à la même question.
Un lot vide, plus grand que la borne de MEN-007, ou visant un gabarit inconnu est refusé **avant** qu'aucun `Job` n'existe : une requête mal formée n'est pas un travail qui échoue, c'est un travail qui n'a jamais existé.

**DEC-076 — Le template de workflow : trois jetons en liste close, un jeton est une valeur entière, et la substitution se fait sur le graphe.**
Choix : `config/workflow.comfyui.json` porte `versionSchema`, la clause de cadrage en **tableau de lignes** jointes par `\n`, l'identifiant du nœud de sortie, et le graphe au **format API** de ComfyUI. Trois jetons seulement : `{{POSITIVE}}` et `{{SEED}}` exactement une fois, `{{NEGATIVE}}` au plus une fois. Un jeton occupe une valeur de chaîne **entière** ; un jeton noyé dans un texte, ou tout autre `{{…}}`, fait rejeter le fichier en le nommant. Les positions des jetons sont relevées au chargement, et la substitution remplace ces nœuds dans une copie du graphe, sans jamais parcourir ce qu'elle insère. **Ferme la question F du chapitre 16** et corrige la liste de jetons du §6.4.
Conséquence : `{{WIDTH}}` et `{{HEIGHT}}` disparaissent. Les dimensions sont un réglage du workflow que l'utilisateur écrit dans son graphe, et Pawnsmith n'a aucune raison de les imposer : la découpe et le détourage lisent les dimensions de l'image **reçue**. Un jeton de dimension obligerait à porter la valeur à deux endroits, soit la divergence qu'un fichier unique évite.
La règle « valeur entière » est ce qui rend DEC-049 tenable. `"{{POSITIVE}}, masterpiece"` ferait envoyer autre chose que le prompt résolu ; l'interdire à la lecture rend la transformation impossible plutôt que déconseillée. La substitution sur le graphe, elle, est la leçon du correctif de T3 sur `TemplateToken.Substitute` appliquée d'emblée : un `string.Replace` sur le texte JSON casserait le document au premier guillemet d'un prompt, et re-balaierait une valeur insérée — un prompt contenant littéralement `{{SEED}}` recevrait la graine.
Le dépôt livre un **exemple**, `config/workflow.comfyui.example.json`, construit d'après DEC-043 et jamais soumis à un ComfyUI réel ; l'application ne retombe jamais dessus. Point à vérifier au premier lot réel : l'assemblage fixé par le §C.5.3 met le sujet **après** tout le cadrage, alors que le prompt de référence de T0a le plaçait au milieu.

**DEC-077 — Ce qui part au générateur est exactement le prompt résolu ; la clause négative et le graphe ne sont pas figés.**
Choix : `{{POSITIVE}}` reçoit `ResolvedPrompt.From` des trois clauses que le candidat fige, sans substitution, troncature ni réécriture. `{{NEGATIVE}}` reçoit la clause négative du style, normalisée, et elle n'est **pas** figée sur le candidat. Le reste du graphe ne l'est pas non plus.
Conséquence : la contrainte que DEC-049 imposait à T4 est tenue à la lettre, et vérifiée sur ce que le faux serveur a reçu. L'échappement JSON du transport n'est pas une transformation au sens de cette fiche : c'est l'encodage, et ComfyUI décode octet pour octet le texte que le candidat fige.
La clause négative n'est pas une des trois clauses de DEC-028 et n'entre pas dans le désalignement. La figer ajouterait un quatrième champ au candidat, donc un `versionSchema` (DEC-048), pour une valeur que le modèle en usage ignore : à CFG 1,0, le guidage négatif est neutralisé (DEC-043). La décision se rouvrira si un modèle la rend signifiante, avec une mesure sous les yeux. Changer le nombre d'étapes ou le modèle dans le graphe ne désaligne rien non plus : DEC-049 fige des clauses de prompt, pas un environnement d'exécution. Seule la clause de cadrage, parce qu'elle vit dans ce fichier **et** entre dans le prompt, désaligne quand on la touche.

**DEC-078 — `IImageGenerator` est le seul port de la génération ; `IPawnPairProducer` n'est pas écrit.**
Choix : la production du couple est un cas d'usage de l'Application appuyé sur `IImageGenerator`. Le port rend l'image et ses dimensions lues sur l'en-tête ; `CheckAsync` rend une disponibilité — `Available`, `Unreachable`, `Unhealthy` — et ne lève jamais pour un générateur absent. Supersède `IPawnPairProducer` au chapitre 7, et la phrase du §4.2 qui y isolait la génération jumelée ; supersède sur ce seul point la conséquence de DEC-003, dont le choix demeure.
Conséquence : `IPawnPairProducer` couvrait un risque précis — que le modèle ne sache pas produire une planche de rotation, ce qui aurait demandé une implémentation dégradée à deux générations. DEC-043 a montré le contraire. L'écrire aujourd'hui donnerait une interface à implémentation unique, pour toujours ou jusqu'à EVO-009 ; et EVO-009, en passant par la 3D, ne produirait pas d'image jumelée, si bien que la signature dessinée aujourd'hui serait fausse ce jour-là, et le schéma du candidat avec elle. Le point de substitution qui a un usage prévu est le fournisseur (EVO-002), et c'est `IImageGenerator`.

**DEC-079 — La découpe est décidée en T4 et exécutée en T5.**
Choix : la règle est une fonction pure de domaine. La vue de face est la moitié **gauche**, la vue de dos la moitié **droite** ; le partage est vertical, au milieu exact ; une largeur impaire perd sa colonne du milieu, et les deux moitiés ont toujours la même largeur ; une image de moins de deux pixels de large n'est pas découpable. T4 applique la règle à la réception pour refuser une image non découpable ; T5 l'appelle pour découper les pixels, juste avant le détourage.
Conséquence : le chapitre 12 range « découpe » dans T4, et c'est la règle qui y est. Les pixels ne sont pas découpés ici, pour quatre raisons. Le schéma n'a pas de place pour des moitiés non détourées : les écrire dans `frontImageFile` et `backImageFile` les rendrait élisibles (DEC-071) et une planche imprimerait deux rectangles de fond ; leur donner deux champs coûterait un `versionSchema` pour des fichiers sans lecteur. Le seul consommateur des moitiés est le détourage, qui décode les pixels de toute façon. Découper suppose de décoder un PNG, donc une bibliothèque d'image — un choix de licence que le porteur s'est réservé avec T5 — ou un décodeur écrit à la main et probablement remplacé. Et la règle, elle, n'attend rien.
La face à gauche n'est pas détectée, elle est **supposée**, parce que c'est la clause de cadrage qui la garantit ; une détection qui se tromperait inverserait recto et verso sans rien signaler. La colonne perdue plutôt que donnée à l'une des moitiés suit DEC-041 : le couple partage une échelle unique, et deux sources de largeurs différentes y entreraient avec un biais qu'aucune image ne justifie.
À transmettre à T5 : ComfyUI inscrit le graphe et le prompt dans les métadonnées de chaque PNG. L'image jumelée les porte, sans conséquence puisque le profil `Share` la retire ; les moitiés détourées partent dans une archive `Share` et doivent donc être **réencodées**, jamais produites par copie des blocs de l'image source.

**DEC-080 — Les bornes de la génération sont arbitrées, en records d'options ; la question G est scindée.**
Choix : huit bornes, déclarées une fois chacune et jamais en littéral dans le code qui les applique. Dans `GenerationOptions` (Application) : au plus **20** candidats par lot. Dans `ComfyUiOptions` (Infrastructure) : **10 min** par génération, **30 s** par appel HTTP, **5 s** pour l'état de santé, une interrogation par **seconde**, **64 Mio** par image, **8192** pixels de côté, **16 Mio** par réponse JSON. La moitié génération de la question G du chapitre 16 est fermée ; la moitié détourage reste à T5.
Conséquence : ce sont des bornes de sécurité et de confort, qui **s'arbitrent** (DEC-057) — la règle des valeurs physiques ne s'y applique pas. Les deux qui demandent un motif : dix minutes par génération alors qu'une génération en prend quarante secondes, parce que le premier lot après le démarrage de ComfyUI charge un modèle de 12 milliards de paramètres ; vingt candidats par lot, parce que c'est un quart d'heure de carte graphique, ce qu'on lance en connaissance de cause, alors qu'un clic malheureux sur cent occuperait la soirée. Les deux records sont distincts parce que le plafond du lot est une règle de l'application, et les délais une propriété de l'adaptateur ComfyUI : un fournisseur distant (EVO-002) aurait d'autres délais et le même plafond.

**DEC-081 — L'adresse du générateur est un réglage de déploiement ; MEN-003 se traite à la racine.**
Choix : l'adresse se règle par configuration ou variable d'environnement, **jamais par l'API**. Elle doit être une URI absolue en `http` ou `https`, sans identifiants, sans requête ni fragment, sinon `GENERATOR_URL_INVALID` au démarrage. Le client HTTP ne suit **aucune redirection** et n'utilise **aucun proxy**. La liste blanche de ports que prescrivait MEN-003 est écartée. La contre-mesure de MEN-003 au chapitre 9 est corrigée.
Conséquence : une SSRF suppose qu'un attaquant choisisse l'adresse que le serveur appelle. Une adresse que seul l'opérateur écrit, dans un fichier de son poste, n'est choisie par personne d'autre ; c'est retirer le vecteur plutôt que le filtrer. Filtrer les ports que l'opérateur a lui-même écrits ne protège de rien, et casserait le jour où ComfyUI tourne ailleurs que sur 8188. Les deux règles du client sont ce qui rend la validation de l'adresse effective : une redirection suivie laisserait le serveur validé désigner lui-même la cible suivante, et un proxy enverrait les prompts à un tiers alors que DEC-007 a choisi un générateur local. L'interdiction des identifiants dans l'URI suit MEN-006 : un secret dans une adresse finit dans un journal.
Ce que la fiche engage pour T6 : l'interface pourra **afficher** l'adresse et l'état du générateur, jamais la modifier.

**DEC-082 — L'export d'un candidat élu mais désaligné passe outre, et le signale clause par clause.**
Choix : la planche est produite avec l'élu tel qu'il est. Le rapport de planche liste chaque élu désaligné et les clauses qui ont bougé ; il dit aussi quand le désalignement est **inconnu**, faute de workflow configuré. Aucun blocage, aucune confirmation. **Ferme la question C du chapitre 16**, dont c'était la dernière sous-question.
Conséquence : bloquer forcerait à régénérer un pion pour pouvoir l'imprimer, alors que l'image n'est pas fausse — elle a été produite sous un autre prompt, ce qui est une information et non un défaut ; l'utilisateur qui change de style pour ses prochains pions peut vouloir garder les anciens. Avertir par une confirmation au moment de l'export est le mécanisme de consentement que DEC-030 a écarté. Passer outre en le disant est le motif de DEC-056 et DEC-069 : on n'empêche rien, on dit ce qu'on a fait, là où l'information sert — à côté de l'aperçu, avant l'impression. Le cas « inconnu » est signalé parce que se taire laisserait croire à un alignement.

**DEC-083 — Un projet s'adresse par le nom de son dossier, sous sa forme canonique ; jamais par son `projectId`.**
Choix : les routes de l'API désignent un projet par `/api/projects/{folder}`. Le nom n'est accepté que s'il est déjà canonique — `ProjectFolderName.From(folder)` lui est égal, en ordinal — et si le chemin résolu est un enfant direct de la racine des projets. Tout écart rend `PROJECT_NOT_FOUND`, sans distinguer un nom mal formé d'un dossier absent. La liste des projets rend aussi ceux qui ne se chargent pas, avec leur code.
Conséquence : le `projectId` n'est pas unique, par décision (DEC-047) — deux imports de la même archive le partagent —, et une adresse doit désigner une seule chose ; le nom de dossier l'est, par le système de fichiers. Le nom venant d'une URL, il est traité comme MEN-002 et MEN-009 l'exigent : la forme canonique est une liste blanche, la vérification de préfixe une seconde barrière. Un projet cassé reste dans la liste parce qu'il s'ouvre pour être corrigé (DEC-056). Le doublon de `projectId` n'est pas arbitré par l'API : elle l'expose, et la question posée à l'utilisateur appartient au front.

**DEC-084 — Une erreur d'API rend un code, et rien d'autre.**
Choix : toute erreur rend un statut HTTP et le corps `{ "code": "…" }`. Le message n'est jamais renvoyé ; il ira aux journaux en T7. Le statut se déduit du code par une table unique ; un code absent de la table rend `500`, et une exception sans code rend `INTERNAL_ERROR`. Neuf codes naissent dans l'API (`REQUEST_INVALID`, `CROSS_ORIGIN_REFUSED`, `JOB_NOT_FOUND`, `JOB_ALREADY_FINISHED`, `IMAGE_NOT_FOUND`, `UNIVERSE_NOT_FOUND`, `UPLOAD_TOO_LARGE`, `GENERATOR_NOT_CONFIGURED`, `INTERNAL_ERROR`) et trois dans l'Application pour la planche (`PAPER_FORMAT_UNKNOWN`, `SHEET_CAPACITY_EXCEEDED`, `SHEET_INPUT_INVALID`).
Conséquence : le chapitre 10 voulait des codes et non des messages traduits ; un message anglais n'est pas traduit, mais un texte affiché tel quel est ce qu'il interdit. Le second motif est nouveau et suffirait seul : **les messages contiennent des chemins absolus**, qui disent l'arborescence du serveur à quiconque lit la réponse. Le chapitre 8 tient les journaux hors des archives pour cette raison ; un corps de réponse est un canal de plus. Contrepartie assumée : `PROJECT_INVALID` nommait le champ fautif dans son message (C.11) ; l'API ne le transmet pas. Si le front en a besoin, ce sera un champ structuré, pas une phrase.

**DEC-085 — Les lots sont validés avant la file, un seul tourne à la fois, et le registre est borné.**
Choix : une requête de lot est validée tout de suite — taille, gabarit, générateur configuré — et mise en file ; un seul lot s'exécute à la fois, dans l'ordre d'arrivée. `CandidateGeneration` est scindé en `QueueAsync`, qui valide et rend un job `Queued`, et `RunAsync`, qui le mène à son terme ; l'enchaînement des deux reste disponible. Les clauses sont figées au démarrage du lot, pas à la mise en file. Le registre garde les cent derniers jobs terminés. Une annulation retire un job en file sans qu'il tourne, et annule un job en cours.
Conséquence : un générateur local a une carte graphique ; deux lots simultanés se partageraient la file de ComfyUI sans aller plus vite, et l'ordre de leurs candidats deviendrait illisible. Valider avant la file garde la règle de §E.4.4 — une requête mal formée est un job qui n'a jamais existé — quand le job ne démarre plus aussitôt. Figer au démarrage plutôt qu'à la mise en file respecte l'intention de l'utilisateur qui corrige sa clause pendant que son lot attend. Le registre est borné parce qu'il vit en mémoire (DEC-074) et que rien ne justifie qu'il grossisse sans fin.

**DEC-086 — Les écritures d'un même projet passent par une porte par dossier, dans le processus.**
Choix : toute séquence « charger, modifier, sauvegarder » sur un projet franchit `ProjectWriteGate`, un verrou par dossier tenu par l'Application. Les points de terminaison qui écrivent la franchissent ; le lot la franchit pour chaque candidat. Les lectures ne la franchissent pas.
Conséquence : DEC-075 avait réduit la fenêtre de concurrence d'un lot à quelques millisecondes en relisant le projet à chaque graine, en annonçant que ce n'était pas un verrou. L'API crée le second écrivain réel — l'utilisateur qui modifie un gabarit pendant qu'un lot ajoute un candidat —, et la fenêtre devient un risque de perte. Les lectures s'en passent parce que la sauvegarde remplace le fichier d'un bloc (§C.7.3). La porte est dans le processus : deux instances sur la même racine ne se voient pas, ce qui n'est pas un usage prévu d'une application mono-utilisateur (§1.5). DEC-062 reste vrai pour le dépôt, qui n'a toujours aucun état : la porte n'est pas dans le dépôt.

**DEC-087 — La configuration passe par `appsettings.json` et l'environnement ; un générateur mal configuré n'empêche pas de démarrer.**
Choix : l'hôte lit sa configuration par le mécanisme standard d'ASP.NET — `appsettings.json`, surchargé par les variables `Pawnsmith__…`. Racine des projets, dossier de configuration, adresse du générateur, fichier de workflow, taille maximale d'une archive importée. Une calibration, un catalogue ou un template invalides font échouer le démarrage. Une adresse de générateur vide, un workflow absent ou invalide laissent démarrer : le générateur est alors non configuré ou mal configuré, avec son code, et seules les routes de génération le refusent.
Conséquence : c'est l'arrivée annoncée par DEC-057 — le fichier se crée avec le composant qui le lit. Le mécanisme standard est préféré à un fichier propre parce que tout opérateur Docker sait déjà passer une variable d'environnement, et qu'il ne demande aucun code de lecture. Les bornes des records d'options de T2 et T4 gardent leurs défauts et ne sont pas exposées : aucune demande ne le justifie, et en exposer une plus tard est une ligne. La distinction entre ce qui bloque et ce qui ne bloque pas est DEC-056 appliqué à la machine : sans calibration rien ne marche, sans générateur on travaille encore ses projets.

**DEC-088 — Une image n'est servie que si un candidat du projet la référence.**
Choix : la route d'image sert `images/{fichier}` seulement si ce chemin est référencé par un candidat du projet, s'il passe les règles de C.3.5 et la vérification de préfixe. Sinon, `IMAGE_NOT_FOUND`.
Conséquence : ce n'est pas un serveur de fichiers statiques sur `images/`, c'est la liste blanche de l'export (DEC-050) appliquée à la lecture. Un fichier déposé à la main, un orphelin, un fichier temporaire restent invisibles, et un nom de fichier venu d'une URL ne désigne jamais autre chose que ce que le projet déclare (MEN-002).

**DEC-089 — MEN-010 : un navigateur est un client ; requêtes intersites et rebinding DNS sont refusés.**
Choix : ajouter MEN-010 au chapitre 9. Une requête autre que `GET` ou `HEAD` qui porte un en-tête `Origin` différent de l'hôte de la requête est refusée par `CROSS_ORIGIN_REFUSED`. `AllowedHosts` est restreint par défaut à `localhost`, `127.0.0.1` et `[::1]`. L'API n'émet aucun en-tête CORS ; ses corps sont `application/json` ou `application/zip`.
Conséquence : MEN-004 publie l'application sur la boucle locale, ce qui la protège du réseau mais pas du navigateur de l'utilisateur, qui est sur la boucle locale et exécute le code de n'importe quel onglet. Un formulaire HTML forgé envoie un `POST` sans vérification préalable — annuler un lot, par exemple ; le contrôle d'origine le refuse. Un domaine tiers résolu vers `127.0.0.1` rend la page et l'API de même origine aux yeux du navigateur, et le contrôle d'origine passe ; c'est l'en-tête `Host`, alors étranger, que le filtrage d'hôtes refuse. Un utilisateur qui publie volontairement l'application sur son réseau ajoute son nom d'hôte, en connaissance de cause. La menace manquait au chapitre 9 pour la raison que DEC-054 a donnée : elle se déduit d'un code qui n'existait pas encore.

**DEC-090 — Seuls les bords journalisent ; Serilog derrière `ILogger<T>`.**
Choix : le domaine et l'Application n'écrivent aucun journal. Trois bords le font : le démarrage, l'intergiciel d'erreurs, le travailleur des lots. L'API écrit par `ILogger<T>`, l'interface d'ASP.NET ; Serilog en est le seul puits, configuré dans `Infrastructure/Logging` et relié par `Serilog.Extensions.Logging`. L'identifiant de job est poussé par `LogContext.PushProperty` **au point d'appel** du cas d'usage, dans le travailleur. Trois paquets Apache-2.0 : `Serilog`, `Serilog.Sinks.File`, `Serilog.Extensions.Logging`. `Serilog.Formatting.Compact` et `Serilog.AspNetCore` sont écartés.
Conséquence : un cas d'usage qui échoue le dit déjà par un code et un message ; le journaliser au bord ne perd rien et n'ajoute aucune dépendance aux couches intérieures. Le chapitre 8 demandait que l'identifiant soit poussé « en entrée du cas d'usage » ; l'Application ne voyant pas Serilog, c'est son appelant qui le pousse, une fois, et tout ce qui s'exécute au-dessous le porte par le contexte asynchrone. Les événements du cadre arrivent dans le même fichier que ceux de l'application, au même format.

**DEC-091 — Une ligne JSON par événement ; un fichier par jour et par taille ; rétention par nombre.**
Choix : le formateur JSON du cœur de Serilog, message rendu, un objet par ligne. Fichiers `pawnsmith-AAAAMMJJ.ndjson`, puis `_001`, `_002`… quand la taille d'un fichier est atteinte. Réglages `Pawnsmith:Logs:Enabled` (vrai), `Directory` (`data/logs`), `RetainedFileCount` (31), `FileSizeLimitBytes` (50 Mio). Désactiver n'écrit aucun fichier ; la console reste réglée par la clé standard d'ASP.NET, et le niveau par `Logging:LogLevel`.
Conséquence : le volume est borné par construction, environ 1,5 Gio par défaut. Sans passage de taille, le puits cesse d'écrire à la limite — un journal qui se tait le jour où il se passe quelque chose. `.ndjson` plutôt que `.json` : un fichier entier n'est pas un document JSON. Ces valeurs s'arbitrent, elles ne se mesurent pas.

**DEC-092 — Ce qui est journalisé : identifiants, codes, messages ; jamais de prompt ni de corps de requête.**
Choix : démarrage (version, dossiers, état du générateur et message d'un refus) ; erreurs de requête (méthode, chemin sans requête, code, statut, message ; l'exception entière pour une erreur sans code) ; lots (début, fin, code et message d'un échec), chaque événement portant `JobId`. Un démarrage impossible écrit une ligne `Fatal` avant de s'arrêter. Ni prompt, ni clause, ni paramètre, ni corps de requête.
Conséquence : précise le chapitre 8, qui citait les prompts parmi ce qu'un journal contient. Le candidat fige ses trois clauses dans `project.json` (DEC-049) ; une copie au journal serait du texte d'utilisateur de plus, dans un endroit plus difficile à effacer, sans rien apprendre de plus. Le message que DEC-084 refuse à la réponse trouve ici sa destination.

**DEC-093 — MEN-004 : l'avertissement au démarrage, et ce qu'un conteneur ne peut pas savoir.**
Choix : une fois le serveur démarré, chaque adresse d'écoute hors boucle locale (`localhost`, `127.0.0.0/8`, `::1`) produit un `Warning` qui la nomme. En conteneur — reconnu à `DOTNET_RUNNING_IN_CONTAINER` —, le texte rappelle `-p 127.0.0.1:8080:8080` et demande de vérifier la publication.
Conséquence : un conteneur écoute forcément sur toutes ses interfaces, et ne voit pas comment son port est publié sur l'hôte. L'avertissement y est donc toujours émis. C'est écrit comme risque accepté de MEN-004 : l'application ne peut pas vérifier ce que seul l'opérateur décide, elle peut seulement le lui rappeler à chaque démarrage, en disant vrai.

**DEC-094 — Le visualiseur : liste blanche par énumération, lecture bornée par la fin.**
Choix : `GET /api/logs` liste les journaux ; `GET /api/logs/{name}?lines=N` rend les `N` dernières lignes (500 par défaut, 5 000 au plus), chacune comme une chaîne, en lisant au plus 4 Mio depuis la fin. Le dossier est énuméré ; seuls les fichiers ordinaires au nom du motif sont retenus ; le nom demandé doit égaler l'un d'eux en ordinal, et c'est le chemin de l'énumération qui est ouvert. Tout autre nom rend `404 LOG_NOT_FOUND`. Une dernière ligne inachevée n'est pas rendue. L'API appelle l'infrastructure directement, sans port.
Conséquence : MEN-002 est tenu sans aucune concaténation, et un lien symbolique au nom valide est écarté comme MEN-008 l'exige à l'export. Un journal de plusieurs dizaines de mégaoctets ne se charge jamais en entier. Un port dans l'Application serait une abstraction sans règle à porter.

**DEC-095 — MEN-005 s'applique à la planche : 8 192 pixels de côté, lus sur l'en-tête.**
Choix : `FileImageSizeReader`, qui lit déjà l'en-tête de chaque élu avant le rendu, refuse une image dont un côté dépasse 8 192 pixels, par `SHEET_INPUT_INVALID`. La valeur est `MaxImageDimensionPx` de T4.
Conséquence : le rendu de la planche décode chaque élu, et depuis T6 ce décodage est à une requête HTTP d'un PNG importé de quelques kilo-octets annonçant des dizaines de milliers de pixels. Le chapitre 9 ne citait que le détourage comme décodeur ; le lecteur renvoyait les plafonds à T5, qui n'est pas écrite. Une image légitime ne dépasse jamais la borne du générateur. Reste un risque accepté : une planche de nombreuses images à la borne tient autant d'images décodées en mémoire ; le borner demanderait un plafond de planche que rien ne fonde aujourd'hui.

**DEC-096 — MEN-011 : la falsification de journal, tenue par le format.**
Choix : ajouter MEN-011 au chapitre 9. La contre-mesure est le format de DEC-091 — toute valeur est une chaîne JSON échappée, un événement occupe exactement une ligne — et le visualiseur, qui rend chaque ligne comme une chaîne sans l'interpréter.
Conséquence : un nom de projet est un texte libre, venu au besoin d'une archive tierce, et les messages d'erreur le citent. Dans un journal texte, un saut de ligne y fabriquerait un événement que l'application n'a jamais écrit. La menace est née de T7 — elle n'existait pas tant que rien n'était journalisé — et c'est la règle de DEC-054 : une menace se déduit d'un code qui existe.

**DEC-097 — La revue du chapitre 9 : chaque ligne nomme son test ou son risque accepté.**
Choix : le §H.7.1 du cahier T7 dresse, pour MEN-001 à MEN-011, le test qui tient chaque menace et le risque accepté qui reste. Le critère du chapitre 12, écrit pour MEN-001 à MEN-007, est étendu à toutes les lignes. Le tableau se rouvre à chaque tranche qui ouvre une surface : T5 pour MEN-005, le front de T6 pour MEN-010 côté navigateur.
Conséquence : une menace sans test nommé n'est pas couverte, elle est espérée. La revue a trouvé un trou réel (DEC-095) et une menace nouvelle (DEC-096) ; c'est ce qu'elle doit faire, et pourquoi elle ne peut pas être faite une seule fois à la fin.

**DEC-098 — Le détourage se fait sans modèle, sur le fond uni que la clause de cadrage exige.** *Décidée par le porteur, le 4 octobre 2026.*
Choix : chaque moitié de l'image jumelée est détourée par diffusion depuis ses bords sur un fond de couleur unie, sans modèle de segmentation. Le port `IBackgroundRemover` reste, pour qu'un modèle s'y branche si les images réelles le demandent.
Conséquence : **supersède DEC-008** sur le « modèle ONNX embarqué » ; « local, systématique, jamais délégué au générateur » demeure. Les poids des modèles disponibles portent la condition de leurs données : BiRefNet et IS-Net sont entraînés sur DIS5K, dont les conditions interdisent l'usage commercial « même après traitement » ; U²-Net sur DUTS, sans licence commerciale claire ; RMBG est annoncé non commercial. Le code de ces modèles est sous licence permissive, pas leurs données, et la règle du projet ne laisse passer aucun d'eux sans risque. Ce qui rend l'option sans modèle possible est une décision déjà prise : la clause de cadrage (DEC-029, DEC-076) exige un fond gris uni, mesuré uniforme à 83 % au pire en T0a (DEC-043). Le prix : un vêtement du même gris que le fond, au bord de l'image, serait mangé.

**DEC-099 — Les PNG se lisent et s'écrivent à la main, sur le sous-ensemble qu'écrit ComfyUI.** *Décidée par le porteur, le 4 octobre 2026.*
Choix : un décodeur pour 8 bits par canal, RGB ou RGBA, non entrelacé, qui vérifie la signature, borne les dimensions sur l'en-tête, vérifie le CRC de chaque bloc et décompresse exactement la taille que l'en-tête annonce ; tout autre PNG est refusé avec un code. Un encodeur RGBA 8 bits qui n'écrit que `IHDR`, `IDAT` et `IEND`.
Conséquence : ferme le choix que DEC-079 réservait au porteur. ImageSharp est écarté pour sa *Split License*, qui conditionne l'usage commercial ; SkiaSharp, sous MIT, aurait amené des bibliothèques natives par plateforme dans l'image. L'encodeur réencode toujours : les métadonnées de ComfyUI — graphe et prompt — ne passent jamais dans un détourage, ce que DEC-079 exigeait puisque les détourages partent dans une archive `Share`.

**DEC-100 — L'algorithme de détourage, et ses valeurs.**
Choix : couleur du fond = médiane des bords haut, gauche et droit ; distance de Tchebychev ; refus si moins de 60 % de ces bords sont à moins de la tolérance ; retrait par le bas des lignes de sol couvertes à 90 %, sur 5 % de la hauteur au plus ; diffusion en 4-connexité depuis les quatre bords sous une tolérance de 24 ; trous enfermés sous une tolérance stricte de 12 et d'au moins 0,05 % de la moitié retirés aussi ; bord adouci entre une et deux fois la tolérance ; recadrage sur la boîte englobante du sujet ; refus d'un sujet sous 1 % de la moitié. Les valeurs vivent dans un record d'options.
Conséquence : la bande de sol que T0a a vue (DEC-043) est retirée avant la diffusion, sans quoi elle deviendrait la ligne des pieds et fermerait l'espace entre les jambes. Le recadrage met les pieds au bas de l'image, ce que le placement de T1 suppose (§B.4.4). Les valeurs s'arbitrent (DEC-057) et se règlent sur les images réelles avec le CLI.

**DEC-101 — Le lot détoure chaque candidat avant de le sauvegarder ; un détourage raté n'arrête pas le lot.**
Choix : `CandidateGeneration` détoure l'image reçue, hors de la porte d'écriture, puis sauvegarde en une fois l'image jumelée, la face et le dos. Un échec laisse le candidat sans détourage, est noté sur le `Job` (identifiant et code) et journalisé ; le lot continue. `CandidateCutout` détoure un candidat existant à la demande, et remplace un détourage existant.
Conséquence : précise DEC-074 — un échec de génération arrête le lot, un échec de détourage non. Le candidat a été généré et peut être jugé sur son image jumelée (DEC-071) ; seul son détourage manque, et il se relance. Les candidats générés avant T5 se détourent par la même voie.

**DEC-102 — Le port de détourage reçoit l'image jumelée et rend deux PNG.**
Choix : `IBackgroundRemover.CutOutPairAsync(byte[] pairedPng, CancellationToken) → CutoutPair(FrontPng, BackPng)`. La découpe de DEC-079 est appliquée dans l'adaptateur.
Conséquence : **supersède la signature du chapitre 7**, écrite avant DEC-079 et avant qu'on sache que les images voyageraient en octets PNG. La découpe demande des pixels décodés, et le décodage est de l'infrastructure.

**DEC-103 — Cinq codes pour le détourage ; la question G est fermée.**
Choix : `CUTOUT_IMAGE_INVALID`, `CUTOUT_IMAGE_TOO_LARGE`, `CUTOUT_BACKGROUND_NOT_UNIFORM`, `CUTOUT_SUBJECT_NOT_FOUND`, `CANDIDATE_NO_PAIRED_IMAGE`, tous en `422`.
Conséquence : la moitié détourage de la question G demandait les dimensions d'entrée du modèle et la durée acceptable sur processeur. Sans modèle, la première n'existe pas, la borne est celle du générateur (8 192 pixels de côté) ; la seconde est celle d'une diffusion, de l'ordre de la centaine de millisecondes.

**DEC-104 — T5 porte la version `0.9.0`.**
Choix : la première modification de T5 passe la version à `0.9.0`.
Conséquence : **supersède le tableau de DEC-058** sur ce point, qui réservait `0.6.0` à T5. Le dépôt est en `0.8.0` depuis T7, et un numéro de version ne recule pas ; `0.6.0` n'existera jamais. La règle de DEC-058 — une tranche livrée vaut un mineur — demeure.

**DEC-105 — Les libellés d'écran : Proposition, Retenue, Prompt modifié, ComfyUI connecté.** *Décidée par le porteur, le 4 octobre 2026.*
Choix : un candidat s'affiche « Proposition » / « Proposal » ; élu, « Retenue » / « Kept » ; élire, « Retenir pour l'impression » / « Keep for printing » ; désaligné, « Prompt modifié depuis » / « Prompt changed since » ; les clauses figées, « Ce qui a changé depuis cette image » / « What changed since this image » ; l'état du générateur, « ComfyUI connecté » / « ComfyUI connected ».
Conséquence : précise le §15.1, qui rendait le vocabulaire du chapitre 2 contraignant jusque dans les libellés. Les concepts ne changent pas, ni les identifiants du code (DEC-037) ; la contrainte passe par cette table, un libellé par concept et par langue. La revue de la maquette a montré que « candidat », « élu » et « désaligné » n'étaient pas compris par le porteur lui-même : un mot juste que l'utilisateur ne comprend pas est un défaut d'interface.

**DEC-106 — Le catalogue est traduit, et la race et la classe deviennent des listes.** *Décidée par le porteur, le 4 octobre 2026.*
Choix : le catalogue passe en `versionSchema` 2. Chaque paramètre et chaque entrée porte un libellé par culture d'interface (`en`, `fr`) ; la valeur et le fragment restent anglais. Deux clés réservées, `race` et `characterClass`, portent les listes des champs obligatoires. La tête du template devient `{race} {characterClass}`, et chaque jeton reçoit le **fragment** de la valeur quand le catalogue la connaît (`orc` → `an orc`) ; une valeur inconnue est insérée telle qu'écrite et signalée.
Conséquence : l'interface montre des listes dans la langue de l'utilisateur, et le prompt reste identique quelle que soit cette langue — sans traduction automatique, qui demanderait un modèle local lourd ou un service en ligne. L'article appartient à l'entrée : « a orc » disparaît. Les détails restent du texte libre anglais. Une clause sujet stockée ne bouge pas ; elle se recompose à la prochaine modification si elle n'a pas été éditée (DEC-067).

**DEC-107 — Le catalogue personnel n'accepte que des objets complets.** *Décidée par le porteur, le 4 octobre 2026.*
Choix : « Autre… » dans une liste ouvre un formulaire qui crée une entrée **complète** — clé existante, valeur unique, un libellé par culture, un fragment — dans `data/user/catalog.{univers}.json`. Le catalogue servi est la fusion du catalogue livré et du catalogue personnel ; chaque entrée dit son origine. Une entrée livrée ne se supprime pas ; une entrée personnelle oui, sans toucher aux gabarits.
Conséquence : plus aucune valeur brute n'entre dans un gabarit par l'interface, donc plus d'alerte « hors catalogue » à la création ; la tolérance de DEC-056 reste pour les projets importés ou anciens. Limite connue : l'application exige que le fragment existe, elle ne peut pas vérifier qu'il décrit la pose (DEC-064). Le dossier utilisateur n'entre dans aucune archive (DEC-022).

**DEC-108 — L'adresse du générateur se règle depuis l'interface.** *Décidée par le porteur, le 4 octobre 2026.*
Choix : `PUT /api/generator` valide l'adresse par les règles de DEC-081 (http ou https, ni identifiants, ni requête, ni fragment), l'enregistre dans `data/user/generator.json` et reconstruit le générateur. Au démarrage, ce fichier l'emporte sur la configuration. Un lot en file garde le générateur avec lequel il a été accepté.
Conséquence : **supersède DEC-081** sur « jamais par l'API ». Le vecteur que DEC-081 retirait — une page malveillante qui choisit l'adresse — est fermé depuis par MEN-010 : aucune écriture d'une autre origine, aucun `Host` non local. Ce qui reste de DEC-081 demeure : la forme de l'adresse, aucune redirection suivie, aucun proxy. Le prix que DEC-081 faisait payer — éditer une variable d'environnement et redémarrer un conteneur — était trop lourd pour un utilisateur qui n'est pas développeur.

**DEC-109 — La clause sujet est verrouillée par défaut ; on en sort et on y revient.** *Décidée par le porteur, le 4 octobre 2026.*
Choix : l'interface affiche la clause sujet en lecture seule. « Personnaliser le texte » la rend éditable et grise les options ; « Revenir au texte automatique » la recompose, par `DELETE …/subject-clause`. Chaque gabarit porte `subjectClauseEdited`, calculé.
Conséquence : DEC-067 demeure — l'édition se déduit, rien n'est stocké. Ce que la fiche corrige est un piège d'interface : une fois la clause modifiée, les options continuaient de changer sans plus rien changer au prompt, et rien ne le montrait.

**DEC-110 — Une bibliothèque de styles, copiés dans le projet.** *Décidée par le porteur, le 4 octobre 2026.*
Choix : des styles livrés (`config/styles.{univers}.json`, un nom par culture) et des styles personnels (`data/user/styles.{univers}.json`). Choisir un style le **copie** dans le projet. La palette n'est plus exposée : le champ reste dans `project.json`, jamais envoyé au générateur, et une palette se dit dans la clause de style. La clause négative passe dans une section « Avancé ». La clause style n'apparaît jamais au niveau d'un gabarit.
Conséquence : un lien vers la bibliothèque aurait fait basculer en silence les images de tous les projets au moindre changement, et une archive ne serait plus complète. Ferme l'écart trouvé sur la palette, que le glossaire disait « intégrée au style » et que le code stockait sans l'envoyer. Ferme la lecture du §15.5 : DEC-006 garde le style au niveau du projet, pas hors de l'interface. La clause négative n'a aucun effet à CFG 1,0 (DEC-077) ; l'afficher au premier plan promettait un effet qui n'existe pas.

**DEC-111 — L'interface enregistre chaque champ quand on le quitte.** *Décidée par le porteur, le 4 octobre 2026.*
Choix : pas de bouton Enregistrer. Un champ texte s'enregistre à la perte du focus, une liste ou un choix quand il change ; un indicateur dit l'état.
Conséquence : changer d'écran ou de projet ne perd rien. C'est sans danger parce que le désalignement est calculé (DEC-030) : un retour en arrière refait passer les images au vert, et depuis DEC-112 le style ne bouge plus une fois des images produites.

**DEC-112 — L'univers et le style se figent à la première proposition ; un projet se duplique.** *Décidée par le porteur, le 4 octobre 2026.*
Choix : tant qu'aucun gabarit n'a de proposition, tout se modifie. Dès la première, l'univers et le style sont refusés au changement (`UNIVERSE_FROZEN`, `STYLE_FROZEN`) ; nom, géométrie, format et cotes d'onglet restent libres. `POST /api/projects/{folder}/duplicate` crée un projet neuf qui copie les gabarits sans leurs propositions, avec le style demandé. La règle est dans le cas d'usage des réglages, qui compare au projet chargé.
Conséquence : **supersède DEC-030 et DEC-055** pour l'univers et le style ; la géométrie et le format restent régis par elles. Ce n'est pas le mécanisme de consentement que DEC-030 rejetait — aucun avertissement à valider : un projet est un ensemble cohérent d'images d'un même style, et un autre style est un autre projet, que la duplication fournit en un clic. « Prompt modifié » ne subsiste que pour un gabarit retouché après ses images, ou un cadrage changé dans le workflow. La signature de `SaveAsync` ne change pas.

**DEC-113 — L'aperçu de la planche est le PDF lui-même.**
Choix : l'écran Mise en page affiche le PDF de la planche dans la page (`sheet.pdf?disposition=inline`), à côté de l'indicateur de capacité tiré du rapport.
Conséquence : le §15.2 exige que l'aperçu montre ce que le PDF contiendra, repères compris ; le seul aperçu qui le garantit est le PDF. Redessiner la planche dans le navigateur dupliquerait la mise en page du domaine en TypeScript, et le §15.5 interdit que l'interface décide de la planche. Ferme la question laissée ouverte par la maquette.

**DEC-114 — Le front n'ajoute aucune dépendance ; la tranche porte la version `0.10.0`.**
Choix : React, `react-i18next` et `i18next` suffisent. Le routage s'écrit sur le fragment d'URL, à la main. Aucun banc de test du navigateur dans cette tranche ; la parité des clés de traduction est vérifiée par un script sans dépendance. La version passe à `0.10.0`.
Conséquence : une bibliothèque de routage ou de requêtes serait une dépendance pour une dizaine d'écrans. L'absence de tests automatisés du front est un trou connu, écrit au §I.12 du cahier : ce qui le compense est `tsc`, ESLint et un parcours éprouvé dans le conteneur.

---
 
## 12. Découpage en tranches
 
Chaque tranche est livrable, testable, et se termine par une relecture intégrale.
 
Ordre effectif après DEC-044 : **Fondations → T0a → T1 (code) → T2 → … → T0b → T1 (validation)**. T0b est reportée à une date non fixée ; elle ne bloque que la clôture formelle de T1.
 
### Fondations — squelette et chaîne de compilation
 
Structure du dépôt, `Directory.Build.props`, `.editorconfig`, `.gitignore`, solution .NET aux quatre projets, squelette React + `react-i18next`, `Dockerfile` multi-étapes, intégration continue, `LICENSE`, `README.md`, `THIRD-PARTY-NOTICES.md`, `CLAUDE.md`.
 
**Critères de sortie** : la solution compile, les tests (vides) s'exécutent, `docker run` sert le front, la bascule de langue fonctionne, `Pawnsmith.Domain.csproj` ne référence rien, **l'intégration continue est passée au vert au moins une fois**.
 
*Détaillée en partie A du cahier des charges T1.*
 
### T0a — Test décisif de DEC-003 *(hors code, sans impression)*
 
Trois sujets nettement différents, une génération chacun, grille d'évaluation à huit critères. Ne dépend d'aucun code et ne consomme aucun papier.
 
**Critères de sortie** : verdict rendu — le modèle local produit-il une planche de rotation exploitable ? Prompt de référence, modèle, LoRA et paramètres de génération consignés.
 
*C'est la seule question ouverte du projet capable de modifier l'architecture. Elle passe avant tout le reste.*
 
### T1 — Noyau de mise en page
 
Domaine pur plus rendu PDFsharp. Entrée : un dossier de PNG déjà détourés. Sortie : un PDF calibré. Ni IA, ni interface.
 
Écrite avec les valeurs de calibration provisoires. Le code lit les valeurs, il ne les connaît pas.
 
**Critères d'acceptation** : voir B.9 du cahier des charges. Ils exigent une **planche imprimée en main** et ne peuvent donc être cochés qu'après T0b.
 
*Cette tranche vient tôt parce qu'elle porte le risque physique et qu'elle est immédiatement vérifiable au ciseau.*
 
### T0b — Calibration physique *(hors code, avec le CLI de T1)*
 
Tirage papier, mesures, découpe, montage, pose sur le tapis, à l'aide des gabarits produits par le CLI de B.7.
 
**Critères de sortie** : le tableau §5.6 est rempli avec des mesures réelles ; la loi de progression des hauteurs est arbitrée et respecte le plafond du §5.7 ; `calibration.json` ne contient plus aucune valeur provisoire.
 
### T2 — Modèle de projet et persistance
 
Entités, sérialisation, chargement, sauvegarde, export et import d'archives. Plus un **point d'entrée en ligne de commande** — `project new`, `check`, `export`, `import` — écrit en dernière tâche, jetable et non livré comme celui de T1 (DEC-059).
 
**Critères d'acceptation** : voir le **§C.13 du cahier des charges T2**, qui fait foi. Il reprend et précise ceux-ci — aller-retour export/import sans perte en profil `Backup` ; MEN-001 couvert par un test avec archive malveillante ; MEN-006 couvert par un test **exhaustif** sur la liste blanche, et non par la recherche d'un nom de fichier ; MEN-008 et MEN-009 couverts ; `versionSchema` présent ; **aucune valeur dérivée n'est sérialisée** (`promptResolu`, `desaligne`).
 
### T3 — Composition de prompts et catalogue
 
Templates en fichiers, gabarits, catalogue éditable, clause sujet stockée et modifiable, et les règles de gestion que le chapitre 16 réservait à cette tranche. Plus un point d'entrée en ligne de commande, jetable comme ceux de T1 et T2.
 
**Critères d'acceptation** : voir le **§D.12 du cahier des charges T3**, qui fait foi. Il reprend et précise ceux-ci — composition déterministe (même entrée, même sortie) ; les clauses style et cadrage sont inatteignables depuis l'interface de gabarit, et la signature de `IPromptComposer` le rend structurellement vrai ; le désalignement est correctement calculé après édition d'une clause sujet, d'un style ou d'un univers ; `versionSchema` de `project.json` reste à 1.
 
### T4 — Client générateur et production de couples
 
Client HTTP ComfyUI, substitution du template de workflow, génération jumelée, découpe.
 
**Critères d'acceptation** : voir le **§E.13 du cahier des charges T4**, qui fait foi. Il reprend et précise ceux-ci — générateur injoignable géré comme un état normal ; un lot interrompu conserve les candidats déjà produits ; l'image jumelée brute est conservée pour diagnostic ; ce qui part au générateur est exactement le prompt résolu (DEC-049).
 
### T5 — Détourage
 
Runtime ONNX, fournisseur d'exécution configurable, plafonds d'entrée.

**Superséde par DEC-098** : pas de modèle, donc ni runtime ONNX ni fournisseur d'exécution. Spécifiée par le cahier des charges T5 (`pawnsmith-cahier-des-charges-t5.md`), dont le §F.10 fait foi.
 
**Critères d'acceptation** : MEN-005 couvert ; échec propre sur image malformée ; PNG de sortie à fond réellement transparent.
 
*Tranche à relire en profondeur (DEC-027).*
 
### T6 — API et interface
 
Points de terminaison, front React, galerie de candidats, validation, export, localisation complète.

Scindée en deux. La **première partie, l'API**, est spécifiée par le cahier des charges T6 (`pawnsmith-cahier-des-charges-t6.md`), dont le §G.12 fait foi pour elle. La seconde partie, le front, est spécifiée par le cahier `pawnsmith-cahier-des-charges-t6-front.md`, dont le §I.11 fait foi pour elle.
 
**Critères d'acceptation** : aucune chaîne en dur ; bascule français/anglais sans rechargement ; capacité de page affichée ; codes d'erreur correctement traduits ; les candidats désalignés sont visuellement distingués des candidats sains ; la structure du chapitre 15 est respectée, y compris la liste du §15.5.
 
### T7 — Observabilité et durcissement
 
Serilog, visualiseur de journaux, rotation et rétention, revue complète du chapitre 9.

Spécifiée par le cahier des charges T7 (`pawnsmith-cahier-des-charges-t7.md`), dont le §H.9 fait foi. Le visualiseur y est écrit **côté API** ; son écran appartient au front de T6.
 
**Critères d'acceptation** : chaque menace MEN-001 à MEN-007 est soit couverte par un test, soit explicitement documentée comme risque accepté.
 
> Les tranches T2 à T5 n'ont pas d'interface. Elles s'éprouvent par tests d'intégration et, si nécessaire, par un point d'entrée en ligne de commande minimal — jetable, non livré.
 
---
 
## 13. Évolutions différées
 
À reprendre une fois la v1 fonctionnelle et réellement utilisée. Rien ici ne doit être anticipé dans le code au-delà des points d'extension déjà prévus.
 
| Réf. | Évolution | Point d'extension déjà en place |
|---|---|---|
| EVO-001 | **Composition de prompt par modèle de langage.** Second adaptateur de `IPromptComposer`, appelant un point de terminaison compatible OpenAI (Ollama, LM Studio). Ne réécrit **que la clause sujet** ; les clauses style et cadrage lui restent inaccessibles — contrainte désormais portée par la signature du port (DEC-028). Repli silencieux sur le template si injoignable. Séquencer le chargement des modèles pour éviter la contention VRAM. | `IPromptComposer` |
| EVO-002 | **Fournisseur d'images distant.** Second adaptateur de `IImageGenerator`. Introduit un compteur de coût, une confirmation avant lot et un cache prompt+graine. | `IImageGenerator` |
| EVO-003 | **Troisième géométrie** : deux pièces séparées, collées entre elles ou sur une âme carton, avec repères d'alignement en croix. Considérée pour l'instant comme une variante du pion à socle. | Fonction de placement |
| EVO-004 | **Univers supplémentaires** (steampunk, science-fiction, contemporain). | Champ `univers` + fichiers de templates |
| EVO-005 | **Mélange de tailles sur une page**, par shelf packing. Renforcée par DEC-031 : sans elle, une seule créature de taille Small consomme une page entière. | Moteur de mise en page |
| EVO-006 | **Validation en lot** en complément de la validation unitaire. | Interface |
| EVO-007 | **Langues supplémentaires.** | Fichiers de ressources |
| EVO-008 | **Déploiement distribué** : application sur une machine sobre, générateur sur le poste équipé. Sans impact sur la conception — le client est déjà HTTP. À reprendre uniquement si la charge locale devient un problème. | Aucun |
| EVO-009 | **Passage par la 3D** pour la cohérence recto/verso, si DEC-003 déçoit à l'usage : image → modèle 3D → deux rendus orthographiques. | `IPawnPairProducer` |
| EVO-010 | **Import d'images externes déjà détourées.** Un gabarit peut recevoir un couple recto/verso fourni par l'utilisateur au lieu d'un candidat généré. Rend l'application utilisable sans GPU, et sans modèle de diffusion du tout. Le format d'entrée est **déjà** celui de T1 : un dossier de PNG à fond transparent plus un manifeste. À ne pas anticiper dans le code, mais à ne rien faire qui l'empêche. | Format d'entrée de T1 ; `Candidat` |
| EVO-011 | **Taille Minuscule** (Tiny, emprise 12,7 mm). Écartée de la v1 tant que la faisabilité physique n'est pas établie : une unité dépliée ferait 12,7 mm de large sur une hauteur dépliée d'une centaine de millimètres, à découper et plier au milieu. À trancher par un essai papier, pas par un raisonnement. | Table des tailles |
| EVO-012 | **Grilles hexagonales.** Table d'emprises **distincte**, jamais dérivée des emprises carrées — voir §14.4. | Table des tailles |
 
---
 
## 14. Référence — grilles de jeu et tailles de créature
 
Ce chapitre est une table de faits externes au projet. Il existe pour une seule raison : éviter qu'une emprise soit un jour redevinée de mémoire. Les valeurs ci-dessous sont documentées et sourcées ; celles du §5.6, non — ne pas confondre les deux registres.
 
### 14.1 La grille carrée
 
| | Valeur |
|---|---|
| Côté d'une case | **1 pouce = 25,4 mm exactement** |
| Équivalent en jeu | 5 pieds (1,524 m) |
| Éditions concernées | D&D 3.5, 4, 5e (2014), D&D 2024, Pathfinder 1 et 2, Starfinder |
 
Standard stable depuis une vingtaine d'années ; aucune édition récente n'y a touché. Le tapis de référence Chessex offre 23,5 × 26 pouces de surface, soit 22 × 25 cases.
 
Des tapis européens à cases de 30 mm ou 25 mm ronds existent. Ils sont minoritaires, mais ils justifient à eux seuls que DEC-015 rende les emprises surchargeables.
 
### 14.2 Tailles de créature et emprises
 
| Catégorie (VO) | Nom Pawnsmith | Espace occupé | Cases carrées | Socle du commerce |
|---|---|---|---|---|
| Tiny | *(hors v1, EVO-011)* | 2,5 × 2,5 pieds | ¼ (4 par case) | 0,5 pouce — 12,7 mm |
| Small | `Small` | 5 × 5 pieds | 1 | 1 pouce — **25,4 mm** |
| Medium | `Medium` | 5 × 5 pieds | 1 | 1 pouce — **25,4 mm** |
| Large | `Large` | 10 × 10 pieds | 4 (2×2) | 2 pouces — **50,8 mm** |
| Huge | `Huge` | 15 × 15 pieds | 9 (3×3) | 3 pouces — **76,2 mm** |
| Gargantuan | `Gargantuan` | 20 × 20 pieds ou plus | 16 (4×4) | 4 pouces — **101,6 mm** |
 
Identique en D&D 2024 et en Pathfinder 2. Pathfinder 1 comportait en outre **Colossal** (30 pieds, 6 × 6 cases, 152,4 mm), abandonnée en Pathfinder 2 ; hors périmètre.
 
> **Nuance importante.** Les règles ne définissent que l'**espace occupé au sol**. Le diamètre du socle est une convention du hobby, très respectée mais non normative — et **aucune règle ne définit la hauteur d'une créature**. C'est ce vide qui rend DEC-032 nécessaire.
 
### 14.3 Échelle réelle de la grille
 
1 pouce pour 5 pieds donne une échelle d'environ **1:60**. À cette échelle, un humanoïde d'1,80 m mesurerait 30,5 mm de haut. Les hauteurs retenues par Pawnsmith sont volontairement supérieures, pour la lisibilité à un mètre. Voir DEC-032.
 
### 14.4 La grille hexagonale — mise en garde
 
Hors périmètre v1 (EVO-012), mais la mise en garde doit être écrite avant qu'on en ait besoin.
 
- **Aucun fabricant n'indique comment il mesure ses hexagones.** Trois conventions coexistent : plat-à-plat, pointe-à-pointe, longueur d'arête. Sur un hexagone régulier, plat-à-plat = 0,866 × pointe-à-pointe ; l'écart entre deux lectures d'un « hexagone de 1 pouce » atteint 3,4 mm.
- Faisceau d'indices en faveur de **plat-à-plat = 25,4 mm** : le comptage d'hexagones du tapis Chessex réversible (21/22 × 28 sur 23,5 × 26 pouces) ne recolle qu'avec un pas horizontal d'un pouce ; et c'est la seule lecture qui laisse un socle de 25 mm entrer dans l'hexagone, donc la seule qui rende les deux faces d'un tapis réversible mutuellement compatibles. **C'est une inférence, pas une mesure** : à confirmer au réglet sur un tapis réel avant tout usage.
- **Les emprises hexagonales et carrées ne se correspondent pas.** Les règles optionnelles du DMG 5e font occuper 1 hexagone à une Medium, 3 à une Large, 7 à une Huge, 12 à une Gargantuan — surfaces nettement inférieures aux 4, 9 et 16 cases carrées équivalentes. Le socle physique d'une Huge (76 mm) n'entre pas dans 7 hexagones d'un pouce. **Ne jamais dériver une emprise hexagonale d'une emprise carrée par le calcul** : si l'hexagonal entre un jour au périmètre, c'est une table de valeurs distincte, mesurée.
### 14.5 Sources
 
- LITKO — *D&D Miniature Base Sizes Chart* : https://litko.net/pages/dnd-base-sizes-guide
- Archives of Nethys — *Pathfinder 2e, Size, Space, and Reach* : https://2e.aonprd.com/Rules.aspx?ID=2359
- Wargamer — *DnD sizes explained for 5.5e* : https://www.wargamer.com/dnd/sizes-5e
- Chessex — *Reversible Battlemat 1" Squares & 1" Hexes* : https://www.chessex.com/reversible-battlemat-1-squares-1-hexes-23-x-26-playing-surface
- Matters of Critical Insignificance — *Creature size on hex grid* : https://criticalinsignificance.wordpress.com/2021/02/09/rant-creature-size-on-hex-grid-is-waay-off/
*Consultées le 29 août 2026.*

---

## 15. Interface — coquille retenue

> **Statut.** Ce chapitre fixe la **structure** de l'interface, pas son apparence. Il est écrit à la suite d'une maquette exploratoire du 29 août 2026, dont la charpente a été retenue et le contenu rejeté (DEC-034). L'interface est livrée en **T6** ; rien ici n'est à coder avant, au-delà du squelette de A.5.

### 15.1 Navigation

Cinq étapes, dans l'ordre du pipeline du chapitre 4 :

| Étape | Contenu | Tranche |
|---|---|---|
| **Projet** | Création, nom, univers, style, géométrie, format de papier. Les quatre derniers sont **modifiables après création** ; les modifier désaligne les candidats existants plutôt que d'être interdit ou précédé d'un avertissement (DEC-030, DEC-055). *Depuis DEC-112 :* l'univers et le style se figent à la première proposition ; la géométrie et le format restent modifiables. | T2 |
| **Gabarits** | Saisie des gabarits : paramètres, quantité, prompt résolu. | T3 |
| **Génération** | Lancement des lots, galerie de candidats, validation du couple recto/verso. | T4, T5 |
| **Mise en page** | Aperçu des planches calculées, capacité de page. | T6 |
| **Impression** | Export PDF, choix de la culture. | T6 |

Le vocabulaire du chapitre 2 est **contraignant jusque dans les libellés d'écran** : on écrit *Gabarit*, jamais « créature » ni « figurine ». Une divergence de vocabulaire dans l'interface est un défaut au même titre qu'une divergence dans le code — c'est même là qu'elle coûte le plus cher, puisque c'est la seule que l'utilisateur voit.

L'ordre est celui du pipeline, mais **la navigation n'est pas un assistant** : on revient à une étape antérieure sans perdre l'état, et sans repasser par les suivantes.

### 15.2 Anatomie de l'écran de mise en page

Trois zones :

- **Au centre, l'aperçu de la planche courante**, à l'échelle, cotes du format affichées sur les bords. C'est le centre de gravité de l'écran : une planche se juge en la regardant, pas en lisant des chiffres.
- **À gauche, le panneau de paramètres** (§15.3).
- **À droite, les mesures de la page** : format, taille des pions, capacité, place restante.

**L'aperçu montre ce que le PDF contiendra, repères d'impression compris** — trait de calibration, traits de coupe, lignes de pliage. Un aperçu qui les masque donne une fausse idée de l'encombrement réel : la zone de calibration mange 14 mm de hauteur utile, ce qui suffit à faire perdre une rangée. Masquer les repères, c'est afficher une capacité qui n'existe pas.

### 15.3 Panneau de paramètres à deux niveaux

La distinction obligatoire / optionnel est celle de DEC-024, et elle est structurante :

| Niveau | Champs | Sémantique d'un champ laissé vide |
|---|---|---|
| **Obligatoires** | `race`, `classe`, `taille` | — ils ne peuvent pas être vides |
| **Optionnels** | clés du catalogue : arme, armure, vêtement, couleur… Le catalogue est global, un fichier par univers, et chaque valeur porte un fragment de phrase (DEC-063, DEC-064 ; §D.4 du cahier T3) | **« non contraint »**, et non « absent de l'illustration » |
| **Libres** | `details`, puis la `clauseSujet`, stockée et éditable | — |
| **Lecture seule** | le `promptResolu`, dérivé et affiché tel quel (DEC-028) | — |

**La formulation des optionnels est un travail d'interface à part entière, pas un choix de libellé.** Un utilisateur qui décoche « arme » croit demander un personnage désarmé ; il demande en réalité un personnage dont l'arme n'est pas imposée. C'est l'incompréhension la plus prévisible du produit, et elle se règle par les mots, pas par un champ de plus.

Ce qui **n'a rien à faire dans ce panneau** : l'univers, le style et la géométrie. Ce sont des propriétés de **projet**, jamais de gabarit, et les afficher parmi les paramètres d'un gabarit invite exactement la dérive que DEC-006 existe pour empêcher. Ils se modifient au niveau du projet, où les modifier désaligne les candidats existants (DEC-030, DEC-055) — c'est là que l'interface doit les présenter.

### 15.4 Indicateur de capacité

Le §5.4 le demande. Ce qu'il affiche :

- la **capacité** de la page courante, **en cellules** ;
- le **nombre de cellules occupées** ;
- la **taille** des pions de la page, puisqu'une page n'en porte qu'une seule (DEC-005).

Il est utile parce qu'il répond à la question réellement posée pendant la composition : *est-ce que ce gobelin de plus tient sur cette page, ou est-ce qu'il en coûte une nouvelle ?*

**Un taux de remplissage en pourcentage ne répond pas à cette question et ne doit pas remplacer le compte.** À 85 % de surface occupée il peut ne rester aucune cellule libre, le reste étant réparti entre les gouttières et le bord. Ce qui se compte, ce sont les cellules.

Une **capacité nulle** s'affiche comme une erreur explicite nommant la taille, le format et la géométrie (§5.4), jamais comme une page vide.

### 15.5 Ce que l'interface ne fait pas

Chacun de ces points découle d'une décision déjà prise. Ils sont listés ensemble parce qu'ils sont tous naturels à ajouter, et tous faux.

| L'interface… | Parce que |
|---|---|
| …n'expose ni compte, ni connexion, ni partage | Non-objectif permanent du §1.5. Et l'absence d'authentification n'est tenable que si personne ne croit qu'il y en a une (MEN-004) |
| …ne permet ni de déplacer ni de faire pivoter un pion à la souris | La planche est **calculée** par le domaine, pas composée à la main (§5.4). Le rendu ne décide de rien, l'interface non plus |
| …ne mélange jamais deux tailles sur une page | DEC-005. Le mélange est différé en EVO-005 |
| …ne permet pas de désactiver les repères d'impression | DEC-017 |
| …n'offre aucune mise à l'échelle automatique | §B.6 du cahier des charges. Seul `scaleCorrectionFactor` agit, et il agit sur **tout**, trait de calibration compris (§B.5.5) |
| …n'expose ni la clause style ni la clause cadrage | DEC-028, DEC-029 |

*Depuis DEC-110 :* la clause style n'est jamais exposée **au niveau d'un gabarit** ; elle s'affiche et se choisit à l'étape Projet, où le §3.1 et le §15.1 la rendent modifiable. La clause cadrage n'apparaît nulle part.

### 15.6 Arbitrages rendus, et la question qui reste

La maquette a soulevé deux questions qu'aucune décision antérieure ne couvrait. Elles sont tranchées, en **DEC-035** (marges uniformes) et **DEC-036** (le paysage est une entrée de configuration, pas une bascule). Aucune des deux n'est un sujet d'interface : la première touche la formule de capacité du §B.5.2, donc le cœur de T1.

**Question tranchée par la négative, en T2 : `gutterMm` ne devient pas un réglage de projet, et `silhouetteMarginMm` non plus.** Voir **DEC-052**, qui ferme cette question.

Les quatre valeurs du bloc `layout` de `calibration.json` n'ont toujours pas la même nature, et les traiter en bloc reste une erreur : `pageMarginMm` et `calibrationZoneHeightMm` sont des **propriétés de l'imprimante et de la planche**, elles se mesurent en T0b ; `gutterMm` est une **préférence** — le §B.5.3 admet lui-même la valeur 0, qui « laisse moins de marge au ciseau » ; `silhouetteMarginMm` était classée entre les deux.

Ce qui a tranché n'est pas cette distinction, c'est le critère de DEC-040 — **qui détermine la valeur** — appliqué au fait que Pawnsmith est mono-utilisateur (§1.5). Une valeur déterminée par la main de l'utilisateur est ici une propriété **globale**, pas une propriété de projet : la placer dans le projet obligerait la même personne à la ressaisir partout. Les seules valeurs surchargeables par projet restent celles que DEC-040 a désignées, `tabWidthMm` et `tabHeightMm` (DEC-053).

---

## 16. Questions ouvertes

> **Ce chapitre reprend le §4 de `pawnsmith-etat-du-projet.md`, qui est supprimé.** Ce document était périmé partout ailleurs — il annonçait la bible v0.3, T0a « à faire », T1 « non démarrée » — mais son §4 portait neuf questions écrites nulle part d'autre, et plusieurs fiches les citent. Toute fiche antérieure à la v0.10 qui renvoie à « la question X de l'état du projet » désigne donc la question X **de ce chapitre**. Les fiches ne sont pas éditées pour autant : ce sont des enregistrements datés.

Aucune de ces questions n'est bloquante aujourd'hui. Elles sont classées par **échéance**, c'est-à-dire par la tranche qui ne peut pas commencer sans la réponse.

| Réf. | Sujet | À trancher avant |
|---|---|---|
| **E** | **Contrat d'API.** *Fermée pour sa partie serveur* par le cahier T6 (§G.3 à §G.10) : points de terminaison, verbes, charges utiles, codes et statuts. **Fermée pour le front** par le cahier T6 front (§I.10) : les écrans et les routes qu'ils consomment. | ~~T6 (front)~~ |
| **G** | **Valeurs non fonctionnelles.** *Scindée par DEC-080* : la moitié génération — délai d'attente, plafond de candidats par lot, taille et dimensions d'une image reçue — est arbitrée au §E.9 du cahier T4. Reste la moitié détourage : dimensions maximales en entrée du modèle de segmentation, durée acceptable d'un détourage sur processeur. DEC-057 pose que ces bornes **s'arbitrent** et ne se mesurent pas. **Fermée par DEC-103** : sans modèle (DEC-098), il n'y a pas de dimension d'entrée de modèle ; la borne est celle du générateur, et la durée est celle d'une diffusion. | ~~T5~~ |
| **H** | **Dépôt public ou privé.** *Visibilité toujours non confirmée.* Elle est citée par DEC-058, qui exclut la révision de source de la version pour ne pas publier d'identifiant de commit dans une archive — précaution qui vaut dans les deux cas, donc la question ne bloque rien. | Libre |
| **I** | **Loi de progression des hauteurs.** DEC-032 pose la contrainte — plafond d'environ 112 mm sur US Letter — mais pas les valeurs. Se tranche en T0b, tapis sous les yeux, les cinq tailles montées côte à côte. | T0b |

**Questions fermées depuis l'ouverture du chapitre**, conservées parce qu'une question refermée se repose sinon tous les trois mois :

| Sujet | Résolution |
|---|---|
| Éditabilité du prompt résolu | DEC-028 — seule la clause sujet est éditable |
| Exposition de la clause de cadrage | DEC-029 — jamais dans l'interface ; point d'extension par fichier |
| Verrouillage de `style`, `univers`, `geometrie`, `formatPapier` | DEC-030, confirmée par DEC-055 — plus aucun verrou, désalignement calculé. *Rouverte et tranchée par DEC-112* : univers et style figés à la première proposition, géométrie et format libres |
| Granularité de la géométrie (projet ou export) | DEC-030 — paramètre de rendu, modifiable librement |
| Nommage et nombre de tailles | DEC-031 — cinq tailles nommées d'après les règles ; l'échelle S/M/L/XL/XXL est écartée |
| Origine des emprises de grille | Chapitre 14 — table sourcée, valeurs définitives |
| Origine des hauteurs de pion | DEC-032 — aucune source ne les définit ; c'est une décision bornée par le papier |
| Ordre T0 / T1, et prérequis de T0 | DEC-033 puis DEC-044 — T0a d'abord, T0b après le code de T1, puis reportée |
| Niveau de `gutterMm` et de `silhouetteMarginMm` | DEC-052 — ils restent dans la calibration |
| Identité d'un projet | DEC-047 — un `projectId` opaque ; le nom du dossier n'a aucune sémantique |
| **A** — Parcours utilisateur | §D.3 du cahier T3 — six étapes, T3 ne touche que la deuxième. Pas de fiche : un parcours est une description, pas une décision |
| **B** — Sort de l'ancien élu | DEC-068 — il redevient non élu, son statut ne bouge pas |
| **C** — Gabarit sans élu, groupe de taille sans élu | DEC-069 — ignoré et signalé, jamais de page vide |
| **C** — Suppression d'un gabarit dont un candidat est élu | DEC-070 — tout part, fichiers compris |
| **C** — Couplage statut / fichiers | DEC-071 — le statut n'exige rien, l'élection exige les deux détourages |
| **C** — Quantité dépassant la capacité | Sans objet — `Pagination.Plan` de T1 pagine, seule une capacité nulle est une erreur |
| **D** — Entité Catalogue | DEC-063 — global, en fichier de données, un par univers |
| **B** — Machine à états du `Job` | DEC-074 — cinq états, trois terminaux, un échec arrête le lot, le `Job` vit en mémoire |
| **F** — Template de workflow ComfyUI | DEC-076 — trois jetons en liste close, valeur entière, substitution sur le graphe ; schéma au §E.5 du cahier T4 |
| **C** — Export d'un élu désaligné | DEC-082 — l'export passe outre et le signale, clause par clause |

> **Un point mineur laissé de côté, qui n'a pas de fiche.** La demande initiale « on fait attention aux règles de l'OWASP » a été traitée par un modèle de menace déduit de l'architecture (chapitre 9) plutôt que par une checklist générique. C'est un arbitrage assumé. DEC-054 en montre la contrepartie : une menace ne se déduit que d'un code qui existe, donc le chapitre 9 se revoit **à chaque tranche** qui ouvre une surface, et non une seule fois en T7.

