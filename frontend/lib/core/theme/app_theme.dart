import 'package:flutter/material.dart';

class AppTheme {
  static const Color primaryGreen = Color(0xFF1ED760);
  static const Color primaryDark = Color(0xFF061009);
  static const Color background = Color(0xFF080C09);
  static const Color surface = Color(0xFF111713);
  static const Color surfaceHigh = Color(0xFF19221C);
  static const Color border = Color(0xFF2B382F);
  static const Color textPrimary = Color(0xFFF4F8F5);
  static const Color textSecondary = Color(0xFFACB9B0);
  static const Color warning = Color(0xFFFFB86B);
  static const Color danger = Color(0xFFFF6B78);

  static ThemeData darkTheme = ThemeData(
    useMaterial3: true,
    brightness: Brightness.dark,
    scaffoldBackgroundColor: background,
    primaryColor: primaryGreen,
    colorScheme: const ColorScheme.dark(
      primary: primaryGreen,
      onPrimary: primaryDark,
      secondary: Color(0xFF78E99E),
      onSecondary: primaryDark,
      surface: surface,
      onSurface: textPrimary,
      error: danger,
      outline: border,
    ),
    dividerColor: border,
    splashColor: primaryGreen.withValues(alpha: .10),
    highlightColor: primaryGreen.withValues(alpha: .06),
    appBarTheme: const AppBarTheme(
      backgroundColor: background,
      foregroundColor: textPrimary,
      surfaceTintColor: Colors.transparent,
      elevation: 0,
      scrolledUnderElevation: 0,
      centerTitle: false,
      titleTextStyle: TextStyle(
        color: textPrimary,
        fontSize: 20,
        fontWeight: FontWeight.w700,
        letterSpacing: -.2,
      ),
      iconTheme: IconThemeData(color: textSecondary),
    ),
    textTheme: const TextTheme(
      displaySmall: TextStyle(
        color: textPrimary,
        fontSize: 34,
        fontWeight: FontWeight.w800,
        letterSpacing: -1,
      ),
      headlineLarge: TextStyle(
        color: textPrimary,
        fontSize: 30,
        fontWeight: FontWeight.w800,
        letterSpacing: -.8,
      ),
      headlineSmall: TextStyle(
        color: textPrimary,
        fontSize: 24,
        fontWeight: FontWeight.w700,
        letterSpacing: -.4,
      ),
      titleLarge: TextStyle(
        color: textPrimary,
        fontSize: 20,
        fontWeight: FontWeight.w700,
      ),
      titleMedium: TextStyle(
        color: textPrimary,
        fontSize: 16,
        fontWeight: FontWeight.w700,
      ),
      titleSmall: TextStyle(
        color: textSecondary,
        fontSize: 13,
        fontWeight: FontWeight.w600,
      ),
      bodyLarge: TextStyle(color: textPrimary, fontSize: 16, height: 1.45),
      bodyMedium: TextStyle(color: textPrimary, fontSize: 14, height: 1.45),
      bodySmall: TextStyle(color: textSecondary, fontSize: 12, height: 1.4),
      labelLarge: TextStyle(
        fontSize: 14,
        fontWeight: FontWeight.w700,
        letterSpacing: .1,
      ),
    ),
    cardTheme: CardThemeData(
      color: surface,
      surfaceTintColor: Colors.transparent,
      elevation: 0,
      margin: EdgeInsets.zero,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(18),
        side: const BorderSide(color: border),
      ),
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: surfaceHigh,
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 17),
      labelStyle: const TextStyle(color: textSecondary),
      floatingLabelStyle: const TextStyle(
        color: primaryGreen,
        fontWeight: FontWeight.w700,
      ),
      floatingLabelBehavior: FloatingLabelBehavior.auto,
      hintStyle: const TextStyle(color: Color(0xFF71827C)),
      prefixIconColor: textSecondary,
      suffixIconColor: textSecondary,
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: const BorderSide(color: border),
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: const BorderSide(color: border),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: const BorderSide(color: primaryGreen, width: 1.5),
      ),
      errorBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: const BorderSide(color: danger),
      ),
      focusedErrorBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: const BorderSide(color: danger, width: 1.5),
      ),
    ),
    elevatedButtonTheme: ElevatedButtonThemeData(style: _primaryButtonStyle()),
    filledButtonTheme: FilledButtonThemeData(style: _primaryButtonStyle()),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        foregroundColor: textPrimary,
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
        side: const BorderSide(color: border),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
        textStyle: const TextStyle(fontWeight: FontWeight.w700),
      ),
    ),
    textButtonTheme: TextButtonThemeData(
      style: TextButton.styleFrom(
        foregroundColor: primaryGreen,
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        textStyle: const TextStyle(fontWeight: FontWeight.w700),
      ),
    ),
    iconButtonTheme: IconButtonThemeData(
      style: IconButton.styleFrom(
        foregroundColor: textSecondary,
        hoverColor: primaryGreen.withValues(alpha: .10),
        highlightColor: primaryGreen.withValues(alpha: .08),
      ),
    ),
    dialogTheme: DialogThemeData(
      backgroundColor: surface,
      surfaceTintColor: Colors.transparent,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(22),
        side: const BorderSide(color: border),
      ),
      insetPadding: const EdgeInsets.symmetric(horizontal: 20, vertical: 24),
      titleTextStyle: const TextStyle(
        color: textPrimary,
        fontSize: 21,
        fontWeight: FontWeight.w700,
      ),
    ),
    snackBarTheme: SnackBarThemeData(
      backgroundColor: surfaceHigh,
      contentTextStyle: const TextStyle(color: textPrimary),
      behavior: SnackBarBehavior.floating,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
    ),
    progressIndicatorTheme: const ProgressIndicatorThemeData(
      color: primaryGreen,
    ),
    switchTheme: SwitchThemeData(
      thumbColor: WidgetStateProperty.resolveWith(
        (states) =>
            states.contains(WidgetState.selected) ? primaryDark : textSecondary,
      ),
      trackColor: WidgetStateProperty.resolveWith(
        (states) =>
            states.contains(WidgetState.selected) ? primaryGreen : border,
      ),
    ),
  );

  static ButtonStyle _primaryButtonStyle() => ElevatedButton.styleFrom(
    backgroundColor: primaryGreen,
    foregroundColor: primaryDark,
    disabledBackgroundColor: border,
    disabledForegroundColor: textSecondary,
    elevation: 0,
    padding: const EdgeInsets.symmetric(horizontal: 22, vertical: 17),
    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
    textStyle: const TextStyle(
      fontSize: 14,
      fontWeight: FontWeight.w800,
      letterSpacing: .15,
    ),
  );
}
