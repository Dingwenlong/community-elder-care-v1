import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'app_router.dart';
import '../core/theme/app_theme.dart';
import '../elder/settings/elder_settings_page.dart';

class CommunityCareApp extends ConsumerWidget {
  const CommunityCareApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final router = ref.watch(appRouterProvider);
    final fontScale = ref.watch(elderFontScaleProvider);
    return MaterialApp.router(
      title: '社区独居老人照料系统',
      debugShowCheckedModeBanner: false,
      routerConfig: router,
      builder: (context, child) {
        final mediaQuery = MediaQuery.of(context);
        return MediaQuery(
          data: mediaQuery.copyWith(textScaler: TextScaler.linear(fontScale)),
          child: child!,
        );
      },
      theme: buildAppTheme(),
    );
  }
}
