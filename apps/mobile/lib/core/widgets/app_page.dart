import 'package:flutter/material.dart';

import '../theme/app_theme.dart';

enum AppNoticeTone { info, success, warning, danger }

extension on AppNoticeTone {
  Color get foreground => switch (this) {
    AppNoticeTone.info => AppColors.navy,
    AppNoticeTone.success => AppColors.success,
    AppNoticeTone.warning => AppColors.warning,
    AppNoticeTone.danger => AppColors.danger,
  };

  Color get background => switch (this) {
    AppNoticeTone.info => AppColors.primarySoft,
    AppNoticeTone.success => AppColors.successSoft,
    AppNoticeTone.warning => AppColors.warningSoft,
    AppNoticeTone.danger => AppColors.dangerSoft,
  };
}

class AppPageHeader extends StatelessWidget {
  const AppPageHeader({
    super.key,
    required this.title,
    this.eyebrow,
    this.subtitle,
    this.trailing,
    this.elder = false,
  });

  final String title;
  final String? eyebrow;
  final String? subtitle;
  final Widget? trailing;
  final bool elder;

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (eyebrow != null) ...[
                Text(
                  eyebrow!,
                  style: TextStyle(
                    color: AppColors.primary,
                    fontSize: elder ? 18 : 14,
                    fontWeight: FontWeight.w700,
                    letterSpacing: .4,
                  ),
                ),
                const SizedBox(height: AppSpacing.xs),
              ],
              Text(
                title,
                style: elder ? AppTextStyles.display : AppTextStyles.pageTitle,
              ),
              if (subtitle != null) ...[
                const SizedBox(height: AppSpacing.sm),
                Text(
                  subtitle!,
                  style: elder
                      ? AppTextStyles.secondary.copyWith(color: AppColors.ink)
                      : AppTextStyles.bodySmall,
                ),
              ],
            ],
          ),
        ),
        if (trailing != null) ...[
          const SizedBox(width: AppSpacing.lg),
          trailing!,
        ],
      ],
    );
  }
}

class AppSectionHeading extends StatelessWidget {
  const AppSectionHeading({
    super.key,
    required this.title,
    this.description,
    this.trailing,
    this.elder = false,
  });

  final String title;
  final String? description;
  final Widget? trailing;
  final bool elder;

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.end,
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                style: elder ? AppTextStyles.title : AppTextStyles.sectionTitle,
              ),
              if (description != null) ...[
                const SizedBox(height: AppSpacing.xs),
                Text(
                  description!,
                  style: elder
                      ? AppTextStyles.secondary.copyWith(color: AppColors.ink)
                      : AppTextStyles.bodySmall,
                ),
              ],
            ],
          ),
        ),
        if (trailing != null) ...[
          const SizedBox(width: AppSpacing.md),
          trailing!,
        ],
      ],
    );
  }
}

class AppInlineNotice extends StatelessWidget {
  const AppInlineNotice({
    super.key,
    required this.message,
    required this.icon,
    this.tone = AppNoticeTone.info,
    this.action,
    this.elder = false,
    this.liveRegion = false,
  });

  final String message;
  final IconData icon;
  final AppNoticeTone tone;
  final Widget? action;
  final bool elder;
  final bool liveRegion;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      liveRegion: liveRegion,
      label: message,
      child: Container(
        width: double.infinity,
        decoration: BoxDecoration(
          color: tone.background,
          border: Border(
            left: BorderSide(color: tone.foreground, width: 4),
            top: const BorderSide(color: AppColors.line),
            right: const BorderSide(color: AppColors.line),
            bottom: const BorderSide(color: AppColors.line),
          ),
        ),
        padding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.lg,
          vertical: AppSpacing.md,
        ),
        child: Row(
          children: [
            Icon(icon, size: elder ? 30 : 22, color: tone.foreground),
            const SizedBox(width: AppSpacing.md),
            Expanded(
              child: Text(
                message,
                style: TextStyle(
                  color: AppColors.inkStrong,
                  fontSize: elder ? 18 : 15,
                  height: 1.45,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
            if (action != null) ...[
              const SizedBox(width: AppSpacing.sm),
              action!,
            ],
          ],
        ),
      ),
    );
  }
}

class AppStatusPanel extends StatelessWidget {
  const AppStatusPanel({
    super.key,
    required this.icon,
    required this.title,
    required this.tone,
    this.description,
    this.child,
    this.elder = false,
  });

  final IconData icon;
  final String title;
  final AppNoticeTone tone;
  final String? description;
  final Widget? child;
  final bool elder;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      decoration: BoxDecoration(
        color: AppColors.surface,
        border: Border.all(color: AppColors.line),
        borderRadius: AppRadius.lgAll,
      ),
      clipBehavior: Clip.antiAlias,
      child: Stack(
        children: [
          Padding(
            padding: EdgeInsets.all(elder ? AppSpacing.xxl : AppSpacing.lg),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Icon(icon, size: elder ? 46 : 30, color: tone.foreground),
                SizedBox(height: elder ? AppSpacing.lg : AppSpacing.md),
                Text(
                  title,
                  style:
                      (elder ? AppTextStyles.title : AppTextStyles.sectionTitle)
                          .copyWith(color: tone.foreground),
                ),
                if (description != null) ...[
                  const SizedBox(height: AppSpacing.sm),
                  Text(
                    description!,
                    style: elder
                        ? AppTextStyles.secondary.copyWith(color: AppColors.ink)
                        : AppTextStyles.bodySmall.copyWith(
                            color: AppColors.ink,
                          ),
                  ),
                ],
                if (child != null) ...[
                  const SizedBox(height: AppSpacing.xl),
                  child!,
                ],
              ],
            ),
          ),
          Positioned(
            left: 0,
            top: 0,
            bottom: 0,
            width: 6,
            child: ColoredBox(color: tone.foreground),
          ),
        ],
      ),
    );
  }
}

class AppSkeleton extends StatelessWidget {
  const AppSkeleton({
    super.key,
    this.height = 18,
    this.width = double.infinity,
    this.radius = AppRadius.sm,
  });

  final double height;
  final double width;
  final double radius;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: width,
      height: height,
      decoration: BoxDecoration(
        color: AppColors.surfaceMuted,
        borderRadius: BorderRadius.circular(radius),
      ),
    );
  }
}
