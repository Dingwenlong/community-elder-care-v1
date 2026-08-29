import 'package:flutter/material.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../../core/api/contracts.dart';
import '../../core/theme/app_theme.dart';

class ConsentScopeCard extends StatelessWidget {
  const ConsentScopeCard({
    super.key,
    required this.grantedFields,
    required this.expiresAt,
  });

  final Set<ConsentField> grantedFields;
  final DateTime expiresAt;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: const BoxDecoration(
        color: AppColors.primarySoft,
        border: Border(
          left: BorderSide(color: AppColors.primary, width: 4),
          top: BorderSide(color: AppColors.line),
          right: BorderSide(color: AppColors.line),
          bottom: BorderSide(color: AppColors.line),
        ),
      ),
      padding: const EdgeInsets.all(AppSpacing.lg),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Icon(
            LucideIcons.shieldCheck,
            color: AppColors.primary,
            size: 28,
          ),
          const SizedBox(width: AppSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('当前授权范围', style: AppTextStyles.sectionTitle),
                const SizedBox(height: AppSpacing.sm),
                Text(
                  grantedFields.map(consentFieldLabel).join('、'),
                  style: AppTextStyles.bodySmall.copyWith(color: AppColors.ink),
                ),
                const SizedBox(height: AppSpacing.sm),
                Text(
                  '有效期至 ${expiresAt.year}年${expiresAt.month}月${expiresAt.day}日',
                  style: AppTextStyles.caption.copyWith(color: AppColors.ink),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

String consentFieldLabel(ConsentField field) => switch (field) {
  ConsentField.recentStatus => '最近状态',
  ConsentField.careEventSummary => '照料进展',
  ConsentField.visitSummary => '探访摘要',
  ConsentField.reminderCompletion => '提醒完成情况',
  ConsentField.healthRiskSummary => '健康风险摘要',
  ConsentField.emergencyContact => '应急联系人',
};
