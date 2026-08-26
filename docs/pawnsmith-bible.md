# Pawnsmith — Bible du projet

| | |
|---|---|
| **Nom de code** | Pawnsmith |
| **Version du document** | 0.1 |
| **Date** | 26 août 2026 |
| **Statut** | Brouillon — évolutif |
| **Porteur** | Grégoire |
| **Licence visée** | Open source, permissive (MIT recommandé) |

> **Comment lire ce document.** Il est vivant. Le chapitre 11 (journal des décisions) fait foi : quand une décision change, on ajoute une fiche, on ne réécrit pas l'ancienne. Les valeurs marquées `À CALIBRER` sont volontairement absentes tant que la tranche T0 n'a pas été menée — ne pas les inventer.

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
- Export PDF, formats A4 et US Letter.
- Sauvegarde et rechargement de projets, export et import d'archives.
- Interface bilingue français / anglais.

### 1.4 Hors périmètre de la v1 (différé, voir chapitre 13)

- Fournisseur d'images distant (API en ligne).
- Composition de prompts assistée par modèle de langage.
- Univers autres que fantasy.
- Troisième géométrie (pièces séparées collées sur âme carton).
- Mélange de plusieurs tailles de pions sur une même page.
- Langues au-delà du français et de l'anglais.

### 1.5 Non-objectifs permanents

Ces points ne sont pas « plus tard », ils sont **hors sujet**. Les inscrire ici évite d'y revenir tous les trois mois.

- Pawnsmith ne fournit ni ne distribue de modèle de diffusion. Il fournit l'interface pour s'y brancher ; l'obtention, l'installation et la conformité du modèle relèvent de l'utilisateur.
- Pawnsmith n'est pas multi-utilisateur : pas de comptes, pas d'authentification, pas de cloisonnement. C'est une application mono-utilisateur auto-hébergée.
- Pawnsmith ne produit pas de modèles 3D et ne commande pas d'impression auprès d'un prestataire.
- Pawnsmith n'est pas un éditeur d'images. Le retouchage se fait ailleurs.

---

## 2. Glossaire

Ce vocabulaire est contraignant : il est repris tel quel dans le code, les noms de classes, l'interface et les messages de journalisation. Toute divergence est un défaut.

| Terme | Définition |
|---|---|
| **Projet** | Unité de travail persistante. Contient un style, une géométrie, un univers, un ensemble de gabarits, et produit une ou plusieurs planches. |
| **Univers** | Registre esthétique global du projet (fantasy en v1). Détermine quel jeu de templates de prompts est utilisé. |
| **Style** | Ensemble figé (rendu, palette, formulation littérale) verrouillé à la création du projet. Garantit la cohérence visuelle. |
| **Géométrie** | Mode de construction physique du pion. Verrouillée au niveau du projet. |
| **Gabarit** | *Ce que l'utilisateur veut* : une créature définie par ses paramètres (race, classe, taille, équipement, détails), sa quantité, et son prompt résolu. Persistant et stable. |
| **Candidat** | *Ce que le modèle a produit* : une tentative concrète pour un gabarit, identifiée par une graine. Un gabarit peut avoir N candidats ; un seul est élu. |
| **Couple recto/verso** | Paire d'images indissociable attachée à un candidat : la vue de face et la vue de dos du même personnage. La validation porte sur le couple, jamais sur une face isolée. |
| **Taille** | Emprise du pion sur la grille de jeu (Moyenne, Grande, Très Grande, Gigantesque). Sert de clé de regroupement en pages. |
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
| `univers` | énumération | `Fantasy` en v1. Champ présent pour l'extension. |
| `style` | Style | Verrouillé après création. |
| `geometrie` | énumération | `TentePliee` \| `PionASocle`. Verrouillée après création. |
| `formatPapier` | FormatPapier | Référence vers une entrée du catalogue de formats. |
| `gabarits` | liste de Gabarit | |
| `creeLe`, `modifieLe` | horodatage | |

**Style** — figé à la création.

| Champ | Type | Notes |
|---|---|---|
| `nom` | texte | |
| `clauseStyle` | texte | Chaîne littérale injectée dans chaque prompt. **Jamais réécrite, ni par l'utilisateur au niveau du gabarit, ni par un modèle de langage.** |
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
| `details` | texte | Champ libre, concaténé à la clause sujet. |
| `promptResolu` | texte | Produit par le composeur, **stocké et éditable**. Ne se régénère pas tout seul. |
| `quantite` | entier ≥ 1 | Nombre d'exemplaires du même pion sur la planche. |
| `candidats` | liste de Candidat | |
| `idCandidatElu` | identifiant nullable | |

**Candidat**

| Champ | Type | Notes |
|---|---|---|
| `id` | identifiant | |
| `graine` | entier | |
| `promptUtilise` | texte | Copie figée du prompt au moment de la génération. Permet de comprendre a posteriori pourquoi un candidat diffère. |
| `statut` | énumération | `Brouillon` \| `Valide` \| `Rejete` |
| `fichierJumelee` | chemin | Image brute contenant les deux vues, conservée pour diagnostic. |
| `fichierRectoDetoure` | chemin | PNG à fond transparent. |
| `fichierVersoDetoure` | chemin | PNG à fond transparent. |
| `genereLe` | horodatage | |

**Taille** — table de référence, valeurs par défaut surchargeables dans les réglages.

| Nom | Emprise grille | Largeur pion | Hauteur pion |
|---|---|---|---|
| Moyenne | 1 × 1 case | 25,4 mm | `À CALIBRER` |
| Grande | 2 × 2 cases | 50,8 mm | `À CALIBRER` |
| Très Grande | 3 × 3 cases | 76,2 mm | `À CALIBRER` |
| Gigantesque | 4 × 4 cases | 101,6 mm | `À CALIBRER` |

> **Piège à ne pas manquer** : l'emprise sur la grille et la hauteur visuelle du pion sont **deux dimensions indépendantes**. Un humanoïde de taille Moyenne occupe une case de 25,4 mm mais mesure environ le double en hauteur. Ne pas déduire l'une de l'autre.

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

L'export produit une archive ZIP de ce dossier. **L'archive ne contient jamais de secret, ni de journal.** Un projet doit pouvoir être partagé sans réflexion préalable — cette propriété est un invariant, pas une bonne pratique.

---

## 4. Pipeline de production

Chaque étape est un port distinct (chapitre 7). Le découplage est la propriété centrale du système : **la production d'images ne sait rien de la mise en page, et réciproquement.**

| # | Étape | Entrée | Sortie | Mode d'échec |
|---|---|---|---|---|
| 1 | Composition du prompt | Gabarit + Style + clause cadrage | Prompt résolu, stocké et éditable | Aucun (déterministe) |
| 2 | Génération jumelée | Prompt + graine | Une image contenant vue de face et vue de dos côte à côte | Générateur injoignable, délai dépassé, modèle en erreur |
| 3 | Découpe | Image jumelée | Deux images indépendantes | Partage vertical incorrect si le modèle n'a pas respecté le cadrage |
| 4 | Détourage | Deux images | Deux PNG à fond transparent | Image malformée, dimensions hors bornes |
| 5 | Validation | Couple recto/verso | Candidat élu | Aucun (action utilisateur) |
| 6 | Mise en page | Gabarits élus + format + géométrie | Modèle de planche (positions en mm) | Capacité de page dépassée |
| 7 | Rendu PDF | Modèle de planche | Fichier PDF | Écriture disque |

### 4.1 Clause de cadrage

L'étape 1 injecte une clause littérale non modifiable par l'utilisateur, qui impose : **planche de rotation, vue de face et vue de dos, corps entier, pieds au bord inférieur, fond uni, aucun élément coupé, ratio portrait**.

Cette clause n'est pas une préférence esthétique : c'est ce qui rend l'étape 3 découpable et l'étape 4 fiable. Un fond de forêt se détoure mal ; un personnage cadré à mi-cuisse est inutilisable.

### 4.2 Pourquoi la génération jumelée

Deux générations indépendantes du même personnage ne produisent pas le même personnage. Les modèles de diffusion n'ont pas de permanence d'objet : l'arme change de forme, la cape disparaît, la palette dérive. La cohérence recto/verso est **structurellement garantie** si les deux vues sont dessinées dans la même passe.

Contrepartie assumée : chaque vue n'occupe que la moitié de la résolution générée.

Cette étape est isolée derrière un port unique (`IPawnPairProducer`). Si la calibration T0 montre que le modèle local ne produit pas de planche de rotation exploitable, on substitue une implémentation dégradée — deux générations indépendantes à graine partagée — **sans toucher au reste du système**.

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

L'interface expose la capacité restante de la page courante — information réellement utile lors de la composition.

### 5.5 Repères d'impression (obligatoires, non désactivables en v1)

| Repère | Rôle |
|---|---|
| **Trait de calibration** | Segment de 100,0 mm exactement, légendé. Seule protection contre une impression hors échelle. |
| **Traits de coupe** | Contour de découpe, trait fin gris clair, invisible sur le pion découpé. |
| **Ligne de pliage** | Trait discontinu à la position du pli haut. |

Le texte porté sur la planche est localisé : la requête d'export transporte la langue voulue (chapitre 10).

### 5.6 Paramètres à calibrer (tranche T0)

Ces valeurs ne doivent **pas** être devinées. Elles sortent d'un tirage papier réel.

| Paramètre | Unité | Statut |
|---|---|---|
| Grammage papier retenu | g/m² | `À CALIBRER` |
| Facteur de correction d'échelle de l'imprimante | ratio | `À CALIBRER` |
| Hauteur du pion par taille | mm | `À CALIBRER` |
| Largeur et hauteur de l'onglet | mm | `À CALIBRER` |
| Cotes de l'encoche (ouverture / extrémité) | mm | `À CALIBRER` |
| Hauteur des volets de tente | mm | `À CALIBRER` |
| Marge de sécurité autour de la silhouette | mm | `À CALIBRER` |

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

ComfyUI n'accepte pas un prompt mais un **graphe de workflow JSON**. L'application stocke donc un *template de workflow* comportant des jetons nommés (`{{POSITIVE}}`, `{{NEGATIVE}}`, `{{SEED}}`, `{{WIDTH}}`, `{{HEIGHT}}`) qu'elle substitue avant envoi. Ce template est un **fichier de configuration**, pas du code : un utilisateur dont le workflow diffère peut l'adapter sans recompiler.

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

// Deux implémentations prévues ; une seule livrée en v1.
public interface IPromptComposer
{
    string Compose(Gabarit gabarit, Style style, Univers univers);
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

---

## 11. Journal des décisions

Format : contexte implicite, choix, conséquence. On n'édite pas une fiche : on en ajoute une nouvelle qui supersède.

**DEC-001 — Géométrie double, verrouillée au projet.**
Choix : `TentePliee` et `PionASocle`, paramétrables, mais fixées à la création du projet.
Conséquence : pas de mélange de géométries sur une planche ; l'abstraction se réduit à l'appendice sous la ligne des pieds.

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

**DEC-025 — Univers en paramètre global, fantasy seul en v1.**
Choix : le champ existe, un seul jeu de templates est livré.
Conséquence : extension future sans migration de schéma.

**DEC-026 — Nom de code : Pawnsmith.**
Choix : retenu contre *Simulacra* et *Standee*.
Conséquence : préfixe de tous les espaces de noms et nom du dépôt. Décision volontairement prise tôt.

**DEC-027 — Périmètre de compréhension du porteur.**
Choix : le code est produit par assistance, mais **intégralement relu**, tranche par tranche, avec compréhension approfondie de la couche ONNX.
Conséquence : contraint le style de code vers l'explicite (DEC-021) et impose un découpage en petites tâches relisibles (chapitre 12).

---

## 12. Découpage en tranches

Chaque tranche est livrable, testable, et se termine par une relecture intégrale.

### T0 — Calibration manuelle *(hors code)*

Produire à la main une planche complète : générer cinq images, détourer, monter, imprimer, découper, assembler, poser sur le tapis.

**Critères de sortie** : le tableau §5.6 est rempli avec des mesures réelles ; le test décisif de DEC-003 est fait (le modèle local produit-il une planche de rotation exploitable ?) ; un prompt de référence fonctionnel est consigné.

### T1 — Noyau de mise en page

Domaine pur plus rendu PDFsharp. Entrée : un dossier de PNG déjà détourés. Sortie : un PDF calibré. Ni IA, ni interface.

**Critères d'acceptation** : le trait de calibration mesure 100 mm ± 0,5 sur le papier ; les pions découpés tiennent dans leur socle ; le verso est à l'endroit après pliage ; le calcul de capacité est couvert par des tests unitaires.

*Cette tranche vient en premier parce qu'elle porte le risque physique et qu'elle est immédiatement vérifiable au ciseau.*

### T2 — Modèle de projet et persistance

Entités, sérialisation, chargement, sauvegarde, export et import d'archives.

**Critères d'acceptation** : aller-retour export/import sans perte ; MEN-001 couvert par un test avec archive malveillante ; MEN-006 couvert par un test vérifiant l'absence de secret dans l'export ; `versionSchema` présent.

### T3 — Composition de prompts et catalogue

Templates en fichiers, gabarits, catalogue éditable, prompt résolu stocké et modifiable.

**Critères d'acceptation** : composition déterministe (même entrée, même sortie) ; les clauses style et cadrage sont inatteignables depuis l'interface de gabarit.

### T4 — Client générateur et production de couples

Client HTTP ComfyUI, substitution du template de workflow, génération jumelée, découpe.

**Critères d'acceptation** : générateur injoignable géré comme un état normal ; un lot interrompu conserve les candidats déjà produits ; l'image jumelée brute est conservée pour diagnostic.

### T5 — Détourage

Runtime ONNX, fournisseur d'exécution configurable, plafonds d'entrée.

**Critères d'acceptation** : MEN-005 couvert ; échec propre sur image malformée ; PNG de sortie à fond réellement transparent.

*Tranche à relire en profondeur (DEC-027).*

### T6 — API et interface

Points de terminaison, front React, galerie de candidats, validation, export, localisation complète.

**Critères d'acceptation** : aucune chaîne en dur ; bascule français/anglais sans rechargement ; capacité de page affichée ; codes d'erreur correctement traduits.

### T7 — Observabilité et durcissement

Serilog, visualiseur de journaux, rotation et rétention, revue complète du chapitre 9.

**Critères d'acceptation** : chaque menace MEN-001 à MEN-007 est soit couverte par un test, soit explicitement documentée comme risque accepté.

> Les tranches T2 à T5 n'ont pas d'interface. Elles s'éprouvent par tests d'intégration et, si nécessaire, par un point d'entrée en ligne de commande minimal — jetable, non livré.

---

## 13. Évolutions différées

À reprendre une fois la v1 fonctionnelle et réellement utilisée. Rien ici ne doit être anticipé dans le code au-delà des points d'extension déjà prévus.

| Réf. | Évolution | Point d'extension déjà en place |
|---|---|---|
| EVO-001 | **Composition de prompt par modèle de langage.** Second adaptateur de `IPromptComposer`, appelant un point de terminaison compatible OpenAI (Ollama, LM Studio). Ne réécrit **que la clause sujet** ; les clauses style et cadrage lui restent inaccessibles. Repli silencieux sur le template si injoignable. Séquencer le chargement des modèles pour éviter la contention VRAM. | `IPromptComposer` |
| EVO-002 | **Fournisseur d'images distant.** Second adaptateur de `IImageGenerator`. Introduit un compteur de coût, une confirmation avant lot et un cache prompt+graine. | `IImageGenerator` |
| EVO-003 | **Troisième géométrie** : deux pièces séparées, collées entre elles ou sur une âme carton, avec repères d'alignement en croix. Considérée pour l'instant comme une variante du pion à socle. | Fonction de placement |
| EVO-004 | **Univers supplémentaires** (steampunk, science-fiction, contemporain). | Champ `univers` + fichiers de templates |
| EVO-005 | **Mélange de tailles sur une page**, par shelf packing. | Moteur de mise en page |
| EVO-006 | **Validation en lot** en complément de la validation unitaire. | Interface |
| EVO-007 | **Langues supplémentaires.** | Fichiers de ressources |
| EVO-008 | **Déploiement distribué** : application sur une machine sobre, générateur sur le poste équipé. Sans impact sur la conception — le client est déjà HTTP. À reprendre uniquement si la charge locale devient un problème. | Aucun |
| EVO-009 | **Passage par la 3D** pour la cohérence recto/verso, si DEC-003 déçoit à l'usage : image → modèle 3D → deux rendus orthographiques. | `IPawnPairProducer` |
