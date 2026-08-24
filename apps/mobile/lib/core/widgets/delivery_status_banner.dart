import 'package:flutter/material.dart';

class DeliveryStatusBanner extends StatelessWidget {
  const DeliveryStatusBanner({
    super.key,
    required this.message,
    required this.delivered,
    this.onRetry,
  });

  final String message;
  final bool delivered;
  final VoidCallback? onRetry;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      liveRegion: true,
      label: message,
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: delivered ? const Color(0xFFE8F5E9) : const Color(0xFFFFF4E5),
          border: Border.all(
            color: delivered
                ? const Color(0xFF2E7D32)
                : const Color(0xFF9A5A00),
          ),
        ),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Row(
            children: [
              Expanded(
                child: Text(
                  message,
                  style: const TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ),
              if (onRetry != null)
                TextButton(onPressed: onRetry, child: const Text('重新发送')),
            ],
          ),
        ),
      ),
    );
  }
}
