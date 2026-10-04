# Pawnsmith — Cahier des charges : tranche T5 (détourage)

| | |
|---|---|
| **Version** | 1.0 |
| **Date** | 4 octobre 2026 |
| **Document parent** | `pawnsmith-bible.md` v0.18 — chapitres 4, 7, 9, 11 et 12 en particulier |
| **Documents frères** | les cahiers T1 à T4, T6 et T7, dont ce document reprend la forme |
| **Portée** | Découper l'image jumelée en deux moitiés et détourer chacune, dans le lot comme à la demande |

> **Régime d'écriture.** Comme T4, T6 et T7, ce document est écrit dans le régime « tranche et consigne ». **Les deux choix que le porteur s'était réservés ont été tranchés par lui**, le 4 octobre 2026 : un détourage **sans modèle**, et des PNG lus et écrits **à la main**. Le reste a été tranché sans lui ; chaque décision est une fiche du chapitre 11 (DEC-098 à DEC-104), reprise au §8 de `CLAUDE.md`.

> **Comment lire ce document.** La bible prévoyait un modèle ONNX (DEC-008, chapitre 7). F.1 dit pourquoi il n'y en a pas, et ce qui le rend possible : la clause de cadrage exige déjà un fond gris uni. F.2 à F.4 décrivent le décodage, l'algorithme et le port ; F.5 et F.6 disent quand le détourage tourne et ce qu'il fait en cas d'échec.
>
> Les sections sont numérotées **F.x**, la lettre réservée à T5 depuis T4.

---

## F.0 Consignes de travail

Celles des tranches précédentes s'appliquent. Trois avec une force particulière.

- **Une image vient d'ailleurs.** Le générateur, comme une archive importée, peut fournir n'importe quoi. Tout se vérifie **sur l'en-tête, avant d'allouer** (MEN-005), et un fichier inattendu est refusé avec un code, jamais décodé « au mieux ».
- **Aucune valeur inventée.** Les seuils du détourage s'**arbitrent** (DEC-057) : ils vivent dans un record d'options, chacun avec son motif, et aucun n'apparaît en littéral dans le code qui l'applique.
- **Les images de test se fabriquent en code**, comme en T4. Aucun binaire n'est commité ; les images réelles de T0a restent dans `refs/`, hors du dépôt.

---

## F.1 Pourquoi pas de modèle

### F.1.1 Ce que la licence a écarté

Le choix revenait au porteur, parce que c'est une question de licence. L'inventaire, vérifié dans les dépôts et non de mémoire :

| Modèle | Licence du code | Ce qui bloque |
|---|---|---|
| BiRefNet | MIT | Entraîné sur **DIS5K**, dont les conditions d'utilisation disent : *« commercial use of this dataset is prohibited even after copying, editing, processing or any operations of this database »* |
| IS-Net (DIS) | Apache-2.0 — le dépôt écrit « *our code and evaluation metric* » | Mêmes poids, même jeu de données DIS5K |
| U²-Net | Apache-2.0 | Entraîné sur DUTS, un jeu de recherche sans licence commerciale claire |
| RMBG (BRIA) | — | Annoncé non commercial. **Non vérifié ici** : `huggingface.co` est bloqué par le proxy de l'environnement |

La licence d'un code ne couvre pas d'elle-même les poids, et les poids portent la trace de leurs données. La règle du projet — licence OSI permissive **sans condition d'usage commercial** — n'en laisse passer aucun sans risque.

### F.1.2 Ce qui rend un détourage sans modèle possible

La clause de cadrage (§E.5, DEC-076) exige du générateur un **fond gris pâle uni**, « *completely plain and even, no scenery, no ground plane, no cast shadow* ». T0a l'a mesuré : fond uniforme à **99 %** sur le meilleur sujet, **83 %** sur le moins bon (DEC-043). Un personnage sur un fond uni se sépare par **diffusion depuis les bords** : tout ce qui touche le bord de l'image et ressemble au fond est du fond.

C'est déterministe, sans dépendance, rapide sur processeur, et testable avec des images fabriquées en code.

### F.1.3 Ce qu'on accepte, et la porte qu'on laisse ouverte

Un vêtement du même gris que le fond, touchant le bord, serait mangé. Une ombre portée malgré la consigne resterait. **Le port `IBackgroundRemover` reste** (F.4) : si les vraies images le demandent, un modèle s'y branche sans toucher au reste. La qualité se juge sur les premières sorties réelles, avec le CLI de F.8.

À consigner en **DEC-098**, décidée par le porteur. Elle **supersède DEC-008** sur un point — « modèle ONNX embarqué » — et garde le reste : détourage local, systématique, jamais délégué au générateur.

---

## F.2 Lire et écrire un PNG

### F.2.1 Le sous-ensemble lu

Le décodeur ne lit que ce que ComfyUI écrit : **8 bits par canal, RGB ou RGBA, non entrelacé**. Tout autre PNG — palette, niveaux de gris, 16 bits, entrelacé — est refusé par `CUTOUT_IMAGE_INVALID`, en nommant ce qui manque. On n'écrit pas ce qu'on ne peut pas éprouver.

Contrôles, dans l'ordre :

1. Signature PNG, puis `IHDR` en tête.
2. **Dimensions sur l'en-tête, avant toute allocation** : un côté de plus de 8 192 pixels rend `CUTOUT_IMAGE_TOO_LARGE` (MEN-005). La borne est celle du générateur (§E.9) et de la planche (DEC-095).
3. **CRC de chaque bloc** vérifié : un fichier tronqué ou altéré se refuse, il ne se décode pas à moitié.
4. **Taille décompressée connue d'avance** : `hauteur × (1 + largeur × octets par pixel)`. Le flux zlib est lu **exactement** jusque-là ; un octet de trop ou de moins est un refus. Une bombe de décompression ne dépasse jamais ce que l'en-tête a annoncé, et l'en-tête a déjà été borné.
5. Les cinq filtres de ligne du format (aucun, Sub, Up, Average, Paeth) sont défaits.

### F.2.2 L'écriture

L'encodeur écrit du **RGBA 8 bits**, filtre aucun, compressé par le `ZLibStream` du framework. Il n'écrit **que** `IHDR`, `IDAT` et `IEND` : les métadonnées de ComfyUI — le graphe et le prompt — ne passent jamais dans un détourage. C'est ce que DEC-079 demandait, puisque les détourages partent dans une archive `Share`.

### F.2.3 Pourquoi à la main

ImageSharp est écarté : sa *Six Labors Split License* pose des conditions à l'usage commercial. SkiaSharp (MIT) aurait convenu, au prix de bibliothèques natives par plateforme dans l'image Docker. Pour un sous-ensemble connu, deux cents lignes testées valent mieux qu'une dépendance — la règle du §3 de `CLAUDE.md`.

À consigner en **DEC-099**, décidée par le porteur.

---

## F.3 L'algorithme

Il s'applique à **chaque moitié** de l'image jumelée, découpée par la règle de DEC-079 (`PairSplit`). Une image est un tableau de pixels RGBA.

### F.3.1 La couleur du fond

La **médiane**, canal par canal, des pixels du bord **haut, gauche et droit**. Pas du bord bas : c'est là que se trouvent les pieds et la bande de sol.

La **distance** d'un pixel au fond est le plus grand écart sur les trois canaux (distance de Tchebychev, de 0 à 255). Plus simple qu'une distance euclidienne, et lisible : « aucun canal ne s'écarte de plus de *t* ».

**Le fond doit être assez uni** : si moins de **60 %** des pixels des trois bords sont à moins de la tolérance de la médiane, l'image est refusée par `CUTOUT_BACKGROUND_NOT_UNIFORM`. T0a mesurait 83 % sur son plus mauvais sujet ; 60 % laisse de la marge sans accepter un décor.

### F.3.2 La bande de sol

T0a a vu une bande de sol de 8 à 10 pixels sur 832, collée aux pieds, malgré la consigne. Gardée, **elle deviendrait la ligne des pieds**, puisque T1 cale l'image sur son bord bas.

Règle : en partant du bas, une ligne dont **au moins 90 %** des pixels s'écartent du fond est une ligne de sol ; on retire ces lignes, et on s'arrête à la première qui n'en est pas une, ou après **5 %** de la hauteur. Des pieds ne couvrent jamais 90 % de la largeur d'une moitié : la clause de cadrage exige une silhouette étroite.

### F.3.3 Le fond, par diffusion

Depuis chaque pixel des quatre bords (bas compris, une fois la bande retirée) dont la distance au fond est sous la **tolérance** (24), une diffusion en 4-connexité marque comme fond tout pixel voisin sous la tolérance. Ce qui reste est le sujet.

Retirer la bande avant la diffusion compte : sinon l'espace entre les jambes, fermé en bas par la bande, resterait gris.

### F.3.4 Les trous de fond

Entre un bras et le corps, un peu de fond peut être enfermé. Une région **non reliée au bord**, dont chaque pixel est sous une tolérance **stricte** (12), et d'au moins **0,05 %** des pixels de la moitié, est aussi du fond. La tolérance stricte protège un vêtement gris proche du fond ; la taille minimale protège un reflet.

### F.3.5 Le bord adouci

Un pixel du sujet voisin du fond reçoit une opacité proportionnelle à sa distance : 0 à la tolérance, opaque à deux fois la tolérance. Le reste du sujet est opaque, le fond totalement transparent. Sans cela, le contour imprimé serait en escalier, et garderait un liseré gris.

### F.3.6 Le recadrage

L'image est **recadrée sur la boîte englobante du sujet**. Le bas de la boîte est donc la ligne des pieds — ce que T1 attend (§B.4.4). Les côtés et le haut recadrés laissent le pion occuper toute la largeur disponible. Recto et verso sont recadrés chacun de son côté ; DEC-041 leur impose ensuite une échelle commune.

Un sujet de moins de **1 %** des pixels de la moitié — ou aucun — rend `CUTOUT_SUBJECT_NOT_FOUND`.

### F.3.7 Les valeurs

| Option | Défaut | Motif |
|---|---|---|
| `MaxImageDimensionPx` | 8 192 | La borne du générateur et de la planche |
| `BackgroundTolerance` | 24 | Absorbe le bruit d'un fond « uni » généré sans mordre sur un vêtement clair |
| `HoleTolerance` | 12 | La moitié : un trou enfermé doit être vraiment du fond |
| `MinHoleFraction` | 0,0005 | Écarte reflets et pixels isolés |
| `MinBorderUniformity` | 0,60 | Sous les 83 % de T0a, au-dessus d'un décor |
| `GroundBandMinCoverage` | 0,90 | Une silhouette étroite n'y arrive jamais |
| `GroundBandMaxFraction` | 0,05 | Quatre fois la bande observée en T0a, et le sujet reste intact |
| `MinSubjectFraction` | 0,01 | En dessous, ce n'est pas un personnage |

Ces valeurs **s'arbitrent** (DEC-057) et se règlent sur les vraies images, avec le CLI de F.8. **À vérifier sur les premières sorties réelles**, comme T0a le demandait.

À consigner en **DEC-100**.

---

## F.4 Le port

```csharp
public interface IBackgroundRemover
{
    Task<CutoutPair> CutOutPairAsync(byte[] pairedPng, CancellationToken ct);
}

public sealed record CutoutPair(byte[] FrontPng, byte[] BackPng);
```

Le port reçoit **l'image jumelée** et rend **deux PNG détourés**. La découpe (`PairSplit`, DEC-079) est appliquée dans l'adaptateur, parce qu'elle demande des pixels décodés et que le décodage est de l'infrastructure.

Il **supersède la signature du chapitre 7** (`RemoveAsync(RawImage) → TransparentImage`), écrite quand on ne savait pas encore qu'une image arriverait jumelée, ni que les types d'image seraient de simples octets PNG.

Un seul adaptateur en v1, `UniformBackgroundRemover`. Un modèle s'ajouterait à côté, comme second adaptateur.

À consigner en **DEC-102**.

---

## F.5 Quand le détourage tourne

### F.5.1 Dans le lot, systématiquement

DEC-008 le veut systématique. Dans `CandidateGeneration`, chaque image reçue est **détourée avant d'être sauvegardée** : elle est déjà en mémoire, et la sauvegarde écrit alors en une fois l'image jumelée, la face et le dos, avec un candidat qui porte ses trois fichiers. Le détourage se fait **hors de la porte d'écriture** (DEC-086) : il ne lit ni n'écrit le projet.

### F.5.2 Un détourage raté n'arrête pas le lot

Le candidat est **sauvegardé sans détourage**, donc non élisible (DEC-071), et le lot continue. L'échec est **noté sur le `Job`**, en mémoire comme lui — identifiant du candidat et code —, rendu par l'API, et journalisé par le travailleur (DEC-092).

Arrêter le lot serait le mauvais compromis : le candidat est généré, il a coûté du temps de carte graphique, et l'utilisateur peut le juger sur son image jumelée. Seul son détourage manque, et il se relance.

### F.5.3 À la demande

`CandidateCutout` détoure un candidat **existant** : il lit son image jumelée, écrit la face et le dos, et met à jour le candidat, derrière la porte d'écriture. C'est la voie des candidats générés avant T5, et celle d'un nouvel essai après un réglage. Un détourage existant est **remplacé**.

Un candidat sans image jumelée — retirée par un import `Share` — ne se détoure pas : `CANDIDATE_NO_PAIRED_IMAGE`.

À consigner en **DEC-101**.

---

## F.6 Les codes

| Code | Statut HTTP | Sens |
|---|---|---|
| `CUTOUT_IMAGE_INVALID` | 422 | Pas un PNG, ou un PNG hors du sous-ensemble lu (F.2.1), ou altéré |
| `CUTOUT_IMAGE_TOO_LARGE` | 422 | Un côté au-delà de la borne (MEN-005) |
| `CUTOUT_BACKGROUND_NOT_UNIFORM` | 422 | Le bord n'est pas un fond uni |
| `CUTOUT_SUBJECT_NOT_FOUND` | 422 | Rien ne reste, ou presque, une fois le fond retiré |
| `CANDIDATE_NO_PAIRED_IMAGE` | 422 | Rien à détourer |

Ils ferment **la moitié détourage de la question G** : il n'y a ni dimension d'entrée de modèle, ni fournisseur d'exécution ; la durée est celle d'une diffusion sur deux millions de pixels, de l'ordre de la centaine de millisecondes.

À consigner en **DEC-103**.

---

## F.7 L'API

| Verbe | Route | Réponse |
|---|---|---|
| `POST` | `/api/projects/{folder}/blueprints/{id}/candidates/{candidateId}/cutout` | Le gabarit, avec le candidat détouré |

Le `Job` de `GET /api/jobs/{id}` gagne `cutoutFailures : [{ candidateId, code }]`.

---

## F.8 Le CLI

- `candidate cutout --path … --id … --candidate …` — le cas d'usage de F.5.3.
- `cutout --pair image.png --out dossier/` — **sans projet** : détoure une image jumelée quelconque et écrit `front.png` et `back.png`. C'est l'outil pour juger l'algorithme sur les images réelles de T0a, dans `refs/`, et pour régler F.3.7.

---

## F.9 Tests attendus

| # | Ce qu'il vérifie |
|---|---|
| 1 | Un PNG RGB et un PNG RGBA écrits par l'encodeur se relisent à l'identique, pour chacun des cinq filtres |
| 2 | Un PNG palette, 16 bits, niveaux de gris ou entrelacé est refusé, en nommant ce qui manque |
| 3 | Un côté de plus de 8 192 pixels est refusé sur l'en-tête, sans rien décompresser |
| 4 | Un CRC faux est refusé |
| 5 | Un flux décompressé plus long ou plus court que l'en-tête l'annonce est refusé |
| 6 | L'encodeur n'écrit que `IHDR`, `IDAT`, `IEND` : un bloc `tEXt` de la source ne survit pas |
| 7 | La couleur du fond est la médiane des bords haut, gauche et droit |
| 8 | Un fond trop peu uni est refusé |
| 9 | Le fond relié au bord devient transparent ; le sujet reste opaque |
| 10 | La bande de sol est retirée ; les pieds deviennent le bas de l'image |
| 11 | La bande de sol n'est jamais retirée au-delà de 5 % de la hauteur |
| 12 | L'espace entre les jambes, fermé par la bande, devient transparent |
| 13 | Un trou de fond enfermé devient transparent ; un vêtement gris proche du fond, non |
| 14 | Le bord du sujet est adouci |
| 15 | L'image est recadrée sur le sujet |
| 16 | Une image sans sujet est refusée |
| 17 | L'adaptateur découpe la paire selon DEC-079 et détoure les deux moitiés |
| 18 | Dans le lot, un candidat sort avec ses trois fichiers et devient élisible |
| 19 | Dans le lot, un détourage raté laisse le candidat sans détourage, note l'échec sur le `Job`, et le lot continue |
| 20 | À la demande, un candidat existant est détouré ; un détourage existant est remplacé |
| 21 | Un candidat sans image jumelée rend `CANDIDATE_NO_PAIRED_IMAGE` |
| 22 | La route de l'API détoure, et rend chaque code avec son statut |
| 23 | La planche se rend avec des détourages RGBA : PDFsharp accepte la transparence |
| 24 | De bout en bout : générer, détourer, élire, imprimer |

---

## F.10 Critères d'acceptation

T5 est terminée quand :

- [ ] Les **tests de F.9** passent.
- [ ] MEN-005 est couvert pour le décodage du détourage : bornes sur l'en-tête, taille décompressée exacte.
- [ ] Un PNG malformé échoue proprement, avec un code, sans arrêter le processus.
- [ ] Les PNG de sortie ont un **fond réellement transparent**, et ne portent aucune métadonnée de la source.
- [ ] Aucune dépendance ajoutée.
- [ ] Les fiches DEC de F.11 sont écrites ; le chapitre 9 nomme les tests de MEN-005 pour T5.

Le critère du chapitre 12 — « runtime ONNX, fournisseur d'exécution configurable » — tombe avec DEC-098.

---

## F.11 Décisions à consigner dans la bible

| Réf. | Objet | Section |
|---|---|---|
| **DEC-098** | Détourage sans modèle, sur le fond uni que la clause de cadrage exige. Décidée par le porteur. **Supersède DEC-008** sur le modèle ONNX | F.1 |
| **DEC-099** | PNG lus et écrits à la main, sous-ensemble 8 bits RGB/RGBA. Décidée par le porteur | F.2 |
| **DEC-100** | L'algorithme : médiane des bords, bande de sol, diffusion, trous, bord adouci, recadrage ; les valeurs | F.3 |
| **DEC-101** | Détourage dans le lot, avant la sauvegarde ; un échec n'arrête pas le lot ; détourage à la demande | F.5 |
| **DEC-102** | Le port reçoit l'image jumelée et rend deux PNG. **Supersède la signature du chapitre 7** | F.4 |
| **DEC-103** | Cinq codes ; **ferme la moitié détourage de la question G** | F.6 |
| **DEC-104** | T5 porte la version `0.9.0`. **Supersède le tableau de DEC-058** sur ce point | F.12 |

---

## F.12 Découpage en tâches

| # | Tâche |
|---|---|
| 1 | PNG : décodeur et encodeur, bornes, CRC ; `<Version>0.9.0</Version>` (tests 1 à 6) |
| 2 | L'algorithme sur une image décodée (tests 7 à 16) |
| 3 | Port, adaptateur, écriture des détourages par le dépôt (test 17) |
| 4 | Le lot détoure ; `Job.CutoutFailures` ; `CandidateCutout` (tests 18 à 21) |
| 5 | API et CLI (test 22) |
| 6 | Planche RGBA et bout en bout (tests 23 et 24) |
| 7 | Documentation : chapitre 9, `CLAUDE.md`, README |

**La version est `0.9.0`, pas `0.6.0`.** DEC-058 réservait `0.6.0` à T5, mais le dépôt est en `0.8.0` depuis T7, et un numéro de version ne recule pas. `0.6.0` n'existera jamais ; la trace en est DEC-104.
