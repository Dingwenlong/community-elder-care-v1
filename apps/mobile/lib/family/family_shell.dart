import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../core/widgets/app_navigation.dart';

class FamilyShell extends StatelessWidget {
  const FamilyShell({super.key, required this.navigationShell});

  final StatefulNavigationShell navigationShell;

  static const _destinations = <AppNavigationDestination>[
    AppNavigationDestination(
      label: '最近状态',
      icon: LucideIcons.house,
      selectedIcon: LucideIcons.house,
    ),
    AppNavigationDestination(
      label: '事件',
      icon: LucideIcons.clipboardList,
      selectedIcon: LucideIcons.clipboardList,
    ),
    AppNavigationDestination(
      label: '照料记录',
      icon: LucideIcons.fileText,
      selectedIcon: LucideIcons.fileText,
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
    return LayoutBuilder(
      builder: (context, constraints) {
        if (constraints.maxWidth >= 600) {
          return Scaffold(
            body: SafeArea(
              child: Row(
                children: [
                  AppNavigationRail(
                    destinations: _destinations,
                    selectedIndex: navigationShell.currentIndex,
                    onSelected: _select,
                  ),
                  const VerticalDivider(width: 1),
                  Expanded(
                    child: Center(
                      child: ConstrainedBox(
                        constraints: const BoxConstraints(maxWidth: 920),
                        child: navigationShell,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          );
        }
        return Scaffold(
          body: navigationShell,
          bottomNavigationBar: AppBottomNavigation(
            destinations: _destinations,
            selectedIndex: navigationShell.currentIndex,
            onSelected: _select,
          ),
        );
      },
    );
  }
}
