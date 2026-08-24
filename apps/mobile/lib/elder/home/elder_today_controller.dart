import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:uuid/uuid.dart';

import '../../auth/session_controller.dart';
import '../../core/api/api_client.dart';
import '../../core/outbox/outbox_entry.dart';
import '../../core/outbox/outbox_repository.dart';
import '../../core/outbox/outbox_sync_service.dart';

final elderTodayGatewayProvider = Provider<ElderTodayGateway>((ref) {
  return ApiElderTodayGateway(ref.watch(apiClientProvider));
});

final elderTodayControllerProvider =
    StateNotifierProvider<ElderTodayController, ElderTodayState>((ref) {
      return ElderTodayController(
        elderId: ref.watch(sessionControllerProvider)?.elderId,
        gateway: ref.watch(elderTodayGatewayProvider),
        repository: ref.watch(outboxRepositoryProvider),
        syncService: ref.watch(outboxSyncServiceProvider),
      );
    });

abstract interface class ElderTodayGateway {
  Future<ElderTodaySnapshot> loadToday(String elderId);
  Future<void> completeReminder(String reminderId, String requestId);
  Future<void> snoozeReminder(
    String reminderId,
    String requestId,
    DateTime nextReminderAt,
  );
}

class ApiElderTodayGateway implements ElderTodayGateway {
  const ApiElderTodayGateway(this.apiClient);

  final ApiClient apiClient;

  @override
  Future<ElderTodaySnapshot> loadToday(String elderId) {
    return apiClient.get(
      '/api/v1/elders/$elderId/today',
      (json) =>
          ElderTodaySnapshot.fromJson(Map<String, Object?>.from(json! as Map)),
    );
  }

  @override
  Future<void> completeReminder(String reminderId, String requestId) async {
    await apiClient.post<Object?>(
      '/api/v1/reminders/$reminderId/complete',
      (json) => json,
      body: {'requestId': requestId},
    );
  }

  @override
  Future<void> snoozeReminder(
    String reminderId,
    String requestId,
    DateTime nextReminderAt,
  ) async {
    await apiClient.post<Object?>(
      '/api/v1/reminders/$reminderId/snooze',
      (json) => json,
      body: {
        'requestId': requestId,
        'nextReminderAt': nextReminderAt.toUtc().toIso8601String(),
      },
    );
  }
}

class ElderTodaySnapshot {
  const ElderTodaySnapshot({
    required this.elderId,
    required this.serverTime,
    required this.isDemoData,
    required this.checkIns,
    required this.reminders,
  });

  final String elderId;
  final DateTime serverTime;
  final bool isDemoData;
  final List<TodayCheckIn> checkIns;
  final List<TodayReminder> reminders;

  bool get hasCheckedIn => checkIns.isNotEmpty;

  ElderTodaySnapshot copyWith({
    List<TodayCheckIn>? checkIns,
    List<TodayReminder>? reminders,
  }) => ElderTodaySnapshot(
    elderId: elderId,
    serverTime: serverTime,
    isDemoData: isDemoData,
    checkIns: checkIns ?? this.checkIns,
    reminders: reminders ?? this.reminders,
  );

  factory ElderTodaySnapshot.fromJson(Map<String, Object?> json) {
    return ElderTodaySnapshot(
      elderId: json['elderId']! as String,
      serverTime: DateTime.parse(json['serverTime']! as String),
      isDemoData: json['isDemoData']! as bool,
      checkIns: (json['checkIns']! as List)
          .map(
            (item) =>
                TodayCheckIn.fromJson(Map<String, Object?>.from(item as Map)),
          )
          .toList(growable: false),
      reminders: (json['reminders']! as List)
          .map(
            (item) =>
                TodayReminder.fromJson(Map<String, Object?>.from(item as Map)),
          )
          .toList(growable: false),
    );
  }
}

class TodayCheckIn {
  const TodayCheckIn({required this.requestId, required this.receivedAt});

  final String requestId;
  final DateTime receivedAt;

  factory TodayCheckIn.fromJson(Map<String, Object?> json) => TodayCheckIn(
    requestId: json['requestId']! as String,
    receivedAt: DateTime.parse(json['receivedAt']! as String),
  );
}

class TodayReminder {
  const TodayReminder({
    required this.id,
    required this.label,
    required this.state,
    this.dueAt,
  });

  final String id;
  final String label;
  final String state;
  final DateTime? dueAt;

  TodayReminder copyWith({String? state}) => TodayReminder(
    id: id,
    label: label,
    state: state ?? this.state,
    dueAt: dueAt,
  );

  factory TodayReminder.fromJson(Map<String, Object?> json) => TodayReminder(
    id: json['id']! as String,
    label: json['demoLabel']! as String,
    state: json['state']! as String,
    dueAt: DateTime.parse(json['nextDueAt']! as String),
  );
}

enum CheckInDeliveryStatus { idle, sending, unsent, sent }

class ElderTodayState {
  const ElderTodayState({
    this.snapshot,
    this.isLoading = true,
    this.errorMessage,
    this.checkInDelivery = CheckInDeliveryStatus.idle,
  });

  final ElderTodaySnapshot? snapshot;
  final bool isLoading;
  final String? errorMessage;
  final CheckInDeliveryStatus checkInDelivery;

  ElderTodayState copyWith({
    ElderTodaySnapshot? snapshot,
    bool? isLoading,
    String? errorMessage,
    CheckInDeliveryStatus? checkInDelivery,
  }) => ElderTodayState(
    snapshot: snapshot ?? this.snapshot,
    isLoading: isLoading ?? this.isLoading,
    errorMessage: errorMessage,
    checkInDelivery: checkInDelivery ?? this.checkInDelivery,
  );
}

class ElderTodayController extends StateNotifier<ElderTodayState> {
  ElderTodayController({
    required this.elderId,
    required this.gateway,
    required this.repository,
    required this.syncService,
  }) : super(const ElderTodayState()) {
    load();
  }

  final String? elderId;
  final ElderTodayGateway gateway;
  final OutboxRepository repository;
  final OutboxSyncService syncService;
  String? _checkInRequestId;

  Future<void> load() async {
    final id = elderId;
    if (id == null) {
      state = const ElderTodayState(
        isLoading: false,
        errorMessage: '登录资料缺少老人标识，请重新登录。',
      );
      return;
    }
    try {
      final snapshot = await gateway.loadToday(id);
      if (mounted) {
        state = ElderTodayState(snapshot: snapshot, isLoading: false);
      }
    } on Object {
      if (mounted) {
        state = const ElderTodayState(
          isLoading: false,
          errorMessage: '今日资料暂时无法加载，核心求助功能仍可使用。',
        );
      }
    }
  }

  Future<void> confirmSafety() async {
    final id = elderId;
    if (id == null || state.checkInDelivery == CheckInDeliveryStatus.sending) {
      return;
    }
    state = state.copyWith(checkInDelivery: CheckInDeliveryStatus.sending);
    _checkInRequestId ??= const Uuid().v4();
    await repository.enqueue(
      OutboxEntry(
        requestId: _checkInRequestId!,
        kind: OutboxKind.checkIn,
        payload: {
          'elderId': id,
          'clientTime': DateTime.now().toUtc().toIso8601String(),
        },
        priority: OutboxPriority.normal,
        createdAt: DateTime.now().toUtc(),
      ),
    );
    await _syncCheckIn();
  }

  Future<void> retryCheckIn() async {
    if (_checkInRequestId == null ||
        state.checkInDelivery == CheckInDeliveryStatus.sending) {
      return;
    }
    state = state.copyWith(checkInDelivery: CheckInDeliveryStatus.sending);
    await _syncCheckIn();
  }

  Future<void> completeReminder(TodayReminder reminder) async {
    await gateway.completeReminder(reminder.id, const Uuid().v4());
    _replaceReminder(reminder.copyWith(state: 'Completed'));
  }

  Future<void> snoozeReminder(TodayReminder reminder) async {
    await gateway.snoozeReminder(
      reminder.id,
      const Uuid().v4(),
      DateTime.now().toUtc().add(const Duration(minutes: 10)),
    );
    _replaceReminder(reminder.copyWith(state: 'Snoozed'));
  }

  Future<void> _syncCheckIn() async {
    await syncService.flush();
    final entry = await repository.findByRequestId(_checkInRequestId!);
    if (!mounted) return;
    final delivered = entry?.state == OutboxState.sent;
    var snapshot = state.snapshot;
    if (delivered && snapshot != null && !snapshot.hasCheckedIn) {
      snapshot = snapshot.copyWith(
        checkIns: [
          TodayCheckIn(
            requestId: _checkInRequestId!,
            receivedAt: DateTime.now().toUtc(),
          ),
        ],
      );
    }
    state = state.copyWith(
      snapshot: snapshot,
      checkInDelivery: delivered
          ? CheckInDeliveryStatus.sent
          : CheckInDeliveryStatus.unsent,
    );
  }

  void _replaceReminder(TodayReminder updated) {
    final snapshot = state.snapshot;
    if (snapshot == null || !mounted) return;
    state = state.copyWith(
      snapshot: snapshot.copyWith(
        reminders: [
          for (final reminder in snapshot.reminders)
            if (reminder.id == updated.id) updated else reminder,
        ],
      ),
    );
  }
}
