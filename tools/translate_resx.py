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
        "externallog", "deleteqso",
        # With their angle brackets: escaped as &lt;oldcall&gt; these sent the
        # Ukrainian model into a degenerate run of "=" hundreds of characters
        # long. Masked as a unit they are simply passed through.
        "<oldcall>", "<oldtimestamp>", "oldcall", "oldtimestamp",
        # The word DXKeeper's own English UI displays, quoted in our text.
        "Listening",
        "QSO", "ADIF", "RST", "UDP", "TCP", "DDE", "XML", "IPv4",
        # The DXKeeper menu paths are whole entries in DO_NOT_TRANSLATE_KEYS
        # and reach sentences only through a {0}, so they need no term masking.
    ],
    key=len,
    reverse=True,
)

PLACEHOLDER = re.compile(r"\{\d+\}")

# Masking uses HTML rather than a text sentinel, because a text sentinel does
# not survive a non-Latin target. Measured: "QQ0ZZ" comes back from Korean as
# "사이트맵" (the engine translated it as the word "sitemap") and from Ukrainian
# as "КК1ЗЗ" (transliterated into Cyrillic). Any Latin-letter marker is just
# another word to a translation model.
#
# LibreTranslate's format:"html" mode parses the input and translates only text
# nodes, leaving element tags alone. So each placeholder and protected term
# becomes an empty element, which comes back untouched in every language tested
# (ko, uk, ja, zh-Hans). Note that WRAPPING a term is not enough - "<b>DXKeeper
# </b>" returned as "<b>DX保持器</b>", tag preserved and content translated.
# The term has to BE the tag.
#
# The win over splitting the string at its placeholders is context: the engine
# still sees one whole sentence and can order it naturally.
SENTINEL = re.compile(r'<x\s+id="(\d+)"\s*/?>(?:</x>)?')


def mask(text):
    """Replace placeholders and protected terms with empty HTML elements."""
    originals = []
    marker = "\x00%d\x00"

    def take(match):
        originals.append(match.group(0))
        return marker % (len(originals) - 1)

    text = PLACEHOLDER.sub(take, text)
    for term in PROTECTED_TERMS:
        while term in text:
            originals.append(term)
            text = text.replace(term, marker % (len(originals) - 1), 1)

    # Escape only the operator-visible text. Masked content is already out of
    # the way, so "<oldcall>" in a source string cannot be mistaken for markup.
    text = (text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;"))
    text = text.replace("\n", "<br>")

    for i in range(len(originals)):
        text = text.replace(marker % i, f'<x id="{i}"></x>')
    return text, originals


def unmask(text, originals):
    """Restore the masked content and undo the HTML transport encoding.

    Returns None if any sentinel is missing, which tells the caller to keep the
    English text rather than write damaged output.
    """
    seen = set()

    def put(match):
        i = int(match.group(1))
        seen.add(i)
        return originals[i] if i < len(originals) else match.group(0)

    text = SENTINEL.sub(put, text)
    if seen != set(range(len(originals))):
        return None

    text = re.sub(r"<br\s*/?>", "\n", text)
    return (text.replace("&lt;", "<").replace("&gt;", ">")
                .replace("&quot;", '"').replace("&#39;", "'")
                .replace("&amp;", "&"))


def translate(text, target):
    body = json.dumps({
        "q": text, "source": "en", "target": target, "format": "html",
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
            bare = re.sub(r"<br\s*/?>", "", SENTINEL.sub("", masked))
            if not re.search(r"[A-Za-z]", bare):
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
            elif len(restored) > 3 * len(english) + 40:
                # Degeneration guard. A translation model that loses its way
                # emits a long repeated run - one produced 250 "=" characters
                # in the middle of a sentence. No honest translation of a UI
                # string grows by that much, and the damage is not otherwise
                # detectable: placeholders and masked terms all survive it.
                problems.append(
                    f"{name}: output degenerated "
                    f"({len(english)} chars -> {len(restored)})")
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
