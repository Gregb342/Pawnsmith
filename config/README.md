# `config/`

## `calibration.json`

Toutes les valeurs physiques du projet vivent ici (B.2 du cahier des charges).
**Aucune de ces valeurs ne doit apparaître comme constante dans le code source.**

> ⚠️ **Les valeurs actuelles sont provisoires**, à l'exception des dimensions de
> papier et des emprises de grille (`gridFootprintMm`). Elles seront remplacées
> par les mesures d'un tirage papier de contrôle (tranche T0, §5.6 de la bible).
> Le remplacement ne doit demander aucune modification de code.

**Piège à ne pas confondre** : `gridFootprintMm` est l'emprise du pion sur la
grille de jeu ; `pawnHeightMm` est sa hauteur visuelle debout. Ce sont deux
dimensions **indépendantes**. Ne jamais déduire l'une de l'autre.

À ce stade des fondations, **aucun code ne lit ce fichier.** Il est versionné
maintenant pour que les valeurs mesurées en T0 aient un endroit où atterrir
avant que le domaine ne soit écrit.
