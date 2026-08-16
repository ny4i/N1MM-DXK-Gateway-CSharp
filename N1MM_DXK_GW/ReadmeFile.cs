// SPDX-License-Identifier: GPL-3.0-or-later

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace N1MM_DXK_GW;

/// <summary>
/// Finds and opens the README, in the operator's language when there is a
/// current translation for it.
///
/// The interesting rule is the one about staleness. README.txt changes, and a
/// translation made from an older copy goes on describing a program that no
/// longer exists — with no way for its reader to tell. That is worse than
/// English: a wrong instruction sends somebody to a menu that is not there or
/// tells them to delete the wrong file, and they have no reason to doubt it.
///
/// So every translation records the SHA-256 of the English README it was made
/// from, and a translation whose recorded hash does not match the English file
/// actually shipped is not used. English is shown instead. Rot is therefore
/// safe by construction rather than by anyone remembering to check.
/// </summary>
public static class ReadmeFile
{
   private const string EnglishName = "README.txt";

   private static readonly Regex RecordedHash =
      new(@"source-sha256:\s*([0-9a-f]{16})", RegexOptions.IgnoreCase);

   /// <summary>True when a README is present to open at all.</summary>
   public static bool Exists => File.Exists(EnglishPath);

   private static string EnglishPath =>
      Path.Combine(AppContext.BaseDirectory, EnglishName);

   /// <summary>
   /// The file that should be opened: the translation for the running language
   /// if one is present and current, otherwise the English.
   /// </summary>
   public static string PathToOpen()
   {
      var english = EnglishPath;
      if (!File.Exists(english))
      {
         return english;
      }

      foreach (var culture in CandidateCultures())
      {
         var translated = Path.Combine(AppContext.BaseDirectory, $"README.{culture}.txt");
         if (!File.Exists(translated))
         {
            continue;
         }
         if (IsCurrent(translated, english))
         {
            return translated;
         }
         // Present but stale. Deliberately falls through to English rather
         // than showing instructions for an older version.
         break;
      }

      return english;
   }

   /// <summary>
   /// The running language, then its neutral parent. An operator on pt-BR gets
   /// README.pt-BR.txt if one exists and README.pt.txt otherwise, which is the
   /// same order the resource loader uses for the interface.
   /// </summary>
   private static IEnumerable<string> CandidateCultures()
   {
      var culture = CultureInfo.CurrentUICulture;
      while (culture != null && culture != CultureInfo.InvariantCulture &&
             !string.IsNullOrEmpty(culture.Name))
      {
         yield return culture.Name;
         culture = culture.Parent;
      }
   }

   private static bool IsCurrent(string translated, string english)
   {
      try
      {
         var recorded = ReadRecordedHash(translated);
         return recorded != null &&
                string.Equals(recorded, HashOf(english), StringComparison.OrdinalIgnoreCase);
      }
      catch (IOException)
      {
         return false;
      }
      catch (UnauthorizedAccessException)
      {
         return false;
      }
   }

   private static string? ReadRecordedHash(string path)
   {
      // Only the banner at the top carries it; no need to read the whole file.
      using var reader = new StreamReader(path, Encoding.UTF8);
      for (var i = 0; i < 12; i++)
      {
         var line = reader.ReadLine();
         if (line == null)
         {
            return null;
         }
         var match = RecordedHash.Match(line);
         if (match.Success)
         {
            return match.Groups[1].Value;
         }
      }
      return null;
   }

   /// <summary>
   /// Hashes the English README with line endings normalised, so a checkout
   /// that converted CRLF does not make every translation look stale.
   /// </summary>
   private static string HashOf(string path)
   {
      var bytes = File.ReadAllBytes(path);
      var text = Encoding.UTF8.GetString(bytes).Replace("\r\n", "\n");
      var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
      return Convert.ToHexString(hash)[..16].ToLowerInvariant();
   }

   /// <summary>Opens the README, reporting plainly if it cannot.</summary>
   public static void Show(Window? owner)
   {
      var path = PathToOpen();
      if (!File.Exists(path))
      {
         MessageBox.Show(owner,
            string.Format(CultureInfo.CurrentCulture, Strings.DlgReadmeMissing, path),
            Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
         return;
      }

      try
      {
         Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
      }
      catch (Exception ex)
      {
         MessageBox.Show(owner,
            string.Format(CultureInfo.CurrentCulture, Strings.DlgCouldNotOpenFile,
                          Path.GetFileName(path), ex.Message),
            Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
      }
   }
}
