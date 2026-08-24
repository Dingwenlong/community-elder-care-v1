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

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets(
    'danger fallback, draft confirmation and memory deletion are safe',
    (tester) async {
      final gateway = FakeAiGateway();
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            initialSessionProvider.overrideWithValue(
              const SessionState(
                token: 'elder-token',
                role: DemoRole.elder,
                isDemoMode: true,
                elderId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
              ),
            ),
            elderTodayGatewayProvider.overrideWithValue(const AiTodayGateway()),
            aiGatewayProvider.overrideWithValue(gateway),
          ],
          child: const CommunityCareApp(),
        ),
      );
      await pumpUntilText(tester, '老人首页');
      final homeContext = tester.element(find.text('老人首页'));
      GoRouter.of(homeContext).go('/elder/chat');
      await pumpUntilText(tester, '陪伴问答');

      await tester.enterText(find.byType(EditableText), '我摔倒了，起不来');
      await tester.tap(find.text('发送'));
      await pumpUntilText(tester, '如果能够操作，请立即呼叫身边的人。');
      expect(gateway.chatInputs, ['我摔倒了，起不来']);

      gateway.offline = false;
      await tester.enterText(find.byType(EditableText), '帮我申请代购生活用品');
      await tester.tap(find.text('发送'));
      await pumpUntilText(tester, 'AI 草稿');
      expect(gateway.confirmedDraftIds, isEmpty);
      await tester.tap(find.text('确认提交'));
      await pumpUntilText(tester, '服务请求已确认提交');
      expect(gateway.confirmedDraftIds, ['draft-1']);

      expect(gateway.confirmedMemoryIds, isEmpty);
      await tester.tap(find.text('确认记忆'));
      await pumpUntilText(tester, '记忆已确认');
      expect(gateway.confirmedMemoryIds, ['memory-1']);

      final chatContext = tester.element(find.text('陪伴问答'));
      GoRouter.of(chatContext).go('/elder/settings');
      await pumpUntilText(tester, '喜欢参加社区书法活动');
      await tester.tap(find.text('删除记忆'));
      await pumpUntilMissing(tester, '喜欢参加社区书法活动');
      expect(gateway.deletedMemoryIds, ['memory-1']);
    },
  );
}

Future<void> pumpUntilText(WidgetTester tester, String text) async {
  for (var attempt = 0; attempt < 50; attempt++) {
    await tester.pump(const Duration(milliseconds: 100));
    if (find.text(text).evaluate().isNotEmpty) return;
  }
  fail('Timed out waiting for "$text".');
}

Future<void> pumpUntilMissing(WidgetTester tester, String text) async {
  for (var attempt = 0; attempt < 50; attempt++) {
    await tester.pump(const Duration(milliseconds: 100));
    if (find.text(text).evaluate().isEmpty) return;
  }
  fail('Timed out waiting for "$text" to disappear.');
}

class AiTodayGateway implements ElderTodayGateway {
  const AiTodayGateway();

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

class FakeAiGateway implements AiGateway {
  bool offline = true;
  final chatInputs = <String>[];
  final confirmedDraftIds = <String>[];
  final confirmedMemoryIds = <String>[];
  final deletedMemoryIds = <String>[];
  final memories = <AiMemory>[];

  @override
  Future<AiChatReply> chat(String input, String sessionId) async {
    chatInputs.add(input);
    if (offline) throw StateError('offline');
    return const AiChatReply(
      reply: '已生成服务请求草稿，请确认后提交。',
      usedFallback: false,
      dangerCode: 'NONE',
      isEmergency: false,
      serviceRequestDraft: AiDraft(
        id: 'draft-1',
        generatedText: '希望社区协助代购生活用品',
        status: 'Pending',
      ),
      memoryCandidate: AiMemory(
        id: 'memory-1',
        generatedText: '喜欢参加社区书法活动',
        isConfirmed: false,
      ),
    );
  }

  @override
  Future<AiDraft> confirmDraft(String draftId) async {
    confirmedDraftIds.add(draftId);
    return const AiDraft(
      id: 'draft-1',
      generatedText: '希望社区协助代购生活用品',
      status: 'Confirmed',
    );
  }

  @override
  Future<AiMemory> confirmMemory(String candidateId) async {
    confirmedMemoryIds.add(candidateId);
    const memory = AiMemory(
      id: 'memory-1',
      generatedText: '喜欢参加社区书法活动',
      isConfirmed: true,
    );
    memories.add(memory);
    return memory;
  }

  @override
  Future<List<AiMemory>> listMemories() async => List.of(memories);

  @override
  Future<void> deleteMemory(String memoryId) async {
    deletedMemoryIds.add(memoryId);
    memories.removeWhere((memory) => memory.id == memoryId);
  }
}
