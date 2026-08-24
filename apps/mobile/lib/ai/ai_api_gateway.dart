import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../auth/session_controller.dart';
import '../core/api/api_client.dart';

final aiGatewayProvider = Provider<AiGateway>((ref) {
  return ApiAiGateway(
    apiClient: ref.watch(apiClientProvider),
    elderId: ref.watch(sessionControllerProvider)?.elderId,
  );
});

abstract interface class AiGateway {
  Future<AiChatReply> chat(String input, String sessionId);
  Future<AiDraft> confirmDraft(String draftId);
  Future<AiMemory> confirmMemory(String candidateId);
  Future<List<AiMemory>> listMemories();
  Future<void> deleteMemory(String memoryId);
}

class ApiAiGateway implements AiGateway {
  const ApiAiGateway({required this.apiClient, required this.elderId});

  final ApiClient apiClient;
  final String? elderId;

  @override
  Future<AiChatReply> chat(String input, String sessionId) {
    return apiClient.post(
      '/api/v1/ai/elder-chat',
      (json) => AiChatReply.fromJson(Map<String, Object?>.from(json! as Map)),
      body: {
        'elderId': _requiredElderId,
        'sessionId': sessionId,
        'input': input,
      },
    );
  }

  @override
  Future<AiDraft> confirmDraft(String draftId) {
    return apiClient.post(
      '/api/v1/ai/drafts/$draftId/confirm',
      (json) => AiDraft.fromJson(Map<String, Object?>.from(json! as Map)),
    );
  }

  @override
  Future<AiMemory> confirmMemory(String candidateId) {
    return apiClient.post(
      '/api/v1/ai/memory-candidates/$candidateId/confirm',
      (json) => AiMemory.fromJson(Map<String, Object?>.from(json! as Map)),
    );
  }

  @override
  Future<List<AiMemory>> listMemories() {
    return apiClient.get(
      '/api/v1/ai/memories',
      (json) => (json! as List)
          .map(
            (item) => AiMemory.fromJson(Map<String, Object?>.from(item as Map)),
          )
          .toList(growable: false),
    );
  }

  @override
  Future<void> deleteMemory(String memoryId) async {
    await apiClient.delete<Object?>(
      '/api/v1/ai/memories/$memoryId',
      (json) => json,
    );
  }

  String get _requiredElderId {
    final value = elderId;
    if (value == null) throw StateError('elder_scope_missing');
    return value;
  }
}

class AiChatReply {
  const AiChatReply({
    required this.reply,
    required this.usedFallback,
    required this.dangerCode,
    required this.isEmergency,
    this.serviceRequestDraft,
    this.memoryCandidate,
  });

  final String reply;
  final bool usedFallback;
  final String dangerCode;
  final bool isEmergency;
  final AiDraft? serviceRequestDraft;
  final AiMemory? memoryCandidate;

  factory AiChatReply.fromJson(Map<String, Object?> json) {
    final danger = Map<String, Object?>.from(json['dangerCue']! as Map);
    return AiChatReply(
      reply: json['reply']! as String,
      usedFallback: json['usedFallback']! as bool,
      dangerCode: danger['code']! as String,
      isEmergency: danger['isEmergency']! as bool,
      serviceRequestDraft: json['serviceRequestDraft'] is Map
          ? AiDraft.fromJson(
              Map<String, Object?>.from(json['serviceRequestDraft']! as Map),
            )
          : null,
      memoryCandidate: json['memoryCandidate'] is Map
          ? AiMemory.fromJson(
              Map<String, Object?>.from(json['memoryCandidate']! as Map),
            )
          : null,
    );
  }
}

class AiDraft {
  const AiDraft({
    required this.id,
    required this.generatedText,
    required this.status,
  });

  final String id;
  final String generatedText;
  final String status;

  factory AiDraft.fromJson(Map<String, Object?> json) => AiDraft(
    id: json['id']! as String,
    generatedText: json['generatedText']! as String,
    status: json['status']! as String,
  );
}

class AiMemory {
  const AiMemory({
    required this.id,
    required this.generatedText,
    required this.isConfirmed,
  });

  final String id;
  final String generatedText;
  final bool isConfirmed;

  factory AiMemory.fromJson(Map<String, Object?> json) => AiMemory(
    id: json['id']! as String,
    generatedText: json['generatedText']! as String,
    isConfirmed: json['isConfirmed']! as bool,
  );
}
