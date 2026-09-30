# Démonstration « Contrôle 2 » – écran de jeu, fenêtre d'édition, groupes et dimmers

> Guide d'essai du chantier « Contrôle 2 » (exigences ERG-032 à ERG-039, doc 60 §4.9, questions Q37 et Q38). Durée :
> 1 h à 1 h 30. Matériel : PC seul pour les exemples 1 à 9 et 11 (le plan de l'écran et le simulateur suffisent) ; Arduino,
> appareils et **les deux APC mini** pour l'exemple 10. Branche Git : `ergo/controle-2`. Version à vérifier dans la barre de
> titre : **v1.007.NNN** (annoncée par la discussion de développement).
> Procédure : [doc 33](../33-procedure-essais.md) ; les résultats sont notés dans
> [essais/ERG2-resultats.md](../essais/ERG2-resultats.md).

## Ce que livre le chantier

| Élément | Où |
|---|---|
| **Écran de jeu** : plus de sélecteur de modes ; colonnes en grand, Groupes dimmer, Looks, Pilote, Journal, Stop / Tout stopper, Verrou soirée | Écran **Contrôle** (premier de la liste) |
| **Fenêtre d'édition d'une scène** : un **brouillon** avec Appliquer / Annuler / Valider et la case Aveugle ; non bloquante, déplaçable sur un second écran | Bande **✎** d'un bouton de scène |
| **Groupes d'appareils** en arbre, un appareil dans un seul groupe, groupe implicite « Non assigné » | Installation → onglet **Gestion des dimmers** ; fichier `groupes.json` |
| **Dimmers de groupe** : un fader par groupe, la lumière est multipliée le long de l'arbre | Contrôle → panneau **Groupes dimmer** |
| **Niveau de couche** (le fader de couche s'appelle maintenant « Niveau ») | Colonnes |
| **Seconde platine MIDI** pour les dimmers | APC mini n° 2 |

Ce qui a **disparu** de l'écran Contrôle : le sélecteur LIVE / ÉDITION / AVEUGLE, la disposition « Spectacle », les panneaux
Propriétés, Plan, Réglages et Effets (ils sont dans la fenêtre d'édition).

## Préparer

1. **Fermez LuXia** avant de compiler ou de lancer une autre version (une seule instance à la fois).
2. Régénérez la copie de travail du show (jamais l'original), dans PowerShell :

```powershell
Remove-Item -Recurse -Force "D:\Develop\Claude\CSharp\DMX\samples\Show de travail"
```

```powershell
Copy-Item -Recurse "D:\Develop\Claude\CSharp\DMX\samples\Show de référence" "D:\Develop\Claude\CSharp\DMX\samples\Show de travail"
```

3. Dans `samples\Show de travail\projet.json`, renommez `"name"` en « Show de travail ». Lancez LuXia, **Projet → Ouvrir…** →
   `samples\Show de travail`.
4. **Projet → Problèmes du projet…** : un seul avertissement attendu (« Lyres sur 3 positions », connu depuis P5).

> Les anciennes dispositions de panneaux (`controle.json`, `spectacle.json`) ne servent plus : l'écran de jeu a sa propre
> disposition (`jeu.json`, dans `%AppData%\LuXia\dispositions`).

---

## Exemple 1 – L'écran de jeu (ERG-032, ERG-035, ERG-039)

1. Ouvrez l'écran **Contrôle**.
2. **Observez** : aucun sélecteur de modes en haut. À gauche les **Colonnes** (une par couche), à droite **Groupes dimmer** (vide,
   avec une explication) et **Looks**, en bas **Pilote automatique** et **Journal**.
3. En haut : **■ Stop**, **■ Tout stopper**, **🔒 Verrou soirée**, **↶ ↷**, **Panneaux ▾**, **Rétablir la disposition**.
4. **Colonnes** : sous le nom de chaque couche, le curseur s'appelle maintenant **Niveau** (il n'était pas « Master »). Passez la
   souris dessus : l'infobulle dit qu'il multiplie les intensités de la couche.
5. Vérifiez les **tailles** : boutons de scène hauts et faciles à cliquer, bande **✎** large, boutons ◀ ▶ ■ nets. Cliquez
   **☰ Resserré** : les boutons se réduisent (choix de densité) ; recliquez pour revenir.
6. Cliquez le « **?** » de chaque panneau : une aide de trois lignes s'affiche.
7. **Panneaux ▾** : fermez **Looks** (croix du panneau), puis rouvrez-le par le menu. **Rétablir la disposition** remet tout comme livré.
8. Faites glisser la barre de titre d'un panneau hors de la fenêtre (deuxième écran si vous en avez un) : il se détache ;
   « Panneaux ▾ → (détaché : le remettre en place) » le ramène.
9. **Ce que ça illustre** : un seul lieu pour jouer, sans mode à garder en tête.

## Exemple 2 – Créer l'arbre des groupes (ERG-036)

1. **Installation** → onglet **Gestion des dimmers**. **Observez** : un seul groupe, « **Non assigné** », qui contient tous les appareils.
2. **+ Groupe** → nom « **Parc lumineux** ». Il apparaît dans l'arbre, sélectionné.
3. Avec « Parc lumineux » sélectionné, **+ Sous-groupe** → « **Face (PAR)** ». Sélectionnez « Face (PAR) » puis **+ Sous-groupe** → « **PAR scène** ».
4. Sélectionnez « PAR scène ». Dans la colonne de droite (« Ajouter au groupe choisi », qui ne propose que les appareils **non assignés**), cliquez **← Ajouter** en face de **PAR 1**, **PAR 2**, **PAR 3**, **PAR 4**.
   **Observez** : ils quittent « Non assigné » (l'arbre le montre) et apparaissent dans « Appareils du groupe ».
5. Créez de la même façon, à la racine ou sous « Parc lumineux » :
   « **Barres** » (Barre 1, Barre 2) sous « Parc lumineux » ; « **Lyres** » (Lyre 1, Lyre 2) sous « Parc lumineux » ; « **UV** » (UV 1, UV 2) à la racine.
6. **Un appareil dans un seul groupe** : sélectionnez « Barres », cochez **Tous voir** (la liste montre alors aussi les appareils déjà rangés), **← Ajouter** « PAR 1 » : une **confirmation** demande si vous déplacez l'appareil ; validez : il quitte « PAR scène » (ce groupe n'a plus que PAR 2, 3, 4). Remettez PAR 1 dans « PAR scène » de la même façon, puis décochez **Tous voir**.
7. Sélectionnez « Lyres » : dans le détail, changez **Groupe parent** pour « (racine) » puis remettez « Parc lumineux ». Essayez de mettre « Parc lumineux » sous « Face (PAR) » : la liste ne le propose pas (un groupe ne peut pas être sous son propre sous-groupe).
8. Sélectionnez « Non assigné » : le champ **Nom** devient « Divers », **Renommer**. Le groupe implicite est renommé (il ne se supprime pas). Remettez « Non assigné ».
9. **▲ ▼** : montez « UV » avant « Parc lumineux » puis remettez-le après.
10. **Ce groupe a un dimmer** : cochée pour tous les groupes sauf « Lyres » (décochez-la : le repère « ② n » de la ligne disparaît et les numéros de fader se décalent).
    Recochez-la ensuite.
11. **Supprimer…** sur « Face (PAR) » : une confirmation s'affiche ; validez. **Observez** : « PAR scène » remonte sous « Parc lumineux ». Recréez
    ensuite « Face (PAR) » sous « Parc lumineux » (**+ Sous-groupe**) et remettez « PAR scène » dedans avec son champ **Groupe parent**. Le groupe recréé se place après ses frères : montez-le avec **▲** pour retrouver l'ordre de l'arbre final.
12. Arbre final attendu : Parc lumineux [Face (PAR) [PAR scène : PAR 1 à 4], Barres : Barre 1, 2 ; Lyres : Lyre 1, 2] ; UV : UV 1, 2 ; Non assigné : Gros PAR 1, 2, Effet multi-têtes, Fumée.
13. Les **barres « niveau actuel »** de chaque ligne (lecture seule) sont à 100 % ; le repère **② n** (n° du fader de la seconde platine MIDI) est expliqué par la légende « ② n : fader de la 2e platine MIDI » et son infobulle. Le bloc « **Flux d'intensité** » en bas du détail les résume.
14. **Projet → Problèmes du projet…** : aucun avertissement sur `groupes.json`.

## Exemple 3 – Les dimmers de groupe à l'écran (ERG-037)

1. Écran **Contrôle**. **Observez** : le panneau **Groupes dimmer** montre un fader par groupe qui a un dimmer, avec son nom, « = 100 % » et un repère « ② 1 … ».
2. Dans les colonnes, lancez **Plein feu** (couche Intensité) et **Blanc chaud sur les 4 PAR** (couche Couleurs). Les PAR réels sont allumés. Pour **voir** l'effet à l'écran, ouvrez l'écran **Simulateur**, ou la **fenêtre d'édition** (✎, sans rien modifier) dont le plan montre les appareils ; l'écran de jeu n'a plus de plan.
3. Baissez le fader **Face (PAR)** à **50 %** : les PAR baissent de moitié (la couleur reste la même, seule l'intensité change). **Observez** : le texte sous « PAR scène » passe à « = 50 % » ; un bandeau **jaune** apparaît : « Retouches en direct (temporaires, rien n'est enregistré) : Face (PAR) — dimmer 50 % ».
4. Baissez maintenant **Parc lumineux** à **80 %** : les PAR sont à 40 % (80 % × 50 %) ; les lyres, barres restent à 80 % ; **l'UV n'a pas changé** (autre branche). Vérifiez les textes « = … % » sous chaque fader.
5. Baissez **UV** à 40 % pendant que la scène **Scintillement UV** (couche Ambiance, si présente) ou **UV plein** joue : l'UV baisse, la forme du scintillement est conservée (plus doux, pas plat).
6. **Grand Master** (en haut de la fenêtre) à 50 % : tout baisse encore de moitié (il reste à part) ; remettez-le à 100 %.
7. **Double-clic** sur un fader (ou sur le **Grand Master**) : il revient à 100 %. **Tout à 100 %** (en haut du panneau) ou **Échap** ou **Libérer tout** (bandeau) remettent tout à 100 % ; le bandeau disparaît.
8. **Blackout** : tout devient noir ; retirez-le, les dimmers ont gardé leur niveau.
9. **Ce que ça illustre** : chaque étage multiplie ; une retouche en direct n'est jamais enregistrée (**Projet → Ouvrir…** le même projet : tout est à 100 %). Le bandeau « Retouches en direct » occupe une place **fixe** en haut : rien ne saute quand il s'allume.

## Exemple 4 – Le niveau d'une couche (ERG-039)

1. Lancez **Rouge – couleur seule** et **Plein feu**. Baissez le **Niveau** de la couche **Intensité** à 50 % : la lumière baisse de moitié (la couleur reste).
2. Vérifiez qu'aucun réglage de couche ne dépend de la scène : changer de scène dans la couche ne modifie pas le **Niveau**.
3. Remettez le niveau à 100 %.

## Exemple 5 – La fenêtre d'édition : ouvrir, modifier, annuler (ERG-033)

1. Colonne **Couleurs**, bouton **Chenillard 4 couleurs** : cliquez la **bande ✎** à droite du bouton.
2. **Observez** : une **fenêtre « LuXia – Édition de « Chenillard 4 couleurs » (brouillon) »** s'ouvre, avec un bandeau **BROUILLON** vert. Le bouton de la scène est **entouré de vert** dans l'écran de jeu. Trois zones : **Plan des appareils**, **Réglages des appareils | Effets**, **Propriétés et étapes**. En bas : **Aveugle**, **Appliquer**, **Annuler**, **Valider**.
3. Dans **Propriétés et étapes**, choisissez l'**étape 2** ; sur le **Plan**, sélectionnez **PAR 1 à 4** (glissez un rectangle ou Ctrl + clic ; le bouton « Tous les PAR » prend aussi les gros PAR) ; dans la zone du centre, onglet **Réglages des appareils**, sous-onglet **Couleur**, choisissez une autre couleur (violet par exemple). **Observez** : les PAR prennent la couleur sur la sortie (plan, simulateur, PAR réels) ; le bandeau affiche « **modifié** ».
4. Sans fermer la fenêtre, revenez à l'écran de jeu : la scène n'a **pas** changé dans les colonnes (c'est un brouillon). Clic droit sur le bouton du chenillard → **Renommer** : un message dit que la scène est ouverte dans la fenêtre d'édition (refus).
5. Cliquez **la croix** de la fenêtre : une **confirmation** apparaît (abandonner les modifications ? Oui : la scène revient à l'original et la fenêtre se ferme ; Non : elle reste ouverte). Répondez **Non**.
6. **Ctrl+Z** dans la fenêtre : la couleur revient. **Ctrl+Y** la remet.
7. Cliquez **Annuler** : la fenêtre se ferme **sans question**. Le contour vert disparaît. Lancez le chenillard : l'étape 2 est toujours l'originale.

## Exemple 6 – Appliquer et Valider (ERG-034)

1. Rouvrez le chenillard (✎). Étape 2, PAR 1 à 4, couleur violette, comme à l'exemple 5.
2. Cliquez **Appliquer** : la fenêtre **reste ouverte**, le bandeau « modifié » disparaît ; lancez le chenillard dans les colonnes : la scène joue **avec** l'étape 2 violette (elle est écrite) et le brouillon ne recouvre plus la scène lancée (la sortie suit la scène jouée).
3. Changez encore une couleur puis **Annuler** : la fenêtre se ferme et la scène revient à **l'état appliqué** (violet), pas à l'original.
4. Rouvrez, modifiez, cliquez **Valider** : la fenêtre se ferme, la scène est écrite.
5. Remettez l'étape 2 en couleur d'origine par le même chemin (Valider).
6. Ouvrez une scène puis, sans rien modifier, cliquez la croix : la fenêtre se ferme.
7. Ouvrez « Chenillard », modifiez, puis cliquez ✎ sur **Plein feu** : refus (message), la fenêtre du chenillard passe au premier plan. Annulez.

## Exemple 7 – Aveugle (ERG-034)

1. Lancez **Plein feu** (la sortie est allumée). Ouvrez **Lyres sur 3 positions** avec ✎.
2. Cochez **Aveugle**. **Observez** : le bandeau devient **bleu clair**, le texte du pied dit « Aperçu au plan seulement : la sortie sur scène ne change pas », et le plan porte un bandeau « APERÇU ».
3. Sur le **Plan**, sélectionnez les lyres ; **Réglages → Position**, choisissez une position. **Le plan montre l'aperçu** (bandeau « APERÇU … la sortie ne change pas »), **mais les lyres réelles ne bougent pas** et le simulateur ne bouge pas.
4. Décochez **Aveugle** : les lyres réelles vont vers la nouvelle position.
5. **Annuler** : tout revient.

## Exemple 8 – Effets et « ▶ Lancer » dans la fenêtre (ERG-029, ERG-031)

1. Ouvrez **Plein feu** (✎). Plan : « Tous les PAR ». Onglet **Effets**, choisissez « Vague douce » → **+ Ajouter à la sélection**.
2. L'effet joue sur la sortie (hors Aveugle). Réglez une molette : le changement se voit environ 0,5 s plus tard.
3. Cliquez **▶ Lancer** (Propriétés) : la scène se joue **avec le brouillon**, sans changer de mode. Un réglage (molette, couleur) reprend la main du brouillon.
4. **■ Arrêter** l'arrête. **Annuler** : l'effet disparaît de la scène.

## Exemple 9 – Le verrou soirée (ERG-021)

1. Cliquez **🔒 Verrou soirée** : le bouton devient **bleu**. **Observez** : les bandes **✎** et les boutons **+ scène** ont **disparu** (on ne fait que jouer). Jouer, ■ Stop, les dimmers et les niveaux de couche fonctionnent toujours.
2. Levez le verrou (le bouton redevient sombre). Ouvrez une scène (✎), puis essayez de poser le verrou : refus (« Fermez d'abord la fenêtre d'édition ») et le bouton **reste sombre**. **Annuler**, puis posez le verrou : OK. Levez-le.

## Exemple 10 – Seconde platine MIDI (ERG-038) – **avec le matériel**

1. Branchez **les deux** APC mini (MK1 et MK2). Le projet a maintenant des dimmers de groupe.
2. Ouvrez l'écran **Live** : le bandeau MIDI liste les platines et indique laquelle sert aux dimmers (« … – dimmers de groupe »). Par défaut c'est la **deuxième dans l'ordre alphabétique des ports** (« APC MINI » puis « APC mini mk2 » : le **MK2** sert donc aux dimmers).
3. **Platine des couches** (l'autre) : les pads lancent les scènes, les faders 1 à 8 = niveaux de couches, le fader 9 = Grand Master : **comme avant**.
4. **Platine des dimmers** : faders **1 à 8** = les dimmers dans l'ordre de l'arbre (Parc lumineux, Face, PAR scène, Barres, Lyres, UV…) ; les **pads sont éteints**. Un fader ne prend la main qu'une fois croisé le niveau actuel (reprise douce). Les repères « ② n » du panneau correspondent aux faders.
5. **Boutons ronds du bas 1 à 8** (les boutons ronds au-dessus des faders) : remettent le dimmer à 100 % ; leur LED reste **allumée tant que le dimmer est retouché**. Après la remise à 100 %, le fader physique (resté plus bas) **ne reprend la main qu'en recroisant 100 %** : pas de saut.
6. **Maj** (le bouton **Shift** de la platine, en bas de la colonne de droite) **+ bouton rond du bas 4** (puis 3) : page suivante / précédente si plus de 8 dimmers (à tester en créant des groupes supplémentaires).
7. Inverser les rôles : le fichier `midi.json` n'existe pas dans un projet neuf : **créez-le** dans `samples\Show de travail` avec ce contenu (PowerShell), puis rouvrez le projet :

```powershell
Set-Content -Path "D:\Develop\Claude\CSharp\DMX\samples\Show de travail\midi.json" -Value '{ "formatVersion": 1, "dimmerController": "MK1" }'
```
8. **Ce que ça illustre** : une platine pour jouer, l'autre pour les réglages fins.

## Exemple 11 – Reprise, fermeture, fiabilité

1. Ouvrez une scène (✎), modifiez-la, puis **fermez LuXia** (fenêtre principale) avec la fenêtre d'édition ouverte : LuXia se ferme **sans blocage** ; au redémarrage, la scène est **inchangée** (le brouillon est abandonné, rien n'a été écrit).
2. Rouvrez : `groupes.json` est conservé (l'arbre est là), les dimmers sont à 100 % (rien n'est enregistré).
3. Ouvrez une scène, puis **Projet → Ouvrir…** un autre projet : la fenêtre d'édition se ferme d'elle-même.
4. Dans le simulateur ou sur le plan, vérifiez qu'aucune scène ne joue de façon inattendue après ces essais.

## Exemple 12 – Écran de portable (ERG-035)

1. Réduisez la fenêtre de LuXia à 1366 × 768 (ou testez sur un portable) : l'écran de jeu reste utilisable (colonnes lisibles avec défilement, dimmers accessibles, aucune cible minuscule).
2. Ouvrez une scène : la fenêtre d'édition reste utilisable (Appliquer / Annuler / Valider visibles).
3. **Affichage → taille 125 %** : les deux écrans suivent.

---

## À noter à l'essai

- Pour chaque exemple : ✅ / ❌ / 💡, ce que vous voyez, ce que vous attendiez.
- Tout ce qui vous semble « trop petit » ou « pas clair » : dites-le, même si cela fonctionne.
- Les écrans **Live** et **Scènes** existent encore (ils seront retirés après cet essai, choix C5).
