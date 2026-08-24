import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:mobile/app/community_care_app.dart';
import 'package:mobile/auth/session_controller.dart';
import 'package:mobile/core/api/contracts.dart';
import 'package:mobile/family/home/family_status_controller.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('family sees authorized summaries but no raw or internal notes', (
    tester,
  ) async {
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
          familyStatusGatewayProvider.overrideWithValue(
            const GrantedFamilyStatusGateway(),
          ),
        ],
        child: const CommunityCareApp(),
      ),
    );
    await pumpUntilText(tester, '最近状态');

    expect(find.text('照料进展'), findsOneWidget);
    expect(find.text('探访摘要'), findsOneWidget);
    expect(find.text('AI 原始对话'), findsNothing);
    expect(find.text('社区内部备注'), findsNothing);
    expect(find.textContaining('详细住址'), findsNothing);
  });
}

Future<void> pumpUntilText(WidgetTester tester, String text) async {
  for (var attempt = 0; attempt < 50; attempt++) {
    await tester.pump(const Duration(milliseconds: 100));
    if (find.text(text).evaluate().isNotEmpty) return;
  }
  fail('Timed out waiting for "$text".');
}

class GrantedFamilyStatusGateway implements FamilyStatusGateway {
  const GrantedFamilyStatusGateway();

  @override
  Future<FamilyStatusSnapshot> load(String elderId) async =>
      FamilyStatusSnapshot(
        elderDisplayName: '演示·李安康',
        grantedFields: const {
          ConsentField.recentStatus,
          ConsentField.careEventSummary,
          ConsentField.visitSummary,
        },
        consentExpiresAt: DateTime.utc(2027, 8, 24),
        recentStatus: '今天 08:05 已完成平安确认',
        reminderSummary: null,
        careProgress: '社区正在电话确认',
        visitSummary: '社区人员昨日完成上门探访',
        lastCommunityConfirmation: '今天 08:12 社区已记录确认进展',
      );
}
