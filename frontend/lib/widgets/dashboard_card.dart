import 'package:flutter/material.dart';
import '../core/theme/app_theme.dart';

class DashboardCard extends StatelessWidget {
  final IconData icon;
  final String title;
  final VoidCallback onTap;

  const DashboardCard({
    super.key,
    required this.icon,
    required this.title,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      borderRadius: BorderRadius.circular(16),
      onTap: onTap,
      child: Container(
        decoration: BoxDecoration(
          color: const Color.fromARGB(255, 57, 57, 57),
          borderRadius: BorderRadius.circular(16),
          border: Border.all(
            color: AppTheme.primaryGreen,
            width: 1.5,
          ),
        ),
        padding: const EdgeInsets.all(20),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              icon,
              color: AppTheme.primaryGreen,
              size: 40,
            ),
            const SizedBox(height: 12),
            Text(
              title,
              textAlign: TextAlign.center,
              style: const TextStyle(
                color: Colors.white,
                fontWeight: FontWeight.bold,
                fontSize: 14,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
