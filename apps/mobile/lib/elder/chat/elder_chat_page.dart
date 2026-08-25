import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_tts/flutter_tts.dart';

import '../../ai/ai_draft_confirmation_card.dart';
import '../../core/theme/app_theme.dart';
import '../settings/elder_settings_page.dart';
import 'elder_chat_controller.dart';

class ElderChatPage extends ConsumerStatefulWidget {
  const ElderChatPage({super.key});

  @override
  ConsumerState<ElderChatPage> createState() => _ElderChatPageState();
}

class _ElderChatPageState extends ConsumerState<ElderChatPage> {
  final _input = TextEditingController();
  final _tts = FlutterTts();

  @override
  void dispose() {
    _input.dispose();
    _tts.stop();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(elderChatControllerProvider);
    final ttsEnabled = ref.watch(elderTtsEnabledProvider);
    final latestReply = state.messages
        .where((message) => !message.fromElder)
        .lastOrNull;
    return Scaffold(
      appBar: AppBar(title: const Text('陪伴问答')),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(AppSpacing.xl),
          children: [
            Container(
              padding: const EdgeInsets.all(AppSpacing.lg),
              decoration: BoxDecoration(
                color: AppColors.warningSoft,
                borderRadius: AppRadius.smAll,
                border: Border.all(color: AppColors.warning),
              ),
              child: const Text(
                'AI 仅作辅助，核心求助由安全规则和人工处理',
                style: TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.w700,
                  color: AppColors.warning,
                ),
              ),
            ),
            const SizedBox(height: AppSpacing.xl),
            const Text(
              '常见问题',
              style: TextStyle(
                fontSize: 22,
                fontWeight: FontWeight.w700,
                color: AppColors.inkStrong,
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            Wrap(
              spacing: AppSpacing.sm,
              runSpacing: AppSpacing.sm,
              children: [
                for (final question in const [
                  '怎么查看今天的提醒？',
                  '身体不舒服怎么办？',
                  '怎么联系社区？',
                ])
                  OutlinedButton(
                    onPressed: () => ref
                        .read(elderChatControllerProvider.notifier)
                        .send(question),
                    child: Text(question),
                  ),
              ],
            ),
            const SizedBox(height: AppSpacing.xl),
            for (final message in state.messages)
              Padding(
                padding: const EdgeInsets.only(bottom: AppSpacing.md),
                child: Column(
                  crossAxisAlignment: message.fromElder
                      ? CrossAxisAlignment.end
                      : CrossAxisAlignment.start,
                  children: [
                    Text(
                      message.fromElder ? '我' : '固定回复',
                      style: const TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.w700,
                        color: AppColors.inkMuted,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.xs),
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: AppSpacing.lg,
                        vertical: AppSpacing.md,
                      ),
                      decoration: BoxDecoration(
                        color: message.fromElder
                            ? AppColors.primary
                            : AppColors.surface,
                        borderRadius: AppRadius.lgAll,
                        boxShadow: message.fromElder ? null : AppShadows.sm,
                      ),
                      child: Text(
                        message.text,
                        style: TextStyle(
                          fontSize: 18,
                          height: 1.5,
                          color: message.fromElder
                              ? AppColors.surface
                              : AppColors.ink,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            if (state.serviceRequestDraft case final draft?) ...[
              AiDraftConfirmationCard(
                draft: draft,
                onConfirm: state.isSending
                    ? null
                    : () => ref
                          .read(elderChatControllerProvider.notifier)
                          .confirmDraft(),
              ),
              const SizedBox(height: 12),
            ],
            if (state.memoryCandidate case final memory?) ...[
              Container(
                padding: const EdgeInsets.all(AppSpacing.lg),
                decoration: BoxDecoration(
                  color: AppColors.surface,
                  borderRadius: AppRadius.lgAll,
                  border: Border.all(color: AppColors.primary),
                  boxShadow: AppShadows.sm,
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const Text(
                      '记忆候选',
                      style: TextStyle(
                        fontSize: 20,
                        fontWeight: FontWeight.w700,
                        color: AppColors.inkStrong,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.sm),
                    Text(
                      memory.generatedText,
                      style: const TextStyle(color: AppColors.ink),
                    ),
                    const SizedBox(height: AppSpacing.md),
                    OutlinedButton(
                      onPressed: state.isSending
                          ? null
                          : () => ref
                                .read(elderChatControllerProvider.notifier)
                                .confirmMemory(),
                      child: const Text('确认记忆'),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: AppSpacing.md),
            ],
            TextField(
              controller: _input,
              minLines: 1,
              maxLines: 3,
              decoration: const InputDecoration(labelText: '输入想问的内容'),
            ),
            const SizedBox(height: AppSpacing.md),
            FilledButton(
              onPressed: state.isSending
                  ? null
                  : () {
                      final text = _input.text;
                      _input.clear();
                      ref.read(elderChatControllerProvider.notifier).send(text);
                    },
              child: const Text('发送'),
            ),
            const SizedBox(height: AppSpacing.md),
            OutlinedButton(
              onPressed: ttsEnabled && latestReply != null
                  ? () => _tts.speak(latestReply.text)
                  : null,
              child: const Text('朗读回复'),
            ),
          ],
        ),
      ),
    );
  }
}
