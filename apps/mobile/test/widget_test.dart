import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/main.dart';

void main() {
  testWidgets('shows the elder-care demo identity', (tester) async {
    await tester.pumpWidget(const CommunityCareApp());

    expect(find.text('社区独居老人照料系统'), findsOneWidget);
    expect(find.text('演示数据'), findsOneWidget);
  });
}
