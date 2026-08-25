import 'package:flutter/material.dart';

/// 设计令牌 —— 数值以 docs/ui/design-tokens.json 为单一事实来源。
/// 改任何数值前先改 design-tokens.json，再同步本文件与 admin-web tokens.css。
/// 页面与组件内禁止硬编码色值/间距，一律引用本文件常量。
/// 品牌色、语义色与中性色。
abstract final class AppColors {
  // 品牌色
  static const Color primary = Color(0xFF0969DA);
  static const Color primaryHover = Color(0xFF0758B8);
  static const Color primarySoft = Color(0xFFE8F1FC);
  static const Color navy = Color(0xFF0B315D);
  static const Color navyDeep = Color(0xFF082747);

  // 点缀色（暖橙）
  static const Color accentWarm = Color(0xFFFFB000);
  static const Color accentWarmStrong = Color(0xFF8A5A00);
  static const Color accentWarmSoft = Color(0xFFFFF5E6);

  // 语义色
  static const Color danger = Color(0xFFC92A2A);
  static const Color dangerSoft = Color(0xFFFFF0F0);
  static const Color warning = Color(0xFFA15C00);
  static const Color warningSoft = Color(0xFFFFF5E6);
  static const Color success = Color(0xFF216E4E);
  static const Color successSoft = Color(0xFFE9F7F0);

  // 中性色
  static const Color inkStrong = Color(0xFF10253F);
  static const Color ink = Color(0xFF263D57);
  static const Color inkMuted = Color(0xFF61738A);
  static const Color paper = Color(0xFFF5F7FA);
  static const Color surface = Color(0xFFFFFFFF);
  static const Color surfaceMuted = Color(0xFFEEF2F6);
  static const Color surfaceZebra = Color(0xFFFAFBFC);
  static const Color line = Color(0xFFD7DEE7);
  static const Color lineStrong = Color(0xFFB9C5D2);

  // 遮罩
  static const Color modalBackdrop = Color(0x73082747); // rgba(8,39,71,.45)
}

/// 事件等级配色，与管理端 EventLevelBadge 语义一致。
enum AppEventLevel { l1, l2, l3, closed }

extension AppEventLevelColors on AppEventLevel {
  Color get fg => switch (this) {
        AppEventLevel.l1 => AppColors.danger,
        AppEventLevel.l2 => AppColors.warning,
        AppEventLevel.l3 => AppColors.navy,
        AppEventLevel.closed => AppColors.success,
      };

  Color get bg => switch (this) {
        AppEventLevel.l1 => AppColors.dangerSoft,
        AppEventLevel.l2 => AppColors.warningSoft,
        AppEventLevel.l3 => AppColors.primarySoft,
        AppEventLevel.closed => AppColors.successSoft,
      };
}

/// 字体族。Android 实际命中系统思源黑体，fallback 链与令牌保持一致。
abstract final class AppFonts {
  static const List<String> displayFallback = <String>[
    'Microsoft YaHei UI',
    'Noto Sans SC',
    'PingFang SC',
  ];
  static const List<String> numericFallback = <String>[
    'Bahnschrift',
    'DIN Alternate',
  ];
}

/// 字号阶梯（客户端基准：老人端 1.0x 档；家属端较小档位直接取用
/// `Theme.of(context).textTheme`，此处定义常用文本样式）。
abstract final class AppTextStyles {
  static const TextStyle display = TextStyle(
    fontSize: 32,
    fontWeight: FontWeight.w700,
    height: 1.3,
    color: AppColors.inkStrong,
  );
  static const TextStyle title = TextStyle(
    fontSize: 24,
    fontWeight: FontWeight.w700,
    height: 1.3,
    color: AppColors.inkStrong,
  );
  static const TextStyle body = TextStyle(
    fontSize: 20,
    fontWeight: FontWeight.w400,
    height: 1.5,
    color: AppColors.ink,
  );
  static const TextStyle secondary = TextStyle(
    fontSize: 18,
    fontWeight: FontWeight.w400,
    height: 1.5,
    color: AppColors.inkMuted,
  );
  static const TextStyle action = TextStyle(
    fontSize: 24,
    fontWeight: FontWeight.w700,
    height: 1.3,
    color: AppColors.surface,
  );

  /// 数字专用（时钟、统计），等宽数字防跳动。
  static const TextStyle numeric = TextStyle(
    fontSize: 32,
    fontWeight: FontWeight.w700,
    height: 1.3,
    color: AppColors.inkStrong,
    fontFamilyFallback: AppFonts.numericFallback,
    fontFeatures: <FontFeature>[FontFeature.tabularFigures()],
  );
}

/// 间距阶梯（4px 基网），与管理端 --space-* 一一对应。
abstract final class AppSpacing {
  static const double xs = 4; // space-1
  static const double sm = 8; // space-2
  static const double md = 12; // space-3
  static const double lg = 16; // space-4
  static const double xl = 20; // space-45
  static const double xxl = 24; // space-5
  static const double xxxl = 32; // space-6
  static const double huge = 48; // space-7
}

/// 圆角令牌。
abstract final class AppRadius {
  static const double sm = 6;
  static const double md = 10;
  static const double lg = 14;
  static const double xl = 20;
  static const double pill = 999;

  static const BorderRadius smAll = BorderRadius.all(Radius.circular(sm));
  static const BorderRadius mdAll = BorderRadius.all(Radius.circular(md));
  static const BorderRadius lgAll = BorderRadius.all(Radius.circular(lg));
  static const BorderRadius xlAll = BorderRadius.all(Radius.circular(xl));
}

/// 阴影令牌（仅浅色主题；用 BoxShadow 而非 Material elevation 染色）。
abstract final class AppShadows {
  static const List<BoxShadow> sm = <BoxShadow>[
    BoxShadow(color: Color(0x0F10253F), blurRadius: 2, offset: Offset(0, 1)),
    BoxShadow(color: Color(0x1410253F), blurRadius: 3, offset: Offset(0, 1)),
  ];
  static const List<BoxShadow> md = <BoxShadow>[
    BoxShadow(color: Color(0x1A10253F), blurRadius: 12, offset: Offset(0, 4)),
  ];
  static const List<BoxShadow> lg = <BoxShadow>[
    BoxShadow(color: Color(0x2910253F), blurRadius: 32, offset: Offset(0, 12)),
  ];
}

/// 动效时长（毫秒）。读取 MediaQuery.disableAnimations 后应归零。
abstract final class AppMotion {
  static const Duration fast = Duration(milliseconds: 120);
  static const Duration normal = Duration(milliseconds: 200);
  static const Duration slow = Duration(milliseconds: 250);
  static const Duration emphasis = Duration(milliseconds: 300);
  static const Duration celebration = Duration(milliseconds: 500);
  static const Cubic easing = Cubic(0.2, 0, 0, 1);
}

/// 全局 ThemeData。色值已与管理端令牌对齐：
/// error 使用 danger #C92A2A，secondary 使用 navy #0B315D。
ThemeData buildAppTheme() {
  const colorScheme = ColorScheme.light(
    primary: AppColors.primary,
    onPrimary: Colors.white,
    secondary: AppColors.navy,
    onSecondary: Colors.white,
    error: AppColors.danger,
    surface: AppColors.surface,
    onSurface: AppColors.inkStrong,
    outline: AppColors.lineStrong,
  );

  return ThemeData(
    useMaterial3: true,
    scaffoldBackgroundColor: AppColors.paper,
    colorScheme: colorScheme,
    appBarTheme: const AppBarTheme(
      centerTitle: false,
      backgroundColor: AppColors.surface,
      foregroundColor: AppColors.inkStrong,
      elevation: 0,
      scrolledUnderElevation: 0,
    ),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        minimumSize: const Size(48, 52),
        shape: const RoundedRectangleBorder(borderRadius: AppRadius.mdAll),
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        minimumSize: const Size(48, 52),
        shape: const RoundedRectangleBorder(borderRadius: AppRadius.mdAll),
      ),
    ),
    inputDecorationTheme: const InputDecorationTheme(
      border: OutlineInputBorder(borderRadius: AppRadius.smAll),
    ),
    cardTheme: const CardThemeData(
      color: AppColors.surface,
      elevation: 0,
      shape: RoundedRectangleBorder(borderRadius: AppRadius.lgAll),
    ),
    dividerTheme: const DividerThemeData(color: AppColors.line, thickness: 1),
  );
}
