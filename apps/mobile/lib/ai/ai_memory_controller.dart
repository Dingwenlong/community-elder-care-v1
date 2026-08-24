import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'ai_api_gateway.dart';

final aiMemoryControllerProvider =
    StateNotifierProvider.autoDispose<AiMemoryController, AiMemoryState>((ref) {
      return AiMemoryController(ref.watch(aiGatewayProvider));
    });

class AiMemoryState {
  const AiMemoryState({
    this.memories = const [],
    this.isLoading = true,
    this.errorMessage,
  });

  final List<AiMemory> memories;
  final bool isLoading;
  final String? errorMessage;
}

class AiMemoryController extends StateNotifier<AiMemoryState> {
  AiMemoryController(this.gateway) : super(const AiMemoryState()) {
    refresh();
  }

  final AiGateway gateway;

  Future<void> refresh() async {
    state = AiMemoryState(memories: state.memories, isLoading: true);
    try {
      final memories = await gateway.listMemories();
      if (mounted) state = AiMemoryState(memories: memories, isLoading: false);
    } on Object {
      if (mounted) {
        state = const AiMemoryState(
          isLoading: false,
          errorMessage: 'AI 记忆暂时无法加载。',
        );
      }
    }
  }

  Future<void> delete(String memoryId) async {
    await gateway.deleteMemory(memoryId);
    if (!mounted) return;
    state = AiMemoryState(
      memories: state.memories
          .where((memory) => memory.id != memoryId)
          .toList(growable: false),
      isLoading: false,
    );
  }
}
