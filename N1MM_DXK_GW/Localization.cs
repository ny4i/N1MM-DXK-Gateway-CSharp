// SPDX-License-Identifier: GPL-3.0-or-later

using System.Globalization;
using System.IO;
using System.Reflection;

namespace N1MM_DXK_GW;

/// <summary>
/// Chooses the language the window is drawn in.
///
/// Two rules shape everything here:
///
///  * Only the UI culture is set, never the formatting culture. The Windows
///    regional settings decide how numbers and dates look, and an operator who
///    wants the window in German has not asked for German decimal separators
///    in the process. Keeping them separate also protects the frequency
///    formatting, which must stay invariant (a real VB6 bug) — that code pins
///    InvariantCulture explicitly, and nothing here should make it matter.
///
///  * The choice is applied once, before the first window is built. XAML
///    resolves its 45 x:Static references at load time, so a language change
///    while running would repaint nothing. Rather than fake a live switch by
///    rebuilding the visual tree, the setting says plainly that it applies on
///    restart.
/// </summary>
public static class Localization
{
   /// <summary>
   /// Applies a configured culture name. Empty, unknown, or not-shipped all
   /// mean "follow Windows", which is the safe outcome: the operator gets the
   /// neutral English resources rather than a window that fails to open.
   /// </summary>
   public static void Apply(string cultureName)
   {
      if (string.IsNullOrWhiteSpace(cultureName))
      {
         return;
      }

      try
      {
         var culture = CultureInfo.GetCultureInfo(cultureName);
         CultureInfo.DefaultThreadCurrentUICulture = culture;
         CultureInfo.CurrentUICulture = culture;
      }
      catch (CultureNotFoundException)
      {
         // A hand-edited registry value, or a culture this version of Windows
         // does not know. Fall through to the Windows language.
      }
   }

   /// <summary>
   /// The translations actually shipped alongside the executable, found by
   /// looking for satellite assemblies rather than from a list in code — so
   /// dropping in a new Strings.&lt;culture&gt;.resx needs no code change, which
   /// is what the .csproj promises.
   ///
   /// Returns them ordered by native name. Never throws: a missing or
   /// unreadable output directory just means "English only".
   /// </summary>
   public static IReadOnlyList<CultureInfo> AvailableTranslations()
   {
      var found = new List<CultureInfo>();
      try
      {
         var assembly = Assembly.GetExecutingAssembly();
         var baseDir = Path.GetDirectoryName(assembly.Location);
         if (string.IsNullOrEmpty(baseDir))
         {
            return found;
         }

         var satelliteName = assembly.GetName().Name + ".resources.dll";
         foreach (var dir in Directory.EnumerateDirectories(baseDir))
         {
            if (!File.Exists(Path.Combine(dir, satelliteName)))
            {
               continue;
            }
            try
            {
               found.Add(CultureInfo.GetCultureInfo(Path.GetFileName(dir)));
            }
            catch (CultureNotFoundException)
            {
               // A directory that merely looks like a culture folder.
            }
         }
      }
      catch (IOException)
      {
      }
      catch (UnauthorizedAccessException)
      {
      }

      found.Sort((a, b) => string.Compare(a.NativeName, b.NativeName, StringComparison.CurrentCulture));
      return found;
   }
}