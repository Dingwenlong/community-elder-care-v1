import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

class FamilyShell extends StatelessWidget {
  const FamilyShell({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('家属首页'),
        actions: [
          TextButton(
            onPressed: () => context.go('/family/settings'),
            child: const Text('设置'),
          ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(20),
        children: const [
          DecoratedBox(
            decoration: BoxDecoration(
              color: Color(0xFFE8F1FB),
              border: Border.fromBorderSide(
                BorderSide(color: Color(0xFF7AA7D8)),
              ),
            ),
            child: Padding(
              padding: EdgeInsets.all(12),
              child: Text(
                '授权范围内信息',
                style: TextStyle(fontWeight: FontWeight.w700),
              ),
            ),
          ),
          SizedBox(height: 24),
          Text(
            '已授权照料摘要',
            style: TextStyle(fontSize: 28, fontWeight: FontWeight.w700),
          ),
          SizedBox(height: 8),
          Text('这里只显示老人已授权的照料摘要。'),
        ],
      ),
    );
  }
}
