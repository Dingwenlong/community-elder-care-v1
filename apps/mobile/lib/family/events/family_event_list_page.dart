import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../../auth/session_controller.dart';
import '../../core/api/api_client.dart';
import '../../core/api/api_problem.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
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

final familyEventDetailProvider = FutureProvider.autoDispose
    .family<FamilyEventSummary, String>((ref, eventId) {
      return ref.watch(familyEventQueryGatewayProvider).get(eventId);
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
      appBar: AppBar(
        automaticallyImplyLeading: false,
        title: const Text('照料事件'),
      ),
      body: RefreshIndicator(
        onRefresh: () => ref.refresh(familyEventListProvider.future),
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.fromLTRB(
            AppSpacing.xl,
            AppSpacing.lg,
            AppSpacing.xl,
            AppSpacing.huge,
          ),
          children: [
            const AppPageHeader(
              eyebrow: '授权范围内',
              title: '照料事件',
              subtitle: '这里只显示已授权的事件摘要和处理状态。',
            ),
            const SizedBox(height: AppSpacing.xxl),
            OutlinedButton.icon(
              onPressed: report.isSending
                  ? null
                  : () => ref
                        .read(familyReportControllerProvider.notifier)
                        .report(),
              icon: const Icon(LucideIcons.phoneOff),
              label: Text(report.isSending ? '正在报告' : '报告联系不上老人'),
              style: OutlinedButton.styleFrom(
                foregroundColor: AppColors.danger,
                side: const BorderSide(color: AppColors.danger, width: 2),
                minimumSize: const Size.fromHeight(54),
              ),
            ),
            const SizedBox(height: AppSpacing.xxl),
            events.when(
              data: (items) => items.isEmpty
                  ? const AppInlineNotice(
                      message: '当前授权范围内暂无照料事件。',
                      icon: LucideIcons.clipboardCheck,
                      tone: AppNoticeTone.info,
                    )
                  : Container(
                      decoration: BoxDecoration(
                        color: AppColors.surface,
                        border: Border.all(color: AppColors.line),
                      ),
                      child: Column(
                        children: [
                          for (
                            var index = 0;
                            index < items.length;
                            index++
                          ) ...[
                            _EventListRow(event: items[index]),
                            if (index < items.length - 1)
                              const Divider(
                                height: 1,
                                indent: AppSpacing.lg,
                                endIndent: AppSpacing.lg,
                              ),
                          ],
                        ],
                      ),
                    ),
              error: (error, stackTrace) => AppStatusPanel(
                icon: error is ApiProblem && error.code == 'CONSENT_REQUIRED'
                    ? LucideIcons.shieldX
                    : LucideIcons.cloudOff,
                title: error is ApiProblem && error.code == 'CONSENT_REQUIRED'
                    ? '老人已撤回此项授权'
                    : '事件摘要暂时无法加载',
                description: '下拉页面可重新读取。',
                tone: AppNoticeTone.warning,
              ),
              loading: () => const Column(
                children: [
                  AppSkeleton(height: 82),
                  SizedBox(height: AppSpacing.md),
                  AppSkeleton(height: 82),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _EventListRow extends StatelessWidget {
  const _EventListRow({required this.event});

  final FamilyEventSummary event;

  @override
  Widget build(BuildContext context) {
    final color = statusColor(event.status);
    return Material(
      color: AppColors.surface,
      child: InkWell(
        onTap: () => context.push('/family/events/${event.id}'),
        child: ConstrainedBox(
          constraints: const BoxConstraints(minHeight: 78),
          child: Row(
            children: [
              Container(width: 5, height: 78, color: color),
              const SizedBox(width: AppSpacing.lg),
              Icon(statusIcon(event.status), size: 27, color: color),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: Padding(
                  padding: const EdgeInsets.symmetric(vertical: AppSpacing.lg),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(event.summary, style: AppTextStyles.sectionTitle),
                      const SizedBox(height: AppSpacing.xs),
                      Text(
                        statusLabel(event.status),
                        style: AppTextStyles.caption.copyWith(
                          color: color,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              const Icon(
                LucideIcons.chevronRight,
                size: 24,
                color: AppColors.inkMuted,
              ),
              const SizedBox(width: AppSpacing.lg),
            ],
          ),
        ),
      ),
    );
  }
}

String statusLabel(String status) => switch (status) {
  'PendingConfirmation' => '社区正在电话确认',
  'FollowUpPending' => '已安排次日回访',
  'Closed' => '照料已完成',
  _ => '社区正在跟进',
};

Color statusColor(String status) => switch (status) {
  'PendingConfirmation' => AppColors.warning,
  'FollowUpPending' => AppColors.primary,
  'Closed' => AppColors.success,
  _ => AppColors.navy,
};

Color statusSoftColor(String status) => switch (status) {
  'PendingConfirmation' => AppColors.warningSoft,
  'FollowUpPending' => AppColors.primarySoft,
  'Closed' => AppColors.successSoft,
  _ => AppColors.surfaceMuted,
};

IconData statusIcon(String status) => switch (status) {
  'PendingConfirmation' => LucideIcons.phoneCall,
  'FollowUpPending' => LucideIcons.calendarClock,
  'Closed' => LucideIcons.circleCheck,
  _ => LucideIcons.workflow,
};
