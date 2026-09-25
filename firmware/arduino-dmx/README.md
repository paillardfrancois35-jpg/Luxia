# Firmware `arduino-dmx`

Interface USB → DMX512 pour **Arduino Leonardo** + shield **Conceptinetics CTC-DRA-10-R2**.
Cahier des charges : [doc 10](../../docs/10-sortie-dmx-et-firmware.md) (SORT-040 à SORT-049). Version : **1.0**.

## Protocole

Sous-ensemble du protocole **Enttec DMX USB Pro** : `0x7E | label | longueur (LSB, MSB) | données | 0xE7`.

| Label | Sens | Rôle | Réponse |
|---|---|---|---|
| 6 | PC → Arduino | Trame DMX : start code `0` + 1 à 512 valeurs | — |
| 10 | PC → Arduino | Numéro de série | 4 octets |
| 3 | PC → Arduino | Paramètres | version (mineure, majeure), break, MAB, débit |
| 77 | PC → Arduino | Identification du projet | `DMX-LEONARDO;fw=1.0;ch=512` |
| 0x11 | PC → Arduino | Ancien format du POC (sans start code) | — (transition) |

Sûreté : sans trame DMX valide pendant **2 s**, tous les canaux passent à 0 (fumée comprise).

LED interne : clignote pendant la réception ; **fixe** = chien de garde déclenché (PC perdu) ; **éteinte** = rien reçu depuis la mise sous tension.

## Matériel

| Élément | Réglage |
|---|---|
| Carte | Arduino Leonardo (USB natif, `Serial` = USB, `Serial1` = DMX) |
| Shield | Conceptinetics CTC-DRA-10-R2 |
| Cavalier direction (DE/RE) | broche **2** |
| Mode du shield | **émission** (master) |
| Cavaliers EN / !EN | position « EN » en fonctionnement ; sur Leonardo le téléversement passe par l’USB natif, séparé de `Serial1` (à confirmer sur la carte) |
| Terminaison 120 Ω | en bout de chaîne DMX (sur le dernier appareil ou par bouchon) |

## Bibliothèques

| Bibliothèque | Version testée | Installation |
|---|---|---|
| DMXSerial (Matthias Hertel) | 1.5.3 (cœur arduino:avr 1.8.8) | `arduino-cli lib install DMXSerial` |

## Compiler et téléverser

```bash
arduino-cli core install arduino:avr
arduino-cli lib install DMXSerial
arduino-cli compile --fqbn arduino:avr:leonardo firmware/arduino-dmx
arduino-cli upload --fqbn arduino:avr:leonardo -p COMx firmware/arduino-dmx
```

> Le téléversement est toujours fait **par l'utilisateur ou avec son accord** (règle Q19).

## Vérifier sans logiciel du projet

Avec un terminal série binaire, envoyer `7E 4D 00 00 E7` : la carte répond `7E 4D 1A 00 44 4D 58 2D …  E7`
(« DMX-LEONARDO;fw=1.0;ch=512 »). L'application DMX le fait automatiquement (détection du port).
