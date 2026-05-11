namespace N1MM_DXK_GW;

static class Program
{
   // VB6 used App.PrevInstance to enforce a single instance per Windows session.
   // We use a named Mutex with no namespace prefix — that's the per-session
   // local namespace, matching VB6 semantics. Two users on the same machine
   // can still each run their own copy.
   private const string SingleInstanceMutexName = "N1MM-DXKeeper-Gateway-SingleInstance";

   [STAThread]
   static void Main()
   {
      using var mutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var createdNew);
      if (!createdNew)
      {
         MessageBox.Show(
            "N1MM-DXKeeper Gateway is already running.",
            "N1MM-DXKeeper Gateway",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
         return;
      }

      ApplicationConfiguration.Initialize();
      Application.Run(new MainForm());

      // Mutex is released by the `using` block when Application.Run returns.
   }
}
