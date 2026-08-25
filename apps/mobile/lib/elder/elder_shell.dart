import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../core/outbox/outbox_sync_service.dart';

class ElderShell extends ConsumerWidget {
  const ElderShell({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final delivery = ref.watch(emergencyOutboxControllerProvider);
    return Scaffold(
      appBar: AppBar(
        title: const Text('老人首页'),
        actions: [
          TextButton(
            onPressed: () => context.go('/elder/settings'),
            child: const Text('设置'),
          ),
        ],
      ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            const _DemoModeBanner(),
            const SizedBox(height: 20),
            const Text(
              '李奶奶，早上好',
              style: TextStyle(fontSize: 28, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 8),
            const Text('请完成今天的平安确认。'),
            const SizedBox(height: 28),
            FilledButton(onPressed: () {}, child: const Text('我今天平安')),
            const SizedBox(height: 16),
            OutlinedButton(
              onPressed: delivery.isSending
                  ? null
                  : () => ref
                        .read(emergencyOutboxControllerProvider.notifier)
                        .queueEmergency(),
              style: OutlinedButton.styleFrom(
                foregroundColor: const Color(0xFF173B67),
                side: const BorderSide(color: Color(0xFF173B67), width: 2),
              ),
              child: const Text('我需要帮助'),
            ),
            if (delivery.status != EmergencyDeliveryStatus.idle) ...[
              const SizedBox(height: 16),
              Text(
                delivery.status == EmergencyDeliveryStatus.sent
                    ? '已送达'
                    : '尚未送达',
                style: const TextStyle(fontWeight: FontWeight.w700),
              ),
            ],
            if (delivery.status == EmergencyDeliveryStatus.unsent) ...[
              const SizedBox(height: 8),
              OutlinedButton(
                onPressed: delivery.isSending
                    ? null
                    : () => ref
                          .read(emergencyOutboxControllerProvider.notifier)
                          .retry(),
                child: const Text('重新发送'),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _DemoModeBanner extends StatelessWidget {
  const _DemoModeBanner();

  @override
  Widget build(BuildContext context) {
    return const DecoratedBox(
      decoration: BoxDecoration(
        color: Color(0xFFE8F1FB),
        border: Border.fromBorderSide(BorderSide(color: Color(0xFF7AA7D8))),
      ),
      child: Padding(
        padding: EdgeInsets.all(12),
        child: Text(
          '模拟服务 · 不会真实拨号',
          style: TextStyle(
            color: Color(0xFF173B67),
            fontWeight: FontWeight.w700,
          ),
        ),
      ),
    );
  }
}
