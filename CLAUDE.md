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
6. **Send queue** (`QsoSendQueue`): ADIF records are enqueued, not sent inline. A single worker sends one QSO at a time and waits for DXKeeper's acknowledgement before starting the next. This is not an optimisation — see *QSO delivery* below.
7. **TCP send** (`DxKeeperTcpClient`): connects to DXKeeper at registry base port + 1 (default 52001), writes the framed message, then **waits for DXKeeper to close the connection**. That close is the only acknowledgement DXKeeper gives. Any other ending means the QSO was not delivered.

---

## Key Design Decisions

### Settings / Registry Paths

Use the same registry path as the VB6 app so existing user settings survive upgrade:
- **Gateway settings:** `HKCU\Software\VB and VBA Program Settings\N1MM-DXKeeper Gateway\N1MM-DXK-Gateway`
- **DXKeeper TCP port:** `HKCU\Software\VB and VBA Program Settings\DXKeeper\TCPServer\ServiceBasePort` (default `52000`; actual port = base + 1)

Use `Microsoft.Win32.Registry` (already a NuGet dep) for all registry access.

**The DXKeeper TCP port is derived, never configured.** Read `ServiceBasePort` and add 1. Do not add a setting, command-line flag, or UI field to override it: the derivation is what guarantees the gateway is pointed wherever DXKeeper actually listens, and a settable port could aim at somewhere nothing serves. An override was proposed as a test affordance and rejected for this reason.

**DXKeeper's registry hive is read-only to us.** Reading `ServiceBasePort` is the design; writing anything under DXKeeper's key is not ours to do — that is the live configuration of a working station. To test the delivery-failure path, stop DXKeeper.

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

Both queues (`MessageDispatcher`'s inbound `ConcurrentQueue<string>` and `QsoSendQueue`'s outbound channel) are **unbounded**, so there is no overflow path and no data loss.

Earlier revisions of this file said to overwrite the oldest entry on overflow. That was wrong, and contradicted VB6, which discarded the *arriving* datagram at depth 256 so queued messages kept FIFO order. Bounding either queue would mean choosing a QSO to throw away; memory is not the constraint (a queued QSO is a few hundred bytes and DXKeeper drains in seconds).

### QSO delivery — do not weaken these

Established by measurement against live DXKeeper on 2026-08-15 (see the VB6 repo's `CLAUDE.md`), and they are properties of DXKeeper, not of VB6:

- **DXKeeper acknowledges `externallog` by closing the connection, and by nothing else.** There is no reply body.
- **A successful `Write` proves nothing.** Windows completes the handshake into the listen backlog, so a connection DXKeeper has not yet accepted still reports connected and accepts a write. Closing at that point destroys the command with no error at either end — measured at 5 of 20, then 12 of 20 QSOs silently lost. `DxKeeperTcpClient` therefore waits up to 10 s for a zero-length read and reports `Unconfirmed` otherwise.
- **DXKeeper can be seconds behind** (4.5 s lags in its own log — it drains TCP commands through an internal DDE queue while doing callbook and award work). Hence `QsoSendQueue`: one send in flight, paced to DXKeeper's acknowledgement rather than to N1MM's send rate. Sending concurrently just produces rejected sends.
- **No QSO is discarded silently.** Anything not confirmed — `Failed`, `Unconfirmed`, `Busy`, or still queued at shutdown — is appended to `FailedQSOs.adi` next to the executable by `FailedQsoStore`, for the operator to import by hand.
- **Never retry automatically.** DXKeeper does not detect duplicate QSOs (40 sends of 20 distinct calls produced 39 records), so retrying a QSO it may already have processed would duplicate it. `FailedQSOs.adi` is the recovery path.

### 1-Byte UDP Spurious Wakeup

Silently ignore UDP datagrams of length ≤ 1 (Microsoft KB Q260018 — a known Winsock quirk that also affects .NET `UdpClient`).

### `SO_REUSEADDR` is set — and whether that shares or splits depends on the address

`UdpListener` sets `SO_REUSEADDR` before `Bind` (Windows only honours it then). The behaviour differs by how the datagram is addressed:

| Addressing | Delivered to |
|---|---|
| unicast | exactly **one** bound socket |
| broadcast | **every** socket bound to the port |
| multicast | every socket bound to the port **that joined the group** |

Measured on the development network — two sockets, one port, `SO_REUSEADDR` set: a subnet broadcast reached 2 of 2, a unicast reached 1 of 2. A packet capture shows N1MM Logger+ sending to `192.168.x.255`, i.e. subnet broadcast; TR4W's `UDP BROADCAST ADDRESS` is a user-set destination, commonly also a broadcast address.

So for the traffic this gateway actually receives, `SO_REUSEADDR` lets it coexist with other consumers on the same port, each getting a full copy — which is required, since the operator runs TR4W alongside.

**What it costs:** the exclusive bind used to be a backstop against a second copy of the gateway double-logging every QSO. That now rests entirely on the single-instance mutex in `Program.cs`, which is per-session — two Windows sessions on one machine could each run a gateway and both log the same broadcast QSOs.

**Multicast reception is not implemented.** It needs a group join (`JoinMulticastGroup`) in addition to `SO_REUSEADDR`, and there is no configured group address. If TR4W is ever pointed at a multicast group, this must be added or the gateway will receive nothing.

An earlier revision of this file argued the opposite — that `SO_REUSEADDR` must never be set — reasoning from the unicast rule alone. That was wrong for the broadcast traffic this gateway actually receives.

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
| Circular queue (depth 256) | `System.Collections.Concurrent.ConcurrentQueue<string>` (unbounded) |
| `Common.Log` / `Common.DebugLog` | Simple `Logger` class writing to `ErrorLog.txt` |
| `HandlingDataFlag` re-entry guard | `bool` field on the form (UI thread only) |
| Synchronous `SendCommand` (paced the queue implicitly) | `QsoSendQueue` — one send in flight, awaits acknowledgement |
| `PeerClosedFlag` wait in `SendCommand` | `DxKeeperTcpClient.WaitForPeerCloseAsync`, 10 s |
| `N1MM_DXK_Module.SaveFailedQSO` | `FailedQsoStore` → `FailedQSOs.adi` |

---

## Known Issues NOT to Reproduce from VB6

These bugs were fixed before the C# port began — do not reintroduce:

- `contactdelete` handler extracted timestamp instead of callsign
- `HandlingDataCount` never reset after re-entry bursts
- `IsAnARRLSection` was missing CO, IA, KS, MN, MO, ND, NE, SD and had a duplicate NV
- `SendLogNewQSO` and a duplicate `BooleanParamString` were dead code
- DXKeeper TCP response was silently discarded (should log at debug level)
- Unrecognized UDP message types fell through silently (should log at debug level)

### Defects introduced by the C# port itself, since fixed

- **A TCP send that timed out waiting for DXKeeper's close was reported as "logged QSO".** The wait existed but its timeout was swallowed, which is exactly the VB6 v1.3.3 bug — conflating "the local stack took the bytes" with "DXKeeper got it". Now surfaces as `SendOutcome.Unconfirmed`.
- **The peer-close wait was 2 s**, below DXKeeper's measured 4.5 s queue lag. Now 10 s, matching VB6.
- **A half-close (`Shutdown(SocketShutdown.Send)`) was sent after the write.** The VB6 client cannot do this and delivers reliably without it, and the command frame is self-delimiting, so DXKeeper needs no FIN to know the request is complete. Removed rather than left as untested behaviour against an unaccepted backlog connection.
- **Sends were fire-and-forget from the dispatcher's drain loop**, so a burst started concurrent sends and the single-in-flight guard discarded all but the first. Now serialised through `QsoSendQueue`.
- **Undeliverable QSOs were logged as text and dropped.** Now written to `FailedQSOs.adi`.
- **`SO_REUSEADDR` was set on the UDP socket** with a comment claiming it enabled port sharing. It does not — see *One receiver per UDP port* above. Removed.
- **`ChannelReader.Count` crashed the gateway on the first QSO.** `QsoSendQueue` created its channel with `SingleReader = true`, which selects `SingleConsumerUnboundedChannel`; that reader does not implement `Count` and throws `NotSupportedException: Specified method is not supported.` Reading it from `OnContactInfo` on the UI thread was an unhandled WinForms exception — the app died before building a single ADIF record, having received every datagram. `PendingCount` is now an explicit `Interlocked` counter, independent of which channel implementation the options select.
- **`RadioInfo` was reported as an invalid message.** It is a known N1MM type the gateway simply does not handle, broadcast several times a second — reporting it as invalid would bury real problems. Known-but-unhandled types are now noted once per session at debug level and never surface in the operation log; a genuinely *unrecognized* root element is still reported, since that is how a misconfigured sender becomes visible.

### Failure containment

`MessageDispatcher.Drain` runs on the UI thread, so an exception escaping a handler is an unhandled WinForms exception: the gateway dies mid-contest and every later QSO is lost silently. `Drain` therefore wraps each message in a try/catch and raises `DispatchFailed`, which logs the fault and full body to `ErrorLog.txt` and tells the operator the gateway is still running. One bad message, or one defect in a handler, must cost at most that message. This guard was added after the `ChannelReader.Count` crash above, which it would have contained.

---

## Coding Standards

- **3-space indentation**, spaces only (no tabs)
- Explicit braces on all blocks, even single-statement bodies
- No inline `if` on one line
