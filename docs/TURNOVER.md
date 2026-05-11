# N1MM-DXK-Gateway C# Port — Turnover Document

## What This Project Is

A C# / .NET 8 WinForms rewrite of the VB6 application at `c:\projects\N1MM DXK Gateway`.

The gateway sits between two amateur radio applications:
- **N1MM Logger+** — contest logging software that broadcasts QSO data as XML over UDP
- **DXKeeper** — logbook software that receives QSOs via TCP and DDE

It also optionally interacts with **DXView** (callsign lookup) and **Pathfinder** (QSL info), both via DDE.

The full architecture of the original is documented in:
`c:\projects\N1MM DXK Gateway\CLAUDE.md`

**Read that file before writing any code.** It describes the data flow, message queue design, XML parsing approach, DDE connection model, and key invariants in detail.

---

## Data Flow

```
N1MM Logger+ --[UDP XML : 12060]--> Gateway --[TCP : 52001]--> DXKeeper (log QSO)
                                            --[DDE]---------> DXKeeper (check callsign)
                                            --[DDE]---------> DXView   (lookup callsign)
                                            --[DDE]---------> Pathfinder (QSL info)
```

---

## Project Structure

```
c:\projects\N1MM-DXK-Gateway-CSharp\
   docs\
      TURNOVER.md          ← this file
   N1MM_DXK_GW\
      N1MM_DXK_GW.csproj   ← .NET 8 WinForms, targets windows
      Program.cs            ← entry point (dotnet scaffold)
      Form1.cs              ← rename to MainForm.cs
      Form1.Designer.cs     ← rename to MainForm.Designer.cs
```

---

## Environment

- **OS:** Windows 11
- **IDE:** Visual Studio 2026
- **.NET SDKs installed:** 8.0 (LTS), 9.0, 10.0
- **Target framework:** `net8.0-windows` — do not upgrade, .NET 8 is LTS and was chosen deliberately
- **GitHub repo:** https://github.com/ny4i/N1MM-DXK-Gateway-CSharp

---

## NuGet Packages Already Added

| Package | Version | Purpose |
|---------|---------|---------|
| Specshell.NDde | 4.0.0 | DDE client — sends commands to DXKeeper, DXView, Pathfinder |
| Microsoft.Win32.Registry | 5.0.0 | Read/write Windows Registry for settings persistence |

**Specshell.NDde notes:**
- Security reviewed and approved — wraps Win32 DDEML API only, no network calls or telemetry
- Use `DdeClient.Execute(command, timeout)` to send DDE commands
- The library requires an STA message pump — it handles this internally via a hidden WinForms window
- License is "Shared Source License for NDde" (custom permissive, not OSI-certified — acceptable for this project)

---

## First Things to Do in This Session

### 1. Remove obj/ from git tracking
The `obj/` folder was accidentally committed before the `.gitignore` was added. Clean it up:
```
cd "c:\projects\N1MM-DXK-Gateway-CSharp"
git rm -r --cached N1MM_DXK_GW/obj/
git commit -m "Remove obj/ from tracking"
git push origin main
```

### 2. Rename Form1 to MainForm
The scaffold creates `Form1.cs` / `Form1.Designer.cs`. Rename both to `MainForm.cs` / `MainForm.Designer.cs` and update the class name and `Program.cs` accordingly.

### 3. Add CLAUDE.md
Run `/init` to generate a CLAUDE.md for this repo documenting the C# architecture as it develops.

---

## VB6 → C# Mapping

| VB6 Component | C# Equivalent |
|---------------|---------------|
| Winsock UDP control | `System.Net.Sockets.UdpClient` |
| Winsock TCP control | `System.Net.Sockets.TcpClient` |
| DDE Label controls (3×) | `NDde.Client.DdeClient` instances |
| `GetSetting` / `SaveSetting` | `Microsoft.Win32.Registry` |
| Manual XML string parsing | `System.Xml.Linq.XDocument` / `XElement` |
| VB6 `Timer` control | `System.Windows.Forms.Timer` |
| `App.ProductName` / `App.Revision` | `Application.ProductName` / `Assembly.GetName().Version` |
| `Common.Log` / `Common.DebugLog` | Implement a simple `Logger` class writing to `ErrorLog.txt` |

---

## Key Design Decisions to Preserve

### Message Queue
The VB6 code uses a circular buffer (depth 64) to dequeue UDP messages. In C#, this can be replaced with a `System.Collections.Concurrent.ConcurrentQueue<string>` drained by a `System.Windows.Forms.Timer` — cleaner and thread-safe without the manual pointer arithmetic.

### Re-entrancy Guard
The VB6 `HandlingDataFlag` boolean prevents `HandleData` from being called while already processing. In C#, use `System.Threading.Interlocked.CompareExchange` or a simple `bool` flag — the WinForms timer fires on the UI thread so a plain `bool` is sufficient.

### DDE Connections
Three DDE connections are maintained with auto-reconnect timers (10s interval):
- `DXKeeper|DDEServer` / `DDECommand`
- `DXView|DDEServer` / `DDECommand`
- `Pathfinder|DDEServer` / `DDECommand`

DDE is **only used for secondary features** (callsign check and lookup). The primary QSO logging path uses TCP and should be implemented first. DDE can be stubbed and filled in later.

### TCP to DXKeeper
DXKeeper's TCP base port is read from the Windows Registry:
- Key: `HKCU\Software\VB and VBA Program Settings\DXKeeper\TCPServer\ServiceBasePort`
- Default: `52000`
- Actual port used: base port + 1 = `52001`

Message format: `<command:N>externallog<parameters:N>...` where fields are encoded as `<fieldname:length>value`.

### Settings Storage
VB6 used `SaveSetting` / `GetSetting` which writes to:
`HKCU\Software\VB and VBA Program Settings\N1MM-DXKeeper Gateway\`

The C# port should use the same registry path for backwards compatibility so existing user settings are preserved on upgrade.

### Frequency Format
N1MM sends frequency in **tens of hertz** (e.g., `1442000000` = 144.200 MHz). The VB6 `FormatFrequency` function converts this to a decimal string without using locale-sensitive formatting. Replicate this carefully — the original had a bug with locale decimal separators that was fixed in v1.2.1.

---

## Known Issues in the VB6 Code (Already Fixed in This Port's Source Branch)

These were fixed in the `code-review-fixes` branch and merged to main before the C# port began — they should **not** be reproduced in the rewrite:

- `contactdelete` handler was calling the wrong function to extract the callsign
- `HandlingDataCount` was never reset after re-entry bursts
- `IsAnARRLSection` was missing 8 Midwest sections (CO, IA, KS, MN, MO, ND, NE, SD) and had a duplicate NV entry
- `SendLogNewQSO` and a duplicate `BooleanParamString` were dead code (superseded in v1.1.6)
- DXKeeper TCP response was silently discarded — should be logged at debug level
- Unrecognized UDP message types fell through silently — should log at debug level
- `contactdelete` is parsed but **not forwarded to DXKeeper** — this remains unimplemented

---

## Coding Standards

Per the global `CLAUDE.md` at `C:\Users\toms\.claude\CLAUDE.md`:
- **3-space indentation**
- No tabs — spaces only
- `begin`/`end` rules don't apply in C#, but keep blocks explicit and readable
