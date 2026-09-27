# Chantier ergonomique – prototype technique et maquettes « Contrôle »

> Branche `ergo/analyse`, version de développement **1.005** (étiquettes `v1.005.NNN`). Doc de référence :
> [60 – Ergonomie](../60-ergonomie.md) §7.2, §7.3, §9, §10. Exigences ERG-001 à ERG-007.

Le prototype est une **application séparée** : `LuXia-Prototype.exe`. Il ne lit aucun projet et n'envoie rien en DMX ;
LuXia peut rester fermé ou ouvert, ils ne se gênent pas. Une seule fenêtre du prototype à la fois.

```bash
dotnet run --project tools/Luxia.Tools.Prototype
```

La barre de titre affiche **« LuXia – prototype ergonomique v1.005.NNN »** : le numéro à vérifier est donné dans la
discussion.

## 1. Ancrage des panneaux (ERG-001)

| # | Faire | Attendu |
|---|---|---|
| 1 | Au lancement | Disposition **Contrôle** : Colonnes et Galerie au centre (onglets), Propriétés et Mesures à droite, Plan des appareils et Position / Couleur / Journal en bas. |
| 2 | Glisser l'onglet **Couleur** par son titre vers le bord droit de la fenêtre, lâcher sur une flèche d'ancrage | Le panneau se colle à droite. |
| 3 | Glisser l'onglet **Journal** sur le groupe de **Propriétés** (flèche centrale) | Il s'empile en onglet avec Propriétés. |
| 4 | Glisser **Position** hors de la fenêtre (ou menu ▾ du panneau → *Float* : ces menus de Dock sont en anglais, écart noté au doc 60 §10) | Il s'ouvre dans sa propre fenêtre, déplaçable sur le deuxième écran. |
| 5 | Épingle 📌 d'un panneau | Il se replie sur le bord ; un clic sur son nom le rouvre. |
| 6 | Croix ✕ d'un panneau, puis **Panneaux ▾** | Le panneau est marqué « (fermé) » ; le choisir le remet à sa place. |
| 7 | Bouton **?** en haut à droite d'un panneau | Trois lignes : ce qu'est le panneau, à quoi il sert (F6). |
| 8 | **Disposition : Spectacle** puis **Contrôle** | Colonnes en grand + Pilote automatique + Journal ; retour à Contrôle tel qu'on l'avait laissé. |

## 2. Enregistrement de la disposition (ERG-002)

| # | Faire | Attendu |
|---|---|---|
| 1 | Réorganiser (étapes du §1), attendre 2 s | Barre d'état : « Disposition « Contrôle » enregistrée à hh:mm:ss — …\disposition-controle.json ». |
| 2 | Fermer le prototype, le relancer | Même disposition, **fenêtre détachée comprise**, panneaux fermés toujours fermés. |
| 3 | **Rétablir la disposition** | Retour à la disposition livrée. |

## 3. Grille Pan / Tilt (ERG-003) — panneau Position

| # | Faire | Attendu |
|---|---|---|
| 1 | Lyres 1 et 2 cochées : cliquer dans la grille | Les deux points viennent sous le curseur **en gardant leur écart**, puis suivent le glisser. |
| 2 | Pousser le groupe contre un bord | Le groupe s'arrête entier ; aucun point n'est écrasé contre le bord. |
| 3 | Décocher la lyre 2, **Maj + glisser** | Réglage fin (dix fois plus lent), sans saut. |
| 4 | Molette ; Maj + molette ; Ctrl + molette ; flèches | Tilt fin ; Pan fin ; ×10 ; pas fin. Degrés lisibles en haut (Pan sur 540°, Tilt sur 270°). |
| 5 | **Éditer les zones** : glisser dans le vide | Nouvelle zone rouge (interdite). Avec « Zone permise », rectangle vert pointillé, l'extérieur assombri. |
| 6 | Glisser une zone ; tirer ses 8 poignées ; Suppr | Déplacée ; redimensionnée sans jamais se retourner ; retirée. |

## 4. Sélecteur de couleur (ERG-004) — panneau Couleur

| # | Faire | Attendu |
|---|---|---|
| 1 | Glisser dans le carré, puis dans la barre de droite | Teinte / saturation, puis intensité ; aperçu et valeurs en % à droite ; les points de la grille Pan / Tilt prennent la couleur. |
| 2 | Molette sur le carré ; sur la barre (Maj : ×10) | Teinte ±1° ; intensité ±1 %. |
| 3 | **+** ; clic sur un favori ; clic droit sur un favori | Ajouté ; repris ; retiré. |

## 5. Galerie (ERG-005) et Mesures (ERG-006)

- Onglet **Galerie** : chaque composant dans ses états ; tous répondent à la souris.
- Onglet **Mesures** : pendant un glisser rapide sur la grille ou le carré de couleur, relever **images par seconde**,
  **demandes / s** et **mémoire** ; puis avec une fenêtre détachée sur le deuxième écran. Valeurs à noter dans la fiche ERG-006.
- Captures sans écran : `LuXia-Prototype --captures <dossier>` (galerie, dispositions), `--maquettes <dossier>` (maquettes).

## 6. Maquettes « Contrôle » (ERG-007) — à valider avant tout développement des écrans

Images dans [docs/maquettes](../maquettes/) (1920 × 1080, rendues par Avalonia avec les vrais composants et des données
fictives), et ouvrables en vrai par le bouton **Maquettes ▾** du prototype (panneaux déplaçables) :

| Image | Montre |
|---|---|
| `maquette-1-controle-live.png` | Mode **LIVE** (par défaut) : on joue ; deux lyres retouchées en direct (pastilles jaunes, « Libérer ») |
| `maquette-2-controle-edition.png` | Mode **ÉDITION** : l'étape 2 du « Chenillard 4 couleurs » corrigée sur les 4 PAR (pastilles vertes, bandeau vert) |
| `maquette-3-controle-aveugle.png` | Mode **AVEUGLE** : position des lyres préparée dans « Lyres sur 3 positions » sans toucher la sortie (plan en aperçu) |
| `maquette-4-controle-zones.png` | Zones de la lyre 1 : interdite (public) et permise (limites), propres au lieu |

Points à valider : question **Q35** ([doc 01](../01-questions-ouvertes.md)).
