import 'package:flutter/material.dart';

import '../theme/app_theme.dart';

/// 老人端状态大卡：签到状态、重要提醒等首页核心信息的承载容器。
/// 全宽、语义浅底 + 左侧状态色带 + 大标题。
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
      decoration: BoxDecoration(
        color: backgroundColor,
        borderRadius: AppRadius.xlAll,
        border: Border.all(color: AppColors.line),
      ),
      clipBehavior: Clip.antiAlias,
      child: Stack(
        children: [
          Padding(
            padding: const EdgeInsets.all(AppSpacing.xxl),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Icon(icon, size: 48, color: iconColor),
                const SizedBox(height: AppSpacing.md),
                Text(
                  title,
                  style: AppTextStyles.title.copyWith(color: titleColor),
                ),
                if (subtitle != null) ...[
                  const SizedBox(height: AppSpacing.sm),
                  Text(
                    subtitle!,
                    style: AppTextStyles.secondary.copyWith(
                      color: AppColors.ink,
                    ),
                  ),
                ],
                if (child != null) ...[
                  const SizedBox(height: AppSpacing.xl),
                  child!,
                ],
              ],
            ),
          ),
          Positioned(
            left: 0,
            top: 0,
            bottom: 0,
            width: 6,
            child: ColoredBox(color: iconColor),
          ),
        ],
      ),
    );
  }
}
