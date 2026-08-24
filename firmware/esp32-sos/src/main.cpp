#include <Arduino.h>
#include <HTTPClient.h>
#include <WiFi.h>
#include <esp_system.h>
#include <time.h>

#include "demo_config.h"

namespace {
constexpr uint8_t kButtonPin = 0;
constexpr uint8_t kLedPin = 2;
constexpr unsigned long kDebounceMs = 50;
constexpr unsigned long kHoldMs = 2000;
constexpr unsigned long kWifiTimeoutMs = 10000;
constexpr int kMaxAttempts = 3;

bool stablePressed = false;
bool lastReading = false;
bool sentForCurrentPress = false;
unsigned long lastReadingChangedAt = 0;
unsigned long pressedAt = 0;

void setLed(bool enabled) {
  digitalWrite(kLedPin, enabled ? HIGH : LOW);
}

void blinkLed(int count, unsigned long onMs, unsigned long offMs) {
  for (int index = 0; index < count; ++index) {
    setLed(true);
    delay(onMs);
    setLed(false);
    delay(offMs);
  }
}

bool connectWifi() {
  if (WiFi.status() == WL_CONNECTED) {
    return true;
  }

  WiFi.mode(WIFI_STA);
  WiFi.begin(COMMUNITYCARE_WIFI_SSID, COMMUNITYCARE_WIFI_PASSWORD);
  const unsigned long startedAt = millis();
  while (WiFi.status() != WL_CONNECTED && millis() - startedAt < kWifiTimeoutMs) {
    delay(200);
  }
  return WiFi.status() == WL_CONNECTED;
}

String createEventId() {
  uint8_t bytes[16];
  for (uint8_t index = 0; index < sizeof(bytes); index += 4) {
    const uint32_t randomValue = esp_random();
    memcpy(bytes + index, &randomValue, sizeof(randomValue));
  }
  bytes[6] = (bytes[6] & 0x0F) | 0x40;
  bytes[8] = (bytes[8] & 0x3F) | 0x80;

  char value[37];
  snprintf(
      value,
      sizeof(value),
      "%02x%02x%02x%02x-%02x%02x-%02x%02x-%02x%02x-%02x%02x%02x%02x%02x%02x",
      bytes[0], bytes[1], bytes[2], bytes[3], bytes[4], bytes[5], bytes[6], bytes[7],
      bytes[8], bytes[9], bytes[10], bytes[11], bytes[12], bytes[13], bytes[14], bytes[15]);
  return String(value);
}

String deviceTimeIso() {
  const time_t now = time(nullptr);
  if (now < 1577836800) {
    return "1970-01-01T00:00:00Z";
  }

  struct tm utcTime;
  gmtime_r(&now, &utcTime);
  char value[21];
  strftime(value, sizeof(value), "%Y-%m-%dT%H:%M:%SZ", &utcTime);
  return String(value);
}

String buildPayload(const String& eventId) {
  String payload;
  payload.reserve(280);
  payload += "{\"deviceId\":\"";
  payload += COMMUNITYCARE_DEVICE_ID;
  payload += "\",\"eventId\":\"";
  payload += eventId;
  payload += "\",\"deviceTime\":\"";
  payload += deviceTimeIso();
  payload += "\",\"signalType\":\"SosButton\",\"buttonState\":\"Held2Seconds\"}";
  return payload;
}

bool postSos(const String& eventId) {
  if (!connectWifi()) {
    return false;
  }

  const String endpoint = String(COMMUNITYCARE_API_BASE_URL) + "/api/v1/device-signals";
  const String payload = buildPayload(eventId);
  for (int attempt = 0; attempt < kMaxAttempts; ++attempt) {
    setLed(true);
    HTTPClient http;
    http.setConnectTimeout(5000);
    http.setTimeout(8000);
    http.begin(endpoint);
    http.addHeader("Content-Type", "application/json");
    http.addHeader("X-Device-Token", COMMUNITYCARE_DEVICE_TOKEN);
    const int statusCode = http.POST(payload);
    http.end();
    setLed(false);
    if (statusCode >= 200 && statusCode < 300) {
      return true;
    }
    if (attempt + 1 < kMaxAttempts) {
      delay(500UL << attempt);
    }
  }
  return false;
}

void handleConfirmedHold() {
  const String eventId = createEventId();
  const bool delivered = postSos(eventId);
  if (delivered) {
    blinkLed(2, 220, 160);
  } else {
    blinkLed(3, 650, 250);
  }
}
}  // namespace

void setup() {
  pinMode(kButtonPin, INPUT_PULLUP);
  pinMode(kLedPin, OUTPUT);
  setLed(false);
  connectWifi();
  configTime(0, 0, "pool.ntp.org", "time.nist.gov");
}

void loop() {
  const bool reading = digitalRead(kButtonPin) == LOW;
  const unsigned long now = millis();
  if (reading != lastReading) {
    lastReading = reading;
    lastReadingChangedAt = now;
  }

  if (now - lastReadingChangedAt >= kDebounceMs && stablePressed != reading) {
    stablePressed = reading;
    if (stablePressed) {
      pressedAt = now;
      sentForCurrentPress = false;
    } else {
      sentForCurrentPress = false;
    }
  }

  if (stablePressed && !sentForCurrentPress && now - pressedAt >= kHoldMs) {
    sentForCurrentPress = true;
    handleConfirmedHold();
  }
  delay(10);
}
