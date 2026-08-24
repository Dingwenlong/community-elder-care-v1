import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../auth/login_page.dart';
import '../auth/session_controller.dart';
import '../core/api/contracts.dart';
import '../elder/home/elder_home_page.dart';
import '../elder/help/help_category_page.dart';
import '../elder/chat/elder_chat_page.dart';
import '../elder/reminders/reminder_page.dart';
import '../elder/settings/elder_settings_page.dart';
import '../family/family_shell.dart';

final appRouterProvider = Provider<GoRouter>((ref) {
  final session = ref.watch(sessionControllerProvider);
  final router = GoRouter(
    initialLocation: _homeFor(session),
    redirect: (context, state) {
      final location = state.uri.path;
      if (session == null) return location == '/login' ? null : '/login';
      return switch (session.role) {
        DemoRole.elder => location.startsWith('/elder/') ? null : '/elder/home',
        DemoRole.family =>
          location.startsWith('/family/') ? null : '/family/home',
        DemoRole.communityStaff ||
        DemoRole.serviceWorker ||
        DemoRole.administrator =>
          location == '/use-community-web' ? null : '/use-community-web',
      };
    },
    routes: [
      GoRoute(path: '/login', builder: (context, state) => const LoginPage()),
      GoRoute(
        path: '/elder/home',
        builder: (context, state) => const ElderHomePage(),
      ),
      GoRoute(
        path: '/elder/reminders',
        builder: (context, state) => const ReminderPage(),
      ),
      GoRoute(
        path: '/elder/help',
        builder: (context, state) => const HelpCategoryPage(),
      ),
      GoRoute(
        path: '/elder/chat',
        builder: (context, state) => const ElderChatPage(),
      ),
      GoRoute(
        path: '/family/home',
        builder: (context, state) => const FamilyShell(),
      ),
      GoRoute(
        path: '/elder/settings',
        builder: (context, state) => const ElderSettingsPage(),
      ),
      GoRoute(
        path: '/family/settings',
        builder: (context, state) => const DemoSettingsPage(),
      ),
      GoRoute(
        path: '/use-community-web',
        builder: (context, state) => const CommunityWebRequiredPage(),
      ),
    ],
  );
  ref.onDispose(router.dispose);
  return router;
});

String _homeFor(SessionState? session) => switch (session?.role) {
  DemoRole.elder => '/elder/home',
  DemoRole.family => '/family/home',
  DemoRole.communityStaff ||
  DemoRole.serviceWorker ||
  DemoRole.administrator => '/use-community-web',
  null => '/login',
};

class DemoSettingsPage extends ConsumerWidget {
  const DemoSettingsPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Scaffold(
      appBar: AppBar(title: const Text('演示设置')),
      body: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          const DecoratedBox(
            decoration: BoxDecoration(
              color: Color(0xFFE8F1FB),
              border: Border.fromBorderSide(
                BorderSide(color: Color(0xFF7AA7D8)),
              ),
            ),
            child: Padding(
              padding: EdgeInsets.all(12),
              child: Text(
                '演示模式',
                style: TextStyle(fontWeight: FontWeight.w700),
              ),
            ),
          ),
          const SizedBox(height: 20),
          FilledButton(
            onPressed: () => ref
                .read(sessionControllerProvider.notifier)
                .switchDemoAccount(),
            child: const Text('切换演示账号'),
          ),
        ],
      ),
    );
  }
}

class CommunityWebRequiredPage extends ConsumerWidget {
  const CommunityWebRequiredPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Text(
                  '请使用社区管理端',
                  style: TextStyle(fontSize: 28, fontWeight: FontWeight.w700),
                ),
                const SizedBox(height: 12),
                const Text('当前角色不进入老人或家属 App。'),
                const SizedBox(height: 24),
                OutlinedButton(
                  onPressed: () =>
                      ref.read(sessionControllerProvider.notifier).logout(),
                  child: const Text('返回登录'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
