import 'dart:convert';

enum OutboxPriority {
  normal(10),
  high(100);

  const OutboxPriority(this.storageValue);

  final int storageValue;

  static OutboxPriority fromStorageValue(int value) =>
      value == high.storageValue ? high : normal;
}

enum OutboxKind {
  checkIn('CheckIn'),
  careEvent('CareEvent');

  const OutboxKind(this.storageName);

  final String storageName;

  static OutboxKind fromStorageName(String value) => switch (value) {
    'CheckIn' => checkIn,
    'CareEvent' || 'EmergencyHelp' => careEvent,
    _ => throw FormatException('Unsupported outbox kind: $value'),
  };
}

enum OutboxState { pending, failed, sent }

class OutboxEntry {
  const OutboxEntry({
    this.id,
    required this.requestId,
    required this.kind,
    required this.payload,
    required this.priority,
    required this.createdAt,
    this.attemptCount = 0,
    this.lastError,
    this.state = OutboxState.pending,
  });

  final int? id;
  final String requestId;
  final OutboxKind kind;
  final Map<String, Object?> payload;
  final OutboxPriority priority;
  final DateTime createdAt;
  final int attemptCount;
  final String? lastError;
  final OutboxState state;

  Map<String, Object?> toDatabaseRow() => {
    'request_id': requestId,
    'kind': kind.storageName,
    'payload_json': jsonEncode(payload),
    'priority': priority.storageValue,
    'created_at': createdAt.toUtc().toIso8601String(),
    'attempt_count': attemptCount,
    'last_error': lastError,
    'state': state.name,
  };

  factory OutboxEntry.fromDatabaseRow(Map<String, Object?> row) {
    final decodedPayload = jsonDecode(row['payload_json']! as String);
    return OutboxEntry(
      id: row['id']! as int,
      requestId: row['request_id']! as String,
      kind: OutboxKind.fromStorageName(row['kind']! as String),
      payload: Map<String, Object?>.from(decodedPayload as Map),
      priority: OutboxPriority.fromStorageValue(row['priority']! as int),
      createdAt: DateTime.parse(row['created_at']! as String),
      attemptCount: row['attempt_count']! as int,
      lastError: row['last_error'] as String?,
      state: OutboxState.values.byName(row['state']! as String),
    );
  }
}
