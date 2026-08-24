import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../auth/session_controller.dart';
import '../../core/api/api_client.dart';
import '../../core/api/api_problem.dart';
import '../../core/api/contracts.dart';

final familyStatusGatewayProvider = Provider<FamilyStatusGateway>((ref) {
  return ApiFamilyStatusGateway(ref.watch(apiClientProvider));
});

final familyStatusControllerProvider =
    StateNotifierProvider.autoDispose<
      FamilyStatusController,
      FamilyStatusState
    >((ref) {
      return FamilyStatusController(
        elderId: ref.watch(sessionControllerProvider)?.elderId,
        gateway: ref.watch(familyStatusGatewayProvider),
      );
    });

abstract interface class FamilyStatusGateway {
  Future<FamilyStatusSnapshot> load(String elderId);
}

class ApiFamilyStatusGateway implements FamilyStatusGateway {
  const ApiFamilyStatusGateway(this.apiClient);

  final ApiClient apiClient;

  @override
  Future<FamilyStatusSnapshot> load(String elderId) {
    return apiClient.get(
      '/api/v1/family/elders/$elderId/summary',
      (json) => FamilyStatusSnapshot.fromJson(
        Map<String, Object?>.from(json! as Map),
      ),
    );
  }
}

class FamilyStatusSnapshot {
  const FamilyStatusSnapshot({
    required this.elderDisplayName,
    required this.grantedFields,
    required this.consentExpiresAt,
    required this.recentStatus,
    required this.reminderSummary,
    required this.careProgress,
    required this.visitSummary,
    required this.lastCommunityConfirmation,
  });

  final String elderDisplayName;
  final Set<ConsentField> grantedFields;
  final DateTime consentExpiresAt;
  final String? recentStatus;
  final String? reminderSummary;
  final String? careProgress;
  final String? visitSummary;
  final String? lastCommunityConfirmation;

  factory FamilyStatusSnapshot.fromJson(Map<String, Object?> json) {
    return FamilyStatusSnapshot(
      elderDisplayName: json['elderDisplayName']! as String,
      grantedFields: (json['grantedFields']! as List)
          .map((value) => _consentFieldFromJson(value! as String))
          .toSet(),
      consentExpiresAt: DateTime.parse(json['consentExpiresAt']! as String),
      recentStatus: json['recentStatus'] as String?,
      reminderSummary: json['reminderSummary'] as String?,
      careProgress: json['careProgress'] as String?,
      visitSummary: json['visitSummary'] as String?,
      lastCommunityConfirmation: json['lastCommunityConfirmation'] as String?,
    );
  }
}

ConsentField _consentFieldFromJson(String value) => switch (value) {
  'RecentStatus' => ConsentField.recentStatus,
  'CareEventSummary' => ConsentField.careEventSummary,
  'VisitSummary' => ConsentField.visitSummary,
  'ReminderCompletion' => ConsentField.reminderCompletion,
  'HealthRiskSummary' => ConsentField.healthRiskSummary,
  'EmergencyContact' => ConsentField.emergencyContact,
  _ => throw FormatException('Unsupported consent field: $value'),
};

class FamilyStatusState {
  const FamilyStatusState({
    this.snapshot,
    this.isLoading = true,
    this.isRevoked = false,
    this.errorMessage,
  });

  final FamilyStatusSnapshot? snapshot;
  final bool isLoading;
  final bool isRevoked;
  final String? errorMessage;
}

class FamilyStatusController extends StateNotifier<FamilyStatusState> {
  FamilyStatusController({required this.elderId, required this.gateway})
    : super(const FamilyStatusState()) {
    refresh();
  }

  final String? elderId;
  final FamilyStatusGateway gateway;

  Future<void> refresh() async {
    final id = elderId;
    state = const FamilyStatusState(isLoading: true);
    if (id == null) {
      state = const FamilyStatusState(
        isLoading: false,
        errorMessage: '登录资料缺少授权对象，请重新登录。',
      );
      return;
    }
    try {
      final snapshot = await gateway.load(id);
      if (mounted) {
        state = FamilyStatusState(snapshot: snapshot, isLoading: false);
      }
    } on ApiProblem catch (error) {
      if (!mounted) return;
      if (error.code == 'CONSENT_REQUIRED') {
        state = const FamilyStatusState(isLoading: false, isRevoked: true);
      } else {
        state = FamilyStatusState(
          isLoading: false,
          errorMessage: error.message,
        );
      }
    } on Object {
      if (mounted) {
        state = const FamilyStatusState(
          isLoading: false,
          errorMessage: '授权摘要暂时无法加载，请下拉刷新。',
        );
      }
    }
  }
}
