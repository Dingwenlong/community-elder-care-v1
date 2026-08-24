abstract class ElderChatGateway {
  Future<String> send(String text);
}

class FixedUnavailableElderChatGateway implements ElderChatGateway {
  const FixedUnavailableElderChatGateway();

  @override
  Future<String> send(String text) async => '当前智能陪伴暂不可用。如有危险，请立即点击“我需要帮助”。';
}
