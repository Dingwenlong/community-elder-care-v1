import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:lucide_icons_flutter/lucide_icons.dart';

import '../core/api/api_problem.dart';
import '../core/theme/app_theme.dart';
import '../core/widgets/app_page.dart';
import 'session_controller.dart';

class LoginPage extends ConsumerStatefulWidget {
  const LoginPage({super.key});

  @override
  ConsumerState<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends ConsumerState<LoginPage> {
  final _username = TextEditingController();
  final _password = TextEditingController();
  final _passwordFocus = FocusNode();
  var _submitting = false;
  var _showPassword = false;
  String? _errorMessage;

  @override
  void dispose() {
    _username.dispose();
    _password.dispose();
    _passwordFocus.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_submitting) return;
    FocusScope.of(context).unfocus();
    setState(() {
      _submitting = true;
      _errorMessage = null;
    });
    try {
      await ref
          .read(sessionControllerProvider.notifier)
          .login(_username.text, _password.text);
    } on ApiProblem catch (error) {
      if (mounted) setState(() => _errorMessage = error.message);
    } on Object {
      if (mounted) setState(() => _errorMessage = '登录未完成，请稍后重试。');
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.surface,
      body: SafeArea(
        child: LayoutBuilder(
          builder: (context, constraints) {
            final textScale = MediaQuery.textScalerOf(context).scale(16) / 16;
            final heroHeight = textScale > 1.4
                ? (constraints.maxWidth * .82).clamp(300.0, 420.0)
                : (constraints.maxWidth * .58).clamp(230.0, 340.0);
            return SingleChildScrollView(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Semantics(
                    image: true,
                    label: '社区照料人员上门探访老人',
                    child: SizedBox(
                      height: heroHeight,
                      child: Stack(
                        fit: StackFit.expand,
                        children: [
                          Image.asset(
                            'assets/images/login-care-scene.webp',
                            fit: BoxFit.cover,
                            alignment: Alignment.center,
                            filterQuality: FilterQuality.medium,
                          ),
                          const ColoredBox(color: AppColors.heroScrim),
                          Padding(
                            padding: const EdgeInsets.all(AppSpacing.xxl),
                            child: Align(
                              alignment: Alignment.bottomLeft,
                              child: ConstrainedBox(
                                constraints: const BoxConstraints(
                                  maxWidth: 560,
                                ),
                                child: Column(
                                  mainAxisSize: MainAxisSize.min,
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    const Text(
                                      '社区独居老人照料协同系统',
                                      style: TextStyle(
                                        color: AppColors.accentWarm,
                                        fontSize: 16,
                                        fontWeight: FontWeight.w800,
                                        letterSpacing: 1.2,
                                      ),
                                    ),
                                    const SizedBox(height: AppSpacing.sm),
                                    Text(
                                      '安邻照料',
                                      style: AppTextStyles.pageTitle.copyWith(
                                        color: AppColors.surface,
                                        fontSize: 30,
                                      ),
                                    ),
                                    const SizedBox(height: AppSpacing.sm),
                                    const Text(
                                      '平安确认、照料跟进与家属知情，一处完成。',
                                      style: TextStyle(
                                        color: AppColors.surface,
                                        fontSize: 16,
                                        height: 1.5,
                                        fontWeight: FontWeight.w500,
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                  Center(
                    child: ConstrainedBox(
                      constraints: const BoxConstraints(maxWidth: 520),
                      child: Padding(
                        padding: const EdgeInsets.fromLTRB(
                          AppSpacing.xxl,
                          AppSpacing.xxl,
                          AppSpacing.xxl,
                          AppSpacing.huge,
                        ),
                        child: AutofillGroup(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.stretch,
                            children: [
                              const AppPageHeader(
                                eyebrow: '老人和家属端',
                                title: '欢迎回来',
                                subtitle: '请输入已开通的账号。',
                              ),
                              const SizedBox(height: AppSpacing.xxl),
                              TextField(
                                controller: _username,
                                autocorrect: false,
                                textInputAction: TextInputAction.next,
                                autofillHints: const [AutofillHints.username],
                                decoration: const InputDecoration(
                                  labelText: '账号',
                                  prefixIcon: Icon(LucideIcons.userRound),
                                ),
                                onSubmitted: (_) =>
                                    _passwordFocus.requestFocus(),
                              ),
                              const SizedBox(height: AppSpacing.lg),
                              TextField(
                                controller: _password,
                                focusNode: _passwordFocus,
                                obscureText: !_showPassword,
                                textInputAction: TextInputAction.done,
                                autofillHints: const [AutofillHints.password],
                                onSubmitted: (_) => _submit(),
                                decoration: InputDecoration(
                                  labelText: '密码',
                                  prefixIcon: const Icon(LucideIcons.keyRound),
                                  suffixIcon: IconButton(
                                    tooltip: _showPassword ? '隐藏密码' : '显示密码',
                                    onPressed: () => setState(
                                      () => _showPassword = !_showPassword,
                                    ),
                                    icon: Icon(
                                      _showPassword
                                          ? LucideIcons.eyeOff
                                          : LucideIcons.eye,
                                    ),
                                  ),
                                ),
                              ),
                              if (_errorMessage != null) ...[
                                const SizedBox(height: AppSpacing.lg),
                                AppInlineNotice(
                                  message: _errorMessage!,
                                  icon: LucideIcons.circleAlert,
                                  tone: AppNoticeTone.danger,
                                  liveRegion: true,
                                ),
                              ],
                              const SizedBox(height: AppSpacing.xxl),
                              FilledButton.icon(
                                onPressed: _submitting ? null : _submit,
                                icon: _submitting
                                    ? const SizedBox.square(
                                        dimension: 20,
                                        child: CircularProgressIndicator(
                                          strokeWidth: 2,
                                        ),
                                      )
                                    : const Icon(LucideIcons.logIn),
                                label: Text(_submitting ? '正在登录' : '登录'),
                                style: FilledButton.styleFrom(
                                  minimumSize: const Size.fromHeight(56),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            );
          },
        ),
      ),
    );
  }
}
