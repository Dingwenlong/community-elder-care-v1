import 'package:flutter/material.dart';

class LargeActionButton extends StatelessWidget {
  const LargeActionButton({
    super.key,
    required this.label,
    required this.semanticLabel,
    required this.onPressed,
    this.outlined = false,
  });

  final String label;
  final String semanticLabel;
  final VoidCallback? onPressed;
  final bool outlined;

  @override
  Widget build(BuildContext context) {
    final child = Text(
      label,
      style: const TextStyle(fontSize: 24, fontWeight: FontWeight.w700),
    );
    return Semantics(
      button: true,
      label: semanticLabel,
      excludeSemantics: true,
      child: SizedBox(
        width: double.infinity,
        height: 64,
        child: outlined
            ? OutlinedButton(onPressed: onPressed, child: child)
            : FilledButton(onPressed: onPressed, child: child),
      ),
    );
  }
}
