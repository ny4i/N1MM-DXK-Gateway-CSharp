// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace N1MM_DXK_GW;

/// <summary>
/// Buffers raw UDP XML strings from the listener and dispatches them on the
/// UI thread via <see cref="Drain"/>. Events fire on whatever thread calls
/// Drain (typically the WinForms timer = UI thread, so subscribers can touch
/// controls directly).
///
/// VB6 invariant preserved: contactinfo is only dispatched when
/// &lt;isoriginal&gt;true&lt;/isoriginal&gt; — N1MM also broadcasts a
/// non-original copy for spotting that must NOT trigger DXKeeper logging.
/// </summary>
public sealed class MessageDispatcher
{
   private readonly ConcurrentQueue<string> queue = new();
   private bool isDraining;

   public event Action<XElement>? ContactInfoReceived;
   public event Action<XElement>? ContactReplaceReceived;
   public event Action<XElement>? LookupInfoReceived;
   public event Action<XElement>? ContactDeleteReceived;
   public event Action<AdifBuilder.Result>? AdifRecordReceived;   // (one logged QSO, as ADIF)
   public event Action<string, string>? InvalidMessageReceived;   // (rawXml, reason)
   public event Action<string>? UnhandledMessageReceived;         // (messageType)
   public event Action<string, Exception>? DispatchFailed;        // (rawXml, fault)

   /// <summary>
   /// N1MM broadcast types this gateway knowingly does not act on. They are
   /// not errors and must not be reported as such: N1MM emits RadioInfo
   /// continuously while a radio is connected, so treating it as an invalid
   /// message would bury genuine problems under thousands of false reports.
   ///
   /// Distinct from a root element we do not recognise at all, which stays a
   /// reportable condition — that is how a misconfigured sender (e.g. another
   /// program pointed at our port) becomes visible.
   /// </summary>
   private static readonly HashSet<string> KnownUnhandledTypes =
      new(StringComparer.OrdinalIgnoreCase)
      {
         "radioinfo",       // radio state, broadcast continuously
         "dynamicresults",  // periodic score summary
      };

   public int PendingCount => queue.Count;

   public void Enqueue(string rawXml) => queue.Enqueue(rawXml);

   public void Drain()
   {
      // Re-entrancy guard: a subscriber that pumps the message loop (e.g.
      // shows a MessageBox) could re-trigger the timer tick mid-drain.
      if (isDraining)
      {
         return;
      }
      isDraining = true;
      try
      {
         while (queue.TryDequeue(out var xml))
         {
            try
            {
               ProcessOne(xml);
            }
            catch (Exception ex)
            {
               // Drain runs on the UI thread, so an escaping exception is an
               // unhandled WinForms exception: the gateway dies mid-contest and
               // every subsequent QSO is lost. That actually happened — a
               // NotSupportedException from ChannelReader.Count took the app
               // down on the first contactinfo. One bad message, or one defect
               // in a handler, must cost at most that message.
               DispatchFailed?.Invoke(xml, ex);
            }
         }
      }
      finally
      {
         isDraining = false;
      }
   }

   /// <summary>
   /// Raises one event per QSO in a received ADIF datagram.
   ///
   /// Split here rather than in the handler because a sender is free to put
   /// more than one record in a datagram, and each is a separate QSO for
   /// DXKeeper. A datagram that turns out to hold no usable record is
   /// reported, since something addressed to this port and shaped like ADIF
   /// was meant to log something.
   /// </summary>
   private void DispatchAdif(string datagram)
   {
      var records = AdifBuilder.ReadAdifRecords(datagram);
      if (records.Count == 0)
      {
         InvalidMessageReceived?.Invoke(datagram,
            "ADIF received but no record in it had a CALL field");
         return;
      }

      foreach (var record in records)
      {
         AdifRecordReceived?.Invoke(record);
      }
   }

   private void ProcessOne(string xml)
   {
      XDocument doc;
      try
      {
         doc = XDocument.Parse(xml);
      }
      catch (XmlException strictEx)
      {
         // Not XML at all, but ADIF — which is a supported input, not a
         // mistake. WSJT-X ("Enable logged contact ADIF broadcast") and
         // SDR-Control send a logged QSO this way instead of as N1MM XML, and
         // the gateway logs those to DXKeeper just as it does contactinfo.
         //
         // This branch used to report the datagram as invalid and tell the
         // operator to switch the feature off, on the assumption that ADIF
         // arriving here meant a sender pointed at the wrong port. That was
         // wrong: it is the documented way SDR-Control logs to DXKeeper.
         if (xml.IndexOf("<eor>", StringComparison.OrdinalIgnoreCase) >= 0)
         {
            DispatchAdif(xml);
            return;
         }

         // Strict parse failed. Some N1MM-compatible loggers (notably TR4W)
         // emit mismatched open/close tag casing such as
         // <IsClaimedQso>...</IsClaimedQSO> — not well-formed XML, but every
         // production VB6 build of this gateway accepted it because the
         // original code did substring scanning on a lowercased copy of the
         // XML. Match that tolerance by retrying once with tag names folded
         // to lowercase. Values are untouched.
         try
         {
            doc = XDocument.Parse(NormalizeTagCasing(xml));
         }
         catch (XmlException lenientEx)
         {
            InvalidMessageReceived?.Invoke(xml,
               $"XML parse error: {strictEx.Message} (lenient retry also failed: {lenientEx.Message})");
            return;
         }
      }

      var root = doc.Root;
      if (root == null)
      {
         InvalidMessageReceived?.Invoke(xml, "No root element");
         return;
      }

      switch (root.Name.LocalName.ToLowerInvariant())
      {
         case "contactinfo":
            if (XmlHelpers.TrueValue(root, "isoriginal"))
            {
               ContactInfoReceived?.Invoke(root);
            }
            // Non-original contactinfo is N1MM's spotting broadcast — silently ignored,
            // matching the VB6 behaviour. Logging it as "invalid" would be noisy.
            break;
         case "contactreplace":
            // A QSO edited in N1MM. Same element shape as contactinfo, plus
            // <oldcall> and <oldtimestamp> giving the pre-edit identity.
            //
            // The isoriginal filter is applied here for the same reason as on
            // contactinfo — a non-original copy is a spotting broadcast, not a
            // logging instruction. N1MM's own contactreplace carries
            // <IsOriginal>True</IsOriginal> in the capture we have; treating a
            // non-original one as an edit would delete a real QSO on the
            // strength of a spot, which is the more dangerous mistake.
            if (XmlHelpers.TrueValue(root, "isoriginal"))
            {
               ContactReplaceReceived?.Invoke(root);
            }
            break;
         case "lookupinfo":
            LookupInfoReceived?.Invoke(root);
            break;
         case "contactdelete":
            ContactDeleteReceived?.Invoke(root);
            break;
         default:
            if (KnownUnhandledTypes.Contains(root.Name.LocalName))
            {
               UnhandledMessageReceived?.Invoke(root.Name.LocalName);
            }
            else
            {
               InvalidMessageReceived?.Invoke(xml, $"Unrecognized message type: {root.Name.LocalName}");
            }
            break;
      }
   }

   // Matches "<" or "</" followed by a tag name (letter then word chars or
   // hyphens). The <?xml ?> processing instruction starts with "<?" so it
   // doesn't match. Attribute values stay outside the captured group so
   // they're untouched.
   private static readonly Regex TagNameRegex =
      new(@"<(/?)([A-Za-z][\w-]*)", RegexOptions.Compiled);

   private static string NormalizeTagCasing(string xml) =>
      TagNameRegex.Replace(xml, m => "<" + m.Groups[1].Value + m.Groups[2].Value.ToLowerInvariant());
}