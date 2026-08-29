import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../events/family_report_controller.dart';
import '../widgets/consent_scope_card.dart';
import 'family_status_controller.dart';

class FamilyHomePage extends ConsumerWidget {
  const FamilyHomePage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final status = ref.watch(familyStatusControllerProvider);
    final report = ref.watch(familyReportControllerProvider);
    final statusController = ref.read(familyStatusControllerProvider.notifier);
    return Scaffold(
      appBar: AppBar(
        automaticallyImplyLeading: false,
        title: const Text('家属首页'),
      ),
      body: RefreshIndicator(
        onRefresh: statusController.refresh,
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
              eyebrow: '已授权照料摘要',
              title: '家属照料进展',
              subtitle: '这里只显示老人当前授权的摘要，不保存社区内部资料。',
            ),
            const SizedBox(height: AppSpacing.lg),
            OutlinedButton.icon(
              onPressed: statusController.refresh,
              icon: const Icon(LucideIcons.refreshCw),
              label: const Text('刷新授权状态'),
              style: OutlinedButton.styleFrom(
                minimumSize: const Size.fromHeight(52),
              ),
            ),
            const SizedBox(height: AppSpacing.xxl),
            if (status.isLoading) ...[
              const AppSkeleton(height: 84),
              const SizedBox(height: AppSpacing.md),
              const AppSkeleton(height: 160),
              const SizedBox(height: AppSpacing.md),
              const AppSkeleton(height: 96),
            ] else if (status.isRevoked)
              const AppStatusPanel(
                icon: LucideIcons.shieldX,
                title: '老人已撤回此项授权',
                description: '已授权摘要已从当前页面移除。',
                tone: AppNoticeTone.danger,
              )
            else if (status.errorMessage != null)
              AppStatusPanel(
                icon: LucideIcons.cloudOff,
                title: '授权摘要暂时无法加载',
                description: status.errorMessage,
                tone: AppNoticeTone.warning,
              )
            else if (status.snapshot case final snapshot?) ...[
              _ElderIdentity(name: snapshot.elderDisplayName),
              const SizedBox(height: AppSpacing.lg),
              ConsentScopeCard(
                grantedFields: snapshot.grantedFields,
                expiresAt: snapshot.consentExpiresAt,
              ),
              const SizedBox(height: AppSpacing.xxl),
              const AppSectionHeading(
                title: '平安与照料摘要',
                description: '按当前授权范围展示。',
              ),
              const SizedBox(height: AppSpacing.md),
              Container(
                decoration: BoxDecoration(
                  color: AppColors.surface,
                  border: Border.all(color: AppColors.line),
                ),
                child: Column(
                  children: [
                    if (snapshot.recentStatus != null)
                      _SummaryRow(
                        icon: LucideIcons.circleCheck,
                        title: '平安状态',
                        text: snapshot.recentStatus!,
                        color: AppColors.success,
                      ),
                    if (snapshot.reminderSummary != null)
                      _SummaryRow(
                        icon: LucideIcons.bell,
                        title: '提醒完成情况',
                        text: snapshot.reminderSummary!,
                        color: AppColors.primary,
                      ),
                    if (snapshot.careProgress != null)
                      _SummaryRow(
                        icon: LucideIcons.workflow,
                        title: '照料进展',
                        text: snapshot.careProgress!,
                        color: AppColors.primary,
                      ),
                    if (snapshot.visitSummary != null)
                      _SummaryRow(
                        icon: LucideIcons.houseHeart,
                        title: '探访摘要',
                        text: snapshot.visitSummary!,
                        color: AppColors.accentWarmStrong,
                      ),
                    if (snapshot.lastCommunityConfirmation != null)
                      _SummaryRow(
                        icon: LucideIcons.building2,
                        title: '社区确认',
                        text: snapshot.lastCommunityConfirmation!,
                        color: AppColors.navy,
                        showDivider: false,
                      ),
                  ],
                ),
              ),
            ],
            const SizedBox(height: AppSpacing.xxl),
            OutlinedButton.icon(
              onPressed: report.isSending
                  ? null
                  : () => ref
                        .read(familyReportControllerProvider.notifier)
                        .report(),
              icon: const Icon(LucideIcons.phoneOff),
              label: Text(report.isSending ? '正在报告' : '报告联系不上老人'),
              style: OutlinedButton.styleFrom(
                foregroundColor: AppColors.danger,
                side: const BorderSide(color: AppColors.danger, width: 2),
                minimumSize: const Size.fromHeight(56),
              ),
            ),
            if (report.errorMessage != null) ...[
              const SizedBox(height: AppSpacing.md),
              AppInlineNotice(
                message: report.errorMessage!,
                icon: LucideIcons.circleAlert,
                tone: AppNoticeTone.warning,
                liveRegion: true,
              ),
            ],
            if (report.event case final event?) ...[
              const SizedBox(height: AppSpacing.lg),
              _FamilyEventCard(event: event),
            ],
          ],
        ),
      ),
    );
  }
}

class _ElderIdentity extends StatelessWidget {
  const _ElderIdentity({required this.name});

  final String name;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Container(
          width: 52,
          height: 52,
          alignment: Alignment.center,
          color: AppColors.primarySoft,
          child: const Icon(
            LucideIcons.userRound,
            color: AppColors.primary,
            size: 28,
          ),
        ),
        const SizedBox(width: AppSpacing.md),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(name, style: AppTextStyles.sectionTitle),
              const SizedBox(height: AppSpacing.xs),
              const Text('家属账号', style: AppTextStyles.caption),
            ],
          ),
        ),
      ],
    );
  }
}

class _SummaryRow extends StatelessWidget {
  const _SummaryRow({
    required this.icon,
    required this.title,
    required this.text,
    required this.color,
    this.showDivider = true,
  });

  final IconData icon;
  final String title;
  final String text;
  final Color color;
  final bool showDivider;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(AppSpacing.lg),
      decoration: BoxDecoration(
        border: showDivider
            ? const Border(bottom: BorderSide(color: AppColors.line))
            : null,
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: color, size: 28),
          const SizedBox(width: AppSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(title, style: AppTextStyles.sectionTitle),
                const SizedBox(height: AppSpacing.xs),
                Text(
                  text,
                  style: AppTextStyles.bodySmall.copyWith(color: AppColors.ink),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _FamilyEventCard extends StatelessWidget {
  const _FamilyEventCard({required this.event});

  final FamilyEventSummary event;

  @override
  Widget build(BuildContext context) {
    return AppStatusPanel(
      icon: LucideIcons.phoneOff,
      title: event.summary,
      tone: AppNoticeTone.warning,
      child: const Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('来源：家属上报'),
          SizedBox(height: AppSpacing.xs),
          Text('级别：需要确认'),
          SizedBox(height: AppSpacing.xs),
          Text('状态：等待社区确认'),
        ],
      ),
    );
  }
}
