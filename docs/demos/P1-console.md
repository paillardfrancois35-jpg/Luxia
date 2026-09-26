# Démonstration P1 – Console

> Guide de découverte de la phase P1 (doc 40 §7, doc 41 §11). Durée : 30 minutes.
> Matériel : PC ; pour la partie matériel : Arduino (firmware 1.0) + les appareils branchés au plan d'adresses du doc 41 §2
> (au minimum 1 PAR en mode 7 canaux à l'adresse 1). Branche Git : `p1/console`.

## Ce que livre P1

| Élément | Où |
|---|---|
| Coquille de l'application : navigation (Console, Sorties), menu **Projet** (Nouveau, Ouvrir), barre d'état permanente | `LuXia.exe` |
| Console en mode canaux : pages de faders, prise / libération, sélection multiple, saisie directe, moniteur de sortie | écran **Console** |
| Instantanés de console, rangés dans le projet (`console.json`) | panneau de droite de la Console |
| 6 instantanés du show de référence (catégorie « Phase P1 ») | `samples/Show de référence/console.json` |

**Lancer avec le show de référence :**

```bash
dotnet run --project src/Luxia.App -- "samples/Show de référence"
```

(ou menu **Projet → Ouvrir…** puis le dossier `samples/Show de référence` ; il est ensuite rouvert automatiquement.)

---

## Exemple 1 – Prendre et libérer un fader (CONS-003, CONS-005)

**Faire** : Console, page 1-16 (ou 1-32 selon la largeur de la fenêtre). Cliquer sur le fader 1 **sans bouger** : il est « pris »
(bordure et remplissage **orange**) à sa valeur actuelle, 0. Glisser vers le haut : la valeur monte (toute la hauteur = 255 pas).

**Observer** :
- la case 1 du **moniteur** s'éclaire et s'encadre en orange ; « 1 canal pris » ;
- sur le matériel (PAR 1 en 7 canaux) : rien ne s'allume tant que seul le gradateur (canal 1) est monté — prendre aussi le canal 2 (rouge) : le PAR s'allume en rouge ;
- **Libérer la sélection** (ou **Tout libérer**) : le fader redevient bleu et retombe à la valeur de la chaîne de rendu (0 en P1, puisqu'aucune scène n'existe encore).

**Illustre** : la surcharge brute (étape 11 de la chaîne de rendu, doc 02 §9) ; le fader affiche **toujours la valeur réellement émise**.

## Exemple 2 – Les modes de saisie (CONS-002)

| Geste | Effet |
|---|---|
| Glisser | relatif, sans saut au point cliqué |
| Molette | ±1 ; **Maj** + molette : ±10 |
| Flèches ↑ ↓ (fader sélectionné) | ±1 |
| Page préc. / suiv. | ±10 |
| Début / Fin | 255 / 0 |
| Double-clic sur le fader, ou clic dans la case sous le fader | saisie directe (0-255) puis **Entrée** ; **Échap** annule |

Une saisie invalide (ex. 300) est refusée avec un message bleu en haut ; la valeur ne change pas.

## Exemple 3 – Plusieurs faders ensemble : R, G, B d'un PAR (CONS-006)

**Faire** : clic sur le fader 2, **Maj**+clic sur le fader 4 (sélection 2-4, cadres bleus). Monter l'un d'eux.

**Observer** :
- case **cochée** « déplacement relatif » : les trois montent **du même écart** (en partant de valeurs différentes, elles gardent leur écart) ;
- case **décochée** (absolu) : les trois prennent **la même valeur** → avec le canal 1 monté, le PAR 1 passe du noir au blanc ;
- **Ctrl**+clic ajoute ou retire un fader ; **Échap** ou « Désélectionner » vide la sélection.

## Exemple 4 – Pages et moniteur (CONS-001, CONS-040, CONS-041, CONS-043)

**Faire** : ◀ ▶ pour parcourir les pages (16, 32 ou 48 faders selon la largeur ; agrandir la fenêtre pour passer à 32 ou 48).
Survoler le moniteur ; cocher « Valeurs » ; cliquer une case (ex. 111) : la console saute à la page de ce canal.

**Observer** : le trait bleu sous le moniteur montre la page affichée ; le survol indique « Canal 111 – valeur … – pris à la console ».
Le nom de l'appareil et de l'attribut au survol viendront avec le patch (P3).

## Exemple 5 – Instantanés du show de référence (CONS-010)

**Faire** : double-clic sur un instantané (ou « Rappeler »). Un instantané **remplace** les faders pris de son univers.

| Instantané | Canaux | Ce qu'on doit voir sur le matériel |
|---|---|---|
| PAR 1 en blanc | 1-7 | PAR 1 blanc plein (gradateur 255, R = V = B = 255, strobe et fonction à 0) |
| PAR 1 à 4 en blanc | 1-28 | les 4 PAR blancs ; un PAR éteint = adresse ou mode à vérifier (A001, A008, A015, A022) |
| PAR 1 à 4 en rouge à 50 % | 1-28 | les 4 PAR rouges à mi-puissance (gradateur 128) |
| Lyre 1 au centre | 111-121 | lyre 1 au milieu de sa course Pan/Tilt, faisceau blanc ouvert |
| Lyres 1 et 2 au centre | 111-121, 126-136 | les deux lyres au centre |
| UV plein | 161-174 | les deux UV à fond |

**Piège (règle montrée)** : aucun instantané ne touche le canal **180** (fumée) ni le canal **Reset** des lyres (121 et 136) :
un test automatique le vérifie. Pour essayer : prendre le fader 121, le monter à 255 : la lyre 1 se **réinitialise** — c'est
exactement ce que l'on veut éviter par erreur ; les limites de sûreté (P5) protégeront la fumée, même à la console (GEN-042).

**Mémoriser** : prendre quelques faders, « Mémoriser les faders pris… », donner un nom ; l'instantané est écrit dans `console.json` du projet.
**Supprimer** demande confirmation (GEN-103).

## Exemple 6 – Scénario SC-01 complet

1. Démarrer l'application **sans** Arduino : barre d'état « Arduino déconnecté ».
2. Brancher l'Arduino : « Arduino connecté (COMx – DMX-LEONARDO 1.0) », ~40 trames/s.
3. Rappeler « PAR 1 en blanc » : le PAR s'allume.
4. Débrancher l'USB : le PAR s'éteint en ≤ 2 s (chien de garde du firmware) ; barre d'état « en erreur » puis « déconnecté ».
5. Rebrancher : reconnexion < 3 s ; le PAR se **rallume seul** (les faders pris sont toujours là : l'état est dans le moteur, pas dans la carte).

## Ce qui n'est pas encore là (et pourquoi)

| Élément | Quand |
|---|---|
| Nom d'appareil / attribut / plage sur les faders et au survol (CONS-007, CONS-041) | P3 (patch) |
| Délimitation des appareils dans le moniteur (CONS-043, 1ʳᵉ partie) | P3 |
| Blackout et limites de sûreté appliqués aux faders pris (CONS-008) | P4 / P5 |
| Bouton « Figer » de la maquette | P5 (CMD-003) |
| Moniteur dans une fenêtre séparée (CONS-044, S) | non réalisé |

## Grille de retour

| Exemple | Correct | À revoir | Idée / remarque |
|---|---|---|---|
| 1 – Prendre / libérer | ☐ | ☐ | |
| 2 – Modes de saisie | ☐ | ☐ | |
| 3 – Sélection multiple | ☐ | ☐ | |
| 4 – Pages et moniteur | ☐ | ☐ | |
| 5 – Instantanés | ☐ | ☐ | |
| 6 – SC-01 | ☐ | ☐ | |
