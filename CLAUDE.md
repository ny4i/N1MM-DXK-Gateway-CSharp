# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Project Is

A C# / .NET 8 WinForms rewrite of the VB6 application at `c:\projects\N1MM DXK Gateway`.

The gateway bridges two amateur radio applications:
- **N1MM Logger+** — contest logging software that broadcasts QSO (contact) data as XML over UDP port 12060
- **DXKeeper** — logbook software that receives QSOs via TCP port 52001 and DDE

Secondary integrations via DDE: **DXView** (callsign lookup) and **Pathfinder** (QSL info).

The full VB6 architecture is documented in `c:\projects\N1MM DXK Gateway\CLAUDE.md`. Read that file for deep background on the original design. The C# port is a faithful rewrite — the logic, message formats, and invariants are the same.

---

## Build & Run

```powershell
# Build
dotnet build N1MM_DXK_GW\N1MM_DXK_GW.csproj

# Run
dotnet run --project N1MM_DXK_GW\N1MM_DXK_GW.csproj

# There are no automated tests — verification is done by running the app against live N1MM traffic or simulated UDP packets.
```

**Target framework is `net8.0-windows` — do not upgrade.** .NET 8 is LTS and was chosen deliberately.

---

## Data Flow

```
N1MM Logger+ --[UDP XML : 12060]--> Gateway --[TCP : 52001]--> DXKeeper (log QSO)
                                            --[DDE]---------> DXKeeper (callsign check)
                                            --[DDE]---------> DXView   (callsign lookup)
                                            --[DDE]---------> Pathfinder (QSL info)
```

### Message Processing Pipeline

1. **UDP reception** (`UdpClient` on port 12060): raw bytes pushed into a `ConcurrentQueue<string>`.
2. **Dequeue timer** (`System.Windows.Forms.Timer`, 100ms): drains the queue, calls `HandleData`. A plain `bool` re-entry guard (`HandlingDataFlag`) prevents overlapping calls — safe because the WinForms timer fires on the UI thread.
3. **HandleData**: for each message:
   - Validates XML preamble
   - Extracts root element name as `MessageType`
   - Dispatches:
     - `"contactinfo"` with `<isoriginal>true</isoriginal>` → build ADIF record → send to DXKeeper via TCP
     - `"lookupinfo"` → DDE callsign check/lookup
     - `"contactdelete"` → parsed but **not forwarded** (unimplemented, matches VB6 behavior)
4. **XML parsing**: use `System.Xml.Linq.XDocument` / `XElement` (replaces the VB6 manual string search approach).
5. **ADIF construction**: produces `<FIELDNAME:N>value` framed fields for the DXKeeper TCP wire format `<command:N>externallog<parameters:N>...`.
6. **TCP send**: connects to DXKeeper at registry base port + 1 (default 52001), sends framed message, fire-and-forget (no response expected but log at debug level).

---

## Key Design Decisions

### Settings / Registry Paths

Use the same registry path as the VB6 app so existing user settings survive upgrade:
- **Gateway settings:** `HKCU\Software\VB and VBA Program Settings\N1MM-DXKeeper Gateway\N1MM-DXK-Gateway`
- **DXKeeper TCP port:** `HKCU\Software\VB and VBA Program Settings\DXKeeper\TCPServer\ServiceBasePort` (default `52000`; actual port = base + 1)

Use `Microsoft.Win32.Registry` (already a NuGet dep) for all registry access.

### DDE Connections

Three `NDde.Client.DdeClient` instances:
- `DXKeeper|DDEServer` / `DDECommand`
- `DXView|DDEServer` / `DDECommand`
- `Pathfinder|DDEServer` / `DDECommand`

Auto-reconnect on disconnect using a 10-second `System.Windows.Forms.Timer`. DDE is **secondary** — implement TCP logging first; DDE can be stubbed.

`Specshell.NDde` wraps Win32 DDEML only. It requires STA (already enforced by `[STAThread]` in `Program.cs`).

### Frequency Formatting

N1MM sends frequency in **tens of hertz** (e.g., `1442000000` = 144.200 MHz). The `FormatFrequency` conversion must **not** use locale-sensitive formatting (`double.ToString()` with a culture format). Always use `CultureInfo.InvariantCulture`. This was a real bug in VB6 v1.2.0 fixed in v1.2.1.

### Single-Instance Guard

`Form_Load` should check for an existing process with the same name and exit if one is found.

### Queue Overflow

Log to `ErrorLog.txt` on overflow; overwrite (data loss) rather than blocking — same behavior as VB6.

### 1-Byte UDP Spurious Wakeup

Silently ignore UDP datagrams of length ≤ 1 (Microsoft KB Q260018 — a known Winsock quirk that also affects .NET `UdpClient`).

---

## NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `Specshell.NDde` | 4.0.0 | DDE client (DXKeeper, DXView, Pathfinder) |
| `Microsoft.Win32.Registry` | 5.0.0 | Settings persistence in Windows Registry |

---

## VB6 → C# Component Mapping

| VB6 | C# |
|-----|----|
| Winsock UDP control | `System.Net.Sockets.UdpClient` |
| Winsock TCP control | `System.Net.Sockets.TcpClient` |
| DDE Label controls | `NDde.Client.DdeClient` |
| `GetSetting` / `SaveSetting` | `Microsoft.Win32.Registry` |
| Manual XML string parsing | `System.Xml.Linq.XDocument` / `XElement` |
| VB6 `Timer` control | `System.Windows.Forms.Timer` |
| Circular queue (depth 64) | `System.Collections.Concurrent.ConcurrentQueue<string>` |
| `Common.Log` / `Common.DebugLog` | Simple `Logger` class writing to `ErrorLog.txt` |
| `HandlingDataFlag` re-entry guard | `bool` field on the form (UI thread only) |

---

## Known Issues NOT to Reproduce from VB6

These bugs were fixed before the C# port began — do not reintroduce:

- `contactdelete` handler extracted timestamp instead of callsign
- `HandlingDataCount` never reset after re-entry bursts
- `IsAnARRLSection` was missing CO, IA, KS, MN, MO, ND, NE, SD and had a duplicate NV
- `SendLogNewQSO` and a duplicate `BooleanParamString` were dead code
- DXKeeper TCP response was silently discarded (should log at debug level)
- Unrecognized UDP message types fell through silently (should log at debug level)

---

## Coding Standards

- **3-space indentation**, spaces only (no tabs)
- Explicit braces on all blocks, even single-statement bodies
- No inline `if` on one line
