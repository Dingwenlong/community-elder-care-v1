import 'dart:convert';

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
    this.elderId,
  });

  final String token;
  final DemoRole role;
  final bool isDemoMode;
  final String? elderId;

  Map<String, Object?> toJson() => {
    'token': token,
    'role': role.jsonName,
    'isDemoMode': isDemoMode,
    'elderId': elderId,
  };

  static SessionState fromJson(Map<String, Object?> json) => SessionState(
    token: json['token']! as String,
    role: DemoRole.fromJson(json['role']! as String),
    isDemoMode: json['isDemoMode']! as bool,
    elderId: json['elderId'] as String?,
  );
}

class LoginResponse {
  const LoginResponse({
    required this.accessToken,
    required this.expiresAt,
    required this.role,
    required this.isDemoMode,
    this.elderId,
  });

  final String accessToken;
  final DateTime expiresAt;
  final DemoRole role;
  final bool isDemoMode;
  final String? elderId;

  static LoginResponse fromJson(Map<String, Object?> json) {
    final accessToken = json['accessToken']! as String;
    return LoginResponse(
      accessToken: accessToken,
      expiresAt: DateTime.parse(json['expiresAt']! as String),
      role: DemoRole.fromJson(json['role']! as String),
      isDemoMode: json['isDemoMode']! as bool,
      elderId: _jwtStringClaim(accessToken, 'elder_id'),
    );
  }
}

String? _jwtStringClaim(String token, String name) {
  final parts = token.split('.');
  if (parts.length != 3) return null;
  try {
    final payload = jsonDecode(
      utf8.decode(base64Url.decode(base64Url.normalize(parts[1]))),
    );
    if (payload is! Map) return null;
    return payload[name] as String?;
  } on Object {
    return null;
  }
}
