import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../../core/widgets/delivery_status_banner.dart';
import '../../core/widgets/large_action_button.dart';
import 'help_request_controller.dart';

class HelpCategoryPage extends ConsumerWidget {
  const HelpCategoryPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(helpRequestControllerProvider);
    final controller = ref.read(helpRequestControllerProvider.notifier);
    final selected = state.selected;
    return Scaffold(
      appBar: AppBar(title: const Text('我需要帮助')),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.fromLTRB(
            AppSpacing.xl,
            AppSpacing.lg,
            AppSpacing.xl,
            AppSpacing.huge,
          ),
          children: selected == null
              ? _categoryChoices(controller)
              : _requestDetail(selected, state, controller),
        ),
      ),
    );
  }

  List<Widget> _categoryChoices(HelpRequestController controller) => [
    const AppPageHeader(
      eyebrow: '先选择情况',
      title: '需要哪种帮助？',
      subtitle: '选错也没关系，下一步还会再次确认。',
      elder: true,
    ),
    const SizedBox(height: AppSpacing.lg),
    const AppInlineNotice(
      message: '电话、短信和 120 均为模拟操作，不会真实拨号。',
      icon: LucideIcons.flaskConical,
      tone: AppNoticeTone.info,
      elder: true,
    ),
    const SizedBox(height: AppSpacing.xxl),
    LayoutBuilder(
      builder: (context, constraints) {
        const gap = AppSpacing.md;
        final tileWidth = (constraints.maxWidth - gap) / 2;
        return Wrap(
          spacing: gap,
          runSpacing: gap,
          children: [
            for (final category in HelpCategory.values)
              SizedBox(
                width: tileWidth,
                child: _HelpCategoryTile(
                  category: category,
                  onTap: () => controller.select(category),
                ),
              ),
          ],
        );
      },
    ),
  ];

  List<Widget> _requestDetail(
    HelpCategory category,
    HelpRequestState state,
    HelpRequestController controller,
  ) {
    final urgent = category == HelpCategory.emergency;
    return [
      AppPageHeader(
        eyebrow: urgent ? '紧急求助' : '社区请求',
        title: urgent ? '确认发送紧急求助' : category.label,
        subtitle: urgent ? '确认后系统会把模拟求助发送给社区。' : category.summary,
        elder: true,
      ),
      const SizedBox(height: AppSpacing.xxl),
      if (category == HelpCategory.emergency ||
          category == HelpCategory.unwell) ...[
        const AppInlineNotice(
          message: '如果能够操作，请立即呼叫身边的人。',
          icon: LucideIcons.triangleAlert,
          tone: AppNoticeTone.danger,
          elder: true,
        ),
        const SizedBox(height: AppSpacing.md),
        const AppInlineNotice(
          message: '系统正在把模拟求助发送给社区；当前不会真实拨打 120。',
          icon: LucideIcons.phoneOff,
          tone: AppNoticeTone.warning,
          elder: true,
        ),
        const SizedBox(height: AppSpacing.xxl),
      ],
      if (state.deliveryStatus == HelpDeliveryStatus.idle)
        LargeActionButton(
          label: urgent ? '确认发送' : '发送模拟请求',
          semanticLabel: urgent ? '确认发送紧急模拟求助' : '发送${category.label}模拟请求',
          icon: urgent ? LucideIcons.siren : LucideIcons.send,
          danger: urgent,
          onPressed: controller.submit,
        ),
      if (state.deliveryStatus == HelpDeliveryStatus.sending)
        const AppStatusPanel(
          icon: LucideIcons.loaderCircle,
          title: '正在发送',
          description: '请稍候，不要重复点击。',
          tone: AppNoticeTone.info,
          elder: true,
          child: LinearProgressIndicator(minHeight: 6),
        ),
      if (state.deliveryStatus == HelpDeliveryStatus.unsent)
        DeliveryStatusBanner(
          message: '尚未送达',
          delivered: false,
          onRetry: controller.retry,
        ),
      if (state.deliveryStatus == HelpDeliveryStatus.sent)
        const DeliveryStatusBanner(message: '已送达', delivered: true),
      const SizedBox(height: AppSpacing.lg),
      LargeActionButton(
        label: '返回求助类别',
        semanticLabel: '返回重新选择求助类别',
        icon: LucideIcons.arrowLeft,
        outlined: true,
        onPressed: controller.clearSelection,
      ),
    ];
  }
}

class _HelpCategoryTile extends StatelessWidget {
  const _HelpCategoryTile({required this.category, required this.onTap});

  final HelpCategory category;
  final VoidCallback onTap;

  IconData get icon => switch (category) {
    HelpCategory.emergency => LucideIcons.siren,
    HelpCategory.unwell => LucideIcons.heartPulse,
    HelpCategory.lifeService => LucideIcons.handHeart,
    HelpCategory.wantToTalk => LucideIcons.messagesSquare,
  };

  @override
  Widget build(BuildContext context) {
    final urgent = category == HelpCategory.emergency;
    final color = urgent ? AppColors.danger : AppColors.navy;
    return Semantics(
      button: true,
      label: '选择${category.label}求助',
      excludeSemantics: true,
      child: Material(
        color: urgent ? AppColors.dangerSoft : AppColors.surface,
        shape: RoundedRectangleBorder(
          borderRadius: AppRadius.lgAll,
          side: BorderSide(color: color, width: urgent ? 2 : 1),
        ),
        child: InkWell(
          onTap: onTap,
          borderRadius: AppRadius.lgAll,
          child: ConstrainedBox(
            constraints: const BoxConstraints(minHeight: 138),
            child: Padding(
              padding: const EdgeInsets.all(AppSpacing.md),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(icon, size: 46, color: color),
                  const SizedBox(height: AppSpacing.md),
                  Text(
                    category.label,
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      color: color,
                      fontSize: 20,
                      height: 1.3,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
