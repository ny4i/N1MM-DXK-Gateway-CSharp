#!/usr/bin/env python3
"""
Translate Strings.resx into a satellite Strings.<culture>.resx via LibreTranslate.

    python tools/translate_resx.py de ja uk        # add what is new or changed
    python tools/translate_resx.py de --force      # retranslate everything

INCREMENTAL BY DEFAULT, AND THAT IS THE POINT.

These are machine translations that humans correct afterwards. A tool that
retranslated everything on every run would throw that work away the first time
a single string was added, so by default an entry is left exactly as it is when
the satellite already holds a translation made from the same English text.

Only two things cost a request: a key the satellite does not have, and a key
whose English source has changed since it was translated - where the existing
translation is now describing something else and has to be redone.

The English source each entry was translated from is stored in that entry's
<comment>, so the comparison needs no separate state file and survives anyone
hand-editing the .resx in Poedit. Keys dropped from Strings.resx disappear on
the next run.

Use --force only when you mean to discard human corrections.

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
        # The marker on the two most serious messages the gateway emits. Six of
        # thirteen languages translated it as a word - Japanese turned it into
        # "メニュー" (menu) - which silently removed the one visual cue that says
        # "DXKeeper no longer holds this QSO at all".
        "***",
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


# Pulls the English source back out of a satellite entry's comment. That
# snapshot is what lets the tool tell "already translated" from "the English
# changed underneath it" without keeping a separate state file.
STORED_EN = re.compile(r"^EN: (.*?)(?:\n    NOTE: |\n    NOT TRANSLATED: |$)",
                       re.DOTALL)


def load_existing(path):
    """Read a satellite as {name: (value, english_it_was_translated_from)}."""
    if not path.exists():
        return {}
    out = {}
    for data in ET.parse(path).getroot().findall("data"):
        name = data.get("name")
        value = data.find("value")
        note = data.find("comment")
        if name is None or value is None:
            continue
        stored = None
        if note is not None and note.text:
            m = STORED_EN.match(note.text)
            if m:
                stored = m.group(1)
        out[name] = (value.text or "", stored)
    return out


def main(targets, force=False):
    tree = ET.parse(RESX)
    root = tree.getroot()

    entries = []
    for data in root.findall("data"):
        name = data.get("name")
        value = data.find("value")
        if name is None or value is None or value.text is None:
            continue
        note = data.find("comment")
        entries.append((name, value.text, note.text if note is not None else None))

    print(f"{len(entries)} strings in Strings.resx")

    for target in targets:
        out = RESX.parent / f"Strings.{target}.resx"
        existing = {} if force else load_existing(out)
        translated = {}
        problems = []
        kept = 0
        sent = 0

        for i, (name, english, _note) in enumerate(entries, 1):
            if name in DO_NOT_TRANSLATE_KEYS:
                translated[name] = english
                kept += 1
                continue

            # Already translated FROM THIS EXACT ENGLISH - leave it alone.
            # This is what makes the tool safe to re-run once a human has
            # corrected the machine output: their work is never overwritten,
            # and only genuinely new or genuinely changed strings cost a
            # request. A changed English source falls through and is
            # retranslated, because the old translation is now wrong.
            prior, stored_en = existing.get(name, (None, None))
            if prior is not None and stored_en == english:
                translated[name] = prior
                kept += 1
                continue

            masked, originals = mask(english)

            # Nothing left to translate once the protected terms are masked -
            # e.g. AppTitle, which is entirely a product name. Sending it would
            # only give the engine a chance to damage the sentinel.
            bare = re.sub(r"<br\s*/?>", "", SENTINEL.sub("", masked))
            if not re.search(r"[A-Za-z]", bare):
                translated[name] = english
                kept += 1
                continue

            sent += 1
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
        print(f"wrote {out.name} - {kept} kept, {sent} translated"
              + (f", {len(problems)} left in English:" if problems else ""))
        for p in problems:
            print(f"    {p}")


def write(path, translated, entries):
    """Emit a satellite .resx: the standard header, then values only.

    Each entry's <comment> carries the English source AND the neutral file's
    translator note, because these are machine translations that a human has to
    review. The source alone is not enough: the note is what says "keep the ***
    marker", "this is a fragment, no full stop", or "{0} is a filename - do not
    translate it". Poedit shows the comment beside each string, so a reviewer
    gets both without opening the neutral file alongside.
    """
    header = RESX.read_text(encoding="utf-8")
    prologue = header[: header.index("  <data name=")]

    def esc(s):
        return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")

    parts = [prologue]
    for name, english, note in entries:
        comment = f"EN: {esc(english)}"
        if note:
            comment += f"\n    NOTE: {esc(note)}"
        if translated[name] == english:
            comment += ("\n    NOT TRANSLATED: left in English deliberately - "
                        "either a do-not-translate entry, or the machine pass "
                        "damaged it and was rejected.")
        parts.append(f'  <data name="{name}" xml:space="preserve">\n'
                     f"    <value>{esc(translated[name])}</value>\n"
                     f"    <comment>{comment}</comment>\n"
                     f"  </data>\n")
    parts.append("</root>\n")
    path.write_text("".join(parts), encoding="utf-8")


if __name__ == "__main__":
    args = [a for a in sys.argv[1:] if a != "--force"]
    if not args:
        sys.exit(__doc__)
    main(args, force="--force" in sys.argv[1:])
