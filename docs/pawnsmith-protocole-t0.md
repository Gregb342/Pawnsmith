# Pawnsmith — Protocole T0 : calibration physique
 
| | |
|---|---|
| **Version** | 1.3 |
| **Date** | 29 août 2026 |
| **Document parent** | `pawnsmith-bible.md` v0.3, DEC-032 et DEC-033 |
 
> **Changements depuis la v1.2** — T0b est marquée comme reportée (DEC-044). L'étape 0 se juge désormais en rapport avec le trait de calibration et non en millimètres absolus, et passe après l'étape 2 (DEC-045) : en l'état, elle aurait fait conclure à un bug de T1 pour une simple réduction d'imprimante.
 
> **Changements depuis la v1.1** — T0a est marquée comme menée et concluante, avec renvoi vers DEC-043. Le modèle est corrigé : Krea 2 Turbo et non FLUX Krea. L'étape 1 précise ce que la grille ne mesure pas et qui pourtant décide — rotation contre miroir.
 
> **Changements depuis la v1.0** — Le protocole est scindé en **T0a** (test décisif, exécutable immédiatement) et **T0b** (mesures physiques, après le code de T1). Le « script Python de gabarit » posé en prérequis est supprimé : les gabarits sont produits par le CLI de T1 (DEC-033). L'étape 4 ne demande plus de déduire les grandes tailles « par proportion » — c'était faux et physiquement impossible (DEC-032). Ajout d'une étape 0 de vérification du moteur.
 
> **Pourquoi cette étape.** Aucune des valeurs cherchées ici ne se déduit par le calcul. Elles dépendent de ton imprimante, de ton papier et de ton tour de main. Un principe qui resservira ailleurs : **ne jamais automatiser un processus qu'on n'a pas exécuté manuellement au moins une fois.**
 
---
 
# T0a — Test décisif
 
**Durée estimée** : 1 heure
**Prérequis** : ComfyUI opérationnel avec le modèle local. **Aucun code Pawnsmith, aucune impression.**
 
> ✅ **T0a a été menée le 1er septembre 2026, et elle est concluante.** Verdict, prompt de référence et paramètres de génération sont consignés en **DEC-043**. Ce qui suit reste la procédure, à rejouer si le modèle change.
 
> ⚠️ **Correction.** Les versions antérieures de ce protocole annonçaient « FLUX Krea ». Le modèle réellement installé est **Krea 2 Turbo** — architecture différente, encodeur Qwen3-VL, 8 étapes à CFG 1 dont on ne sort pas. Conséquence : **les LoRA et ControlNet de l'écosystème FLUX sont incompatibles** (DEC-043).
 
T0a ne dépend de rien et passe **avant tout le reste**, y compris avant le code de T1. C'est la seule question ouverte du projet capable de faire tomber une décision d'architecture.
 
## Étape 1 — La planche de rotation
 
**Ce qu'on teste** : la capacité du modèle local à produire, en une seule génération, une vue de face et une vue de dos du **même** personnage. Et, ce que la grille ne mesure pas directement mais qui décide : une **rotation** du personnage, pas un miroir de la vue de face (DEC-002).
 
**Protocole** : trois sujets nettement différents — par exemple un guerrier orc en armure lourde, un mage humain en robe, un éclaireur avec cape et capuche. La cape et la capuche sont volontairement choisies : ce sont les éléments que les modèles perdent en premier.
 
Pour chaque sujet, une seule génération demandant explicitement une feuille de personnage avec vue de face et vue de dos alignées, corps entier, pieds au bord inférieur, fond uni.
 
**Grille d'évaluation** — cocher pour chaque sujet :
 
| Critère | Sujet 1 | Sujet 2 | Sujet 3 |
|---|---|---|---|
| Les deux vues sont alignées horizontalement | ☐ | ☐ | ☐ |
| Les deux vues sont à la même échelle | ☐ | ☐ | ☐ |
| Même palette de couleurs | ☐ | ☐ | ☐ |
| Même arme, même forme | ☐ | ☐ | ☐ |
| Même silhouette générale | ☐ | ☐ | ☐ |
| Cape / capuche / éléments dorsaux cohérents | ☐ | ☐ | ☐ |
| Pieds au bord inférieur, corps entier | ☐ | ☐ | ☐ |
| Fond uni, détourable proprement | ☐ | ☐ | ☐ |
 
**Verdict** :
 
- **6 critères ou plus sur 8, pour au moins 2 sujets sur 3** → DEC-003 tient. On continue comme prévu.
- **En dessous** → on bascule sur une implémentation dégradée, ou on explore EVO-009 (passage par la 3D). À rapporter avant de spécifier T4.
**À consigner** : le prompt exact utilisé, le modèle et le LoRA éventuel, les paramètres de génération. C'est la base de tous les templates de T3.
 
**Sortie de T0a** : le verdict, et le prompt de référence. Rien d'autre. Les images produites servent aussi de matière première aux PNG de test de T1 — les garder.
 
---
 
# T0b — Mesures physiques
 
> ⏸️ **T0b est reportée** à une date non fixée (DEC-044). T1 reste ouverte : écrite et testée, non validée. Le report ne bloque aucune autre tranche.
 
**Durée estimée** : 2 heures
**Prérequis** : le code de T1 compile et le CLI de B.7 produit un PDF. Une imprimante, du papier de plusieurs grammages, ciseaux, règle graduée ou réglet, colle en bâton, un socle de pion si disponible, un tapis de jeu quadrillé.
 
**Méthode de tirage** : les planches de test sont produites par le CLI de T1, avec des **fichiers de calibration variantes**. On ne modifie jamais `config/calibration.json` entre deux tirages ; on écrit `calibration-volets-5.json`, `calibration-volets-8.json`, etc., et on les passe en `--calibration`. Cela laisse une trace exacte de ce qui a produit quoi.
 
## Étape 0 — Vérifier le moteur avant de mesurer le papier
 
DEC-033 identifie un risque : un défaut de géométrie dans T1 contaminerait toutes les mesures qui suivent, et on mesurerait consciencieusement du faux.
 
> ⚠️ **Faire l'étape 2 d'abord** (DEC-045). Ce contrôle se juge **en rapport avec le trait de calibration mesuré sur la même feuille**, jamais en millimètres absolus. La première planche est forcément tirée avec un facteur de 1,0, donc une imprimante qui réduit de 2 % ferait mesurer 117,6 mm à une cellule de 120 : on conclurait à un bug de T1 pour une propriété de l'imprimante. Le trait et la cellule subissent la même réduction, c'est ce qui rend le rapport fiable — et c'est exactement ce que garantit le §B.5.5 en appliquant le facteur d'échelle au trait comme au reste.
 
Sur la première planche imprimée, en notant `T` la longueur mesurée du trait de calibration :
 
| Contrôle | Attendu |
|---|---|
| Hauteur totale d'une cellule, bord haut à bord bas du contour | `2 × (pawnHeightMm + appendiceMm) × T / 100` |
| Largeur d'une cellule | `pawnWidthMm × T / 100` |
| Onglet centré horizontalement | Écarts gauche et droite égaux |
 
Un écart au-delà de la tolérance de lecture du réglet arrête T0b : c'est un bug de T1, pas une propriété du papier.
 
## Étape 2 — Facteur de correction d'échelle
 
**Ce qu'on cherche** : `print.scaleCorrectionFactor`.
 
1. Imprimer une planche en **taille réelle / 100 %**, surtout pas en « ajuster à la page ».
2. Mesurer le trait de calibration à la règle, au demi-millimètre.
3. `scaleCorrectionFactor = 100 / mesure_relevée`
Si tu mesures 98,5 mm, le facteur vaut 1,0152. Si tu mesures exactement 100, il vaut 1,0 et tu as de la chance.
 
**Refaire ce test après chaque changement de pilote d'impression ou d'imprimante.**
 
| Mesure relevée | Facteur calculé |
|---|---|
| ______ mm | ______ |
 
## Étape 3 — Grammage
 
**Ce qu'on cherche** : le grammage de travail, qui conditionne les autres mesures.
 
Imprimer la même planche sur les grammages disponibles — typiquement 80, 160 et 250 g/m². Pour chacun, découper une figurine, plier, coller, poser debout.
 
| Grammage | Se plie proprement | Tient debout | Verdict |
|---|---|---|---|
| 80 g/m² | ☐ | ☐ | |
| 160 g/m² | ☐ | ☐ | |
| 250 g/m² | ☐ | ☐ | |
 
Le pli doit être net sans craqueler, et le pion doit tenir sans gondoler. Si aucun grammage ne convient seul, tester la plastification au ruban d'emballage.
 
**Retenu** : ______ g/m²
 
## Étape 4 — Hauteurs de pion
 
**Ce qu'on cherche** : `sizes.Medium.pawnHeightMm`, puis la **loi de progression** des autres tailles (DEC-032).
 
### 4a — La taille Medium
 
Composer trois figurines de taille Medium à 45, 50 et 55 mm de hauteur visible. Les monter, les poser sur le tapis, à côté d'une figurine du commerce si tu en as une.
 
Critères : lisible à un mètre, pas écrasé, pas dominant par rapport à une case de 25,4 mm.
 
| Hauteur testée | Trop petit | Juste | Trop grand |
|---|---|---|---|
| 45 mm | ☐ | ☐ | ☐ |
| 50 mm | ☐ | ☐ | ☐ |
| 55 mm | ☐ | ☐ | ☐ |
 
**Retenu pour Medium** : ______ mm
 
### 4b — Les autres tailles
 
> ⚠️ **Ne pas déduire les autres hauteurs « par proportion ».** C'est ce que disait la v1.0 de ce protocole, et c'est faux deux fois. D'abord, les règles de jeu ne définissent que l'emprise au sol, jamais la hauteur d'une créature : aucune proportion n'est donnée nulle part. Ensuite, une progression proportionnelle aux emprises (×1, ×2, ×3, ×4) donnerait 200 mm pour `Gargantuan`, soit une hauteur dépliée de 420 mm — la feuille en fait 297.
 
Le **plafond dur** est celui du §B.5.6 du cahier des charges : environ **112 mm** de hauteur de pion si US Letter doit rester utilisable, 121 mm si l'on se limite à l'A4. C'est une contrainte, pas un objectif.
 
Tirer une planche par taille aux valeurs candidates, monter un exemplaire de chacune, **les poser côte à côte sur le tapis** et juger la progression comme un ensemble. C'est la seule façon de voir si l'écart entre `Large` et `Huge` est lisible.
 
| Taille | Emprise | Hauteur candidate | Retenue |
|---|---|---|---|
| Small | 25,4 mm | ______ mm | ______ mm |
| Medium | 25,4 mm | *(étape 4a)* | ______ mm |
| Large | 50,8 mm | ______ mm | ______ mm |
| Huge | 76,2 mm | ______ mm | ______ mm |
| Gargantuan | 101,6 mm | ______ mm (≤ 112) | ______ mm |
 
**Contrôle obligatoire avant de clore l'étape** : pour chaque taille retenue, vérifier que `2 × (hauteur + appendice) ≤ 245 mm`. Une valeur au-dessus rend la taille inutilisable sur US Letter et produit une capacité nulle.
 
## Étape 5 — Onglet et socle
 
**Ce qu'on cherche** : `geometry.tabAndSocket.tabWidthMm` et `tabHeightMm`.
 
**Si tu as un socle du commerce** : mesurer la fente, largeur et profondeur. L'onglet doit faire la largeur de la fente moins un cheveu, et sa hauteur au moins la profondeur de la fente.
 
**Si tu n'en as pas** : cette étape est reportée. Note-le et laisse les valeurs provisoires — cela ne bloque ni T1 ni sa relecture, seulement sa validation en géométrie `TabAndSocket`. Teste alors uniquement la géométrie `FoldedTent`.
 
Attention : après pliage et collage, l'onglet est en **double épaisseur**. Mesure sur un onglet assemblé, pas sur une simple feuille.
 
| Mesure | Valeur |
|---|---|
| Largeur de fente du socle | ______ mm |
| Profondeur de fente | ______ mm |
| Épaisseur d'un onglet plié-collé | ______ mm |
| `tabWidthMm` retenu | ______ mm |
| `tabHeightMm` retenu | ______ mm |
 
## Étape 6 — Volets de tente
 
**Ce qu'on cherche** : `geometry.foldedTent.flapHeightMm`.
 
Découper trois figurines identiques avec des volets de 5, 8 et 12 mm. Plier les volets vers l'extérieur, poser debout, souffler dessus doucement.
 
| Hauteur de volet | Tient debout | Stable au souffle | Discret |
|---|---|---|---|
| 5 mm | ☐ | ☐ | ☐ |
| 8 mm | ☐ | ☐ | ☐ |
| 12 mm | ☐ | ☐ | ☐ |
 
**Retenu** : ______ mm
 
## Étape 7 — Marge de silhouette
 
**Ce qu'on cherche** : `layout.silhouetteMarginMm`.
 
Découper la même figurine trois fois : au ras du trait, avec 1,5 mm de marge, avec 3 mm. Évaluer combien de fois le ciseau mord dans le dessin, et à quel point la marge blanche devient visible une fois le pion debout.
 
| Marge | Silhouette rognée | Marge blanche visible |
|---|---|---|
| 0 mm | ☐ | ☐ |
| 1,5 mm | ☐ | ☐ |
| 3 mm | ☐ | ☐ |
 
**Retenu** : ______ mm
 
---
 
## Fiche de synthèse à rapporter
 
```json
{
  "scaleCorrectionFactor": ______,
  "grammageRetenu": ______,
  "sizes": {
    "Small":      { "pawnHeightMm": ______ },
    "Medium":     { "pawnHeightMm": ______ },
    "Large":      { "pawnHeightMm": ______ },
    "Huge":  { "pawnHeightMm": ______ },
    "Gargantuan": { "pawnHeightMm": ______ }
  },
  "geometry": {
    "foldedTent": { "flapHeightMm": ______ },
    "tabAndSocket": { "tabWidthMm": ______, "tabHeightMm": ______ }
  },
  "layout": { "silhouetteMarginMm": ______ }
}
```
 
**Plus, et c'est le plus important** : le verdict de T0a et le prompt de référence qui a fonctionné.
 
---
 
## Erreurs à éviter
 
- **Imprimer en « ajuster à la page »**. C'est le piège numéro un, il invalide toutes les mesures d'un coup.
- **Sauter l'étape 0** et mesurer sur une planche produite par un moteur non vérifié.
- **Mesurer sur une feuille non assemblée** pour l'onglet. La double épaisseur change tout.
- **Enchaîner les tests sans noter au fur et à mesure.** À la troisième figurine, tu ne sauras plus laquelle avait 8 mm de volet.
- **Modifier `config/calibration.json` entre deux tirages** au lieu d'écrire un fichier variante. On perd la trace de ce qui a produit quoi.
- **Vouloir tout finir.** Si l'étape 5 est impossible faute de socle, saute-la. Reporter une mesure est sans conséquence ; inventer une valeur en coûte une impression complète à détecter.