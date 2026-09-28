# Chantier ergonomique – essai de l'écran « Contrôle »

> Branche `ergo/analyse`, version de développement **1.005** (numéro exact annoncé dans la discussion, à lire dans la
> barre de titre). Doc de référence : [60 – Ergonomie](../60-ergonomie.md) §4, §9 (ERG-009 à ERG-020), §11 (choix faits
> par délégation, **à rediscuter après usage**).

## 0. Préparer

1. Fermer LuXia s'il est ouvert (une seule instance à la fois).
2. Fabriquer la démo (copie du show de référence + contenu d'essai ; régénérable à volonté, jamais l'original) :

   ```bash
   python tools/generer-demo-controle.py
   ```

3. Lancer LuXia et ouvrir `samples/Démo Contrôle` (menu **Projet → Ouvrir…**) :

   ```bash
   dotnet run --project src/Luxia.App -- "samples/Démo Contrôle"
   ```

4. Vérifier : le **logo LuXia** dans la fenêtre de démarrage, en haut à gauche de la navigation et dans **Aide → À
   propos** ; l'icône de la barre des tâches (les deux faisceaux croisés) ; l'écran **Contrôle** ouvert en premier.

Ce qu'on voit (doc 60 §6) : en haut le sélecteur **LIVE · ÉDITION · 👁 AVEUGLE** et un **bandeau** qui dit en toutes
lettres où vont les réglages ; au centre les **Colonnes** (une par couche, 8 comme les faders de l'APC) ; à droite les
**Propriétés** ; en bas le **Plan des appareils** et les **Réglages des appareils** (onglet **Journal** à côté). Chaque
panneau a un **?** qui explique à quoi il sert.

## 1. Jouer (mode LIVE, par défaut)

| # | Faire | Attendu |
|---|---|---|
| 1 | Clic sur **Plein feu** (colonne Intensité) | La scène joue : bouton à sa couleur ; le plan s'allume ; le Journal note « ▶ Plein feu ». |
| 2 | Clic sur **Accueil ambre / bleu** (Couleurs) | Les PAR alternent ambre / bleu ; « étape 1 / 2 » puis « 2 / 2 » sous le nom. |
| 3 | Boutons **◀ ▶** de la colonne Couleurs | Grisés tant que la scène qui joue n'a qu'une étape ; avec **Accueil ambre / bleu** ou **Chenillard 4 couleurs**, étape précédente / suivante tout de suite. |
| 4 | **■** de la colonne Intensité (Plein feu), puis master de la colonne Couleurs à 40 % | Les PAR baissent ; le fader 2 de l'APC fait la même chose. Tant que Plein feu joue, rien ne bouge : la plus forte intensité l'emporte (HTP), et Plein feu envoie 100 %. |
| 5 | Maintenir **Flash blanc** (Flashs) | Flash (fixe) tant qu'on appuie. |
| 6 | Colonne **★ Libre** : **Effet multi-têtes seul**, **Barre 1 : arc-en-ciel** | La 8e couche pilote un appareil à part (ton idée), fader 8 de l'APC. |
| 7 | **■** d'une colonne | Arrêt net ; seule une scène qui porte un fondu de sortie s'éteint en fondu (**Fondu lent (4 s)**, colonne Couleurs). |

## 2. Retoucher en direct (LIVE)

| # | Faire | Attendu |
|---|---|---|
| 1 | Sur le plan : clic sur **PAR 1**, puis **Ctrl + clic** sur PAR 2 ; ou glisser un rectangle ; ou **Tous les PAR** | Les appareils sélectionnés sont entourés de bleu ; les Réglages disent « PAR 1, PAR 2 · surcharge LIVE, non enregistrée ». |
| 2 | Onglet **Couleur** : glisser dans le carré | Les PAR changent aussitôt ; pastilles **jaunes** (surcharge LIVE) ; le bandeau compte les appareils surchargés. |
| 3 | Relancer une scène de couleur | La retouche **reste** par-dessus (F2 : intervention voulue). |
| 4 | **Libérer la sélection**, ou **Libérer tout** dans le bandeau, ou **Échap** | Les scènes reprennent la main ; pastilles vides. |

## 3. Corriger une scène (ÉDITION)

| # | Faire | Attendu |
|---|---|---|
| 1 | Clic sur **ÉDITION** sans scène choisie | Refus poli : « Choisissez d'abord la scène à éditer : bande ✎… ». |
| 2 | Bande **✎** d'**Accueil ambre / bleu** | Contour de la scène ; Propriétés : nom, lecture, **bande d'étapes** « 1 · Ambre », « 2 · Bleu ». |
| 3 | **ÉDITION**, puis clic sur l'étape **2 · Bleu** | Bandeau **vert** : « écrit dans « Accueil ambre / bleu », étape 2 » ; l'étape est **montrée sur la sortie**. |
| 4 | Sélectionner les 4 PAR, onglet Couleur, choisir un violet | Écrit dans l'étape (pastilles **vertes**) ; le Journal note « ✎ enregistré : Couleur » une demi-seconde après. |
| 5 | **Ctrl + Z**, puis **Ctrl + Y** | Annulé, rétabli (le Journal le dit) ; les boutons ↶ ↷ disent quoi en infobulle. |
| 6 | Propriétés : maintien de l'étape à 3 s, **+ étape**, **Dupliquer**, **◀ ▶**, **Supprimer** | Tout s'enregistre à la saisie, sans bouton « Enregistrer » ; Ctrl + Z annule. |
| 7 | Clic droit sur une scène : Renommer, Dupliquer, Couleur, Couche, Masquer du Live, Supprimer | Mêmes verbes partout ; une suppression demande confirmation et s'annule par Ctrl + Z. |
| 8 | Bouton **Revenir en LIVE** du bandeau | L'étape n'est plus montrée ; les scènes reprennent. |

## 4. Préparer sans rien montrer (AVEUGLE)

| # | Faire | Attendu |
|---|---|---|
| 0 | En LIVE, mettre en condition : **Plein feu**, **Bleu sur tout le parc**, **Lyres : piste centre** | Tout est allumé en bleu, les lyres visent le centre : c'est ce que le public voit, et qui ne doit plus bouger. |
| 1 | ✎ sur **Balayage doux des lyres**, puis **👁 AVEUGLE** | Bandeau **bleu clair** ; le plan affiche « APERÇU 👁 — la sortie ne change pas ». |
| 2 | Sélectionner Lyre 1 et Lyre 2, onglet **Position**, cliquer dans la grille | Les deux points se déplacent **en gardant leur écart** ; sur le plan (aperçu) les lyres bougent ; **les vraies lyres ne bougent pas**. |
| 3 | Revenir en LIVE, lancer la scène | Les lyres jouent la position réglée. |

## 5. Zones de sécurité des lyres

| # | Faire | Attendu |
|---|---|---|
| 1 | Sélectionner **Lyre 1**, onglet Position, **Éditer les zones du lieu** | Bandeau **ZONES** (orange) : les zones valent pour toutes les scènes. La démo a déjà « Public » (interdite, rouge) et « Limites » (**permise**, pointillés verts, extérieur assombri). |
| 2 | Choisir **interdite** ou **permise**, glisser dans le vide | Nouvelle zone ; poignées pour l'ajuster, Suppr pour la retirer. |
| 3 | Terminer les zones ; lancer **Piège : lyre 1 vers le public** | La lyre s'arrête au bord de la zone interdite ; elle ne sort jamais de la zone permise. |

## 6. Disposer les panneaux

| # | Faire | Attendu |
|---|---|---|
| 1 | Glisser l'onglet **Journal** ailleurs, détacher **Plan des appareils** dans une fenêtre (menu ▾ du panneau → **Détacher dans une fenêtre**), replier un panneau (épingle) | Menus **en français** ; la fenêtre détachée va sur le deuxième écran. |
| 2 | Fermer un panneau (✕), puis **Panneaux ▾** | « (fermé) » ; le choisir le remet à sa place. |
| 2 bis | Détacher un panneau, double-clic sur sa barre de titre ; puis **Panneaux ▾** → « (détaché : le remettre en place) » (ou le fermer puis le rappeler) | Le double-clic agrandit / restaure la fenêtre ; le panneau revient à sa place dans la fenêtre principale et la fenêtre vide se ferme. |
| 3 | Quitter LuXia, relancer | Même disposition (enregistrée sur ce poste). **Rétablir la disposition** revient à celle livrée. |

## 7. Looks, disposition Spectacle, verrou, taille

| # | Faire | Attendu |
|---|---|---|
| 1 | Panneau **Looks** (onglet sous Propriétés) : la démo en a trois, **F1** « Temps mort », **F2** « Retour de piste », **F3** « Ambiance UV » | Clic (ou touche F1…) : tout s'arrête et le look rejoue ses scènes et ses masters ; le Journal note « ✦ look ». Survol : la liste des actions. |
| 2 | Lancer deux ou trois scènes, régler un master, **+ Capturer ce qui joue**, nommer | Nouveau look qui refait cet état ; clic droit : Mettre à jour, Renommer, Couleur, Supprimer. |
| 3 | En haut : **Spectacle** | Colonnes en grand, panneau **Pilote automatique** (place réservée pour P10) avec les looks en gros boutons, Journal. **Contrôle** : on retrouve sa disposition. |
| 4 | **🔒 Verrou soirée** | Retour en LIVE ; ÉDITION, AVEUGLE, ✎, « + scène », propriétés, zones, Ctrl + Z : refusés avec la raison ; jouer, retoucher en direct et les looks marchent. Re-clic : déverrouillé. |
| 5 | Menu **Affichage → Taille de l'interface : 125 %** (ou 150 %) | Tout grossit ; gardé au prochain lancement. |

## 8. Ce qui reste comme avant

Les écrans **Live** et **Scènes** sont toujours là (choix C5) : on pourra comparer. Ils seront retirés quand le
Contrôle aura fait ses preuves.

## 9. Captures de référence

`dotnet run --project tools/Luxia.Tools.Captures -- "samples/Show de référence" <dossier> 1920 1080` écrit aussi
« Contrôle - LIVE surcharge », « Contrôle - ÉDITION », « Contrôle - AVEUGLE lyres », « Contrôle - zones » (copies dans
[docs/maquettes/captures](../maquettes/captures/)).
