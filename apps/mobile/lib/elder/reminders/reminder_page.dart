import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_tts/flutter_tts.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../../core/widgets/large_action_button.dart';
import '../home/elder_today_controller.dart';
import '../settings/elder_settings_page.dart';

class ReminderPage extends ConsumerStatefulWidget {
  const ReminderPage({super.key});

  @override
  ConsumerState<ReminderPage> createState() => _ReminderPageState();
}

class _ReminderPageState extends ConsumerState<ReminderPage> {
  final _tts = FlutterTts();

  @override
  void dispose() {
    _tts.stop();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(elderTodayControllerProvider);
    final controller = ref.read(elderTodayControllerProvider.notifier);
    final reminders = [...?state.snapshot?.reminders]
      ..sort((left, right) {
        final leftDone = left.state == 'Completed';
        final rightDone = right.state == 'Completed';
        if (leftDone != rightDone) return leftDone ? 1 : -1;
        final leftTime = left.dueAt ?? DateTime(9999);
        final rightTime = right.dueAt ?? DateTime(9999);
        return leftTime.compareTo(rightTime);
      });
    final ttsEnabled = ref.watch(elderTtsEnabledProvider);

    return Scaffold(
      appBar: AppBar(
        automaticallyImplyLeading: false,
        title: const Text('今日提醒'),
      ),
      body: SafeArea(
        bottom: false,
        child: RefreshIndicator(
          onRefresh: controller.load,
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.fromLTRB(
              AppSpacing.xl,
              AppSpacing.lg,
              AppSpacing.xl,
              AppSpacing.huge,
            ),
            children: [
              const AppPageHeader(
                eyebrow: '今日安排',
                title: '提醒',
                subtitle: '按时间查看事项，完成后可直接标记。',
                elder: true,
              ),
              const SizedBox(height: AppSpacing.xxl),
              if (state.isLoading) ...[
                const AppSkeleton(height: 96),
                const SizedBox(height: AppSpacing.md),
                const AppSkeleton(height: 96),
              ] else if (state.errorMessage != null)
                AppStatusPanel(
                  icon: LucideIcons.cloudOff,
                  title: '提醒暂时无法加载',
                  description: state.errorMessage,
                  tone: AppNoticeTone.danger,
                  elder: true,
                  child: LargeActionButton(
                    label: '重新读取',
                    semanticLabel: '重新读取今日提醒',
                    icon: LucideIcons.refreshCw,
                    outlined: true,
                    onPressed: controller.load,
                  ),
                )
              else if (reminders.isEmpty)
                const AppInlineNotice(
                  message: '今天没有提醒，好好休息。',
                  icon: LucideIcons.sun,
                  tone: AppNoticeTone.success,
                  elder: true,
                )
              else
                for (var index = 0; index < reminders.length; index++) ...[
                  _ReminderItem(
                    reminder: reminders[index],
                    ttsEnabled: ttsEnabled,
                    onRead: () => _tts.speak(reminders[index].label),
                    onComplete: () =>
                        controller.completeReminder(reminders[index]),
                    onSnooze: () => controller.snoozeReminder(reminders[index]),
                  ),
                  if (index < reminders.length - 1)
                    const SizedBox(height: AppSpacing.lg),
                ],
            ],
          ),
        ),
      ),
    );
  }
}

class _ReminderItem extends StatelessWidget {
  const _ReminderItem({
    required this.reminder,
    required this.ttsEnabled,
    required this.onRead,
    required this.onComplete,
    required this.onSnooze,
  });

  final TodayReminder reminder;
  final bool ttsEnabled;
  final VoidCallback onRead;
  final VoidCallback onComplete;
  final VoidCallback onSnooze;

  @override
  Widget build(BuildContext context) {
    final completed = reminder.state == 'Completed';
    final accent = completed ? AppColors.success : AppColors.accentWarmStrong;
    final dueAt = reminder.dueAt;
    final timeLabel = dueAt == null
        ? '未设具体时间'
        : '${dueAt.hour.toString().padLeft(2, '0')}:${dueAt.minute.toString().padLeft(2, '0')}';
    return Container(
      decoration: BoxDecoration(
        color: AppColors.surface,
        border: Border.all(color: AppColors.line),
        borderRadius: AppRadius.lgAll,
      ),
      clipBehavior: Clip.antiAlias,
      child: Stack(
        children: [
          Padding(
            padding: const EdgeInsets.all(AppSpacing.lg),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Icon(
                      completed ? LucideIcons.circleCheck : LucideIcons.clock,
                      size: 34,
                      color: accent,
                    ),
                    const SizedBox(width: AppSpacing.md),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(reminder.label, style: AppTextStyles.title),
                          const SizedBox(height: AppSpacing.xs),
                          Text(
                            '$timeLabel · ${_stateLabel(reminder.state)}',
                            style: AppTextStyles.secondary.copyWith(
                              color: completed
                                  ? AppColors.success
                                  : AppColors.ink,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: AppSpacing.lg),
                LargeActionButton(
                  label: '朗读提醒',
                  semanticLabel: '朗读${reminder.label}',
                  icon: LucideIcons.volume2,
                  outlined: true,
                  onPressed: ttsEnabled ? onRead : null,
                ),
                if (!completed) ...[
                  const SizedBox(height: AppSpacing.md),
                  LargeActionButton(
                    label: '已完成',
                    semanticLabel: '标记${reminder.label}已完成',
                    icon: LucideIcons.check,
                    onPressed: onComplete,
                  ),
                  const SizedBox(height: AppSpacing.md),
                  LargeActionButton(
                    label: '稍后提醒',
                    semanticLabel: '稍后提醒${reminder.label}',
                    icon: LucideIcons.alarmClock,
                    outlined: true,
                    onPressed: onSnooze,
                  ),
                ],
              ],
            ),
          ),
          Positioned(
            left: 0,
            top: 0,
            bottom: 0,
            width: 6,
            child: ColoredBox(color: accent),
          ),
        ],
      ),
    );
  }
}

String _stateLabel(String state) => switch (state) {
  'Completed' => '已完成',
  'Snoozed' => '已稍后提醒',
  _ => '待处理',
};
