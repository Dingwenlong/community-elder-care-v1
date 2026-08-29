import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../../ai/ai_memory_controller.dart';
import '../../auth/session_controller.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../../core/widgets/large_action_button.dart';

final elderFontScaleProvider = StateProvider<double>((ref) => 1);
final elderTtsEnabledProvider = StateProvider<bool>((ref) => true);

class ElderSettingsPage extends ConsumerWidget {
  const ElderSettingsPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final fontScale = ref.watch(elderFontScaleProvider);
    final ttsEnabled = ref.watch(elderTtsEnabledProvider);
    final memories = ref.watch(aiMemoryControllerProvider);
    return Scaffold(
      appBar: AppBar(
        automaticallyImplyLeading: false,
        title: const Text('老人设置'),
      ),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.xl,
          AppSpacing.lg,
          AppSpacing.xl,
          AppSpacing.huge,
        ),
        children: [
          Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const AppPageHeader(
                eyebrow: '我的',
                title: '设置',
                subtitle: '调整字号、朗读和已确认的个人资料。',
                elder: true,
              ),
              const SizedBox(height: AppSpacing.xxl),
              const _SettingsSection(
                icon: LucideIcons.phoneCall,
                title: '应急联系人',
                child: Text(
                  '李女士 · 子女 · 199****0001',
                  style: AppTextStyles.body,
                ),
              ),
              const _SettingsSection(
                icon: LucideIcons.shieldCheck,
                title: '授权摘要',
                child: Text(
                  '已授权家属查看：近期状态、事件摘要、探访摘要、提醒完成情况。',
                  style: AppTextStyles.body,
                ),
              ),
              _SettingsSection(
                icon: LucideIcons.brain,
                title: 'AI 记忆',
                child: memories.isLoading
                    ? const Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          AppSkeleton(height: 24),
                          SizedBox(height: AppSpacing.md),
                          AppSkeleton(height: 64),
                        ],
                      )
                    : memories.memories.isEmpty
                    ? AppInlineNotice(
                        message: memories.errorMessage ?? '暂无已确认的 AI 记忆。',
                        icon: LucideIcons.brain,
                        tone: memories.errorMessage == null
                            ? AppNoticeTone.info
                            : AppNoticeTone.warning,
                        elder: true,
                      )
                    : Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          for (
                            var index = 0;
                            index < memories.memories.length;
                            index++
                          ) ...[
                            Text(
                              memories.memories[index].generatedText,
                              style: AppTextStyles.body,
                            ),
                            const SizedBox(height: AppSpacing.md),
                            LargeActionButton(
                              label: '删除记忆',
                              semanticLabel: '删除这条 AI 记忆',
                              icon: LucideIcons.trash2,
                              outlined: true,
                              danger: true,
                              onPressed: () => ref
                                  .read(aiMemoryControllerProvider.notifier)
                                  .delete(memories.memories[index].id),
                            ),
                            if (index < memories.memories.length - 1)
                              const Divider(height: AppSpacing.huge),
                          ],
                        ],
                      ),
              ),
              _SettingsSection(
                icon: LucideIcons.textCursorInput,
                title: '字体大小',
                description: '系统字号更大时，以系统设置为准。',
                child: SegmentedButton<double>(
                  showSelectedIcon: false,
                  segments: const [
                    ButtonSegment(value: 1, label: Text('标准')),
                    ButtonSegment(value: 1.3, label: Text('大')),
                    ButtonSegment(value: 1.6, label: Text('特大')),
                  ],
                  selected: {fontScale},
                  onSelectionChanged: (selection) =>
                      ref.read(elderFontScaleProvider.notifier).state =
                          selection.single,
                ),
              ),
              _SettingsSection(
                icon: LucideIcons.volume2,
                title: '文字转语音',
                child: Material(
                  color: AppColors.surface,
                  child: SwitchListTile(
                    contentPadding: EdgeInsets.zero,
                    title: const Text(
                      '允许手动朗读提醒和固定回复',
                      style: AppTextStyles.body,
                    ),
                    subtitle: Text(
                      '只在点击朗读按钮后发声，不录音。',
                      style: AppTextStyles.secondary.copyWith(
                        color: AppColors.ink,
                      ),
                    ),
                    value: ttsEnabled,
                    onChanged: (value) =>
                        ref.read(elderTtsEnabledProvider.notifier).state =
                            value,
                  ),
                ),
              ),
              const SizedBox(height: AppSpacing.lg),
              const AppInlineNotice(
                message: '当前账号',
                icon: LucideIcons.userRound,
                tone: AppNoticeTone.info,
                elder: true,
              ),
              const SizedBox(height: AppSpacing.lg),
              LargeActionButton(
                label: '切换账号',
                semanticLabel: '退出当前账号并返回登录',
                icon: LucideIcons.logOut,
                outlined: true,
                onPressed: () => ref
                    .read(sessionControllerProvider.notifier)
                    .switchDemoAccount(),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _SettingsSection extends StatelessWidget {
  const _SettingsSection({
    required this.icon,
    required this.title,
    required this.child,
    this.description,
  });

  final IconData icon;
  final String title;
  final String? description;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.xxl),
      decoration: const BoxDecoration(
        border: Border(bottom: BorderSide(color: AppColors.line)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Icon(icon, size: 32, color: AppColors.primary),
              const SizedBox(width: AppSpacing.md),
              Expanded(child: Text(title, style: AppTextStyles.title)),
            ],
          ),
          if (description != null) ...[
            const SizedBox(height: AppSpacing.sm),
            Text(
              description!,
              style: AppTextStyles.secondary.copyWith(color: AppColors.ink),
            ),
          ],
          const SizedBox(height: AppSpacing.lg),
          child,
        ],
      ),
    );
  }
}
