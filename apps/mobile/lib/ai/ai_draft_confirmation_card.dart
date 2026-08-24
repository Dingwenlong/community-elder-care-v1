import 'package:flutter/material.dart';

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
    return DecoratedBox(
      decoration: BoxDecoration(
        color: const Color(0xFFFFF4E5),
        border: Border.all(color: const Color(0xFF9A5A00)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              'AI 草稿',
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 8),
            Text(draft.generatedText, style: const TextStyle(fontSize: 18)),
            const SizedBox(height: 12),
            FilledButton(onPressed: onConfirm, child: const Text('确认提交')),
          ],
        ),
      ),
    );
  }
}
