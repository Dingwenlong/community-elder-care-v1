import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../core/theme/app_theme.dart';
import '../core/widgets/app_navigation.dart';
import '../core/widgets/large_action_button.dart';

class ElderShell extends StatelessWidget {
  const ElderShell({super.key, required this.navigationShell});

  final StatefulNavigationShell navigationShell;

  static const _destinations = <AppNavigationDestination>[
    AppNavigationDestination(
      label: '首页',
      icon: LucideIcons.house,
      selectedIcon: LucideIcons.house,
    ),
    AppNavigationDestination(
      label: '提醒',
      icon: LucideIcons.bell,
      selectedIcon: LucideIcons.bell,
    ),
    AppNavigationDestination(
      label: '陪伴',
      icon: LucideIcons.messageCircle,
      selectedIcon: LucideIcons.messageCircle,
    ),
    AppNavigationDestination(
      label: '我的',
      icon: LucideIcons.userRound,
      selectedIcon: LucideIcons.userRound,
    ),
  ];

  void _select(int index) {
    navigationShell.goBranch(
      index,
      initialLocation: index == navigationShell.currentIndex,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 560),
          child: navigationShell,
        ),
      ),
      bottomNavigationBar: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Material(
            color: AppColors.surface,
            child: Container(
              width: double.infinity,
              decoration: const BoxDecoration(
                border: Border(top: BorderSide(color: AppColors.line)),
              ),
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.lg,
                AppSpacing.md,
                AppSpacing.lg,
                AppSpacing.sm,
              ),
              child: Center(
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 528),
                  child: LargeActionButton(
                    label: '我需要帮助',
                    semanticLabel: '打开求助类别',
                    icon: LucideIcons.circleHelp,
                    danger: true,
                    onPressed: () => context.push('/elder/help'),
                  ),
                ),
              ),
            ),
          ),
          AppBottomNavigation(
            destinations: _destinations,
            selectedIndex: navigationShell.currentIndex,
            onSelected: _select,
            elder: true,
          ),
        ],
      ),
    );
  }
}
