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
  });

  final String label;
  final String semanticLabel;
  final VoidCallback? onPressed;
  final bool outlined;
  final bool danger;

  @override
  Widget build(BuildContext context) {
    final child = Text(
      label,
      style: const TextStyle(fontSize: 24, fontWeight: FontWeight.w700),
    );
    return Semantics(
      button: true,
      label: semanticLabel,
      excludeSemantics: true,
      child: SizedBox(
        width: double.infinity,
        height: 64,
        child: outlined
            ? OutlinedButton(
                onPressed: onPressed,
                style: OutlinedButton.styleFrom(
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
