import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
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
          padding: const EdgeInsets.all(AppSpacing.xl),
          children: selected == null
              ? _categoryChoices(controller)
              : _requestDetail(selected, state, controller),
        ),
      ),
    );
  }

  List<Widget> _categoryChoices(HelpRequestController controller) => [
    const Text(
      '请选择最接近的情况',
      style: TextStyle(
        fontSize: 24,
        fontWeight: FontWeight.w700,
        color: AppColors.inkStrong,
      ),
    ),
    const SizedBox(height: AppSpacing.sm),
    const Text(
      '电话、短信和 120 均为模拟操作，不会真实拨号。',
      style: TextStyle(fontSize: 18, color: AppColors.inkMuted),
    ),
    const SizedBox(height: AppSpacing.xl),
    for (final category in HelpCategory.values) ...[
      LargeActionButton(
        label: category.label,
        semanticLabel: '选择${category.label}求助',
        outlined: category != HelpCategory.emergency,
        danger: category == HelpCategory.emergency,
        onPressed: () => controller.select(category),
      ),
      const SizedBox(height: AppSpacing.md),
    ],
  ];

  List<Widget> _requestDetail(
    HelpCategory category,
    HelpRequestState state,
    HelpRequestController controller,
  ) {
    final urgent = category == HelpCategory.emergency;
    return [
      Text(
        urgent ? '确认发送紧急求助' : category.label,
        style: TextStyle(
          fontSize: 26,
          fontWeight: FontWeight.w700,
          color: urgent ? AppColors.danger : AppColors.inkStrong,
        ),
      ),
      const SizedBox(height: AppSpacing.lg),
      if (category == HelpCategory.emergency ||
          category == HelpCategory.unwell) ...[
        const Text(
          '如果能够操作，请立即呼叫身边的人。',
          style: TextStyle(
            fontSize: 20,
            fontWeight: FontWeight.w700,
            color: AppColors.danger,
          ),
        ),
        const SizedBox(height: AppSpacing.sm),
        const Text(
          '系统正在把模拟求助发送给社区；当前不会真实拨打 120。',
          style: TextStyle(fontSize: 18, color: AppColors.ink),
        ),
        const SizedBox(height: AppSpacing.xl),
      ] else ...[
        Text(
          category.summary,
          style: const TextStyle(fontSize: 18, color: AppColors.ink),
        ),
        const SizedBox(height: AppSpacing.xl),
      ],
      if (state.deliveryStatus == HelpDeliveryStatus.idle)
        LargeActionButton(
          label: urgent ? '确认发送' : '发送模拟请求',
          semanticLabel: urgent ? '确认发送紧急模拟求助' : '发送${category.label}模拟请求',
          danger: urgent,
          onPressed: controller.submit,
        ),
      if (state.deliveryStatus == HelpDeliveryStatus.sending)
        const Center(child: CircularProgressIndicator()),
      if (state.deliveryStatus == HelpDeliveryStatus.unsent)
        DeliveryStatusBanner(
          message: '尚未送达',
          delivered: false,
          onRetry: controller.retry,
        ),
      if (state.deliveryStatus == HelpDeliveryStatus.sent)
        const DeliveryStatusBanner(message: '已送达', delivered: true),
      const SizedBox(height: AppSpacing.lg),
      OutlinedButton(
        onPressed: controller.clearSelection,
        child: const Text('返回求助类别'),
      ),
    ];
  }
}
