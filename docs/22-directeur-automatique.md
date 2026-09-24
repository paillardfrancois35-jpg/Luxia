# 22 – Directeur automatique

> Cahier des charges – module **Directeur** (préfixe `AUTO`). Phase principale : **P10** → **Jalon 2 « soirée automatique »**.
> Références : [02 §13](02-principes-et-architecture-fonctionnelle.md), [18 §4](18-live.md), [19](19-audio-et-tempo.md), [20](20-show-et-sequences.md), [21](21-lecture-en-cours-et-style.md).

---

## 1. Rôle

Quand le mode **AUTO** est actif, le Directeur **choisit et pilote des shows** en fonction de la musique, pour tenir une soirée
dansante **sans intervention**, sans répétition lassante, et en respectant les verrous et la sûreté.

Le Directeur **ne manipule jamais les canaux** : il émet des commandes (lancer un show, forcer une transition, régler un master,
poser un verrou, lancer une scène de transition) avec l'origine `Directeur`.

```
 Style + confiance ─┐
 Énergie, niveau,   │      ┌──────────────── DIRECTEUR ────────────────┐
 break/drop/montée ─┼────▶ │ 1. Sélection du show (style, énergie,     │──▶ LancerShow / ForcerTransition
 Tempo, mesures ────┤      │    historique, poids)                     │──▶ Scènes de transition
 Morceau changé ────┤      │ 2. Rotation / variété                      │──▶ Masters / intensité max
 Silence, pub ──────┤      │ 3. Règles de réaction                      │──▶ Verrous temporaires
 Verrous, reprises ─┘      │ 4. Contraintes et budgets                  │──▶ Journal des décisions (motifs)
 manuelles                 └───────────────────────────────────────────┘
```

## 2. Ce que l'utilisateur prépare

| Élément | Description |
|---|---|
| **Shows candidats** | Shows (doc 20) portant des métadonnées : styles visés (un ou plusieurs, ou « tous »), plage d'énergie, **poids**, durée minimale / maximale de jeu, rôle |
| **Rôles de shows** | `Principal` (jeu normal), `Transition` (court, entre deux morceaux), `Attente` (silence, pause, pub), `Slow`, `Ouverture` (premier morceau de l'auto) |
| **Show par défaut** | Utilisé pour le style **Inconnu** : piloté uniquement par l'énergie |
| **Thèmes de couleurs par style** | Palettes de combinaisons (PAL-010) associées aux familles de styles (ex. Latino → chaud) : un même show générique peut ainsi prendre l'esthétique du style |
| **Réglages** | Délais, budgets, anti-répétition, reprise en main (§5) |

> Objectif de contenu (pas une exigence logicielle) : **au moins 2 à 3 shows principaux par famille** fréquente, + 1 show générique solide.

## 3. Décisions

### 3.1 Au changement de morceau

1. Recevoir `MorceauChangé` ; attendre `StyleDétecté` (délai max réglable, défaut 3 s ; sinon style **Inconnu**).
2. Jouer un show de rôle **Transition** (optionnel, réglable : aucune / fondu au noir court / flash / show de transition), quantifié sur le premier temps détecté du nouveau morceau si possible.
3. Choisir le show principal :
   - **filtrer** : rôle `Principal`, style compatible (style détecté, ou famille « tous »), plage d'énergie compatible avec l'énergie courante, non exclu par un verrou ;
   - **écarter** les N derniers shows joués (défaut N = 3) sauf s'il ne reste plus rien ;
   - **tirer au sort** parmi les restants selon leurs **poids** (pondérés par l'ancienneté de leur dernière diffusion) ;
   - si la **confiance** du style est faible (< seuil, défaut 0,5) : préférer les shows multi-styles / le show par défaut.
4. Appliquer le **thème de couleurs** du style (si le show le permet).
5. Journaliser la décision avec ses **motifs**.

### 3.2 Pendant le morceau

| Situation | Réaction (réglable) |
|---|---|
| Durée max de jeu du show atteinte (ex. morceau long, mix continu) | Changer de show **au prochain début de phrase** (8 / 16 mesures) |
| Énergie durablement hors de la plage du show (≥ 2 phrases) | Changer pour un show compatible, au début de phrase |
| Break / drop / montée | Transmis au show (ses transitions y réagissent) ; si le show ne gère pas ces événements : réaction par défaut du Directeur (ex. drop → flash + strobe court, break → mouvements ralentis) |
| Silence / pause | Show `Attente` après 3 s |
| Pub | Show `Attente` ; pas de changement de « dernier show joué » |
| Énergie basse durable + tempo lent | Bascule vers un show `Slow` même si le style dit autre chose |
| Style corrigé par l'utilisateur | Nouveau choix de show au prochain début de phrase |

### 3.3 Exigences

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| AUTO-001 | I | P10 | Activation / désactivation (CMD-060) ; à l'activation, choix immédiat d'un show selon l'état courant (rôle `Ouverture` si défini). | — |
| AUTO-002 | I | P10 | Décisions au changement de morceau conformes au §3.1. | Tests par scénario. |
| AUTO-003 | I | P10 | Réactions pendant le morceau conformes au §3.2, chacune activable / désactivable. | Tests par scénario. |
| AUTO-004 | I | P10 | **Anti-répétition** : un show n'est pas rejoué avant N autres (défaut 3) quand d'autres candidats existent ; pondération par ancienneté. | Simulation 200 morceaux : aucune violation, distribution conforme aux poids. |
| AUTO-005 | I | P10 | **Style Inconnu** et confiance faible gérés par le show par défaut / les shows multi-styles. | — |
| AUTO-006 | I | P10 | Tous les changements de show sont **quantifiés** (temps, mesure ou phrase) : jamais de changement « à contretemps ». | Vérification des instants dans le journal de simulation. |
| AUTO-007 | I | P10 | Chaque décision publie `DécisionDirecteur` (EVT-050) avec le show choisi, les candidats écartés et le **motif** lisible (« style Rock 0,82 ; énergie Énergique ; Rock A joué il y a 2 morceaux → Rock B »). | Affichage en Live. |
| AUTO-008 | M | P10 | **Thèmes de couleurs par style** appliqués aux shows qui l'acceptent. | Même show générique → couleurs chaudes en Latino, froides en Électro. |
| AUTO-009 | M | P10 | Si aucun show candidat n'existe (projet incomplet), repli sur le show par défaut puis, à défaut, sur une scène neutre ; avertissement en Live. | — |

## 4. Verrous et budgets

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| AUTO-020 | I | P10 | **Verrous** (CMD-061, LIVE-025) : pas de strobe, pas de mouvement, pas de fumée, intensité max, couleur imposée, style imposé, show imposé. Le Directeur n'émet aucune commande qui les enfreint ; le moteur les applique aussi comme filtre final. | Verrou strobe → aucune scène de strobe, y compris depuis un show. |
| AUTO-021 | I | P10 | **Budget d'effets agressifs** (GEN-087) : temps cumulé de strobe / flashs par période glissante (ex. 30 s / 5 min), budget de **fumée** (ex. 3 rafales / 10 min), réglables. | Endurance : budgets jamais dépassés. |
| AUTO-022 | M | P10 | **Ambiances rapides** (LIVE-027) : Slow, Calme, Fête, Pause → forcent rôle/style/énergie jusqu'à annulation. | — |
| AUTO-023 | M | P10 | Plages horaires facultatives : ex. « avant 22 h : intensité max 60 %, pas de strobe » (repas, discours). | — |

## 5. Cohabitation manuel / automatique

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| AUTO-040 | I | P10 | Toute commande manuelle (souris, clavier, MIDI) sur une couche pendant le mode auto fait passer cette couche en **manuel** : le Directeur et le show n'y touchent plus (LIVE-026). | Test. |
| AUTO-041 | I | P10 | Retour au Directeur : bouton par couche / global, et automatiquement au **changement de morceau** ou après un délai d'inactivité (réglable, défaut : au changement de morceau). | Test. |
| AUTO-042 | I | P10 | Les actions permanentes (blackout, flash, fumée, figer, Grand Master) restent **toujours** prioritaires et ne désactivent pas le mode auto. | Flash en auto → flash, puis reprise normale. |
| AUTO-043 | I | P10 | Désactiver le mode auto **conserve** l'état lumineux courant (pas de coupure) ; l'utilisateur reprend à partir de là. | Test. |

## 6. Réglages et répétition

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| AUTO-060 | I | P10 | Écran **Directeur** : liste des shows candidats avec leurs métadonnées (éditables en tableau), couverture par style / énergie (« Latino : 1 show, Énergique : 0 → à compléter »), réglages du §3 à §5. | Tableau de couverture. |
| AUTO-061 | I | P10 | **Mode répétition** : jouer une vraie playlist au PC, mode auto actif, sortie = simulateur (sans matériel) ; journal complet. | Répétition de 1 h au simulateur. |
| AUTO-062 | I | P10 | **Simulation accélérée** sans audio (outil sans interface) : flux d'événements scénarisés (morceaux, styles, énergies, drops) sur plusieurs heures simulées → statistiques (shows joués, répétitions, budgets, temps par style). | 6 h simulées en < 1 min, rapport produit. |
| AUTO-063 | M | P10 | Après une soirée : **rapport** (morceaux, styles, shows joués, corrections, limites atteintes) pour améliorer le contenu. | — |

## 7. Tests

| Test | Type | Contenu |
|---|---|---|
| T-AUTO-01 | Unitaire | Filtrage et tirage des candidats ; anti-répétition ; confiance faible ; Inconnu ; Pub ; Slow. |
| T-AUTO-02 | Unitaire | Verrous et budgets (y compris via les shows). |
| T-AUTO-03 | Unitaire | Manuel / auto : couche manuelle, retour au changement de morceau, actions permanentes. |
| T-AUTO-04 | Simulation | 6 h simulées, 3 profils de soirée (mariage, anniversaire, soirée dansante) → statistiques de variété et respect des contraintes. |
| T-AUTO-05 | Répétition | 3 h de playlist réelle au simulateur (jalon 2, étape 1). |
| T-AUTO-06 | Terrain | Soirée réelle en mode auto (jalon 2, étape 2), rapport de soirée analysé. |
