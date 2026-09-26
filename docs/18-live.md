# 18 – Écran Live

> Cahier des charges – module **Live** (préfixe `LIVE`). Phase principale : **P5** (Live v1), enrichi en **P7** (musique), **P8** (show), **P10** (auto).
> Références : [02 §11, §12, §15](02-principes-et-architecture-fonctionnelle.md), [17](17-couches-et-palettes.md), [18b (MIDI)](18b-controleurs-midi.md), [22 (Directeur)](22-directeur-automatique.md).

---

## 1. Rôle et principes

L'écran de **jeu en soirée**. Il doit pouvoir être utilisé **dans le noir, d'une main, sans réfléchir** :

- tout ce qui est vital est **visible en permanence** et à **un clic / une touche** ;
- **aucune fenêtre modale** (P9), aucune action destructrice ;
- thème sombre, gros éléments, contrastes forts, pas de zones blanches ;
- souris + clavier + APC mini (pas de tactile).

## 2. Disposition

```
┌─ BANDEAU D'ÉTAT ───────────────────────────────────────────────────────────────────────────────────────────┐
│ ● Sortie OK 40 t/s │ ♪ « Titre – Artiste » │ Style : Rock (82 %) [✎] │ 124,0 BPM ●○○○ (audio) │ Énergie ▮▮▮▮▯ │ AUTO: OFF │
├─ COUCHES (colonnes) ──────────────────────────────────────────────────────────┬─ ACTIONS PERMANENTES ─────────┤
│ Intensité   Couleurs     Mouvements   Faisceau   Effets     Ambiance          │  [   BLACKOUT   ]             │
│ [Plein]     [Rouge  ]▶   [Piste  ]    [Gobo 1]   [Strobe]   [UV    ]          │  [ FLASH ] [ STROBE ]        │
│ [50 %]      [Bleu   ]    [Cercle ]▶   [Gobo 2]   [Prog 1]   [Fumée ]          │  [ FUMÉE (maintien) ]         │
│ [Vague]▶    [Arc-ciel]   [Balay. ]               …                            │  [ TAP ] [×2] [÷2] [Figer]    │
│ …           …            …                                                    │  [   AUTO ON / OFF   ]        │
│ ■ stop      ■ stop       ■ stop       ■ stop     ■ stop     ■ stop            │                              │
│ ▮ master    ▮ master     ▮ master     ▮ master   ▮ master   ▮ master          │  ▮ GRAND MASTER               │
├─ PALETTES RAPIDES (sélection : [Tous PAR] [Lyres] [Barres]) ───────────────────┴──────────────────────────────┤
│ Couleurs : [■][■][■][■][■][■]…   Positions : [Piste][Boule][Public?][Repos]   Intensité : [100][50][20]      │
├─ SHOW / AUTOMATIQUE ────────────────────────────────────────────┬─ VERROUS ─────────────┬─ JOURNAL ──────────┤
│ Show : Rock B – étape « Refrain » ─▶ prochaine : « Couplet »     │ ☐ Pas de strobe       │ 22:41 Morceau…     │
│ [Forcer la transition] [Changer de show ▾]                        │ ☐ Pas de mouvement    │ 22:41 Show Rock B  │
│ Décision : « style Rock, énergie haute, pas joué depuis 40 min »  │ ☐ Pas de fumée        │ 22:40 Drop détecté │
│                                                                   │ Intensité max [100 %] │                    │
└───────────────────────────────────────────────────────────────────┴───────────────────────┴────────────────────┘
```

## 3. Exigences – structure

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| LIVE-001 | I | P5 | **Bandeau d'état** permanent : état de la sortie, blackout, figé, mode auto, et (dès disponibles) morceau, style, BPM + battement, énergie. | Revue. |
| LIVE-002 | I | P5 | **Colonnes de couches** : pour chaque couche, les scènes **visibles en Live**, dans l'ordre de la couche ; la scène active est mise en évidence avec sa progression (étape, barre) ; bouton stop et master de couche. | Revue. |
| LIVE-003 | I | P5 | Un clic sur une scène la lance (CMD-010) ; un clic sur la scène active l'arrête (réglable : arrête / relance). Clic maintenu sur une scène de couche Flash = flash. | Test. |
| LIVE-004 | I | P5 | **Actions permanentes** toujours visibles : Blackout, Flash, Strobe (maintien), Fumée (maintien + rafale), Tap / ×2 / ÷2, Figer, Auto, Grand Master. | Revue. |
| LIVE-005 | I | P5 | **Palettes rapides** : choix d'une sélection d'appareils puis clic sur une palette → surcharge des attributs correspondants (programmeur live) ; bouton « Libérer » pour rendre la main aux couches. | Forcer les lyres sur « Boule » puis libérer. |
| LIVE-006 | M | P5 | Disposition personnalisable : ordre et largeur des colonnes, zones affichées / masquées ; enregistrée dans le projet. | — |
| LIVE-007 | M | P5 | Mini-simulateur optionnel dans l'écran Live (ou simulateur sur second écran, SIM-007). | — |
| LIVE-008 | I | P5 | Indication visible de toute **limite de sûreté** active et de tout **verrou** (GEN-086). | — |
| LIVE-009 | M | P5 | **Journal** défilant des derniers événements (morceaux, décisions, limites, erreurs). | — |
| LIVE-010 | I | P5 | Alerte non bloquante et visible si la sortie est déconnectée ou si un module est en erreur (GEN-093). | Débrancher l'Arduino → alerte rouge, reconnexion affichée. |
| LIVE-011 | M | P5 | Accès à l'**assistant d'installation** (INST-070) et à la **calibration des positions** sans quitter le Live. | — |

## 4. Exigences – musique et automatique

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| LIVE-020 | I | P7 | Affichage du BPM, de la source (audio / tap / fixe), de la confiance, d'un indicateur de battement (1-2-3-4) et de l'énergie. | Revue. |
| LIVE-021 | I | P7 | Tap tempo, ×2, ÷2, ±, recalage de phase (« le 1 est maintenant »), choix de la source. | — |
| LIVE-022 | I | P9 | Affichage du morceau en cours et du **style détecté** avec sa confiance ; bouton **Corriger le style** (liste des styles) → correction appliquée à ce titre ou à cet artiste, mémorisée (MUS). | Corriger → style changé, base musicale mise à jour. |
| LIVE-023 | I | P8 | Zone **Show** : show en cours, étape active, prochaines transitions possibles ; forcer une transition ; lancer / arrêter un show. | — |
| LIVE-024 | I | P10 | Bouton **AUTO** (on/off) ; affichage de la **dernière décision** du Directeur et de son motif. | — |
| LIVE-025 | I | P10 | **Verrous** : pas de strobe, pas de mouvement, pas de fumée, intensité maximale, couleur imposée (palette), style imposé ; activables en un clic. | Verrou « pas de strobe » → aucune scène de strobe lancée par le Directeur. |
| LIVE-026 | I | P10 | **Reprise en main** : une action manuelle sur une couche pendant le mode auto la passe en **manuel** (repère visuel) ; bouton « Rendre au Directeur » par couche et global ; rendu automatique au changement de morceau (réglable). | Lancer une couleur à la main en auto → couche Couleurs « manuel » jusqu'au morceau suivant. |
| LIVE-027 | M | P10 | Boutons d'**ambiance rapide** : « Slow », « Calme », « Fête », « Pause » (forcent un style/énergie au Directeur jusqu'à annulation). | — |

## 5. Raccourcis clavier (proposition)

Actifs quel que soit le focus en Live (GEN-071). Les touches à maintenir n'agissent que tant qu'elles sont enfoncées.

| Touche | Action | Type |
|---|---|---|
| **Espace** | Tap tempo | appui |
| **B** | Blackout (bascule) | appui |
| **F** | Flash blanc général | maintien |
| **S** | Strobe général | maintien |
| **Z** | Fumée | maintien |
| **Ctrl + A** | Mode auto on/off (combinaison pour éviter les appuis accidentels) | appui |
| **G** | Figer (bascule) | appui |
| **Échap** | Libérer les palettes rapides / surcharges live | appui |
| **1 … 9** | Lancer la scène N de la couche sélectionnée | appui |
| **← →** | Changer de couche sélectionnée | appui |
| **Page ↑ / ↓** | Grand Master ± 10 % | appui |
| **N** | Forcer la transition suivante du show | appui |

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| LIVE-040 | I | P5 | Raccourcis du tableau ci-dessus. | Test manuel. |
| LIVE-041 | S | P5 | Raccourcis personnalisables. | — |

## 6. Performances

| ID | Pri. | Phase | Exigence | Critère d'acceptation |
|---|---|---|---|---|
| LIVE-060 | I | P5 | L'écran Live se rafraîchit à ≥ 20 images/s sans affecter le moteur. | Mesure. |
| LIVE-061 | I | P5 | Latence clic → sortie < 50 ms (GEN-090). | Mesure. |

## 7. Tests

| Test | Type | Contenu |
|---|---|---|
| T-LIVE-01 | Intégration | Chaque bouton / raccourci émet la bonne commande avec l'origine `Utilisateur`. |
| T-LIVE-02 | Intégration | Reprise en main en auto : couche manuelle, rendu au Directeur. |
| T-LIVE-03 | Manuel | Scénario « soirée manuelle » de 30 min au simulateur (check-list jalon 1). |
| T-LIVE-04 | Manuel | Utilisation dans une pièce sombre : lisibilité, erreurs de manipulation. |

## 8. Notes de réalisation (P5)

| Sujet | Réalisation |
|---|---|
| Écran | Module `Luxia.UI.Modules.Live`, premier écran de la navigation. Bandeau d'état en pastilles (sortie, contrôleurs MIDI, figé, fumée, lieu, sûreté, positions non calibrées) ; colonnes de couches (150 px) ; actions à droite ; palettes rapides et journal en bas. Blackout et Grand Master ne sont pas dupliqués : l'en-tête de la fenêtre les montre sur tous les écrans. |
| Écarts assumés (Q32) | Rien n'est affiché pour ce qui n'existe pas encore (tempo, style, show, auto : P7 à P10). Disposition personnalisable (LIVE-006), mini-simulateur (LIVE-007), assistant (LIVE-011) et raccourcis personnalisables (LIVE-041) reportés au chantier d'ergonomie ; `live.json` règle les couches masquées et les boutons. |
| Boutons à maintenir | Appui / relâche sur le pointeur (la perte du pointeur relâche aussi : jamais de flash coincé). |
| Raccourcis (LIVE-040) | B, F, S, Z, G, Échap, 1-9 (couche encadrée), ← →, Page ↑↓ ; hors saisie de texte, répétition automatique ignorée, relâche toujours traitée. Espace, Ctrl+A et N avec leur phase. |
| Journal (LIVE-009, GEN-112) | Événements du bus (scènes avec origine, sûreté, sortie, refus) ; case « Commandes » : journal des commandes du moteur. |
