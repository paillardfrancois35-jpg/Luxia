/*
  POC pupitre lumiere - recepteur DMX
  Cible : Arduino Leonardo + shield DMX Conceptinetics CTC-DRA-10-R2

  ---------------------------------------------------------------------------
  BIBLIOTHEQUE : "DMXSerial" (Matthias Hertel) - a installer via le
  gestionnaire de bibliotheques de l'IDE Arduino. Aucune modification a faire.
  ---------------------------------------------------------------------------
  - Sur la Leonardo (32U4), DMXSerial utilise automatiquement Serial1
    (broches 0/1). L'USB ('Serial') reste libre pour parler au PC.
  - Broche de direction du RS485 (DE/RE) : 2 par defaut  -> cavalier du
    shield a placer sur la broche 2.

  ---------------------------------------------------------------------------
  CAVALIERS DU SHIELD (a confirmer sur photo)
  ---------------------------------------------------------------------------
  - Direction (DE/RE) : broche 2
  - Mode : MASTER / emission
  - Terminaison 120 ohms : ON si le PAR est en bout de chaine DMX

  ---------------------------------------------------------------------------
  PROTOCOLE SERIE  (USB, cote 'Serial')
  ---------------------------------------------------------------------------
      0x7E 0x11   lenLo lenHi   payload[len]   0xE7

  len     = nombre de canaux DMX, little-endian (1..512)
  payload = valeurs 0..255 a partir du canal 1
  Resynchro : on attend 0x7E 0x11 ; trame ignoree si l'octet final n'est pas 0xE7
  Securite : sans trame valide pendant 2 s, tous les canaux retombent a 0

  LED interne (broche 13) : bascule a chaque trame valide recue
*/

#include <DMXSerial.h>

static const uint16_t DMX_CHANNELS = 512;

static const uint8_t SOM1 = 0x7E;
static const uint8_t SOM2 = 0x11;
static const uint8_t EOM  = 0xE7;

static uint8_t  payload[DMX_CHANNELS];
static uint32_t lastFrameMs = 0;

enum RxState : uint8_t { WAIT_SOM1, WAIT_SOM2, LEN_LO, LEN_HI, PAYLOAD, WAIT_EOM };
static RxState  st   = WAIT_SOM1;
static uint16_t need = 0;
static uint16_t got  = 0;

static void blackout() {
  for (uint16_t c = 1; c <= DMX_CHANNELS; c++) {
    DMXSerial.write(c, 0);
  }
}

void setup() {
  Serial.begin(115200);            // USB CDC : le debit est ignore, on initialise quand meme
  pinMode(LED_BUILTIN, OUTPUT);

  DMXSerial.init(DMXController);    // mode controleur (emission) ; broche direction = 2
  blackout();
}

void loop() {
  while (Serial.available() > 0) {
    uint8_t b = (uint8_t)Serial.read();

    switch (st) {
      case WAIT_SOM1:
        st = (b == SOM1) ? WAIT_SOM2 : WAIT_SOM1;
        break;

      case WAIT_SOM2:
        st = (b == SOM2) ? LEN_LO : WAIT_SOM1;
        break;

      case LEN_LO:
        need = b;
        st   = LEN_HI;
        break;

      case LEN_HI:
        need |= (uint16_t)b << 8;
        if (need == 0 || need > DMX_CHANNELS) {
          st = WAIT_SOM1;
        } else {
          got = 0;
          st  = PAYLOAD;
        }
        break;

      case PAYLOAD:
        payload[got++] = b;
        if (got >= need) st = WAIT_EOM;
        break;

      case WAIT_EOM:
        if (b == EOM) {
          for (uint16_t i = 0; i < need; i++) {
            DMXSerial.write(i + 1, payload[i]);
          }
          lastFrameMs = millis();
          digitalWrite(LED_BUILTIN, !digitalRead(LED_BUILTIN));
        }
        st = WAIT_SOM1;
        break;
    }
  }

  // Perte de liaison : extinction de securite
  if (lastFrameMs != 0 && (millis() - lastFrameMs) > 2000) {
    blackout();
    lastFrameMs = 0;
  }
}
