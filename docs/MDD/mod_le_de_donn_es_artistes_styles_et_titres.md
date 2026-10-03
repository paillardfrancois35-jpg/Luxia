# Spécification du Modèle de Données

Ce document présente la structure détaillée du modèle de données pour la gestion des artistes, de leurs alias, de leurs styles musicaux et de leurs titres, incluant le dictionnaire des données, les règles de gestion d'intégrité référentielle et le code SQL DDL.

---

## 1. Dictionnaire des Données

### Table : `ARTISTE`

Représente l'entité principale pour la gestion des artistes.

| Colonne | Type | Modificateurs | Description |
| :--- | :--- | :--- | :--- |
| **`CODE`** | `VARCHAR(50)` | `PRIMARY KEY` | Code unique identifiant l'artiste. |
| **`NOM`** | `VARCHAR(100)` | `NOT NULL` | Nom principal de l'artiste. |
| **`STYLE_CODE`** | `VARCHAR(50)` | `FOREIGN KEY` | Référence le style principal de l'artiste (`STYLE.CODE`). |
| **`STYLE_POIDS`** | `INTEGER` | `CHECK (0 TO 1)` | Pondération du style (valeur entre 0 et 1). |
| **`Commentaire`** | `TEXT` | `NULL` | Notes ou informations complémentaires. |

---

### Table : `ALIAS`

Contient les alias ou noms de scène secondaires associés à un artiste.

| Colonne | Type | Modificateurs | Description |
| :--- | :--- | :--- | :--- |
| **`ARTISTE_CODE`** | `VARCHAR(50)` | `PK`, `FK` | Référence l'artiste (`ARTISTE.CODE`). |
| **`ALIAS`** | `VARCHAR(100)` | `PK` | Nom de l'alias. |

* **Clé Primaire Composée :** `(ARTISTE_CODE, ALIAS)`
* **Règle de suppression :** `ON DELETE CASCADE` (La suppression de l'artiste entraîne automatiquement celle de ses alias).

---

### Table : `STYLE`

Référentiel des styles ou genres musicaux.

| Colonne | Type | Modificateurs | Description |
| :--- | :--- | :--- | :--- |
| **`CODE`** | `VARCHAR(50)` | `PRIMARY KEY` | Code unique du style (ex: `ROCK`, `JAZZ`). |
| **`LIBELLE`** | `VARCHAR(100)` | `NOT NULL` | Libellé lisible du style. |
| **`Commentaire`** | `TEXT` | `NULL` | Description du style. |

* **Règle de suppression :** `ON DELETE RESTRICT` (Impossible de supprimer un style s'il est utilisé par au moins un artiste ou un titre).

---

### Table : `TITRE`

Répertorie les œuvres/titres musicaux créés par un artiste.

| Colonne | Type | Modificateurs | Description |
| :--- | :--- | :--- | :--- |
| **`ARTISTE_CODE`** | `VARCHAR(50)` | `PK`, `FK` | Référence l'artiste auteur (`ARTISTE.CODE`). |
| **`LIBELLE`** | `VARCHAR(150)` | `PK` | Intitulé ou nom du titre. |
| **`STYLE_CODE`** | `VARCHAR(50)` | `FOREIGN KEY` | Référence le style spécifique du titre (`STYLE.CODE`). |
| **`STYLE_POIDS`** | `INTEGER` | `CHECK (0 TO 1)` | Pondération du style pour le titre (0 à 1). |

* **Clé Primaire Composée :** `(ARTISTE_CODE, LIBELLE)`
* **Règle de suppression :** `ON DELETE CASCADE` (La suppression de l'artiste entraîne la suppression de ses titres).

---

## 2. Relations et Intégrité Référentielle

1. **Relation `ARTISTE` $\rightarrow$ `ALIAS` (1 à plusieurs) :**
   * **Contrainte :** `ON DELETE CASCADE`.
   * **Effet :** Lorsqu'un artiste est supprimé, l'ensemble de ses alias enregistrés dans la table `ALIAS` sont automatiquement supprimés.

2. **Relation `ARTISTE` $\rightarrow$ `TITRE` (1 à plusieurs) :**
   * **Contrainte :** `ON DELETE CASCADE`.
   * **Effet :** Lorsqu'un artiste est supprimé, tous les titres qui lui sont rattachés dans la table `TITRE` sont automatiquement supprimés.

3. **Relation `STYLE` $\rightarrow$ `ARTISTE` et `STYLE` $\rightarrow$ `TITRE` (1 à plusieurs) :**
   * **Contrainte :** `ON DELETE RESTRICT` (ou `NO ACTION`).
   * **Effet :** La suppression d'un style dans la table `STYLE` est bloquée si ce code est encore référencé par un ou plusieurs artistes ou titres.

4. **Contrainte de Validité (`STYLE_POIDS`) :**
   * Pour `ARTISTE` et `TITRE`, la colonne `STYLE_POIDS` accepte uniquement des valeurs entières comprises entre 0 et 1 via une clause `CHECK (STYLE_POIDS BETWEEN 0 AND 1)`.

---

## 3. Script SQL DDL (Création des Tables)

```sql
-- 1. Table STYLE (doit être créée en premier car référencée par ARTISTE et TITRE)
CREATE TABLE STYLE (
    CODE VARCHAR(50) PRIMARY KEY,
    LIBELLE VARCHAR(100) NOT NULL,
    Commentaire TEXT
);

-- 2. Table ARTISTE
CREATE TABLE ARTISTE (
    CODE VARCHAR(50) PRIMARY KEY,
    NOM VARCHAR(100) NOT NULL,
    STYLE_CODE VARCHAR(50),
    STYLE_POIDS INT CHECK (STYLE_POIDS BETWEEN 0 AND 1),
    Commentaire TEXT,
    CONSTRAINT fk_artiste_style 
        FOREIGN KEY (STYLE_CODE) 
        REFERENCES STYLE(CODE) 
        ON DELETE RESTRICT
);

-- 3. Table ALIAS
CREATE TABLE ALIAS (
    ARTISTE_CODE VARCHAR(50) NOT NULL,
    ALIAS VARCHAR(100) NOT NULL,
    PRIMARY KEY (ARTISTE_CODE, ALIAS),
    CONSTRAINT fk_alias_artiste 
        FOREIGN KEY (ARTISTE_CODE) 
        REFERENCES ARTISTE(CODE) 
        ON DELETE CASCADE
);

-- 4. Table TITRE
CREATE TABLE TITRE (
    ARTISTE_CODE VARCHAR(50) NOT NULL,
    LIBELLE VARCHAR(150) NOT NULL,
    STYLE_CODE VARCHAR(50),
    STYLE_POIDS INT CHECK (STYLE_POIDS BETWEEN 0 AND 1),
    PRIMARY KEY (ARTISTE_CODE, LIBELLE),
    CONSTRAINT fk_titre_artiste 
        FOREIGN KEY (ARTISTE_CODE) 
        REFERENCES ARTISTE(CODE) 
        ON DELETE CASCADE,
    CONSTRAINT fk_titre_style 
        FOREIGN KEY (STYLE_CODE) 
        REFERENCES STYLE(CODE) 
        ON DELETE RESTRICT
);
```

---

## 5. Réalisation dans LuXia (v1.011, discussion dev du 2026-10-03)

Ce modèle de l'utilisateur a été **analysé puis simplifié** (décisions Q53 à Q57, D41) ; la réalisation est en **JSON** dans le projet ([doc 50 §12j](../50-format-des-donnees.md)),
pas en base SQL. Correspondance :

| Modèle ci-dessus | Dans LuXia |
|---|---|
| `ARTISTE.CODE`, `NOM`, `STYLE_CODE` | `artistes.json` : `code` (« A00012 », stable), `name`, `style` |
| `ARTISTE.STYLE_POIDS`, `TITRE.STYLE_POIDS` | **Supprimés** : un artiste = un style, un titre a son style ou celui de l'artiste (le poids ne servait qu'à baisser une confiance de 0,1) |
| `ARTISTE.Commentaire`, `STYLE.Commentaire` | **Non repris** (aucun usage dans l'identification) ; à ajouter si l'utilisateur en veut |
| `ALIAS` (`ARTISTE_CODE`, `ALIAS`) | `aliases` de l'artiste ; **un alias est unique dans toute la base** et ne peut pas être le nom d'un autre artiste (contrôlé à l'enregistrement de la fiche) |
| `STYLE.CODE`, `LIBELLE` | `taxonomie.json` : `id`, `name` (modifiable) ; l'ordre de la liste est l'ordre d'affichage ; « Inconnu » est une vraie ligne |
| `TITRE` (`ARTISTE_CODE`, `LIBELLE`, `STYLE_CODE`) | `titres.json` : `artist`, `title`, `version`, `style` **facultatif** ; **titres non livrés** |
| Origine (« source »), historique des corrections | **Supprimés** (`corrections.json` et `aclasser.json` abandonnés, anciens fichiers en `.v1.bak`) |
| Export des trois tables | **Échange JSON** `styles` / `artists` / `aliases` ([schéma](../schemas/echange-base-musicale.schema.json)) ; l'import du gros fichier de base de l'utilisateur suit ce format (⏳ Q57) |

