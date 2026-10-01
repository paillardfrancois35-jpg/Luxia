# 33 – Procédure d'essai : une discussion « dev », une discussion « test »

> Décision de l'utilisateur, 2026-09-29 (essai P6) : pour économiser le contexte, chaque grosse phase se mène avec **deux
> discussions** ouvertes en parallèle. Ce document fait foi pour leur partage des rôles.

## 1. Rôles

| | Discussion **dev** | Discussion **test** |
|---|---|---|
| But | Développer, corriger, documenter, livrer | Accompagner l'utilisateur dans le guide d'essai, pas à pas |
| Code, compilation, Git | Oui (seule à compiler, commiter, pousser, étiqueter, fusionner) | **Jamais** : aucune modification de code, de données, de doc ; pas de `dotnet build`, pas de commit |
| Fiches d'exigences, matrice, passation | Oui (à partir du fichier de résultats) | Non |
| Fichier écrit | Tout | **Un seul** : le fichier de résultats de la phase (§3) |
| Lecture | Tout | Tout (guide, docs, code pour comprendre un comportement) |
| LuXia | Peut le fermer pour compiler ; annonce la version à vérifier | Demande à l'utilisateur ce qu'il voit ; ne lance ni ne ferme LuXia |

**Modèle** (décision de l'utilisateur, 2026-09-29, chantier « Contrôle 2 ») : la discussion test peut tourner avec un
modèle plus léger (Sonnet 5) : elle ne fait que suivre le guide et noter des résultats. La discussion dev garde le modèle
le plus solide pour les lots d'architecture. À la fin du développement, la discussion dev **rédige** le guide d'essai
(`docs/demos/…`) et le fichier de résultats amorcé (`docs/essais/…-resultats.md`), puis donne le message du §4 rempli.

## 2. Déroulement

1. La discussion **dev** livre la phase : guide `docs/demos/Pn-*.md`, version à vérifier (lue dans la version du produit de
   `src/Luxia.App/bin/Debug/net10.0/LuXia.dll`, doc 03 §11), fichier de résultats amorcé (§3).
2. L'utilisateur ouvre la discussion **test** avec le message du §4.
3. La discussion **test** déroule le guide **un exemple à la fois**, en français, avec des consignes cliquables (onglet,
   bouton, libellé exact) ; à chaque exemple, elle **note le résultat** dans le fichier (§3) et passe au suivant. Face à
   une anomalie, elle aide à la décrire précisément (ce qui est fait, ce qui est vu, ce qui était attendu, capture), cherche
   dans le code ou la doc une explication **sans rien corriger**, la note, et propose de continuer si c'est possible.
4. Quand un correctif est nécessaire, l'utilisateur dit à la discussion **dev** : « lis `docs/essais/Pn-resultats.md` ». La
   discussion dev corrige, met à jour les fiches (entrées « Utilisateur | Test », « Validation »), compile, commite, et
   indique **la nouvelle version à vérifier** ; la discussion test la note et reprend l'exemple concerné.
5. **Glossaire** (toutes les discussions, dev comme test) : dès qu'une discussion explique un terme technique ou musical à
   l'utilisateur (break, drop, kick, latence…), elle lui **propose de l'ajouter au [glossaire](glossaire.md)** avec une définition
   courte ; elle l'ajoute s'il accepte (discussion dev), ou la note dans le fichier de résultats, ligne « Glossaire »
   (discussion test), pour que la discussion dev l'intègre.
6. En fin de guide, la discussion test fait le bilan dans le fichier ; la discussion dev propose l'analyse ergonomique de
   fin de phase (doc 32 §5.6), puis fusion et étiquette après validation.

## 3. Fichier de résultats

`docs/essais/Pn-resultats.md` (par exemple `docs/essais/P6-resultats.md`), amorcé par la discussion dev. Un tableau par
exemple du guide, **en ajout seul** (on ne réécrit pas une ligne, on en ajoute une) :

| Date | Version | Exemple | Résultat | Observation de l'utilisateur | Demande / anomalie |
|---|---|---|---|---|---|

Résultat : ✅ conforme · ❌ anomalie (correctif demandé à la discussion dev) · 💡 idée d'amélioration · ⏸ en attente
(matériel absent…). La discussion test cite les mots de l'utilisateur quand ils précisent une attente.

## 4. Message d'ouverture de la discussion test (modèle)

> Nous reprenons le projet LuXia (dépôt `D:\Develop\Claude\CSharp\DMX`), en **discussion test** : lis
> `docs/33-procedure-essais.md` et respecte-le strictement (aucun correctif, aucune compilation, aucun commit ; seul fichier
> que tu peux écrire : `docs/essais/Pn-resultats.md`). Lis ensuite ce fichier de résultats et le guide
> `docs/demos/Pn-*.md`, puis accompagne-moi pas à pas à partir de l'exemple N. Version à vérifier dans la barre de titre :
> **v1.00N.NNN**. Réponds toujours en français ; mon terminal est PowerShell.
