// SPDX-License-Identifier: GPL-3.0-or-later

namespace N1MM_DXK_GW;

/// <summary>
/// Shared wire-format helpers for the DXLab Suite IPC protocol. Both the
/// TCP path (DXKeeper externallog) and the DDE path (DXKeeper check,
/// DXView lookup, Pathfinder getqslinfo) use the same &lt;name:len&gt;value
/// field encoding defined in DDECommon.bas.
/// </summary>
public static class DxLabWire
{
   /// <summary>
   /// Encode a single named field. Length prefix uses character count
   /// (matching VB6 Len()). Empty data still emits a zero-length field
   /// rather than being omitted — DXLab parsers expect the field to be present.
   /// </summary>
   public static string EncodeField(string name, string data) =>
      $"<{name}:{data.Length}>{data}";

   /// <summary>
   /// Format a DXLab server ID as a 3-digit zero-padded prefix.
   /// LogServer=1 → "001", QSLInfoServer=2 → "002", DXViewServer=3 → "003".
   /// </summary>
   public static string ServerPrefix(int serverId) => serverId.ToString("000");

   // DXLab server IDs (from DDECommon.bas)
   public const int LogServer = 1;        // DXKeeper
   public const int QSLInfoServer = 2;    // Pathfinder
   public const int DXViewServer = 3;     // DXView
}