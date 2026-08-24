import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/widgets/delivery_status_banner.dart';
import '../../core/widgets/large_action_button.dart';
import 'elder_today_controller.dart';

class ElderHomePage extends ConsumerWidget {
  const ElderHomePage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(elderTodayControllerProvider);
    final controller = ref.read(elderTodayControllerProvider.notifier);
    final snapshot = state.snapshot;
    final checkedIn = snapshot?.hasCheckedIn ?? false;

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
            const SizedBox(height: 18),
            Text(
              _dateLabel(snapshot?.serverTime ?? DateTime.now()),
              style: const TextStyle(fontSize: 18),
            ),
            const SizedBox(height: 6),
            const Text(
              '李奶奶，早上好',
              style: TextStyle(fontSize: 28, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 8),
            const Text('今日天气：晴，27°C（演示）', style: TextStyle(fontSize: 18)),
            const SizedBox(height: 18),
            if (state.isLoading)
              const Center(child: CircularProgressIndicator())
            else if (state.errorMessage != null)
              Text(state.errorMessage!, style: const TextStyle(fontSize: 18))
            else
              Text(
                checkedIn
                    ? '今天已签到'
                    : '今天有 ${snapshot?.reminders.where((item) => item.state == 'Pending').length ?? 0} 项待办，请先确认平安。',
                style: const TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.w700,
                ),
              ),
            const SizedBox(height: 24),
            LargeActionButton(
              label: checkedIn ? '签到完成' : '我今天平安',
              semanticLabel: checkedIn ? '今天已签到' : '确认我今天平安',
              onPressed:
                  checkedIn ||
                      state.checkInDelivery == CheckInDeliveryStatus.sending
                  ? null
                  : controller.confirmSafety,
            ),
            if (state.checkInDelivery == CheckInDeliveryStatus.sent) ...[
              const SizedBox(height: 14),
              const DeliveryStatusBanner(message: '签到已送达', delivered: true),
            ],
            if (state.checkInDelivery == CheckInDeliveryStatus.unsent) ...[
              const SizedBox(height: 14),
              DeliveryStatusBanner(
                message: '签到尚未送达',
                delivered: false,
                onRetry: controller.retryCheckIn,
              ),
            ],
            const SizedBox(height: 16),
            LargeActionButton(
              label: '我需要帮助',
              semanticLabel: '打开求助类别',
              outlined: true,
              onPressed: () => context.go('/elder/help'),
            ),
            const SizedBox(height: 20),
            OutlinedButton(
              onPressed: () => context.go('/elder/reminders'),
              child: const Text('查看今日提醒'),
            ),
            const SizedBox(height: 10),
            OutlinedButton(
              onPressed: () => context.go('/elder/chat'),
              child: const Text('打开陪伴问答'),
            ),
          ],
        ),
      ),
    );
  }
}

String _dateLabel(DateTime date) => '${date.year}年${date.month}月${date.day}日';

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
          '演示模式 · 不会拨打真实电话',
          style: TextStyle(
            color: Color(0xFF173B67),
            fontSize: 18,
            fontWeight: FontWeight.w700,
          ),
        ),
      ),
    );
  }
}
