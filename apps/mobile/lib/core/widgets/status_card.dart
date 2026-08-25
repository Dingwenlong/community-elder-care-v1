import 'package:flutter/material.dart';

import '../theme/app_theme.dart';

/// 老人端状态大卡：签到状态、重要提醒等首页核心信息的承载容器。
/// 全宽、radius/xl 圆角、语义浅底 + 48px 图标 + 大标题。
class StatusCard extends StatelessWidget {
  const StatusCard({
    super.key,
    required this.backgroundColor,
    required this.icon,
    required this.iconColor,
    required this.title,
    required this.titleColor,
    this.subtitle,
    this.child,
  });

  final Color backgroundColor;
  final IconData icon;
  final Color iconColor;
  final String title;
  final Color titleColor;
  final String? subtitle;
  final Widget? child;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(AppSpacing.xxl),
      decoration: BoxDecoration(
        color: backgroundColor,
        borderRadius: AppRadius.xlAll,
        boxShadow: AppShadows.sm,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          Icon(icon, size: 48, color: iconColor),
          const SizedBox(height: AppSpacing.md),
          Text(
            title,
            textAlign: TextAlign.center,
            style: AppTextStyles.title.copyWith(color: titleColor),
          ),
          if (subtitle != null) ...[
            const SizedBox(height: AppSpacing.sm),
            Text(
              subtitle!,
              textAlign: TextAlign.center,
              style: AppTextStyles.secondary.copyWith(color: AppColors.ink),
            ),
          ],
          if (child != null) ...[
            const SizedBox(height: AppSpacing.xl),
            child!,
          ],
        ],
      ),
    );
  }
}
