// SPDX-License-Identifier: GPL-3.0-or-later

using System.Globalization;
using System.IO;
using System.Reflection;

namespace N1MM_DXK_GW;

public sealed class Logger
{
   // 100 MB matches the VB6 cap. When exceeded, debug logging is auto-disabled
   // so a stuck debug build can't fill the user's disk.
   private const long MaxLogSizeBytes = 100_000_000;

   private readonly object writeLock = new();
   private readonly string logPath;
   private bool startupHeaderWritten;

   public bool DebugEnabled { get; set; }

   // Fired (on the calling thread) after a successful Log() write so the UI
   // can surface its "see ErrorLog" hint. Handlers must marshal to the UI thread.
   public event Action? LogWritten;

   public Logger(string? logPath = null)
   {
      this.logPath = logPath ?? Path.Combine(AppContext.BaseDirectory, "ErrorLog.txt");
   }

   public string LogPath => logPath;

   public void Log(string message)
   {
      lock (writeLock)
      {
         try
         {
            if (File.Exists(logPath) && new FileInfo(logPath).Length > MaxLogSizeBytes)
            {
               // Cap reached. Silently drop further writes and stop generating debug
               // output, matching the VB6 SetDebug(False) behavior.
               DebugEnabled = false;
               return;
            }

            using var writer = new StreamWriter(logPath, append: true);
            if (!startupHeaderWritten)
            {
               WriteStartupHeader(writer);
               startupHeaderWritten = true;
            }
            writer.WriteLine(
               $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)} > {message}");
         }
         catch
         {
            // Logging must never crash the app. Swallow IO errors silently.
            return;
         }
      }

      try
      {
         LogWritten?.Invoke();
      }
      catch
      {
         // A bad subscriber must not poison the logger.
      }
   }

   public void DebugLog(string message)
   {
      if (DebugEnabled)
      {
         Log(message);
      }
   }

   private static void WriteStartupHeader(StreamWriter writer)
   {
      var asm = Assembly.GetEntryAssembly() ?? typeof(Logger).Assembly;
      var version = asm.GetName().Version?.ToString() ?? "unknown";
      var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

      writer.WriteLine();
      writer.WriteLine($"{stamp}     > N1MM-DXKeeper Gateway version {version} (C# port)");
      writer.WriteLine($"{stamp}     > App.Path          : {AppContext.BaseDirectory}");
      writer.WriteLine($"{stamp}     > Operating System  : {Environment.OSVersion}");
      writer.WriteLine($"{stamp}     > .NET runtime      : {Environment.Version}");
      writer.WriteLine($"{stamp}     > Locale            : {CultureInfo.CurrentCulture.Name}");
   }
}