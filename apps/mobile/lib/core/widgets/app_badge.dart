import 'package:flutter/material.dart';

import '../theme/app_theme.dart';

/// 通用状态徽章（家属端/老人端共用）。
/// 语义色与 docs/ui/design-tokens.json 的 eventLevel 一致。
class AppBadge extends StatelessWidget {
  const AppBadge({super.key, required this.label, this.level});

  final String label;

  /// 事件等级语义；为 null 时使用中性灰底。
  final AppEventLevel? level;

  @override
  Widget build(BuildContext context) {
    final fg = level?.fg ?? AppColors.inkMuted;
    final bg = level?.bg ?? AppColors.surfaceMuted;
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: AppSpacing.xs,
      ),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(AppRadius.pill),
      ),
      child: Text(
        label,
        style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600, color: fg),
      ),
    );
  }
}
