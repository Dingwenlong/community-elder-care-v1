import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../core/api/api_client.dart';
import '../core/api/contracts.dart';
import '../core/storage/secure_session_store.dart';

const _apiBaseUrl = String.fromEnvironment(
  'API_BASE_URL',
  defaultValue: 'http://10.0.2.2:5180',
);

final initialSessionProvider = Provider<SessionState?>((ref) => null);
final sessionStoreProvider = Provider<SessionStore>(
  (ref) => SecureSessionStore(),
);
final apiClientProvider = Provider<ApiClient>((ref) {
  final client = ApiClient(baseUri: Uri.parse(_apiBaseUrl));
  ref.onDispose(client.close);
  return client;
});

final sessionControllerProvider =
    StateNotifierProvider<SessionController, SessionState?>((ref) {
      return SessionController(
        store: ref.watch(sessionStoreProvider),
        apiClient: ref.watch(apiClientProvider),
        initialSession: ref.watch(initialSessionProvider),
      );
    });

class SessionController extends StateNotifier<SessionState?> {
  SessionController({
    required this.store,
    required this.apiClient,
    SessionState? initialSession,
  }) : super(initialSession) {
    if (initialSession != null) {
      apiClient.setAccessToken(initialSession.token);
    } else {
      _restore();
    }
  }

  final SessionStore store;
  final ApiClient apiClient;

  Future<void> _restore() async {
    final restored = await store.read();
    if (!mounted || restored == null) return;
    apiClient.setAccessToken(restored.token);
    state = restored;
  }

  Future<void> login(String username, String password) async {
    final response = await apiClient.post<LoginResponse>(
      '/api/v1/auth/login',
      (json) => LoginResponse.fromJson(
        Map<String, Object?>.from(json! as Map<String, dynamic>),
      ),
      body: {'username': username.trim(), 'password': password},
    );
    final session = SessionState(
      token: response.accessToken,
      role: response.role,
      isDemoMode: response.isDemoMode,
      elderId: response.elderId,
    );
    apiClient.setAccessToken(session.token);
    await store.write(session);
    if (mounted) state = session;
  }

  Future<void> logout() async {
    apiClient.setAccessToken(null);
    await store.clear();
    if (mounted) state = null;
  }

  Future<void> switchDemoAccount() => logout();
}
