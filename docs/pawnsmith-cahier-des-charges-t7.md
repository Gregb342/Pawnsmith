# Pawnsmith — Cahier des charges : tranche T7 (observabilité et durcissement)

| | |
|---|---|
| **Version** | 1.1 |
| **Date** | 3 octobre 2026 |
| **Document parent** | `pawnsmith-bible.md` v0.17 — chapitres 8, 9, 11 et 12 en particulier |
| **Documents frères** | les cahiers T1 à T4 et T6 (parties B à E, et G), dont ce document reprend la forme |
| **Portée** | La journalisation, le visualiseur de journaux **côté API**, et la revue complète du chapitre 9. Pas le front |

> **Changements depuis la v1.0** — Écriture de la tranche et sa relecture. Le pont de Serilog ajoutait un filtre qui laissait passer tous les niveaux vers les fichiers, au mépris de `Logging:LogLevel` ; le fournisseur est enregistré à la main (H.3.2). Deux défauts de T4 et T6 corrigés, que la journalisation rendait réels : l'adresse d'un générateur dont le workflow est refusé était gardée sans vérification, identifiants compris ; et les refus d'adresse répétaient le texte brut, secrets compris (H.4.1). Trois risques acceptés de plus, écrits en H.4.4 et H.7.1 : un refus de ComfyUI peut citer un fragment de prompt ; un lien dur ou un échange de fichier dans le dossier des journaux ; les droits des fichiers de journal. Un `%00` dans un nom de journal est refusé par Kestrel lui-même, avant toute route.

> **Régime d'écriture.** Comme T4 et T6, ce document a été écrit et tranché **sans arbitrage du porteur**. Chaque décision est une fiche du chapitre 11 (DEC-090 à DEC-097), reprise au §8 de `CLAUDE.md` sous « Décisions prises sans toi ».

> **Comment lire ce document.** Le chapitre 12 de la bible donne à T7 trois sujets : Serilog, le visualiseur de journaux, la revue du chapitre 9. Les deux premiers sont du code neuf (H.2 à H.6) ; le troisième est un **inventaire**, menace par menace, de ce qui la tient — un test, ou un risque accepté et écrit (H.7). L'inventaire a trouvé un trou réel, MEN-005 sur la planche, qui est bouché ici (H.7.2).
>
> Les sections sont numérotées **H.x** : la lettre F reste réservée à T5, qui n'est pas écrite.

---

## H.0 Consignes de travail

Celles des tranches précédentes s'appliquent. Trois avec une force particulière.

- **Seuls les bords journalisent.** Le domaine et l'Application n'écrivent aucun journal et ne connaissent aucune bibliothèque de journalisation. Un cas d'usage qui échoue le dit par un code et un message, que le bord — le travailleur des lots, l'intergiciel d'erreurs, le démarrage — écrit au journal (H.2).
- **Un journal est une surface.** Il contient des chemins absolus et l'adresse du générateur ; il sera lu par un visualiseur dont le nom de fichier vient d'une URL (MEN-002). Il se traite comme une donnée sensible : jamais dans un projet, jamais dans une réponse d'erreur, jamais lu par concaténation.
- **Une menace sans test n'est pas couverte.** La revue du chapitre 9 ne se contente pas d'une phrase : chaque ligne nomme le test qui la tient, ou écrit le risque accepté et pourquoi (H.7).

---

## H.1 Objectif et périmètre

**Entrée** : les événements de l'application — démarrage, erreurs, lots.
**Sortie** : des fichiers de journal JSON, tournants et bornés ; deux routes de lecture ; un chapitre 9 dont chaque ligne est tenue.

### Dans le périmètre

- La **journalisation** : Serilog, sortie JSON, rotation, rétention, désactivation (H.2, H.3).
- **Ce qui est journalisé** : démarrage, erreurs de requête, cycle de vie des lots avec l'identifiant de job (H.4).
- L'**avertissement de MEN-004** au démarrage (H.5).
- Le **visualiseur côté API** : lister les fichiers, en lire la fin (H.6).
- La **revue du chapitre 9**, MEN-001 à MEN-010, et MEN-011 qu'elle fait naître (H.7).

### Hors périmètre

- **L'écran du visualiseur** : c'est le front de T6, qui n'est pas commencé. L'API en donne la matière.
- **Graylog** : le chapitre 8 demande un format branchable sans travail de parsing ; le brancher est une décision de déploiement, pas une tranche.
- **T5** : son critère MEN-005 (le détourage décode des images) reste le sien.

---

## H.2 Qui journalise, et avec quoi

### H.2.1 Les bords seulement

Le domaine ne référence rien (§A.3). L'Application ne référence que le domaine. Leur faire journaliser demanderait une dépendance de plus à chacune — une abstraction de journalisation au minimum —, pour écrire des choses qu'un code d'erreur et un message disent déjà. **Ce qui échoue remonte, et le bord l'écrit.** Trois bords :

| Bord | Ce qu'il écrit |
|---|---|
| Le démarrage (`ApiHost`) | Version, dossiers, état du générateur et la raison d'un refus, adresses d'écoute (H.4.1, H.5) |
| L'intergiciel d'erreurs | Le code **et le message** de toute erreur de requête — le message que DEC-084 refuse à la réponse (H.4.2) |
| Le travailleur des lots | Début et fin de chaque lot, avec l'identifiant du job (H.4.3) |

### H.2.2 Serilog, branché sur l'interface standard

L'API écrit par `ILogger<T>`, l'interface de journalisation d'ASP.NET — celle que le cadre utilise déjà pour ses propres événements. **Serilog est le seul puits** : un fournisseur de `ILogger` le relie à cette interface, de sorte que les événements de l'application et ceux du cadre arrivent dans le même fichier, au même format.

Trois paquets, tous Apache-2.0, licence vérifiée dans le dépôt de chacun et dans le `.nuspec` publié :

| Paquet | Où | Pourquoi |
|---|---|---|
| `Serilog` 4.4.0 | `Infrastructure` | Le cœur : le journal, l'enrichissement par contexte, et le formateur JSON |
| `Serilog.Sinks.File` 7.0.0 | `Infrastructure` | L'écriture en fichier, la rotation, la rétention |
| `Serilog.Extensions.Logging` 10.0.0 | `Api` | Le pont entre `ILogger<T>` et Serilog |

Deux paquets **écartés**, parce que vingt lignes ou rien les remplacent : `Serilog.Formatting.Compact` (le cœur de Serilog contient déjà un formateur JSON, H.3.1) et `Serilog.AspNetCore` (qui amène la console, la lecture de configuration par réflexion et le débogage, dont aucun n'est demandé).

La configuration de Serilog vit dans `Infrastructure/Logging` — le chapitre 7 y range Serilog. L'API l'appelle et la branche.

### H.2.3 L'identifiant de job, poussé une fois

Le chapitre 8 : l'identifiant d'un job est « poussé une seule fois en entrée du cas d'usage via `LogContext.PushProperty` ». L'Application ne pouvant pas voir Serilog, **l'entrée du cas d'usage est son point d'appel** : le travailleur pousse `JobId` autour de l'appel à `CandidateGeneration.RunAsync`, et tout événement écrit pendant le lot — par lui, ou par un adaptateur d'infrastructure appelé au-dessous — le porte. `LogContext` repose sur un contexte asynchrone : la propriété suit l'exécution, elle n'est jamais passée en paramètre.

À consigner en **DEC-090**.

---

## H.3 Les fichiers

### H.3.1 Le format

**Un objet JSON par ligne**, produit par le formateur JSON du cœur de Serilog, avec le message rendu. Chaque événement porte son horodatage, son niveau, son modèle de message, ses propriétés, et l'exception le cas échéant.

Les fichiers portent l'extension **`.ndjson`** (*newline-delimited JSON*) et non `.json` : un fichier entier n'est pas un document JSON, c'en est une suite. Un outil qui voit `.json` tente de lire le fichier d'un bloc et échoue.

### H.3.2 Rotation, rétention, désactivation

| Clé | Défaut | Rôle |
|---|---|---|
| `Pawnsmith:Logs:Enabled` | `true` | `false` : **aucun fichier n'est écrit**, aucun dossier créé |
| `Pawnsmith:Logs:Directory` | `data/logs` | Relatif au dossier de l'application ; en conteneur, le volume `/app/data/logs` (DEC-022) |
| `Pawnsmith:Logs:RetainedFileCount` | `31` | Nombre de fichiers gardés ; les plus anciens sont supprimés |
| `Pawnsmith:Logs:FileSizeLimitBytes` | 50 Mio | Taille d'un fichier ; atteinte, le journal passe au fichier suivant |

- **Un fichier par jour** : `pawnsmith-20261003.ndjson`. Un jour qui dépasse la taille d'un fichier continue dans `pawnsmith-20261003_001.ndjson`, puis `_002`… Sans ce passage, le comportement par défaut du puits est de **cesser d'écrire** une fois la taille atteinte — un journal qui se tait le jour où il se passe quelque chose.
- **La rétention compte les fichiers**, comme le chapitre 8 le demande. Le volume est donc borné par construction : au plus 31 × 50 Mio, soit environ 1,5 Gio. Un jour bavard peut évincer des jours plus anciens ; c'est le prix d'une borne qui tient.
- **Désactiver coupe les fichiers, pas la console.** La sortie console d'ASP.NET reste réglée par la clé standard `Logging:Console:LogLevel:Default`, que tout opérateur .NET connaît ; `None` la coupe. Une console n'est pas un journal sur disque de l'application, et le conteneur en gère la rétention lui-même.
- Le **niveau** est celui d'ASP.NET, `Logging:LogLevel` : un seul réglage pour la console et les fichiers. Par défaut `Information`, `Microsoft.AspNetCore` à `Warning`. Le fournisseur Serilog est donc enregistré **à la main** : l'extension `AddSerilog` du paquet y ajoute un filtre propre à son fournisseur, qui l'emporte sur `Logging:LogLevel` et laissait passer tous les niveaux.

Ces valeurs **s'arbitrent**, elles ne se mesurent pas (comme celles de DEC-051) : la règle des valeurs physiques ne s'y applique pas.

À consigner en **DEC-091**.

---

## H.4 Ce qui est journalisé

### H.4.1 Le démarrage

Une ligne `Information` qui dit ce que le processus a lu : version, racine des projets, dossier de configuration, dossier des journaux, état du générateur et son adresse. Un générateur **mal configuré** y ajoute une ligne `Warning` portant le code **et le message** du refus — le message que `GET /api/generator` ne rend pas.

**Ce message ne répète jamais un secret.** Un refus d'adresse ne cite au plus que `schéma://hôte:port/chemin`, sans identifiants, requête ni fragment, et rien du tout quand le texte n'a pas pu être lu comme une adresse. Une adresse n'est montrée — par `GET /api/generator` comme au journal — que si elle passe elle-même les règles de DEC-081, y compris quand c'est le workflow qui a été refusé et que l'adresse n'a jamais été examinée.

Une calibration, un catalogue ou un template illisible empêche de démarrer (§G.2.2). Ce refus est écrit au journal, niveau `Fatal`, **avant** que le processus ne s'arrête : c'est la ligne qu'on vient chercher quand le conteneur redémarre en boucle.

### H.4.2 Les erreurs de requête

| Erreur | Niveau | Contenu |
|---|---|---|
| Une exception porteuse d'un code (`ICodedException`), ou traduite en code par l'intergiciel | `Warning` | Méthode, chemin, code, statut, **message** |
| Une exception sans code (`INTERNAL_ERROR`) | `Error` | Méthode, chemin, et l'exception entière, pile comprise |

Le **chemin** de la requête est journalisé, pas sa **requête** (`?…`) ni son corps : un corps porte du texte d'utilisateur, et un journal n'a pas à en garder une copie.

### H.4.3 Les lots

| Événement | Niveau | Contenu |
|---|---|---|
| Le lot commence | `Information` | `JobId`, dossier, gabarit, nombre demandé |
| Le lot finit `Completed` ou `Cancelled` | `Information` | `JobId`, état, nombre produit |
| Le lot finit `Failed` | `Warning` | `JobId`, nombre produit, **code et message** de l'échec |
| Une exception échappe au cas d'usage (ne devrait pas arriver, §G.7.1) | `Error` | `JobId`, l'exception |

### H.4.4 Ce qui n'est jamais journalisé exprès

**Ni prompt, ni clause, ni paramètre, ni corps de requête.** Le chapitre 8 cite les prompts parmi ce qu'un journal contient ; c'était une prudence — un journal *peut* en contenir —, pas un programme. Le candidat fige déjà ses trois clauses dans `project.json` (DEC-049) : un prompt au journal serait une seconde copie de texte d'utilisateur, dans un endroit plus difficile à effacer, sans rien apprendre de plus. Les journaux portent des **identifiants**, des **codes** et des **messages d'erreur** — qui, eux, peuvent nommer un dossier de projet, donc le nom que l'utilisateur lui a donné.

**Une exception connue, acceptée.** Un refus de ComfyUI cite le début de sa réponse d'erreur (§E.7), et ComfyUI peut y reprendre une valeur du workflow soumis : un fragment de prompt peut donc atteindre le journal par un lot en échec. Ce n'est pas exprès, c'est la seule voie connue, et filtrer le texte d'un tiers serait plus fragile que l'admettre.

À consigner en **DEC-092**.

---

## H.5 MEN-004 : l'avertissement au démarrage

MEN-004 demande un « avertissement au démarrage si l'écoute n'est pas locale ». Une fois le serveur démarré, chaque adresse d'écoute est examinée ; **une adresse qui n'est pas de boucle locale** produit une ligne `Warning` qui la nomme.

Est de boucle locale : `localhost`, toute adresse IPv4 en `127.0.0.0/8`, et `::1`. Ne l'est pas : `0.0.0.0`, `[::]`, `+`, `*`, toute autre adresse ou tout autre nom.

**Le cas du conteneur, honnêtement.** Dans un conteneur, l'application écoute **forcément** sur toutes les interfaces : c'est la seule façon pour Docker de lui faire parvenir les connexions. Le conteneur ne voit pas comment son port est publié sur l'hôte — `-p 127.0.0.1:8080:8080` ou `-p 8080:8080` lui sont indiscernables. L'avertissement y est donc **toujours** émis, et son texte le dit : il rappelle la forme canonique et demande de vérifier la publication. Une ligne par démarrage, qui dit vrai, vaut mieux qu'un silence qui suppose. Le conteneur se reconnaît à la variable `DOTNET_RUNNING_IN_CONTAINER`, que posent les images officielles de .NET.

À consigner en **DEC-093**.

---

## H.6 Le visualiseur, côté API

### H.6.1 Les routes

| Route | Réponse |
|---|---|
| `GET /api/logs` | `{ "enabled": bool, "files": [ { "name", "sizeBytes", "lastWriteUtc" } ] }`, du plus récent au plus ancien |
| `GET /api/logs/{name}?lines=N` | `{ "name", "lines": [ "…" ], "truncated": bool }` — les `N` dernières lignes, dans l'ordre du fichier |

- `lines` vaut 500 par défaut, de 1 à 5 000 ; hors de ces bornes, `400 REQUEST_INVALID`.
- La lecture part de la fin et ne lit **jamais plus de 4 Mio**, quelle que soit la taille du fichier. `truncated` est vrai quand le fichier contient plus que ce qui est rendu.
- Chaque ligne est rendue **comme une chaîne**, telle qu'elle est dans le fichier : l'API ne la ré-interprète pas, ne la réécrit pas, ne la filtre pas. Le front la lira comme du JSON.
- Une **dernière ligne inachevée** — sans fin de ligne, parce que le journal est en train de l'écrire — n'est pas rendue.
- `enabled` dit si la journalisation est active. Les fichiers déjà présents restent lisibles quand elle ne l'est pas : couper le journal n'efface pas l'historique.

### H.6.2 MEN-002 : la liste blanche

**Le nom venu de l'URL ne sert jamais à fabriquer un chemin.** Le visualiseur énumère le dossier des journaux ; il ne retient que les fichiers

1. dont le nom correspond exactement à `pawnsmith-AAAAMMJJ.ndjson` ou `pawnsmith-AAAAMMJJ_NNN.ndjson` ;
2. qui sont des **fichiers ordinaires** — un lien symbolique est écarté même s'il porte un nom valide, comme MEN-008 l'exige à l'export.

Le nom demandé doit être **égal, en ordinal**, à l'un des noms retenus ; c'est le chemin **de l'énumération** qui est ouvert, jamais une concaténation du nom reçu. Tout autre nom — inconnu, hors motif, `..`, chemin absolu, lien — rend **`404 LOG_NOT_FOUND`**, sans distinction : dire qu'un fichier existe mais n'est pas lisible renseignerait sur le disque.

Le fichier en cours est ouvert en lecture **avec partage d'écriture** : le journal continue d'écrire pendant qu'on le lit.

### H.6.3 Un code de plus

| Code | Statut | Sens |
|---|---|---|
| `LOG_NOT_FOUND` | `404` | Aucun journal de ce nom dans la liste blanche |

### H.6.4 Pourquoi l'API lit directement l'infrastructure

Le visualiseur n'a aucune règle de gestion : lister un dossier, lire une fin de fichier. Un port dans l'Application serait une abstraction introduite « au cas où », ce que §2 de `CLAUDE.md` refuse. L'API référence l'infrastructure (A.3) et appelle `LogDirectory` directement, comme elle appelle déjà les lecteurs de configuration au démarrage.

À consigner en **DEC-094**.

---

## H.7 La revue du chapitre 9

### H.7.1 L'inventaire

Le critère d'acceptation de T7 au chapitre 12 : « chaque menace MEN-001 à MEN-007 est soit couverte par un test, soit explicitement documentée comme risque accepté ». La revue est étendue à **toutes** les lignes du chapitre, MEN-008 à MEN-010 comprises, et à MEN-011 qu'elle fait naître (H.7.3).

| Menace | Tenue par | Risque accepté |
|---|---|---|
| **MEN-001** Zip slip | `ArchiveInspectorTests` (entrées `../`, absolues, doublons de casse), `ProjectImageFilesTests` | Les deux contrôles doublés de DEC-051 à l'extraction sont inatteignables tant que l'inspection refuse les doublons ; gardés parce que la fiche les demande |
| **MEN-002** Traversée, visualiseur | `LogRoutesTests` (H.8, n° 11 à 14) ; et la même règle pour les images (`BlueprintRoutesTests`, DEC-088) | Qui peut écrire dans le dossier des journaux peut y déposer un **lien dur**, ou échanger un fichier entre l'énumération et l'ouverture : seuls les liens symboliques sont écartés. Écrire dans ce dossier, c'est déjà tenir la machine. Les fichiers prennent les droits du processus, comme les projets |
| **MEN-003** SSRF | `GeneratorCheckTests` (adresse refusée : schéma, identifiants, requête, fragment ; redirection non suivie ; un refus ne répète aucun secret) ; `StartupLogTests` (adresse à identifiants ni montrée ni journalisée) | Hypothèse de déploiement en réseau de confiance, documentée (DEC-081) : l'opérateur choisit l'adresse |
| **MEN-004** Exposition réseau | `StartupLogTests` (H.8, n° 8 et 9) ; forme canonique dans le `Dockerfile` et le README | **En conteneur, l'application ne peut pas vérifier la publication de son port** (H.5) : l'avertissement est toujours émis, et c'est à l'opérateur de vérifier |
| **MEN-005** Image non fiable | Génération : `GeneratorGenerationTests` (taille et dimensions bornées avant écriture). Planche : `FileImageSizeReaderTests` (H.8, n° 18) — **trou bouché par T7**, H.7.2. Archive : bornes de C.9.3 | Le décodage du détourage est **T5**, et son critère reste le sien. Une planche de nombreuses images, chacune à la borne, tient en mémoire autant d'images décodées (H.7.2) |
| **MEN-006** Fuite de secret | `ProjectExporterTests` (liste blanche **exhaustive**, DEC-050) ; aucun champ de secret dans le modèle | — |
| **MEN-007** Ressources | `CandidateGenerationTests` et `JobRoutesTests` (plafond du lot avant la file, annulation) ; `JobRoutesTests` et `ArchiveRoutesTests` (corps de requête bornés à la réception, `413`) | — |
| **MEN-008** Lien symbolique, export | `ProjectExporterTests`, `PairedImageWriteTests` ; et le visualiseur (H.8, n° 14) | **Sous Windows sans privilège de lien, le test est dégradé** ; la CI Ubuntu certifie la menace |
| **MEN-009** Nom de projet | `ProjectFolderNameTests`, `ProjectCreatorTests`, `ProjectImporterTests` | — |
| **MEN-010** Intersite, rebinding | `HostTests` (origine étrangère refusée, `Host` étranger refusé, aucun en-tête CORS) | Un utilisateur qui ajoute son nom d'hôte à `AllowedHosts` publie l'application en connaissance de cause |
| **MEN-011** Falsification de journal | `LogFileTests` (H.8, n° 17) | — |

Trois risques déjà connus de T2, sans menace numérotée, restent écrits au §8 de `CLAUDE.md` et ne sont pas rouverts : l'atomicité de l'échange de fichier n'est pas éprouvable par un test unitaire ; une entrée d'archive qui sous-déclare sa taille arrive tronquée ; la branche Zip64 n'est pas exercée.

### H.7.2 MEN-005 sur la planche : le trou

**Ce qui se passe.** Le rendu de la planche fait décoder par PDFsharp l'image de chaque élu : un PNG doit être décompressé pour être réécrit dans un PDF. La taille qu'il occupe une fois décodé se lit dans son en-tête — largeur × hauteur × 4 octets. Une archive importée peut porter un PNG de quelques kilo-octets annonçant 60 000 × 60 000 pixels : quatorze gigaoctets à décoder. Depuis T6, ce décodage est **à une requête HTTP** de distance (`GET …/sheet.pdf`).

`FileImageSizeReader` lit déjà l'en-tête de chaque image **avant** le rendu, pour l'échelle (B.6) ; son commentaire renvoyait les plafonds à T5. T5 n'est pas écrite, et la planche décode aujourd'hui.

**La contre-mesure.** Le lecteur refuse une image dont **un côté dépasse 8 192 pixels**, par `SHEET_INPUT_INVALID`, avant tout décodage. La valeur n'est pas nouvelle : c'est `MaxImageDimensionPx` de T4 (§E.8), la borne de ce que le générateur a le droit d'écrire. Une image légitime — un couple généré coupé en deux, puis détouré à dimensions égales — ne la dépasse jamais.

**Le reste du risque.** Une image à la borne pèse 256 Mio décodée, et une planche peut en porter plusieurs. Le borner aussi demanderait un plafond de la planche entière, que rien ne fonde aujourd'hui ; c'est écrit comme risque accepté, à revoir si T5 change la taille des images.

À consigner en **DEC-095**.

### H.7.3 MEN-011 : falsifier un journal

**Le vecteur.** Un journal texte écrit une ligne par événement ; un message qui contient un saut de ligne en fabrique une seconde, qui ressemble à un événement que l'application n'a jamais écrit. Les messages de ce code nomment des dossiers de projet, et le nom d'un projet est un texte libre — y compris venu d'une archive tierce.

**La contre-mesure est structurelle.** Le format est du JSON : toute valeur est une chaîne JSON échappée, un saut de ligne y devient `\n`, et un événement occupe toujours **exactement une ligne**. Le visualiseur rend chaque ligne comme une chaîne, sans l'interpréter. Un test journalise un message portant un saut de ligne et un faux événement, et vérifie qu'une seule ligne est écrite.

À consigner en **DEC-096**, qui ajoute MEN-011 au chapitre 9.

### H.7.4 La règle de revue

DEC-054 l'avait constaté : une menace se déduit d'un code qui existe, donc le chapitre 9 se revoit **à chaque tranche**. T7 ne clôt pas ce travail, elle le fait une fois en entier et en laisse la forme : un tableau où chaque ligne nomme son test ou son risque accepté. **T5 et le front de T6 rouvriront le tableau** — T5 pour MEN-005, le front pour MEN-010 côté navigateur.

À consigner en **DEC-097**.

---

## H.8 Tests attendus

Les tests d'API démarrent l'hôte réel, comme en T6, et **lisent les fichiers de journal** du dossier temporaire du harnais. Aucun faux journal : le puits de fichier est éprouvé tel qu'il tourne.

| # | Ce qu'il vérifie |
|---|---|
| 1 | Un fichier `pawnsmith-AAAAMMJJ.ndjson` est écrit dans le dossier configuré ; chaque ligne est un objet JSON |
| 2 | Désactivée, la journalisation n'écrit aucun fichier et ne crée aucun dossier |
| 3 | La rétention garde au plus le nombre de fichiers configuré (éprouvée par passage de taille) |
| 4 | Une erreur codée écrit code et message au journal, et la réponse ne porte que le code |
| 5 | Une erreur sans code écrit l'exception au niveau `Error` |
| 6 | Chaque événement d'un lot porte son `JobId` ; un événement hors lot n'en porte pas |
| 7 | Un lot en échec écrit son code et son message |
| 8 | Classement des adresses d'écoute : boucle locale ou non |
| 9 | Démarrée sur `127.0.0.1`, aucune alerte ; sur `0.0.0.0`, une alerte qui nomme l'adresse |
| 10 | Le démarrage écrit version, dossiers et état du générateur ; un générateur mal configuré écrit le message du refus |
| 11 | La liste ne contient que les noms du motif, du plus récent au plus ancien |
| 12 | Un fichier hors motif, présent dans le dossier, n'est ni listé ni lisible : `404 LOG_NOT_FOUND` |
| 13 | Un nom de traversée — `..`, encodé, absolu — rend `404` |
| 14 | Un lien symbolique au nom valide est écarté |
| 15 | La lecture rend les `N` dernières lignes et `truncated` ; `lines` hors bornes rend `400` |
| 16 | Une dernière ligne inachevée n'est pas rendue ; le fichier en cours d'écriture se lit |
| 17 | MEN-011 : un message portant un saut de ligne et un faux événement occupe une seule ligne |
| 18 | MEN-005 : une image de plus de 8 192 pixels de côté est refusée sur son en-tête ; 8 192 passe |
| 19 | La planche d'un projet dont l'élu dépasse la borne rend `422 SHEET_INPUT_INVALID` |
| 20 | Un démarrage impossible (calibration illisible) écrit une ligne `Fatal` |

S'y ajoutent, nés de l'écriture : les niveaux `Debug` et `Verbose` absents des fichiers au réglage par défaut ; le refus d'une rétention ou d'une taille nulles par le nom du réglage ; la liste quand la journalisation est coupée ; un `%00` refusé par le serveur ; un fichier plus grand que la fenêtre de lecture, qui ne rend que des lignes entières ; une adresse à identifiants ni montrée ni journalisée ; et six adresses refusées dont aucun refus ne répète le secret.

---

## H.9 Critères d'acceptation

T7 est terminée quand :

- [ ] Les **tests de H.8** passent.
- [ ] Les journaux sont du JSON, une ligne par événement, dans un dossier qui n'est **jamais** celui d'un projet.
- [ ] La rétention borne le volume ; la journalisation se coupe par configuration.
- [ ] Chaque événement d'un lot porte l'identifiant du job, poussé une seule fois.
- [ ] Le visualiseur ne lit qu'une liste blanche, et jamais par concaténation (MEN-002).
- [ ] **Chaque ligne du chapitre 9 nomme son test ou son risque accepté** (H.7.1).
- [ ] Trois dépendances ajoutées, justifiées dans le commit qui les introduit, et inscrites à `THIRD-PARTY-NOTICES.md` dans le même commit.
- [ ] Les fiches DEC de H.11 sont écrites ; MEN-011 est au chapitre 9.

---

## H.10 Ce que cette tranche ne fait pas

| T7… | Parce que |
|---|---|
| …ne dessine pas le visualiseur | Front de T6 |
| …n'envoie rien à Graylog | Décision de déploiement ; le format y est prêt |
| …ne journalise pas le domaine ni l'Application | H.2.1 |
| …ne journalise pas les prompts | H.4.4 |
| …ne borne pas la mémoire d'une planche entière | H.7.2, risque accepté |
| …ne touche pas au détourage | T5 |

---

## H.11 Décisions à consigner dans la bible

| Réf. | Objet | Section |
|---|---|---|
| **DEC-090** | Seuls les bords journalisent ; Serilog derrière `ILogger<T>` ; `JobId` poussé au point d'appel du cas d'usage. Trois paquets | H.2 |
| **DEC-091** | Une ligne JSON par événement, `.ndjson`, un fichier par jour et par taille, rétention par nombre de fichiers ; la désactivation coupe les fichiers | H.3 |
| **DEC-092** | Ce qui est journalisé ; ni prompt, ni clause, ni corps de requête | H.4 |
| **DEC-093** | MEN-004 : l'avertissement, et ce qu'un conteneur ne peut pas savoir | H.5 |
| **DEC-094** | Le visualiseur : deux routes, liste blanche par énumération, lecture bornée par la fin, `LOG_NOT_FOUND` | H.6 |
| **DEC-095** | MEN-005 sur la planche : 8 192 pixels de côté, lus sur l'en-tête | H.7.2 |
| **DEC-096** | MEN-011 : falsification de journal, tenue par le format | H.7.3 |
| **DEC-097** | La revue du chapitre 9 : chaque ligne nomme son test ou son risque ; le tableau se rouvre à chaque tranche | H.7 |

---

## H.12 Découpage en tâches

| # | Tâche |
|---|---|
| 1 | Journalisation : `LogOptions`, construction du journal Serilog, branchement dans l'hôte ; trois paquets, `THIRD-PARTY-NOTICES.md` ; `<Version>0.8.0</Version>` (tests 1 à 3, 17, 20) |
| 2 | Ce qui est journalisé : démarrage, erreurs de requête, lots et `JobId` ; avertissement de MEN-004 (tests 4 à 10) |
| 3 | Visualiseur : `LogDirectory`, deux routes, `LOG_NOT_FOUND` (tests 11 à 16) |
| 4 | MEN-005 sur la planche (tests 18 et 19) |
| 5 | Documentation : chapitre 9 tenu, `CLAUDE.md`, README |

**La version est `0.8.0`**, comme DEC-058 l'annonçait pour T7.
