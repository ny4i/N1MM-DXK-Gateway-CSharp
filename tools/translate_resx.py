#!/usr/bin/env python3
"""
Translate Strings.resx into a satellite Strings.<culture>.resx via LibreTranslate.

    python tools/translate_resx.py de ja uk

The translating part is trivial. The part that matters is everything around it:
a machine-translation pass will happily rewrite "{0}" into "{ 0 }", translate
"DXKeeper" as a common noun, and localise the DXKeeper menu paths that must
stay English because DXKeeper's own interface is English-only.

So each string is masked before it goes out and unmasked when it comes back,
and the result is checked before it is written. A file that fails the check is
not written at all - a broken satellite is worse than a missing one, because a
missing one falls back to English and a broken one shows damaged text.

Placeholder damage used to be able to crash the gateway; MainWindow.L() now
catches FormatException and shows the raw template instead. This script is the
first line of defence, that catch is the second.
"""

import re
import sys
import json
import time
import urllib.request
import xml.etree.ElementTree as ET
from pathlib import Path

LIBRETRANSLATE = "http://127.0.0.1:5000/translate"

RESX = Path(__file__).resolve().parent.parent / "N1MM_DXK_GW" / "I18N" / "Strings.resx"

# Keys that are menu paths inside other programs. Copied through verbatim.
DO_NOT_TRANSLATE_KEYS = {
    "DxKeeperConfigPath",
    "DxKeeperConfigNetworkService",
}

# Terms that must survive untouched: product names, protocol names, wire-format
# identifiers, and filenames. Longest first so "Club Log" masks before "Log".
PROTECTED_TERMS = sorted(
    [
        "N1MM-DXKeeper Gateway", "N1MM Logger+", "N1MM",
        "DXKeeper", "DXView", "Pathfinder", "DXLab",
        "Club Log", "LoTW", "eQSL.cc", "eQSL", "QRZ", "ARRL",
        "ErrorLog.txt", "FailedQSOs",
        "contactinfo", "contactreplace", "contactdelete", "lookupinfo",
        "externallog", "deleteqso", "oldcall", "oldtimestamp",
        "QSO", "ADIF", "RST", "UDP", "TCP", "DDE", "XML", "IPv4",
        # The DXKeeper menu paths are whole entries in DO_NOT_TRANSLATE_KEYS
        # and reach sentences only through a {0}, so they need no term masking.
    ],
    key=len,
    reverse=True,
)

PLACEHOLDER = re.compile(r"\{\d+\}")

# Alphanumeric, no spaces, no punctuation: survives a translation pass far
# better than bracketed or unicode sentinels, which get spaced out or dropped.
def token(i):
    return f"QQ{i}ZZ"


def mask(text):
    """Replace placeholders and protected terms with opaque tokens."""
    originals = []

    def take(match):
        originals.append(match.group(0))
        return token(len(originals) - 1)

    text = PLACEHOLDER.sub(take, text)
    for term in PROTECTED_TERMS:
        while term in text:
            originals.append(term)
            text = text.replace(term, token(len(originals) - 1), 1)
    return text, originals


def unmask(text, originals):
    """Restore tokens, tolerating the ways an engine damages them.

    Measured against LibreTranslate: a sentinel that lands at the end of a
    segment comes back with its doubled tail collapsed - "QQ0ZZ" returns as
    "QQ0Z" - while the same sentinel mid-sentence survives intact. Engines also
    case-fold and occasionally space out such runs. So restoration matches the
    numeric core with a tolerant fringe rather than demanding the exact token.

    Returns None if a token cannot be found at all, which is the signal for the
    caller to keep the English text rather than write damaged output.
    """
    for i, original in enumerate(originals):
        loose = re.compile(rf"[Qq]{{1,2}}\s*{i}\s*[Zz]{{1,2}}")
        text, n = loose.subn(lambda _: original.replace("\\", "\\\\"), text)
        if n == 0:
            return None
    return text


def translate(text, target):
    body = json.dumps({
        "q": text, "source": "en", "target": target, "format": "text",
    }).encode()
    req = urllib.request.Request(
        LIBRETRANSLATE, data=body, headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=120) as r:
        return json.load(r)["translatedText"]


def placeholders(text):
    return sorted(PLACEHOLDER.findall(text))


def main(targets):
    tree = ET.parse(RESX)
    root = tree.getroot()

    entries = []
    for data in root.findall("data"):
        name = data.get("name")
        value = data.find("value")
        if name is None or value is None or value.text is None:
            continue
        entries.append((name, value.text))

    print(f"{len(entries)} strings in Strings.resx")

    for target in targets:
        out = RESX.parent / f"Strings.{target}.resx"
        translated = {}
        problems = []

        for i, (name, english) in enumerate(entries, 1):
            if name in DO_NOT_TRANSLATE_KEYS:
                translated[name] = english
                continue

            masked, originals = mask(english)

            # Nothing left to translate once the protected terms are masked -
            # e.g. AppTitle, which is entirely a product name. Sending it would
            # only give the engine a chance to damage the sentinel.
            if not re.search(r"[A-Za-z]", re.sub(r"[Qq]{1,2}\d+[Zz]{1,2}", "", masked)):
                translated[name] = english
                continue

            try:
                got = translate(masked, target)
            except Exception as e:                      # noqa: BLE001
                problems.append(f"{name}: request failed ({e})")
                translated[name] = english
                continue

            restored = unmask(got, originals)
            if restored is None:
                problems.append(f"{name}: a masked term was destroyed in translation")
                translated[name] = english
            elif placeholders(restored) != placeholders(english):
                problems.append(
                    f"{name}: placeholders changed "
                    f"{placeholders(english)} -> {placeholders(restored)}")
                translated[name] = english
            else:
                translated[name] = restored

            print(f"  [{target}] {i}/{len(entries)} {name}", flush=True)
            time.sleep(0.05)

        write(out, translated, entries)
        print(f"wrote {out.name}"
              + (f" - {len(problems)} string(s) left in English:" if problems else ""))
        for p in problems:
            print(f"    {p}")


def write(path, translated, entries):
    """Emit a satellite .resx: the standard header, then values only.

    The English source is kept as the <comment> of each entry, which is what a
    reviewer needs in front of them and what Poedit shows as context.
    """
    header = RESX.read_text(encoding="utf-8")
    prologue = header[: header.index("  <data name=")]

    parts = [prologue]
    for name, english in entries:
        value = (translated[name]
                 .replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;"))
        source = (english
                  .replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;"))
        parts.append(f'  <data name="{name}" xml:space="preserve">\n'
                     f"    <value>{value}</value>\n"
                     f"    <comment>EN: {source}</comment>\n"
                     f"  </data>\n")
    parts.append("</root>\n")
    path.write_text("".join(parts), encoding="utf-8")


if __name__ == "__main__":
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    main(sys.argv[1:])
