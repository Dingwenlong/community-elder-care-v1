import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:integration_test/integration_test.dart';
import 'package:mobile/app/community_care_app.dart';
import 'package:mobile/auth/session_controller.dart';
import 'package:mobile/core/api/contracts.dart';
import 'package:mobile/elder/home/elder_today_controller.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('elder primary actions are labelled and at least 44dp', (
    tester,
  ) async {
    final semantics = tester.ensureSemantics();

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
            const AccessibilityTodayGateway(),
          ),
        ],
        child: const CommunityCareApp(),
      ),
    );
    await pumpUntilText(tester, '我今天平安');

    expect(find.bySemanticsLabel(RegExp('确认我今天平安')), findsOneWidget);
    expect(find.bySemanticsLabel(RegExp('打开求助类别')), findsOneWidget);
    expectControlsAtLeast44Dp(tester);

    final homeContext = tester.element(find.text('老人首页'));
    GoRouter.of(homeContext).go('/elder/chat');
    await pumpUntilText(tester, 'AI 当前不可用，核心求助功能仍可使用');
    expect(find.text('朗读回复'), findsOneWidget);
    expectControlsAtLeast44Dp(tester);

    final chatContext = tester.element(find.text('陪伴问答'));
    GoRouter.of(chatContext).go('/elder/settings');
    await pumpUntilText(tester, '授权摘要');
    expect(find.text('应急联系人（演示）'), findsOneWidget);
    expect(find.text('AI 记忆'), findsOneWidget);
    expect(find.text('字体大小'), findsOneWidget);
    expect(find.text('文字转语音'), findsOneWidget);
    expectControlsAtLeast44Dp(tester);
    semantics.dispose();
  });
}

void expectControlsAtLeast44Dp(WidgetTester tester) {
  final controls = find.byWidgetPredicate(
    (widget) =>
        widget is ButtonStyleButton ||
        widget is IconButton ||
        widget is SwitchListTile ||
        widget is SegmentedButton,
  );
  expect(controls, findsWidgets);
  for (final element in controls.evaluate()) {
    final renderObject = element.renderObject;
    if (renderObject is! RenderBox || !renderObject.hasSize) continue;
    expect(
      renderObject.size.shortestSide,
      greaterThanOrEqualTo(44),
      reason: '${element.widget.runtimeType} is smaller than 44dp',
    );
  }
}

Future<void> pumpUntilText(WidgetTester tester, String text) async {
  for (var attempt = 0; attempt < 50; attempt++) {
    await tester.pump(const Duration(milliseconds: 100));
    if (find.text(text).evaluate().isNotEmpty) return;
  }
  fail('Timed out waiting for "$text".');
}

class AccessibilityTodayGateway implements ElderTodayGateway {
  const AccessibilityTodayGateway();

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
