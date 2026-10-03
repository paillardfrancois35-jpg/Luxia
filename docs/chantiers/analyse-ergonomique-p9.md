# Analyse ergonomique – fin de P9 « Lecture en cours et style »

> Doc 32 §5.6, **proposition** rédigée le 2026-10-03 d'après les captures de l'écran de jeu et de la fenêtre « Base musicale »
> (`luxia-captures`, 1680 × 1050) **avant l'essai** : elle sera complétée par ce que l'utilisateur dira à l'essai. Statuts :
> ⏳ proposé · ❓ décision de l'utilisateur · ✅ fait.

> **Mise à jour du 2026-10-03 (après le premier essai, 1.011.075)** : les constats sur la fenêtre « Base musicale » ci-dessous portent sur l'**ancienne** fenêtre ; elle a été
> refaite en « liste + fiche » ([doc 60 §4.11](../60-ergonomie.md), ERG-040 à ERG-042) : captures [fiche](captures-p9/base-musicale-fiche.png),
> [fiche modifiée](captures-p9/base-musicale-fiche-modifiee.png), [À classer](captures-p9/base-musicale-a-classer.png). **Reste à auditer** (Q59) : les autres écrans
> « liste + fiche » (Bibliothèque, Installation, effets, scènes, shows) face à cette charte ; l'analyse complète est à proposer avant la validation de la v1.011.

## 1. Ce que montrent les captures

| Écran | Constat | Nature |
|---|---|---|
| Écran de jeu, bloc « Morceau en cours » | Une ligne de 46 px sous le bloc BPM : lisible, hauteur fixe, texte coupé par « … » sans casser la mise en page (titre de 140 caractères testé) | Bon |
| Écran de jeu | Trois lignes d'en-tête avant les colonnes (Stop et verrou, BPM, morceau) puis la ligne des retouches : l'écran reste utilisable à 1680 × 1050 mais la zone des colonnes perd ≈ 150 px | À surveiller en 1366 × 768 |
| Fenêtre « Base musicale » | Trois onglets, boutons de famille en liste de 14 boutons larges : clair, mais les noms de familles sont longs (« Festif / Tubes de soirée ») | Acceptable |
| Onglet « À classer » | Une touche par famille (1 à 9, 0, Q, W, E, R) : rapide, mais le lien touche ↔ famille n'est que dans un texte gris en bas | À améliorer |

## 2. Propositions

| # | Gravité | Constat | Proposition | Coût | Suite |
|---|---|---|---|---|---|
| **E1** | Moyenne | Le bloc « Morceau en cours » et le panneau « Pilote automatique » (« Titre en cours : — », « Style : — ») disent la même chose, l'un vrai, l'autre vide | Brancher les deux champs du panneau sur le même état, ou retirer ces deux lignes du panneau jusqu'à P10 | petit | ⏳ à décider |
| **E2** | Moyenne | Les touches de l'onglet « À classer » ne sont écrites que dans une ligne de texte gris | Afficher la touche **sur chaque bouton de famille** (« 5 · Rock »), comme les chiffres d'un pavé | petit | ⏳ |
| **E3** | Moyenne | Corriger le style demande 3 clics (Corriger ▾ → « Pour cet artiste » → famille) dans un menu à deux niveaux | Essayer en soirée ; si c'est lent : les 3 familles les plus corrigées en tête du menu, ou un menu de 14 familles avec un seul choix « artiste / titre » en tête | petit | ❓ à juger à l'essai |
| **E3b** | Basse | Une couleur seule (vert / orange / rouge / gris) porte la confiance de la puce de style | Le pourcentage est déjà écrit ; garder les deux | — | ✅ |
| **E4** | Basse | « Saisir… » ouvre une bulle sans validation par Entrée | Entrée dans « Artiste » valide | petit | ⏳ |
| **E5** | Basse | Les noms de familles sont longs dans la puce et dans les boutons | Nom court affiché (« Électro », « Festif ») en gardant le nom entier en info-bulle | petit | ❓ |
| **E6** | Moyenne | Un titre inconnu joué en soirée n'est signalé que par une puce grise : on peut ne pas le voir | Petit compteur « n à classer » sur le bouton « Base… » (à partir du journal de soirée) | moyen | ⏳ |
| **E7** | Basse | Le journal de soirée est un fichier CSV : pas de consultation à l'écran | Hors périmètre ; l'onglet « À classer » en est la vue utile | — | ✅ décidé (P9) |
| **E8** | Basse | « Base… » et ses trois onglets sont dans une fenêtre à part (Q50) : pas de bouton pour la rouvrir depuis le menu de l'application | Entrée « Projet → Base musicale… » | petit | ⏳ |

## 3. Ce que l'essai doit trancher

E3 (nombre de clics pour corriger) et E5 (noms de familles) ne se jugent qu'avec de la musique réelle : le guide demande, aux exemples 5 à 7 et 18, **combien de temps** prend chaque geste.
