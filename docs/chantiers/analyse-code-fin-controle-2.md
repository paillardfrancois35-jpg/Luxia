# Analyse de code de fin de chantier – « Contrôle 2 »

> Demande de l'utilisateur du 2026-09-30 : nettoyage, commentaires, code mort, besoin de découper ou de factoriser.
> Méthode : recherche des membres sans usage, balayage des `TODO` et du code commenté, taille des fichiers, relecture des
> modules touchés par le chantier. Ce qui a été fait est marqué ✅ ; le reste est recommandé et chiffré.

## 1. Constat global

| Mesure | Résultat |
|---|---|
| Avertissements de compilation | 0 ; `dotnet format --verify-no-changes` propre (usings, style) |
| `TODO` / `FIXME` | 1 seul, légitime et rattaché à une phase : `ShowCompiler.cs` (P7, MOT-016) |
| Code commenté laissé en place | aucun |
| Membres privés inutilisés (analyseurs IDE0051, 0052, 0060, 0059) | aucun |
| Commentaires | documentation XML en français sur l'API publique, avec les identifiants d'exigences ; les commentaires expliquent le pourquoi |
| Tests | 770 (hors matériel), tous verts |

## 2. Fait dans ce chantier ✅

| Sujet | Avant | Après |
|---|---|---|
| **`ControlViewModel` (ancien écran Contrôle)** : après la refonte, il n'était plus utilisé que par la fenêtre d'édition, mais portait encore les colonnes, les looks, les dimmers, le verrou, Stop, le bandeau de mode, les dispositions… en double de `GameViewModel` | ≈ 330 lignes, à moitié mort en production | **`EditBenchViewModel`** (≈ 160 lignes : session, Plan, Réglages, Effets, Propriétés, journal, annuler / rétablir) ; le reste n'existe plus qu'à l'écran de jeu |
| Journal de la fenêtre d'édition | Ses traces allaient dans un journal jamais affiché | La fenêtre d'édition écrit dans **le journal de l'écran de jeu** |
| `ColumnsPanelViewModel` | Gardait un chemin « scène choisie dans la session » (ancien modèle à modes) | Une seule voie : ✎ → fenêtre d'édition ; contour de la scène ouverte |
| Tests | Un fichier de 720 lignes mêlant écran de jeu et édition | `GamePanelsTests` (colonnes, looks, journal, Stop) et `EditBenchPanelsTests` (plan, réglages, propriétés) ; 3 tests devenus sans objet retirés (mode LIVE, ✎ qui « choisit »), d'autres couverts par `GameViewModelTests` |
| Option 125 % / 150 % | Menu Affichage | Masquée (décision de l'utilisateur) ; le code reste |
| Dépôt | `samples/*/Versions/` pouvait polluer le dépôt si l'on ouvrait l'échantillon | Ignoré par Git |

## 3. Recommandé (non fait : risque ou taille)

| # | Sujet | Constat | Recommandation |
|---|---|---|---|
| 1 | **`ControlSession` (≈ 960 lignes)** | Mélange la sélection, le mode, le brouillon (chantier), les surcharges LIVE (code mort en production), l'historique, l'envoi aux moteurs | Retirer le **mode LIVE** (surcharges d'appareils, `StateOf` jaune, rappel de mode) : environ 150 lignes en moins et des panneaux plus simples ; puis extraire le brouillon (`DraftScenes`) et l'envoi aux moteurs (`OutputPusher`) en classes à part. ≈ 40 tests à adapter |
| 2 | **`RenderEngine` (≈ 1 320 lignes), `Playback` (≈ 1 000)** | Gros mais cohérents (une chaîne de rendu, D26) ; testés en profondeur, sans allocation | Les découper en classes partielles par étape (fusion, masters, sûreté, publication) **sans changer le code** ; à faire avec P7 (horloge, tempo) qui y touche de toute façon |
| 3 | **`EffectsPanelViewModel` (≈ 880), `SettingsPanelViewModel` (≈ 700), `InstallationViewModel` (≈ 720)** | Panneaux qui font tout | Même méthode que pour les dimmers (modèle de vue enfant par onglet / zone) : Effets = bibliothèque + éditeur de thèmes + aperçu ; Installation = un modèle de vue par onglet |
| 4 | **Motif « niveau affiché gardé jusqu'à confirmation du moteur »** | Répété dans les niveaux de couche, les dimmers et le Grand Master (`EngineEcho`, `Sync`, garde `_syncing`) | Une classe de base commune pour les faders à niveau (un demi-jour, sans risque si les trois écrans sont testés) |
| 5 | **Écrans Live et Scènes** | Font double emploi avec l'écran de jeu et la fenêtre d'édition | Les retirer (recommandation B de l'analyse ergonomique) : ≈ 4 900 lignes (1 300 + 3 600) et leurs tests en moins |
| 6 | **Prototype** (`tools/Luxia.Tools.Prototype`) et maquettes | Outil d'étude, plus utilisé par l'application | Le garder pour les maquettes futures ; ne pas y investir |

## 4. Règle à retenir

Quand un écran change de rôle (ici : l'écran Contrôle devenu écran de jeu), **renommer et purger son modèle de vue tout de
suite** plutôt que de le laisser porter l'ancien rôle : c'est ce qui avait laissé un « établi » à moitié mort et un journal
invisible.
