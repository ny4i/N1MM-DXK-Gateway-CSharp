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
   public event Action<XElement>? LookupInfoReceived;
   public event Action<XElement>? ContactDeleteReceived;
   public event Action<string, string>? InvalidMessageReceived; // (rawXml, reason)

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
            ProcessOne(xml);
         }
      }
      finally
      {
         isDraining = false;
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
         // WSJT-X broadcasts ADIF (not XML) on a configurable UDP port by
         // default. If the user has WSJT-X pointed at our port we want a
         // useful hint rather than just "XML parse error".
         if (xml.IndexOf("<EOR>", StringComparison.OrdinalIgnoreCase) >= 0)
         {
            InvalidMessageReceived?.Invoke(xml,
               "Received ADIF instead of N1MM XML — check WSJT-X Settings > Reporting (disable 'Enable logged contact ADIF broadcast' or change its port)");
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
         case "lookupinfo":
            LookupInfoReceived?.Invoke(root);
            break;
         case "contactdelete":
            ContactDeleteReceived?.Invoke(root);
            break;
         default:
            InvalidMessageReceived?.Invoke(xml, $"Unknown message type: {root.Name.LocalName}");
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
