import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../auth/session_controller.dart';
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
      appBar: AppBar(title: const Text('家属设置')),
      body: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          if (status.snapshot case final snapshot?)
            ConsentScopeCard(
              grantedFields: snapshot.grantedFields,
              expiresAt: snapshot.consentExpiresAt,
            )
          else
            const Text('授权范围需联网重新确认。'),
          const SizedBox(height: 18),
          const Text(
            '家属账号不能自行增加授权范围。',
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 18),
          SwitchListTile(
            contentPadding: EdgeInsets.zero,
            title: const Text('照料进展通知'),
            subtitle: const Text('仅接收当前授权范围内的演示通知。'),
            value: _notificationsEnabled,
            onChanged: (value) => setState(() {
              _notificationsEnabled = value;
            }),
          ),
          const SizedBox(height: 18),
          OutlinedButton(
            onPressed: () => ref
                .read(sessionControllerProvider.notifier)
                .switchDemoAccount(),
            child: const Text('切换演示账号'),
          ),
        ],
      ),
    );
  }
}
