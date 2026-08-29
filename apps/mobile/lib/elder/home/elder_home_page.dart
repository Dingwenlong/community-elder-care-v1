import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
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
    final pendingReminders =
        snapshot?.reminders
            .where((item) => item.state == 'Pending')
            .toList(growable: false) ??
        const <TodayReminder>[];

    return Scaffold(
      appBar: AppBar(
        automaticallyImplyLeading: false,
        title: const Text('老人首页'),
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
              const _DemoModeBanner(),
              const SizedBox(height: AppSpacing.xxl),
              AppPageHeader(
                eyebrow: _dateLabel(snapshot?.serverTime ?? DateTime.now()),
                title: '李奶奶，早上好',
                subtitle: '今日天气：晴，27°C',
                elder: true,
              ),
              const SizedBox(height: AppSpacing.xxl),
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
                const SizedBox(height: AppSpacing.huge),
                _ReminderSection(reminders: pendingReminders),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

String _dateLabel(DateTime date) => '${date.year}年${date.month}月${date.day}日';

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
        icon: LucideIcons.heartPulse,
        iconColor: AppColors.navy,
        title: '正在读取今日资料…',
        titleColor: AppColors.navy,
        child: Padding(
          padding: EdgeInsets.only(top: AppSpacing.sm),
          child: LinearProgressIndicator(minHeight: 6),
        ),
      );
    }

    if (state.errorMessage != null) {
      return StatusCard(
        backgroundColor: AppColors.dangerSoft,
        icon: LucideIcons.cloudOff,
        iconColor: AppColors.danger,
        title: '暂时连不上社区',
        titleColor: AppColors.danger,
        subtitle: state.errorMessage!,
        child: LargeActionButton(
          label: '重新读取',
          semanticLabel: '重新读取今日资料',
          icon: LucideIcons.refreshCw,
          outlined: true,
          onPressed: controller.load,
        ),
      );
    }

    if (checkedIn) {
      return const StatusCard(
        backgroundColor: AppColors.successSoft,
        icon: LucideIcons.circleCheck,
        iconColor: AppColors.success,
        title: '今天已签到',
        titleColor: AppColors.success,
        subtitle: '社区已经收到你的平安消息，安心休息。',
        child: LargeActionButton(
          label: '签到完成',
          semanticLabel: '今天已签到',
          icon: LucideIcons.shieldCheck,
          onPressed: null,
        ),
      );
    }

    final pendingCount =
        snapshot?.reminders.where((item) => item.state == 'Pending').length ??
        0;
    return StatusCard(
      backgroundColor: AppColors.primarySoft,
      icon: LucideIcons.shieldAlert,
      iconColor: AppColors.navy,
      title: '今天还没报平安',
      titleColor: AppColors.navy,
      subtitle: '今天有 $pendingCount 项待办，请先确认平安。',
      child: LargeActionButton(
        label: '我今天平安',
        semanticLabel: '确认我今天平安',
        icon: LucideIcons.shieldCheck,
        onPressed: state.checkInDelivery == CheckInDeliveryStatus.sending
            ? null
            : controller.confirmSafety,
      ),
    );
  }
}

class _ReminderSection extends StatelessWidget {
  const _ReminderSection({required this.reminders});

  final List<TodayReminder> reminders;

  Color _accentOf(String label) {
    if (label.contains('药')) return AppColors.accentWarmStrong;
    if (label.contains('探访')) return AppColors.primary;
    if (label.contains('随访')) return AppColors.success;
    return AppColors.navy;
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const AppSectionHeading(
          title: '今日待办',
          description: '点开提醒，可朗读、完成或稍后处理。',
          elder: true,
        ),
        const SizedBox(height: AppSpacing.lg),
        if (reminders.isEmpty)
          const AppInlineNotice(
            message: '今天没有待办，好好休息。',
            icon: LucideIcons.sun,
            tone: AppNoticeTone.success,
            elder: true,
          )
        else
          Container(
            decoration: BoxDecoration(
              color: AppColors.surface,
              border: Border.all(color: AppColors.line),
              borderRadius: AppRadius.lgAll,
            ),
            clipBehavior: Clip.antiAlias,
            child: Column(
              children: [
                for (var index = 0; index < reminders.length; index++) ...[
                  _ReminderRow(
                    reminder: reminders[index],
                    accent: _accentOf(reminders[index].label),
                  ),
                  if (index < reminders.length - 1)
                    const Divider(height: 1, indent: 20, endIndent: 20),
                ],
              ],
            ),
          ),
      ],
    );
  }
}

class _ReminderRow extends StatelessWidget {
  const _ReminderRow({required this.reminder, required this.accent});

  final TodayReminder reminder;
  final Color accent;

  @override
  Widget build(BuildContext context) {
    final dueAt = reminder.dueAt;
    final time = dueAt == null
        ? '今日'
        : '${dueAt.hour.toString().padLeft(2, '0')}:${dueAt.minute.toString().padLeft(2, '0')}';
    return Material(
      color: AppColors.surface,
      child: InkWell(
        onTap: () => context.go('/elder/reminders'),
        child: ConstrainedBox(
          constraints: const BoxConstraints(minHeight: 76),
          child: Row(
            children: [
              Container(width: 5, height: 76, color: accent),
              const SizedBox(width: AppSpacing.lg),
              Icon(LucideIcons.clock, color: accent, size: 30),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: Padding(
                  padding: const EdgeInsets.symmetric(vertical: AppSpacing.md),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(reminder.label, style: AppTextStyles.body),
                      const SizedBox(height: AppSpacing.xs),
                      Text(
                        time,
                        style: AppTextStyles.secondary.copyWith(
                          color: AppColors.ink,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              const Icon(
                LucideIcons.chevronRight,
                size: 30,
                color: AppColors.inkMuted,
              ),
              const SizedBox(width: AppSpacing.lg),
            ],
          ),
        ),
      ),
    );
  }
}

class _DemoModeBanner extends StatelessWidget {
  const _DemoModeBanner();

  @override
  Widget build(BuildContext context) {
    return const AppInlineNotice(
      message: '模拟服务 · 不会真实拨号',
      icon: LucideIcons.flaskConical,
      tone: AppNoticeTone.info,
      elder: true,
    );
  }
}
