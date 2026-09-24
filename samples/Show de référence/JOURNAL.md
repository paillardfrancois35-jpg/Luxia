# Journal du show de référence

> Ce qui a été ajouté à chaque phase, ce qui a été validé, les retours de l'utilisateur (doc 41 §1, REF-1 à REF-6).

## P0 – Fondations (2026-09-24)

### Ajouté

| Élément | Fichier | Description |
|---|---|---|
| Fiche du projet | `projet.json` | Identifiant stable, nom, description (format 1) |
| Enregistrement du chenillard | `Enregistrements/P0-chenillard-1-180.dmxrec` | Canaux 1 à 180, **180 (fumée) exclu**, valeur 50 % (128), 250 ms par canal, une passe (45,4 s, 1817 trames à 40 Hz) |

La configuration de sortie n'est **pas** dans le show : elle est dans les préférences du poste (SORT-006, Q15).

### Comment rejouer / vérifier

```bash
dmx-headless relire "samples/Show de référence/Enregistrements/P0-chenillard-1-180.dmxrec" --canaux 1-180
dmx-headless lancer --test 1-180 --exclus 180 --valeur 50 --pas 1000 --une-fois
```

La seconde commande rejoue le chenillard **sur le matériel** (1 s par canal, ≈ 3 min) : chaque appareil branché au plan
d'adresses du doc 41 §2 réagit quand son canal passe (avec la limite du mode 7 canaux expliquée dans le guide P0, exemple 3).

### Non-régression

Test automatique `ReferenceShowP0Tests` : l'enregistrement est relu (canaux 1 → 179 dans l'ordre, 180 jamais allumé,
une seule valeur à 128) et le moteur actuel, rejoué en temps virtuel, reproduit la même séquence.

### Validation par l'utilisateur

| Élément | Statut | Retour |
|---|---|---|
| Chaque appareil réagit au chenillard | ⏳ en attente | |

## P1 – Console (2026-09-24)

### Ajouté

| Élément | Fichier | Description |
|---|---|---|
| 6 instantanés de console (catégorie « Phase P1 ») | `console.json` | PAR 1 en blanc ; PAR 1 à 4 en blanc ; PAR 1 à 4 en rouge à 50 % ; Lyre 1 au centre ; Lyres 1 et 2 au centre ; UV plein |

Valeurs établies à partir des notices : LPC008S 7 canaux (fiche `betopper-lpc008s.md`), lyre Tomshine 11 canaux
(Pan, Pan fin, Tilt, Tilt fin, couleur, gobo, obturateur, gradateur, vitesse, contrôle, mode), BeamZ BUV463 7 canaux
(maître, 4 rangées UV, strobe / vitesse, programmes). Détail dans le guide `docs/demos/P1-console.md`.

### Non-régression

Test `ReferenceShowP1Tests` : le projet s'ouvre sans message ; chaque instantané, rappelé par le moteur, produit
exactement ses canaux ; aucun ne touche la fumée (180) ni les canaux de contrôle des lyres (120, 135).

### Validation par l'utilisateur

| Élément | Statut | Retour |
|---|---|---|
| Canaux conformes au plan d'adresses (chaque instantané allume ce qui est annoncé) | ⏳ en attente | |
