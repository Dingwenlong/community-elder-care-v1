import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:integration_test/integration_test.dart';
import 'package:mobile/ai/ai_api_gateway.dart';
import 'package:mobile/app/community_care_app.dart';
import 'package:mobile/auth/session_controller.dart';
import 'package:mobile/core/api/contracts.dart';
import 'package:mobile/core/outbox/outbox_entry.dart';
import 'package:mobile/core/outbox/outbox_repository.dart';
import 'package:mobile/core/outbox/outbox_sync_service.dart';
import 'package:mobile/elder/home/elder_today_controller.dart';
import 'package:mobile/family/home/family_status_controller.dart';
import 'package:path/path.dart' as path;
import 'package:sqflite/sqflite.dart';
import 'package:uuid/uuid.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets(
    'elder check-in, offline help, danger guidance and family scope form one safe story',
    (tester) async {
      final databasePath = path.join(
        await getDatabasesPath(),
        'main-story-test-${const Uuid().v4()}.db',
      );
      final repository = OutboxRepository(databasePath: databasePath);
      final sender = MainStoryOutboxSender();
      addTearDown(() async {
        await repository.close();
        await deleteDatabase(databasePath);
      });

      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            initialSessionProvider.overrideWithValue(
              const SessionState(
                token: 'elder-main-story-token',
                role: DemoRole.elder,
                isDemoMode: true,
                elderId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
              ),
            ),
            elderTodayGatewayProvider.overrideWithValue(
              const MainStoryTodayGateway(),
            ),
            outboxRepositoryProvider.overrideWithValue(repository),
            outboxSenderProvider.overrideWithValue(sender),
            aiGatewayProvider.overrideWithValue(const OfflineAiGateway()),
          ],
          child: const CommunityCareApp(),
        ),
      );
      await pumpUntilText(tester, '我今天平安');

      await tester.tap(find.text('我今天平安'));
      await pumpUntilText(tester, '签到已送达');
      expect(
        sender.sent.where((entry) => entry.kind == OutboxKind.checkIn),
        hasLength(1),
      );

      sender.online = false;
      await tester.tap(find.text('我需要帮助'));
      await pumpUntilText(tester, '紧急情况');
      await tester.tap(find.text('紧急情况'));
      await pumpUntilText(tester, '确认发送');
      await tester.tap(find.text('确认发送'));
      await pumpUntilText(tester, '尚未送达');
      final pending = await repository.pending();
      expect(
        pending.where((entry) => entry.kind == OutboxKind.careEvent),
        hasLength(1),
      );
      final queuedRequestId = pending.single.requestId;

      sender.online = true;
      await tester.tap(find.text('重新发送'));
      await pumpUntilText(tester, '已送达');
      final deliveredEvents = sender.sent
          .where((entry) => entry.kind == OutboxKind.careEvent)
          .toList();
      expect(deliveredEvents, hasLength(1));
      expect(deliveredEvents.single.requestId, queuedRequestId);
      expect(await repository.pending(), isEmpty);

      final helpContext = tester.element(find.text('我需要帮助'));
      GoRouter.of(helpContext).go('/elder/chat');
      await pumpUntilText(tester, '陪伴问答');
      await tester.enterText(find.byType(EditableText), '我摔倒了，起不来');
      await tester.tap(find.text('发送'));
      await pumpUntilText(tester, '如果能够操作，请立即呼叫身边的人。');
      expect(find.textContaining('当前不会真实拨打 120'), findsOneWidget);
      expect(find.text('拨打 120'), findsNothing);

      await tester.pumpWidget(const SizedBox.shrink());
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            initialSessionProvider.overrideWithValue(
              const SessionState(
                token: 'family-main-story-token',
                role: DemoRole.family,
                isDemoMode: true,
                elderId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
              ),
            ),
            familyStatusGatewayProvider.overrideWithValue(
              const MainStoryFamilyGateway(),
            ),
          ],
          child: const CommunityCareApp(),
        ),
      );
      await pumpUntilText(tester, '已授权照料摘要');
      expect(find.text('最近状态'), findsOneWidget);
      expect(find.text('照料进展'), findsOneWidget);
      expect(find.text('探访摘要'), findsOneWidget);
      expect(find.text('AI 原始对话'), findsNothing);
      expect(find.text('社区内部备注'), findsNothing);
    },
  );
}

Future<void> pumpUntilText(WidgetTester tester, String text) async {
  for (var attempt = 0; attempt < 60; attempt++) {
    await tester.pump(const Duration(milliseconds: 100));
    if (find.text(text).evaluate().isNotEmpty) return;
  }
  fail('Timed out waiting for "$text".');
}

class MainStoryOutboxSender implements OutboxSender {
  bool online = true;
  final sent = <OutboxEntry>[];

  @override
  Future<void> send(OutboxEntry entry) async {
    if (!online) throw StateError('offline');
    sent.add(entry);
  }
}

class MainStoryTodayGateway implements ElderTodayGateway {
  const MainStoryTodayGateway();

  @override
  Future<ElderTodaySnapshot> loadToday(String elderId) async =>
      ElderTodaySnapshot(
        elderId: elderId,
        serverTime: DateTime.utc(2026, 8, 24, 8),
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

class OfflineAiGateway implements AiGateway {
  const OfflineAiGateway();

  @override
  Future<AiChatReply> chat(String input, String sessionId) =>
      throw StateError('offline');

  @override
  Future<AiDraft> confirmDraft(String draftId) => throw StateError('offline');

  @override
  Future<AiMemory> confirmMemory(String candidateId) =>
      throw StateError('offline');

  @override
  Future<void> deleteMemory(String memoryId) => throw StateError('offline');

  @override
  Future<List<AiMemory>> listMemories() async => const [];
}

class MainStoryFamilyGateway implements FamilyStatusGateway {
  const MainStoryFamilyGateway();

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
        careProgress: '社区正在处理演示照料事件',
        visitSummary: '社区人员已完成演示上门探访',
        lastCommunityConfirmation: '今天 08:12 社区已记录确认进展',
      );
}
