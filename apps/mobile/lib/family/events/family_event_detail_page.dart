import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'family_event_list_page.dart';

class FamilyEventDetailPage extends ConsumerWidget {
  const FamilyEventDetailPage({super.key, required this.eventId});

  final String eventId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Scaffold(
      appBar: AppBar(title: const Text('事件进展')),
      body: FutureBuilder(
        future: ref.read(familyEventQueryGatewayProvider).get(eventId),
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return const Center(child: CircularProgressIndicator());
          }
          if (!snapshot.hasData) {
            return const Center(child: Text('事件摘要暂时无法加载。'));
          }
          final event = snapshot.data!;
          return ListView(
            padding: const EdgeInsets.all(20),
            children: [
              Text(
                event.summary,
                style: const TextStyle(
                  fontSize: 26,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 14),
              Text(
                _safeProgress(event.status),
                style: const TextStyle(fontSize: 19),
              ),
              const SizedBox(height: 18),
              const Text('页面不展示详细住址、内部责任队列、原始备注或原始 AI 内容。'),
            ],
          );
        },
      ),
    );
  }
}

String _safeProgress(String status) => switch (status) {
  'PendingConfirmation' => '社区正在电话确认',
  'FollowUpPending' => '已安排次日回访',
  'Closed' => '本次照料已完成',
  _ => '社区正在跟进',
};
