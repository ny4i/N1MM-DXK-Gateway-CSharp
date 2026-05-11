using System.Xml.Linq;

namespace N1MM_DXK_GW;

/// <summary>
/// Case-insensitive XElement lookups. The VB6 original lowercases the whole
/// XML buffer before searching, so all tag matches are effectively case-
/// insensitive. N1MM sends consistently cased XML, but matching VB6's
/// behaviour exactly avoids silent breakage if N1MM ever varies casing.
/// </summary>
internal static class XmlHelpers
{
   public static XElement? Find(XElement parent, string localName)
   {
      foreach (var e in parent.Elements())
      {
         if (string.Equals(e.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase))
         {
            return e;
         }
      }
      return null;
   }

   public static string GetValue(XElement parent, string localName)
   {
      return Find(parent, localName)?.Value?.Trim() ?? string.Empty;
   }

   public static bool TrueValue(XElement parent, string localName)
   {
      return string.Equals(GetValue(parent, localName), "true", StringComparison.OrdinalIgnoreCase);
   }
}
