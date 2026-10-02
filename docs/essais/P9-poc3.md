# P9 – Résultats du PoC-3 (lecture en cours selon les lecteurs)

> Fichier de résultats amorcé le 2026-10-02 (P9, lot 1), en ajout seul. Guide : [demos/P9-poc3-sonde.md](../demos/P9-poc3-sonde.md).
> Résultat : ✅ conforme · ❌ anomalie · 💡 idée · ⏸ en attente. Ces observations alimentent la normalisation (doc 21 §3.1,
> lot 2) et la faisabilité de la timeline (doc 23).

| Date | Version | Lecteur / exemple | Résultat | Observation de l'utilisateur | Demande / anomalie |
|---|---|---|---|---|---|
| 2026-10-02 | 1.011 (lot 1) | Poste de développement, aucun lecteur | ✅ | Sonde lancée 7 s sans lecture : « (aucune session média) », aucune erreur (MUS-006). | — |
| 2026-10-02 | 1.011 (lot 1) | Deezer (application Windows) | ✅ | « Deezer ok ». Session `Deezer.62021768415AF_q7m17pa7q8kj0` ; titre et artistes exacts (`Spider Dance` — `Holder, GameChops`), album vide, durée 3:00, position relevée à 0,3 s (précise), pause et reprise détectées. | Nom d'application à raccourcir (corrigé : « Deezer »). Deezer ferme sa session peu après une pause. |
| 2026-10-02 | 1.011 (lot 1) | YouTube Music (Chrome) | ✅ | « YouTube Music ok ». Titre = titre de la vidéo avec l'artiste après une barre (`Tous les cris les S.O.S. (Kokwak Hardstyle Remix) \| Daniel Balavoine`), « artiste » = nom de la chaîne (`Kokwak`), album vide. Position relevée 54 s plus tôt pendant la pause, vitesse 0. | Normalisation (lot 2) : l'artiste est dans le titre, après « \| » ; la chaîne n'est pas l'artiste. |
| 2026-10-02 | 1.011 (lot 1) | Plusieurs lecteurs (MUS-001) | ❌ | Deezer en pause puis fermant sa session : la sonde a annoncé « MORCEAU » pour Chrome, en pause depuis avant (faux changement de morceau). | Corrigé en 1.011 : une session jamais vue en lecture ne remplace pas la session suivie ; test ajouté. |
| 2026-10-02 | 1.011 (lot 1) | VLC 3.0.24 (fichier `Gasolina.mp3`) | ❌ | « VLC NOK ». Aucune session média : VLC 3.0 ne s'annonce pas à Windows (le titre n'existe que dans la barre de titre de la fenêtre). Interface web de VLC non activée (port 8080 fermé). | Décision à prendre : source VLC par l'interface web de VLC (voir proposition dans la discussion). |
