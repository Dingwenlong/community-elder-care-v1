import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../auth/session_controller.dart';
import '../../core/api/api_client.dart';
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
          padding: const EdgeInsets.all(20),
          children: [
            const Text('这里只显示已授权的事件摘要。', style: TextStyle(fontSize: 18)),
            const SizedBox(height: 16),
            FilledButton(
              onPressed: report.isSending
                  ? null
                  : () => ref
                        .read(familyReportControllerProvider.notifier)
                        .report(),
              child: const Text('报告联系不上老人'),
            ),
            const SizedBox(height: 18),
            events.when(
              data: (items) => Column(
                children: [
                  for (final event in items)
                    ListTile(
                      contentPadding: EdgeInsets.zero,
                      title: Text(event.summary),
                      subtitle: Text(_statusLabel(event.status)),
                      trailing: const Icon(Icons.chevron_right),
                      onTap: () => context.go('/family/events/${event.id}'),
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

String _statusLabel(String status) => switch (status) {
  'PendingConfirmation' => '社区正在电话确认',
  'FollowUpPending' => '已安排次日回访',
  'Closed' => '照料已完成',
  _ => '社区正在跟进',
};
