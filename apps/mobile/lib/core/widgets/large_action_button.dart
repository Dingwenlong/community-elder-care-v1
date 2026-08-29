import 'package:flutter/material.dart';

import '../theme/app_theme.dart';

/// 老人端大操作按钮：64px 高、24px 加粗字、radius/xl 圆角。
/// [danger] 为 true 时使用危险实色（紧急求助等场景）。
class LargeActionButton extends StatelessWidget {
  const LargeActionButton({
    super.key,
    required this.label,
    required this.semanticLabel,
    required this.onPressed,
    this.outlined = false,
    this.danger = false,
    this.icon,
  });

  final String label;
  final String semanticLabel;
  final VoidCallback? onPressed;
  final bool outlined;
  final bool danger;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    final child = Row(
      mainAxisAlignment: MainAxisAlignment.center,
      mainAxisSize: MainAxisSize.min,
      children: [
        if (icon != null) ...[
          Icon(icon, size: 28),
          const SizedBox(width: AppSpacing.md),
        ],
        Flexible(
          child: Text(
            label,
            textAlign: TextAlign.center,
            style: const TextStyle(fontSize: 24, fontWeight: FontWeight.w700),
          ),
        ),
      ],
    );
    return Semantics(
      container: true,
      button: true,
      enabled: onPressed != null,
      label: semanticLabel,
      onTap: onPressed,
      excludeSemantics: true,
      child: SizedBox(
        width: double.infinity,
        child: outlined
            ? OutlinedButton(
                onPressed: onPressed,
                style: OutlinedButton.styleFrom(
                  foregroundColor: danger
                      ? AppColors.danger
                      : AppColors.primary,
                  side: BorderSide(
                    color: danger ? AppColors.danger : AppColors.primary,
                    width: 2,
                  ),
                  minimumSize: const Size.fromHeight(64),
                  padding: const EdgeInsets.symmetric(
                    horizontal: AppSpacing.lg,
                    vertical: AppSpacing.md,
                  ),
                  shape: const RoundedRectangleBorder(
                    borderRadius: AppRadius.xlAll,
                  ),
                ),
                child: child,
              )
            : FilledButton(
                onPressed: onPressed,
                style: FilledButton.styleFrom(
                  backgroundColor: danger ? AppColors.danger : null,
                  minimumSize: const Size.fromHeight(64),
                  padding: const EdgeInsets.symmetric(
                    horizontal: AppSpacing.lg,
                    vertical: AppSpacing.md,
                  ),
                  shape: const RoundedRectangleBorder(
                    borderRadius: AppRadius.xlAll,
                  ),
                ),
                child: child,
              ),
      ),
    );
  }
}
