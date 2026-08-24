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

  testWidgets('elder can confirm safety with one primary action', (
    tester,
  ) async {
    final databasePath = path.join(
      await getDatabasesPath(),
      'check-in-test-${const Uuid().v4()}.db',
    );
    final repository = OutboxRepository(databasePath: databasePath);
    final sender = RecordingOutboxSender();
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
            const SeededTodayGateway(),
          ),
          outboxRepositoryProvider.overrideWithValue(repository),
          outboxSenderProvider.overrideWithValue(sender),
        ],
        child: const CommunityCareApp(),
      ),
    );
    await pumpUntilText(tester, '我今天平安');

    await tester.tap(find.text('我今天平安'));
    await pumpUntilText(tester, '签到已送达');

    expect(find.text('今天已签到'), findsOneWidget);
    expect(sender.sent, hasLength(1));
    expect(sender.sent.single.kind, OutboxKind.checkIn);
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

class SeededTodayGateway implements ElderTodayGateway {
  const SeededTodayGateway();

  @override
  Future<ElderTodaySnapshot> loadToday(String elderId) async {
    return ElderTodaySnapshot(
      elderId: elderId,
      serverTime: DateTime.utc(2026, 8, 24, 8),
      isDemoData: true,
      checkIns: const [],
      reminders: const [
        TodayReminder(
          id: '44444444-4444-4444-4444-444444444401',
          label: '按既有医嘱查看今日服药提醒',
          state: 'Pending',
        ),
      ],
    );
  }

  @override
  Future<void> completeReminder(String reminderId, String requestId) async {}

  @override
  Future<void> snoozeReminder(
    String reminderId,
    String requestId,
    DateTime nextReminderAt,
  ) async {}
}

class RecordingOutboxSender implements OutboxSender {
  final sent = <OutboxEntry>[];

  @override
  Future<void> send(OutboxEntry entry) async => sent.add(entry);
}
