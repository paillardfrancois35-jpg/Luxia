/*
  DMX – interface USB → DMX512
  Cible : Arduino Leonardo + shield Conceptinetics CTC-DRA-10-R2
  Cahier des charges : docs/10-sortie-dmx-et-firmware.md (SORT-040 à SORT-049)

  Protocole PC -> Arduino : sous-ensemble Enttec DMX USB Pro (doc 10 §5)
      0x7E | label | longueur LSB | longueur MSB | données | 0xE7

    label  6  (PC→A)  Output Only Send DMX : start code (0) + 1..512 valeurs
    label 10  (PC→A)  Get Widget Serial Number        -> réponse 4 octets
    label  3  (PC→A)  Get Widget Parameters (2 oct.)  -> version, break, MAB, débit
    label 77  (PC→A)  Identification du projet        -> "DMX-LEONARDO;fw=x.y;ch=512"
    label 0x11 (PC→A) Ancien format du POC (valeurs sans start code), accepté pendant la transition

  Comportement :
    - la ligne DMX est rafraîchie en continu par DMXSerial, indépendamment du PC (SORT-040) ;
    - tous les canaux à 0 au démarrage (SORT-041) ;
    - chien de garde : sans message DMX valide pendant 2 s, tous les canaux à 0, fumée comprise (SORT-042) ;
    - un message n'est appliqué qu'une fois reçu en entier et valide (SORT-045) ;
    - aucune allocation dynamique, un seul tampon de réception de 513 octets (SORT-044).

  LED interne (SORT-046) : bascule à chaque message DMX valide (clignote pendant l'émission) ;
  allumée fixe si le chien de garde s'est déclenché ; éteinte tant qu'aucun message n'a été reçu.
*/

#include <DMXSerial.h>

// SORT-047 : version unique du firmware, renvoyée par les labels 3 et 77.
#define FW_VERSION_MAJOR 1
#define FW_VERSION_MINOR 0
#define STR_(x) #x
#define STR(x) STR_(x)
static const char IDENTITY[] = "DMX-LEONARDO;fw=" STR(FW_VERSION_MAJOR) "." STR(FW_VERSION_MINOR) ";ch=512";

static const uint16_t DMX_CHANNELS = 512;
static const uint16_t MAX_DATA = DMX_CHANNELS + 1;  // start code + 512 valeurs
static const uint16_t MIN_LINE_CHANNELS = 24;       // trame DMX la plus courte émise (SORT-049)
static const uint32_t WATCHDOG_MS = 2000;           // GEN-080

static const uint8_t SOM = 0x7E;
static const uint8_t EOM = 0xE7;
static const uint8_t LABEL_GET_PARAMS = 3;
static const uint8_t LABEL_SEND_DMX = 6;
static const uint8_t LABEL_GET_SERIAL = 10;
static const uint8_t LABEL_LEGACY_DMX = 0x11;
static const uint8_t LABEL_IDENTIFY = 77;

// Numéro de série fictif (BCD, poids faible en premier), exigé par les logiciels compatibles Enttec.
static const uint8_t SERIAL_NUMBER[4] = {0x01, 0x00, 0x00, 0x00};

static uint8_t rx[MAX_DATA];

enum RxState : uint8_t { WAIT_SOM, LABEL, LEN_LO, LEN_HI, DATA, WAIT_EOM };
static RxState state = WAIT_SOM;
static uint8_t label = 0;
static uint16_t length = 0;
static uint16_t received = 0;

enum LinkState : uint8_t { NEVER_CONNECTED, RECEIVING, WATCHDOG_TRIPPED };
static LinkState link = NEVER_CONNECTED;
static uint32_t lastDmxMs = 0;
static uint16_t lineChannels = DMX_CHANNELS;

static void setAll(uint8_t value) {
  for (uint16_t c = 1; c <= DMX_CHANNELS; c++) {
    DMXSerial.write(c, value);
  }
}

static void sendMessage(uint8_t lbl, const uint8_t *data, uint16_t len) {
  Serial.write(SOM);
  Serial.write(lbl);
  Serial.write((uint8_t)(len & 0xFF));
  Serial.write((uint8_t)(len >> 8));
  if (len > 0) {
    Serial.write(data, len);
  }
  Serial.write(EOM);
}

// SORT-049 : la ligne n'émet que les canaux reçus (au moins MIN_LINE_CHANNELS) : fréquence DMX plus élevée pour les petits parcs.
static void applyChannels(const uint8_t *values, uint16_t count) {
  for (uint16_t i = 0; i < count; i++) {
    DMXSerial.write(i + 1, values[i]);
  }

  uint16_t wanted = count < MIN_LINE_CHANNELS ? MIN_LINE_CHANNELS : count;
  if (wanted != lineChannels) {
    lineChannels = wanted;
    DMXSerial.maxChannel(lineChannels);
  }

  lastDmxMs = millis();
  link = RECEIVING;
  digitalWrite(LED_BUILTIN, !digitalRead(LED_BUILTIN));
}

static void handleMessage() {
  switch (label) {
    case LABEL_SEND_DMX:
      // Start code différent de 0 : protocole non éclairage, ignoré (doc 10 §5.4).
      if (length >= 2 && rx[0] == 0) {
        applyChannels(rx + 1, length - 1);
      }
      break;

    case LABEL_LEGACY_DMX:
      if (length >= 1 && length <= DMX_CHANNELS) {
        applyChannels(rx, length);
      }
      break;

    case LABEL_GET_SERIAL:
      sendMessage(LABEL_GET_SERIAL, SERIAL_NUMBER, sizeof(SERIAL_NUMBER));
      break;

    case LABEL_GET_PARAMS: {
      // Version firmware (LSB, MSB), durée du break (×10,67 µs), durée du MAB (×10,67 µs), débit (trames/s).
      const uint8_t params[5] = {FW_VERSION_MINOR, FW_VERSION_MAJOR, 9, 1, 40};
      sendMessage(LABEL_GET_PARAMS, params, sizeof(params));
      break;
    }

    case LABEL_IDENTIFY:
      sendMessage(LABEL_IDENTIFY, (const uint8_t *)IDENTITY, sizeof(IDENTITY) - 1);
      break;

    default:
      break;  // label inconnu : ignoré
  }
}

static void feed(uint8_t b) {
  switch (state) {
    case WAIT_SOM:
      if (b == SOM) state = LABEL;
      break;

    case LABEL:
      label = b;
      state = LEN_LO;
      break;

    case LEN_LO:
      length = b;
      state = LEN_HI;
      break;

    case LEN_HI:
      length |= (uint16_t)b << 8;
      received = 0;
      if (length > MAX_DATA) {
        state = WAIT_SOM;  // longueur impossible : message rejeté, resynchronisation
      } else {
        state = (length == 0) ? WAIT_EOM : DATA;
      }
      break;

    case DATA:
      rx[received++] = b;
      if (received >= length) state = WAIT_EOM;
      break;

    case WAIT_EOM:
      // SORT-045 : appliqué seulement si l'octet final est correct ; sinon la dernière trame valide reste en place.
      if (b == EOM) handleMessage();
      state = WAIT_SOM;
      break;
  }
}

void setup() {
  pinMode(LED_BUILTIN, OUTPUT);
  digitalWrite(LED_BUILTIN, LOW);

  // Mode contrôleur : DMXSerial émet en continu sur Serial1 ; broche de direction RS-485 = 2 (SORT-040).
  DMXSerial.init(DMXController);
  setAll(0);  // SORT-041
  DMXSerial.maxChannel(lineChannels);

  Serial.begin(115200);  // USB natif : débit ignoré ; ne jamais utiliser 1200 côté PC (SORT-012)
}

void loop() {
  while (Serial.available() > 0) {
    feed((uint8_t)Serial.read());
  }

  // SORT-042 / GEN-080 : perte du PC -> blackout complet.
  if (link == RECEIVING && (millis() - lastDmxMs) > WATCHDOG_MS) {
    setAll(0);
    link = WATCHDOG_TRIPPED;
    digitalWrite(LED_BUILTIN, HIGH);
  }
}
