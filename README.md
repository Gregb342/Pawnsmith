# Pawnsmith

Pawnsmith est une application web auto-hébergée qui produit des **planches PDF
calibrées pour l'impression domestique**, destinées à fabriquer des figurines de
jeu de rôle en carton (« pions », *standees*).

À partir de paramètres de haut niveau — race, classe, taille, équipement — elle
compose des prompts déterministes, pilote un modèle de diffusion **local**
(ComfyUI), détoure les images produites, et les met en page en grille uniforme
avec les repères d'impression nécessaires à une découpe correcte.

Sa propriété centrale est la **cohérence visuelle** : toutes les figurines d'un
même projet partagent le style du projet, et le couple recto/verso d'un même
personnage est produit en une seule génération. L'univers et le style se figent
à la première proposition ; pour un autre style, on duplique le projet
(DEC-112). Une proposition dont le prompt a bougé depuis sa création — un
gabarit retouché, un workflow changé — est signalée « prompt modifié depuis »
plutôt qu'interdite (DEC-105).

> **État d'avancement.** Les **fondations** (partie A), la tranche **T1**
> — moteur de mise en page et rendu PDF —, la tranche **T2** — modèle de
> projet, persistance, archives —, la tranche **T3** — composition de la
> clause sujet, catalogue, règles de gestion — et la tranche **T4** — client
> du générateur ComfyUI, lots de candidats — sont écrites, ainsi que la tranche
> **T6** — l'API puis l'**interface** —, la tranche **T7** — journaux,
> visualiseur, revue du modèle de menace — et la tranche **T5** — détourage
> sans modèle, sur fond uni. **1 073 tests verts.**
>
> L'**interface** est complète, en français et en anglais : projets, gabarits,
> génération auprès d'un ComfyUI, détourage, mise en page avec l'aperçu du
> PDF, impression, catalogue personnel, réglage du générateur, journaux. Elle
> n'a **jamais été éprouvée contre un vrai ComfyUI** ni sur papier : les
> sessions de développement n'en avaient pas. Le harnais en ligne de commande
> de `tools/` reste là pour les mesures de T0b.
> Le détail tranche par tranche est dans le §8 de [`CLAUDE.md`](CLAUDE.md).

Voir [`docs/pawnsmith-bible.md`](docs/pawnsmith-bible.md) pour la vision, le
modèle de données et le **journal des décisions**, qui fait foi ; les cahiers
des charges [T1](docs/pawnsmith-cahier-des-charges-t1.md),
[T2](docs/pawnsmith-cahier-des-charges-t2.md),
[T3](docs/pawnsmith-cahier-des-charges-t3.md),
[T4](docs/pawnsmith-cahier-des-charges-t4.md),
[T5](docs/pawnsmith-cahier-des-charges-t5.md),
[T6](docs/pawnsmith-cahier-des-charges-t6.md) (l'API),
[T6, le front](docs/pawnsmith-cahier-des-charges-t6-front.md) et
[T7](docs/pawnsmith-cahier-des-charges-t7.md) pour les spécifications
détaillées.

---

## Prérequis

| Outil | Version | Nécessaire pour |
|---|---|---|
| [SDK .NET](https://dotnet.microsoft.com/download) | **10.0** (LTS) | Compilation et tests du back |
| [Node.js](https://nodejs.org/) | **22 LTS** | Compilation du front |
| [Docker](https://docs.docker.com/get-docker/) | récent | Lancement en conteneur |

Le lancement en conteneur ne demande que Docker.

---

## Lancement en développement, hors conteneur

Deux processus, dans deux terminaux.

**Le front**, servi par Vite avec rafraîchissement à chaud :

```bash
cd src/Pawnsmith.Web && npm install && npm run dev
```

**L'API** :

```bash
dotnet run --project src/Pawnsmith.Api
```

En développement, `appsettings.Development.json` fait lire à l'API les dossiers
du dépôt : `config/` pour la calibration, `data/projects/` pour les projets
(ignoré par git). Hors conteneur, l'API sert son propre `wwwroot`, qui est
vide : c'est le serveur Vite qui affiche le front, sur `http://localhost:5173`,
et qui passe les appels `/api` à l'API, sur le port 8080. Pour vérifier l'assemblage réel — l'API servant le
front compilé, comme en production — passer par le conteneur.

---

## Lancement des tests

```bash
dotnet test Pawnsmith.sln
```

Le front n'a pas encore de tests — ils arrivent avec l'interface, en T6. Son
analyse statique et sa compilation valent vérification :

```bash
cd src/Pawnsmith.Web && npm run lint && npm run build
```

---

## Produire une planche

C'est le livrable réel de T1, et la seule chose que l'application sache faire
de bout en bout aujourd'hui :

```bash
dotnet run --project tools/Pawnsmith.Cli -- sheet --manifest ./manifeste.json --calibration ./config/calibration.json --out ./planche.pdf
```

Ajouter `--debug` imprime « tête » et « pieds » dans chaque panneau : diagnostic
seulement, jamais sur une planche destinée au ciseau.

**Manipuler un projet** — les quatre sous-commandes de T2. Toutes demandent
`--calibration`, `check` compris : les diagnostics relationnels en dépendent.

```bash
CLI="dotnet run --project tools/Pawnsmith.Cli --"

$CLI project new    --root ./data/projects --name "Donjon" --geometry TabAndSocket --paper-format A4 --calibration ./config/calibration.json
$CLI project check  --path ./data/projects/donjon --calibration ./config/calibration.json
$CLI project export --path ./data/projects/donjon --profile Share --out ./data/archives --calibration ./config/calibration.json
$CLI project import --archive ./data/archives/donjon-share-202609091309.zip --root ./data/projects --name "Donjon restauré" --calibration ./config/calibration.json
```

`check` affiche les erreurs et les diagnostics **séparément**, parce que c'est
exactement la distinction de DEC-056 : une erreur est rendue *à la place* d'un
projet, un diagnostic *avec* lui. `export` liste les entrées de l'archive, parce
qu'une liste blanche se croit quand on la voit.

**Composer des gabarits** — les sous-commandes de T3. `add` et `edit` demandent
le template et le catalogue de l'univers, dans `config/` ; une valeur que le
catalogue ne connaît pas est insérée telle quelle et **signalée**, jamais
refusée (DEC-056).

```bash
CLI="dotnet run --project tools/Pawnsmith.Cli --"
P=./data/projects/donjon
CFG="--template ./config/prompt-template.fantasy.json --catalog ./config/catalog.fantasy.json --calibration ./config/calibration.json"

$CLI blueprint add    --path $P --race goblin --class skirmisher --size Medium --param weapon=spear --param armour=leather --details "one ear torn" --quantity 6 $CFG
$CLI blueprint edit   --path $P --id <guid> --race orc --class skirmisher --size Medium --param weapon=axe $CFG
$CLI blueprint clause --path $P --id <guid> --clause "a scarred orc skirmisher with a notched axe" --calibration ./config/calibration.json
$CLI blueprint elect  --path $P --id <guid> --candidate <guid> --calibration ./config/calibration.json
$CLI blueprint remove --path $P --id <guid> --calibration ./config/calibration.json
$CLI project sheet    --path $P --out ./planche.pdf --calibration ./config/calibration.json
```

`edit` recompose la clause sujet **tant que personne ne l'a éditée à la main**,
et la laisse intacte sinon — l'édition se déduit, elle n'est pas stockée
(DEC-067). `project sheet` tire la planche du projet et **nomme** chaque gabarit
qu'elle a laissé de côté faute de candidat élu (DEC-069).

**Générer des candidats** — les sous-commandes de T4. Il faut un ComfyUI qui
tourne, et un `config/workflow.comfyui.json` **exporté de ta propre machine** :
le dépôt n'en livre qu'un exemple, `config/workflow.comfyui.example.json`, et
`config/README.md` dit comment fabriquer le vrai.

```bash
GEN="--workflow ./config/workflow.comfyui.json --generator-url http://127.0.0.1:8188"

$CLI generator check    $GEN
$CLI candidate generate --path $P --id <guid> --count 4 $GEN --calibration ./config/calibration.json
```

`generator check` affiche la clause de cadrage et l'état du générateur ; un
générateur éteint est un état, pas une erreur. `candidate generate` affiche
chaque transition du lot ; chaque candidat est sauvegardé dès qu'il existe, et
`Ctrl+C` annule le lot en gardant ce qui a été produit (DEC-074, DEC-075).

**Détourer** — les sous-commandes de T5. Le lot détoure déjà chaque candidat
qu'il produit ; un détourage raté n'arrête pas le lot, il est nommé dans son
compte rendu (DEC-101). `candidate cutout` redétoure un candidat existant, et
`cutout` détoure une image jumelée quelconque, hors de tout projet — c'est
l'outil pour régler les valeurs de `CutoutOptions` sur de vraies images.

```bash
$CLI candidate cutout --path $P --id <guid> --candidate <guid> --calibration ./config/calibration.json
$CLI cutout           --pair ./paire.png --out ./detourage
```

Le détourage ne demande **aucun modèle** (DEC-098) : il exige le fond uni que
la clause de cadrage impose, et le refuse s'il ne le trouve pas
(`CUTOUT_BACKGROUND_NOT_UNIFORM`).

---

## Lancement en conteneur

Construction de l'image :

```bash
docker build -t pawnsmith .
```

Forme canonique de lancement :

```bash
docker run --rm -p 127.0.0.1:8080:8080 -v pawnsmith-projects:/app/data/projects -v pawnsmith-logs:/app/data/logs -v pawnsmith-user:/app/data/user pawnsmith
```

L'application est alors sur <http://127.0.0.1:8080>.

Le parcours suit les cinq étapes en haut de l'écran, dans l'ordre du
pipeline, sans être un assistant : on revient à une étape sans rien perdre,
et chaque champ s'enregistre quand on le quitte (DEC-111).

1. **Projet** — nom, géométrie, format de papier, style (une bibliothèque
   pour démarrer).
2. **Gabarits** — race, classe, taille, quantité, équipement, choisis dans le
   catalogue ; « Autre… » ajoute un objet complet au catalogue personnel.
3. **Génération** — un lot de propositions par gabarit, détourées dans la
   foulée ; on en retient une par gabarit.
4. **Mise en page** — l'aperçu est le PDF lui-même ; la capacité se compte en
   cellules.
5. **Impression** — la langue des légendes, les vérifications avant
   découpe, le téléchargement.

> ### ⚠️ Pawnsmith n'a aucune authentification
>
> C'est un choix de conception assumé : Pawnsmith est mono-utilisateur et
> auto-hébergé. Il n'y a ni comptes, ni mots de passe, ni cloisonnement, et il
> n'y en aura pas.
>
> **La conséquence est directe : ne publiez jamais le port sur toutes les
> interfaces.** `-p 8080:8080` expose l'application, sans le moindre contrôle
> d'accès, à tout ce qui peut joindre la machine. Le préfixe `127.0.0.1:` de la
> commande ci-dessus n'est pas décoratif — il est la seule chose qui protège
> l'instance. Pour un accès distant, passez par un tunnel SSH ou un
> reverse proxy assurant lui-même l'authentification. (MEN-004)

Les trois volumes sont distincts et le restent : les journaux contiennent des
chemins absolus, l'URL du générateur et des messages d'erreur qui nomment vos
projets, et ne doivent jamais repartir dans l'archive d'un projet partagé
(DEC-022). Le dossier utilisateur, `/app/data/user`, garde ce que vous
réglez une fois pour tous vos projets : le catalogue personnel, les styles
personnels et l'adresse du générateur (DEC-107, DEC-108, DEC-110). Aucune
archive ne l'emporte.

En conteneur, Pawnsmith écrit à chaque démarrage un **avertissement** sur
son adresse d'écoute : un conteneur écoute forcément sur toutes ses
interfaces, et ne peut pas voir comment son port est publié. L'avertissement
vous demande de vérifier la publication ; avec la commande ci-dessus, elle est
bonne (DEC-093).

### Les journaux

Une ligne JSON par événement, dans `/app/data/logs` : un fichier par jour,
`pawnsmith-AAAAMMJJ.ndjson`, un de plus tous les 50 Mio, les 31 plus récents
gardés (DEC-091). Aucun prompt n'y est écrit exprès (DEC-092).

| Variable | Défaut | Rôle |
|---|---|---|
| `Pawnsmith__Logs__Enabled` | `true` | `false` : aucun fichier écrit. La console d'ASP.NET reste réglée par `Logging__Console__LogLevel__Default` |
| `Pawnsmith__Logs__RetainedFileCount` | `31` | Nombre de fichiers gardés |
| `Pawnsmith__Logs__FileSizeLimitBytes` | `52428800` | Taille d'un fichier avant le suivant |
| `Logging__LogLevel__Default` | `Information` | Niveau, pour la console comme pour les fichiers |

La page **Journaux** de l'interface les lit ; l'API les sert aussi :

```bash
curl -s http://127.0.0.1:8080/api/logs
curl -s "http://127.0.0.1:8080/api/logs/pawnsmith-20261003.ndjson?lines=50"
```

### Brancher le générateur

**Depuis l'interface**, page Générateur : l'adresse s'y saisit, se teste, et
s'enregistre dans le dossier utilisateur (DEC-108). Le **workflow**, lui, reste
un fichier : l'export « Export (API) » de votre ComfyUI, monté dans
`/app/config/` (voir [`config/README.md`](config/README.md)).

Ou par variables d'environnement, au mécanisme standard d'ASP.NET (DEC-087) :

```bash
docker run --rm -p 127.0.0.1:8080:8080 \
  -e Pawnsmith__Generator__Url=http://192.168.1.20:8188 \
  -v "$PWD/config/workflow.comfyui.json:/app/config/workflow.comfyui.json:ro" \
  -v pawnsmith-projects:/app/data/projects -v pawnsmith-logs:/app/data/logs -v pawnsmith-user:/app/data/user pawnsmith
```

Sans adresse, la génération n'est pas configurée et tout le reste fonctionne ;
une adresse ou un workflow refusés laissent l'application démarrer, et
`GET /api/generator` dit pourquoi. **Une adresse enregistrée depuis
l'interface l'emporte sur la variable d'environnement** au démarrage suivant :
c'est le dernier choix explicite. Une adresse avec identifiants, requête ou
fragment est refusée (`GENERATOR_ADDRESS_INVALID`).

### L'API

L'interface n'utilise que l'API, qui s'essaie aussi en `curl`. Toutes les
routes sont sous `/api`, décrites au §G.6 du cahier T6 et, pour celles qu'a
ajoutées le front, aux §I.4 à I.9 de son cahier. Une erreur rend
`{ "code": "…" }` et rien d'autre (DEC-084).

```bash
API=http://127.0.0.1:8080/api

curl -s $API/configuration
curl -s -X POST $API/projects -H 'Content-Type: application/json' \
     -d '{"name":"Donjon","universe":"Fantasy","geometry":"TabAndSocket","paperFormat":"A4"}'
curl -s -X POST $API/projects/donjon/blueprints -H 'Content-Type: application/json' \
     -d '{"race":"goblin","characterClass":"skirmisher","size":"Medium","optionalParameters":[{"key":"weapon","value":"spear"}],"details":"","quantity":6}'
curl -s -X POST $API/projects/donjon/blueprints/<id>/jobs -H 'Content-Type: application/json' -d '{"count":4}'
curl -s $API/jobs
curl -s -X POST $API/projects/donjon/blueprints/<id>/candidates/<candidateId>/cutout
curl -s $API/projects/donjon/sheet/report
curl -s -o planche.pdf "$API/projects/donjon/sheet.pdf?culture=fr"
```

Une requête qui écrit, envoyée par une page d'une autre origine, est refusée,
et seuls les noms d'hôte locaux sont acceptés (MEN-010). Pour publier
l'application sur son réseau, il faut ajouter son nom d'hôte à
`AllowedHosts` — en connaissance de cause.

---

## Structure du dépôt

```
pawnsmith/
├── config/calibration.json         # valeurs physiques — aucune constante dans le code
├── docs/                           # bible du projet et cahier des charges
├── src/
│   ├── Pawnsmith.Domain/           # pur, ne référence rien
│   ├── Pawnsmith.Application/      # cas d'usage, ports          → Domain
│   ├── Pawnsmith.Infrastructure/   # PDFsharp, disque, Serilog    → Application, Domain
│   ├── Pawnsmith.Api/              # ASP.NET Core, sert le front  → tout
│   └── Pawnsmith.Web/              # front React + TypeScript (voir son README)
├── tests/                          # un projet de test par couche testée, plus l'API
├── tools/Pawnsmith.Cli/            # harnais jetable, non livré
└── Dockerfile
```

**La règle de dépendance est stricte et sans exception** : `Domain` ne référence
rien, `Application` référence `Domain`, `Infrastructure` référence les deux,
`Api` référence tout. Aucune flèche en sens inverse, jamais — et c'est le
compilateur qui le tient, par les références de projet.

À l'intérieur de chaque projet, les fichiers sont rangés en **dossiers
thématiques** dont les namespaces suivent le chemin. Le domaine se range par
sujet (`Primitives`, `PhysicalValues`, `Units`, `Sheets`, `Projects`,
`Prompts`), l'infrastructure par technologie d'adaptateur (`Json`, `Imaging`,
`Pdf`, `Projects`, `Cutout`). Le §A.3 du cahier T1 en donne la règle et les deux pièges.

---

## Contribuer

- Commits au format [Conventional Commits](https://www.conventionalcommits.org/)
  (`feat:`, `fix:`, `docs:`, `test:`, `chore:`, `refactor:`, `build:`, `ci:`).
- Versionnement sémantique, à partir de `0.1.0`.
- Messages de commit et commentaires de code **en anglais** ; documentation
  fonctionnelle **en français**.
- Toute nouvelle dépendance doit être justifiée dans le message du commit qui
  l'introduit et ajoutée à [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md)
  dans le même commit. En cas de doute entre une dépendance et vingt lignes de
  code, écrire les vingt lignes.

[`CLAUDE.md`](CLAUDE.md) résume la méthode de travail et la politique de
dépendances à l'usage des assistants de code.

---

## Licence

[MIT](LICENSE). Les composants tiers et leurs licences sont listés dans
[`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md).
