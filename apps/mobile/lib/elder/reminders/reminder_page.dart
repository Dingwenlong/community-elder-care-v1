import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_tts/flutter_tts.dart';

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
        padding: const EdgeInsets.all(20),
        itemCount: reminders.length,
        separatorBuilder: (context, index) => const SizedBox(height: 12),
        itemBuilder: (context, index) {
          final reminder = reminders[index];
          return DecoratedBox(
            decoration: BoxDecoration(
              color: Colors.white,
              border: Border.all(color: const Color(0xFFB7C1CE)),
            ),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    reminder.label,
                    style: const TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 10),
                  Text('状态：${_stateLabel(reminder.state)}'),
                  const SizedBox(height: 12),
                  OutlinedButton(
                    onPressed: ttsEnabled
                        ? () => _tts.speak(reminder.label)
                        : null,
                    child: const Text('朗读提醒'),
                  ),
                  if (reminder.state != 'Completed') ...[
                    const SizedBox(height: 8),
                    FilledButton(
                      onPressed: () => ref
                          .read(elderTodayControllerProvider.notifier)
                          .completeReminder(reminder),
                      child: const Text('已完成'),
                    ),
                    const SizedBox(height: 8),
                    OutlinedButton(
                      onPressed: () => ref
                          .read(elderTodayControllerProvider.notifier)
                          .snoozeReminder(reminder),
                      child: const Text('稍后提醒'),
                    ),
                  ],
                ],
              ),
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
