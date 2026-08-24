import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:uuid/uuid.dart';

import 'outbox_entry.dart';
import 'outbox_repository.dart';

abstract interface class OutboxSender {
  Future<void> send(OutboxEntry entry);
}

final outboxSenderProvider = Provider<OutboxSender>(
  (ref) => const UnavailableOutboxSender(),
);

final outboxSyncServiceProvider = Provider<OutboxSyncService>((ref) {
  return OutboxSyncService(
    repository: ref.watch(outboxRepositoryProvider),
    sender: ref.watch(outboxSenderProvider),
  );
});

final emergencyOutboxControllerProvider =
    StateNotifierProvider<EmergencyOutboxController, EmergencyDeliveryState>((
      ref,
    ) {
      return EmergencyOutboxController(
        repository: ref.watch(outboxRepositoryProvider),
        syncService: ref.watch(outboxSyncServiceProvider),
      );
    });

class UnavailableOutboxSender implements OutboxSender {
  const UnavailableOutboxSender();

  @override
  Future<void> send(OutboxEntry entry) {
    throw StateError('network_unavailable');
  }
}

class OutboxSyncService {
  const OutboxSyncService({required this.repository, required this.sender});

  final OutboxRepository repository;
  final OutboxSender sender;

  Future<void> flush() async {
    for (final entry in await repository.pending()) {
      final id = entry.id;
      if (id == null) continue;
      try {
        await sender.send(entry);
        await repository.markSent(id);
      } on Object catch (error) {
        await repository.markFailed(id, error);
      }
    }
  }
}

enum EmergencyDeliveryStatus { idle, unsent, sent }

class EmergencyDeliveryState {
  const EmergencyDeliveryState({
    this.status = EmergencyDeliveryStatus.idle,
    this.isSending = false,
  });

  final EmergencyDeliveryStatus status;
  final bool isSending;
}

class EmergencyOutboxController extends StateNotifier<EmergencyDeliveryState> {
  EmergencyOutboxController({
    required this.repository,
    required this.syncService,
  }) : super(const EmergencyDeliveryState());

  final OutboxRepository repository;
  final OutboxSyncService syncService;
  String? _requestId;

  Future<void> queueEmergency() async {
    if (state.isSending) return;
    state = EmergencyDeliveryState(status: state.status, isSending: true);
    _requestId ??= const Uuid().v4();
    await repository.enqueue(
      OutboxEntry(
        requestId: _requestId!,
        kind: 'EmergencyHelp',
        payload: const {'trigger': 'ExplicitSos', 'summary': '老人主动发起演示求助'},
        priority: OutboxPriority.high,
        createdAt: DateTime.now().toUtc(),
      ),
    );
    await _sendAndRefresh();
  }

  Future<void> retry() async {
    if (state.isSending || _requestId == null) return;
    state = EmergencyDeliveryState(status: state.status, isSending: true);
    await _sendAndRefresh();
  }

  Future<void> _sendAndRefresh() async {
    await syncService.flush();
    final entry = await repository.findByRequestId(_requestId!);
    if (!mounted) return;
    state = EmergencyDeliveryState(
      status: entry?.state == OutboxState.sent
          ? EmergencyDeliveryStatus.sent
          : EmergencyDeliveryStatus.unsent,
    );
  }
}
