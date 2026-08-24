import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:uuid/uuid.dart';

import '../../auth/session_controller.dart';
import '../../core/api/api_client.dart';

final familyReportGatewayProvider = Provider<FamilyReportGateway>((ref) {
  return ApiFamilyReportGateway(ref.watch(apiClientProvider));
});

final familyReportControllerProvider =
    StateNotifierProvider.autoDispose<
      FamilyReportController,
      FamilyReportState
    >((ref) {
      return FamilyReportController(
        elderId: ref.watch(sessionControllerProvider)?.elderId,
        gateway: ref.watch(familyReportGatewayProvider),
      );
    });

abstract interface class FamilyReportGateway {
  Future<FamilyEventSummary> reportCannotReach({
    required String elderId,
    required String clientRequestId,
    required DateTime occurredAt,
    String? note,
  });
}

class ApiFamilyReportGateway implements FamilyReportGateway {
  const ApiFamilyReportGateway(this.apiClient);

  final ApiClient apiClient;

  @override
  Future<FamilyEventSummary> reportCannotReach({
    required String elderId,
    required String clientRequestId,
    required DateTime occurredAt,
    String? note,
  }) {
    return apiClient.post(
      '/api/v1/care-events/',
      (json) =>
          FamilyEventSummary.fromJson(Map<String, Object?>.from(json! as Map)),
      body: {
        'clientRequestId': clientRequestId,
        'elderId': elderId,
        'summary': note?.trim().isNotEmpty == true
            ? '家属报告联系不上老人：${note!.trim()}'
            : '家属报告联系不上老人',
        'occurredAt': occurredAt.toUtc().toIso8601String(),
      },
    );
  }
}

class FamilyEventSummary {
  const FamilyEventSummary({
    required this.id,
    required this.source,
    required this.level,
    required this.status,
    required this.summary,
  });

  final String id;
  final String source;
  final String level;
  final String status;
  final String summary;

  factory FamilyEventSummary.fromJson(Map<String, Object?> json) =>
      FamilyEventSummary(
        id: json['id']! as String,
        source: json['source']! as String,
        level: json['level']! as String,
        status: json['status']! as String,
        summary: _naturalEventSummary(json['status']! as String),
      );
}

String _naturalEventSummary(String status) => switch (status) {
  'PendingConfirmation' => '社区正在电话确认',
  'FollowUpPending' => '已安排次日回访',
  'Closed' => '本次照料已完成',
  _ => '社区正在跟进',
};

class FamilyReportState {
  const FamilyReportState({
    this.event,
    this.isSending = false,
    this.errorMessage,
  });

  final FamilyEventSummary? event;
  final bool isSending;
  final String? errorMessage;
}

class FamilyReportController extends StateNotifier<FamilyReportState> {
  FamilyReportController({required this.elderId, required this.gateway})
    : super(const FamilyReportState());

  final String? elderId;
  final FamilyReportGateway gateway;
  String? _requestId;
  DateTime? _occurredAt;

  Future<void> report({String? note}) async {
    final id = elderId;
    if (id == null || state.isSending) return;
    state = FamilyReportState(event: state.event, isSending: true);
    _requestId ??= const Uuid().v4();
    _occurredAt ??= DateTime.now().toUtc();
    try {
      final event = await gateway.reportCannotReach(
        elderId: id,
        clientRequestId: _requestId!,
        occurredAt: _occurredAt!,
        note: note,
      );
      if (mounted) state = FamilyReportState(event: event);
    } on Object {
      if (mounted) {
        state = FamilyReportState(
          event: state.event,
          errorMessage: '上报结果暂未确认，可使用同一请求再次提交。',
        );
      }
    }
  }
}
