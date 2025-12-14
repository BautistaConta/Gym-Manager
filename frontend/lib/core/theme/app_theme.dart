import 'package:flutter/material.dart';

class AppTheme {
  static const Color primaryGreen = Color.fromARGB(255, 27, 232, 48);

  static ThemeData darkTheme = ThemeData(
    brightness: Brightness.dark,
    scaffoldBackgroundColor: Colors.black,

    primaryColor: primaryGreen,

    colorScheme: const ColorScheme.dark(
      primary: primaryGreen,
      secondary: primaryGreen,
      background: Colors.black,
    ),

    // AppBar
    appBarTheme: const AppBarTheme(
      backgroundColor: Colors.black,
      elevation: 0,
      centerTitle: true,
      titleTextStyle: TextStyle(
        color: primaryGreen,
        fontSize: 20,
        fontWeight: FontWeight.bold,
      ),
      iconTheme: IconThemeData(color: primaryGreen),
    ),

    // Textos
    textTheme: const TextTheme(
      headlineLarge: TextStyle(
        color: primaryGreen,
        fontWeight: FontWeight.bold,
      ),
      bodyMedium: TextStyle(
        color: Colors.white,
      ),
      bodySmall: TextStyle(
        color: Colors.grey,
      ),
    ),

    // TextButton
    textButtonTheme: TextButtonThemeData(
      style: TextButton.styleFrom(
        foregroundColor: primaryGreen,
      ),
    ),

    // Inputs (TextField)
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: const Color(0xFF1A1A1A),
      labelStyle: const TextStyle(color: Colors.grey),
      prefixIconColor: primaryGreen,
      suffixIconColor: primaryGreen,
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: Colors.transparent),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: primaryGreen),
      ),
    ),

    // Botones principales
    elevatedButtonTheme: ElevatedButtonThemeData(
      style: ElevatedButton.styleFrom(
        backgroundColor: primaryGreen,
        foregroundColor: Colors.black,
        padding: const EdgeInsets.symmetric(vertical: 16),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
        ),
        textStyle: const TextStyle(
          fontSize: 16,
          fontWeight: FontWeight.bold,
          letterSpacing: 1,
        ),
      ),
    ),

    // Loading
    progressIndicatorTheme: const ProgressIndicatorThemeData(
      color: primaryGreen,
    ),
  );
}
