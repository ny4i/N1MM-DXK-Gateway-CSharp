#!/usr/bin/env python3
"""
Translate README.txt into README.<culture>.txt via LibreTranslate.

    python tools/translate_readme.py de ja uk
    python tools/translate_readme.py --check          # report stale translations
    python tools/translate_readme.py de --force       # redo even if current

WHY A HASH IS RECORDED IN EVERY FILE

The English README changes. When it does, every translation silently becomes a
description of an older program - and a stale instruction is worse than an
English one, because the reader has no way to tell it is wrong. It happened
during development: the README described a warning bar for hours after that bar
had been replaced.

So each translation records the SHA-256 of the English README it was made from.
The gateway compares that against the English file it actually ships and falls
back to English when they differ, rather than showing instructions for a
version that no longer exists. --check lists which need redoing.

WHY THE LAYOUT SURVIVES

This is a plain text file read in Notepad, so the shape carries meaning:
headings with underlines, an aligned two-column table of loggers, indented
blocks under file names. Translating it line by line would wreck the prose;
translating it as one lump would wreck the layout. Blocks are classified and
handled accordingly - prose is translated whole and re-wrapped, structured
lines keep their indentation and alignment, and headings have their underline
rebuilt to the length of the translated title.
"""

import re
import sys
import hashlib
import textwrap
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from translate_resx import (mask, unmask, translate, glued, repair_glued,
                            repair_smiley, SENTINEL)

ROOT = Path(__file__).resolve().parent.parent
README = ROOT / "README.txt"
WIDTH = 78

# Anything here is masked before translation, on top of the product and
# protocol names translate_resx already protects. Longest first.
EXTRA_PROTECTED = sorted(
    [
        "README.txt", "COPYING.txt", "NOTICE.txt", "ErrorLog.txt",
        "FailedQSOs_<date>_<time>.adi", "FailedQSOs", "N1MM_DXK_GW.exe",
        "JTAlert", "WSJT-X", "SDR-Control", "TR4W", "DXLab Launcher", "DXLab",
        "Desktop Runtime", "Windows Update", "Start menu",
        "Config > Configure Ports ...", "Broadcast Data",
        "Config > Defaults tab > Network Service", "Settings > Reporting",
        "Network Service", "Base Port", "Listening",
        "Enable logged contact ADIF broadcast", "External Callsign Lookup",
        "UDP BROADCAST ADDRESS", "Server port number",
        "Upload to eQSL.cc", "Upload to LoTW", "Upload to Club Log",
        "Query Callbook", "Lookup previous QSOs", "Log debugging information",
        "Operation Log", "Connection Status", "Failed QSOs",
        "Copy details", "Display Error Log", "see ErrorLog",
        "Windows 10", "Windows 11", "Notepad",
    ],
    key=len,
    reverse=True,
)

URL = re.compile(r"https?://\S+|\b\d{1,3}(?:\.\d{1,3}){3}(?::\d+)?\b|\b\d{4,5}\b"
                 r"|\bC:\\[^\s,]*|[\w.]+@[\w.]+")

HEADING_UNDERLINE = re.compile(r"^[-=]{3,}\s*$")


def english_hash():
    text = README.read_bytes().replace(b"\r\n", b"\n")
    return hashlib.sha256(text).hexdigest()[:16]


def recorded_hash(path):
    if not path.exists():
        return None
    for line in path.read_text(encoding="utf-8", errors="replace").splitlines()[:12]:
        m = re.search(r"source-sha256:\s*([0-9a-f]{16})", line)
        if m:
            return m.group(1)
    return None


def translate_text(text, target):
    """One phrase through the masking, the engine and the repairs.

    Everything protected goes through the shared HTML masking - URLs, paths and
    port numbers by pattern, README-specific names by term - because that is
    the only marker that survives a translation engine. An earlier version here
    used its own control-character markers and they came back as bare digits.
    """
    if not text.strip():
        return text

    masked, originals = mask(text, extra_patterns=[URL], extra_terms=EXTRA_PROTECTED)
    bare = re.sub(r"<br\s*/?>", "", SENTINEL.sub("", masked))
    if not re.search(r"[A-Za-z]{2}", bare):
        return text                        # nothing here but protected content

    try:
        got = translate(masked, target)
    except Exception as e:                  # noqa: BLE001
        print(f"    request failed, keeping English: {e}")
        return text

    restored = unmask(got, originals)
    if restored is None:
        return text
    restored, _ = repair_smiley(restored, text)
    restored, _ = repair_glued(restored, text)
    if glued(restored, text):
        return text
    return restored


def is_structured(block):
    """Indented, aligned or list-like lines whose shape must be preserved."""
    for line in block:
        if re.match(r"^\s{2,}\S", line) or re.search(r"\S\s{3,}\S", line):
            return True
        if re.match(r"^\s*[-*]\s", line) or re.match(r"^\s*\d+\.\s", line):
            return True
    return False


def translate_block(block, target):
    # A heading and its underline: translate the title, rebuild the rule.
    if len(block) == 2 and HEADING_UNDERLINE.match(block[1]):
        title = translate_text(block[0].strip(), target)
        return [title, block[1].strip()[0] * max(len(title), 3)]

    if all(HEADING_UNDERLINE.match(l) for l in block):
        return block

    if is_structured(block):
        out = []
        for line in block:
            if not line.strip():
                out.append(line)
                continue
            # Keep the leading indent, and any aligned label column, exactly.
            m = re.match(r"^(\s*)(\S.*?)(\s{3,})(\S.*)$", line)
            if m:
                indent, label, gap, body = m.groups()
                out.append(f"{indent}{label}{gap}{translate_text(body, target)}")
            else:
                m2 = re.match(r"^(\s*)(.*)$", line)
                out.append(f"{m2.group(1)}{translate_text(m2.group(2), target)}")
        return out

    # Prose: translate whole so the engine sees sentences, then re-wrap.
    indent = re.match(r"^(\s*)", block[0]).group(1)
    joined = " ".join(l.strip() for l in block)
    done = translate_text(joined, target)
    return textwrap.wrap(done, width=WIDTH, initial_indent=indent,
                         subsequent_indent=indent) or [indent]


def banner(target, digest):
    return [
        "=" * WIDTH,
        f" MACHINE TRANSLATION into {target}. The English README.txt is",
        " authoritative; where this file disagrees with it, it is this file",
        " that is wrong. Corrections are very welcome.",
        "",
        f" source-sha256: {digest}",
        "=" * WIDTH,
        "",
    ]


def main(targets, force=False):
    digest = english_hash()
    lines = README.read_text(encoding="utf-8").splitlines()

    blocks, current = [], []
    for line in lines:
        if line.strip():
            current.append(line)
        else:
            if current:
                blocks.append(current)
                current = []
            blocks.append([])
    if current:
        blocks.append(current)

    for target in targets:
        out = ROOT / f"README.{target}.txt"
        if not force and recorded_hash(out) == digest:
            print(f"{out.name}: already current")
            continue

        print(f"{out.name}: translating {len([b for b in blocks if b])} blocks")
        result = banner(target, digest)
        for i, block in enumerate(blocks):
            if not block:
                result.append("")
                continue
            result.extend(translate_block(block, target))
            print(f"  [{target}] block {i + 1}/{len(blocks)}", flush=True)

        out.write_text("\n".join(result) + "\n", encoding="utf-8")
        print(f"{out.name}: written")


def check():
    digest = english_hash()
    print(f"English README.txt sha256: {digest}")
    stale = []
    for path in sorted(ROOT.glob("README.*.txt")):
        got = recorded_hash(path)
        state = "current" if got == digest else f"STALE (made from {got})"
        print(f"  {path.name:22} {state}")
        if got != digest:
            stale.append(path.name)
    if stale:
        print(f"\n{len(stale)} translation(s) need redoing. The gateway shows "
              f"English for these rather than out-of-date instructions.")
    return 1 if stale else 0


if __name__ == "__main__":
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    if "--check" in sys.argv[1:]:
        sys.exit(check())
    if not args:
        sys.exit(__doc__)
    main(args, force="--force" in sys.argv[1:])
