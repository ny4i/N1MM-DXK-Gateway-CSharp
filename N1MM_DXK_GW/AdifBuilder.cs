using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace N1MM_DXK_GW;

/// <summary>
/// Builds an ADIF (Amateur Data Interchange Format) record from an N1MM
/// contactinfo XML element. The field order and ADIF/XML name mapping
/// mirror the VB6 HandleData routine so DXKeeper sees identical input.
///
/// Field wire format: &lt;FIELDNAME:N&gt;VALUE&lt;space&gt;
/// where N is the byte/char length of VALUE. Trailing space and capitalised
/// field names match the VB6 Data_to_ADIF helper exactly.
/// </summary>
public sealed class AdifBuilder
{
   public sealed class Result
   {
      public string AdifRecord { get; init; } = string.Empty;
      public string Summary { get; init; } = string.Empty;
      public string Call { get; init; } = string.Empty;
      public string Band { get; init; } = string.Empty;
      public string Mode { get; init; } = string.Empty;
   }

   public static Result Build(XElement contactInfo)
   {
      var sb = new StringBuilder();

      var call = Raw(contactInfo, "call");
      var txFreqRaw = Raw(contactInfo, "txfreq");
      var rxFreqRaw = Raw(contactInfo, "rxfreq");
      var band = GetBandForFrequency(txFreqRaw);
      var bandRx = GetBandForFrequency(rxFreqRaw);
      var mode = Raw(contactInfo, "mode");
      var exchange = Raw(contactInfo, "exchange1");

      AppendField(sb, "CALL", call);
      AppendField(sb, "RST_RCVD", Raw(contactInfo, "rcv"));
      AppendField(sb, "RST_SENT", Raw(contactInfo, "snt"));

      AppendField(sb, "FREQ", FormatFrequency(txFreqRaw));
      AppendField(sb, "BAND", band);

      AppendField(sb, "FREQ_RX", FormatFrequency(rxFreqRaw));
      AppendField(sb, "BAND_RX", bandRx);

      AppendField(sb, "MODE", mode);

      var timestamp = Raw(contactInfo, "timestamp");
      if (TryParseTimestamp(timestamp, out var ts))
      {
         AppendField(sb, "QSO_DATE", ts.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
         AppendField(sb, "TIME_ON", ts.ToString("HHmmss", CultureInfo.InvariantCulture));
      }

      AppendField(sb, "STX", Raw(contactInfo, "sntnr"));
      AppendField(sb, "SRX", Raw(contactInfo, "rcvnr"));
      AppendField(sb, "SRX_STRING", exchange);
      AppendField(sb, "PFX", Raw(contactInfo, "wpxprefix"));
      AppendField(sb, "CONTEST_ID", Raw(contactInfo, "contestname"));
      AppendField(sb, "NAME", Raw(contactInfo, "name"));
      AppendField(sb, "COMMENT", Raw(contactInfo, "comment"));
      AppendField(sb, "GRIDSQUARE", Raw(contactInfo, "gridsquare"));
      AppendField(sb, "STATION_CALLSIGN", Raw(contactInfo, "mycall"));
      AppendField(sb, "OPERATOR", Raw(contactInfo, "operator"));
      AppendField(sb, "QTH", Raw(contactInfo, "qth"));
      AppendField(sb, "STX_STRING", Raw(contactInfo, "sentexchange"));

      var section = Raw(contactInfo, "section");
      if (IsArrlSection(section))
      {
         AppendField(sb, "ARRL_SECT", section);
      }

      AppendField(sb, "PRECEDENCE", Raw(contactInfo, "prec"));

      var rxPower = Raw(contactInfo, "power");
      var rxPowerNormalized = rxPower.Trim().ToUpperInvariant() switch
      {
         "K" or "KW" => "1000",
         "QRP" => "5",
         _ => rxPower
      };
      AppendField(sb, "RX_PWR", rxPowerNormalized);

      sb.Append("<EOR>");

      var summary = call;
      if (band.Length > 0)
      {
         summary += " on " + band;
      }
      if (mode.Length > 0)
      {
         summary += " in " + mode;
      }
      if (exchange.Length > 0)
      {
         summary += " with exchange " + exchange;
      }

      return new Result
      {
         AdifRecord = sb.ToString(),
         Summary = summary,
         Call = call,
         Band = band,
         Mode = mode,
      };
   }

   /// <summary>
   /// The ADIF record identifying a QSO for deletion.
   ///
   /// DXKeeper's QSO identity is CALL + QSO_DATE + TIME_ON and nothing else —
   /// confirmed against TR4W's BuildDXKeeperDeleteMessage, which sends exactly
   /// these three. That is what makes this gateway able to handle deletes at
   /// all: it is a stateless go-between with no access to either program's
   /// database, and every field of the key arrives in the datagram that asks
   /// for the delete.
   ///
   /// <paramref name="useOldIdentity"/> selects which call/timestamp to use.
   /// A contactreplace carries both the current values and &lt;oldcall&gt; /
   /// &lt;oldtimestamp&gt;; the delete must target the pre-edit record, so an
   /// edit that changed the callsign or the time still matches. A
   /// contactdelete carries only the current values.
   /// </summary>
   public static DeleteKey BuildDeleteRecord(XElement message, bool useOldIdentity)
   {
      var call = useOldIdentity ? FirstNonEmpty(message, "oldcall", "call")
                                : Raw(message, "call");
      var stamp = useOldIdentity ? FirstNonEmpty(message, "oldtimestamp", "timestamp")
                                 : Raw(message, "timestamp");

      if (string.IsNullOrWhiteSpace(call) || !TryParseTimestamp(stamp, out var ts))
      {
         return new DeleteKey { IsValid = false, Call = call.Trim() };
      }

      var sb = new StringBuilder();
      AppendField(sb, "CALL", call);
      AppendField(sb, "QSO_DATE", ts.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
      AppendField(sb, "TIME_ON", ts.ToString("HHmmss", CultureInfo.InvariantCulture));
      sb.Append("<EOR>");

      return new DeleteKey
      {
         IsValid = true,
         AdifRecord = sb.ToString(),
         Call = call.Trim(),
         Summary = $"{call.Trim()} at {ts.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}Z",
      };
   }

   public sealed class DeleteKey
   {
      public bool IsValid { get; init; }
      public string AdifRecord { get; init; } = string.Empty;
      public string Call { get; init; } = string.Empty;
      public string Summary { get; init; } = string.Empty;
   }

   /// <summary>
   /// First of <paramref name="names"/> with a non-blank value. N1MM emits
   /// oldcall/oldtimestamp on every contactinfo and contactreplace, but a
   /// blank one must fall back rather than produce a delete that matches
   /// nothing — or, worse, matches the wrong QSO.
   /// </summary>
   private static string FirstNonEmpty(XElement parent, params string[] names)
   {
      foreach (var name in names)
      {
         var value = Raw(parent, name);
         if (!string.IsNullOrWhiteSpace(value))
         {
            return value;
         }
      }
      return string.Empty;
   }

   private static void AppendField(StringBuilder sb, string fieldName, string value)
   {
      // Match VB6 Data_to_ADIF: only emit if Trim(value) is non-empty,
      // but emit the value un-trimmed (length is also un-trimmed).
      if (string.IsNullOrWhiteSpace(value))
      {
         return;
      }
      sb.Append('<').Append(fieldName).Append(':').Append(value.Length).Append('>')
        .Append(value).Append(' ');
   }

   private static string Raw(XElement parent, string localName)
   {
      // Case-insensitive lookup, value un-trimmed (matches VB6 XMLData).
      var element = XmlHelpers.Find(parent, localName);
      return element?.Value ?? string.Empty;
   }

   private static bool TryParseTimestamp(string raw, out DateTime ts)
   {
      // N1MM emits "yyyy-MM-dd HH:mm:ss" UTC (see N1MM contactinfo schema).
      return DateTime.TryParse(
         raw,
         CultureInfo.InvariantCulture,
         DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
         out ts);
   }

   /// <summary>
   /// Convert N1MM tens-of-Hz frequency to "MMM.NNNNN" MHz string. Pure string
   /// manipulation so locale decimal-separator settings can't break it (the
   /// real fix for the VB6 v1.2.0 locale bug shipped in v1.2.1).
   /// </summary>
   internal static string FormatFrequency(string tensOfHz)
   {
      if (string.IsNullOrWhiteSpace(tensOfHz))
      {
         return string.Empty;
      }
      var src = tensOfHz.Trim();
      if (src.Length < 6)
      {
         src = new string('0', 6 - src.Length) + src;
      }
      // Insert decimal point 5 chars from the right.
      var len = src.Length;
      return src[..(len - 5)] + "." + src[(len - 5)..];
   }

   /// <summary>
   /// Frequency-to-band lookup. Input is the raw N1MM value (tens of Hz);
   /// internally we divide by 100 to get kHz, matching the VB6 lookup table.
   /// </summary>
   internal static string GetBandForFrequency(string tensOfHzString)
   {
      if (!long.TryParse(tensOfHzString.Trim(), NumberStyles.Integer,
            CultureInfo.InvariantCulture, out var tensOfHz))
      {
         return string.Empty;
      }
      // tens of Hz / 100 = kHz (with one decimal place truncated). The VB6
      // uses CDbl + comparisons against decimal kHz boundaries, so we use
      // double to preserve fractional kHz (e.g. 60M = 5330.5–5405 kHz).
      var kHz = tensOfHz / 100.0;

      return kHz switch
      {
         >= 136 and <= 137 => "2190M",
         >= 472 and <= 479 => "630M",
         >= 501 and <= 504 => "560M",
         >= 1800 and <= 2000 => "160M",
         >= 3500 and <= 4000 => "80M",
         >= 5330.5 and <= 5405 => "60M",
         >= 7000 and <= 7300 => "40M",
         >= 10100 and <= 10150 => "30M",
         >= 14000 and <= 14350 => "20M",
         >= 18068 and <= 18168 => "17M",
         >= 21000 and <= 21450 => "15M",
         >= 24890 and <= 24990 => "12M",
         >= 28000 and <= 29700 => "10M",
         >= 50000 and <= 54000 => "6M",
         >= 70000 and <= 71000 => "4M",
         >= 144000 and <= 148000 => "2M",
         >= 222000 and <= 225000 => "1.25M",
         >= 420000 and <= 450000 => "70CM",
         >= 902000 and <= 928000 => "33CM",
         >= 1240000 and <= 1300000 => "23CM",
         >= 2300000 and <= 2450000 => "13CM",
         >= 3300000 and <= 3500000 => "9CM",
         >= 5650000 and <= 5925000 => "6CM",
         >= 10000000 and <= 10500000 => "3CM",
         >= 24000000 and <= 24250000 => "1.25CM",
         _ => string.Empty
      };
   }

   // ARRL section list with the VB6 bugs fixed:
   // - added CO (Rocky Mountain), and the Midwest (IA, KS, MO, NE) + Dakota (MN, ND, SD) divisions
   // - de-duplicated NV
   private static readonly HashSet<string> ArrlSections = new(StringComparer.OrdinalIgnoreCase)
   {
      // Pacific
      "EB", "LAX", "ORG", "PAC", "SB", "SCV", "SDG", "SF", "SJV", "SV",
      // Southeastern
      "AL", "GA", "KY", "NC", "NFL", "PR", "SC", "SFL", "TN", "VA", "VI", "WCF",
      // Great Lakes
      "MI", "OH", "WV",
      // New England
      "CT", "EMA", "ME", "NH", "RI", "VT", "WMA",
      // Hudson
      "ENY", "NLI", "NNJ", "NNY", "SNJ", "WNY",
      // Atlantic
      "DE", "EPA", "MDC", "WPA",
      // Canadian sections (RAC)
      "AB", "BC", "GH", "MB", "NB", "NL", "NS", "ONE", "ONN", "ONS", "PE", "QC", "SK", "TER",
      // Delta + West Gulf
      "AR", "LA", "MS", "NM", "NTX", "OK", "STX", "WTX",
      // Northwestern + Rocky Mountain (added CO; removed duplicate NV)
      "AK", "AZ", "CO", "EWA", "ID", "MT", "NV", "OR", "UT", "WWA", "WY",
      // Central
      "IL", "IN", "WI",
      // Midwest (added)
      "IA", "KS", "MO", "NE",
      // Dakota (added)
      "MN", "ND", "SD",
   };

   internal static bool IsArrlSection(string section) =>
      !string.IsNullOrWhiteSpace(section) && ArrlSections.Contains(section.Trim());
}
