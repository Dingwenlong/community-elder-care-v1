import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_tts/flutter_tts.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../../ai/ai_draft_confirmation_card.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../../core/widgets/large_action_button.dart';
import '../settings/elder_settings_page.dart';
import 'elder_chat_controller.dart';

class ElderChatPage extends ConsumerStatefulWidget {
  const ElderChatPage({super.key});

  @override
  ConsumerState<ElderChatPage> createState() => _ElderChatPageState();
}

class _ElderChatPageState extends ConsumerState<ElderChatPage> {
  final _input = TextEditingController();
  final _scroll = ScrollController();
  final _latestMessageKey = GlobalKey();
  final _tts = FlutterTts();

  @override
  void dispose() {
    _input.dispose();
    _scroll.dispose();
    _tts.stop();
    super.dispose();
  }

  void _send() {
    final text = _input.text;
    if (text.trim().isEmpty) return;
    _input.clear();
    ref.read(elderChatControllerProvider.notifier).send(text);
  }

  void _revealLatestMessage({required double alignment}) {
    if (!mounted || !_scroll.hasClients) return;
    final latestContext = _latestMessageKey.currentContext;
    if (latestContext != null) {
      Scrollable.ensureVisible(
        latestContext,
        alignment: alignment,
        duration: Duration.zero,
      );
      return;
    }

    _scroll.jumpTo(_scroll.position.maxScrollExtent);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      final builtLatestContext = _latestMessageKey.currentContext;
      if (builtLatestContext == null) return;
      Scrollable.ensureVisible(
        builtLatestContext,
        alignment: alignment,
        duration: Duration.zero,
      );
    });
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(elderChatControllerProvider);
    final controller = ref.read(elderChatControllerProvider.notifier);
    final ttsEnabled = ref.watch(elderTtsEnabledProvider);
    final latestReply = state.messages
        .where((message) => !message.fromElder)
        .lastOrNull;

    ref.listen(elderChatControllerProvider, (previous, next) {
      if (previous?.messages.length == next.messages.length) return;
      final hasFollowUp =
          next.serviceRequestDraft != null || next.memoryCandidate != null;
      WidgetsBinding.instance.addPostFrameCallback((_) {
        _revealLatestMessage(alignment: hasFollowUp ? .08 : 1);
      });
    });

    return Scaffold(
      appBar: AppBar(
        automaticallyImplyLeading: false,
        title: const Text('陪伴问答'),
      ),
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            Expanded(
              child: ListView(
                controller: _scroll,
                padding: const EdgeInsets.fromLTRB(
                  AppSpacing.xl,
                  AppSpacing.lg,
                  AppSpacing.xl,
                  AppSpacing.xxl,
                ),
                children: [
                  const AppPageHeader(
                    eyebrow: '陪伴与指引',
                    title: '想问什么？',
                    subtitle: '可以询问提醒、社区联系和日常操作。',
                    elder: true,
                  ),
                  const SizedBox(height: AppSpacing.lg),
                  const AppInlineNotice(
                    message: 'AI 仅作辅助，核心求助由安全规则和人工处理',
                    icon: LucideIcons.shieldAlert,
                    tone: AppNoticeTone.warning,
                    elder: true,
                  ),
                  const SizedBox(height: AppSpacing.xxl),
                  if (state.messages.isEmpty) ...[
                    const AppSectionHeading(title: '常见问题', elder: true),
                    const SizedBox(height: AppSpacing.md),
                    for (final question in const [
                      '怎么查看今天的提醒？',
                      '身体不舒服怎么办？',
                      '怎么联系社区？',
                    ]) ...[
                      LargeActionButton(
                        label: question,
                        semanticLabel: '询问：$question',
                        icon: LucideIcons.messageCircleQuestion,
                        outlined: true,
                        onPressed: state.isSending
                            ? null
                            : () => controller.send(question),
                      ),
                      const SizedBox(height: AppSpacing.md),
                    ],
                  ],
                  if (state.messages.isNotEmpty) ...[
                    const SizedBox(height: AppSpacing.xl),
                    const AppSectionHeading(title: '对话', elder: true),
                    const SizedBox(height: AppSpacing.lg),
                  ],
                  for (final (index, message) in state.messages.indexed)
                    Padding(
                      padding: const EdgeInsets.only(bottom: AppSpacing.md),
                      child: _MessageBubble(
                        key: index == state.messages.length - 1
                            ? _latestMessageKey
                            : null,
                        message: message,
                      ),
                    ),
                  if (state.serviceRequestDraft case final draft?) ...[
                    AiDraftConfirmationCard(
                      draft: draft,
                      onConfirm: state.isSending
                          ? null
                          : controller.confirmDraft,
                    ),
                    const SizedBox(height: AppSpacing.md),
                  ],
                  if (state.memoryCandidate case final memory?) ...[
                    _MemoryCandidate(
                      text: memory.generatedText,
                      onConfirm: state.isSending
                          ? null
                          : controller.confirmMemory,
                    ),
                    const SizedBox(height: AppSpacing.md),
                  ],
                  if (state.isSending)
                    const AppInlineNotice(
                      message: '正在整理回复，请稍候。',
                      icon: LucideIcons.loaderCircle,
                      tone: AppNoticeTone.info,
                      elder: true,
                      liveRegion: true,
                    ),
                ],
              ),
            ),
            Container(
              decoration: const BoxDecoration(
                color: AppColors.surface,
                border: Border(top: BorderSide(color: AppColors.line)),
              ),
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.xl,
                AppSpacing.md,
                AppSpacing.xl,
                AppSpacing.lg,
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  TextField(
                    controller: _input,
                    minLines: 1,
                    maxLines: 3,
                    textInputAction: TextInputAction.send,
                    onSubmitted: (_) => _send(),
                    decoration: const InputDecoration(
                      labelText: '输入想问的内容',
                      prefixIcon: Icon(LucideIcons.messageCircle),
                    ),
                  ),
                  const SizedBox(height: AppSpacing.md),
                  LargeActionButton(
                    label: '发送',
                    semanticLabel: '发送陪伴问答内容',
                    icon: LucideIcons.send,
                    onPressed: state.isSending ? null : _send,
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  LargeActionButton(
                    label: '朗读回复',
                    semanticLabel: '朗读最新回复',
                    icon: LucideIcons.volume2,
                    outlined: true,
                    onPressed: ttsEnabled && latestReply != null
                        ? () => _tts.speak(latestReply.text)
                        : null,
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _MessageBubble extends StatelessWidget {
  const _MessageBubble({super.key, required this.message});

  final ElderChatMessage message;

  @override
  Widget build(BuildContext context) {
    final fromElder = message.fromElder;
    return Align(
      alignment: fromElder ? Alignment.centerRight : Alignment.centerLeft,
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 440),
        child: Column(
          crossAxisAlignment: fromElder
              ? CrossAxisAlignment.end
              : CrossAxisAlignment.start,
          children: [
            Text(
              fromElder ? '我' : '陪伴回复',
              style: AppTextStyles.secondary.copyWith(
                color: AppColors.ink,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: AppSpacing.xs),
            Container(
              padding: const EdgeInsets.symmetric(
                horizontal: AppSpacing.lg,
                vertical: AppSpacing.md,
              ),
              decoration: BoxDecoration(
                color: fromElder ? AppColors.primary : AppColors.surface,
                borderRadius: AppRadius.lgAll,
                border: fromElder ? null : Border.all(color: AppColors.line),
              ),
              child: Text(
                message.text,
                style: AppTextStyles.secondary.copyWith(
                  color: fromElder ? AppColors.surface : AppColors.inkStrong,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _MemoryCandidate extends StatelessWidget {
  const _MemoryCandidate({required this.text, required this.onConfirm});

  final String text;
  final VoidCallback? onConfirm;

  @override
  Widget build(BuildContext context) {
    return AppStatusPanel(
      icon: LucideIcons.brain,
      title: '记忆候选',
      description: text,
      tone: AppNoticeTone.info,
      elder: true,
      child: LargeActionButton(
        label: '确认记忆',
        semanticLabel: '确认保存这条 AI 记忆',
        icon: LucideIcons.check,
        outlined: true,
        onPressed: onConfirm,
      ),
    );
  }
}
