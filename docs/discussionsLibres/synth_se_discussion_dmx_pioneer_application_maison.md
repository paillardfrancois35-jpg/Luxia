# Synthèse : Conception d'une application de commande DMX automatique

## 1. Fonctionnement du système Pioneer DJ (rekordbox Lighting & RB-DMX1)

Le système Pioneer DJ repose sur un principe hybride qui va bien au-delà de l'écoute audio en direct :

* **Analyse préalable (Hors-ligne) :** Lors de l'importation de la musique, rekordbox effectue un découpage structurel (*Phrasing*) pour identifier les sections (*Intro, Couplet, Refrain/Drop, Break, Outro*) et générer une grille temporelle (*Beat Grid*).
* **Restitution synchronisée (En direct) :** Lors du mix, le logiciel envoie les instructions DMX pré-calculées en parfaite synchronisation avec la lecture. Les boucles, sauts de piste ou variations de pitch sont répercutés instantanément sur la lumière sans latence d'analyse.

---

## 2. Idées clés & Architecture pour une application maison

### A. Analyse & Traitement du signal Audio
* **Réseau de grilles (BPM & Beat) :** Synchroniser les événements majeurs sur les temps (ex: changement de scène sur le temps 1, strobes sur les temps secondaires).
* **Découpage spectral (FFT à 3 bandes) :**
  * *Basses (< 150 Hz) :* Impulsions de dimmer principal ou mouvements de lyres.
  * *Médiums (150 Hz – 4 kHz) :* Changements de couleurs et mouvements fluides.
  * *Aigus (> 4 kHz) :* Déclenchement des flashs, strobes ou effets courts.
* **Détection du niveau d'énergie (Phrasing) :** Différencier les états du morceau (*Break, Build-up, Drop*) pour adapter l'intensité globale des jeux de lumières.

### B. Abstraction & Architecture logicielle
* **Couche virtuelle :** Séparer la logique métier (ex: `Intensité = 80%`, `Couleur = Rouge`, `Position = Centre`) du matériel physique.
* **Profils de projecteurs (Fixtures) :** Fichiers de configuration traduisant les attributs virtuels vers les canaux DMX spécifiques de chaque projecteur.
* **Groupement de machines :** Définir des rôles (*Wash, Beam, Pars de fond*) pour leur affecter des comportements dédiés.

### C. Mouvements et Harmonie visuelle
* **Générateurs d'effets (LFO) :** Utiliser des fonctions mathématiques (sinus, triangle, dent de scie) couplées au BPM pour piloter de manière fluide les axes Pan/Tilt.
* **Palettes de couleurs restreintes :** Limiter les générateurs aléatoires à des harmonies de couleurs prédéfinies (complémentaires, analogues, bi-tons) pour éviter le rendu brouillon du spectre RVB complet.

### D. Contrôle manuel & Sécurité (Overrides)
* **Boutons prioritaires :** Intégrer des fonctions d'urgence ou d'impact direct (Blackout, Strobe / Flash général).
* **Mode Freeze :** Permettre de figer une ambiance visuelle réussie pendant un temps fort.

---

## 3. Stratégies d'intégration pour le direct

1. **Audio Temps Réel + Tampon :** Conserver un court historique audio en mémoire pour anticiper les baisses de rythme (*breaks*).
2. **Pré-analyse de fichiers :** Utiliser des bibliothèques d'analyse audio (ex: *librosa*, *aubio*) pour générer des cartes d'énergie avant la diffusion.
3. **Protocole de synchronisation (OS2L / Ableton Link) :** Interconnecter l'application à un logiciel DJ tiers pour récupérer directement le BPM, les temps et la structure sans dépendre uniquement d'un micro ou d'une entrée ligne.