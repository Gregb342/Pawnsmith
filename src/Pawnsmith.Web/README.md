# Pawnsmith.Web — organisation du front

Ce fichier existe pour une raison précise : le porteur du projet est développeur
.NET et relit intégralement le code (DEC-027). Une convention de rangement front
qui va de soi pour qui en fait tous les jours ne va pas de soi à la relecture.

## L'arborescence

```
src/
├── main.tsx              Point d'entrée. Monte React dans le DOM, et rien d'autre.
├── api/
│   ├── client.ts         Une fonction par route de l'API ; le seul fichier qui appelle fetch.
│   └── types.ts          Les DTO de l'API, écrits à la main en regard des records C#.
├── app/                  La coquille : routage, données partagées, enregistrement.
│   ├── App.tsx           Barre du haut, étapes, aiguillage des écrans.
│   ├── router.ts         Le routage sur le fragment d'adresse (#/…), écrit à la main.
│   ├── AppData.tsx       Configuration, catalogue, styles : chargés une fois.
│   ├── ProjectData.tsx   Le projet ouvert, tel que l'API l'a rendu en dernier.
│   ├── SaveStatus.tsx    L'indicateur d'enregistrement automatique (DEC-111).
│   ├── capacity.ts       Fonctions pures, testées : les tailles qui ne tiennent pas.
│   └── projects.ts       … les identifiants partagés de la liste des projets.
├── components/           Composants réutilisables, sans logique métier.
├── screens/              Un fichier par écran : les cinq étapes et quatre pages.
├── i18n/                 Câblage de la traduction et catalogues fr et en.
└── styles/
    └── global.css        Les jetons (couleurs, rayons, polices) et les classes communes.
```

Les tests sont **à côté de leur module**, en `*.test.ts` : `router.test.ts` à
côté de `router.ts`.

Quatre choix, avec leur motif.

**`main.tsx` reste à la racine.** C'est le fichier que `index.html` désigne par
son chemin ; le déplacer obligerait à modifier deux fichiers pour une seule
idée. C'est aussi la convention Vite, donc ce qu'un repreneur cherchera en
premier.

**Pas de fichier `index.ts` qui réexporte** (ce qu'on appelle un *barrel* dans
l'écosystème JavaScript). Un `import './i18n'` qui résout vers
`i18n/index.ts` économise trois caractères et coûte une question à chaque
relecture : quel fichier, au juste ? On écrit `./i18n/config`. C'est la même
exigence que « aucun code implicite ou magique » du §0 du cahier des charges.

**Ce qui a de la logique est une fonction pure, hors du composant.** Un
composant affiche ce que l'API rend (§I.0 du cahier T6 front) ; quand il faut
tout de même trier, compter ou choisir, la règle va dans un fichier `.ts` sans
React, et elle a son test (DEC-118). `capacity.ts` et `projects.ts` sont nés
ainsi.

**Une seule feuille de style, sur des jetons.** Les couleurs, rayons et polices
sont des variables CSS sur `:root` : un système de design (D8) les remplacera
sans toucher aux composants.

## Les vérifications

```bash
npm run lint
npm test
npm run build
```

`lint` lance **ESLint**, configuré dans `eslint.config.js`, puis le script de
parité des catalogues (`scripts/check-i18n.mjs`) : `fr.json` et `en.json` ont
exactement les mêmes clés, aucune vide. ESLint ne fait pas double emploi avec
`tsc` : le vérificateur de types prouve que le code est **cohérent**, pas qu'il
est du **React correct**. La règle des hooks est l'exemple qui justifie l'outil
à lui seul — un hook appelé dans une condition ou une boucle compile
parfaitement et se comporte mal à l'exécution, par intermittence. Une règle de
plus refuse `dangerouslySetInnerHTML`, la seule façon d'insérer du HTML brut
dans la page (MEN-013, DEC-116).

`test` lance **Vitest** (DEC-118), en environnement Node, sur les modules purs :
routage, lecture des erreurs, construction des requêtes, nom d'un fichier
téléchargé, liste des projets, capacités. `fetch` y est remplacé par un faux
qui enregistre ce qu'on lui demande ; `Response` et `File` sont ceux de Node.
Les écrans ne sont pas testés ici : ils ne portent pas de règle, et ils se
vérifient dans un navigateur réel contre l'API.

`build` lance `tsc --noEmit` **avant** Vite : les erreurs de typage font échouer
la construction, elles ne sont pas seulement signalées dans l'éditeur. Le
`tsconfig.json` est en mode `strict`, avec `noUnusedLocals`,
`noUnusedParameters` et `noUncheckedIndexedAccess`.

Les trois tournent dans l'intégration continue, dans cet ordre.

Ce qui garantit que **chaque code d'erreur de l'API a sa traduction** n'est pas
ici mais côté .NET : `tests/Pawnsmith.Api.Tests/TranslationTests.cs` lit les
deux catalogues, parce que c'est là que la liste des codes existe.
