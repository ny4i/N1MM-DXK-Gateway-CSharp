# Licensing

N1MM-DXKeeper Gateway
Copyright (C) 2026 Tom Schaefer, NY4I

This program is free software: you can redistribute it and/or modify it under
the terms of the **GNU General Public License, version 3** or (at your option)
any later version, as published by the Free Software Foundation. The full text
is in [COPYING](COPYING).

This program is distributed in the hope that it will be useful, but **WITHOUT
ANY WARRANTY**; without even the implied warranty of MERCHANTABILITY or FITNESS
FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

SPDX-License-Identifier: `GPL-3.0-or-later`

---

## Additional permission under GNU GPL version 3 section 7

You have permission to link N1MM-DXKeeper Gateway with the **NDde** library
(`Specshell.NDde`), which is distributed under the Microsoft Shared Source
License for NDde, and to convey the resulting executable, provided that NDde's
own copyright and licence notices are preserved and distributed with it.

This permission is granted by the copyright holder named above.

**Why it is needed.** NDde's licence requires that object-form distribution be
made "only under a license that complies with this license", which is not
straightforwardly compatible with the GPL. NDde is linked into this program
rather than run as a separate process, so without this permission it would be
unclear whether the combined binary could be conveyed under the GPL at all.
Granting the exception resolves that, and costs users nothing: NDde remains
freely usable for any purpose, commercial or not.

If NDde is ever replaced by a direct DDEML implementation, this permission
becomes unnecessary and can be removed.

---

## Third-party components

| Component | Licence | Redistributed |
|---|---|---|
| [NDde](https://github.com/Specshell/specshell.software.ndde) (`Specshell.NDde`) | Microsoft Shared Source License for NDde — [text](third-party-licenses/NDde-LICENSE.txt) | yes |
| [WPF-UI](https://github.com/lepoco/wpfui) | MIT — [text](third-party-licenses/WPF-UI-LICENSE.md) | yes |
| `Microsoft.Win32.Registry` | MIT (part of .NET, https://github.com/dotnet/runtime) | resolves to the in-box .NET assembly |

The application icon is derived from the icon of the original VB6 gateway, the
work of the same copyright holder.

---

## Relationship to other programs

The gateway talks to **N1MM Logger+**, **DXKeeper**, **DXView** and
**Pathfinder** over UDP, TCP and DDE. Those are separate programs communicating
at arm's length over documented interfaces; none of them is linked into this
one, and this licence places no obligation on any of them. Their names are the
trademarks or property of their respective authors and are used here only to
say what this program interoperates with.

The DXLab Suite wire formats this program implements are the work of
**AA6YQ (Dave Bernstein)**. The constants and the `<name:length>value` field
encoding in `DxLabWire.cs` are facts about that protocol, reimplemented here
for interoperability rather than copied.

---

## Translations

The satellite `.resx` translations under `N1MM_DXK_GW/I18N/` are covered by this
same licence. Note that the GNU GPL text itself must be conveyed in English:
the Free Software Foundation does not treat translations of the licence as
legally valid, so `COPYING` is not translated even though the user interface is.
