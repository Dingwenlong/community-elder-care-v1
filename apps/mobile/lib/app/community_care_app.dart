import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'app_router.dart';
import '../auth/session_controller.dart';
import '../core/api/contracts.dart';
import '../core/theme/app_theme.dart';
import '../elder/settings/elder_settings_page.dart';

class CommunityCareApp extends ConsumerWidget {
  const CommunityCareApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final router = ref.watch(appRouterProvider);
    final fontScale = ref.watch(elderFontScaleProvider);
    final session = ref.watch(sessionControllerProvider);
    return MaterialApp.router(
      title: '安邻照料｜社区独居老人照料协同系统',
      debugShowCheckedModeBanner: false,
      routerConfig: router,
      builder: (context, child) {
        final mediaQuery = MediaQuery.of(context);
        if (session?.role != DemoRole.elder) {
          return child!;
        }
        final systemScale = mediaQuery.textScaler.scale(16) / 16;
        final effectiveScale = math.max(systemScale, fontScale);
        return MediaQuery(
          data: mediaQuery.copyWith(
            textScaler: TextScaler.linear(effectiveScale),
          ),
          child: child!,
        );
      },
      theme: buildAppTheme(),
    );
  }
}
