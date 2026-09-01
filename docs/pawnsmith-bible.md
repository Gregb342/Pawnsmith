# Pawnsmith — Bible du projet
 
| | |
|---|---|
| **Nom de code** | Pawnsmith |
| **Version du document** | 0.7 |
| **Date** | 29 août 2026 |
| **Statut** | Brouillon — évolutif |
| **Porteur** | Grégoire |
| **Licence visée** | Open source, permissive (MIT recommandé) |
 
> **Comment lire ce document.** Il est vivant. Le chapitre 11 (journal des décisions) fait foi : quand une décision change, on ajoute une fiche, on ne réécrit pas l'ancienne. Les valeurs marquées `À CALIBRER` sont volontairement absentes tant que la tranche T0 n'a pas été menée — ne pas les inventer.
 
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
- Deux géométries de pion : tente pliée et pion à onglet avec socle.
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
| **Géométrie** | Mode de construction physique du pion. Uniforme pour tout le projet. |
| **Gabarit** | *Ce que l'utilisateur veut* : une créature définie par ses paramètres (race, classe, taille, équipement, détails), sa quantité, et sa clause sujet. Persistant et stable. |
| **Candidat** | *Ce que le modèle a produit* : une tentative concrète pour un gabarit, identifiée par une graine. Un gabarit peut avoir N candidats ; un seul est élu. |
| **Clause cadrage** | Segment de prompt constant, jamais exposé dans l'interface. Impose ce qui rend l'image découpable et détourable. Voir §4.1 et DEC-029. |
| **Clause sujet** | Segment de prompt propre au gabarit, produit par le composeur à partir de ses paramètres. **Seul segment éditable par l'utilisateur.** |
| **Clause style** | Segment de prompt propre au projet, appliqué identiquement à tous les gabarits. |
| **Prompt résolu** | Assemblage des trois clauses. Valeur **dérivée** : ni stockée sur le gabarit, ni éditable. |
| **Désaligné** | État **calculé** d'un candidat dont le `promptUtilise` figé diffère du prompt résolu que produirait le composeur aujourd'hui. Orthogonal au statut. Voir DEC-030. |
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
| `versionSchema` | entier | Obligatoire dès la v1. Permet la migration des projets anciens. |
| `nom` | texte | |
| `univers` | énumération | `Fantasy` en v1. Champ présent pour l'extension. Modifiable ; désaligne les candidats existants (DEC-030). |
| `style` | Style | Modifiable ; désaligne les candidats existants (DEC-030). |
| `geometrie` | énumération | `FoldedTent` \| `TabAndSocket`. Modifiable sans conséquence sur les candidats : paramètre de rendu. |
| `formatPapier` | FormatPapier | Référence vers une entrée du catalogue de formats. Modifiable sans conséquence sur les candidats. |
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
| `promptUtilise` | texte | Copie figée du prompt résolu au moment de la génération. Permet de comprendre a posteriori pourquoi un candidat diffère, et sert de base au calcul du désalignement. |
| `statut` | énumération | `Brouillon` \| `Valide` \| `Rejete` |
| `desaligne` | *(calculé)* | Vrai si `promptUtilise` diffère du prompt résolu actuel du gabarit. **Jamais persisté.** |
| `fichierJumelee` | chemin | Image brute contenant les deux vues, conservée pour diagnostic. |
| `fichierRectoDetoure` | chemin | PNG à fond transparent. |
| `fichierVersoDetoure` | chemin | PNG à fond transparent. |
| `genereLe` | horodatage | |
 
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
├── projet.json            # toutes les entités ci-dessus
├── images/
│   ├── {idCandidat}-jumelee.png
│   ├── {idCandidat}-recto.png
│   └── {idCandidat}-verso.png
└── exports/
    └── mon-projet-moyenne.pdf
```
 
Justification : versionnable, sauvegardable, diffable, et lisible dans plusieurs années même si l'application ne démarre plus.
 
Les valeurs dérivées — `promptResolu`, `desaligne` — ne sont **pas** sérialisées. Une valeur calculée qu'on persiste devient une valeur qui ment dès la première modification manquée.
 
L'export produit une archive ZIP de ce dossier. **L'archive ne contient jamais de secret, ni de journal.** Un projet doit pouvoir être partagé sans réflexion préalable — cette propriété est un invariant, pas une bonne pratique.
 
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
 
### 5.2 Les deux géométries
 
**Tente pliée** — les deux vues sont réunies par une ligne de pliage haute. Sous la ligne des pieds, des volets se replient vers l'extérieur, ou le pion tient en V inversé. Aucun socle nécessaire.
 
**Pion à onglet et socle** — même construction, mais un onglet rectangulaire dépasse sous la ligne des pieds et coulisse dans un socle du commerce.
 
La seule différence entre les deux est **ce qui est ajouté sous la ligne des pieds, et de combien la hauteur dépliée s'allonge**. Tout le reste est commun. L'abstraction correspondante est donc minimale : une fonction qui décrit l'appendice inférieur et la hauteur totale.
 
### 5.3 Règle de placement du verso
 
Pour les deux géométries : **le verso est placé au-dessus de la ligne de pliage, tourné à 180°.** Omettre la rotation produit un personnage tête en bas après pliage. Cette règle est vérifiée par un test unitaire.
 
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
 
public interface IProjectRepository
{
    Task<Project> LoadAsync(string path, CancellationToken ct);
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
| MEN-006 | **Fuite de secret** | Clé d'API ou identifiants sérialisés dans `projet.json` puis partagés | Secrets exclusivement en variables d'environnement. Aucun champ de secret dans le modèle de projet. Test automatisé vérifiant l'absence de secret dans l'export. |
| MEN-007 | **Consommation de ressources** | Lot de génération de taille non bornée | Plafond configurable du nombre de candidats par lot. Annulation coopérative des jobs. |
 
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

---
 
## 12. Découpage en tranches
 
Chaque tranche est livrable, testable, et se termine par une relecture intégrale.
 
Ordre effectif après DEC-033 : **Fondations → T0a → T1 (code) → T0b → T1 (validation) → T2 …**
 
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
 
Entités, sérialisation, chargement, sauvegarde, export et import d'archives.
 
**Critères d'acceptation** : aller-retour export/import sans perte ; MEN-001 couvert par un test avec archive malveillante ; MEN-006 couvert par un test vérifiant l'absence de secret dans l'export ; `versionSchema` présent ; **aucune valeur dérivée n'est sérialisée** (`promptResolu`, `desaligne`).
 
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
| **Projet** | Création, nom, univers, style, géométrie, format de papier. Les quatre derniers sont **verrouillés après création** (DEC-001, DEC-006, DEC-025). | T2 |
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
| **Libres** | `details`, puis le `promptResolu`, stocké et éditable | — |

**La formulation des optionnels est un travail d'interface à part entière, pas un choix de libellé.** Un utilisateur qui décoche « arme » croit demander un personnage désarmé ; il demande en réalité un personnage dont l'arme n'est pas imposée. C'est l'incompréhension la plus prévisible du produit, et elle se règle par les mots, pas par un champ de plus.

Ce qui **n'a rien à faire dans ce panneau** : l'univers, le style et la géométrie. Ils sont verrouillés au niveau du projet, et les afficher parmi les paramètres d'un gabarit invite exactement la dérive que DEC-006 existe pour empêcher. Changer de style implique de dupliquer le projet ; l'interface doit rendre cela évident plutôt que de le cacher derrière un champ modifiable.

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

**Reste ouvert, et c'est T2 que cela concerne, pas T6** : `gutterMm` doit-il devenir un réglage de projet ?

Les quatre valeurs du bloc `layout` de `calibration.json` n'ont pas la même nature, et les traiter en bloc est une erreur qui se paiera. `pageMarginMm` et `calibrationZoneHeightMm` sont des **propriétés de l'imprimante et de la planche** : elles se mesurent en T0b, elles ne se choisissent pas. `gutterMm` est une **préférence** — le §B.5.3 admet lui-même la valeur 0, qui « laisse moins de marge au ciseau » : c'est la dextérité de l'utilisateur, pas son matériel. `silhouetteMarginMm` est entre les deux.

Si `gutterMm` devient un réglage de projet, il rejoint l'entité `Projet` et donc le schéma de T2. T1 n'est pas concernée : elle continue de le lire dans la calibration, et le manifeste du §B.3 n'a pas à le porter.
