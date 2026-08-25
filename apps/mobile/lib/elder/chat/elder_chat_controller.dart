import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:uuid/uuid.dart';

import '../../ai/ai_api_gateway.dart';
import '../../ai/local_danger_cue_scanner.dart';

const _offlineReply = 'AI 当前不可用，核心求助功能仍可使用。你可以查看提醒，或点击“我需要帮助”。';
const _emergencyGuidancePrimary = '如果能够操作，请立即呼叫身边的人。';
const _emergencyGuidanceDelivery = '系统正在把求助发送给社区；当前不会真实拨打 120。';

final elderChatControllerProvider =
    StateNotifierProvider<ElderChatController, ElderChatState>((ref) {
      return ElderChatController(ref.watch(aiGatewayProvider));
    });

class ElderChatMessage {
  const ElderChatMessage({required this.text, required this.fromElder});

  final String text;
  final bool fromElder;
}

class ElderChatState {
  const ElderChatState({
    this.messages = const [],
    this.isSending = false,
    this.serviceRequestDraft,
    this.memoryCandidate,
  });

  final List<ElderChatMessage> messages;
  final bool isSending;
  final AiDraft? serviceRequestDraft;
  final AiMemory? memoryCandidate;

  ElderChatState copyWith({
    List<ElderChatMessage>? messages,
    bool? isSending,
    AiDraft? serviceRequestDraft,
    bool clearDraft = false,
    AiMemory? memoryCandidate,
    bool clearMemory = false,
  }) => ElderChatState(
    messages: messages ?? this.messages,
    isSending: isSending ?? this.isSending,
    serviceRequestDraft: clearDraft
        ? null
        : serviceRequestDraft ?? this.serviceRequestDraft,
    memoryCandidate: clearMemory
        ? null
        : memoryCandidate ?? this.memoryCandidate,
  );
}

class ElderChatController extends StateNotifier<ElderChatState> {
  ElderChatController(this.gateway) : super(const ElderChatState());

  final AiGateway gateway;
  final _scanner = const LocalDangerCueScanner();
  final _sessionId = const Uuid().v4();

  Future<void> send(String text) async {
    final value = text.trim();
    if (value.isEmpty || state.isSending) return;
    final localDanger = _scanner.scan(value);
    final messages = [
      ...state.messages,
      ElderChatMessage(text: value, fromElder: true),
    ];
    if (localDanger.isEmergency) {
      messages
        ..add(
          const ElderChatMessage(
            text: _emergencyGuidancePrimary,
            fromElder: false,
          ),
        )
        ..add(
          const ElderChatMessage(
            text: _emergencyGuidanceDelivery,
            fromElder: false,
          ),
        );
    }
    state = ElderChatState(messages: messages, isSending: true);

    try {
      final reply = await gateway.chat(value, _sessionId);
      if (!mounted) return;
      final nextMessages = [...state.messages];
      if (localDanger.isEmergency && nextMessages.length >= 2) {
        nextMessages[nextMessages.length - 1] = ElderChatMessage(
          text: reply.reply,
          fromElder: false,
        );
      } else {
        nextMessages.add(ElderChatMessage(text: reply.reply, fromElder: false));
      }
      state = ElderChatState(
        messages: nextMessages,
        serviceRequestDraft: reply.serviceRequestDraft,
        memoryCandidate: reply.memoryCandidate,
      );
    } on Object {
      if (!mounted) return;
      state = ElderChatState(
        messages: localDanger.isEmergency
            ? state.messages
            : [
                ...state.messages,
                const ElderChatMessage(text: _offlineReply, fromElder: false),
              ],
      );
    }
  }

  Future<void> confirmDraft() async {
    final draft = state.serviceRequestDraft;
    if (draft == null || state.isSending) return;
    state = state.copyWith(isSending: true);
    try {
      await gateway.confirmDraft(draft.id);
      if (!mounted) return;
      state = state.copyWith(
        isSending: false,
        clearDraft: true,
        messages: [
          ...state.messages,
          const ElderChatMessage(text: '服务请求已确认提交', fromElder: false),
        ],
      );
    } on Object {
      if (mounted) state = state.copyWith(isSending: false);
    }
  }

  Future<void> confirmMemory() async {
    final memory = state.memoryCandidate;
    if (memory == null || state.isSending) return;
    state = state.copyWith(isSending: true);
    try {
      await gateway.confirmMemory(memory.id);
      if (!mounted) return;
      state = state.copyWith(
        isSending: false,
        clearMemory: true,
        messages: [
          ...state.messages,
          const ElderChatMessage(text: '记忆已确认', fromElder: false),
        ],
      );
    } on Object {
      if (mounted) state = state.copyWith(isSending: false);
    }
  }
}
