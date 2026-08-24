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
import 'package:sqflite/sqflite.dart';
import 'package:path/path.dart' as path;
import 'package:uuid/uuid.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('offline emergency remains queued and reconnect sends it once', (
    tester,
  ) async {
    final databasePath = path.join(
      await getDatabasesPath(),
      'outbox-test-${const Uuid().v4()}.db',
    );
    final repository = OutboxRepository(databasePath: databasePath);
    final sender = FakeOutboxSender()..online = false;
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
            ),
          ),
          outboxRepositoryProvider.overrideWithValue(repository),
          outboxSenderProvider.overrideWithValue(sender),
        ],
        child: const CommunityCareApp(),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.text('我需要帮助'));
    await pumpUntilText(tester, '尚未送达');

    final container = ProviderScope.containerOf(
      tester.element(find.text('我需要帮助')),
    );
    expect(
      container.read(emergencyOutboxControllerProvider).status,
      EmergencyDeliveryStatus.unsent,
    );
    expect(find.text('尚未送达'), findsOneWidget);
    final pending = await repository.pending();
    final queued = expectSingle(pending);
    expect(queued.priority, OutboxPriority.high);
    expect(
      queued.requestId,
      matches(
        RegExp(
          r'^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$',
        ),
      ),
    );

    sender.online = true;
    await tester.tap(find.text('重新发送'));
    await pumpUntilText(tester, '已送达');

    expect(find.text('已送达'), findsOneWidget);
    expect(sender.sentRequestIds, [queued.requestId]);
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

OutboxEntry expectSingle(List<OutboxEntry> entries) {
  expect(entries, hasLength(1));
  return entries.single;
}

class FakeOutboxSender implements OutboxSender {
  bool online = false;
  final sentRequestIds = <String>[];

  @override
  Future<void> send(OutboxEntry entry) async {
    if (!online) throw StateError('offline');
    sentRequestIds.add(entry.requestId);
  }
}
