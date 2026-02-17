#!/usr/bin/env python3
"""Batch-update the sidebar in every site/*.html page (except index.html).

Each page gets an identical sidebar with the current page marked class="active".
"""

import os
import re

SITE_DIR = os.path.join(os.path.dirname(__file__), '..', 'site')

# Canonical sidebar structure: list of (category_title, [(filename, label), ...])
SIDEBAR = [
    ("Events &amp; Simulation", [
        ("event-entity-archetype.html", "Event Entity Archetype"),
        ("fire-ignition.html", "Fire Ignition"),
        ("vehicle-out-of-control.html", "Vehicle Accidents"),
        ("crime-trigger.html", "Crime Trigger"),
        ("citizen-sickness.html", "Citizen Sickness"),
        ("water-system.html", "Water System"),
        ("emergency-dispatch.html", "Emergency Dispatch"),
        ("disaster-simulation.html", "Disaster Simulation"),
    ]),
    ("Infrastructure", [
        ("electricity-grid.html", "Electricity Grid"),
        ("water-sewage-pipes.html", "Water &amp; Sewage Pipes"),
        ("road-network.html", "Road &amp; Network Building"),
        ("zoning.html", "Zoning"),
        ("pathfinding.html", "Pathfinding"),
        ("traffic-flow.html", "Traffic Flow"),
    ]),
    ("Economy &amp; Population", [
        ("citizens-households.html", "Citizens &amp; Households"),
        ("economy-budget.html", "Economy &amp; Budget"),
        ("demand-systems.html", "Demand Systems"),
        ("resource-production.html", "Resource Production"),
        ("company-simulation.html", "Company Simulation"),
        ("workplace-labor.html", "Workplace &amp; Labor"),
        ("land-value-property.html", "Land Value &amp; Property"),
        ("trade-connections.html", "Trade &amp; Connections"),
        ("tourism-economy.html", "Tourism Economy"),
        ("cargo-transport.html", "Cargo Transport"),
    ]),
    ("City Services", [
        ("garbage-collection.html", "Garbage Collection"),
        ("education.html", "Education"),
        ("public-transit.html", "Public Transit"),
        ("healthcare.html", "Healthcare"),
        ("deathcare.html", "Deathcare"),
        ("parks-recreation.html", "Parks &amp; Recreation"),
        ("police-dispatch.html", "Police Dispatch"),
    ]),
    ("Governance", [
        ("districts-policies.html", "Districts &amp; Policies"),
        ("map-tile-purchase.html", "Map Tile Purchase"),
    ]),
    ("Environment", [
        ("weather-climate.html", "Weather &amp; Climate"),
        ("pollution.html", "Pollution"),
        ("terrain-resources.html", "Terrain &amp; Resources"),
    ]),
    ("Vehicles &amp; Transport", [
        ("vehicle-spawning.html", "Vehicle Spawning"),
    ]),
    ("Tools &amp; Input", [
        ("tool-activation.html", "Tool Activation"),
        ("tool-raycast.html", "Tool Raycast"),
        ("input-action-lifecycle.html", "Input Action Lifecycle"),
        ("mod-hotkey-input.html", "Mod Hotkey Input"),
        ("object-tool-system.html", "Object Placement Tool"),
    ]),
    ("UI &amp; Settings", [
        ("mod-ui-buttons.html", "Mod UI Buttons"),
        ("mod-options-ui.html", "Mod Options UI"),
        ("info-views-overlays.html", "Info Views &amp; Overlays"),
        ("chirper-notifications.html", "Chirper Notifications"),
    ]),
    ("Modding Framework", [
        ("save-load-persistence.html", "Save/Load Persistence"),
        ("prefab-system.html", "Prefab System"),
        ("localization.html", "Localization"),
        ("harmony-transpilers.html", "Harmony Transpilers"),
        ("mod-loading-dependencies.html", "Mod Loading"),
    ]),
]


def build_sidebar_html(active_filename: str) -> str:
    """Return the full <aside>...</aside> block with the active page marked."""
    lines = ['  <aside class="sidebar">']
    for cat_title, pages in SIDEBAR:
        lines.append(f'    <h3>{cat_title}</h3>')
        for fname, label in pages:
            if fname == active_filename:
                lines.append(f'    <a href="{fname}" class="active">{label}</a>')
            else:
                lines.append(f'    <a href="{fname}">{label}</a>')
        lines.append('')  # blank line between categories
    # remove trailing blank line
    if lines[-1] == '':
        lines.pop()
    lines.append('  </aside>')
    return '\n'.join(lines)


def update_file(filepath: str, filename: str) -> bool:
    """Replace the sidebar in a single HTML file. Returns True if changed."""
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # Match from <aside class="sidebar"> to </aside>
    pattern = r'  <aside class="sidebar">.*?  </aside>'
    new_sidebar = build_sidebar_html(filename)

    new_content, count = re.subn(pattern, new_sidebar, content, count=1, flags=re.DOTALL)
    if count == 0:
        print(f'  WARNING: no sidebar found in {filename}')
        return False

    if new_content == content:
        return False

    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(new_content)
    return True


def main():
    changed = 0
    skipped = 0
    for filename in sorted(os.listdir(SITE_DIR)):
        if not filename.endswith('.html') or filename == 'index.html':
            continue
        filepath = os.path.join(SITE_DIR, filename)
        if update_file(filepath, filename):
            print(f'  Updated: {filename}')
            changed += 1
        else:
            skipped += 1
            print(f'  Unchanged: {filename}')
    print(f'\nDone. {changed} files updated, {skipped} unchanged.')


if __name__ == '__main__':
    main()
