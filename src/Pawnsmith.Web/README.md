# Pawnsmith.Web — organisation du front

Ce fichier existe pour une raison précise : le porteur du projet est développeur
.NET et relit intégralement le code (DEC-027). Une convention de rangement front
qui va de soi pour qui en fait tous les jours ne va pas de soi à la relecture.

## L'arborescence

```
src/
├── main.tsx              Point d'entrée. Monte React dans le DOM, et rien d'autre.
├── app/                  La coquille de l'application : ce qui entoure les écrans.
│   └── App.tsx
├── components/           Composants réutilisables, sans logique métier.
│   └── LanguageSelector.tsx
├── i18n/                 Câblage de la traduction et catalogues.
│   ├── config.ts
│   └── locales/
│       ├── fr.json
│       └── en.json
└── styles/
    └── global.css        Le seul style global : remise à zéro et cadre de page.
```

Trois choix, avec leur motif.

**`main.tsx` reste à la racine.** C'est le fichier que `index.html` désigne par
son chemin ; le déplacer obligerait à modifier deux fichiers pour une seule
idée. C'est aussi la convention Vite, donc ce qu'un repreneur cherchera en
premier.

**Pas de fichier `index.ts` qui réexporte** (ce qu'on appelle un *barrel* dans
l'écosystème JavaScript). Un `import './i18n'` qui résout vers
`i18n/index.ts` économise trois caractères et coûte une question à chaque
relecture : quel fichier, au juste ? On écrit `./i18n/config`. C'est la même
exigence que « aucun code implicite ou magique » du §0 du cahier des charges.

**Pas de dossier vide en prévision de T6.** L'interface est livrée en T6, avec
cinq écrans (chapitre 15 de la bible). Ils prendront place dans un dossier
`features/`, un par écran, à côté de ceux ci-dessus. Ce dossier **n'est pas
créé aujourd'hui** : les instructions du projet interdisent d'anticiper une
tranche à venir. La phrase ci-dessus tient lieu d'intention ; le dossier arrive
avec son premier fichier.

## Ce que le front ne porte pas encore

- **Aucun test.** Le front de T1 est une coquille : un titre, une accroche, un
  sélecteur de langue. Les tests d'interface arrivent avec ce qu'ils testent,
  en T6.
- **Aucun linter.** ESLint est la convention de l'écosystème, mais il ajoute une
  poignée de dépendances de développement, et le §A.2 exige que chaque
  dépendance soit justifiée dans le commit qui l'introduit. À trancher avant
  T6, pas dans un commit de rangement.
- **Aucun style par composant.** Une seule feuille globale suffit à ce que le
  front contient. Le jour où les écrans arrivent, le choix entre modules CSS,
  fichier par composant ou une autre approche appartient à T6.

## Les vérifications

```bash
npm run build
```

`build` lance `tsc --noEmit` **avant** Vite : les erreurs de typage font échouer
la construction, elles ne sont pas seulement signalées dans l'éditeur. Le
`tsconfig.json` est en mode `strict`, avec `noUnusedLocals`,
`noUnusedParameters` et `noUncheckedIndexedAccess`.
