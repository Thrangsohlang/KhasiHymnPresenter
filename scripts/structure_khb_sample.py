from __future__ import annotations

import csv
import json
import re
import zipfile
from dataclasses import dataclass, field, asdict
from pathlib import Path
from xml.etree import ElementTree as ET


WORD_NS = {"w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main"}
VERSE_START_RE = re.compile(r"^\s*(\d+)\s*[\.\)]?\s*(.+?)\s*$")
TITLE_NUMBER_RE = re.compile(r"^\s*(\d+)\.\s*(.+?)\s*$")

# Keep the cleaning conservative. Only remove obvious extraction noise that is
# highly unlikely to be part of the hymn text.
KNOWN_NOISE_REPLACEMENTS = (
    ("rieh,Na", "rieh, Na"),
    ("Shakhyrdopjong", "Shakhyrdop jong"),
    ("I'U", "I' U"),
    ("Ia ngi, U ri ia lade.", "Ia ngi, U ri ia lade."),
    ("b’U la thaw;U Jingiaroh", "b’U la thaw; U Jingiaroh"),
)

SUSPICIOUS_PATTERNS = {
    "contains_non_latin_character": re.compile(r"[^\u0009\u000A\u000D\u0020-\u024F\u2018\u2019]"),
    "contains_known_ocr_artifact": re.compile(
        r"\b(?:Isaiah|norm|fen|Lion|Uا|UE I|ud A|all|nii|my|dems|for nad d)\b"
    ),
    "contains_trailing_page_number": re.compile(r"\b\d{2,}\s*$"),
}

MANUAL_REVIEW_FLAGS = {
    (4, 4): ["possible_overlap_with_hymn_5_verse_1"],
}


@dataclass
class Paragraph:
    index: int
    text: str
    is_bold: bool
    is_numbered: bool


@dataclass
class Verse:
    number: int
    text: str
    source_paragraphs: list[int] = field(default_factory=list)
    review_flags: list[str] = field(default_factory=list)


@dataclass
class Hymn:
    number: int
    title: str
    verses: list[Verse] = field(default_factory=list)


def iter_docx_paragraphs(docx_path: Path) -> list[Paragraph]:
    with zipfile.ZipFile(docx_path) as archive:
        root = ET.fromstring(archive.read("word/document.xml"))

    paragraphs: list[Paragraph] = []
    for index, paragraph in enumerate(root.find("w:body", WORD_NS).findall("w:p", WORD_NS), start=1):
        texts: list[str] = []
        is_bold = False
        is_numbered = False

        p_props = paragraph.find("w:pPr", WORD_NS)
        if p_props is not None:
            if p_props.find("w:numPr", WORD_NS) is not None:
                is_numbered = True
            run_props = p_props.find("w:rPr", WORD_NS)
            if run_props is not None and run_props.find("w:b", WORD_NS) is not None:
                is_bold = True

        for node in paragraph.iter():
            tag = node.tag.rsplit("}", 1)[-1]
            if tag == "r":
                run_props = node.find("w:rPr", WORD_NS)
                if run_props is not None and run_props.find("w:b", WORD_NS) is not None:
                    is_bold = True
            elif tag == "t":
                texts.append(node.text or "")
            elif tag == "br":
                texts.append("\n")

        text = normalize_whitespace("".join(texts))
        if text:
            paragraphs.append(Paragraph(index=index, text=text, is_bold=is_bold, is_numbered=is_numbered))

    return paragraphs


def normalize_whitespace(text: str) -> str:
    cleaned_lines: list[str] = []
    for raw_line in text.replace("\xa0", " ").splitlines():
        line = re.sub(r"\s+", " ", raw_line).strip()
        if not line:
            continue
        line = re.sub(r"\s+([,;:.!?])", r"\1", line)
        line = re.sub(r"([,;:.!?])(?=[^\s\"'])", r"\1 ", line)
        cleaned_lines.append(line)
    return "\n".join(cleaned_lines).strip()


def clean_verse_text(text: str) -> str:
    cleaned = normalize_whitespace(text)
    for old, new in KNOWN_NOISE_REPLACEMENTS:
        cleaned = cleaned.replace(old, new)
    return cleaned.strip()


def review_flags_for_text(text: str) -> list[str]:
    flags: list[str] = []
    for flag_name, pattern in SUSPICIOUS_PATTERNS.items():
        if pattern.search(text):
            flags.append(flag_name)
    return flags


def parse_hymns(paragraphs: list[Paragraph]) -> list[Hymn]:
    hymns: list[Hymn] = []
    current_hymn: Hymn | None = None
    current_verse: Verse | None = None
    next_hymn_number = 1
    next_verse_number = 1

    for paragraph in paragraphs:
        if paragraph.is_bold:
            if current_verse is not None and current_hymn is not None:
                current_hymn.verses.append(current_verse)
                current_verse = None
            if current_hymn is not None:
                hymns.append(current_hymn)

            title_match = TITLE_NUMBER_RE.match(paragraph.text)
            if title_match:
                hymn_number = int(title_match.group(1))
                title = title_match.group(2).strip()
                next_hymn_number = hymn_number + 1
            else:
                hymn_number = next_hymn_number
                title = paragraph.text.strip()
                next_hymn_number += 1

            current_hymn = Hymn(number=hymn_number, title=title)
            next_verse_number = 1
            continue

        if current_hymn is None:
            continue

        verse_match = VERSE_START_RE.match(paragraph.text)
        if verse_match:
            if current_verse is not None:
                current_hymn.verses.append(current_verse)
            verse_number = int(verse_match.group(1))
            verse_text = clean_verse_text(verse_match.group(2))
            current_verse = Verse(
                number=verse_number,
                text=verse_text,
                source_paragraphs=[paragraph.index],
                review_flags=review_flags_for_text(verse_text),
            )
            next_verse_number = verse_number + 1
            continue

        if paragraph.is_numbered or current_verse is None:
            if current_verse is not None:
                current_hymn.verses.append(current_verse)
            verse_text = clean_verse_text(paragraph.text)
            current_verse = Verse(
                number=next_verse_number,
                text=verse_text,
                source_paragraphs=[paragraph.index],
                review_flags=review_flags_for_text(verse_text),
            )
            next_verse_number += 1
            continue

        current_verse.text = clean_verse_text(f"{current_verse.text}\n{paragraph.text}")
        current_verse.source_paragraphs.append(paragraph.index)
        current_verse.review_flags = review_flags_for_text(current_verse.text)

    if current_verse is not None and current_hymn is not None:
        current_hymn.verses.append(current_verse)
    if current_hymn is not None:
        hymns.append(current_hymn)

    for hymn in hymns:
        for verse in hymn.verses:
            verse.review_flags.extend(MANUAL_REVIEW_FLAGS.get((hymn.number, verse.number), []))
            verse.review_flags = sorted(set(verse.review_flags))

    return hymns


def write_json(output_path: Path, hymns: list[Hymn], source_file: Path) -> None:
    payload = {
        "source_file": str(source_file).replace("\\", "/"),
        "hymn_count": len(hymns),
        "hymns": [asdict(hymn) for hymn in hymns],
    }
    output_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def write_csv(output_path: Path, hymns: list[Hymn]) -> None:
    with output_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(
            handle,
            fieldnames=[
                "hymn_number",
                "title",
                "verse_number",
                "text",
                "source_paragraphs",
                "review_flags",
            ],
        )
        writer.writeheader()
        for hymn in hymns:
            for verse in hymn.verses:
                writer.writerow(
                    {
                        "hymn_number": hymn.number,
                        "title": hymn.title,
                        "verse_number": verse.number,
                        "text": verse.text,
                        "source_paragraphs": ",".join(str(index) for index in verse.source_paragraphs),
                        "review_flags": ",".join(verse.review_flags),
                    }
                )


def write_review_notes(output_path: Path, hymns: list[Hymn]) -> None:
    lines = [
        "# Khasi Hymn Sample Review Notes",
        "",
        "These entries were structured automatically from the Word document with conservative cleaning.",
        "Any verse listed below should be checked against the source before wider import.",
        "",
    ]

    flagged_count = 0
    for hymn in hymns:
        for verse in hymn.verses:
            if not verse.review_flags:
                continue
            flagged_count += 1
            lines.append(
                f"- Hymn {hymn.number}, Verse {verse.number}: {', '.join(verse.review_flags)} "
                f"(source paragraphs: {', '.join(str(index) for index in verse.source_paragraphs)})"
            )
            lines.append(f'  Text: "{verse.text}"')

    if flagged_count == 0:
        lines.append("No review flags were generated.")

    output_path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> None:
    repo_root = Path(__file__).resolve().parent.parent
    source_file = repo_root / "Data" / "Consolidated" / "KHB_Consolidated.docx"
    output_dir = repo_root / "Data" / "Structured"
    output_dir.mkdir(parents=True, exist_ok=True)

    paragraphs = iter_docx_paragraphs(source_file)
    hymns = parse_hymns(paragraphs)

    write_json(output_dir / "khb_sample_001_010.json", hymns, source_file)
    write_csv(output_dir / "khb_sample_001_010.csv", hymns)
    write_review_notes(output_dir / "khb_sample_001_010_review.md", hymns)

    print(f"Structured {len(hymns)} hymns into {output_dir}")


if __name__ == "__main__":
    main()
