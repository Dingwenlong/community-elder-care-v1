import 'package:flutter/material.dart';

import '../../core/api/contracts.dart';

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
    return DecoratedBox(
      decoration: BoxDecoration(
        color: const Color(0xFFE8F1FB),
        border: Border.all(color: const Color(0xFF7AA7D8)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              '当前授权范围',
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 8),
            Text(
              grantedFields.map(consentFieldLabel).join('、'),
              style: const TextStyle(fontSize: 18),
            ),
            const SizedBox(height: 6),
            Text(
              '有效期至 ${expiresAt.year}年${expiresAt.month}月${expiresAt.day}日',
              style: const TextStyle(fontSize: 16),
            ),
          ],
        ),
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
