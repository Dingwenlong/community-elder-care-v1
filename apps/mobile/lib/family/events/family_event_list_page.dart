import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../auth/session_controller.dart';
import '../../core/api/api_client.dart';
import '../../core/theme/app_theme.dart';
import 'family_report_controller.dart';

final familyEventQueryGatewayProvider = Provider<FamilyEventQueryGateway>((
  ref,
) {
  return ApiFamilyEventQueryGateway(ref.watch(apiClientProvider));
});

final familyEventListProvider =
    FutureProvider.autoDispose<List<FamilyEventSummary>>((ref) {
      final elderId = ref.watch(sessionControllerProvider)?.elderId;
      if (elderId == null) return const [];
      return ref.watch(familyEventQueryGatewayProvider).list(elderId);
    });

abstract interface class FamilyEventQueryGateway {
  Future<List<FamilyEventSummary>> list(String elderId);
  Future<FamilyEventSummary> get(String eventId);
}

class ApiFamilyEventQueryGateway implements FamilyEventQueryGateway {
  const ApiFamilyEventQueryGateway(this.apiClient);

  final ApiClient apiClient;

  @override
  Future<List<FamilyEventSummary>> list(String elderId) {
    return apiClient.get(
      '/api/v1/care-events/',
      (json) => (json! as List)
          .map(
            (item) => FamilyEventSummary.fromJson(
              Map<String, Object?>.from(item as Map),
            ),
          )
          .toList(growable: false),
    );
  }

  @override
  Future<FamilyEventSummary> get(String eventId) {
    return apiClient.get(
      '/api/v1/care-events/$eventId',
      (json) =>
          FamilyEventSummary.fromJson(Map<String, Object?>.from(json! as Map)),
    );
  }
}

class FamilyEventListPage extends ConsumerWidget {
  const FamilyEventListPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final events = ref.watch(familyEventListProvider);
    final report = ref.watch(familyReportControllerProvider);
    return Scaffold(
      appBar: AppBar(title: const Text('照料事件')),
      body: RefreshIndicator(
        onRefresh: () => ref.refresh(familyEventListProvider.future),
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(AppSpacing.xl),
          children: [
            const Text(
              '这里只显示已授权的事件摘要。',
              style: TextStyle(fontSize: 18, color: AppColors.inkMuted),
            ),
            const SizedBox(height: AppSpacing.lg),
            FilledButton(
              onPressed: report.isSending
                  ? null
                  : () => ref
                        .read(familyReportControllerProvider.notifier)
                        .report(),
              child: const Text('报告联系不上老人'),
            ),
            const SizedBox(height: AppSpacing.xl),
            events.when(
              data: (items) => Column(
                children: [
                  for (final event in items)
                    Padding(
                      padding: const EdgeInsets.only(bottom: AppSpacing.md),
                      child: _EventListCard(event: event),
                    ),
                ],
              ),
              error: (error, stackTrace) => const Text('事件摘要暂时无法加载。'),
              loading: () => const Center(child: CircularProgressIndicator()),
            ),
          ],
        ),
      ),
    );
  }
}

class _EventListCard extends StatelessWidget {
  const _EventListCard({required this.event});

  final FamilyEventSummary event;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: AppColors.surface,
      borderRadius: AppRadius.lgAll,
      child: InkWell(
        borderRadius: AppRadius.lgAll,
        onTap: () => context.go('/family/events/${event.id}'),
        child: Container(
          constraints: const BoxConstraints(minHeight: 64),
          padding: const EdgeInsets.all(AppSpacing.lg),
          decoration: BoxDecoration(
            borderRadius: AppRadius.lgAll,
            boxShadow: AppShadows.sm,
          ),
          child: Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      event.summary,
                      style: const TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.w600,
                        color: AppColors.inkStrong,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.xs),
                    Text(
                      _statusLabel(event.status),
                      style: TextStyle(
                        fontSize: 14,
                        color: _statusColor(event.status),
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ],
                ),
              ),
              const Icon(
                Icons.chevron_right,
                size: 28,
                color: AppColors.inkMuted,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

String _statusLabel(String status) => switch (status) {
  'PendingConfirmation' => '社区正在电话确认',
  'FollowUpPending' => '已安排次日回访',
  'Closed' => '照料已完成',
  _ => '社区正在跟进',
};

Color _statusColor(String status) => switch (status) {
  'PendingConfirmation' => AppColors.warning,
  'FollowUpPending' => AppColors.primary,
  'Closed' => AppColors.success,
  _ => AppColors.navy,
};
