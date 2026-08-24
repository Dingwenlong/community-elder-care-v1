class LocalDangerCueResult {
  const LocalDangerCueResult({
    required this.isEmergency,
    required this.needsConfirmation,
    required this.code,
  });

  final bool isEmergency;
  final bool needsConfirmation;
  final String code;
}

class LocalDangerCueScanner {
  const LocalDangerCueScanner();

  LocalDangerCueResult scan(String input) {
    final text = input.trim();
    if (_contains(text, ['不想活了', '想死', '自杀', '结束生命'])) {
      return _emergency('SELF_HARM');
    }
    if (_contains(text, ['喘不上气', '呼吸困难', '无法呼吸', '不能呼吸'])) {
      return _emergency('BREATHING_DIFFICULTY');
    }
    if (_contains(text, ['胸口很痛', '胸口剧痛', '胸痛'])) {
      return _emergency('CHEST_PAIN');
    }
    if (_contains(text, ['摔倒', '跌倒']) &&
        _contains(text, ['起不来', '站不起来', '无法起身'])) {
      return _emergency('FALL_CANNOT_STAND');
    }
    if (_contains(text, ['差点摔倒', '差点跌倒', '防滑垫'])) {
      return _confirmation('POSSIBLE_FALL_RISK');
    }
    if (_contains(text, ['胸闷', '胸口不舒服'])) {
      return _confirmation('POSSIBLE_CHEST_DISCOMFORT');
    }
    return const LocalDangerCueResult(
      isEmergency: false,
      needsConfirmation: false,
      code: 'NONE',
    );
  }

  bool _contains(String input, List<String> phrases) =>
      phrases.any(input.contains);

  LocalDangerCueResult _emergency(String code) => LocalDangerCueResult(
    isEmergency: true,
    needsConfirmation: true,
    code: code,
  );

  LocalDangerCueResult _confirmation(String code) => LocalDangerCueResult(
    isEmergency: false,
    needsConfirmation: true,
    code: code,
  );
}
