import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:uuid/uuid.dart';

import '../../auth/session_controller.dart';
import '../../core/outbox/outbox_entry.dart';
import '../../core/outbox/outbox_repository.dart';
import '../../core/outbox/outbox_sync_service.dart';

enum HelpCategory { emergency, unwell, lifeService, wantToTalk }

extension HelpCategoryCopy on HelpCategory {
  String get label => switch (this) {
    HelpCategory.emergency => '紧急情况',
    HelpCategory.unwell => '身体不适',
    HelpCategory.lifeService => '生活服务',
    HelpCategory.wantToTalk => '想找人说说话',
  };

  String get trigger => switch (this) {
    HelpCategory.emergency => 'ExplicitSos',
    HelpCategory.unwell => 'DangerCue',
    HelpCategory.lifeService || HelpCategory.wantToTalk => 'LifeServiceNeed',
  };

  String get summary => switch (this) {
    HelpCategory.emergency => '老人主动发起紧急求助',
    HelpCategory.unwell => '老人报告身体不适，需要社区确认',
    HelpCategory.lifeService => '老人提出生活服务需求',
    HelpCategory.wantToTalk => '老人希望有人陪伴交谈',
  };

  OutboxPriority get priority => switch (this) {
    HelpCategory.emergency || HelpCategory.unwell => OutboxPriority.high,
    HelpCategory.lifeService ||
    HelpCategory.wantToTalk => OutboxPriority.normal,
  };
}

enum HelpDeliveryStatus { idle, sending, unsent, sent }

class HelpRequestState {
  const HelpRequestState({
    this.selected,
    this.deliveryStatus = HelpDeliveryStatus.idle,
  });

  final HelpCategory? selected;
  final HelpDeliveryStatus deliveryStatus;
}

final helpRequestControllerProvider =
    StateNotifierProvider<HelpRequestController, HelpRequestState>((ref) {
      return HelpRequestController(
        elderId: ref.watch(sessionControllerProvider)?.elderId,
        repository: ref.watch(outboxRepositoryProvider),
        syncService: ref.watch(outboxSyncServiceProvider),
      );
    });

class HelpRequestController extends StateNotifier<HelpRequestState> {
  HelpRequestController({
    required this.elderId,
    required this.repository,
    required this.syncService,
  }) : super(const HelpRequestState());

  final String? elderId;
  final OutboxRepository repository;
  final OutboxSyncService syncService;
  String? _requestId;

  void select(HelpCategory category) {
    _requestId = null;
    state = HelpRequestState(selected: category);
  }

  void clearSelection() {
    _requestId = null;
    state = const HelpRequestState();
  }

  Future<void> submit() async {
    final category = state.selected;
    final id = elderId;
    if (category == null ||
        state.deliveryStatus == HelpDeliveryStatus.sending) {
      return;
    }
    if (id == null) {
      state = HelpRequestState(
        selected: category,
        deliveryStatus: HelpDeliveryStatus.unsent,
      );
      return;
    }
    state = HelpRequestState(
      selected: category,
      deliveryStatus: HelpDeliveryStatus.sending,
    );
    _requestId ??= const Uuid().v4();
    final now = DateTime.now().toUtc();
    await repository.enqueue(
      OutboxEntry(
        requestId: _requestId!,
        kind: OutboxKind.careEvent,
        payload: {
          'elderId': id,
          'trigger': category.trigger,
          'summary': category.summary,
          'occurredAt': now.toIso8601String(),
        },
        priority: category.priority,
        createdAt: now,
      ),
    );
    await _sync();
  }

  Future<void> retry() async {
    if (_requestId == null ||
        state.deliveryStatus == HelpDeliveryStatus.sending) {
      return;
    }
    state = HelpRequestState(
      selected: state.selected,
      deliveryStatus: HelpDeliveryStatus.sending,
    );
    await _sync();
  }

  Future<void> _sync() async {
    await syncService.flush();
    final entry = await repository.findByRequestId(_requestId!);
    if (!mounted) return;
    state = HelpRequestState(
      selected: state.selected,
      deliveryStatus: entry?.state == OutboxState.sent
          ? HelpDeliveryStatus.sent
          : HelpDeliveryStatus.unsent,
    );
  }
}
