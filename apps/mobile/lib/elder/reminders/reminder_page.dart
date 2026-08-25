import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_tts/flutter_tts.dart';

import '../../core/theme/app_theme.dart';
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
    final reminders = state.snapshot?.reminders ?? const <TodayReminder>[];
    final ttsEnabled = ref.watch(elderTtsEnabledProvider);
    return Scaffold(
      appBar: AppBar(title: const Text('今日提醒')),
      body: ListView.separated(
        padding: const EdgeInsets.all(AppSpacing.xl),
        itemCount: reminders.length,
        separatorBuilder: (context, index) =>
            const SizedBox(height: AppSpacing.md),
        itemBuilder: (context, index) {
          final reminder = reminders[index];
          final completed = reminder.state == 'Completed';
          return Container(
            padding: const EdgeInsets.all(AppSpacing.lg),
            decoration: BoxDecoration(
              color: AppColors.surface,
              borderRadius: AppRadius.lgAll,
              border: Border(
                left: BorderSide(
                  color: completed ? AppColors.success : AppColors.accentWarm,
                  width: 4,
                ),
              ),
              boxShadow: AppShadows.sm,
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  reminder.label,
                  style: const TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.w700,
                    color: AppColors.inkStrong,
                  ),
                ),
                const SizedBox(height: AppSpacing.md),
                Text(
                  '状态：${_stateLabel(reminder.state)}',
                  style: TextStyle(
                    fontSize: 16,
                    color: completed ? AppColors.success : AppColors.inkMuted,
                  ),
                ),
                const SizedBox(height: AppSpacing.md),
                OutlinedButton(
                  onPressed: ttsEnabled
                      ? () => _tts.speak(reminder.label)
                      : null,
                  child: const Text('朗读提醒'),
                ),
                if (reminder.state != 'Completed') ...[
                  const SizedBox(height: AppSpacing.sm),
                  FilledButton(
                    onPressed: () => ref
                        .read(elderTodayControllerProvider.notifier)
                        .completeReminder(reminder),
                    child: const Text('已完成'),
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  OutlinedButton(
                    onPressed: () => ref
                        .read(elderTodayControllerProvider.notifier)
                        .snoozeReminder(reminder),
                    child: const Text('稍后提醒'),
                  ),
                ],
              ],
            ),
          );
        },
      ),
    );
  }
}

String _stateLabel(String state) => switch (state) {
  'Completed' => '已完成',
  'Snoozed' => '已稍后提醒',
  _ => '待处理',
};
