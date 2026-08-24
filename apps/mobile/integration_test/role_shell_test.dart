import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:integration_test/integration_test.dart';
import 'package:mobile/app/community_care_app.dart';
import 'package:mobile/auth/session_controller.dart';
import 'package:mobile/core/api/contracts.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('family session cannot navigate to elder routes', (tester) async {
    await launchWithSession(tester, role: DemoRole.family);

    expect(find.text('家属首页'), findsOneWidget);
    expect(find.text('我今天平安'), findsNothing);
    await expectRouteDenied(tester, '/elder/home');
    expect(find.text('家属首页'), findsOneWidget);
  });

  testWidgets('community staff is directed to the community Web workspace', (
    tester,
  ) async {
    await launchWithSession(tester, role: DemoRole.communityStaff);

    expect(find.text('请使用社区管理端'), findsOneWidget);
    expect(find.text('我今天平安'), findsNothing);
    expect(find.text('家属首页'), findsNothing);
  });
}

Future<void> launchWithSession(
  WidgetTester tester, {
  required DemoRole role,
}) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        initialSessionProvider.overrideWithValue(
          SessionState(
            token: 'integration-token',
            role: role,
            isDemoMode: true,
          ),
        ),
      ],
      child: const CommunityCareApp(),
    ),
  );
  await tester.pumpAndSettle();
}

Future<void> expectRouteDenied(WidgetTester tester, String location) async {
  final context = tester.element(find.text('家属首页'));
  GoRouter.of(context).go(location);
  await tester.pumpAndSettle();
}
