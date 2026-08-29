import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/app/community_care_app.dart';
import 'package:mobile/auth/session_controller.dart';
import 'package:mobile/core/api/contracts.dart';
import 'package:mobile/elder/home/elder_today_controller.dart';
import 'package:mobile/elder/settings/elder_settings_page.dart';
import 'package:mobile/family/family_shell.dart';
import 'package:mobile/family/home/family_status_controller.dart';

void main() {
  testWidgets('elder phone supports 360x800 at selected 1.6 text scale', (
    tester,
  ) async {
    _configureView(tester, const Size(360, 800), systemTextScale: 1);
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          initialSessionProvider.overrideWithValue(_elderSession),
          elderFontScaleProvider.overrideWith((ref) => 1.6),
          elderTodayGatewayProvider.overrideWithValue(
            const _ResponsiveTodayGateway(),
          ),
        ],
        child: const CommunityCareApp(),
      ),
    );
    await _pumpUntilText(tester, '我今天平安');

    expect(_effectiveTextScale(tester, find.text('我今天平安')), 1.6);
    expect(find.text('我需要帮助'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('elder uses system 200% text scale on 412x915', (tester) async {
    _configureView(tester, const Size(412, 915), systemTextScale: 2);
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          initialSessionProvider.overrideWithValue(_elderSession),
          elderFontScaleProvider.overrideWith((ref) => 1.3),
          elderTodayGatewayProvider.overrideWithValue(
            const _ResponsiveTodayGateway(),
          ),
        ],
        child: const CommunityCareApp(),
      ),
    );
    await _pumpUntilText(tester, '我今天平安');

    expect(_effectiveTextScale(tester, find.text('我今天平安')), 2);
    expect(tester.takeException(), isNull);
  });

  testWidgets('family phone keeps bottom navigation at system 200%', (
    tester,
  ) async {
    _configureView(tester, const Size(412, 915), systemTextScale: 2);
    await _pumpFamily(tester);

    expect(find.byType(NavigationRail), findsNothing);
    expect(_effectiveTextScale(tester, find.text('家属照料进展')), 2);
    expect(tester.takeException(), isNull);
  });

  testWidgets('family tablet uses NavigationRail at 800x1280', (tester) async {
    _configureView(tester, const Size(800, 1280), systemTextScale: 2);
    await _pumpFamily(tester);

    expect(find.byType(FamilyShell), findsOneWidget);
    expect(find.byType(NavigationRail), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('reduced motion disables navigation indicator animation', (
    tester,
  ) async {
    _configureView(
      tester,
      const Size(360, 800),
      systemTextScale: 1,
      disableAnimations: true,
    );
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          initialSessionProvider.overrideWithValue(_elderSession),
          elderTodayGatewayProvider.overrideWithValue(
            const _ResponsiveTodayGateway(),
          ),
        ],
        child: const CommunityCareApp(),
      ),
    );
    await _pumpUntilText(tester, '我今天平安');

    final indicators = tester.widgetList<AnimatedContainer>(
      find.byType(AnimatedContainer),
    );
    expect(indicators, isNotEmpty);
    expect(indicators.every((item) => item.duration == Duration.zero), isTrue);
    expect(tester.takeException(), isNull);
  });
}

const _elderSession = SessionState(
  token: 'elder-responsive-token',
  role: DemoRole.elder,
  isDemoMode: true,
  elderId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
);

const _familySession = SessionState(
  token: 'family-responsive-token',
  role: DemoRole.family,
  isDemoMode: true,
  elderId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
);

void _configureView(
  WidgetTester tester,
  Size size, {
  required double systemTextScale,
  bool disableAnimations = false,
}) {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = size;
  tester.platformDispatcher.textScaleFactorTestValue = systemTextScale;
  tester.platformDispatcher.accessibilityFeaturesTestValue =
      FakeAccessibilityFeatures(disableAnimations: disableAnimations);
  addTearDown(tester.view.resetDevicePixelRatio);
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.platformDispatcher.clearTextScaleFactorTestValue);
  addTearDown(tester.platformDispatcher.clearAccessibilityFeaturesTestValue);
}

Future<void> _pumpFamily(WidgetTester tester) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        initialSessionProvider.overrideWithValue(_familySession),
        familyStatusGatewayProvider.overrideWithValue(
          const _ResponsiveFamilyGateway(),
        ),
      ],
      child: const CommunityCareApp(),
    ),
  );
  await _pumpUntilText(tester, '家属首页');
}

double _effectiveTextScale(WidgetTester tester, Finder finder) {
  final context = tester.element(finder);
  return MediaQuery.textScalerOf(context).scale(16) / 16;
}

Future<void> _pumpUntilText(WidgetTester tester, String text) async {
  for (var attempt = 0; attempt < 60; attempt++) {
    await tester.pump(const Duration(milliseconds: 100));
    if (find.text(text).evaluate().isNotEmpty) return;
  }
  fail('Timed out waiting for "$text".');
}

class _ResponsiveTodayGateway implements ElderTodayGateway {
  const _ResponsiveTodayGateway();

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

class _ResponsiveFamilyGateway implements FamilyStatusGateway {
  const _ResponsiveFamilyGateway();

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
