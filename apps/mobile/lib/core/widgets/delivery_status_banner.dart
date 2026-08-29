import 'package:flutter/material.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../theme/app_theme.dart';
import 'app_page.dart';

class DeliveryStatusBanner extends StatelessWidget {
  const DeliveryStatusBanner({
    super.key,
    required this.message,
    required this.delivered,
    this.onRetry,
  });

  final String message;
  final bool delivered;
  final VoidCallback? onRetry;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      liveRegion: true,
      label: message,
      child: AppInlineNotice(
        message: message,
        icon: delivered ? LucideIcons.circleCheck : LucideIcons.cloudOff,
        tone: delivered ? AppNoticeTone.success : AppNoticeTone.warning,
        elder: true,
        liveRegion: true,
        action: onRetry == null
            ? null
            : TextButton(
                onPressed: onRetry,
                style: TextButton.styleFrom(foregroundColor: AppColors.warning),
                child: const Text('重新发送'),
              ),
      ),
    );
  }
}
