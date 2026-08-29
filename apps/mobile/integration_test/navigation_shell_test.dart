import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:integration_test/integration_test.dart';
import 'package:mobile/ai/ai_api_gateway.dart';
import 'package:mobile/app/community_care_app.dart';
import 'package:mobile/auth/session_controller.dart';
import 'package:mobile/core/api/contracts.dart';
import 'package:mobile/elder/home/elder_today_controller.dart';
import 'package:mobile/family/events/family_event_list_page.dart';
import 'package:mobile/family/events/family_report_controller.dart';
import 'package:mobile/family/home/family_status_controller.dart';
import 'package:mobile/family/records/family_care_records_page.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('elder shell keeps four tabs, help action and chat state', (
    tester,
  ) async {
    final semantics = tester.ensureSemantics();
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          initialSessionProvider.overrideWithValue(_elderSession),
          elderTodayGatewayProvider.overrideWithValue(
            const _NavigationTodayGateway(),
          ),
          aiGatewayProvider.overrideWithValue(const _NavigationAiGateway()),
        ],
        child: const CommunityCareApp(),
      ),
    );
    await _pumpUntilText(tester, '老人首页');
    _expectPersistentHelp();

    await _tapDestination(tester, '提醒');
    expect(find.text('今日提醒'), findsOneWidget);
    _expectPersistentHelp();

    await _tapDestination(tester, '陪伴');
    expect(find.text('陪伴问答'), findsOneWidget);
    await tester.enterText(find.byType(EditableText), '保留这段未发送内容');
    tester.binding.focusManager.primaryFocus?.unfocus();
    await tester.pump();

    await _tapDestination(tester, '首页');
    expect(find.text('老人首页'), findsOneWidget);
    await _tapDestination(tester, '陪伴');
    expect(
      tester.widget<EditableText>(find.byType(EditableText)).controller.text,
      '保留这段未发送内容',
    );

    tester.binding.focusManager.primaryFocus?.unfocus();
    await _tapDestination(tester, '我的');
    expect(find.text('老人设置'), findsOneWidget);
    _expectPersistentHelp();

    final context = tester.element(find.text('老人设置'));
    GoRouter.of(context).go('/family/home');
    await _pumpUntilText(tester, '老人首页');
    expect(find.text('家属首页'), findsNothing);
    semantics.dispose();
  });

  testWidgets('family shell exposes four tabs and keeps role boundaries', (
    tester,
  ) async {
    final semantics = tester.ensureSemantics();
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          initialSessionProvider.overrideWithValue(_familySession),
          familyStatusGatewayProvider.overrideWithValue(
            const _NavigationFamilyStatusGateway(),
          ),
          familyEventQueryGatewayProvider.overrideWithValue(
            const _NavigationEventGateway(),
          ),
          familyCareRecordsGatewayProvider.overrideWithValue(
            const _NavigationRecordsGateway(),
          ),
        ],
        child: const CommunityCareApp(),
      ),
    );
    await _pumpUntilText(tester, '家属首页');

    await _tapDestination(tester, '事件');
    expect(find.text('照料事件'), findsWidgets);
    await _tapDestination(tester, '照料记录');
    expect(find.text('当前授权范围内暂无照料记录。'), findsOneWidget);
    await _tapDestination(tester, '我的');
    expect(find.text('家属设置'), findsOneWidget);
    await _tapDestination(tester, '最近状态');
    expect(find.text('家属首页'), findsOneWidget);

    final context = tester.element(find.text('家属首页'));
    GoRouter.of(context).go('/elder/home');
    await _pumpUntilText(tester, '家属首页');
    expect(find.text('老人首页'), findsNothing);
    semantics.dispose();
  });
}

const _elderSession = SessionState(
  token: 'elder-navigation-token',
  role: DemoRole.elder,
  isDemoMode: true,
  elderId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
);

const _familySession = SessionState(
  token: 'family-navigation-token',
  role: DemoRole.family,
  isDemoMode: true,
  elderId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
);

Future<void> _tapDestination(WidgetTester tester, String label) async {
  await tester.tap(find.bySemanticsLabel(label));
  await tester.pumpAndSettle();
}

void _expectPersistentHelp() {
  expect(find.bySemanticsLabel('打开求助类别'), findsOneWidget);
}

Future<void> _pumpUntilText(WidgetTester tester, String text) async {
  for (var attempt = 0; attempt < 60; attempt++) {
    await tester.pump(const Duration(milliseconds: 100));
    if (find.text(text).evaluate().isNotEmpty) return;
  }
  fail('Timed out waiting for "$text".');
}

class _NavigationTodayGateway implements ElderTodayGateway {
  const _NavigationTodayGateway();

  @override
  Future<ElderTodaySnapshot> loadToday(String elderId) async =>
      ElderTodaySnapshot(
        elderId: elderId,
        serverTime: DateTime.utc(2026, 8, 29, 8),
        isDemoData: true,
        checkIns: const [],
        reminders: const [],
      );

  @override
  Future<void> completeReminder(String reminderId, String requestId) async {}

  @override
  Future<void> snoozeReminder(
    String reminderId,
    String requestId,
    DateTime nextReminderAt,
  ) async {}
}

class _NavigationAiGateway implements AiGateway {
  const _NavigationAiGateway();

  @override
  Future<AiChatReply> chat(String input, String sessionId) =>
      throw StateError('not used');

  @override
  Future<AiDraft> confirmDraft(String draftId) => throw StateError('not used');

  @override
  Future<AiMemory> confirmMemory(String candidateId) =>
      throw StateError('not used');

  @override
  Future<void> deleteMemory(String memoryId) => throw StateError('not used');

  @override
  Future<List<AiMemory>> listMemories() async => const [];
}

class _NavigationFamilyStatusGateway implements FamilyStatusGateway {
  const _NavigationFamilyStatusGateway();

  @override
  Future<FamilyStatusSnapshot> load(String elderId) async =>
      FamilyStatusSnapshot(
        elderDisplayName: '李安康',
        grantedFields: const {ConsentField.recentStatus},
        consentExpiresAt: DateTime.utc(2027, 8, 29),
        recentStatus: '今天已完成平安确认',
        reminderSummary: null,
        careProgress: null,
        visitSummary: null,
        lastCommunityConfirmation: null,
      );
}

class _NavigationEventGateway implements FamilyEventQueryGateway {
  const _NavigationEventGateway();

  @override
  Future<List<FamilyEventSummary>> list(String elderId) async => const [];

  @override
  Future<FamilyEventSummary> get(String eventId) =>
      throw StateError('not used');
}

class _NavigationRecordsGateway implements FamilyCareRecordsGateway {
  const _NavigationRecordsGateway();

  @override
  Future<List<FamilyCareRecord>> load(String elderId) async => const [];
}
