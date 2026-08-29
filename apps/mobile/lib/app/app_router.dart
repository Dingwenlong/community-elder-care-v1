import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../auth/login_page.dart';
import '../auth/session_controller.dart';
import '../core/api/contracts.dart';
import '../core/theme/app_theme.dart';
import '../core/widgets/app_page.dart';
import '../elder/home/elder_home_page.dart';
import '../elder/help/help_category_page.dart';
import '../elder/chat/elder_chat_page.dart';
import '../elder/reminders/reminder_page.dart';
import '../elder/settings/elder_settings_page.dart';
import '../elder/elder_shell.dart';
import '../family/events/family_event_detail_page.dart';
import '../family/events/family_event_list_page.dart';
import '../family/home/family_home_page.dart';
import '../family/records/family_care_records_page.dart';
import '../family/settings/family_settings_page.dart';
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
      StatefulShellRoute.indexedStack(
        builder: (context, state, navigationShell) =>
            ElderShell(navigationShell: navigationShell),
        branches: [
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/elder/home',
                builder: (context, state) => const ElderHomePage(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/elder/reminders',
                builder: (context, state) => const ReminderPage(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/elder/chat',
                builder: (context, state) => const ElderChatPage(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/elder/settings',
                builder: (context, state) => const ElderSettingsPage(),
              ),
            ],
          ),
        ],
      ),
      GoRoute(
        path: '/elder/help',
        builder: (context, state) => const HelpCategoryPage(),
      ),
      StatefulShellRoute.indexedStack(
        builder: (context, state, navigationShell) =>
            FamilyShell(navigationShell: navigationShell),
        branches: [
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/family/home',
                builder: (context, state) => const FamilyHomePage(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/family/events',
                builder: (context, state) => const FamilyEventListPage(),
                routes: [
                  GoRoute(
                    path: ':eventId',
                    builder: (context, state) => FamilyEventDetailPage(
                      eventId: state.pathParameters['eventId']!,
                    ),
                  ),
                ],
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/family/records',
                builder: (context, state) => const FamilyCareRecordsPage(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/family/settings',
                builder: (context, state) => const FamilySettingsPage(),
              ),
            ],
          ),
        ],
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

class CommunityWebRequiredPage extends ConsumerWidget {
  const CommunityWebRequiredPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(AppSpacing.xxl),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 520),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Container(
                    height: 132,
                    alignment: Alignment.centerLeft,
                    color: AppColors.navy,
                    padding: const EdgeInsets.all(AppSpacing.xxl),
                    child: const Icon(
                      LucideIcons.building2,
                      size: 58,
                      color: AppColors.surface,
                    ),
                  ),
                  const SizedBox(height: AppSpacing.xxl),
                  const AppPageHeader(
                    eyebrow: '工作人员入口',
                    title: '请使用社区管理端',
                    subtitle: '当前角色不进入老人或家属 App。',
                  ),
                  const SizedBox(height: AppSpacing.lg),
                  const AppInlineNotice(
                    message: '社区工作人员、服务人员和管理员请在电脑浏览器中继续操作。',
                    icon: LucideIcons.monitor,
                    tone: AppNoticeTone.info,
                  ),
                  const SizedBox(height: AppSpacing.xxl),
                  OutlinedButton.icon(
                    onPressed: () =>
                        ref.read(sessionControllerProvider.notifier).logout(),
                    icon: const Icon(LucideIcons.arrowLeft),
                    label: const Text('返回登录'),
                    style: OutlinedButton.styleFrom(
                      minimumSize: const Size.fromHeight(54),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
