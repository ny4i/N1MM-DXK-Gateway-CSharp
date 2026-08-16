// SPDX-License-Identifier: GPL-3.0-or-later

using System.Windows.Media;

// WinForms is still referenced (NDde's hidden window), so System.Drawing also
// defines Brush and Color. Pin the WPF ones.
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace N1MM_DXK_GW;

/// <summary>
/// One line of the on-screen operation log.
///
/// Severity exists so a failure is visible without reading every line: during a
/// contest run the log scrolls steadily, and "DXKeeper did not confirm..." must
/// not look the same as a successful QSO. It is derived from the message rather
/// than passed in at every call site, so the classification lives in one place
/// and no caller can forget it.
/// </summary>
public sealed class LogEntry
{
   public enum Level
   {
      Normal,
      Warning,
      Error,
   }

   public string Text { get; init; } = string.Empty;
   public Level Severity { get; init; }

   /// <summary>
   /// Colour for this line. Null for Normal, which leaves the ListBox's own
   /// foreground in play — that is what keeps the log readable in both light
   /// and dark themes without tracking the theme here.
   /// </summary>
   public Brush? Brush => Severity switch
   {
      Level.Error => new SolidColorBrush(Color.FromRgb(0xC4, 0x2B, 0x1C)),
      Level.Warning => new SolidColorBrush(Color.FromRgb(0x9D, 0x5D, 0x00)),
      _ => null,
   };

   /// <summary>
   /// Classifies a log line. Deliberately keyed on the phrases this program
   /// actually emits rather than on a generic word list: "deleted from
   /// DXKeeper" is a normal, successful operation and must not be coloured as
   /// a failure just because it contains an alarming verb.
   /// </summary>
   public static Level Classify(string line)
   {
      if (line.Contains("***", StringComparison.Ordinal) ||
          line.Contains("INTERNAL ERROR", StringComparison.OrdinalIgnoreCase) ||
          line.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase) ||
          line.Contains("LOST QSO", StringComparison.OrdinalIgnoreCase))
      {
         return Level.Error;
      }

      if (line.Contains("did not confirm", StringComparison.OrdinalIgnoreCase) ||
          line.Contains("NOT applied", StringComparison.OrdinalIgnoreCase) ||
          line.Contains("INVALID", StringComparison.OrdinalIgnoreCase) ||
          line.Contains("Reverted", StringComparison.OrdinalIgnoreCase) ||
          line.Contains("nothing sent", StringComparison.OrdinalIgnoreCase))
      {
         return Level.Warning;
      }

      return Level.Normal;
   }
}