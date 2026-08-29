import 'package:flutter/material.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../core/widgets/app_page.dart';
import '../core/widgets/large_action_button.dart';
import 'ai_api_gateway.dart';

class AiDraftConfirmationCard extends StatelessWidget {
  const AiDraftConfirmationCard({
    super.key,
    required this.draft,
    required this.onConfirm,
  });

  final AiDraft draft;
  final VoidCallback? onConfirm;

  @override
  Widget build(BuildContext context) {
    return AppStatusPanel(
      icon: LucideIcons.filePenLine,
      title: 'AI 草稿',
      description: draft.generatedText,
      tone: AppNoticeTone.warning,
      elder: true,
      child: LargeActionButton(
        label: '确认提交',
        semanticLabel: '确认提交 AI 生成的服务请求草稿',
        icon: LucideIcons.send,
        onPressed: onConfirm,
      ),
    );
  }
}
