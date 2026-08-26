# Pawnsmith

Pawnsmith est une application web auto-hébergée qui produit des **planches PDF
calibrées pour l'impression domestique**, destinées à fabriquer des figurines de
jeu de rôle en carton (« pions », *standees*).

À partir de paramètres de haut niveau — race, classe, taille, équipement — elle
compose des prompts déterministes, pilote un modèle de diffusion **local**
(ComfyUI), détoure les images produites, et les met en page en grille uniforme
avec les repères d'impression nécessaires à une découpe correcte.

Sa propriété centrale est la **cohérence visuelle** : toutes les figurines d'un
même projet partagent un style verrouillé à la création, et le couple
recto/verso d'un même personnage est produit en une seule génération.

> **État d'avancement.** Ce dépôt en est aux **fondations** (partie A du cahier
> des charges) : structure, chaîne de compilation, conteneur, intégration
> continue, squelette de front localisé. Les projets .NET sont **vides**.
> Aucune fonctionnalité n'est encore implémentée.

Voir [`docs/pawnsmith-bible.md`](docs/pawnsmith-bible.md) pour la vision, le
modèle de données et le journal des décisions, et
[`docs/pawnsmith-cahier-des-charges-t1.md`](docs/pawnsmith-cahier-des-charges-t1.md)
pour la spécification des fondations et de la première tranche.

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

Hors conteneur, l'API sert son propre `wwwroot`, qui est vide : c'est le serveur
Vite qui affiche le front. Pour vérifier l'assemblage réel — l'API servant le
front compilé, comme en production — passer par le conteneur.

---

## Lancement des tests

```bash
dotnet test Pawnsmith.sln
```

Le front n'a pas encore de tests ; sa compilation vaut vérification :

```bash
cd src/Pawnsmith.Web && npm run build
```

---

## Lancement en conteneur

Construction de l'image :

```bash
docker build -t pawnsmith .
```

Forme canonique de lancement :

```bash
docker run --rm -p 127.0.0.1:8080:8080 -v pawnsmith-projects:/app/data/projects -v pawnsmith-logs:/app/data/logs pawnsmith
```

L'application est alors sur <http://127.0.0.1:8080>.

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

Les deux volumes sont distincts et le restent : les journaux contiennent des
prompts, des chemins absolus et l'URL du générateur, et ne doivent jamais
repartir dans l'archive d'un projet partagé (DEC-022).

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
│   └── Pawnsmith.Web/              # front React + TypeScript
├── tests/
├── tools/Pawnsmith.Cli/            # harnais jetable, non livré
└── Dockerfile
```

**La règle de dépendance est stricte et sans exception** : `Domain` ne référence
rien, `Application` référence `Domain`, `Infrastructure` référence les deux,
`Api` référence tout. Aucune flèche en sens inverse, jamais.

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
