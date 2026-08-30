import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:mobile/app/community_care_app.dart';
import 'package:mobile/auth/session_controller.dart';
import 'package:mobile/core/api/api_problem.dart';
import 'package:mobile/core/api/contracts.dart';
import 'package:mobile/family/home/family_status_controller.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('revocation clears protected summaries immediately', (
    tester,
  ) async {
    final gateway = RevocableFamilyStatusGateway();
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          initialSessionProvider.overrideWithValue(
            const SessionState(
              token: 'family-token',
              role: DemoRole.family,
              isDemoMode: true,
              elderId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
            ),
          ),
          familyStatusGatewayProvider.overrideWithValue(gateway),
        ],
        child: const CommunityCareApp(),
      ),
    );
    await pumpUntilText(tester, '今天 08:05 已完成平安确认');

    gateway.revoked = true;
    await tester.tap(find.text('刷新授权状态'));
    await pumpUntilText(tester, '老人已撤回此项授权');

    expect(find.text('今天 08:05 已完成平安确认'), findsNothing);
    expect(find.text('社区正在电话确认'), findsNothing);

    final container = ProviderScope.containerOf(
      tester.element(find.text('老人已撤回此项授权')),
    );
    await container
        .read(sessionControllerProvider.notifier)
        .switchDemoAccount();
    await tester.pumpAndSettle();
    expect(find.text('安邻照料'), findsOneWidget);
    expect(find.text('今天 08:05 已完成平安确认'), findsNothing);
  });
}

Future<void> pumpUntilText(WidgetTester tester, String text) async {
  for (var attempt = 0; attempt < 50; attempt++) {
    await tester.pump(const Duration(milliseconds: 100));
    if (find.text(text).evaluate().isNotEmpty) return;
  }
  fail('Timed out waiting for "$text".');
}

class RevocableFamilyStatusGateway implements FamilyStatusGateway {
  bool revoked = false;

  @override
  Future<FamilyStatusSnapshot> load(String elderId) async {
    if (revoked) {
      throw const ApiProblem(
        statusCode: 403,
        code: 'CONSENT_REQUIRED',
        message: '老人尚未授权查看这项资料。',
      );
    }
    return FamilyStatusSnapshot(
      elderDisplayName: '李安康',
      grantedFields: const {
        ConsentField.recentStatus,
        ConsentField.careEventSummary,
      },
      consentExpiresAt: DateTime.utc(2027, 8, 24),
      recentStatus: '今天 08:05 已完成平安确认',
      reminderSummary: null,
      careProgress: '社区正在电话确认',
      visitSummary: null,
      lastCommunityConfirmation: null,
    );
  }
}
