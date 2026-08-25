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
