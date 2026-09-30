# Analyse ergonomique de fin de chantier – « Contrôle 2 »

> Doc 32 §5.6. Rédigée le 2026-09-30 dans la discussion de développement, à partir de l'essai
> ([essais/ERG2-resultats.md](../essais/ERG2-resultats.md), v1.007.080, deux revérifications jusqu'à la 1.007.115) et des
> remarques de l'utilisateur. Décisions de l'utilisateur du 2026-09-30 : **la taille d'interface 125 % / 150 % est abandonnée
> pour le moment** (option masquée), la version est **validée**.

## 1. Ce que l'essai a montré

| Sujet | Verdict | Suite |
|---|---|---|
| Écran de jeu sans modes (maquette 5) | « Nettement plus clair » ; conforme à 1366 × 768 | Validé (ERG-032) |
| Fenêtre d'édition en brouillon (maquettes 6 et 7) | Conforme ; la croix **demande** avant d'abandonner (décision de l'utilisateur), une seule fenêtre, mémoire de la taille, de la position et de l'état maximisé | Validé (ERG-033, 034, 035) |
| Arbre de groupes, dimmers, niveau de couche | Conformes ; « Tous voir » et confirmation de déplacement ajoutés à la demande | Validé (ERG-036, 037, 039) |
| Seconde platine MIDI | Conforme au matériel (MK1 et MK2) ; repères par page, choix dans l'interface | Validé (ERG-038) |
| Défauts vus (fermeture bloquée, fenêtres multiples, bandeau qui décale, verrou, affichages) | Tous corrigés et revérifiés | — |

Principe retenu à l'essai et à garder pour la suite : **un état affiché doit toujours être l'état réel** (bouton du verrou,
repères de fader, bandeau), et **toute mise en page doit rester stable quand une information apparaît** (le bandeau
« Retouches en direct » a une place fixe).

## 2. Recommandations

### 2.1 Décidé maintenant

| # | Recommandation | Décision |
|---|---|---|
| 1 | **Taille d'interface 125 % / 150 %** : jugée inexploitable (« tout est trop gros ») et sans utilité pour l'utilisateur | **Masquée** (menu Affichage retiré, interface toujours à 100 %) ; la commande et la préférence restent dans le code pour plus tard (idée doc 99) |
| 2 | **Modes LIVE / ÉDITION / AVEUGLE** | Remplacés par l'écran de jeu et la fenêtre d'édition (doc 60 §4.9) ; ERG-010, 011, 012, 024 abandonnées |
| 3 | **Retouches d'appareils en direct** sur l'écran de jeu | Non : dimmers, niveaux de couche, looks ; le reste dans la fenêtre d'édition |
| 4 | **Aperçu du plan sur l'écran de jeu** | Non retenu (l'écran reste épuré) ; le Simulateur et le plan de la fenêtre d'édition suffisent |

### 2.2 À planifier (par ordre de valeur)

| # | Recommandation | Pourquoi | Taille |
|---|---|---|---|
| A | **Notifications colorées** (vert / bleu / orange / rouge, disparition automatique, croix) avec **inventaire de tous les messages** de l'application | Les refus et avertissements sont discrets et ne se voient que dans la fenêtre où ils naissent (idée de l'utilisateur, essai exemples 6 et 9) | Chantier à part, transverse |
| B | **Retirer les écrans Live et Scènes** (choix C5) | L'écran de jeu et la fenêtre d'édition les remplacent ; ils font double emploi et doublent la maintenance | Un lot, après accord |
| C | **Retirer le mode LIVE de la session d'édition** (surcharges d'appareils, pastilles jaunes, rappel de mode) | Code mort en production depuis l'abandon du mode LIVE ; simplifie `ControlSession` et les panneaux | Un lot (voir l'analyse de code) |
| D | **Glisser-déposer** des appareils vers l'arbre de groupes | Prévu par la maquette 8, remplacé par des boutons | Petit |
| E | **Apprentissage MIDI** « Affecter… » (MIDI-008) | Reporté depuis P5 ; les deux platines ont maintenant des rôles fixes | Un lot |
| F | **Vocabulaire et aide « ? »** des nouveaux panneaux (Groupes dimmer, fenêtre d'édition) à relire à l'usage | Une aide de trois lignes par panneau existe ; à juger en soirée | Petit |

## 3. Suite

Fusion de `ergo/controle-2` dans `main` (étiquette `v1.007`), puis P7 (BPM) ; les lots A à F sont rangés dans la feuille de
route (doc 40) et les idées (doc 99), à décider au fil des phases.
