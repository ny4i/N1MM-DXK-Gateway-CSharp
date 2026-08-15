using System.Reflection;

namespace N1MM_DXK_GW;

/// <summary>
/// Appends undeliverable QSOs to FailedQSOs.adi next to the executable, so the
/// operator can import them into DXKeeper by hand.
///
/// This exists because of a hard invariant carried over from the VB6 gateway
/// (N1MM_DXK_Module.SaveFailedQSO, v1.3.3): <b>no QSO is ever discarded
/// silently</b>. Equally important is what this class deliberately does NOT do —
/// it never retries. DXKeeper does not detect duplicate QSOs (measured: 40 sends
/// of 20 distinct calls produced 39 records), so an automatic retry of a QSO
/// DXKeeper may already have processed would duplicate it in the log. Capturing
/// the record for operator review is the only safe recovery.
///
/// The file format matches the VB6 implementation byte-for-byte in structure:
/// two lines of human-readable instructions, a blank line, an ADIF header, then
/// one record per line.
///
/// Thread-safe: the send queue's worker thread and the shutdown flush can both
/// call Save.
/// </summary>
public sealed class FailedQsoStore
{
   public const string FileName = "FailedQSOs.adi";

   private readonly object writeLock = new();
   private readonly string path;
   private readonly Logger logger;

   public FailedQsoStore(Logger logger, string? path = null)
   {
      this.logger = logger;
      this.path = path ?? Path.Combine(AppContext.BaseDirectory, FileName);
   }

   public string Path_ => path;

   /// <summary>
   /// Append one ADIF record. Returns true if the record reached the file.
   /// A false return means the QSO is genuinely lost, which is why the failure
   /// is escalated to ErrorLog.txt rather than swallowed.
   /// </summary>
   public bool Save(string adifRecord, string reason)
   {
      if (string.IsNullOrWhiteSpace(adifRecord))
      {
         return false;
      }

      lock (writeLock)
      {
         try
         {
            var needHeader = !File.Exists(path);
            using var writer = new StreamWriter(path, append: true);
            if (needHeader)
            {
               WriteHeader(writer);
            }
            writer.WriteLine(adifRecord);
         }
         catch (Exception ex)
         {
            // Last resort: we could not persist a QSO we already failed to
            // deliver. Say so loudly in the error log — this is real data loss.
            logger.Log($"FailedQsoStore: COULD NOT SAVE QSO to {path} ({ex.Message}); reason for save was: {reason}; record: {adifRecord}");
            return false;
         }
      }

      // Logging here also drives the UI's "errors have been logged" link,
      // so the operator is told the file exists without a separate event.
      logger.Log($"FailedQsoStore: {reason}; QSO saved to {path}");
      return true;
   }

   private static void WriteHeader(StreamWriter writer)
   {
      var asm = Assembly.GetEntryAssembly() ?? typeof(FailedQsoStore).Assembly;
      var version = asm.GetName().Version?.ToString() ?? "unknown";

      writer.WriteLine("QSOs that the N1MM-DXKeeper Gateway could not deliver to DXKeeper.");
      writer.WriteLine("Import this file into DXKeeper, then delete it.");
      writer.WriteLine();

      // ADIF header fields use the same <name:len>value encoding as the DXLab
      // wire protocol, so EncodeField serves both.
      writer.Write(DxLabWire.EncodeField("ADIF_VER", "3.1.4"));
      writer.Write(DxLabWire.EncodeField("PROGRAMID", "N1MM-DXKeeper Gateway"));
      writer.WriteLine(DxLabWire.EncodeField("PROGRAMVERSION", version));
      writer.WriteLine("<EOH>");
   }
}
