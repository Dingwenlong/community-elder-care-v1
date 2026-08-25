import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:mobile/app/community_care_app.dart';
import 'package:mobile/auth/session_controller.dart';
import 'package:mobile/core/api/contracts.dart';
import 'package:mobile/core/outbox/outbox_entry.dart';
import 'package:mobile/core/outbox/outbox_repository.dart';
import 'package:mobile/core/outbox/outbox_sync_service.dart';
import 'package:mobile/elder/home/elder_today_controller.dart';
import 'package:path/path.dart' as path;
import 'package:sqflite/sqflite.dart';
import 'package:uuid/uuid.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('offline emergency gives guidance and retries the same request', (
    tester,
  ) async {
    final databasePath = path.join(
      await getDatabasesPath(),
      'elder-help-test-${const Uuid().v4()}.db',
    );
    final repository = OutboxRepository(databasePath: databasePath);
    final sender = ReconnectableOutboxSender()..online = false;
    addTearDown(() async {
      await repository.close();
      await deleteDatabase(databasePath);
    });

    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          initialSessionProvider.overrideWithValue(
            const SessionState(
              token: 'integration-token',
              role: DemoRole.elder,
              isDemoMode: true,
              elderId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
            ),
          ),
          elderTodayGatewayProvider.overrideWithValue(
            const EmptyTodayGateway(),
          ),
          outboxRepositoryProvider.overrideWithValue(repository),
          outboxSenderProvider.overrideWithValue(sender),
        ],
        child: const CommunityCareApp(),
      ),
    );
    await pumpUntilText(tester, '我需要帮助');

    await tester.tap(find.text('我需要帮助'));
    await pumpUntilText(tester, '紧急情况');

    expect(find.text('身体不适'), findsOneWidget);
    expect(find.text('生活服务'), findsOneWidget);
    expect(find.text('想找人说说话'), findsOneWidget);

    await tester.tap(find.text('紧急情况'));
    await pumpUntilText(tester, '确认发送紧急求助');
    expect(sender.attemptedRequestIds, isEmpty);
    expect(find.text('如果能够操作，请立即呼叫身边的人。'), findsOneWidget);
    expect(find.text('系统正在把求助发送给社区；当前不会真实拨打 120。'), findsOneWidget);

    await tester.tap(find.text('确认发送'));
    await pumpUntilText(tester, '尚未送达');
    final queued = (await repository.pending()).single;
    expect(queued.priority, OutboxPriority.high);
    expect(sender.attemptedRequestIds, [queued.requestId]);

    sender.online = true;
    await tester.tap(find.text('重新发送'));
    await pumpUntilText(tester, '已送达');

    expect(sender.sentRequestIds, [queued.requestId]);
    expect(sender.attemptedRequestIds, [queued.requestId, queued.requestId]);
    expect(await repository.pending(), isEmpty);

    await tester.pumpWidget(const SizedBox.shrink());
  });
}

Future<void> pumpUntilText(WidgetTester tester, String text) async {
  for (var attempt = 0; attempt < 50; attempt++) {
    await tester.pump(const Duration(milliseconds: 100));
    if (find.text(text).evaluate().isNotEmpty) return;
  }
  fail('Timed out waiting for "$text".');
}

class EmptyTodayGateway implements ElderTodayGateway {
  const EmptyTodayGateway();

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

class ReconnectableOutboxSender implements OutboxSender {
  bool online = false;
  final attemptedRequestIds = <String>[];
  final sentRequestIds = <String>[];

  @override
  Future<void> send(OutboxEntry entry) async {
    attemptedRequestIds.add(entry.requestId);
    if (!online) throw StateError('offline');
    sentRequestIds.add(entry.requestId);
  }
}
