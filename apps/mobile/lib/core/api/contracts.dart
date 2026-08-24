enum DemoRole {
  elder,
  family,
  communityStaff,
  serviceWorker,
  administrator;

  static DemoRole fromJson(String value) => switch (value) {
    'Elder' => DemoRole.elder,
    'Family' => DemoRole.family,
    'CommunityStaff' => DemoRole.communityStaff,
    'ServiceWorker' => DemoRole.serviceWorker,
    'Administrator' => DemoRole.administrator,
    _ => throw FormatException('Unsupported demo role: $value'),
  };

  String get jsonName => switch (this) {
    DemoRole.elder => 'Elder',
    DemoRole.family => 'Family',
    DemoRole.communityStaff => 'CommunityStaff',
    DemoRole.serviceWorker => 'ServiceWorker',
    DemoRole.administrator => 'Administrator',
  };
}

enum ConsentField {
  recentStatus,
  careEventSummary,
  visitSummary,
  reminderCompletion,
  healthRiskSummary,
  emergencyContact,
}

class SessionState {
  const SessionState({
    required this.token,
    required this.role,
    required this.isDemoMode,
  });

  final String token;
  final DemoRole role;
  final bool isDemoMode;

  Map<String, Object?> toJson() => {
    'token': token,
    'role': role.jsonName,
    'isDemoMode': isDemoMode,
  };

  static SessionState fromJson(Map<String, Object?> json) => SessionState(
    token: json['token']! as String,
    role: DemoRole.fromJson(json['role']! as String),
    isDemoMode: json['isDemoMode']! as bool,
  );
}

class LoginResponse {
  const LoginResponse({
    required this.accessToken,
    required this.expiresAt,
    required this.role,
    required this.isDemoMode,
  });

  final String accessToken;
  final DateTime expiresAt;
  final DemoRole role;
  final bool isDemoMode;

  static LoginResponse fromJson(Map<String, Object?> json) => LoginResponse(
    accessToken: json['accessToken']! as String,
    expiresAt: DateTime.parse(json['expiresAt']! as String),
    role: DemoRole.fromJson(json['role']! as String),
    isDemoMode: json['isDemoMode']! as bool,
  );
}
