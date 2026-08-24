import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../api/contracts.dart';

abstract class SessionStore {
  Future<SessionState?> read();
  Future<void> write(SessionState session);
  Future<void> clear();
}

class SecureSessionStore implements SessionStore {
  SecureSessionStore({FlutterSecureStorage? storage})
    : _storage = storage ?? const FlutterSecureStorage();

  static const _sessionKey = 'community_care_demo_session';
  final FlutterSecureStorage _storage;

  @override
  Future<SessionState?> read() async {
    final value = await _storage.read(key: _sessionKey);
    if (value == null) return null;
    try {
      return SessionState.fromJson(
        Map<String, Object?>.from(jsonDecode(value) as Map<String, dynamic>),
      );
    } on Object {
      await clear();
      return null;
    }
  }

  @override
  Future<void> write(SessionState session) =>
      _storage.write(key: _sessionKey, value: jsonEncode(session.toJson()));

  @override
  Future<void> clear() => _storage.delete(key: _sessionKey);
}
