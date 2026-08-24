import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../ai/ai_memory_controller.dart';
import '../../auth/session_controller.dart';

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
      appBar: AppBar(title: const Text('老人设置')),
      body: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          const _SettingsSection(
            title: '应急联系人（演示）',
            child: Text(
              '演示联系人01 · 子女 · 199****0001',
              style: TextStyle(fontSize: 18),
            ),
          ),
          const _SettingsSection(
            title: '授权摘要',
            child: Text(
              '已授权家属查看：近期状态、事件摘要、探访摘要、提醒完成情况。',
              style: TextStyle(fontSize: 18),
            ),
          ),
          _SettingsSection(
            title: 'AI 记忆',
            child: memories.isLoading
                ? const LinearProgressIndicator()
                : memories.memories.isEmpty
                ? Text(
                    memories.errorMessage ?? '暂无已确认的 AI 记忆。',
                    style: const TextStyle(fontSize: 18),
                  )
                : Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      for (final memory in memories.memories) ...[
                        Text(
                          memory.generatedText,
                          style: const TextStyle(fontSize: 18),
                        ),
                        const SizedBox(height: 6),
                        OutlinedButton(
                          onPressed: () => ref
                              .read(aiMemoryControllerProvider.notifier)
                              .delete(memory.id),
                          child: const Text('删除记忆'),
                        ),
                        const SizedBox(height: 10),
                      ],
                    ],
                  ),
          ),
          _SettingsSection(
            title: '字体大小',
            child: SegmentedButton<double>(
              segments: const [
                ButtonSegment(value: 1, label: Text('标准')),
                ButtonSegment(value: 1.15, label: Text('较大')),
                ButtonSegment(value: 1.3, label: Text('特大')),
              ],
              selected: {fontScale},
              onSelectionChanged: (selection) =>
                  ref.read(elderFontScaleProvider.notifier).state =
                      selection.single,
            ),
          ),
          _SettingsSection(
            title: '文字转语音',
            child: SwitchListTile(
              contentPadding: EdgeInsets.zero,
              title: const Text('允许手动朗读提醒和固定回复'),
              subtitle: const Text('只在点击朗读按钮后发声，不录音。'),
              value: ttsEnabled,
              onChanged: (value) =>
                  ref.read(elderTtsEnabledProvider.notifier).state = value,
            ),
          ),
          const DecoratedBox(
            decoration: BoxDecoration(
              color: Color(0xFFE8F1FB),
              border: Border.fromBorderSide(
                BorderSide(color: Color(0xFF7AA7D8)),
              ),
            ),
            child: Padding(
              padding: EdgeInsets.all(12),
              child: Text('演示模式', style: TextStyle(fontSize: 18)),
            ),
          ),
          const SizedBox(height: 16),
          OutlinedButton(
            onPressed: () => ref
                .read(sessionControllerProvider.notifier)
                .switchDemoAccount(),
            child: const Text('切换演示账号'),
          ),
        ],
      ),
    );
  }
}

class _SettingsSection extends StatelessWidget {
  const _SettingsSection({required this.title, required this.child});

  final String title;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(
            title,
            style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 8),
          child,
        ],
      ),
    );
  }
}
