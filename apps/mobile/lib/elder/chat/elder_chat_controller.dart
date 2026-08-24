import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'elder_chat_gateway.dart';

final elderChatGatewayProvider = Provider<ElderChatGateway>(
  (ref) => const FixedUnavailableElderChatGateway(),
);

final elderChatControllerProvider =
    StateNotifierProvider<ElderChatController, ElderChatState>((ref) {
      return ElderChatController(ref.watch(elderChatGatewayProvider));
    });

class ElderChatMessage {
  const ElderChatMessage({required this.text, required this.fromElder});

  final String text;
  final bool fromElder;
}

class ElderChatState {
  const ElderChatState({this.messages = const [], this.isSending = false});

  final List<ElderChatMessage> messages;
  final bool isSending;
}

class ElderChatController extends StateNotifier<ElderChatState> {
  ElderChatController(this.gateway) : super(const ElderChatState());

  final ElderChatGateway gateway;

  Future<void> send(String text) async {
    final value = text.trim();
    if (value.isEmpty || state.isSending) return;
    state = ElderChatState(
      messages: [
        ...state.messages,
        ElderChatMessage(text: value, fromElder: true),
      ],
      isSending: true,
    );
    final reply = await gateway.send(value);
    if (!mounted) return;
    state = ElderChatState(
      messages: [
        ...state.messages,
        ElderChatMessage(text: reply, fromElder: false),
      ],
    );
  }
}
