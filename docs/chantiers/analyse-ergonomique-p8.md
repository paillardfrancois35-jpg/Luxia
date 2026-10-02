# Analyse ergonomique – fin de P8 « Show & séquences »

> Doc 32 §5.6, rédigée le 2026-10-02 après l'essai en discussion test ([essais/P8-resultats.md](../essais/P8-resultats.md),
> v1.010.079 à .091) et les corrections qui ont suivi. Statuts : ✅ fait · ⏳ proposé · ❓ décision de l'utilisateur.
> Le sujet principal (l'éditeur de show) a son propre cahier des charges : [refonte-editeur-show.md](refonte-editeur-show.md).

## 1. Ce que l'essai a montré

| Constat | Nature | Suite |
|---|---|---|
| Colonne « Shows », bandeau, Forcer, visite guidée : « ok » partout | Le jeu et la supervision sont compris | — |
| Éditeur de séquence (frise, glisser-déposer) : « ok » | Modèle « pistes et blocs » compris | — |
| **Éditeur de show : « très très complexe », « un bordel »** | Trop d'information dépliée, transitions rangées dans l'étape de départ, listes longues, vocabulaire technique | ⏳ discussion dédiée ([cahier des charges](refonte-editeur-show.md)) |
| « 100 » lu « 10 » dans un champ numérique | Champs trop étroits pour leurs flèches | ✅ toute l'interface (flèches 24 px, largeurs calculées) |
| Valider enregistre un show en erreur sans rien dire ; ⛔ et ⚠ de même teinte | Un effet important se fait en silence | ✅ confirmation, ligne au Journal, rouge / jaune |
| « Aussitôt » introuvable dans la liste des conditions | Liste qui défile sans qu'on le voie | ✅ liste agrandie ; classement : refonte |
| Après ×2 en écoute, impossible de savoir qu'on a corrigé ni de revenir au tempo de la musique | État invisible, pas de retour arrière | ✅ bouton « entendu » orange quand il diffère, un clic y revient |
| Chemin de la trace tronqué et non copiable | Information à recopier dans un message d'en-tête | ✅ au Journal |
| PAR noirs pendant un strobe, vague invisible | Le modèle de couches (HTP, couleur nécessaire) reste difficile, comme en P7 | ✅ contenu corrigé ; ⏳ E3 ci-dessous |

## 2. Propositions

| # | Gravité | Constat | Proposition | Coût | Suite |
|---|---|---|---|---|---|
| **E1** | **Haute** | Éditeur de show incompréhensible pour un non-spécialiste | Refonte en discussion dédiée : lecture d'abord (une phrase par transition), une seule représentation, détail à la demande, conditions classées, modèles au lieu d'une page vide | grand | ✅ décidé : discussion dédiée après la validation (doc 99) |
| **E2** | Moyenne | « aussitôt » (condition) et « Tout de suite » (quantification) : deux listes, même idée | Vocabulaire unique dans la refonte (ex. condition « sans attendre », quantification « immédiatement ») | petit | ✅ fait dans P8 : condition « sans condition (dès l'entrée dans l'étape) », quantification « Sans attendre le temps » (aussi pour le départ d'une scène) |
| **E3** | Moyenne | Une scène qui ne s'allume pas (strobe sans couleur, vague sous un Plein feu) ne dit pas pourquoi | Avertissement de la validation : « strobe sans couleur sur ces PAR », « intensité masquée par une couche plus haute » (le moteur sait d'où vient chaque valeur, GEN-043) | moyen | ✅ décidé : plus tard (doc 99) |
| **E4** | Basse | Bouton de trace en tête de la colonne Shows | Temporaire : à retirer au lot 7 (après la re-vérification de l'exemple 13) | petit | ✅ retiré au lot 7 |
| **E5** | Basse | Le guide confondait « bloquant » (refusé au lancement) et « refusé à Valider » | Guide corrigé ; même mot à garder dans la refonte : « erreur = ne pourra pas être lancé » | petit | ✅ |
| **E6** | Moyenne | Re-vérification v1.010.104 : « Les combos, on les uniformise toutes » ; liste dépliée sur fond sombre qui « se noie » dans le fond de l'application, illisible | **Style global** des listes déroulantes (`App.axaml`) : fond neutre et contrasté (gris moyen), bordure, barre de défilement visible, hauteur standard ; la liste des conditions revient à la hauteur standard (19 choix avec défilement visible) | petit | ✅ fait dans P8 (style global de `App.axaml`, vérifié à la capture) |
| **E7** | Basse | Sélectionner le texte du Journal est difficile pendant qu'un show joue (le Journal se rafraîchit) | Figer le défilement quand une ligne est sélectionnée | petit | ✅ décidé : plus tard (doc 99) |

| **E8** | Moyenne | Lot 7 (retrait de Scènes et Live, Q47) : l'inventaire montre des fonctions validées absentes de l'écran de jeu (6 pour Scènes, les actions permanentes pour Live) | Retrait **reporté** ; liste à porter au doc 99 ; en préparation : « Couches… » dans l'en-tête de l'écran de jeu, courbe de fondu et bascule des attributs discrets dans les Propriétés de l'étape | moyen | ✅ décidé le 2026-10-02 : garder Scènes et Live pour v1.010 |

## 3. Re-vérification (v1.010.104)

Ex. 8 (strobe), 15 (champs numériques), 16c (erreurs, Valider, conditions), 3d (tempo « entendu »), 13 (Final au silence, retour à
l'Intro) : ✅ tous conformes. Nouveaux retours : E6, E7.

## 4. Réserves pour la validation de P8

- Exemples 5, 6 et 7 non refaits en .085 (✅ en .079, non touchés par les correctifs).
- Fumée non testée au matériel (appareil non branché jusqu'à la finalisation de l'application) : ex. 10, *Montée*, *Explosion*.
