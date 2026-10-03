# Pawnsmith — Cahier des charges : tranche T6, première partie (l'API)

| | |
|---|---|
| **Version** | 1.0 |
| **Date** | 3 octobre 2026 |
| **Document parent** | `pawnsmith-bible.md` v0.16 — chapitres 9, 10, 12, 15 et 16 en particulier |
| **Documents frères** | les cahiers T1 à T4 (parties B à E), dont ce document reprend la forme |
| **Portée** | Les points de terminaison HTTP au-dessus des cas d'usage existants. **Pas le front.** |

> **Régime d'écriture.** Comme T4, ce document a été écrit et tranché **sans arbitrage du porteur**. Chaque décision est une fiche du chapitre 11 (DEC-082 à DEC-089), reprise au §8 de `CLAUDE.md` sous « Décisions prises sans toi ».

> **Comment lire ce document.** T6 est découpée en deux. Cette **première partie** est l'API : ASP.NET au-dessus des cas d'usage de T1 à T4, avec les codes d'erreur des §C.11, §D.10 et §E.11 exposés tels quels. La **seconde partie**, le front React, est hors de ce document et n'est pas commencée. Les critères d'acceptation du chapitre 12 portent pour l'essentiel sur le front ; ceux qui portent sur l'API sont repris en G.12.
>
> Les sections sont numérotées **G.x** : la lettre F est réservée à T5, qui n'est pas écrite.

> **Une question du chapitre 16 est fermée ici** : C, la dernière sous-question — que fait l'export d'un candidat élu mais désaligné (G.8, DEC-082). La question E (contrat d'API) est fermée **pour sa partie serveur** : points de terminaison, verbes, charges utiles et codes sont ceux de G.5 à G.10.

---

## G.0 Consignes de travail

Celles des tranches précédentes s'appliquent. Trois avec une force particulière.

- **L'API ne décide de rien.** Chaque point de terminaison lit une requête, appelle un cas d'usage existant ou le dépôt, et traduit le résultat. Une règle qui apparaîtrait dans un point de terminaison serait une règle manquante dans l'Application — la même exigence que celle que B.7 pose au CLI, pour la même raison.
- **Aucun code magique.** Pas de découverte automatique de points de terminaison, pas d'enregistrement de services par balayage d'assemblage, pas de mapping automatique (DEC-021). Chaque route est écrite, chaque service est enregistré à la main, chaque DTO a sa méthode `ToDto()`.
- **Une API locale sans authentification est une surface.** MEN-004 la publie sur la boucle locale ; ce n'est pas suffisant contre un navigateur (G.11, MEN-010).

---

## G.1 Objectif et périmètre

**Entrée** : des requêtes HTTP JSON, et deux formats binaires (une archive ZIP à l'import ; rien d'autre).
**Sortie** : du JSON, des codes d'erreur, un PDF, une archive, des images.

### Dans le périmètre

- L'**hôte** : configuration, composition des services, sérialisation JSON, traduction des erreurs en codes (G.2, G.3).
- Les **points de terminaison** : configuration, catalogue, générateur, projets, gabarits, candidats, images, lots, planche, archives (G.5 à G.10).
- La **file des lots** en arrière-plan et la sérialisation des écritures d'un projet (G.7).
- La **question C** : l'export d'un élu désaligné (G.8).
- Les menaces que l'API ouvre (G.11).

### Hors périmètre

Le front (seconde partie de T6). La journalisation et le visualiseur de journaux (T7). Le détourage (T5) — une API ne peut donc pas encore produire de candidat élisible, et c'est normal. Toute modification de l'adresse du générateur par l'API (DEC-081).

> **Deux tentations à nommer.** La première est de mettre un peu de logique « juste pour l'API » dans un point de terminaison — vérifier qu'une quantité est positive, par exemple. Le sauveur le fait déjà et nomme le champ ; le refaire ici créerait deux vérités. La seconde est d'ajouter un paquet de test pour démarrer l'application en mémoire : G.13 montre qu'un hôte réel sur un port local suffit, sans dépendance.

---

## G.2 L'hôte et sa configuration

### G.2.1 Le fichier de configuration arrive avec l'hôte qui le lit

DEC-057 l'avait annoncé : les bornes de T2, et plus tard celles de T4, vivent en records d'options « tant qu'aucun hôte ne lit de fichier ». L'hôte existe. **La configuration passe par le mécanisme standard d'ASP.NET** : `appsettings.json`, surchargé par les variables d'environnement préfixées `Pawnsmith__` — `Pawnsmith__Generator__Url=http://192.168.1.20:8188`, par exemple. C'est le mécanisme que tout opérateur Docker connaît, et il ne demande aucune ligne de code de lecture.

| Clé | Défaut | Rôle |
|---|---|---|
| `Pawnsmith:ProjectsRoot` | `data/projects` | Racine des projets, relative au dossier de l'application. En conteneur, le volume `/app/data/projects` (A.6) |
| `Pawnsmith:ConfigDirectory` | `config` | Où lire `calibration.json`, `catalog.{univers}.json`, `prompt-template.{univers}.json` |
| `Pawnsmith:Generator:Url` | *(vide)* | Adresse du ComfyUI. Vide : génération non configurée |
| `Pawnsmith:Generator:WorkflowFile` | `config/workflow.comfyui.json` | Le workflow exporté de la machine de l'utilisateur |
| `Pawnsmith:MaxUploadBytes` | 1 Gio | Plus grosse archive acceptée à l'import, en octets |
| `AllowedHosts` | `localhost;127.0.0.1;[::1]` | Noms d'hôte acceptés (G.11) |

Les bornes des records d'options de T2 et T4 **gardent leurs valeurs par défaut** et ne sont pas exposées dans le fichier. Les exposer toutes en ferait autant de réglages à documenter pour un besoin que personne n'a exprimé ; en exposer une plus tard est une ligne.

### G.2.2 Ce qui empêche de démarrer, et ce qui ne l'empêche pas

| Fichier | Absent ou invalide |
|---|---|
| `calibration.json` | **Le démarrage échoue.** Charger un projet demande la calibration (DEC-053) ; sans elle, rien ne marche |
| `catalog.fantasy.json`, `prompt-template.fantasy.json` | **Le démarrage échoue.** Ce sont des fichiers livrés ; s'ils sont faux, l'installation est cassée |
| Adresse du générateur vide | L'application démarre ; le générateur est **non configuré** |
| Workflow absent, adresse ou workflow invalides | L'application démarre ; le générateur est **mal configuré**, avec le code du refus (`WORKFLOW_INVALID`, `GENERATOR_URL_INVALID`…) |

**Une erreur de configuration du générateur n'empêche pas de travailler sur ses projets.** C'est DEC-056 transposé : une donnée de machine n'empêche pas l'accès aux données de l'utilisateur. Le refus reste lu **au démarrage**, comme E.10.1 l'exige — il n'attend pas le premier lot —, et il est exposé par `GET /api/generator` (G.6) ; seuls les points de terminaison de génération répondent `503` avec ce code.

À consigner en **DEC-087**.

---

## G.3 Les erreurs : un code, jamais un message

### G.3.1 La forme

Toute erreur rend un statut HTTP et un corps JSON d'**une seule clé** :

```json
{ "code": "BLUEPRINT_NOT_FOUND" }
```

**Le message n'est jamais renvoyé.** Deux raisons, dont la seconde est nouvelle.

- **Le chapitre 10** : l'API rend des codes, et les traductions vivent en un seul endroit, le front. Un message anglais n'est pas traduit, mais il est du texte, et un texte affiché tel quel est exactement ce que le chapitre 10 interdit.
- **Les messages contiennent des chemins absolus.** « The project folder '/app/data/projects/x' does not exist » dit à qui le lit l'arborescence du serveur. Le chapitre 8 interdit aux journaux de voyager pour cette raison ; un corps de réponse HTTP est un canal de plus. Les messages iront aux journaux (T7), pas à l'appelant.

### G.3.2 Le statut, par code

| Statut | Codes |
|---|---|
| `400` | `BATCH_SIZE_INVALID`, `REQUEST_INVALID` (corps illisible, énumération inconnue, identifiant mal formé) |
| `403` | `CROSS_ORIGIN_REFUSED` (G.11) |
| `404` | `PROJECT_NOT_FOUND`, `BLUEPRINT_NOT_FOUND`, `CANDIDATE_NOT_FOUND`, `JOB_NOT_FOUND`, `IMAGE_NOT_FOUND`, `UNIVERSE_NOT_FOUND` |
| `409` | `IMPORT_DESTINATION_EXISTS`, `JOB_ALREADY_FINISHED` |
| `413` | `UPLOAD_TOO_LARGE` |
| `422` | `PROJECT_INVALID`, `PROJECT_PATH_ESCAPE`, `PROJECT_OVERRIDE_INVALID`, `PROJECT_SCHEMA_TOO_RECENT`, `PROJECT_TOO_LARGE`, `ARCHIVE_REJECTED`, `ARCHIVE_LIMIT_EXCEEDED`, `ARCHIVE_EXPORT_FAILED`, `CANDIDATE_NOT_CUT_OUT`, `PAPER_FORMAT_UNKNOWN`, `SHEET_CAPACITY_EXCEEDED`, `SHEET_INPUT_INVALID` |
| `503` | `GENERATOR_NOT_CONFIGURED`, et les codes de configuration du générateur (`WORKFLOW_*`, `GENERATOR_URL_INVALID`) |
| `500` | `INTERNAL_ERROR` — tout ce qu'aucun code ne décrit |

La table est écrite dans le code une fois, en `switch` exhaustif sur la chaîne du code : un code qui n'y figure pas tombe sur `500`, ce qu'un test parcourant tous les codes connus empêche d'arriver en silence.

Les codes **`GENERATOR_UNREACHABLE`, `GENERATOR_TIMEOUT`…** n'apparaissent jamais comme réponse HTTP : un lot les porte dans son état `Failed` (DEC-074), et la requête qui l'a lancé a déjà reçu `202`.

**Neuf codes sont nouveaux** et levés par l'API elle-même : `REQUEST_INVALID`, `CROSS_ORIGIN_REFUSED`, `JOB_NOT_FOUND`, `JOB_ALREADY_FINISHED`, `IMAGE_NOT_FOUND`, `UNIVERSE_NOT_FOUND`, `UPLOAD_TOO_LARGE`, `GENERATOR_NOT_CONFIGURED` et `INTERNAL_ERROR` ; trois naissent dans l'Application pour la planche : `PAPER_FORMAT_UNKNOWN`, `SHEET_CAPACITY_EXCEEDED` (que le chapitre 10 nommait déjà) et `SHEET_INPUT_INVALID`, qui recouvre une image élue introuvable ou illisible sur le disque.

À consigner en **DEC-084**.

---

## G.4 Les DTO

Une frontière, un jeu de DTO (§6.2). Des `record` immuables, une méthode `ToDto()` écrite à la main par type, jamais l'inverse automatique.

Quatre règles de forme, chacune pour un défaut précis.

- **Les énumérations s'écrivent par leur nom** — `"Medium"`, `"TabAndSocket"` —, jamais par leur rang. Un rang change quand on insère un membre ; un nom ne change que par une décision.
- **La graine s'écrit en chaîne décimale.** Le §C.3.4 l'a déjà décidé pour `project.json` : un nombre JSON passe par `JSON.parse`, donc par un double, et au-delà de 2^53 il est arrondi en silence. Le front est React ; l'API fait le même choix que le fichier.
- **Les paramètres optionnels s'écrivent en tableau `[{ key, value }]` trié par clé, ordinal.** Un dictionnaire sérialisé sort dans l'ordre de ses entrées, qui n'est garanti par rien ; deux appels identiques pourraient rendre deux corps différents. Le tri ordinal, et non culturel, pour la raison de C.3.3.
- **Les valeurs dérivées sont calculées à la demande et marquées comme telles** : `resolvedPrompt` sur le gabarit, `misalignedClauses` sur le candidat. Elles ne sont jamais acceptées en entrée. Si le workflow n'est pas configuré, la clause de cadrage est inconnue : les deux valent alors `null`, et le front le dit — un désalignement inconnu n'est pas un alignement.

---

## G.5 Adresser un projet

**Décision : un projet s'adresse par le nom de son dossier, sous la racine des projets — `/api/projects/{folder}`. Jamais par son `projectId`.**

- **Le `projectId` n'est pas unique.** DEC-047 l'a voulu ainsi : importer deux fois la même archive donne deux dossiers au même identifiant, et c'est une copie, pas une corruption. Une adresse doit désigner une seule chose.
- **Le nom du dossier, lui, est unique sous une racine**, par construction du système de fichiers.

Ce nom arrive dans une URL, donc d'un client : c'est MEN-002 et MEN-009 à la fois. Il n'est accepté que s'il est **déjà sous sa forme canonique** — `ProjectFolderName.From(folder) == folder`, comparaison ordinale — puis le chemin résolu est vérifié comme étant un enfant direct de la racine. Tout écart, comme un dossier absent, rend `404 PROJECT_NOT_FOUND`, sans distinguer « mal formé » de « inexistant » : l'appelant légitime n'en a pas besoin, l'autre n'a pas à l'apprendre.

La liste des projets rend, pour chaque dossier de la racine qui contient un `project.json`, son nom de dossier, son `projectId`, son nom, sa date de modification — **ou le code qui l'empêche de se charger**. Un projet cassé reste visible : il s'ouvre pour être corrigé, pas pour disparaître (DEC-056). Le doublon de `projectId` que T2 renvoyait à T6 n'est pas arbitré par l'API : elle expose l'identifiant, le front posera la question.

À consigner en **DEC-083**.

---

## G.6 Les points de terminaison

Toutes les routes sont sous `/api`. Les corps sont en JSON, sauf mention.

### Configuration, catalogue, générateur — lecture seule

| Verbe | Route | Réponse |
|---|---|---|
| `GET` | `/api/configuration` | Formats de papier, tailles, géométries, univers connus |
| `GET` | `/api/universes/{universe}/catalog` | Le catalogue de l'univers, dans l'ordre du fichier |
| `GET` | `/api/generator` | `{ state, code?, address?, framingClause? }`, où `state` vaut `Available`, `Unreachable`, `Unhealthy`, `NotConfigured` ou `Misconfigured` |

`GET /api/generator` **affiche** l'adresse ; aucune route ne la modifie (DEC-081).

### Projets

| Verbe | Route | Corps | Réponse |
|---|---|---|---|
| `GET` | `/api/projects` | — | La liste de G.5 |
| `POST` | `/api/projects` | `{ name, universe, geometry, paperFormat }` | `201`, le projet |
| `GET` | `/api/projects/{folder}` | — | Le projet, ses diagnostics, ses valeurs dérivées |
| `PUT` | `/api/projects/{folder}/settings` | `{ name, universe, geometry, paperFormat, style, calibrationOverrides }` | Le projet |

`PUT …/settings` remplace les champs de projet **sans verrou ni avertissement** : DEC-055, et les candidats concernés se retrouvent désalignés, ce que la réponse montre aussitôt.

### Gabarits et candidats

| Verbe | Route | Corps | Cas d'usage |
|---|---|---|---|
| `POST` | `/api/projects/{folder}/blueprints` | champs du gabarit | `BlueprintEditor.Add` |
| `PUT` | `…/blueprints/{id}` | champs du gabarit | `BlueprintEditor.UpdateFields` (DEC-067) |
| `PUT` | `…/blueprints/{id}/subject-clause` | `{ clause }` | `BlueprintEditor.EditSubjectClause` |
| `DELETE` | `…/blueprints/{id}` | — | `BlueprintRemoval.Remove`, puis suppression des fichiers (DEC-070) |
| `PUT` | `…/blueprints/{id}/election` | `{ candidateId }` ou `{ candidateId: null }` | `CandidateElection.Elect` / `Unelect` |
| `PUT` | `…/blueprints/{id}/candidates/{candidateId}/status` | `{ status }` | `CandidateJudgement.SetStatus` (nouveau, G.6.1) |
| `GET` | `/api/projects/{folder}/images/{fileName}` | — | Le PNG, s'il est référencé (G.6.2) |

Les réponses d'ajout et de modification portent les **diagnostics de composition** à côté du gabarit — une valeur inconnue du catalogue est insérée et signalée, jamais refusée (DEC-056).

### G.6.1 Un seul cas d'usage nouveau : statuer sur un candidat

Le parcours du §D.3 fait **statuer** l'utilisateur sur chaque candidat, et aucun cas d'usage ne le permettait : T3 a écrit l'élection, pas le jugement. `CandidateJudgement.SetStatus` remplace le statut d'un candidat, et rien d'autre — ni l'élection (DEC-068), ni aucune exigence de fichier (DEC-071). Un candidat élu peut être rejeté ; T3 a déjà un test qui dit pourquoi le modèle ne l'interdit pas.

### G.6.2 Les images ne se servent que si elles sont référencées

`GET …/images/{fileName}` ne sert un fichier que si `images/{fileName}` est **référencé par un candidat du projet**, et que ce chemin passe les règles de C.3.5 et la vérification de préfixe. Ce n'est pas un serveur de fichiers statiques sur `images/` : c'est la liste blanche de l'export (DEC-050) appliquée à la lecture. Un fichier déposé à la main dans `images/`, un orphelin, un `.tmp` restent invisibles.

À consigner en **DEC-088**.

---

## G.7 Les lots en arrière-plan

### G.7.1 La file

| Verbe | Route | Corps | Réponse |
|---|---|---|---|
| `POST` | `/api/projects/{folder}/blueprints/{id}/jobs` | `{ count }` ou `{ seeds: ["…"] }` | `202`, le `Job` en `Queued` |
| `GET` | `/api/jobs` | — | Les jobs connus, du plus récent au plus ancien |
| `GET` | `/api/jobs/{id}` | — | Un job |
| `POST` | `/api/jobs/{id}/cancel` | — | Le job, `Cancelled` s'il attendait, en cours d'annulation s'il tournait |

**Un seul lot tourne à la fois**, dans l'ordre d'arrivée. Un générateur local a une carte graphique ; deux lots simultanés se partageraient la même file ComfyUI sans aller plus vite, et l'ordre de leurs candidats deviendrait illisible.

**La requête est validée avant d'être mise en file** : lot vide ou trop grand, gabarit inconnu, générateur non configuré — chacun rend son code tout de suite, et aucun job n'existe. Pour cela, `CandidateGeneration` est **scindé en deux** : `QueueAsync` valide et rend un job `Queued` ; `RunAsync` prend ce job et le mène à son terme. La forme d'une seule méthode, utilisée par le CLI et les tests de T4, reste disponible et enchaîne les deux. Le lot fige toujours ses clauses **au démarrage**, pas à la mise en file : un utilisateur qui corrige sa clause pendant que son lot attend veut que la correction compte.

**Les jobs restent en mémoire** (DEC-074) ; le registre garde les cent derniers jobs terminés et oublie les plus anciens. Un redémarrage les perd tous, et c'est sans conséquence, puisque chaque candidat est déjà dans son projet.

### G.7.2 Les écritures d'un même projet sont sérialisées

DEC-075 l'avait annoncé : relire le projet à chaque graine réduit la fenêtre de concurrence, mais ce n'est pas un verrou. Avec l'API, deux écrivains existent vraiment — un lot qui ajoute un candidat, et l'utilisateur qui modifie un gabarit au même instant.

**Décision : toute séquence « charger, modifier, sauvegarder » sur un projet passe par une porte par dossier**, `ProjectWriteGate`, dans l'Application. Les points de terminaison qui écrivent la franchissent ; le lot la franchit pour chaque candidat, autour de sa relecture, de l'écriture de l'image et de la sauvegarde. Les lectures ne la franchissent pas : elles lisent un fichier remplacé d'un bloc (§C.7.3).

La porte est **dans le processus**. Deux instances de Pawnsmith sur la même racine ne se voient pas, et ce n'est pas un usage prévu : l'application est mono-utilisateur, une instance suffit.

À consigner en **DEC-085**, et **DEC-086** pour la porte.

---

## G.8 La planche — et la question C

| Verbe | Route | Réponse |
|---|---|---|
| `GET` | `/api/projects/{folder}/sheet/report` | Pages, capacité, gabarits sautés, élus désalignés, images limitées par leur largeur |
| `GET` | `/api/projects/{folder}/sheet.pdf?culture=fr` | Le PDF |

### G.8.1 Que fait l'export d'un élu désaligné ?

C'était la dernière sous-question de C, renvoyée à T6 par le §C.5.6. Trois réponses possibles : bloquer, avertir, passer outre.

**Décision : l'export passe outre, et le dit.** Le PDF est produit avec l'élu tel qu'il est ; le rapport de planche liste chaque élu désaligné et **les clauses qui ont bougé**.

- **Bloquer** forcerait à régénérer un pion pour pouvoir imprimer, alors que l'image n'est pas fausse : elle a été produite sous un autre prompt, ce qui est une information, pas un défaut. L'utilisateur qui a changé son style pour ses **prochains** pions peut vouloir garder ses anciens tels qu'ils sont.
- **Avertir** par une confirmation au moment de l'export est le mécanisme de consentement que DEC-030 a écarté : il arrive quand l'utilisateur est décidé, et il apprend à cliquer « oui ».
- **Passer outre en le disant** est le motif que DEC-056 et DEC-069 ont installé : on n'empêche rien, on dit ce qu'on a fait. L'information arrive là où elle sert — à côté de l'aperçu de la planche, avant l'impression.

Le rapport dit aussi quand le désalignement est **inconnu** — workflow non configuré, donc clause de cadrage inconnue (G.4). Ne rien signaler dans ce cas laisserait croire que tout est aligné.

**Ferme la question C du chapitre 16.** À consigner en **DEC-082**.

### G.8.2 Le rapport sans le PDF

Le rapport calcule la mise en page entière — mesure des images, grille, pagination — **sans rendre**. C'est ce que l'indicateur de capacité du §15.4 demande : par page, la **taille**, la **capacité en cellules** et le **nombre de cellules occupées**, calculés sur la calibration **effective** du projet (DEC-053). Une capacité nulle rend `SHEET_CAPACITY_EXCEEDED`, en nommant la taille dans le journal, pas dans la réponse (G.3).

Le calcul vit dans un cas d'usage d'Application, `ProjectSheet`, qui réunit ce que le CLI de T3 faisait à la main — calibration effective, format de papier, `ProjectSheetRequestBuilder`, mesure, mise en page, rendu. Le CLI n'avait pas de logique à lui ; il en avait emprunté une qui n'avait pas encore de maison.

---

## G.9 Les archives

| Verbe | Route | Corps | Réponse |
|---|---|---|---|
| `POST` | `/api/projects/{folder}/archives?profile=Backup` | — | L'archive, `application/zip` |
| `POST` | `/api/projects/import?name=…` | l'archive, `Content-Type: application/zip` | `201`, le projet importé |

L'export écrit l'archive dans un dossier temporaire du système, **hors de toute racine de projets** (la règle de l'exportateur l'exige), la sert, puis l'efface. L'import reçoit **le corps brut**, pas un formulaire `multipart` : c'est plus simple, et c'est aussi ce qui le protège d'un formulaire HTML forgé (G.11). Le corps est borné par `MaxUploadBytes` **pendant** la réception, écrit dans un fichier temporaire, importé par `ImportArchiveAsync`, puis effacé — l'import refuse lui-même toute archive invalide (C.9).

---

## G.10 Les en-têtes et la culture

La culture du PDF est un paramètre de la requête (`culture`), parmi `fr` et `en` ; toute autre valeur rend `400 REQUEST_INVALID`. L'API ne lit pas `Accept-Language` : la culture d'une planche est un choix de l'utilisateur au moment de l'impression (§15.1), pas une propriété de son navigateur.

---

## G.11 Sécurité — ce que l'API ouvre

### G.11.1 MEN-010 : un navigateur est un client

MEN-004 traite l'exposition réseau : l'application se publie sur `127.0.0.1`. **Ce n'est pas suffisant**, parce que le navigateur de l'utilisateur, lui, est sur `127.0.0.1`, et qu'il exécute le code de n'importe quel site ouvert dans un autre onglet. Deux vecteurs, deux contre-mesures.

- **Requête intersite forgée.** Une page tierce peut envoyer un formulaire `POST` vers `http://localhost:8080/api/jobs/{id}/cancel` — un formulaire HTML ne déclenche aucune vérification préalable du navigateur. **Contre-mesure** : toute requête autre que `GET` et `HEAD` qui porte un en-tête `Origin` doit avoir pour origine l'hôte même de la requête, sinon `403 CROSS_ORIGIN_REFUSED`. Les navigateurs envoient `Origin` sur ces requêtes ; un client qui ne l'envoie pas — `curl`, le CLI — n'est pas un navigateur, et n'est pas le vecteur.
- **Rebinding DNS.** Un domaine tiers peut se faire résoudre vers `127.0.0.1` : pour le navigateur, la page et l'API sont alors de même origine, et la vérification ci-dessus passe. **Contre-mesure** : `AllowedHosts` restreint par défaut aux noms locaux (G.2.1). Une requête dont l'en-tête `Host` est `evil.example` est refusée avant tout point de terminaison. Un utilisateur qui publie volontairement l'application sur son réseau ajoute son nom d'hôte, en connaissance de cause.

Le reste est structurel : l'import n'accepte que `application/zip` et les corps JSON que `application/json` — deux types qu'une page tierce ne peut envoyer sans vérification préalable du navigateur, laquelle échoue faute d'en-têtes CORS : l'API n'en émet aucun (§6.3).

À consigner en **DEC-089**, qui ajoute MEN-010 au chapitre 9.

### G.11.2 Ce qui est repris des menaces existantes

| Menace | Où elle est tenue dans l'API |
|---|---|
| MEN-002, MEN-009 | Le nom de dossier de l'URL n'est accepté que canonique, puis vérifié par préfixe (G.5) ; une image n'est servie que si elle est référencée (G.6.2) |
| MEN-003 | Aucune route ne modifie l'adresse du générateur (DEC-081) |
| MEN-005 | L'archive importée est bornée en taille à la réception, puis par les bornes de C.9.3 |
| MEN-006 | Les réponses ne portent aucun message, donc aucun chemin absolu (G.3) |
| MEN-007 | Le plafond du lot est vérifié avant la mise en file, et un seul lot tourne à la fois |

---

## G.12 Critères d'acceptation

La première partie de T6 est terminée quand :

- [ ] Les **tests de G.13** passent.
- [ ] Toute erreur rend `{ "code" }` **et rien d'autre** ; chaque code connu a son statut, et un test parcourt la table.
- [ ] Aucun point de terminaison ne contient de règle de gestion : chacun appelle un cas d'usage ou le dépôt.
- [ ] Un élu désaligné est **exporté et signalé**, avec ses clauses (DEC-082).
- [ ] Un candidat désaligné est **distingué** dans la réponse du projet (`misalignedClauses`), y compris quand le désalignement est inconnu.
- [ ] La capacité de chaque page est rendue en cellules, sur la calibration effective (§15.4).
- [ ] Un lot se lance, se suit, s'annule ; deux lots ne tournent jamais ensemble.
- [ ] Une requête forgée d'une autre origine, et un `Host` étranger, sont refusés (MEN-010).
- [ ] Aucune dépendance NuGet ajoutée.
- [ ] Les fiches DEC de G.15 sont écrites ; MEN-010 est au chapitre 9.

---

## G.13 Tests attendus

**Les tests démarrent l'hôte réel** — Kestrel sur un port local choisi par le système — et lui parlent en HTTP, avec la configuration d'un dossier temporaire. Pas de `Microsoft.AspNetCore.Mvc.Testing` : c'est un paquet de plus pour une chose que vingt lignes font, et la règle du §3 de `CLAUDE.md` tranche ce doute-là en faveur des vingt lignes. Le générateur est remplacé, dans la composition, par un faux écrit à la main.

| # | Ce qu'il vérifie |
|---|---|
| 1 | Toute erreur rend un corps d'une seule clé, `code` ; aucun message, aucun chemin |
| 2 | Chaque code connu a son statut ; un code inconnu rend `500` |
| 3 | `GET /api/configuration` rend les formats, les tailles, les géométries |
| 4 | Le catalogue sort dans l'ordre du fichier ; un univers inconnu rend `404` |
| 5 | Le générateur non configuré, mal configuré, injoignable, disponible : quatre états, sans exception |
| 6 | Créer un projet, le relire, le lister |
| 7 | Un nom de dossier non canonique, ou s'échappant de la racine, rend `404 PROJECT_NOT_FOUND` |
| 8 | Un projet illisible apparaît dans la liste avec son code |
| 9 | Modifier le style désaligne les candidats existants, et la réponse le montre |
| 10 | Ajouter un gabarit compose sa clause et rend ses diagnostics |
| 11 | Modifier un gabarit non édité recompose ; un gabarit édité garde sa clause |
| 12 | Statuer sur un candidat ne change ni l'élection ni les fichiers |
| 13 | Élire un candidat sans détourage rend `422 CANDIDATE_NOT_CUT_OUT` |
| 14 | Supprimer un gabarit supprime ses fichiers |
| 15 | Une image référencée est servie ; une image non référencée rend `404`, même présente sur le disque |
| 16 | Les paramètres optionnels sortent triés ; la graine sort en chaîne |
| 17 | Un lot se lance (`202`, `Queued`), se termine, et ses candidats sont dans le projet |
| 18 | Un lot trop grand rend `400 BATCH_SIZE_INVALID` sans créer de job |
| 19 | Un lot sans générateur configuré rend `503 GENERATOR_NOT_CONFIGURED` |
| 20 | Annuler un lot en file le rend `Cancelled` sans qu'il ait tourné |
| 21 | Deux lots ne tournent jamais en même temps |
| 22 | Une modification faite pendant un lot n'est pas perdue (porte d'écriture) |
| 23 | Le rapport de planche rend capacité et occupation par page, sur la calibration effective |
| 24 | Un élu désaligné est exporté et listé avec ses clauses (DEC-082) |
| 25 | Le PDF sort, dans la culture demandée ; une culture inconnue rend `400` |
| 26 | Export puis import d'une archive : le projet revient |
| 27 | Une archive trop grosse rend `413 UPLOAD_TOO_LARGE` |
| 28 | Une requête `POST` d'une autre origine rend `403 CROSS_ORIGIN_REFUSED` |
| 29 | Un `Host` étranger est refusé |

---

## G.14 Ce que cette partie ne fait pas

| L'API… | Parce que |
|---|---|
| …ne sert pas de front nouveau | Seconde partie de T6 |
| …ne journalise pas | T7 |
| …ne modifie pas l'adresse du générateur | DEC-081 |
| …ne persiste pas les jobs | DEC-074 |
| …n'arbitre pas le doublon de `projectId` | Elle l'expose ; le front posera la question |
| …ne supprime pas de projet | Aucun cas d'usage ne le fait, et un dossier se supprime au gestionnaire de fichiers. À écrire avec le front, s'il le demande |
| …ne détoure pas | T5 |

---

## G.15 Décisions à consigner dans la bible

| Réf. | Objet | Section |
|---|---|---|
| **DEC-082** | L'export d'un élu désaligné passe outre et le signale, clause par clause. **Ferme la question C** | G.8.1 |
| **DEC-083** | Un projet s'adresse par son nom de dossier canonique, jamais par son `projectId` | G.5 |
| **DEC-084** | Une erreur d'API rend un code et rien d'autre ; la table des statuts | G.3 |
| **DEC-085** | Les lots : validés avant la file, un seul à la fois, registre en mémoire borné | G.7.1 |
| **DEC-086** | Les écritures d'un même projet passent par une porte par dossier, dans le processus | G.7.2 |
| **DEC-087** | La configuration passe par `appsettings.json` et l'environnement ; un générateur mal configuré n'empêche pas de démarrer | G.2 |
| **DEC-088** | Une image n'est servie que si un candidat la référence | G.6.2 |
| **DEC-089** | MEN-010 : requêtes intersites et rebinding DNS ; contrôle d'origine et `AllowedHosts` restreint | G.11.1 |

---

## G.16 Découpage en tâches

| # | Tâche |
|---|---|
| 1 | Application : `CandidateJudgement`, `ProjectWriteGate`, `CandidateGeneration` scindé ; `<Version>0.7.0</Version>` |
| 2 | Dépôt : liste des projets, suppression d'images par le port, résolution d'une image à servir |
| 3 | Application : `ProjectSheet` — rapport, PDF, élus désalignés (DEC-082) |
| 4 | Hôte : configuration, composition, JSON, erreurs, origine et hôtes ; projet de tests et son harnais ; configuration, catalogue, générateur |
| 5 | Projets : liste, création, lecture, réglages |
| 6 | Gabarits, candidats, images |
| 7 | Lots : file, registre, travailleur, points de terminaison |
| 8 | Planche : rapport et PDF |
| 9 | Archives : export et import |
| 10 | Documentation |

**La version est `0.7.0`**, et non `0.6.0` : DEC-058 attache un mineur à une tranche, `0.6.0` est celui de T5, qui n'est pas écrite. Le numéro dit « ce binaire relève de T6 ».
