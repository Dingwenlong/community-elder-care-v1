import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../auth/session_controller.dart';
import '../../core/api/api_client.dart';
import '../../core/api/api_problem.dart';
import '../../core/theme/app_theme.dart';

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
      '/api/v1/family/elders/$elderId/care-records',
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
      appBar: AppBar(title: const Text('照料记录')),
      body: records.when(
        data: (items) => ListView.separated(
          padding: const EdgeInsets.all(AppSpacing.xl),
          itemCount: items.length,
          separatorBuilder: (context, index) =>
              const SizedBox(height: AppSpacing.md),
          itemBuilder: (context, index) {
            final record = items[index];
            return Container(
              padding: const EdgeInsets.all(AppSpacing.lg),
              decoration: BoxDecoration(
                color: AppColors.surface,
                borderRadius: AppRadius.lgAll,
                border: Border(
                  left: BorderSide(color: _kindColor(record.kind), width: 4),
                ),
                boxShadow: AppShadows.sm,
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    record.summary,
                    style: const TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.w600,
                      color: AppColors.inkStrong,
                    ),
                  ),
                  const SizedBox(height: AppSpacing.xs),
                  Text(
                    '${record.occurredAt.year}-${record.occurredAt.month.toString().padLeft(2, '0')}-${record.occurredAt.day.toString().padLeft(2, '0')} · ${_kindLabel(record.kind)}',
                    style: const TextStyle(
                      fontSize: 14,
                      color: AppColors.inkMuted,
                    ),
                  ),
                ],
              ),
            );
          },
        ),
        error: (error, stackTrace) => Center(
          child: Text(
            error is ApiProblem && error.code == 'CONSENT_REQUIRED'
                ? '老人已撤回此项授权'
                : '照料记录暂时无法加载。',
          ),
        ),
        loading: () => const Center(child: CircularProgressIndicator()),
      ),
    );
  }
}

String _kindLabel(String kind) => switch (kind) {
  'Visit' => '探访',
  'ServiceOrder' => '服务',
  'FollowUp' => '回访',
  _ => '照料',
};

Color _kindColor(String kind) => switch (kind) {
  'Visit' => AppColors.primary,
  'ServiceOrder' => AppColors.accentWarm,
  'FollowUp' => AppColors.success,
  _ => AppColors.navy,
};
