#!/usr/bin/env python3
"""
Convert mechanics draft markdown files to HTML pages for site/mechanics/.
Usage: python3 tools/md_to_mechanics_html.py
Run from repo root.
"""

import os
import re
import sys
from pathlib import Path

DRAFTS_DIR  = Path("site/mechanics-drafts")
OUTPUT_DIR  = Path("site/mechanics")
SIDEBAR_GROUPS = [
    ("City Services", [
        ("Healthcare & Ambulances",   "healthcare-ambulances.html"),
        ("Health & Disease",          "health-disease.html"),
        ("Fire Department",           "fire.html"),
        ("Crime & Police",            "crime-and-police.html"),
        ("Garbage Collection",        "garbage-collection.html"),
        ("Education",                 "education.html"),
        ("Deathcare",                 "deathcare.html"),
        ("Parks & Recreation",        "parks-recreation.html"),
        ("Public Transit",            "public-transit.html"),
        ("Mail & Post Service",       "mail-post.html"),
        ("Telecom & Internet",        "telecom-internet.html"),
    ]),
    ("Infrastructure", [
        ("Electricity",               "electricity.html"),
        ("Water & Sewage",            "water-sewage.html"),
        ("Traffic & Roads",           "traffic-roads.html"),
        ("Traffic Accidents",         "traffic-accidents.html"),
        ("Parking",                   "parking.html"),
        ("Personal Cars",             "personal-cars.html"),
        ("Cargo & Freight",           "cargo-transport.html"),
        ("Outside Connections",       "outside-connections.html"),
    ]),
    ("Economy", [
        ("Economy & Budget",          "economy-budget.html"),
        ("Companies & Commerce",      "companies-commerce.html"),
        ("Industry & Resources",      "industry-resources.html"),
        ("Terrain & Natural Resources","terrain-resources.html"),
        ("Land Value & Property",     "land-value.html"),
        ("Tourism",                   "tourism.html"),
        ("Jobs & Labor Market",       "jobs-labor.html"),
        ("Demand & Growth",           "demand-growth.html"),
    ]),
    ("Population & Citizens", [
        ("Citizens & Households",     "citizens-households.html"),
        ("Zoning & Development",      "zoning.html"),
        ("Building Construction",     "building-construction.html"),
    ]),
    ("Environment", [
        ("Pollution & Noise",         "pollution.html"),
        ("Weather & Climate",         "weather-climate.html"),
        ("Disasters",                 "disasters.html"),
    ]),
    ("Governance", [
        ("Districts & Policies",      "districts-policies.html"),
        ("Milestones & Progression",  "milestones-progression.html"),
        ("Map Expansion (Tiles)",     "map-expansion.html"),
    ]),
]


def build_sidebar(active_file: str) -> str:
    parts = ['<aside class="sidebar">']
    for group_name, pages in SIDEBAR_GROUPS:
        parts.append(f'  <h3 class="sidebar-toggle">{group_name}</h3>')
        parts.append('  <div class="sidebar-links">')
        for label, href in pages:
            cls = ' class="active"' if href == active_file else ''
            parts.append(f'    <a href="{href}"{cls}>{label}</a>')
        parts.append('  </div>')
    parts.append('</aside>')
    return "\n".join(parts)


def escape_html(text: str) -> str:
    return text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def inline_markup(text: str) -> str:
    """Convert inline markdown: **bold**, *italic*, `code`."""
    # Bold (** or __)
    text = re.sub(r'\*\*(.+?)\*\*', r'<strong>\1</strong>', text)
    text = re.sub(r'__(.+?)__',     r'<strong>\1</strong>', text)
    # Italic (* or _) — only single markers remaining
    text = re.sub(r'(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)', r'<em>\1</em>', text)
    text = re.sub(r'(?<!_)_(?!_)(.+?)(?<!_)_(?!_)',       r'<em>\1</em>', text)
    # Inline code
    text = re.sub(r'`([^`]+)`', r'<code>\1</code>', text)
    return text


def md_to_html_body(md: str):
    """
    Convert markdown text to HTML body content.
    Returns (page_title, html_body).
    """
    lines = md.splitlines()
    html = []
    page_title = "Game Mechanics"

    i = 0
    in_blockquote = False
    blockquote_lines: list[str] = []
    paragraph_lines: list[str] = []

    def flush_paragraph():
        nonlocal paragraph_lines
        if paragraph_lines:
            combined = " ".join(paragraph_lines).strip()
            if combined:
                html.append(f"<p>{inline_markup(combined)}</p>")
            paragraph_lines = []

    def flush_blockquote():
        nonlocal blockquote_lines, in_blockquote
        if blockquote_lines:
            inner = "\n".join(blockquote_lines)
            # Split into paragraphs inside blockquote
            bq_paras = [p.strip() for p in re.split(r'\n\s*\n', inner) if p.strip()]
            bq_html = "\n".join(f"<p>{inline_markup(p)}</p>" for p in bq_paras)
            html.append(f"<blockquote>\n{bq_html}\n</blockquote>")
            blockquote_lines = []
        in_blockquote = False

    while i < len(lines):
        line = lines[i]

        # ATX headers
        m = re.match(r'^(#{1,6})\s+(.*)', line)
        if m:
            flush_paragraph()
            flush_blockquote()
            level = len(m.group(1))
            text  = inline_markup(m.group(2).strip())
            if level == 1 and page_title == "Game Mechanics":
                page_title = m.group(2).strip()
            html.append(f"<h{level}>{text}</h{level}>")
            i += 1
            continue

        # Horizontal rule
        if re.match(r'^-{3,}\s*$', line) or re.match(r'^\*{3,}\s*$', line):
            flush_paragraph()
            flush_blockquote()
            html.append("<hr>")
            i += 1
            continue

        # Blank line
        if line.strip() == "":
            flush_paragraph()
            if in_blockquote:
                blockquote_lines.append("")
            i += 1
            continue

        # Blockquote
        if line.startswith(">"):
            flush_paragraph()
            in_blockquote = True
            content = re.sub(r'^>\s?', '', line)
            blockquote_lines.append(content)
            i += 1
            continue
        else:
            if in_blockquote:
                flush_blockquote()

        # Unordered list item
        m = re.match(r'^[-*+]\s+(.*)', line)
        if m:
            flush_paragraph()
            # Collect all list items
            items = []
            while i < len(lines) and re.match(r'^[-*+]\s+(.*)', lines[i]):
                items.append(re.match(r'^[-*+]\s+(.*)', lines[i]).group(1))
                i += 1
            html.append("<ul>")
            for item in items:
                html.append(f"  <li>{inline_markup(item)}</li>")
            html.append("</ul>")
            continue

        # Ordered list item
        m = re.match(r'^\d+\.\s+(.*)', line)
        if m:
            flush_paragraph()
            items = []
            while i < len(lines) and re.match(r'^\d+\.\s+(.*)', lines[i]):
                items.append(re.match(r'^\d+\.\s+(.*)', lines[i]).group(1))
                i += 1
            html.append("<ol>")
            for item in items:
                html.append(f"  <li>{inline_markup(item)}</li>")
            html.append("</ol>")
            continue

        # Regular text — accumulate into paragraph
        paragraph_lines.append(line.strip())
        i += 1

    flush_paragraph()
    flush_blockquote()

    return page_title, "\n".join(html)


def build_page(title: str, body: str, active_file: str) -> str:
    sidebar = build_sidebar(active_file)
    return f"""<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>{title} - CS2 Game Mechanics</title>
  <link rel="stylesheet" href="../style.css">
</head>
<body>

<div class="top-bar">
  <a href="../index.html" class="site-title">CS2 Research</a>
  <span class="attribution">Generated with Claude | Supervised by repository owner</span>
</div>

<div class="page-layout">

{sidebar}

  <main class="content">
{body}

    <footer>
      <p>Player-facing guide derived from CS2 game mechanics research. For editorial use.</p>
    </footer>
  </main>

</div>

<script src="../sidebar.js"></script>
</body>
</html>
"""


def main():
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    drafts = sorted(DRAFTS_DIR.glob("*.md"))
    if not drafts:
        print(f"No .md files found in {DRAFTS_DIR}", file=sys.stderr)
        sys.exit(1)

    for draft in drafts:
        md_text = draft.read_text(encoding="utf-8")
        stem = draft.stem
        out_file = stem + ".html"
        out_path = OUTPUT_DIR / out_file

        page_title, body = md_to_html_body(md_text)
        html = build_page(page_title, body, out_file)
        out_path.write_text(html, encoding="utf-8")
        print(f"  {out_file}  ({len(html):,} bytes)")

    print(f"\nDone — {len(drafts)} pages written to {OUTPUT_DIR}/")


if __name__ == "__main__":
    main()
