import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_tts/flutter_tts.dart';

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
          padding: const EdgeInsets.all(20),
          children: [
            const DecoratedBox(
              decoration: BoxDecoration(
                color: Color(0xFFFFF4E5),
                border: Border.fromBorderSide(
                  BorderSide(color: Color(0xFF9A5A00)),
                ),
              ),
              child: Padding(
                padding: EdgeInsets.all(14),
                child: Text(
                  'AI 当前不可用，核心求助功能仍可使用',
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
                ),
              ),
            ),
            const SizedBox(height: 18),
            const Text(
              '常见问题',
              style: TextStyle(fontSize: 22, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 10),
            Wrap(
              spacing: 8,
              runSpacing: 8,
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
            const SizedBox(height: 18),
            for (final message in state.messages)
              Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: Text(
                  '${message.fromElder ? '我' : '固定回复'}：${message.text}',
                  style: const TextStyle(fontSize: 18),
                ),
              ),
            TextField(
              controller: _input,
              minLines: 1,
              maxLines: 3,
              decoration: const InputDecoration(labelText: '输入想问的内容'),
            ),
            const SizedBox(height: 10),
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
            const SizedBox(height: 10),
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
