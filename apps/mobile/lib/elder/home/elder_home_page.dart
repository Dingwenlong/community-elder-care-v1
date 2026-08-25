import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/delivery_status_banner.dart';
import '../../core/widgets/large_action_button.dart';
import '../../core/widgets/status_card.dart';
import 'elder_today_controller.dart';

class ElderHomePage extends ConsumerWidget {
  const ElderHomePage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(elderTodayControllerProvider);
    final controller = ref.read(elderTodayControllerProvider.notifier);
    final snapshot = state.snapshot;
    final pendingReminders = snapshot?.reminders
            .where((item) => item.state == 'Pending')
            .toList(growable: false) ??
        const <TodayReminder>[];

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
        bottom: false,
        child: ListView(
          padding: const EdgeInsets.fromLTRB(
            AppSpacing.xl,
            AppSpacing.xl,
            AppSpacing.xl,
            AppSpacing.xxxl,
          ),
          children: [
            const _DemoModeBanner(),
            const SizedBox(height: AppSpacing.xl),
            Text(
              _dateLabel(snapshot?.serverTime ?? DateTime.now()),
              style: AppTextStyles.secondary,
            ),
            const SizedBox(height: AppSpacing.xs),
            const Text('李奶奶，早上好', style: AppTextStyles.display),
            const SizedBox(height: AppSpacing.sm),
            const Text('今日天气：晴，27°C', style: AppTextStyles.secondary),
            const SizedBox(height: AppSpacing.xl),
            _CheckInSection(state: state, controller: controller),
            if (state.checkInDelivery == CheckInDeliveryStatus.sent) ...[
              const SizedBox(height: AppSpacing.lg),
              const DeliveryStatusBanner(message: '签到已送达', delivered: true),
            ],
            if (state.checkInDelivery == CheckInDeliveryStatus.unsent) ...[
              const SizedBox(height: AppSpacing.lg),
              DeliveryStatusBanner(
                message: '签到尚未送达',
                delivered: false,
                onRetry: controller.retryCheckIn,
              ),
            ],
            if (!state.isLoading &&
                state.errorMessage == null &&
                snapshot != null) ...[
              const SizedBox(height: AppSpacing.xxxl),
              _ReminderSection(reminders: pendingReminders),
            ],
            const SizedBox(height: AppSpacing.xxxl),
            OutlinedButton(
              onPressed: () => context.go('/elder/reminders'),
              child: const Text('查看今日提醒'),
            ),
            const SizedBox(height: AppSpacing.md),
            OutlinedButton(
              onPressed: () => context.go('/elder/chat'),
              child: const Text('打开陪伴问答'),
            ),
          ],
        ),
      ),
      bottomNavigationBar: const _SosBar(),
    );
  }
}

String _dateLabel(DateTime date) => '${date.year}年${date.month}月${date.day}日';

/// 平安签到状态区：加载 / 异常 / 未签到 / 已签到 四态大卡。
class _CheckInSection extends StatelessWidget {
  const _CheckInSection({required this.state, required this.controller});

  final ElderTodayState state;
  final ElderTodayController controller;

  @override
  Widget build(BuildContext context) {
    final snapshot = state.snapshot;
    final checkedIn = snapshot?.hasCheckedIn ?? false;

    if (state.isLoading) {
      return const StatusCard(
        backgroundColor: AppColors.surface,
        icon: Icons.favorite_border,
        iconColor: AppColors.inkMuted,
        title: '正在读取今日资料…',
        titleColor: AppColors.inkMuted,
        child: Padding(
          padding: EdgeInsets.only(top: AppSpacing.sm),
          child: Center(child: CircularProgressIndicator()),
        ),
      );
    }

    if (state.errorMessage != null) {
      return StatusCard(
        backgroundColor: AppColors.dangerSoft,
        icon: Icons.cloud_off_outlined,
        iconColor: AppColors.danger,
        title: '暂时连不上社区',
        titleColor: AppColors.danger,
        subtitle: state.errorMessage!,
      );
    }

    if (checkedIn) {
      return StatusCard(
        backgroundColor: AppColors.successSoft,
        icon: Icons.check_circle_outline,
        iconColor: AppColors.success,
        title: '今天已签到',
        titleColor: AppColors.success,
        subtitle: '社区已经收到你的平安消息，安心休息。',
        child: LargeActionButton(
          label: '签到完成',
          semanticLabel: '今天已签到',
          onPressed: null,
        ),
      );
    }

    final pendingCount = snapshot?.reminders
            .where((item) => item.state == 'Pending')
            .length ??
        0;
    return StatusCard(
      backgroundColor: AppColors.dangerSoft,
      icon: Icons.notification_important_outlined,
      iconColor: AppColors.danger,
      title: '今天还没报平安',
      titleColor: AppColors.danger,
      subtitle: '今天有 $pendingCount 项待办，请先确认平安。',
      child: LargeActionButton(
        label: '我今天平安',
        semanticLabel: '确认我今天平安',
        onPressed: state.checkInDelivery == CheckInDeliveryStatus.sending
            ? null
            : controller.confirmSafety,
      ),
    );
  }
}

/// 今日待办：卡片化列表，左侧 4px 色条按类型区分。
class _ReminderSection extends StatelessWidget {
  const _ReminderSection({required this.reminders});

  final List<TodayReminder> reminders;

  Color _accentOf(String label) {
    if (label.contains('药')) return AppColors.accentWarm;
    if (label.contains('探访')) return AppColors.primary;
    if (label.contains('随访')) return AppColors.success;
    return AppColors.navy;
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text('今日待办', style: AppTextStyles.title),
        const SizedBox(height: AppSpacing.md),
        if (reminders.isEmpty)
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(AppSpacing.xxl),
            decoration: BoxDecoration(
              color: AppColors.surface,
              borderRadius: AppRadius.lgAll,
              boxShadow: AppShadows.sm,
            ),
            child: const Text(
              '今天没有待办，好好休息。',
              textAlign: TextAlign.center,
              style: AppTextStyles.body,
            ),
          )
        else
          for (final reminder in reminders) ...[
            _ReminderCard(
              reminder: reminder,
              accent: _accentOf(reminder.label),
            ),
            const SizedBox(height: AppSpacing.md),
          ],
      ],
    );
  }
}

class _ReminderCard extends StatelessWidget {
  const _ReminderCard({required this.reminder, required this.accent});

  final TodayReminder reminder;
  final Color accent;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: AppColors.surface,
      borderRadius: AppRadius.lgAll,
      child: InkWell(
        borderRadius: AppRadius.lgAll,
        onTap: () => context.go('/elder/reminders'),
        child: Container(
          constraints: const BoxConstraints(minHeight: 64),
          decoration: BoxDecoration(
            borderRadius: AppRadius.lgAll,
            border: Border(left: BorderSide(color: accent, width: 4)),
            boxShadow: AppShadows.sm,
          ),
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.lg,
            vertical: AppSpacing.md,
          ),
          child: Row(
            children: [
              Expanded(
                child: Text(reminder.label, style: AppTextStyles.body),
              ),
              const Icon(
                Icons.chevron_right,
                size: 32,
                color: AppColors.inkMuted,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// 底部常驻 SOS 求助栏：滚动时始终可见。
class _SosBar extends StatelessWidget {
  const _SosBar();

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Container(
        decoration: const BoxDecoration(
          color: AppColors.surface,
          border: Border(top: BorderSide(color: AppColors.line)),
        ),
        padding: const EdgeInsets.all(AppSpacing.lg),
        child: LargeActionButton(
          label: '我需要帮助',
          semanticLabel: '打开求助类别',
          danger: true,
          onPressed: () => context.go('/elder/help'),
        ),
      ),
    );
  }
}

class _DemoModeBanner extends StatelessWidget {
  const _DemoModeBanner();

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(AppSpacing.md),
      decoration: BoxDecoration(
        color: AppColors.primarySoft,
        borderRadius: AppRadius.smAll,
        border: Border.all(color: AppColors.primary),
      ),
      child: const Text(
        '模拟服务 · 不会真实拨号',
        style: TextStyle(
          color: AppColors.navy,
          fontSize: 18,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}
