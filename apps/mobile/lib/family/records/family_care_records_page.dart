import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../../auth/session_controller.dart';
import '../../core/api/api_client.dart';
import '../../core/api/api_problem.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';

final familyCareRecordsGatewayProvider = Provider<FamilyCareRecordsGateway>((
  ref,
) {
  return ApiFamilyCareRecordsGateway(ref.watch(apiClientProvider));
});

final familyCareRecordsProvider =
    FutureProvider.autoDispose<List<FamilyCareRecord>>((ref) {
      final elderId = ref.watch(sessionControllerProvider)?.elderId;
      if (elderId == null) return const [];
      return ref.watch(familyCareRecordsGatewayProvider).load(elderId);
    });

abstract interface class FamilyCareRecordsGateway {
  Future<List<FamilyCareRecord>> load(String elderId);
}

class ApiFamilyCareRecordsGateway implements FamilyCareRecordsGateway {
  const ApiFamilyCareRecordsGateway(this.apiClient);

  final ApiClient apiClient;

  @override
  Future<List<FamilyCareRecord>> load(String elderId) {
    return apiClient.get(
      '/api/v1/elders/$elderId/care-records',
      (json) => (json! as List)
          .map(
            (item) => FamilyCareRecord.fromJson(
              Map<String, Object?>.from(item as Map),
            ),
          )
          .toList(growable: false),
    );
  }
}

class FamilyCareRecord {
  const FamilyCareRecord({
    required this.occurredAt,
    required this.kind,
    required this.summary,
  });

  final DateTime occurredAt;
  final String kind;
  final String summary;

  factory FamilyCareRecord.fromJson(Map<String, Object?> json) =>
      FamilyCareRecord(
        occurredAt: DateTime.parse(json['occurredAt']! as String),
        kind: json['kind']! as String,
        summary: json['summary']! as String,
      );
}

class FamilyCareRecordsPage extends ConsumerWidget {
  const FamilyCareRecordsPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final records = ref.watch(familyCareRecordsProvider);
    return Scaffold(
      appBar: AppBar(
        automaticallyImplyLeading: false,
        title: const Text('照料记录'),
      ),
      body: RefreshIndicator(
        onRefresh: () => ref.refresh(familyCareRecordsProvider.future),
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
              title: '照料记录',
              subtitle: '按时间查看已完成的探访、服务和回访摘要。',
            ),
            const SizedBox(height: AppSpacing.xxl),
            records.when(
              data: (items) {
                final sorted = [...items]
                  ..sort(
                    (left, right) =>
                        right.occurredAt.compareTo(left.occurredAt),
                  );
                if (sorted.isEmpty) {
                  return const AppInlineNotice(
                    message: '当前授权范围内暂无照料记录。',
                    icon: LucideIcons.clipboardCheck,
                    tone: AppNoticeTone.info,
                  );
                }
                return Column(
                  children: [
                    for (var index = 0; index < sorted.length; index++)
                      _CareRecordTimelineItem(
                        record: sorted[index],
                        isLast: index == sorted.length - 1,
                      ),
                  ],
                );
              },
              error: (error, stackTrace) => AppStatusPanel(
                icon: error is ApiProblem && error.code == 'CONSENT_REQUIRED'
                    ? LucideIcons.shieldX
                    : LucideIcons.cloudOff,
                title: error is ApiProblem && error.code == 'CONSENT_REQUIRED'
                    ? '老人已撤回此项授权'
                    : '照料记录暂时无法加载',
                description: '下拉页面可重新读取。',
                tone: AppNoticeTone.warning,
              ),
              loading: () => const Column(
                children: [
                  AppSkeleton(height: 94),
                  SizedBox(height: AppSpacing.md),
                  AppSkeleton(height: 94),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _CareRecordTimelineItem extends StatelessWidget {
  const _CareRecordTimelineItem({required this.record, required this.isLast});

  final FamilyCareRecord record;
  final bool isLast;

  @override
  Widget build(BuildContext context) {
    final color = _kindColor(record.kind);
    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          SizedBox(
            width: 32,
            child: Column(
              children: [
                Container(
                  width: 16,
                  height: 16,
                  decoration: BoxDecoration(
                    color: color,
                    shape: BoxShape.circle,
                  ),
                ),
                if (!isLast)
                  Expanded(
                    child: Container(width: 2, color: AppColors.lineStrong),
                  ),
              ],
            ),
          ),
          const SizedBox(width: AppSpacing.sm),
          Expanded(
            child: Container(
              margin: EdgeInsets.only(bottom: isLast ? 0 : AppSpacing.lg),
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.lg,
                0,
                0,
                AppSpacing.lg,
              ),
              decoration: const BoxDecoration(
                border: Border(bottom: BorderSide(color: AppColors.line)),
              ),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Icon(_kindIcon(record.kind), color: color, size: 26),
                  const SizedBox(width: AppSpacing.md),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(record.summary, style: AppTextStyles.sectionTitle),
                        const SizedBox(height: AppSpacing.xs),
                        Text(
                          '${_dateLabel(record.occurredAt)} · ${_kindLabel(record.kind)}',
                          style: AppTextStyles.caption.copyWith(
                            color: AppColors.ink,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

String _dateLabel(DateTime value) =>
    '${value.year}-${value.month.toString().padLeft(2, '0')}-${value.day.toString().padLeft(2, '0')}';

String _kindLabel(String kind) => switch (kind) {
  'Visit' => '探访',
  'ServiceOrder' => '服务',
  'FollowUp' => '回访',
  _ => '照料',
};

Color _kindColor(String kind) => switch (kind) {
  'Visit' => AppColors.primary,
  'ServiceOrder' => AppColors.accentWarmStrong,
  'FollowUp' => AppColors.success,
  _ => AppColors.navy,
};

IconData _kindIcon(String kind) => switch (kind) {
  'Visit' => LucideIcons.houseHeart,
  'ServiceOrder' => LucideIcons.handHeart,
  'FollowUp' => LucideIcons.phoneCall,
  _ => LucideIcons.clipboardCheck,
};
