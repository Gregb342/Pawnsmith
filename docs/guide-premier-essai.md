# Guide du premier essai

Ce guide mène de zéro à une planche imprimée, contre **ton** ComfyUI. Il
suppose seulement que le workflow de T0a — Krea 2 Turbo, image jumelée face et
dos — tourne déjà dans ComfyUI.

Compte une demi-heure, génération comprise. Tout ce qui coince va dans le
dernier chapitre, « Si quelque chose coince ».

---

## 1. Ce qu'il te faut

| Pour | Il faut |
|---|---|
| Le lancement **sans Docker** (recommandé pour ce premier essai) | le [SDK .NET 10](https://dotnet.microsoft.com/download) et [Node.js 22](https://nodejs.org/) |
| Le lancement **avec Docker** | Docker Desktop, qui tourne |
| Générer | ComfyUI qui tourne, avec le workflow de T0a |
| Imprimer | une imprimante, du papier de 250 g/m² si possible |

**Pourquoi sans Docker d'abord :** ComfyUI tourne sur la même machine, et
Pawnsmith le joint alors sur `127.0.0.1` sans aucun réglage. Depuis un
conteneur, il faut en plus que ComfyUI écoute au-delà de la boucle locale
(voir §3.2).

---

## 2. Récupérer le code

```powershell
git fetch origin
git checkout main
git pull
```

---

## 3. Lancer Pawnsmith

### 3.1 Sans Docker

Depuis la racine du dépôt, dans PowerShell :

```powershell
powershell -ExecutionPolicy Bypass -File scripts\start.ps1
```

Sous Linux ou macOS : `scripts/start.sh`.

Le script compile l'interface, puis lance l'application. Il affiche
`Starting Pawnsmith on http://127.0.0.1:8080` : ouvre cette adresse dans ton
navigateur. `Ctrl+C` arrête tout.

Tes données vont dans `data\`, à la racine du dépôt — projets, journaux, et
`data\user\` pour tes réglages (adresse de ComfyUI, workflow importé, catalogue
et styles personnels). Rien de tout ça n'est suivi par git.

### 3.2 Avec Docker

```powershell
docker compose up --build
```

Puis <http://127.0.0.1:8080>. Les données vivent dans trois volumes Docker et
survivent aux redémarrages.

Depuis le conteneur, ta machine s'appelle `host.docker.internal` : l'adresse de
ComfyUI est réglée sur `http://host.docker.internal:8188`. **ComfyUI doit alors
écouter au-delà de la boucle locale** : lance-le avec l'option `--listen`
(édition portable : ajoute `--listen` à la ligne de `run_nvidia_gpu.bat`).

---

## 4. Brancher ComfyUI

Clique sur l'état de ComfyUI, en haut à droite (« ComfyUI mal configuré » au
premier lancement) : c'est la page **Générateur**.

### 4.1 L'adresse

Dans **Adresse de ComfyUI**, mets celle de ton ComfyUI, puis **Enregistrer et
tester** :

| Ton ComfyUI | Adresse |
|---|---|
| Édition portable, ou lancé en ligne de commande | `http://127.0.0.1:8188` |
| Application de bureau ComfyUI | `http://127.0.0.1:8000` |
| Pawnsmith en Docker | `http://host.docker.internal:8188` (ou `:8000`) |

L'adresse est gardée : elle l'emporte sur la configuration aux lancements
suivants.

### 4.2 Le workflow

1. Dans ComfyUI, ouvre le workflow de T0a, celui qui produit **la face à
   gauche et le dos à droite** dans une seule image.
2. Menu **Workflow → Export (API)**. Sur une ancienne version de ComfyUI, il
   faut d'abord cocher « Enable Dev mode Options » dans les réglages, puis
   utiliser le bouton **Save (API Format)**. Garde le fichier `.json` obtenu.
3. Dans Pawnsmith, panneau **Workflow ComfyUI**, **Choisir le fichier
   exporté…**, et donne-lui ce fichier.

Pawnsmith place lui-même le prompt et la graine dans le graphe, et ajoute la
clause de cadrage de T0a — celle qui impose la pose, le fond gris uni et les
deux vues. **Tu n'as rien à modifier dans l'export.** L'état doit passer à
« ComfyUI connecté », et le panneau dire « Importé depuis cette page ».

> Le prompt que tu avais tapé dans ComfyUI est remplacé à chaque génération par
> celui de Pawnsmith. Les dimensions, le nombre d'étapes et le modèle, eux,
> restent ceux de ton graphe.

Si l'import est refusé, le message dit pourquoi : voir §8.

---

## 5. Créer un projet

1. **Nouveau projet**, en haut de la liste. Donne un nom, garde le format A4 et
   la géométrie **Onglet et socle** (ou **Tente pliée** si tu n'as pas de
   socles), puis **Créer le projet**.
2. Étape **Projet** : choisis un **style** dans la bibliothèque. **Fais-le
   maintenant** : dès la première proposition générée, l'univers et le style
   d'un projet se figent, pour que toutes ses figurines restent cohérentes. Pour
   en changer ensuite, il faudra dupliquer le projet.

---

## 6. Composer, générer, choisir

### 6.1 Étape Gabarits

**Nouveau gabarit**, puis :

- **Race**, **classe**, **taille** ;
- les **options** : arme, armure… « au choix du générateur » laisse le modèle
  décider ; ce n'est pas « sans ». Pour demander l'absence, il y a des valeurs
  explicites, comme « tête nue » ;
- la **quantité à imprimer**.

Chaque champ s'enregistre quand tu le quittes ; « Enregistré » s'affiche en
haut à droite. La **description du sujet** se compose toute seule, en anglais ;
« Ce qui part au générateur » montre le sujet et le style.

### 6.2 Étape Génération

**Lancer 4 propositions.** Chaque proposition prend le temps d'une génération
sur ta carte. La file des lots montre l'avancement ; les images arrivent au fur
et à mesure, déjà **détourées**, sur un damier.

Clique une proposition pour l'ouvrir :

- donne-lui un **statut** : Brouillon, Validée, Rejetée ;
- **Retenir pour l'impression** : c'est celle-là qui ira sur la planche. Il en
  faut une par gabarit.

Une proposition « non détourée » a échoué au détourage : **Détourer** relance
l'opération ; le message dit ce qui gênait.

---

## 7. Mise en page et impression

### 7.1 Étape Mise en page

Au centre, **le PDF lui-même**, tel qu'il s'imprimera, repères compris. À
droite, les **cellules** de la page : occupées, libres. À gauche, `−` et `+`
changent le nombre d'exemplaires, et la planche se recalcule.

### 7.2 Étape Impression

Choisis la langue des légendes, puis **Télécharger le PDF**.

À l'impression, **taille réelle, 100 %** : jamais « Ajuster à la page ». En bas
de la planche, un trait de calibration porte sa longueur écrite dessous,
100 mm : mesure-le à la règle. S'il ne fait pas exactement cette longueur,
l'impression n'est pas à l'échelle, et rien ne sera à la bonne taille sur la
grille.

---

## 8. Si quelque chose coince

| Ce que tu vois | Ce qu'il faut faire |
|---|---|
| « ComfyUI injoignable » | ComfyUI tourne-t-il ? Vérifie le port (8188 ou 8000, §4.1). En Docker : `--listen` côté ComfyUI, et l'adresse `host.docker.internal` |
| « Ce fichier n'est pas un workflow utilisable » | C'est l'export ordinaire : refais-le avec **Export (API)** |
| « Ce workflow est ambigu » | Plusieurs `Save Image`, plusieurs graines, ou un encodeur à deux textes (`CLIPTextEncodeFlux`). Garde un seul `Save Image`, ou prépare le fichier à la main : voir `config/README.md`, partie `workflow.comfyui.json` |
| Un lot finit « Échoué » | La raison s'affiche sous le lot. « ComfyUI a refusé la demande » : un nœud ou un fichier de modèle manque dans ComfyUI. La page **Journaux** donne le détail |
| « Le fond de l'image n'est pas uni » au détourage | L'image n'a pas le fond gris uni que demande le cadrage. Envoie-moi l'image jumelée : les réglages du détourage se calibrent sur de vraies images |
| Le PDF ne s'affiche pas dans l'étape Mise en page | Le télécharger depuis Impression marche toujours. Dis-moi quel navigateur |
| La page reste blanche | Le script a-t-il fini de compiler l'interface ? Relance-le, et regarde ses dernières lignes |

**Ce qui m'aidera le plus, en retour :** une ou deux **images jumelées** telles
que ComfyUI les a produites (elles sont dans `data\projects\<projet>\images\`,
nommées `…-pair.png`), le **PDF**, et ce qui t'a surpris à l'usage.
