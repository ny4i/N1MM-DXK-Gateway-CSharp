using System.Collections.Concurrent;
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
      catch (XmlException ex)
      {
         // WSJT-X by default broadcasts ADIF (not XML) on a configurable UDP port.
         // If the user has WSJT-X pointed at our port we want to give a useful hint
         // rather than just "XML parse error".
         var reason = xml.IndexOf("<EOR>", StringComparison.OrdinalIgnoreCase) >= 0
            ? "Received ADIF instead of N1MM XML — check WSJT-X Settings > Reporting (disable 'Enable logged contact ADIF broadcast' or change its port)"
            : "XML parse error: " + ex.Message;
         InvalidMessageReceived?.Invoke(xml, reason);
         return;
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
}
