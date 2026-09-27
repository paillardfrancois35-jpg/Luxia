# Écouter la ligne DMX avec le boîtier Daslight DVC4 Gold (renifleur de dépannage)

Procédure relevée par l'utilisateur le 2026-09-27 (essai P5, diagnostic de l'UV). Elle permet de voir **ce que
reçoivent réellement les appareils** sans matériel supplémentaire, en attendant le renifleur Leonardo (doc 99).

## Montage

- Le boîtier DVC4 Gold (Hardware Manager l'affiche « SIUDI8A / SIUDI8 ») est branché en USB au PC.
- Sa **prise 2** (univers 2) est raccordée sur la ligne DMX pilotée par LuXia (Leonardo), par exemple en bout de
  chaîne. Seule la prise 2 accepte le mode entrée.
- Dans les préférences de Daslight 4, la prise 2 est réglée en **« Input » (« saisie »)** ; la prise 1 ne le permet pas.

## Lecture des valeurs

1. Daslight 4 → menu **Outils** → **Hardware Manager**.
2. Choisir le boîtier (colonne de gauche), puis **DMX In/Out**.
3. Regarder le cadre **Universe 2**, onglet **Dmx In** : une colonne par canal, avec le numéro et la valeur reçue
   (0-255). Faire défiler jusqu'aux canaux voulus.

Attention à bien regarder **l'univers 2** : c'est la prise d'entrée. Les autres onglets (Off, Dmx Out, Dmx In Global
view) ne servent pas ici.

## Premier usage (2026-09-27)

« UV plein » lancé et arrêté depuis le Live : les canaux 161 à 165 reçus passent bien de 0 à 255 et de 255 à 0
(161 = gradateur maître de l'UV 1, 162-165 = rangées). Les trames sur la ligne sont correctes ; l'allumage lent vient
de la réception par le projecteur BeamZ BUV463 (un PAR 160 W à la même adresse s'allume immédiatement).
