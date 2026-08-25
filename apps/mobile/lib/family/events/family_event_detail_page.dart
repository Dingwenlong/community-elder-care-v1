import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import 'family_event_list_page.dart';

class FamilyEventDetailPage extends ConsumerWidget {
  const FamilyEventDetailPage({super.key, required this.eventId});

  final String eventId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Scaffold(
      appBar: AppBar(title: const Text('事件进展')),
      body: FutureBuilder(
        future: ref.read(familyEventQueryGatewayProvider).get(eventId),
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return const Center(child: CircularProgressIndicator());
          }
          if (!snapshot.hasData) {
            return const Center(child: Text('事件摘要暂时无法加载。'));
          }
          final event = snapshot.data!;
          return ListView(
            padding: const EdgeInsets.all(AppSpacing.xl),
            children: [
              Container(
                height: 4,
                decoration: BoxDecoration(
                  color: _statusColor(event.status),
                  borderRadius: BorderRadius.circular(AppRadius.pill),
                ),
              ),
              const SizedBox(height: AppSpacing.lg),
              Text(
                event.summary,
                style: const TextStyle(
                  fontSize: 26,
                  fontWeight: FontWeight.w700,
                  color: AppColors.inkStrong,
                ),
              ),
              const SizedBox(height: AppSpacing.lg),
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(AppSpacing.lg),
                decoration: BoxDecoration(
                  color: _statusSoftColor(event.status),
                  borderRadius: AppRadius.lgAll,
                ),
                child: Text(
                  _safeProgress(event.status),
                  style: TextStyle(
                    fontSize: 19,
                    fontWeight: FontWeight.w600,
                    color: _statusColor(event.status),
                  ),
                ),
              ),
              const SizedBox(height: AppSpacing.xl),
              const Text(
                '页面不展示详细住址、内部责任队列、原始备注或原始 AI 内容。',
                style: TextStyle(color: AppColors.inkMuted),
              ),
            ],
          );
        },
      ),
    );
  }
}

String _safeProgress(String status) => switch (status) {
  'PendingConfirmation' => '社区正在电话确认',
  'FollowUpPending' => '已安排次日回访',
  'Closed' => '本次照料已完成',
  _ => '社区正在跟进',
};

Color _statusColor(String status) => switch (status) {
  'PendingConfirmation' => AppColors.warning,
  'FollowUpPending' => AppColors.primary,
  'Closed' => AppColors.success,
  _ => AppColors.navy,
};

Color _statusSoftColor(String status) => switch (status) {
  'PendingConfirmation' => AppColors.warningSoft,
  'FollowUpPending' => AppColors.primarySoft,
  'Closed' => AppColors.successSoft,
  _ => AppColors.surfaceMuted,
};
