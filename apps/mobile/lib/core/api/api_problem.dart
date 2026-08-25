class ApiProblem implements Exception {
  const ApiProblem({
    required this.statusCode,
    required this.code,
    required this.message,
  });

  final int statusCode;
  final String code;
  final String message;

  static const messages = <String, String>{
    'INVALID_CREDENTIALS': '账号或密码不正确。',
    'FORBIDDEN_SCOPE': '当前账号没有执行这项操作的权限。',
    'CONSENT_REQUIRED': '老人尚未授权查看这项资料。',
    'NOT_FOUND': '没有找到对应资料。',
    'INVALID_TRANSITION': '当前状态不能执行这项操作。',
    'REQUEST_FAILED': '请求未完成，请稍后重试。',
  };

  factory ApiProblem.fromJson(int statusCode, Map<String, Object?> json) {
    final extensions = json['extensions'];
    final extensionCode = extensions is Map<String, Object?>
        ? extensions['code'] as String?
        : null;
    final code = json['code'] as String? ?? extensionCode ?? 'REQUEST_FAILED';
    return ApiProblem(
      statusCode: statusCode,
      code: code,
      message:
          messages[code] ??
          json['detail'] as String? ??
          json['title'] as String? ??
          messages['REQUEST_FAILED']!,
    );
  }

  @override
  String toString() => message;
}
