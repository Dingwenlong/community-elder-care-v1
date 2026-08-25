import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

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
          padding: const EdgeInsets.all(20),
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
      style: TextStyle(fontSize: 24, fontWeight: FontWeight.w700),
    ),
    const SizedBox(height: 8),
    const Text('求助会发送至社区，不会直接拨打电话。', style: TextStyle(fontSize: 18)),
    const SizedBox(height: 20),
    for (final category in HelpCategory.values) ...[
      LargeActionButton(
        label: category.label,
        semanticLabel: '选择${category.label}求助',
        outlined: category != HelpCategory.emergency,
        onPressed: () => controller.select(category),
      ),
      const SizedBox(height: 12),
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
        style: const TextStyle(fontSize: 26, fontWeight: FontWeight.w700),
      ),
      const SizedBox(height: 16),
      if (category == HelpCategory.emergency ||
          category == HelpCategory.unwell) ...[
        const Text(
          '如果能够操作，请立即呼叫身边的人。',
          style: TextStyle(fontSize: 20, fontWeight: FontWeight.w700),
        ),
        const SizedBox(height: 8),
        const Text(
          '系统正在把求助发送给社区；当前不会真实拨打 120。',
          style: TextStyle(fontSize: 18),
        ),
        const SizedBox(height: 18),
      ] else ...[
        Text(category.summary, style: const TextStyle(fontSize: 18)),
        const SizedBox(height: 18),
      ],
      if (state.deliveryStatus == HelpDeliveryStatus.idle)
        LargeActionButton(
          label: urgent ? '确认发送' : '发送请求',
          semanticLabel: urgent ? '确认发送紧急求助' : '发送${category.label}请求',
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
      const SizedBox(height: 16),
      OutlinedButton(
        onPressed: controller.clearSelection,
        child: const Text('返回求助类别'),
      ),
    ];
  }
}
