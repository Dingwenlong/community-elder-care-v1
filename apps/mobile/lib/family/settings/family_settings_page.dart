import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../../auth/session_controller.dart';
import '../../core/theme/app_theme.dart';
import '../../core/widgets/app_page.dart';
import '../home/family_status_controller.dart';
import '../widgets/consent_scope_card.dart';

class FamilySettingsPage extends ConsumerStatefulWidget {
  const FamilySettingsPage({super.key});

  @override
  ConsumerState<FamilySettingsPage> createState() => _FamilySettingsPageState();
}

class _FamilySettingsPageState extends ConsumerState<FamilySettingsPage> {
  var _notificationsEnabled = true;

  @override
  Widget build(BuildContext context) {
    final status = ref.watch(familyStatusControllerProvider);
    return Scaffold(
      appBar: AppBar(
        automaticallyImplyLeading: false,
        title: const Text('家属设置'),
      ),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(
          AppSpacing.xl,
          AppSpacing.lg,
          AppSpacing.xl,
          AppSpacing.huge,
        ),
        children: [
          const AppPageHeader(
            eyebrow: '我的',
            title: '设置',
            subtitle: '查看授权范围并调整当前设备上的显示偏好。',
          ),
          const SizedBox(height: AppSpacing.xxl),
          if (status.snapshot case final snapshot?)
            ConsentScopeCard(
              grantedFields: snapshot.grantedFields,
              expiresAt: snapshot.consentExpiresAt,
            )
          else
            const AppInlineNotice(
              message: '授权范围需联网重新确认。',
              icon: LucideIcons.cloudOff,
              tone: AppNoticeTone.warning,
            ),
          const SizedBox(height: AppSpacing.md),
          const AppInlineNotice(
            message: '家属账号不能自行增加授权范围。',
            icon: LucideIcons.lockKeyhole,
            tone: AppNoticeTone.info,
          ),
          const SizedBox(height: AppSpacing.xxl),
          Material(
            color: AppColors.surface,
            child: Padding(
              padding: const EdgeInsets.all(AppSpacing.lg),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Icon(
                    LucideIcons.bell,
                    color: AppColors.primary,
                    size: 28,
                  ),
                  const SizedBox(width: AppSpacing.md),
                  Expanded(
                    child: SwitchListTile(
                      contentPadding: EdgeInsets.zero,
                      title: const Text(
                        '照料进展通知',
                        style: AppTextStyles.sectionTitle,
                      ),
                      subtitle: const Text(
                        '当前仅记录界面偏好，不连接真实推送服务。',
                        style: AppTextStyles.bodySmall,
                      ),
                      value: _notificationsEnabled,
                      onChanged: (value) => setState(() {
                        _notificationsEnabled = value;
                      }),
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: AppSpacing.xxl),
          OutlinedButton.icon(
            onPressed: () => ref
                .read(sessionControllerProvider.notifier)
                .switchDemoAccount(),
            icon: const Icon(LucideIcons.logOut),
            label: const Text('切换账号'),
            style: OutlinedButton.styleFrom(
              minimumSize: const Size.fromHeight(54),
            ),
          ),
        ],
      ),
    );
  }
}
