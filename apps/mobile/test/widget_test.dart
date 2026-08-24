import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/auth/session_controller.dart';
import 'package:mobile/core/api/contracts.dart';
import 'package:mobile/core/storage/secure_session_store.dart';
import 'package:mobile/main.dart';

void main() {
  test('login session derives the scoped elder id from the server token', () {
    final payload = base64Url
        .encode(
          utf8.encode(
            jsonEncode({'elder_id': 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'}),
          ),
        )
        .replaceAll('=', '');
    final response = LoginResponse.fromJson({
      'accessToken': 'header.$payload.signature',
      'expiresAt': '2026-08-24T16:00:00Z',
      'role': 'Elder',
      'isDemoMode': true,
    });

    expect(response.elderId, 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa');
  });

  testWidgets('shows the elder and family demo login entry', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          sessionStoreProvider.overrideWithValue(const EmptySessionStore()),
        ],
        child: const CommunityCareApp(),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('社区独居老人照料系统'), findsOneWidget);
    expect(find.text('演示账号'), findsOneWidget);
    expect(find.text('登录演示 App'), findsOneWidget);
  });
}

class EmptySessionStore implements SessionStore {
  const EmptySessionStore();

  @override
  Future<void> clear() async {}

  @override
  Future<SessionState?> read() async => null;

  @override
  Future<void> write(SessionState session) async {}
}
