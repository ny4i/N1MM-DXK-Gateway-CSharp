// SPDX-License-Identifier: GPL-3.0-or-later

using System.IO;

namespace N1MM_DXK_GW;

/// <summary>
/// Where the gateway writes ErrorLog.txt and the failed-QSO file.
///
/// Beside the executable, as the VB6 gateway did and as the DXLab programs
/// generally do — DXKeeper itself lives in a plain folder like C:\DXLab\
/// DXKeeper. Keeping everything in one place is genuinely easier to support:
/// "look in the Gateway's folder" beats explaining a hidden AppData path.
///
/// But it must not DEPEND on that folder being writable. An installer that
/// puts the program under C:\Program Files leaves it read-only for a standard
/// user, and .NET applications get no UAC file virtualisation to paper over
/// it — the write simply throws. That would silently disable the failed-QSO
/// file, which is the one thing standing between an undelivered QSO and a lost
/// one, and the complaint about it could not be written either, because that
/// goes to ErrorLog.txt in the same unwritable folder.
///
/// So the program folder is used when it is actually writable, and a per-user
/// folder is used when it is not. Which one is in force is reported at startup
/// rather than left to be discovered.
/// </summary>
public static class AppPaths
{
   private const string PerUserFolderName = "N1MM-DXKeeper Gateway";

   private static readonly Lazy<string> dataDirectory = new(Resolve);

   /// <summary>Directory for files the gateway writes. Always exists.</summary>
   public static string DataDirectory => dataDirectory.Value;

   /// <summary>
   /// True when the writable location is not the program folder, so startup
   /// can say where the files actually went.
   /// </summary>
   public static bool RedirectedFromProgramFolder =>
      !string.Equals(
         Path.TrimEndingDirectorySeparator(DataDirectory),
         Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory),
         StringComparison.OrdinalIgnoreCase);

   private static string Resolve()
   {
      var beside = AppContext.BaseDirectory;
      if (IsWritable(beside))
      {
         return beside;
      }

      var perUser = Path.Combine(
         Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
         PerUserFolderName);
      try
      {
         Directory.CreateDirectory(perUser);
         return perUser;
      }
      catch (Exception)
      {
         // Nowhere writable at all. Returning the program folder keeps every
         // caller's error handling on its normal path rather than introducing
         // a second failure mode here; they already cope with a write that
         // throws, and report it to the operator.
         return beside;
      }
   }

   /// <summary>
   /// Tests writability by actually creating and deleting a file. Checking
   /// ACLs is not the same question: a folder can grant write and still refuse
   /// it through inherited denies, read-only media, or a locked-down policy.
   /// The only reliable test is to try.
   /// </summary>
   private static bool IsWritable(string directory)
   {
      var probe = Path.Combine(directory, $".write-probe-{Environment.ProcessId}");
      try
      {
         using (var stream = new FileStream(probe, FileMode.Create, FileAccess.Write,
                                            FileShare.None, 1, FileOptions.DeleteOnClose))
         {
            stream.WriteByte(0);
         }
         return true;
      }
      catch (Exception)
      {
         return false;
      }
   }
}
