import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../../core/api/api_problem.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import 'family_event_list_page.dart';

class FamilyEventDetailPage extends ConsumerWidget {
  const FamilyEventDetailPage({super.key, required this.eventId});

  final String eventId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final event = ref.watch(familyEventDetailProvider(eventId));
    return Scaffold(
      appBar: AppBar(title: const Text('事件进展')),
      body: SafeArea(
        child: event.when(
          data: (item) => ListView(
            padding: const EdgeInsets.fromLTRB(
              AppSpacing.xl,
              AppSpacing.lg,
              AppSpacing.xl,
              AppSpacing.huge,
            ),
            children: [
              Container(height: 6, color: statusColor(item.status)),
              const SizedBox(height: AppSpacing.xxl),
              AppPageHeader(
                eyebrow: '照料事件摘要',
                title: item.summary,
                subtitle: '当前仅展示已授权的处理进展。',
              ),
              const SizedBox(height: AppSpacing.xxl),
              const AppSectionHeading(title: '当前进展'),
              const SizedBox(height: AppSpacing.md),
              _ProgressTimeline(status: item.status),
              const SizedBox(height: AppSpacing.xxl),
              const AppInlineNotice(
                message: '页面不展示详细住址、内部责任队列、原始备注或原始 AI 内容。',
                icon: LucideIcons.lockKeyhole,
                tone: AppNoticeTone.info,
              ),
            ],
          ),
          error: (error, stackTrace) => ListView(
            padding: const EdgeInsets.all(AppSpacing.xl),
            children: [
              AppStatusPanel(
                icon: error is ApiProblem && error.code == 'CONSENT_REQUIRED'
                    ? LucideIcons.shieldX
                    : LucideIcons.cloudOff,
                title: error is ApiProblem && error.code == 'CONSENT_REQUIRED'
                    ? '老人已撤回此项授权'
                    : '事件摘要暂时无法加载',
                description: '返回事件列表后下拉可重新读取。',
                tone: AppNoticeTone.warning,
              ),
            ],
          ),
          loading: () => const Padding(
            padding: EdgeInsets.all(AppSpacing.xl),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                AppSkeleton(height: 6),
                SizedBox(height: AppSpacing.xxl),
                AppSkeleton(height: 42, width: 220),
                SizedBox(height: AppSpacing.md),
                AppSkeleton(height: 120),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _ProgressTimeline extends StatelessWidget {
  const _ProgressTimeline({required this.status});

  final String status;

  @override
  Widget build(BuildContext context) {
    final color = statusColor(status);
    return Container(
      color: AppColors.surface,
      padding: const EdgeInsets.all(AppSpacing.lg),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Column(
            children: [
              Container(
                width: 20,
                height: 20,
                decoration: BoxDecoration(
                  color: color,
                  shape: BoxShape.circle,
                  border: Border.all(color: AppColors.surface, width: 4),
                ),
              ),
              Container(width: 2, height: 56, color: AppColors.lineStrong),
              Container(
                width: 14,
                height: 14,
                decoration: BoxDecoration(
                  color: AppColors.surface,
                  shape: BoxShape.circle,
                  border: Border.all(color: AppColors.lineStrong, width: 2),
                ),
              ),
            ],
          ),
          const SizedBox(width: AppSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  statusLabel(status),
                  style: AppTextStyles.sectionTitle.copyWith(color: color),
                ),
                const SizedBox(height: AppSpacing.xs),
                Text(
                  '进行中',
                  style: AppTextStyles.caption.copyWith(color: color),
                ),
                const SizedBox(height: AppSpacing.xxl),
                const Text('后续进展', style: AppTextStyles.sectionTitle),
                const SizedBox(height: AppSpacing.xs),
                const Text('社区完成处理后会更新授权摘要。', style: AppTextStyles.bodySmall),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
