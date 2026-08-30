import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:mobile/main.dart' as app;

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('shows a normal elder and family login entry', (tester) async {
    app.main();
    await tester.pumpAndSettle();

    expect(find.text('安邻照料'), findsOneWidget);
    expect(find.text('社区独居老人照料协同系统'), findsOneWidget);
    expect(find.text('账号'), findsOneWidget);
    expect(find.text('密码'), findsOneWidget);
    expect(find.text('登录'), findsOneWidget);
    expect(find.textContaining('演示'), findsNothing);
  });
}
