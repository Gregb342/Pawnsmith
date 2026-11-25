# Pawnsmith — Cahier des charges : tranche T6, seconde partie (le front)

| | |
|---|---|
| **Version** | 1.2 |
| **Date** | 25 novembre 2026 |
| **Document parent** | `pawnsmith-bible.md` v0.21 — chapitres 2, 10, 12 et 15 en particulier |
| **Documents frères** | le cahier T6 (l'API), dont ce document est la suite |
| **Portée** | Les neuf décisions de la revue de maquette (D1 à D9), ce qu'elles demandent à l'API, et le front React |

> **Changements depuis la v1.1** — Ajout du §I.15, la suite écrite le 25 novembre 2026 sans arbitrage du porteur, sur la consigne « continue autant de dev que tu peux ». La revue du chapitre 9 pour le front, qui manquait : **MEN-012** (détournement de clic) et **MEN-013** (injection de contenu), tenues par des en-têtes posés sur toute réponse (DEC-116). Une capacité nulle nomme enfin sa taille (DEC-117). **Le front a des tests automatisés**, Vitest sur les modules purs (DEC-118), ce qui ferme le « trou connu » du §I.12. Un rechargement raté ne remplace plus l'écran ; les cotes du format bordent l'aperçu.

> **Changements depuis la v1.0** — Écriture de la tranche et sa relecture. Ajout du §I.10.5, ce que l'écriture a tranché : le code `GENERATOR_ADDRESS_INVALID`, les entrées personnelles écartées au démarrage, l'interrogation de la file, les libellés de statut, le pas de quantité de la Mise en page. Le §I.12 dit ce qui a été éprouvé dans le navigateur, et les trois défauts que la relecture du front a trouvés.

> **Régime d'écriture.** Les neuf décisions ont été **prises par le porteur**, le 4 octobre 2026, en commentant un document de revue de la maquette. Elles sont consignées en DEC-105 à DEC-112. Le reste — la forme exacte des routes nouvelles, le découpage, l'aperçu de la planche, la version — est tranché sans lui (DEC-113, DEC-114) et repris au §8 de `CLAUDE.md`.

> **Comment lire ce document.** Les sections sont numérotées **I.x**. Les lettres B à H sont prises par T1 à T7. La première moitié du document (I.2 à I.8) est ce que les décisions changent au serveur ; la seconde (I.9 à I.12) est le front.

---

## I.0 Consignes de travail

Celles des tranches précédentes s'appliquent, et celles du cahier T6 pour l'API. Trois de plus pour le front.

- **Aucune chaîne en dur** (chapitre 10). Tout libellé vient des catalogues `fr` et `en`, qui ont **les mêmes clés** : un script le vérifie à chaque `npm run lint`.
- **Le front ne décide de rien.** Il n'assemble pas de prompt, ne calcule pas de capacité, ne devine pas un désalignement : il affiche ce que l'API rend. Une règle qui apparaîtrait dans un composant serait une règle manquante dans l'Application — l'exigence de G.0, appliquée au navigateur.
- **Aucune dépendance de production nouvelle.** React, `react-i18next` et `i18next` suffisent. Le routage tient en trente lignes sur le fragment d'URL (`#/…`) ; une bibliothèque de routage serait une dépendance pour une poignée d'écrans (§3 de `CLAUDE.md` : « vingt lignes plutôt qu'une dépendance »).

---

## I.1 Objectif et périmètre

### Dans le périmètre

- Les libellés d'interface (D1, DEC-105).
- Le catalogue traduit, race et classe en listes (D2, DEC-106).
- Le catalogue personnel et ses objets complets (D3, DEC-107).
- L'adresse du générateur réglée depuis l'interface (D4a, DEC-108).
- La clause sujet verrouillée et sa sortie (D5, DEC-109).
- La bibliothèque de styles (D6, DEC-110).
- L'enregistrement automatique (D7, DEC-111).
- L'univers et le style figés, la duplication (D9, DEC-112).
- L'aperçu de la planche (DEC-113).
- Le front : huit écrans, la barre du haut, les deux langues.

### Hors périmètre

- **D4b, les fournisseurs d'images distants** : EVO-002, après le premier lot ComfyUI réel.
- **D8, le système de design** : après cette tranche, avec le logo du porteur. Le front est écrit sur des **jetons CSS** (variables sur `:root`) pour qu'un système de design les remplace sans toucher aux composants.
- La suppression d'un projet : toujours aucun cas d'usage (G.14).
- Des tests automatisés du front : voir I.12.

---

## I.2 Les libellés (D1, DEC-105)

Les concepts du chapitre 2 ne changent pas. Leurs **libellés d'écran** changent, parce que la revue a montré qu'ils n'étaient pas compris.

| Concept (code) | `fr` | `en` |
|---|---|---|
| `Candidate` | Proposition | Proposal |
| élu / élire | Retenue / Retenir pour l'impression | Kept / Keep for printing |
| désaligné | Prompt modifié depuis | Prompt changed since |
| clauses figées | Ce qui a changé depuis cette image | What changed since this image |
| générateur disponible | ComfyUI connecté | ComfyUI connected |

Le §15.1 rendait le vocabulaire du chapitre 2 contraignant « jusque dans les libellés d'écran ». Il l'est désormais **à travers cette table** : un concept, un libellé par langue, partout.

---

## I.3 Le catalogue traduit (D2, DEC-106)

### I.3.1 Le format, `versionSchema` 2

Chaque paramètre et chaque entrée porte un libellé **par culture d'interface** (`en`, `fr`). La valeur et le fragment restent anglais : ce sont eux qui partent au générateur.

```json
{
  "key": "weapon",
  "labels": { "en": "Weapon", "fr": "Arme" },
  "entries": [
    { "value": "spear", "labels": { "en": "short spear", "fr": "lance courte" },
      "fragment": "wielding a short spear held vertically against the body" }
  ]
}
```

Un libellé manquant ou vide refuse le fichier au chargement (`CATALOG_INVALID`). Le fichier `versionSchema` 1 n'est plus lu : le seul qui existe est livré par le dépôt, et il passe en 2 dans le même commit.

### I.3.2 Race et classe deviennent des clés du catalogue

Deux clés réservées, `race` et `characterClass` — les noms mêmes des jetons de la tête (§D.5). La tête du template devient `{race} {characterClass}`, et **chaque jeton reçoit le fragment** de la valeur quand le catalogue la connaît : `orc` → `an orc`, `goblin` → `a goblin`. Une valeur inconnue est insérée telle qu'écrite et signalée, comme une valeur optionnelle (DEC-056).

C'est ce qui corrige « a orc » (§8 de `CLAUDE.md`, T6) : l'article appartient à l'entrée, pas au template.

Les deux clés ne sont pas des optionnels : `optionalOrder` ne les cite pas, et une composition ne les cherche jamais dans `optionalParameters`.

### I.3.3 Ce que ça ne change pas

Une clause sujet stockée ne bouge pas. Elle se recompose à la prochaine modification d'un champ, si elle n'a pas été éditée (DEC-067).

---

## I.4 Le catalogue personnel (D3, DEC-107)

### I.4.1 Des objets complets seulement

« Autre… » dans une liste ouvre un formulaire **Nouvel objet**. Un objet est complet quand il a :

- une **clé** existante du catalogue livré (on n'invente pas de paramètre) ;
- une **valeur** anglaise, unique dans sa clé — catalogue livré et personnel confondus ;
- un **libellé non vide par culture** ;
- un **fragment** non vide.

L'application exige que le fragment existe ; elle ne peut pas vérifier qu'il décrit bien la pose. Le formulaire pré-remplit donc un exemple de la clé (le fragment de sa première entrée livrée), à adapter.

### I.4.2 Où il vit

Dans le **dossier utilisateur**, `data/user/catalog.{univers}.json` — même format que le catalogue livré, entrées personnelles seulement. Le catalogue servi est la **fusion** : les entrées livrées d'abord, les personnelles ensuite, clé par clé. Chaque entrée dit son origine (`Shipped`, `Personal`).

Le dossier utilisateur est un réglage, `Pawnsmith:UserDirectory`, `data/user` par défaut ; en conteneur, un troisième volume, `/app/data/user`. Il n'entre dans **aucune archive** de projet (DEC-022).

### I.4.3 Les routes

| Verbe | Route | Corps | Réponse |
|---|---|---|---|
| `GET` | `/api/universes/{universe}/catalog` | — | La fusion, avec libellés et origine |
| `POST` | `/api/universes/{universe}/catalog/entries` | `{ key, value, labels, fragment }` | `201`, le catalogue |
| `DELETE` | `/api/universes/{universe}/catalog/entries/{key}/{value}` | — | Le catalogue |

Codes : `CATALOG_ENTRY_INVALID` (422) — clé inconnue, champ vide, libellé manquant ; `CATALOG_ENTRY_DUPLICATE` (409) ; `CATALOG_ENTRY_NOT_FOUND` (404) ; `CATALOG_ENTRY_SHIPPED` (409) — une entrée livrée ne se supprime pas.

Supprimer une entrée personnelle ne touche aucun gabarit : une valeur devenue inconnue est signalée, jamais refusée (DEC-056).

---

## I.5 L'adresse du générateur (D4a, DEC-108)

### I.5.1 Ce qui change

| Verbe | Route | Corps | Réponse |
|---|---|---|---|
| `PUT` | `/api/generator` | `{ address }` ou `{ address: null }` | L'état du générateur, vérifié |

L'adresse est validée par `GeneratorAddress` (schéma http/https, ni identifiants, ni requête, ni fragment), puis **enregistrée** dans `data/user/generator.json`, et le générateur est reconstruit avec le workflow configuré. `null` revient à « non configuré ».

Au démarrage, **le fichier l'emporte** sur `Pawnsmith:Generator:Url` : c'est le dernier choix explicite de l'utilisateur. Sans fichier, l'environnement s'applique comme avant. Le workflow, lui, reste un fichier de configuration : c'est l'export de ComfyUI, pas un réglage d'écran.

### I.5.2 Ce qui ne change pas

Un lot en cours **garde le générateur avec lequel il a été mis en file** : le job le porte. Changer l'adresse vaut pour le lot suivant. Les générateurs remplacés sont libérés à l'arrêt de l'hôte, pas avant, puisqu'un lot peut encore s'en servir.

### I.5.3 MEN-003, relue

DEC-081 interdisait la route pour une raison : une page web malveillante aurait fait appeler par le serveur une adresse interne. Depuis, MEN-010 refuse toute écriture d'une autre origine et tout `Host` non local. Le risque restant est celui de quelqu'un qui tient déjà l'interface — et donc la machine. Les contre-mesures de DEC-081 qui portent sur le **client** restent : aucune redirection suivie, aucun proxy, réponses bornées.

---

## I.6 La clause sujet verrouillée (D5, DEC-109)

DEC-067 tient : l'édition se déduit, elle n'est pas stockée. Deux ajouts à l'API.

- **`subjectClauseEdited`** sur chaque gabarit : vrai si la clause stockée diffère de celle que la composition donnerait. Le front s'en sert pour afficher le cadenas fermé ou ouvert.
- **`DELETE …/blueprints/{id}/subject-clause`** : recompose la clause depuis les champs, et rend les diagnostics de composition. C'est « Revenir au texte automatique ».

Le front affiche la clause en lecture seule ; « Personnaliser le texte » la rend éditable et grise les options, avec le message qu'elles ne modifient plus le texte. Taille et quantité restent actives.

---

## I.7 La bibliothèque de styles (D6, DEC-110)

### I.7.1 Un point de départ, pas un lien

Un style de la bibliothèque est **copié** dans le projet. Modifier la bibliothèque ne touche aucun projet. Le projet reste complet à lui seul, archive comprise.

### I.7.2 Les deux sources

- **Livrés** : `config/styles.{univers}.json`, en lecture seule. Chaque style a un identifiant, un nom par culture, une clause de style et une clause négative.
- **Personnels** : `data/user/styles.{univers}.json`. Un nom (une seule langue : c'est celui de l'utilisateur), une clause, une clause négative.

| Verbe | Route | Corps | Réponse |
|---|---|---|---|
| `GET` | `/api/universes/{universe}/styles` | — | Livrés puis personnels |
| `POST` | `/api/universes/{universe}/styles` | `{ name, styleClause, negativeClause }` | `201`, la liste |
| `DELETE` | `/api/universes/{universe}/styles/{id}` | — | La liste |

Codes : `STYLE_INVALID` (422) — nom ou clause vide ; `STYLE_NOT_FOUND` (404) ; `STYLE_SHIPPED` (409).

Appliquer un style n'a pas de route : le front envoie les réglages du projet avec le style copié, et la règle de I.8 s'applique.

### I.7.3 Palette et clause négative

- **La palette n'est plus exposée.** Le code l'enregistrait sans jamais l'envoyer au générateur. Le champ reste dans `project.json` pour ne pas monter le schéma ; le front ne l'affiche plus et renvoie la valeur reçue. Une palette se dit dans la clause de style.
- **La clause négative passe dans « Avancé ».** À CFG 1,0, Krea 2 Turbo l'ignore (DEC-077).

### I.7.4 Le §15.5 relu

Le §15.5 dit que l'interface « n'expose pas la clause style ». Le §3.1, le §15.1 et DEC-006 la rendent modifiable **au niveau du projet**. La lecture retenue : la clause de style n'apparaît **jamais au niveau d'un gabarit** ; elle s'affiche et se choisit à l'étape Projet. La clause de cadrage, elle, n'apparaît nulle part (DEC-029) : le prompt affiché sur un gabarit est « sujet + style », précédé de la mention que le cadrage vient du workflow.

---

## I.8 Univers et style figés, duplication (D9, DEC-112)

### I.8.1 La règle

Dès qu'un gabarit du projet a **au moins une proposition**, l'univers et le style ne changent plus. Nom, géométrie, format de papier et cotes d'onglet restent modifiables : ils ne changent que la découpe, jamais les images.

C'est une règle d'Application, `ProjectSettingsEditor.Apply` : elle compare les réglages demandés au projet chargé, et refuse avec `UNIVERSE_FROZEN` ou `STYLE_FROZEN` (409). `SaveAsync` ne reçoit toujours pas l'état antérieur (DEC-055) : c'est le cas d'usage qui compare, dans la porte d'écriture.

Le style se compare en entier — nom, clauses, palette — en ordinal.

### I.8.2 La duplication

| Verbe | Route | Corps | Réponse |
|---|---|---|---|
| `POST` | `/api/projects/{folder}/duplicate` | `{ name, style? }` | `201`, le nouveau projet |

Le nouveau projet a un nouvel identifiant, le même univers, la même géométrie, le même format, les mêmes cotes, le style demandé (ou le même), et **les gabarits sans leurs propositions** : mêmes champs, même clause sujet, aucune élection. Ses dossiers et fichiers sont neufs ; rien n'est copié du dossier `images/`.

### I.8.3 Ce que devient « Prompt modifié »

Il subsiste pour deux causes seulement : un gabarit retouché après ses images (sujet), ou une clause de cadrage changée dans le workflow (cadrage). La cause « style » ne peut plus apparaître sur un projet créé après cette tranche ; elle reste calculée pour les projets existants.

---

## I.9 L'aperçu de la planche (DEC-113)

**L'aperçu est le PDF lui-même**, affiché dans la page. `GET …/sheet.pdf` accepte `disposition=inline`, qui rend le fichier sans en forcer le téléchargement ; sans le paramètre, rien ne change.

Le §15.2 exige que l'aperçu montre ce que le PDF contiendra, repères compris. Le seul aperçu qui le garantit est le PDF. Le redessiner dans le navigateur demanderait de dupliquer la mise en page du domaine en TypeScript, et §15.5 interdit que l'interface décide de la planche. L'indicateur de capacité (§15.4) vient du rapport : cellules, occupées, taille.

---

## I.10 Le front

### I.10.1 La coquille

- **Barre du haut** : le nom, le projet ouvert (retour à la liste), l'état de ComfyUI (vers la page Générateur), Catalogue, Journaux, la langue.
- **Étapes**, centrées : Projet, Gabarits, Génération, Mise en page, Impression (§15.1). Elles n'apparaissent qu'avec un projet ouvert.
- **Routage** sur le fragment : `#/projects`, `#/p/{folder}/{étape}`, `#/generator`, `#/catalog`, `#/logs`. Revenir à une étape ne perd rien (§15.1) : l'état vit dans le projet, enregistré.

### I.10.2 Les écrans

| Écran | Contenu |
|---|---|
| **Projets** | Liste, création, import d'une archive ; un projet illisible reste listé avec son code traduit ; deux dossiers au même `projectId` sont signalés comme copies |
| **Projet** | Nom, univers (figé après la première proposition), géométrie, format, cotes d'onglet (si « Onglet et socle »), style (bibliothèque, clause, « Avancé »), duplication, archives |
| **Gabarits** | Liste et éditeur : race, classe, taille, quantité, optionnels, détails, clause sujet verrouillée, prompt affiché ; « Autre… » ouvre Nouvel objet ; suppression confirmée |
| **Génération** | Lancer un lot, file des lots, galerie, statut, retenir, détourer, ce qui a changé |
| **Mise en page** | Pages, capacité en cellules, aperçu PDF, rapport (non imprimés, retenues au prompt modifié, images limitées en largeur) |
| **Impression** | Langue de la planche, vérifications avant découpe, téléchargement |
| **Générateur** | État, adresse modifiable, test |
| **Catalogue** | Entrées personnelles, ajout, suppression |
| **Journaux** | Fichiers, lignes, filtre avertissements et erreurs |

### I.10.3 L'enregistrement automatique (D7, DEC-111)

Chaque champ s'enregistre **quand on le quitte** (perte de focus) ou, pour une liste ou un bouton radio, quand il change. Un indicateur discret dit « Enregistrement… », « Enregistré » ou l'erreur. Il n'y a pas de bouton Enregistrer. Changer d'écran ou de projet ne perd rien : le dernier champ quitté est déjà parti.

### I.10.4 Les erreurs

Une erreur d'API est un code (DEC-084). Le front le traduit par la clé `errors.{CODE}` ; un code sans traduction affiche le code lui-même, jamais une chaîne vide.

### I.10.5 Ce que l'écriture a tranché

| Point | Choix | Motif |
|---|---|---|
| Adresse refusée | `GENERATOR_ADDRESS_INVALID`, `422`, distinct de `GENERATOR_URL_INVALID` (`503`) | Le second est une faute de déploiement, le premier une saisie de l'utilisateur. L'adresse refusée n'est répétée nulle part, journal compris |
| Entrée personnelle devenue invalide | Écartée au démarrage et journalisée, le démarrage continue | Une mise à jour peut livrer une valeur qu'un utilisateur avait ajoutée, ou retirer une clé. Arrêter l'application pour ça punirait l'utilisateur d'avoir été en avance |
| Fichier de styles illisible | `STYLES_INVALID` au démarrage, comme un catalogue illisible | Livré par le dépôt : son erreur est une erreur de déploiement |
| Avancement des lots | Interrogation de `GET /api/jobs` toutes les 1,5 s tant que l'écran Génération est ouvert ; tout changement d'un job du projet recharge le projet | Aucun canal poussé à écrire ni dépendance à ajouter ; une requête locale de quelques centaines d'octets |
| Statut d'une proposition | Brouillon, Validée, Rejetée | Le vocabulaire de la bible (§3.1), au féminin de « proposition ». DEC-105 ne nommait pas les statuts |
| Quantité sur la Mise en page | Un pas `−` / `+` par gabarit imprimé | La question du §15.4 — « ce gobelin de plus tient-il sur la page ? » — se pose là, pas à l'étape Gabarits |
| Culture de la planche | Par défaut la langue de l'interface, choisie à l'Impression | §15.1 et §G.10 : un choix fait au moment d'imprimer, jamais lu du navigateur |
| Développement | Le serveur Vite passe `/api` à l'API **sans réécrire `Host`** | Pour que la garde d'origine de MEN-010 voie le même couple `Origin` / `Host` qu'en production |

---

## I.11 Critères d'acceptation

| Critère (chapitre 12 et décisions) | Comment il est tenu |
|---|---|
| Aucune chaîne en dur | Libellés par `t()` ; script de parité des clés `fr`/`en` |
| Bascule français/anglais sans rechargement | `i18next.changeLanguage` ; libellés du catalogue selon la langue courante |
| Capacité de page affichée | Indicateur de cellules sur Mise en page |
| Codes d'erreur traduits | `errors.{CODE}` pour chaque code de l'API |
| Propositions au prompt modifié distinguées | Bordure, hachures, liste des clauses |
| Structure du chapitre 15, liste du §15.5 | Cinq étapes ; ni pion déplaçable, ni repère désactivable, ni cadrage affiché |
| D1 à D9 | I.2 à I.10 |

---

## I.12 Tests

**Serveur** : chaque route nouvelle a ses tests dans `Pawnsmith.Api.Tests`, chaque règle nouvelle les siens dans le domaine ou l'Application. Au moins :

1. Un catalogue sans libellé `fr` est refusé.
2. La tête prend le fragment de la race : `orc` donne `an orc …`.
3. Une race inconnue est insérée telle qu'écrite et signalée.
4. Une entrée personnelle complète est ajoutée et servie avec l'origine `Personal`.
5. Une entrée sans libellé, sans fragment ou sur une clé inconnue est refusée (`CATALOG_ENTRY_INVALID`).
6. Une valeur déjà livrée est refusée (`CATALOG_ENTRY_DUPLICATE`).
7. Une entrée livrée ne se supprime pas (`CATALOG_ENTRY_SHIPPED`).
8. Le catalogue personnel survit à un redémarrage.
9. `PUT /api/generator` enregistre l'adresse, et un redémarrage la relit avant l'environnement.
10. Une adresse avec identifiants est refusée, et rien n'est enregistré.
11. Un lot en file garde son générateur quand l'adresse change.
12. `subjectClauseEdited` est faux après composition, vrai après édition.
13. `DELETE …/subject-clause` recompose et rend les diagnostics.
14. Les styles livrés et personnels sont listés ; un style personnel s'ajoute et se supprime ; un livré ne se supprime pas.
15. Changer le style d'un projet sans proposition passe ; avec une proposition, `STYLE_FROZEN`.
16. Changer l'univers avec une proposition : `UNIVERSE_FROZEN` ; changer la géométrie : passe.
17. La duplication copie les gabarits sans propositions, avec le style demandé, sous un nouvel identifiant.
18. `sheet.pdf?disposition=inline` ne force pas le téléchargement.

**Front** : pas de test automatisé dans cette tranche. Un banc de test du navigateur (Playwright, Vitest) serait une dépendance de développement de plus, et le §3 demande de la justifier par ce qu'elle protège. Ce qui est vérifié à chaque commit : `tsc`, ESLint (règles des hooks comprises), la parité des clés de traduction, et la construction. **C'est un trou connu**, écrit comme tel.

Ce qui a été éprouvé à la place, écran par écran, dans Chromium, par des scripts **hors dépôt**, contre l'API réelle et un faux ComfyUI qui rend des images détourables : création de projet, géométrie qui montre et cache les cotes d'onglet, style de bibliothèque appliqué et style personnel enregistré ; gabarit ajouté, race changée, « Autre… » qui crée une arme complète, texte personnalisé puis retour au texte automatique, entrée personnelle supprimée et valeur restée « hors liste » ; lot lancé, suivi, détouré, proposition validée et retenue, « prompt modifié depuis » après un changement de race, lot annulé ; cellules de la page, exemplaire de plus, PDF en ligne et téléchargé ; adresse avec identifiants refusée sans fuite au journal ; journaux filtrés ; bascule en anglais. Aucune erreur de console hors les refus attendus.

**Non éprouvé** : l'aperçu PDF **affiché** — Chromium sans affichage n'a pas de lecteur PDF ; l'en-tête `inline` et le contenu du fichier l'ont été. Et rien contre un vrai ComfyUI.

La relecture du front a trouvé trois défauts, corrigés dans un commit à part : un tri des propositions par `localeCompare`, comparaison sensible à la culture ; une séquence d'échappement malformée dans l'adresse (`#/p/%E0`) qui faisait lever `decodeURIComponent` pendant le rendu, page blanche ; un nombre de propositions vidé qui partait en `null`.

---

## I.13 Décisions à consigner dans la bible

| Réf. | Objet | Section |
|---|---|---|
| **DEC-105** | Libellés d'interface : Proposition, Retenue, Prompt modifié, ComfyUI connecté. *Décidée par le porteur.* | I.2 |
| **DEC-106** | Catalogue traduit (`versionSchema` 2) ; race et classe en clés du catalogue, la tête prend leurs fragments. *Décidée par le porteur.* | I.3 |
| **DEC-107** | Catalogue personnel : objets complets seulement, dans le dossier utilisateur. *Décidée par le porteur.* | I.4 |
| **DEC-108** | L'adresse du générateur se règle depuis l'interface. **Supersède DEC-081** sur ce point. *Décidée par le porteur.* | I.5 |
| **DEC-109** | Clause sujet verrouillée par défaut ; retour au texte automatique. DEC-067 demeure. *Décidée par le porteur.* | I.6 |
| **DEC-110** | Bibliothèque de styles copiés ; palette retirée de l'interface ; clause négative en « Avancé » ; lecture du §15.5. *Décidée par le porteur.* | I.7 |
| **DEC-111** | Enregistrement automatique, champ par champ. *Décidée par le porteur.* | I.10.3 |
| **DEC-112** | Univers et style figés à la première proposition ; duplication. **Supersède DEC-030 et DEC-055** pour l'univers et le style. *Décidée par le porteur.* | I.8 |
| **DEC-113** | L'aperçu de la planche est le PDF | I.9 |
| **DEC-114** | Le front sans dépendance nouvelle ; la tranche en `0.10.0` | I.0, I.14 |
| **DEC-116** | MEN-012 et MEN-013 ; politique de contenu et en-têtes sur toute réponse ; `dangerouslySetInnerHTML` interdit | I.15.1 |
| **DEC-117** | Une route de capacités par taille ; une capacité nulle y est une valeur, ce qui permet de la nommer | I.15.2 |
| **DEC-118** | Vitest, dépendance de développement, sur les modules purs du front. **Précise DEC-114**, qui ne visait que la production | I.15.3 |

---

## I.14 Découpage en tâches

| # | Tâche |
|---|---|
| 1 | Documentation : ce cahier, DEC-105 à DEC-114, bible v0.19 |
| 2 | Catalogue traduit, race et classe en listes ; `<Version>0.10.0</Version>` |
| 3 | Catalogue personnel : port, fichier, cas d'usage, routes |
| 4 | Bibliothèque de styles : fichier livré, styles personnels, routes |
| 5 | Univers et style figés ; duplication |
| 6 | Clause sujet : `subjectClauseEdited`, retour au texte automatique ; clauses figées dans le DTO |
| 7 | Adresse du générateur : route, fichier, lots qui gardent leur générateur |
| 8 | Aperçu PDF en ligne ; volume `data/user` |
| 9 | Front : fondations — client d'API, routage, coquille, jetons CSS, parité des clés |
| 10 | Front : Projets et Projet |
| 11 | Front : Gabarits, Nouvel objet, Catalogue |
| 12 | Front : Génération |
| 13 | Front : Mise en page et Impression |
| 14 | Front : Générateur et Journaux |
| 15 | Documentation : `CLAUDE.md`, README, ce cahier en v1.1 |

**La version est `0.10.0`** : une tranche livrée vaut un mineur (DEC-058, DEC-104).

---

## I.15 La suite, du 25 novembre 2026

> **Régime.** Écrite sans arbitrage du porteur, sur la consigne « continue autant de dev que tu peux », comme le reste de la tranche avant relecture. Rien n'est fusionné dans `main`. Une session antérieure du même jour avait commencé à réécrire ce front sans voir cette branche ; son travail est abandonné, et ce qu'il avait de juste est repris ici.

Le front est écrit (I.14, tâches 1 à 15). Cette section fait ce qu'il restait d'ouvert, dans l'ordre de ce qui coûte le plus à laisser.

### I.15.1 Ce que le front ouvre au chapitre 9 (DEC-116)

Le chapitre 9 se rouvre à chaque tranche qui ouvre une surface (DEC-054, DEC-097), et DEC-097 nommait « le front de T6 ». La revue n'avait pas été faite. Le front ouvre **deux menaces** que l'API seule n'avait pas.

**MEN-012 — le détournement de clic.** Une page tierce peut charger l'application dans un cadre invisible, sur `http://localhost:8080`, et placer ses propres boutons par-dessus ceux de Pawnsmith : l'utilisateur croit cliquer chez elle, il supprime un gabarit ou change l'adresse du générateur chez lui. Le contrôle d'`Origin` de MEN-010 ne voit rien : la requête part bien de la page de Pawnsmith. **Contre-mesure** : toute réponse porte `Content-Security-Policy: … frame-ancestors 'self'` et `X-Frame-Options: SAMEORIGIN` (le second pour les navigateurs qui ignorent le premier). `'self'` et non `'none'` : l'aperçu de la planche est un cadre de même origine (DEC-113).

**MEN-013 — l'injection de contenu dans la page.** Le front affiche du **texte d'autrui** : un nom de projet, une clause, des détails, une entrée de catalogue, un nom de dossier — venus au besoin d'une archive importée. Rendu comme du HTML, il exécuterait du script dans l'origine de l'application, qui peut tout faire par l'API. **Contre-mesures**, en trois couches : React échappe tout texte inséré ; l'échappatoire `dangerouslySetInnerHTML` est **interdite par une règle ESLint**, donc par la CI ; et la politique de contenu n'admet que les scripts de l'origine, aucun en ligne, ni `<base>` réécrite, ni formulaire vers ailleurs.

La politique, posée par un intergiciel de l'API **au moment où la réponse part** — l'intergiciel d'erreurs vide la réponse, en-têtes compris, avant d'écrire son code :

```
default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:;
frame-ancestors 'self'; base-uri 'none'; form-action 'self'
```

plus `X-Content-Type-Options: nosniff` — une image servie n'est jamais lue comme un script, quels que soient ses octets — et `Referrer-Policy: no-referrer` — l'adresse d'un projet porte son nom, elle ne part pas vers un tiers.

Le front compilé s'y plie sans changement : aucun script ni style en ligne dans `index.html`, aucune ressource extérieure, et les deux styles posés par React passent par le DOM, que `style-src` ne vise pas. **Éprouvé dans Chromium** : le tour complet des écrans sous la politique ne lève aucune violation.

### I.15.2 Une capacité nulle nomme sa taille (DEC-117)

Le §15.4 veut qu'une capacité nulle s'affiche **comme une erreur nommant la taille, le format et la géométrie**. Or le rapport de planche rend alors `SHEET_CAPACITY_EXCEEDED` et rien d'autre (DEC-084) : l'écran Mise en page affichait la traduction du code, sans dire quelle taille.

**Décision : une route de plus, `GET /api/projects/{folder}/sheet/capacities`**, qui rend pour chaque taille de la calibration sa capacité en cellules sur ce projet — format, géométrie et calibration **effective** (DEC-053). **Une capacité nulle y est une valeur, pas une erreur.** Le cas d'usage est `ProjectSheet.CapacitiesAsync`, dans l'Application, à travers le moteur de T1 inchangé. Quand le rapport refuse pour capacité nulle, la Mise en page demande les capacités et nomme chaque taille imprimée qui ne tient pas, avec le format et la géométrie du projet. DEC-084 ne change pas.

### I.15.3 Le front a des tests (DEC-118)

Le §I.12 écrivait « pas de test automatisé du front », et le §8 de `CLAUDE.md` « c'est le trou principal de la tranche ». Le motif — une dépendance de développement doit se justifier par ce qu'elle protège — est juste ; ce qu'elle protège est maintenant nommé.

**Décision : Vitest, en environnement Node, sur les modules purs.** Vitest est sous MIT, réutilise la configuration de Vite déjà présente, et ne demande rien d'autre. **Pas de bibliothèque de test de composants** (Testing Library, jsdom) : elle doublerait la chaîne de dépendances pour des composants qui, par I.0, ne portent pas de règle. Ce qui a de la logique est **extrait en fonction pure** et testé : le routage, la lecture des erreurs, le nom d'un fichier téléchargé, l'adresse d'une image, le tri et les doublons de la liste des projets, les capacités nulles. La CI lance `npm test` entre le lint et la construction.

Précise **DEC-114**, qui ne visait que les dépendances de production : Vitest n'entre pas dans l'image.

### I.15.4 Chaque code est traduit, et un test .NET le garantit

Le script de parité vérifie que `fr.json` et `en.json` ont les mêmes clés. Il ne vérifie pas qu'un code ajouté côté C# **a** une clé : un `STYLE_FROZEN` sans traduction s'afficherait en majuscules. **Un test de `Pawnsmith.Api.Tests` lit les deux catalogues** et vérifie qu'ils traduisent chaque code connu (la liste même de G.13 n° 2), les codes de génération que porte un job en échec, les codes propres au front, et chaque valeur des énumérations que l'écran affiche par clé — tailles, géométries, univers, statuts, états de job et de générateur, clauses, raisons de saut, profils d'archive.

### I.15.5 Trois reprises

- **Un rechargement raté ne remplace plus l'écran.** Un projet affiché qui ne se relit pas — serveur arrêté, réseau coupé — reste affiché, avec un bandeau qui dit l'erreur et propose de réessayer. Seul le **premier** chargement d'un projet remplace l'écran par son erreur. C'était le cinquième point non couvert du §8 de `CLAUDE.md`.
- **Le téléchargement d'une archive ne révoque plus son adresse dans la foulée** du clic : certains navigateurs n'ont pas encore commencé à lire le fichier. L'adresse est libérée un instant après.
- **Les cotes du format bordent l'aperçu** (§15.2) : largeur au-dessus, hauteur à côté, en millimètres, lues de la configuration. DEC-113 les avait renvoyées au panneau Mesures, faute de mieux ; elles tiennent en deux libellés.

### I.15.6 Tests attendus

| # | Ce qu'il vérifie | Où |
|---|---|---|
| 1 | Toute réponse — page, API, erreur, chemin inconnu — porte la politique, `X-Frame-Options`, `nosniff` et `Referrer-Policy` | .NET |
| 2 | La politique refuse les cadres tiers et les scripts en ligne, directive par directive | .NET |
| 3 | Les capacités rendent une ligne par taille, sur la calibration effective, avant tout candidat | .NET |
| 4 | Une taille qui ne tient pas vaut `0`, sans erreur ; un format inconnu rend `PAPER_FORMAT_UNKNOWN` | .NET |
| 5 | Chaque code et chaque valeur d'énumération affichée est traduit en `fr` et `en` | .NET |
| 6 | Le routage lit et écrit chaque route, dans les deux sens ; une adresse malformée ne lève pas | Vitest |
| 7 | Une erreur `{ code }` devient une `ApiError` ; un corps illisible `HTTP_{status}` ; un rejet de `fetch` `NETWORK_ERROR` | Vitest |
| 8 | Une écriture part en JSON, l'import brut en `application/zip` | Vitest |
| 9 | Le nom d'un fichier se lit de `Content-Disposition`, encodé ou non ; à défaut, le nom de repli | Vitest |
| 10 | L'adresse d'une image encode chaque segment du chemin stocké | Vitest |
| 11 | Les doublons d'identifiant de la liste des projets sont trouvés | Vitest |
| 12 | Les tailles imprimées de capacité nulle sont trouvées, et elles seules | Vitest |
| 13 | ESLint refuse `dangerouslySetInnerHTML` | Configuration, éprouvée par mutation |

### I.15.7 Découpage

| # | Tâche |
|---|---|
| 16 | Documentation : ce §I.15, DEC-116 à DEC-118, MEN-012 et MEN-013, bible v0.21 |
| 17 | En-têtes de sécurité ; ESLint interdit `dangerouslySetInnerHTML` |
| 18 | Capacités par taille ; la Mise en page nomme la taille qui ne tient pas |
| 19 | Le test .NET des traductions |
| 20 | Vitest, et les tests des modules purs ; la CI les lance |
| 21 | Les trois reprises du §I.15.5 |
| 22 | `CLAUDE.md`, README |
