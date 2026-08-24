import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:mobile/main.dart' as app;

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('shows the elder and family demo login entry', (tester) async {
    app.main();
    await tester.pumpAndSettle();

    expect(find.text('社区独居老人照料系统'), findsOneWidget);
    expect(find.text('演示账号'), findsOneWidget);
    expect(find.text('登录演示 App'), findsOneWidget);
  });
}
