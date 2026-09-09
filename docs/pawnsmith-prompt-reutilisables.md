# Pawnsmith — Prompts réutilisables

Prompts de démarrage pour les séances récurrentes. À copier-coller et compléter.

---

## Dans le projet (conception, arbitrage, spécification)

### Spécifier une nouvelle tranche

```
On attaque la spécification fine de la tranche T__.

Avant de rédiger : dis-moi quelles questions du chapitre 16 de
pawnsmith-bible.md doivent être tranchées pour cette tranche, et pose-les-moi.
Ne rédige rien tant qu'elles ne sont pas résolues.

Ensuite, produis un cahier des charges au même format que celui de T1 :
périmètre, hors périmètre explicite, spécification détaillée, tests attendus,
critères d'acceptation vérifiables.
```

### Débriefer le protocole T0

```
Voici les résultats du protocole T0.

[coller la fiche de synthèse]
[coller le verdict de T0a et le prompt de référence]

Mets à jour config/calibration.json, tranche DEC-003 en conséquence, et
dis-moi ce que ça change — ou pas — pour T1 et T4.

Vérifie aussi que chaque hauteur retenue respecte le plafond de DEC-032 :
2 × (hauteur + appendice) ≤ 245 mm sur US Letter.
```

### Ajouter une décision à la bible

```
Décision prise pendant une session de code, à consigner :

Contexte : ...
Choix retenu : ...
Alternatives écartées : ...

Rédige la fiche DEC correspondante, et dis-moi si elle contredit une décision
existante. Si oui, précise laquelle elle supersède.
```

### Faire challenger une idée

```
J'envisage : [idée]

Avant de m'aider à la mettre en œuvre : dis-moi ce qui ne va pas dedans.
Ce qu'elle coûte, ce qu'elle casse ailleurs, ce que je n'ai pas vu. Et
propose une alternative si tu en as une meilleure.
```

### Relire un livrable de Claude Code

```
Claude Code vient de livrer la tranche T__. Voici ce qu'il a produit :

[coller le résumé, ou les fichiers clés]

Confronte ça au cahier des charges. Qu'est-ce qui manque, qu'est-ce qui a
dérivé, qu'est-ce qui a été ajouté sans être demandé ? Et y a-t-il des
décisions structurantes prises en cours de route qui mériteraient une
fiche DEC ?
```

### Mettre à jour l'état du projet

> `pawnsmith-etat-du-projet.md` est supprimé depuis la bible v0.10 : il était
> périmé partout, et le dépôt porte déjà l'avancement. Ce qui reste de lui est
> le chapitre 16 de la bible, les questions ouvertes.

```
Mets à jour la §8 de CLAUDE.md et le chapitre 16 de pawnsmith-bible.md à
partir de ce qui s'est passé depuis la dernière séance : avancement des
tranches, versions des documents de référence, questions tranchées et
questions nouvelles.
```

### Préparer un paquet de consignes pour Claude Code

```
À partir de ce qu'on vient de trancher, rédige les consignes de mise à jour
pour Claude Code : une section par tâche, chacune close par un commit, avec
le motif de chaque changement et pas seulement l'instruction.

Ajoute une section « ce qu'il ne faut PAS faire », et une section rappelant
les fiches DEC concernées — il doit comprendre, pas exécuter.
```

---

## Chez Claude Code (implémentation)

> **Rappel de circulation.** Claude Code lit le **dépôt**, pas la base de
> connaissance du projet. Tout document qu'il doit consulter doit d'abord
> exister dans `docs/`. À l'inverse, les consignes ponctuelles de séance ne
> vont **jamais** dans le dépôt : elles se collent dans la conversation. Un
> document éphémère versionné devient un document à maintenir pour toujours.

### Démarrer une tranche

```
Lis docs/pawnsmith-bible.md et docs/pawnsmith-cahier-des-charges-t__.md
intégralement avant d'écrire quoi que ce soit.

Implémente UNIQUEMENT la tranche T__. Ne déborde sur aucune tranche
ultérieure, même si ça semble pratique.

Travaille par petites tâches, une par commit, en Conventional Commits. Je
relis commit par commit : rien de magique, rien d'implicite, et justifie
tes choix non évidents dans les messages.

Aucun commit n'est créé ni poussé sans ma relecture préalable. Tu me montres
le diff et tu attends.

Si une information manque, arrête-toi et demande. N'invente jamais une
valeur physique.

Aucune nouvelle dépendance sans me demander d'abord. Si j'en valide une,
ajoute-la à THIRD-PARTY-NOTICES.md dans le même commit.

Commence par me proposer ton découpage en tâches, avant de coder.
```

### Séance de mise à jour documentaire

À utiliser quand la conception a bougé et que le dépôt doit rattraper. Déposer
d'abord les fichiers concernés dans l'arbre de travail, **sans les committer**,
puis :

```
Séance de mise à jour documentaire. Aucun code métier, et tu ne démarres
aucune tranche.

J'ai déposé dans l'arbre de travail, sans les committer, les nouvelles
versions des documents de référence. Les consignes complètes sont collées
ci-dessous : lis-les intégralement avant d'agir.

Ordre de travail : [préciser, en commençant par ce qui apporte de
l'information plutôt que par ce qui modifie des fichiers].

Une section = un commit. Tu me montres le diff, tu attends ma relecture avant
de committer, et tu ne pousses rien de toi-même.

Si une consigne contredit ce que tu vois dans le dépôt, tu t'arrêtes et tu me
le dis plutôt que de choisir.

[coller le contenu du document de consignes]
```

### Reprendre après une pause

```
Lis CLAUDE.md et docs/pawnsmith-cahier-des-charges-t__.md.

Fais-moi le point : ce qui est fait, ce qui reste sur la tranche en cours,
et l'état des tests. Ne code rien tant que je n'ai pas validé la suite.
```

### Expliquer du code livré

```
Explique-moi [fichier / méthode], en partant du principe que je relis sans
avoir écrit une ligne. Dis-moi ce que ça fait, pourquoi c'est écrit comme
ça plutôt qu'autrement, et ce qui casserait si je changeais [X].
```