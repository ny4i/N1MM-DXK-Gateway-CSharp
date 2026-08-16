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
     - `"contactreplace"` with `<isoriginal>true</isoriginal>` → delete the pre-edit QSO, then log the edited one (see *Editing and deleting QSOs*)
     - `"contactdelete"` → build the delete key and send `deleteqso`
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

**In user-facing text, never name that registry key.** The operator sets this in **DXKeeper: Config → Defaults tab → Network Service → Base Port**, and that panel's heading also reports whether the service is listening (`Network Service (port 52001): Listening`). Point them there — a registry path is an implementation detail they cannot act on safely.

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
- **No QSO is discarded silently.** Anything not confirmed — `Failed`, `Unconfirmed`, `Busy`, or still queued at shutdown — is appended by `FailedQsoStore` to a **per-run** `FailedQSOs_yyyyMMdd_HHmmss.adi` next to the executable, for the operator to import by hand. One file per run, created lazily on the first failure: a run that loses nothing leaves no file, which is what makes the file's existence a trustworthy alert rather than something to date-check. A status line in the window's bottom-left shows this run's count with links to the file and its folder.
  - **The count is read from the file, never tracked in memory.** Opening the file or folder does *not* clear it — nothing observable tells us the operator actually imported the records. It falls to zero only when the file is gone, which is the one signal we can trust, and is refreshed when the window regains focus so deleting the file clears it without a restart.
  - Trade-off accepted: a file from an *earlier* run stays on disk unnoticed, since the status only counts this run's.
- **Never retry automatically.** DXKeeper does not detect duplicate QSOs (40 sends of 20 distinct calls produced 39 records), so retrying a QSO it may already have processed would duplicate it. `FailedQSOs.adi` is the recovery path.
- **An acknowledgement means enqueued, not executed.** DXKeeper closes the connection once it has put the command on its internal DDE queue. Verified in DXKeeper's own log during a replace: delete enqueued at `05:23:58.353`, re-log at `05:23:58.378`, delete *completed* at `05:24:05.573`, re-log parsed at `05:24:05.809`. Ordering across commands is therefore guaranteed by DXKeeper's FIFO queue, not by our waiting.

### Editing and deleting QSOs

`contactdelete` → `deleteqso`. `contactreplace` → `deleteqso` followed by `externallog`, queued as one inseparable operation.

- **QSO identity is `CALL` + `QSO_DATE` + `TIME_ON`, and nothing else.** Confirmed against TR4W's `BuildDXKeeperDeleteMessage` (`c:\tr4w-d12\tr4w\src\uExternalLogger.pas`), which sends exactly those three. This is what lets a stateless go-between handle edits at all — the gateway cannot query either program's database, and every field of the key arrives in the datagram that asks for the change.
- **`deleteqso` parameters are raw ADIF, NOT wrapped in `<ExternalLogADIF:N>`** — unlike `externallog`. Wrapping them makes DXKeeper match nothing.
- **Delete must precede re-log.** An edit to a non-key field (name, comment, exchange) leaves the identity unchanged, so re-log-first would leave two identical records and the delete would remove an arbitrary one — or both.
- **`contactreplace` supplies `<oldcall>` and `<oldtimestamp>`**, the pre-edit identity, so an edit that changes the callsign or the time still deletes the right record. Build the delete key from those, falling back to the current values only if blank.
- **The delete-succeeded / re-log-failed window is unavoidable** — DXKeeper has no atomic replace. When it happens the QSO is gone from DXKeeper entirely: report it at the top of the operator's attention and preserve the edited record in `FailedQSOs.adi`.
- **When the *delete* half fails, do not preserve the edited record.** DXKeeper still holds the original, so importing the edited copy later would duplicate it.

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

### Multicast reception

Optional, off by default: the **Multicast group** field (registry value `MulticastGroup` under our own key) takes an IPv4 group address, and blank means unicast and broadcast only — which is what N1MM Logger+ sends today. TR4W's `UDP BROADCAST ADDRESS` is free-form, so it can be pointed at a group once its sender-side support lands.

- **The join is additive.** The socket binds `0.0.0.0:port` and then joins, so unicast and broadcast keep arriving. Enabling multicast never costs the operator traffic they were already receiving — verified: with a group joined, a unicast datagram still arrived and was logged.
- **`SO_REUSEADDR` alone is not enough.** Verified: with no group joined, a multicast datagram to the listening port was *not* received at all (0 datagrams); with the group joined, it arrived and reached DXKeeper.
- **One interface, deliberately.** The join uses the interface the routing table selects, not every interface. Joining on all of them is more forgiving on a multi-homed machine, but if the sender's datagrams then arrived on two interfaces every QSO would be received and logged **twice**, and DXKeeper does not detect duplicates. Receiving nothing is obvious within seconds; silent duplicates are found later, by hand. The accepted failure mode is a machine whose default route is not the radio LAN, which is why the joined group is reported in the operation log rather than joined silently. A per-interface setting is the fix if it ever bites.
- **A bad group is reported, not ignored.** An address that is not in `224.0.0.0 – 239.255.255.255` is refused with a message and the listener still starts without multicast — a silent fallback would leave a healthy-looking gateway receiving nothing.

An earlier revision of this file argued the opposite — that `SO_REUSEADDR` must never be set — reasoning from the unicast rule alone. That was wrong for the broadcast traffic this gateway actually receives.

### WPF-UI controls

**Read `docs/WPF-UI-NOTES.md` before changing the window.** WPF-UI controls
several times do something other than what the markup says, and every case in
that file was found by measuring the running window rather than by reading the
XAML — a keyed `Style` without `BasedOn` stripping a control template, an
`InfoBar` silently ignoring its `Content`, a `HyperlinkButton` ignoring
`Foreground`, a `Card` centring instead of filling, a `CardExpander` that does
not animate at all, and a scrollbar drawn over the content it scrolls.

Note also that WPF-UI is WPF, **not** WinUI 3. Guidance found online for WinUI
3, the Windows Community Toolkit or UWP usually describes a different control
with a different template.

### Notification area

`TrayIcon` wraps WinForms' `NotifyIcon`. WPF has no tray support of its own, but this project already references WinForms for NDde's hidden window, so this costs no new package and no new assembly. Everything on it must be touched from the UI thread — it needs a message pump.

- **Minimise hides to the tray; close still quits.** Both gestures keep their usual meaning. Nobody should discover by accident that the gateway they thought they shut down is still the only thing logging their contest, or that the one they meant to tuck away has stopped.
- **The refresh timer runs only while hidden.** A gateway sits up for a whole contest; a 1 Hz timer that ticked regardless would spend all of it waking the CPU to recompute text the window already shows.
- **Failure balloons are rate-limited to one a minute.** A DXKeeper outage fails every QSO, and one balloon each would bury the shack describing a condition the operator has already been told about. The red badge on the icon persists between notices.
- **The tooltip is clamped to 127 characters.** Windows caps it there and .NET throws rather than truncating. Measured on .NET 8: 127 accepted, 128 rejected (it was 63 on .NET Framework). Longest translated tooltip today is 78, so the clamp is a guard, not a routine path.
- **`WithAlertBadge` must `DestroyIcon` the handle from `GetHicon`.** `Icon.FromHandle` does not take ownership, so the returned icon is `Clone`d and the handle released; otherwise every construction leaks a GDI handle.

**Windows 11 puts new notification icons in the overflow flyout by default.** Verified on this machine: after minimising, the icon was not on the taskbar and was found only under "Show Hidden Icons". This is why the red badge alone cannot be the error indication — an operator who has not dragged the icon out will never see it. The balloon notification is the channel that reaches them, and the undelivered count is also in `FailedQSOs_*.adi` and the window's own warning bar. Worth telling operators to pin the icon.

`SaveWindowPosition` reads `RestoreBounds` rather than returning early when minimised. It used to skip saving in that state to avoid recording a taskbar-minimised window's coordinates; quitting from the tray menu now makes minimised the ordinary path, and returning early would silently discard a resize made earlier in the session.

### Localization

Strings live in `N1MM_DXK_GW\I18N\`. `Strings.Designer.cs` is **checked in**, because WPF runs `MarkupCompilePass1` before `PrepareResources` and forcing the reverse is circular. Regenerate it with `dotnet msbuild N1MM_DXK_GW.csproj -t:RegenerateStrings`.

**The `LogicalName` rule in the `.csproj` is load-bearing.** MSBuild derives a manifest resource name from the file's path, so the subfolder alone would embed `N1MM_DXK_GW.I18N.Strings` while the generated code looks up `N1MM_DXK_GW.Strings`. One rule handles both the neutral file and every satellite:

```xml
<EmbeddedResource Update="I18N\*.resx">
  <LogicalName>N1MM_DXK_GW.%(Filename).resources</LogicalName>
</EmbeddedResource>
```

`%(Filename)` strips only the final extension, so `Strings.resx` → `N1MM_DXK_GW.Strings.resources` and `Strings.de.resx` → `N1MM_DXK_GW.Strings.de.resources`.

**The culture must be in the satellite's resource name.** `ResourceManager` composes what it looks for as `BaseName + "." + culture + ".resources"`, so inside the German satellite it asks for `N1MM_DXK_GW.Strings.de.resources`, *not* the neutral name. Forcing every culture file to the culture-less name produced a satellite with the correct assembly identity (`Culture=de`), in the correct folder, loadable by identity, containing all 85 strings — that `ResourceManager` then silently ignored. No exception; the UI simply stayed English. Both failure modes here are runtime-only and survive a clean build with zero warnings, so **verify a language change actually renders** after touching any of this.

Adding a language means dropping `I18N\Strings.<culture>.resx` in. No project or code change: the rule above names it correctly, MSBuild reads the culture from the filename and routes it into the matching satellite, and `Localization.AvailableTranslations` finds it by scanning for `<culture>\N1MM_DXK_GW.resources.dll` next to the executable rather than from a list in code.

**What is translated, and what is deliberately not:**

| | Translated |
|---|---|
| Window chrome, dialogs | yes |
| Operation-log lines reporting a problem the operator must act on | yes |
| Operation-log routine traffic lines (`contactinfo: …`, `lookupinfo: …`) | **no** |
| `ErrorLog.txt`, `FailedQSOs_*.adi` | **no** |
| Menu paths inside DXKeeper | **no** — its interface is English-only, so a translated path names a menu that does not exist |

The routine log lines are mostly wire identifiers, and the log is what an operator pastes into a support thread. Same reason `ErrorLog.txt` is English.

- **Only `CurrentUICulture` is set, never `CurrentCulture`.** Windows regional settings keep deciding number and date formatting. This also keeps the invariant frequency formatting (the VB6 v1.2.0 bug) out of the blast radius.
- **The language applies on restart, and the UI says so.** XAML resolves 45 `x:Static` references at load time, so a live switch would repaint nothing; rebuilding the visual tree to fake it is a lot of machinery for a setting touched once.
- **`SendResult` carries a `SendFailure` enum, not just `ErrorMessage`.** The message is English and goes to `ErrorLog.txt` verbatim; the enum is what the UI maps to a translated sentence. Do not collapse them back into one string — that would translate the support artefact.
- **Translated log lines must pass severity explicitly** to `AppendLog(line, level)`. `LogEntry.Classify` derives severity by grepping English phrases (`"ERROR"`, `"***"`, `"did not confirm"`); a translated failure line would classify as Normal and lose its colour, silently, in exactly the languages that most need it to stand out.
- **`MainWindow.L()` catches `FormatException`** and shows the raw template. Translations are produced outside this repo and a damaged placeholder (`{ 0 }`, an invented `{2}`) would otherwise throw on the send-queue worker and kill the gateway mid-contest. `tools/translate_resx.py` rejects such a string before writing it; the catch is the second line of defence.
- **No idioms in source strings.** "Another send was already in flight" machine-translated into German as a literal statement about aviation. It now reads "another send had not finished yet".

### Producing translations

`tools/translate_resx.py` against a local LibreTranslate on `127.0.0.1:5000` is the whole pipeline — there is no other tool in the loop:

```
python tools/translate_resx.py cs de es fi fr it ja ko nl pt ru uk zh-Hans
```

It masks `{N}` placeholders and protected terms (product names, protocol names, wire identifiers, `<oldcall>`, `Listening`) before each request and restores them after, one string at a time.

**Masking is HTML, not a text sentinel, and must stay that way.** A Latin-letter marker is just another word to a translation model. Measured: `QQ0ZZ` came back from Korean as `사이트맵` — the engine translated the sentinel as the word "sitemap" — and from Ukrainian as `КК1ЗЗ`, transliterated into Cyrillic. So each masked item is sent as an empty element, `<x id="0"></x>`, with `format: "html"`; LibreTranslate parses that and translates only text nodes. Note that *wrapping* a term does not protect it — `<b>DXKeeper</b>` returned as `<b>DX保持器</b>`, tag preserved and content translated. The term has to **be** the tag. Splitting each string at its placeholders would protect them too, but sending whole sentences keeps the context the model needs to order them.

Three checks reject a string and leave it in English rather than write damaged output — placeholder set changed, a masked term missing, or the output more than ~3× the source. That last one catches model degeneration, which is otherwise undetectable: escaped `&lt;oldcall&gt;` once produced a run of 250 `=` characters mid-sentence with every placeholder and masked term intact.

Output is machine quality and wants a native review before being advertised — Ukrainian renders "Connection Status" as "status on servers", Japanese renders "Help" as "Contact us". Each entry carries its English source as a `<comment>`, which is what Poedit shows a reviewer beside each string.

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
