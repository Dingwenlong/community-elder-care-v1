abstract class ElderChatGateway {
  Future<String> send(String text);
}

class FixedUnavailableElderChatGateway implements ElderChatGateway {
  const FixedUnavailableElderChatGateway();

  @override
  Future<String> send(String text) async => '当前仅提供固定问答。如有危险，请立即点击“我需要帮助”。';
}
