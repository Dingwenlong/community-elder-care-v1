import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:mobile/app/community_care_app.dart';
import 'package:mobile/auth/session_controller.dart';
import 'package:mobile/core/api/contracts.dart';
import 'package:mobile/family/events/family_report_controller.dart';
import 'package:mobile/family/home/family_status_controller.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('family report retry references one pending-confirmation event', (
    tester,
  ) async {
    final reportGateway = RecordingFamilyReportGateway();
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
            const ReportFamilyStatusGateway(),
          ),
          familyReportGatewayProvider.overrideWithValue(reportGateway),
        ],
        child: const CommunityCareApp(),
      ),
    );
    await pumpUntilText(tester, '报告联系不上老人');

    await tester.tap(find.text('报告联系不上老人'));
    await pumpUntilText(tester, '社区正在电话确认');
    await tester.tap(find.text('报告联系不上老人'));
    await tester.pump(const Duration(milliseconds: 300));

    expect(reportGateway.requestIds, hasLength(2));
    expect(reportGateway.requestIds.toSet(), hasLength(1));
    expect(find.text('来源：家属上报'), findsOneWidget);
    expect(find.text('级别：需要确认'), findsOneWidget);
    expect(find.text('状态：等待社区确认'), findsOneWidget);
    for (final forbidden in const ['事件级别', '指派', '状态流转', '升级', '关闭事件']) {
      expect(find.text(forbidden), findsNothing);
    }
  });
}

Future<void> pumpUntilText(WidgetTester tester, String text) async {
  for (var attempt = 0; attempt < 50; attempt++) {
    await tester.pump(const Duration(milliseconds: 100));
    if (find.text(text).evaluate().isNotEmpty) return;
  }
  fail('Timed out waiting for "$text".');
}

class ReportFamilyStatusGateway implements FamilyStatusGateway {
  const ReportFamilyStatusGateway();

  @override
  Future<FamilyStatusSnapshot> load(String elderId) async =>
      FamilyStatusSnapshot(
        elderDisplayName: '演示·李安康',
        grantedFields: const {ConsentField.careEventSummary},
        consentExpiresAt: DateTime.utc(2027, 8, 24),
        recentStatus: null,
        reminderSummary: null,
        careProgress: '当前没有待确认事件',
        visitSummary: null,
        lastCommunityConfirmation: null,
      );
}

class RecordingFamilyReportGateway implements FamilyReportGateway {
  final requestIds = <String>[];

  @override
  Future<FamilyEventSummary> reportCannotReach({
    required String elderId,
    required String clientRequestId,
    required DateTime occurredAt,
    String? note,
  }) async {
    requestIds.add(clientRequestId);
    return const FamilyEventSummary(
      id: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',
      source: 'FamilyReport',
      level: 'NeedsConfirmation',
      status: 'PendingConfirmation',
      summary: '社区正在电话确认',
    );
  }
}
