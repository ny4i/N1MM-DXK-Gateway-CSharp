// SPDX-License-Identifier: GPL-3.0-or-later

using System.Globalization;
using Microsoft.Win32;

namespace N1MM_DXK_GW;

public sealed class Settings
{
   // VB6-compatible registry path. Matching this exactly so an upgrade
   // from the original VB6 build preserves the user's existing settings.
   // App name: "N1MM-DXKeeper Gateway"  Section: "N1MM-DXK-Gateway"
   private const string RegistryPath =
      @"Software\VB and VBA Program Settings\N1MM-DXKeeper Gateway\N1MM-DXK-Gateway";

   public const int DefaultUdpPort = 12060;
   public const int WindowPositionUnset = int.MinValue;

   public int UdpPort { get; set; } = DefaultUdpPort;

   /// <summary>
   /// Optional IPv4 multicast group to join, as a dotted quad. Empty means
   /// unicast and broadcast only, which is what N1MM Logger+ sends today.
   /// Stored under our own key, not read from any other program's settings.
   /// </summary>
   public string MulticastGroup { get; set; } = string.Empty;

   /// <summary>
   /// UI language as a .NET culture name ("de", "ja", "pt-BR"). Empty means
   /// follow the Windows display language, which is the default and what most
   /// operators want. Stored under our own key.
   /// </summary>
   public string Language { get; set; } = string.Empty;

   /// <summary>
   /// True once the operator has seen the first-run notice. Not an acceptance
   /// record — the GPL requires no acceptance to run the program — just a note
   /// that the warranty disclaimer has been put in front of them, so it is
   /// shown once rather than at every start.
   /// </summary>
   public bool NoticeSeen { get; set; }

   public bool DxkLookup { get; set; }
   public bool DxkCallbook { get; set; }
   public bool DxkEqslUpload { get; set; }
   public bool DxkLotwUpload { get; set; }
   public bool DxkClubLogUpload { get; set; }
   public bool DebugLogging { get; set; }
   public bool VerboseLogging { get; set; }

   // Window state. WindowPositionUnset means "use designer defaults".
   public int WindowLeft { get; set; } = WindowPositionUnset;
   public int WindowTop { get; set; } = WindowPositionUnset;
   public int WindowWidth { get; set; } = WindowPositionUnset;
   public int WindowHeight { get; set; } = WindowPositionUnset;
   public int WindowState { get; set; } // 0 Normal, 1 Minimized, 2 Maximized — matches FormWindowState enum

   public static Settings Load()
   {
      var s = new Settings();
      using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: false);
      if (key == null)
      {
         return s;
      }

      s.UdpPort = ReadInt(key, "N1MMUDPPort", DefaultUdpPort);
      s.MulticastGroup = key.GetValue("MulticastGroup") as string ?? string.Empty;
      s.Language = key.GetValue("Language") as string ?? string.Empty;
      s.NoticeSeen = ReadBool(key, "NoticeSeen");
      s.DxkLookup = ReadBool(key, "DXKeeperLookup");
      s.DxkCallbook = ReadBool(key, "DXKeeperCallbookQuery");
      s.DxkEqslUpload = ReadBool(key, "DXKeepereQSLUpload");
      s.DxkLotwUpload = ReadBool(key, "DXKeeperLoTWUpload");
      s.DxkClubLogUpload = ReadBool(key, "DXKeeperClubLogUpload");
      s.DebugLogging = ReadBool(key, "DiagMode");
      s.VerboseLogging = ReadBool(key, "VerboseLogging");
      s.WindowLeft = ReadInt(key, "WindowLeft", WindowPositionUnset);
      s.WindowTop = ReadInt(key, "WindowTop", WindowPositionUnset);
      s.WindowWidth = ReadInt(key, "WindowWidth", WindowPositionUnset);
      s.WindowHeight = ReadInt(key, "WindowHeight", WindowPositionUnset);
      s.WindowState = ReadInt(key, "WindowState", 0);
      return s;
   }

   public void Save()
   {
      using var key = Registry.CurrentUser.CreateSubKey(RegistryPath, writable: true);
      // All values are REG_SZ strings to remain interchangeable with VB6's
      // SaveSetting/GetSetting (which only writes string values).
      WriteString(key, "N1MMUDPPort", UdpPort.ToString(CultureInfo.InvariantCulture));
      WriteString(key, "MulticastGroup", MulticastGroup);
      WriteString(key, "Language", Language);
      WriteString(key, "NoticeSeen", BoolToVb(NoticeSeen));
      WriteString(key, "DXKeeperLookup", BoolToVb(DxkLookup));
      WriteString(key, "DXKeeperCallbookQuery", BoolToVb(DxkCallbook));
      WriteString(key, "DXKeepereQSLUpload", BoolToVb(DxkEqslUpload));
      WriteString(key, "DXKeeperLoTWUpload", BoolToVb(DxkLotwUpload));
      WriteString(key, "DXKeeperClubLogUpload", BoolToVb(DxkClubLogUpload));
      WriteString(key, "DiagMode", BoolToVb(DebugLogging));
      WriteString(key, "VerboseLogging", BoolToVb(VerboseLogging));
      WriteString(key, "WindowLeft", WindowLeft.ToString(CultureInfo.InvariantCulture));
      WriteString(key, "WindowTop", WindowTop.ToString(CultureInfo.InvariantCulture));
      WriteString(key, "WindowWidth", WindowWidth.ToString(CultureInfo.InvariantCulture));
      WriteString(key, "WindowHeight", WindowHeight.ToString(CultureInfo.InvariantCulture));
      WriteString(key, "WindowState", WindowState.ToString(CultureInfo.InvariantCulture));
   }

   private static string BoolToVb(bool value) => value ? "1" : "0";

   private static int ReadInt(RegistryKey key, string name, int defaultValue)
   {
      var raw = key.GetValue(name);
      if (raw is string s &&
          int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
      {
         return n;
      }
      return defaultValue;
   }

   private static bool ReadBool(RegistryKey key, string name) =>
      ReadInt(key, name, 0) != 0;

   private static void WriteString(RegistryKey key, string name, string value) =>
      key.SetValue(name, value, RegistryValueKind.String);
}