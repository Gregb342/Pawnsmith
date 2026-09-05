# Pawnsmith — Bible du projet
 
| | |
|---|---|
| **Nom de code** | Pawnsmith |
| **Version du document** | 0.10 |
| **Date** | 2 septembre 2026 |
| **Statut** | Brouillon — évolutif |
| **Porteur** | Grégoire |
| **Licence visée** | Open source, permissive (MIT recommandé) |
 
> **Comment lire ce document.** Il est vivant. Le chapitre 11 (journal des décisions) fait foi : quand une décision change, on ajoute une fiche, on ne réécrit pas l'ancienne. Les valeurs marquées `À CALIBRER` sont volontairement absentes tant que la tranche T0 n'a pas été menée — ne pas les inventer.
 
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
| `clauseSujet` | texte | Produite par le composeur à partir des champs ci-dessus. **Stockée et éditable.** Seul segment du prompt que l'utilisateur peut modifier (DEC-028). Ne se régénère pas toute seule après édition. |
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
 
---
 
## 7. Contrats des ports
 
Signatures indicatives, à affiner à l'implémentation.
 
```csharp
// Disponibilité + génération. L'indisponibilité est un état normal, pas une exception.
public interface IImageGenerator
{
    Task<GeneratorHealth> CheckAsync(CancellationToken ct);
    Task<RawImage> GenerateAsync(GenerationRequest request, CancellationToken ct);
}
 
// Produit le couple recto/verso. Point de substitution du choix DEC-003.
public interface IPawnPairProducer
{
    Task<PawnPair> ProduceAsync(string prompt, int seed, CancellationToken ct);
}
 
// Détourage. Fournisseur d'exécution ONNX configurable (cpu | cuda).
public interface IBackgroundRemover
{
    Task<TransparentImage> RemoveAsync(RawImage image, CancellationToken ct);
}
 
// Deux méthodes, deux responsabilités distinctes — voir DEC-028.
// ComposeSubject produit la clause sujet initiale ; l'utilisateur peut ensuite l'éditer.
// Assemble reconstruit le prompt complet à partir d'une clause sujet éventuellement éditée.
// Aucune signature ne permet de fournir une clause cadrage ou une clause style depuis
// le niveau du gabarit : le verrouillage est porté par le type, pas par une convention.
public interface IPromptComposer
{
    string ComposeSubject(Gabarit gabarit, Univers univers);
    string Assemble(string clauseSujet, Style style);
}
 
// LoadAsync reçoit la calibration : valider une surcharge d'onglet suppose de la
// comparer à pawnWidthMm, et produire les diagnostics relationnels la demande aussi
// (DEC-053, DEC-056).
// SaveAsync ne reçoit PAS l'état antérieur, et ne doit jamais en recevoir : aucun
// champ n'est verrouillé après création, donc le dépôt n'a aucune transition à
// arbitrer. Une telle règle appartiendrait à un cas d'usage (DEC-055).
public interface IProjectRepository
{
    Task<Project> LoadAsync(string path, Calibration calibration, CancellationToken ct);
    Task SaveAsync(Project project, CancellationToken ct);
    Task ExportArchiveAsync(Project project, string destination, CancellationToken ct);
    Task<Project> ImportArchiveAsync(string archivePath, string destination, CancellationToken ct);
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
- Destination par défaut : le volume `/app/data/logs`, **jamais le dossier du projet**. Les journaux contiennent des prompts, des chemins absolus et l'URL du générateur ; ils ne doivent pas partir avec une archive de projet.
- Chaque **Job** porte un identifiant, poussé une seule fois en entrée du cas d'usage via `LogContext.PushProperty`. Il n'est jamais passé en paramètre de méthode en méthode.
- L'interface expose un visualiseur de journaux dans la section configuration. Il lit **uniquement** dans le répertoire de journaux, par liste blanche de noms de fichiers (voir MEN-002).
- La journalisation est désactivable par configuration.
---
 
## 9. Sécurité — modèle de menace
 
Les menaces sont déduites de l'architecture, non d'une liste générique. Chaque entrée est traçable vers une décision de conception.
 
| Réf. | Menace | Vecteur | Contre-mesure |
|---|---|---|---|
| MEN-001 | **Zip Slip** | Archive importée contenant une entrée `../../` (conséquence directe de DEC-011) | Résoudre le chemin absolu de chaque entrée et vérifier qu'il est bien préfixé par le dossier de destination **avant** écriture. Rejet global de l'archive sinon. |
| MEN-002 | **Traversée de chemin** | Visualiseur de journaux avec nom de fichier en paramètre | Liste blanche de noms. Jamais de concaténation de chemin depuis une entrée utilisateur. |
| MEN-003 | **SSRF** | L'URL du générateur est fournie par l'utilisateur et appelée par le serveur | Liste blanche de schémas et de ports. Documenter l'hypothèse de déploiement en réseau de confiance. |
| MEN-004 | **Exposition réseau** | Application sans authentification publiée sur toutes les interfaces par Docker | Documenter `-p 127.0.0.1:8080:8080` comme forme canonique. Avertissement au démarrage si l'écoute n'est pas locale. |
| MEN-005 | **Entrée image non fiable** | Bombe de décompression, dimensions extrêmes, fichier malformé, décodés par le pipeline de détourage | Plafonds de taille et de dimensions vérifiés **avant** décodage. Échec propre du job, pas d'arrêt du processus. |
| MEN-006 | **Fuite de secret** | Clé d'API ou identifiants sérialisés dans `project.json` puis partagés | Secrets exclusivement en variables d'environnement. Aucun champ de secret dans le modèle de projet. Test automatisé vérifiant l'absence de secret dans l'export. |
| MEN-007 | **Consommation de ressources** | Lot de génération de taille non bornée | Plafond configurable du nombre de candidats par lot. Annulation coopérative des jobs. |
| MEN-008 | **Exfiltration par lien symbolique à l'export** | Un lien symbolique déposé dans le dossier d'un projet et pointant hors de celui-ci — volume des journaux, dossier personnel, `/etc`. L'export le suit et le place dans une archive que l'utilisateur envoie lui-même | Ne jamais suivre un lien : résoudre le chemin absolu et vérifier le préfixe, comme MEN-001 à l'import. L'export **échoue** en nommant le lien plutôt que de l'ignorer. Doublé par la liste blanche de DEC-050, qui n'autorise que des `.png` et des `.pdf` référencés |
| MEN-009 | **Traversée de chemin par le nom de projet** | `name` est une chaîne libre, issue de l'utilisateur ou d'une archive tierce, et sert à fabriquer le nom du dossier de projet | Translittération vers une liste blanche de caractères, longueur bornée, noms réservés Windows exclus, points et espaces finaux interdits. Vérification que le chemin résolu est sous la racine des projets **avant** toute création. Jamais de concaténation directe, comme l'exige déjà MEN-002 |
 
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
 
Templates en fichiers, gabarits, catalogue éditable, clause sujet stockée et modifiable.
 
**Critères d'acceptation** : composition déterministe (même entrée, même sortie) ; les clauses style et cadrage sont inatteignables depuis l'interface de gabarit, et la signature de `IPromptComposer` le rend structurellement vrai ; le désalignement est correctement calculé après édition d'une clause sujet, d'un style ou d'un univers.
 
### T4 — Client générateur et production de couples
 
Client HTTP ComfyUI, substitution du template de workflow, génération jumelée, découpe.
 
**Critères d'acceptation** : générateur injoignable géré comme un état normal ; un lot interrompu conserve les candidats déjà produits ; l'image jumelée brute est conservée pour diagnostic.
 
### T5 — Détourage
 
Runtime ONNX, fournisseur d'exécution configurable, plafonds d'entrée.
 
**Critères d'acceptation** : MEN-005 couvert ; échec propre sur image malformée ; PNG de sortie à fond réellement transparent.
 
*Tranche à relire en profondeur (DEC-027).*
 
### T6 — API et interface
 
Points de terminaison, front React, galerie de candidats, validation, export, localisation complète.
 
**Critères d'acceptation** : aucune chaîne en dur ; bascule français/anglais sans rechargement ; capacité de page affichée ; codes d'erreur correctement traduits ; les candidats désalignés sont visuellement distingués des candidats sains ; la structure du chapitre 15 est respectée, y compris la liste du §15.5.
 
### T7 — Observabilité et durcissement
 
Serilog, visualiseur de journaux, rotation et rétention, revue complète du chapitre 9.
 
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
| **Projet** | Création, nom, univers, style, géométrie, format de papier. Les quatre derniers sont **modifiables après création** ; les modifier désaligne les candidats existants plutôt que d'être interdit ou précédé d'un avertissement (DEC-030, DEC-055). | T2 |
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
| **Optionnels** | clés du catalogue : arme, armure, vêtement, couleur… | **« non contraint »**, et non « absent de l'illustration » |
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
| **A** | **Parcours utilisateur.** La documentation décrit la mécanique, jamais l'usage : créer un projet, ajouter un gabarit, lancer un lot, comparer, élire, exporter. À écrire tôt — il révèle la moitié des règles de gestion sans avoir à les chercher. | T3 |
| **B** | **Machines à états.** *Partiellement résolue par DEC-030.* Restent à définir : ce que devient l'ancien élu quand on en élit un nouveau, et la machine à états du Job (en file, en cours, échoué, annulé). | T3 |
| **C** | **Règles de gestion.** *Partiellement résolue par DEC-030.* Restent ouvertes : export avec des gabarits sans candidat élu ? **Export avec un candidat élu mais désaligné — bloquer, avertir, ou passer outre ?** Quantité dépassant la capacité de page ? Suppression d'un gabarit dont un candidat est élu ? Groupe de taille sans élu : page vide ou ignorée ? Couplage entre le statut d'un candidat et la présence de ses fichiers ? | T3 |
| **D** | **Entité Catalogue.** DEC-024 prévoit des listes de valeurs éditables, le modèle de données n'en définit aucune. Global à l'application ou embarqué par projet ? Penchant : global. | T3 |
| **E** | **Contrat d'API.** Points de terminaison, verbes, charges utiles, liste complète des codes d'erreur. Le §C.11 du cahier T2 en fixe déjà dix : ils ne sont pas à réinventer, seulement à exposer. | T6 |
| **F** | **Schémas des fichiers externes.** Template de workflow ComfyUI et ses jetons, fichiers de templates de prompts par univers. Ce sont des contrats publics, et DEC-029 fait du premier le seul point d'accès à la clause de cadrage. | T4 |
| **G** | **Valeurs non fonctionnelles.** Délai d'attente d'une génération, plafond de candidats par lot, dimensions maximales en entrée, durée acceptable d'un détourage sur processeur. Sans chiffres, MEN-005 et MEN-007 ne sont pas implémentables. Précédent utile : DEC-057 pose que ces bornes **s'arbitrent** et ne se mesurent pas, et qu'elles vivent en paramètres tant qu'aucun hôte ne lit de fichier. | T5 |
| **H** | **Dépôt public ou privé.** *Visibilité toujours non confirmée.* Elle est citée par DEC-058, qui exclut la révision de source de la version pour ne pas publier d'identifiant de commit dans une archive — précaution qui vaut dans les deux cas, donc la question ne bloque rien. | Libre |
| **I** | **Loi de progression des hauteurs.** DEC-032 pose la contrainte — plafond d'environ 112 mm sur US Letter — mais pas les valeurs. Se tranche en T0b, tapis sous les yeux, les cinq tailles montées côte à côte. | T0b |

**Questions fermées depuis l'ouverture du chapitre**, conservées parce qu'une question refermée se repose sinon tous les trois mois :

| Sujet | Résolution |
|---|---|
| Éditabilité du prompt résolu | DEC-028 — seule la clause sujet est éditable |
| Exposition de la clause de cadrage | DEC-029 — jamais dans l'interface ; point d'extension par fichier |
| Verrouillage de `style`, `univers`, `geometrie`, `formatPapier` | DEC-030, confirmée par DEC-055 — plus aucun verrou, désalignement calculé |
| Granularité de la géométrie (projet ou export) | DEC-030 — paramètre de rendu, modifiable librement |
| Nommage et nombre de tailles | DEC-031 — cinq tailles nommées d'après les règles ; l'échelle S/M/L/XL/XXL est écartée |
| Origine des emprises de grille | Chapitre 14 — table sourcée, valeurs définitives |
| Origine des hauteurs de pion | DEC-032 — aucune source ne les définit ; c'est une décision bornée par le papier |
| Ordre T0 / T1, et prérequis de T0 | DEC-033 puis DEC-044 — T0a d'abord, T0b après le code de T1, puis reportée |
| Niveau de `gutterMm` et de `silhouetteMarginMm` | DEC-052 — ils restent dans la calibration |
| Identité d'un projet | DEC-047 — un `projectId` opaque ; le nom du dossier n'a aucune sémantique |

> **Un point mineur laissé de côté, qui n'a pas de fiche.** La demande initiale « on fait attention aux règles de l'OWASP » a été traitée par un modèle de menace déduit de l'architecture (chapitre 9) plutôt que par une checklist générique. C'est un arbitrage assumé. DEC-054 en montre la contrepartie : une menace ne se déduit que d'un code qui existe, donc le chapitre 9 se revoit **à chaque tranche** qui ouvre une surface, et non une seule fois en T7.

