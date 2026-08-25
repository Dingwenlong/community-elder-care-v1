import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/theme/app_theme.dart';
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
        title: const Text('家属首页'),
        actions: [
          TextButton(
            onPressed: () => context.go('/family/settings'),
            child: const Text('设置'),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: statusController.refresh,
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(AppSpacing.xl),
          children: [
            const Text(
              '已授权照料摘要',
              style: TextStyle(
                fontSize: 28,
                fontWeight: FontWeight.w700,
                color: AppColors.inkStrong,
              ),
            ),
            const SizedBox(height: AppSpacing.sm),
            const Text(
              '这里只显示老人当前授权的摘要，不保存社区内部资料。',
              style: TextStyle(fontSize: 18, color: AppColors.inkMuted),
            ),
            const SizedBox(height: AppSpacing.xl),
            if (status.isLoading)
              const Center(child: CircularProgressIndicator())
            else if (status.isRevoked)
              const Text(
                '老人已撤回此项授权',
                style: TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.w700,
                  color: AppColors.danger,
                ),
              )
            else if (status.errorMessage != null)
              Text(
                status.errorMessage!,
                style: const TextStyle(fontSize: 18, color: AppColors.ink),
              )
            else if (status.snapshot case final snapshot?) ...[
              Text(
                snapshot.elderDisplayName,
                style: const TextStyle(
                  fontSize: 24,
                  fontWeight: FontWeight.w700,
                  color: AppColors.inkStrong,
                ),
              ),
              const SizedBox(height: AppSpacing.md),
              ConsentScopeCard(
                grantedFields: snapshot.grantedFields,
                expiresAt: snapshot.consentExpiresAt,
              ),
              if (snapshot.recentStatus != null)
                _SummarySection(title: '最近状态', text: snapshot.recentStatus!),
              if (snapshot.reminderSummary != null)
                _SummarySection(
                  title: '提醒完成情况',
                  text: snapshot.reminderSummary!,
                ),
              if (snapshot.careProgress != null)
                _SummarySection(title: '照料进展', text: snapshot.careProgress!),
              if (snapshot.visitSummary != null)
                _SummarySection(title: '探访摘要', text: snapshot.visitSummary!),
              if (snapshot.lastCommunityConfirmation != null)
                _SummarySection(
                  title: '社区确认',
                  text: snapshot.lastCommunityConfirmation!,
                ),
            ],
            const SizedBox(height: AppSpacing.lg),
            FilledButton(
              onPressed: report.isSending
                  ? null
                  : () => ref
                        .read(familyReportControllerProvider.notifier)
                        .report(),
              child: const Text('报告联系不上老人'),
            ),
            if (report.errorMessage != null) ...[
              const SizedBox(height: AppSpacing.md),
              Text(
                report.errorMessage!,
                style: const TextStyle(color: AppColors.danger),
              ),
            ],
            if (report.event case final event?) ...[
              const SizedBox(height: AppSpacing.lg),
              _FamilyEventCard(event: event),
            ],
            const SizedBox(height: AppSpacing.md),
            OutlinedButton(
              onPressed: statusController.refresh,
              child: const Text('刷新授权状态'),
            ),
            const SizedBox(height: AppSpacing.sm),
            OutlinedButton(
              onPressed: () => context.go('/family/events'),
              child: const Text('查看照料事件'),
            ),
            const SizedBox(height: AppSpacing.sm),
            OutlinedButton(
              onPressed: () => context.go('/family/records'),
              child: const Text('查看照料记录'),
            ),
          ],
        ),
      ),
    );
  }
}

class _SummarySection extends StatelessWidget {
  const _SummarySection({required this.title, required this.text});

  final String title;
  final String text;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      margin: const EdgeInsets.only(top: AppSpacing.lg),
      padding: const EdgeInsets.all(AppSpacing.lg),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: AppRadius.lgAll,
        boxShadow: AppShadows.sm,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: const TextStyle(
              fontSize: 20,
              fontWeight: FontWeight.w600,
              color: AppColors.inkStrong,
            ),
          ),
          const SizedBox(height: AppSpacing.xs),
          Text(
            text,
            style: const TextStyle(
              fontSize: 18,
              height: 1.5,
              color: AppColors.ink,
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
    return Container(
      padding: const EdgeInsets.all(AppSpacing.lg),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: AppRadius.lgAll,
        border: Border.all(color: AppColors.warning),
        boxShadow: AppShadows.sm,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            event.summary,
            style: const TextStyle(
              fontSize: 20,
              fontWeight: FontWeight.w700,
              color: AppColors.inkStrong,
            ),
          ),
          const SizedBox(height: AppSpacing.xs),
          const Text('来源：家属上报', style: TextStyle(color: AppColors.ink)),
          const Text('级别：需要确认', style: TextStyle(color: AppColors.ink)),
          const Text('状态：等待社区确认', style: TextStyle(color: AppColors.ink)),
        ],
      ),
    );
  }
}
