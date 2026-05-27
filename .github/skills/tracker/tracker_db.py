#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
import sqlite3
import sys
from pathlib import Path
from typing import Any


SCRIPT_DIR = Path(__file__).resolve().parent
DEFAULT_DB_PATH = SCRIPT_DIR / "tracker.db"
SCHEMA_PATH = SCRIPT_DIR / "schema.sql"

STATUSES = (
    "backlog",
    "planned",
    "in-progress",
    "blocked",
    "implemented",
    "validated",
    "archived",
)
PRIORITIES = ("low", "medium", "high", "critical")
WORK_ITEM_TYPES = {
    "task": "TASK",
    "bug": "BUG",
    "change-request": "CR",
    "research": "RFC",
}
RELATION_TYPES = ("related", "blocks", "depends-on", "duplicates")


def resolve_db_path(raw_path: str) -> Path:
    path = Path(raw_path).expanduser()
    if not path.is_absolute():
        path = (Path.cwd() / path).resolve()
    return path


def emit(payload: dict[str, Any]) -> None:
    print(json.dumps(payload, indent=2, ensure_ascii=True))


def row_to_dict(row: sqlite3.Row | None) -> dict[str, Any] | None:
    if row is None:
        return None
    return {key: row[key] for key in row.keys()}


def normalize_slug(value: str) -> str:
    slug = re.sub(r"[^a-z0-9]+", "-", value.strip().lower()).strip("-")
    if not slug:
        raise ValueError("Slug cannot be empty.")
    return slug


def normalize_single_line_text(value: Any) -> str | None:
    if value is None:
        return None
    text = re.sub(r"\s+", " ", str(value)).strip()
    return text or None


def normalize_multiline_text(value: Any) -> str | None:
    if value is None:
        return None
    text = str(value).replace("\r\n", "\n").replace("\r", "\n").strip()
    if not text:
        return None

    normalized_lines: list[str] = []
    blank_run = 0
    for raw_line in text.split("\n"):
        line = re.sub(r"[ \t]+", " ", raw_line).rstrip()
        if not line:
            blank_run += 1
            if blank_run > 1:
                continue
            normalized_lines.append("")
            continue
        blank_run = 0
        normalized_lines.append(line.strip())

    while normalized_lines and not normalized_lines[0]:
        normalized_lines.pop(0)
    while normalized_lines and not normalized_lines[-1]:
        normalized_lines.pop()

    normalized = "\n".join(normalized_lines).strip()
    return normalized or None


def normalize_feature_payload(payload: dict[str, Any]) -> dict[str, Any]:
    normalized = dict(payload)
    normalized["title"] = normalize_single_line_text(normalized.get("title"))
    normalized["summary"] = normalize_multiline_text(normalized.get("summary"))
    normalized["source_request"] = normalize_single_line_text(normalized.get("source_request"))
    normalized["architecture"] = normalize_multiline_text(normalized.get("architecture"))
    normalized["dependencies"] = normalize_multiline_text(normalized.get("dependencies"))
    normalized["risks"] = normalize_multiline_text(normalized.get("risks"))
    normalized["acceptance_criteria"] = normalize_multiline_text(normalized.get("acceptance_criteria"))
    normalized["implementation_phases"] = normalize_multiline_text(normalized.get("implementation_phases"))
    normalized["impacted_files_modules"] = normalize_multiline_text(
        normalized.get("impacted_files_modules")
    )
    if not normalized["title"]:
        raise ValueError("Feature title cannot be empty.")
    return normalized


def normalize_work_item_payload(payload: dict[str, Any]) -> dict[str, Any]:
    normalized = dict(payload)
    normalized["title"] = normalize_single_line_text(normalized.get("title"))
    normalized["summary"] = normalize_multiline_text(normalized.get("summary"))
    normalized["source_request"] = normalize_single_line_text(normalized.get("source_request"))
    normalized["implementation_notes"] = normalize_multiline_text(
        normalized.get("implementation_notes")
    )
    if not normalized["title"]:
        raise ValueError("Work item title cannot be empty.")
    return normalized


def normalize_feature_row_updates(row: sqlite3.Row) -> dict[str, Any]:
    updates: dict[str, Any] = {}
    candidate = normalize_feature_payload(row_to_dict(row) or {})
    for field in (
        "title",
        "summary",
        "source_request",
        "architecture",
        "dependencies",
        "risks",
        "acceptance_criteria",
        "implementation_phases",
        "impacted_files_modules",
    ):
        if candidate.get(field) != row[field]:
            updates[field] = candidate.get(field)
    return updates


def normalize_work_item_row_updates(row: sqlite3.Row) -> dict[str, Any]:
    updates: dict[str, Any] = {}
    candidate = normalize_work_item_payload(row_to_dict(row) or {})
    for field in ("title", "summary", "source_request", "implementation_notes"):
        if candidate.get(field) != row[field]:
            updates[field] = candidate.get(field)
    return updates


def open_connection(db_path: Path) -> sqlite3.Connection:
    db_path.parent.mkdir(parents=True, exist_ok=True)
    conn = sqlite3.connect(db_path)
    conn.row_factory = sqlite3.Row
    conn.execute("PRAGMA foreign_keys = ON;")
    ensure_schema(conn)
    return conn


def ensure_schema(conn: sqlite3.Connection) -> None:
    existing = conn.execute(
        "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'features';"
    ).fetchone()
    if existing is not None:
        return
    if not SCHEMA_PATH.exists():
        raise FileNotFoundError(f"Schema file not found: {SCHEMA_PATH}")
    conn.executescript(SCHEMA_PATH.read_text(encoding="utf-8"))


def begin_immediate(conn: sqlite3.Connection) -> None:
    conn.execute("BEGIN IMMEDIATE;")


def next_identifier(conn: sqlite3.Connection, prefix: str) -> str:
    conn.execute(
        "INSERT OR IGNORE INTO id_sequences (prefix, next_value) VALUES (?, 1);",
        (prefix,),
    )
    row = conn.execute(
        "SELECT next_value FROM id_sequences WHERE prefix = ?;",
        (prefix,),
    ).fetchone()
    if row is None:
        raise RuntimeError(f"Sequence not found for prefix '{prefix}'.")
    next_value = row["next_value"]
    conn.execute(
        "UPDATE id_sequences SET next_value = ? WHERE prefix = ?;",
        (next_value + 1, prefix),
    )
    return f"{prefix}-{next_value:03d}"


def record_history(
    conn: sqlite3.Connection,
    entity_type: str,
    entity_id: str,
    action: str,
    snapshot: dict[str, Any] | None,
) -> None:
    conn.execute(
        """
        INSERT INTO work_item_history (entity_type, entity_id, action, snapshot)
        VALUES (?, ?, ?, ?);
        """,
        (
            entity_type,
            entity_id,
            action,
            json.dumps(snapshot, sort_keys=True) if snapshot is not None else None,
        ),
    )


def fetch_feature(conn: sqlite3.Connection, feature_id: str) -> sqlite3.Row | None:
    return conn.execute("SELECT * FROM features WHERE id = ?;", (feature_id,)).fetchone()


def fetch_work_item(conn: sqlite3.Connection, work_item_id: str) -> sqlite3.Row | None:
    return conn.execute(
        "SELECT * FROM work_items WHERE id = ?;",
        (work_item_id,),
    ).fetchone()


def fetch_link(conn: sqlite3.Connection, link_id: int) -> sqlite3.Row | None:
    return conn.execute(
        "SELECT * FROM work_item_links WHERE id = ?;",
        (link_id,),
    ).fetchone()


def require_feature(conn: sqlite3.Connection, feature_id: str) -> sqlite3.Row:
    row = fetch_feature(conn, feature_id)
    if row is None:
        raise ValueError(f"Feature '{feature_id}' was not found.")
    return row


def require_work_item(conn: sqlite3.Connection, work_item_id: str) -> sqlite3.Row:
    row = fetch_work_item(conn, work_item_id)
    if row is None:
        raise ValueError(f"Work item '{work_item_id}' was not found.")
    return row


def entity_kind_for_id(entity_id: str) -> str:
    prefix = entity_id.split("-", 1)[0]
    if prefix == "FEAT":
        return "feature"
    if prefix in {"TASK", "BUG", "CR", "RFC"}:
        return "work_item"
    raise ValueError(f"Unsupported entity id '{entity_id}'.")


def split_frontmatter_and_body(text: str) -> tuple[dict[str, Any], str]:
    lines = text.splitlines()
    if not lines or lines[0].strip() != "---":
        return {}, text
    end_index = None
    for index in range(1, len(lines)):
        if lines[index].strip() == "---":
            end_index = index
            break
    if end_index is None:
        return {}, text
    frontmatter_lines = lines[1:end_index]
    body = "\n".join(lines[end_index + 1 :]).lstrip("\n")
    data: dict[str, Any] = {}
    current_list_key: str | None = None
    for raw_line in frontmatter_lines:
        line = raw_line.rstrip()
        stripped = line.strip()
        if not stripped:
            continue
        match = re.match(r"^([A-Za-z0-9_-]+)\s*:\s*(.*)$", stripped)
        if match:
            key = match.group(1)
            value = match.group(2).strip()
            current_list_key = None
            if value == "":
                data[key] = []
                current_list_key = key
            elif value == "[]":
                data[key] = []
            else:
                data[key] = value.strip("\"'")
            continue
        if current_list_key and stripped.startswith("- "):
            data.setdefault(current_list_key, [])
            cast_list = data[current_list_key]
            if isinstance(cast_list, list):
                cast_list.append(stripped[2:].strip().strip("\"'"))
            continue
    return data, body


def parse_markdown_file(path: Path) -> tuple[dict[str, Any], str, str]:
    text = path.read_text(encoding="utf-8")
    frontmatter, body = split_frontmatter_and_body(text)
    title = ""
    for line in text.splitlines():
        stripped = line.strip()
        if stripped.startswith("# "):
            title = stripped[2:].strip()
            break
    return frontmatter, body, title


def normalize_heading(value: str) -> str:
    return re.sub(r"[^a-z0-9]+", " ", value.lower()).strip()


def split_sections(body: str) -> tuple[str, list[tuple[str, str]]]:
    intro_lines: list[str] = []
    sections: list[tuple[str, str]] = []
    current_title: str | None = None
    current_lines: list[str] = []
    for line in body.splitlines():
        match = re.match(r"^##+\s+(.+?)\s*$", line)
        if match:
            if current_title is None and current_lines:
                intro_lines.extend(current_lines)
            elif current_title is not None:
                sections.append((current_title, "\n".join(current_lines).strip()))
            current_title = match.group(1).strip()
            current_lines = []
            continue
        if current_title is None:
            intro_lines.append(line)
        else:
            current_lines.append(line)
    if current_title is not None:
        sections.append((current_title, "\n".join(current_lines).strip()))
    return "\n".join(intro_lines).strip(), sections


def find_section_text(
    sections: list[tuple[str, str]],
    candidates: tuple[str, ...],
) -> str | None:
    normalized_candidates = [normalize_heading(candidate) for candidate in candidates]
    for heading, content in sections:
        normalized_heading = normalize_heading(heading)
        if any(
            normalized_heading == candidate or candidate in normalized_heading
            for candidate in normalized_candidates
        ):
            return content.strip() or None
    return None


def strip_id_prefix(title: str) -> str:
    return re.sub(r"^(FEAT|TASK|BUG|CR|RFC)-\d+\s*[—-]\s*", "", title).strip()


def folder_slug(folder: Path) -> str:
    match = re.match(r"^FEAT-\d+-(.+)$", folder.name)
    if match:
        return match.group(1)
    return normalize_slug(folder.name)


def title_from_slug(slug: str) -> str:
    return slug.replace("-", " ").replace("_", " ").title()


def extract_bullet_lines(body: str) -> list[str]:
    bullets: list[str] = []
    for line in body.splitlines():
        stripped = line.strip()
        if stripped.startswith("- "):
            content = stripped[2:].strip()
            if content.startswith("[") and "](" in content:
                continue
            content = content.replace("`", "").strip()
            if content:
                bullets.append(content)
    return bullets


def build_feature_import_payload(
    feature_dir: Path,
    meta_path: Path | None,
    links_path: Path | None,
) -> dict[str, Any]:
    if meta_path and meta_path.exists():
        frontmatter, body, raw_title = parse_markdown_file(meta_path)
        old_id = str(frontmatter.get("id") or feature_dir.name.split("-", 2)[0] + "-" + feature_dir.name.split("-", 2)[1])
        title = strip_id_prefix(raw_title) or title_from_slug(folder_slug(feature_dir))
        intro, sections = split_sections(body)
        summary = find_section_text(sections, ("Descrizione", "Obiettivo", "Summary", "Description")) or intro
        architecture = find_section_text(
            sections,
            (
                "Decisioni architetturali",
                "Architettura",
                "Decisioni",
                "Architecture",
            ),
        )
        dependencies = find_section_text(sections, ("Dipendenze esterne", "Dipendenze", "Dependencies"))
        risks = find_section_text(sections, ("Rischi", "Risks"))
        acceptance_criteria = find_section_text(
            sections,
            ("Acceptance Criteria", "Criteri di accettazione"),
        )
        implementation_phases = find_section_text(
            sections,
            ("Fasi implementative", "Implementation Phases"),
        )
        impacted = find_section_text(
            sections,
            (
                "Moduli / file impattati",
                "Impacted Files or Modules",
                "File interessati",
                "Impatto",
            ),
        )
        if links_path and links_path.exists():
            links_text = links_path.read_text(encoding="utf-8")
            bullets = extract_bullet_lines(links_text)
            file_bullets = [
                bullet for bullet in bullets if "/" in bullet or "." in bullet or "::" in bullet
            ]
            if file_bullets:
                impacted = "\n".join(filter(None, [impacted, "\n".join(file_bullets)]))
        related = frontmatter.get("related", [])
        if not isinstance(related, list):
            related = [related]
        related = [str(item) for item in related]
        related_feature_ids = [item for item in related if item.startswith("FEAT-")]
        if dependencies is None and related_feature_ids:
            dependencies = ", ".join(related_feature_ids)
        source_request = f"Imported from {meta_path.relative_to(meta_path.parents[2])}"
        if related:
            source_request += f"; related: {', '.join(related)}"
        return {
            "old_id": old_id,
            "title": title,
            "slug": folder_slug(feature_dir),
            "status": str(frontmatter.get("status") or "backlog"),
            "priority": str(frontmatter.get("priority") or "medium"),
            "summary": summary or f"Imported from {meta_path.relative_to(meta_path.parents[2])}",
            "source_request": source_request,
            "architecture": architecture,
            "dependencies": dependencies,
            "risks": risks,
            "acceptance_criteria": acceptance_criteria,
            "implementation_phases": implementation_phases,
            "impacted_files_modules": impacted,
            "related": related,
        }

    slug = folder_slug(feature_dir)
    old_id = feature_dir.name.split("-", 2)[0] + "-" + feature_dir.name.split("-", 2)[1]
    return {
        "old_id": old_id,
        "title": title_from_slug(slug),
        "slug": slug,
        "status": "backlog",
        "priority": "medium",
        "summary": f"Imported from scaffold folder {feature_dir.name}.",
        "source_request": f"Imported from scaffold folder {feature_dir.name}.",
        "architecture": None,
        "dependencies": None,
        "risks": None,
        "acceptance_criteria": None,
        "implementation_phases": None,
        "impacted_files_modules": None,
        "related": [],
    }


def build_work_item_import_payload(
    item_path: Path,
    feature_id: str,
    item_type: str,
) -> dict[str, Any]:
    frontmatter, body, raw_title = parse_markdown_file(item_path)
    old_id = str(frontmatter.get("id") or item_path.stem)
    title = strip_id_prefix(raw_title) or title_from_slug(item_path.stem)
    intro, sections = split_sections(body)
    summary = find_section_text(sections, ("Descrizione", "Obiettivo", "Summary", "Goal")) or intro
    notes_sections = [content for heading, content in sections if content and content != summary]
    implementation_notes = "\n\n".join(notes_sections).strip() or None
    related = frontmatter.get("related", [])
    if not isinstance(related, list):
        related = [related]
    related = [str(item) for item in related]
    related_feature_ids = [item for item in related if item.startswith("FEAT-")]
    source_request = f"Imported from {item_path.relative_to(item_path.parents[3])}"
    if related_feature_ids:
        source_request += f"; related features: {', '.join(related_feature_ids)}"
    return {
        "old_id": old_id,
        "feature_old_id": str(frontmatter.get("feature") or feature_id),
        "title": title,
        "type": str(frontmatter.get("type") or item_type),
        "status": str(frontmatter.get("status") or "backlog"),
        "priority": str(frontmatter.get("priority") or "medium"),
        "summary": summary or None,
        "source_request": source_request,
        "implementation_notes": implementation_notes,
        "related": related,
    }


def insert_feature_row(conn: sqlite3.Connection, payload: dict[str, Any]) -> str:
    payload = normalize_feature_payload(payload)
    feature_id = next_identifier(conn, "FEAT")
    conn.execute(
        """
        INSERT INTO features (
            id, slug, title, summary, status, priority, source_request,
            architecture, dependencies, risks, acceptance_criteria,
            implementation_phases, impacted_files_modules
        )
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
        """,
        (
            feature_id,
            payload["slug"],
            payload["title"],
            payload["summary"],
            payload["status"],
            payload["priority"],
            payload["source_request"],
            payload["architecture"],
            payload["dependencies"],
            payload["risks"],
            payload["acceptance_criteria"],
            payload["implementation_phases"],
            payload["impacted_files_modules"],
        ),
    )
    created = fetch_feature(conn, feature_id)
    record_history(conn, "feature", feature_id, "created", row_to_dict(created))
    return feature_id


def insert_work_item_row(
    conn: sqlite3.Connection,
    payload: dict[str, Any],
    feature_id: str,
) -> str:
    payload = normalize_work_item_payload(payload)
    prefix = WORK_ITEM_TYPES[payload["type"]]
    work_item_id = next_identifier(conn, prefix)
    conn.execute(
        """
        INSERT INTO work_items (
            id, feature_id, parent_work_item_id, type, title, summary,
            status, priority, source_request, implementation_notes
        )
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
        """,
        (
            work_item_id,
            feature_id,
            None,
            payload["type"],
            payload["title"],
            payload["summary"],
            payload["status"],
            payload["priority"],
            payload["source_request"],
            payload["implementation_notes"],
        ),
    )
    created = fetch_work_item(conn, work_item_id)
    record_history(conn, "work_item", work_item_id, "created", row_to_dict(created))
    return work_item_id


def import_docs_command(args: argparse.Namespace) -> int:
    db_path = resolve_db_path(args.db_path)
    docs_root = resolve_db_path(args.docs_root)
    conn = open_connection(db_path)
    try:
        begin_immediate(conn)
        if conn.execute("SELECT 1 FROM features LIMIT 1;").fetchone() is not None:
            raise ValueError("Database already contains data; import expects an empty tracker database.")

        feature_dirs: list[Path] = []
        for root_name in ("features", "backlog"):
            root = docs_root / root_name
            if not root.exists():
                continue
            for child in sorted(root.iterdir()):
                if child.is_dir() and child.name.startswith("FEAT-"):
                    feature_dirs.append(child)

        feature_payloads: dict[str, dict[str, Any]] = {}
        feature_id_map: dict[str, str] = {}
        work_item_payloads: dict[tuple[str, str], dict[str, Any]] = {}

        for feature_dir in sorted(feature_dirs):
            meta_path = feature_dir / "meta.md"
            links_path = feature_dir / "links.md"
            payload = build_feature_import_payload(feature_dir, meta_path if meta_path.exists() else None, links_path if links_path.exists() else None)
            feature_payloads[payload["old_id"]] = payload
        for old_id in sorted(feature_payloads):
            feature_id_map[old_id] = insert_feature_row(conn, feature_payloads[old_id])

        for feature_dir in sorted(feature_dirs):
            feature_old_id = feature_dir.name.split("-", 2)[0] + "-" + feature_dir.name.split("-", 2)[1]
            feature_new_id = feature_id_map[feature_old_id]
            for subdir_name, item_type in (
                ("tasks", "task"),
                ("bugs", "bug"),
                ("cr", "change-request"),
                ("research", "research"),
            ):
                subdir = feature_dir / subdir_name
                if not subdir.exists() or not subdir.is_dir():
                    continue
                for item_path in sorted(subdir.glob("*.md")):
                    payload = build_work_item_import_payload(item_path, feature_new_id, item_type)
                    work_item_payloads[(payload["feature_old_id"], payload["old_id"])] = payload
                    payload["new_id"] = insert_work_item_row(conn, payload, feature_new_id)

        work_item_id_map = {
            f"{feature_old_id}/{old_id}": payload["new_id"]
            for (feature_old_id, old_id), payload in work_item_payloads.items()
            if "new_id" in payload
        }

        created_links = 0
        seen_pairs: set[tuple[str, str, str]] = set()
        for (feature_old_id, old_id), payload in sorted(work_item_payloads.items()):
            source_new_id = work_item_id_map[f"{feature_old_id}/{old_id}"]
            for related_old_id in payload["related"]:
                target_new_id = work_item_id_map.get(f"{feature_old_id}/{related_old_id}")
                if target_new_id is None or target_new_id == source_new_id:
                    continue
                pair = tuple(sorted((source_new_id, target_new_id))) + ("related",)
                if pair in seen_pairs:
                    continue
                seen_pairs.add(pair)
                cursor = conn.execute(
                    """
                    INSERT INTO work_item_links (
                        source_work_item_id, target_work_item_id, relation_type, notes
                    )
                    VALUES (?, ?, ?, ?);
                    """,
                    (source_new_id, target_new_id, "related", "Imported from markdown related list."),
                )
                link_id = int(cursor.lastrowid)
                created = fetch_link(conn, link_id)
                record_history(
                    conn,
                    "work_item_link",
                    str(link_id),
                    "linked",
                    row_to_dict(created),
                )
                created_links += 1

        conn.commit()
    except Exception:
        conn.rollback()
        raise
    finally:
        conn.close()

    emit(
        {
            "status": "imported",
            "db_path": str(db_path),
            "docs_root": str(docs_root),
            "features_imported": len(feature_id_map),
            "work_items_imported": len(work_item_id_map),
            "links_imported": created_links,
            "feature_id_map": feature_id_map,
            "work_item_id_map": work_item_id_map,
        }
    )
    return 0


def init_command(args: argparse.Namespace) -> int:
    db_path = resolve_db_path(args.db_path)
    conn = open_connection(db_path)
    conn.close()
    emit({"status": "initialized", "db_path": str(db_path)})
    return 0


def create_feature_command(args: argparse.Namespace) -> int:
    db_path = resolve_db_path(args.db_path)
    conn = open_connection(db_path)
    slug = normalize_slug(args.slug or args.title)
    try:
        begin_immediate(conn)
        feature_id = insert_feature_row(
            conn,
            {
                "slug": slug,
                "title": args.title,
                "summary": args.summary,
                "status": args.status,
                "priority": args.priority,
                "source_request": args.source_request,
                "architecture": args.architecture,
                "dependencies": args.dependencies,
                "risks": args.risks,
                "acceptance_criteria": args.acceptance_criteria,
                "implementation_phases": args.implementation_phases,
                "impacted_files_modules": args.impacted_files_modules,
            },
        )
        created = fetch_feature(conn, feature_id)
        record_history(conn, "feature", feature_id, "created", row_to_dict(created))
        conn.commit()
    except Exception:
        conn.rollback()
        raise
    finally:
        conn.close()
    emit({"created": row_to_dict(created), "db_path": str(db_path)})
    return 0


def create_work_item_command(args: argparse.Namespace) -> int:
    db_path = resolve_db_path(args.db_path)
    conn = open_connection(db_path)
    try:
        begin_immediate(conn)
        require_feature(conn, args.feature_id)
        if args.parent_id:
            parent = require_work_item(conn, args.parent_id)
            if parent["feature_id"] != args.feature_id:
                raise ValueError("Parent work item must belong to the same feature.")
        work_item_id = insert_work_item_row(
            conn,
            {
                "type": args.type,
                "title": args.title,
                "summary": args.summary,
                "status": args.status,
                "priority": args.priority,
                "source_request": args.source_request,
                "implementation_notes": args.implementation_notes,
            },
            args.feature_id,
        )
        if args.parent_id:
            conn.execute(
                "UPDATE work_items SET parent_work_item_id = ? WHERE id = ?;",
                (args.parent_id, work_item_id),
            )
        created = fetch_work_item(conn, work_item_id)
        record_history(conn, "work_item", work_item_id, "created", row_to_dict(created))
        conn.commit()
    except Exception:
        conn.rollback()
        raise
    finally:
        conn.close()
    emit({"created": row_to_dict(created), "db_path": str(db_path)})
    return 0


def update_item_command(args: argparse.Namespace) -> int:
    db_path = resolve_db_path(args.db_path)
    conn = open_connection(db_path)
    entity_kind = entity_kind_for_id(args.id)
    try:
        begin_immediate(conn)
        if entity_kind == "feature":
            require_feature(conn, args.id)
            updates: dict[str, Any] = {}
            title = normalize_single_line_text(args.title) if args.title is not None else None
            if args.title is not None and not title:
                raise ValueError("Feature title cannot be empty.")
            feature_fields = {
                "title": title,
                "slug": normalize_slug(args.slug) if args.slug else None,
                "summary": normalize_multiline_text(args.summary),
                "status": args.status,
                "priority": args.priority,
                "source_request": normalize_single_line_text(args.source_request),
                "architecture": normalize_multiline_text(args.architecture),
                "dependencies": normalize_multiline_text(args.dependencies),
                "risks": normalize_multiline_text(args.risks),
                "acceptance_criteria": normalize_multiline_text(args.acceptance_criteria),
                "implementation_phases": normalize_multiline_text(args.implementation_phases),
                "impacted_files_modules": normalize_multiline_text(args.impacted_files_modules),
            }
            for key, value in feature_fields.items():
                if value is not None:
                    updates[key] = value
            if not updates:
                raise ValueError("No feature fields were provided for update.")
            assignments = ", ".join(f"{column} = ?" for column in updates)
            values = list(updates.values()) + [args.id]
            conn.execute(
                f"UPDATE features SET {assignments}, updated_at = CURRENT_TIMESTAMP WHERE id = ?;",
                values,
            )
            updated = fetch_feature(conn, args.id)
            record_history(conn, "feature", args.id, "updated", row_to_dict(updated))
        else:
            current = require_work_item(conn, args.id)
            parent_id = args.parent_id
            if args.clear_parent:
                parent_id = None
            if parent_id:
                parent = require_work_item(conn, parent_id)
                if parent["feature_id"] != current["feature_id"]:
                    raise ValueError("Parent work item must belong to the same feature.")
            updates: dict[str, Any] = {}
            title = normalize_single_line_text(args.title) if args.title is not None else None
            if args.title is not None and not title:
                raise ValueError("Work item title cannot be empty.")
            work_item_fields = {
                "title": title,
                "summary": normalize_multiline_text(args.summary),
                "status": args.status,
                "priority": args.priority,
                "source_request": normalize_single_line_text(args.source_request),
                "implementation_notes": normalize_multiline_text(args.implementation_notes),
            }
            for key, value in work_item_fields.items():
                if value is not None:
                    updates[key] = value
            if args.parent_id is not None or args.clear_parent:
                updates["parent_work_item_id"] = parent_id
            if not updates:
                raise ValueError("No work-item fields were provided for update.")
            assignments = ", ".join(f"{column} = ?" for column in updates)
            values = list(updates.values()) + [args.id]
            conn.execute(
                f"UPDATE work_items SET {assignments}, updated_at = CURRENT_TIMESTAMP WHERE id = ?;",
                values,
            )
            updated = fetch_work_item(conn, args.id)
            record_history(conn, "work_item", args.id, "updated", row_to_dict(updated))
        conn.commit()
    except Exception:
        conn.rollback()
        raise
    finally:
        conn.close()
    emit({"updated": row_to_dict(updated), "db_path": str(db_path)})
    return 0


def normalize_text_command(args: argparse.Namespace) -> int:
    db_path = resolve_db_path(args.db_path)
    conn = open_connection(db_path)
    feature_updates = 0
    work_item_updates = 0
    try:
        begin_immediate(conn)

        feature_rows = conn.execute("SELECT * FROM features ORDER BY created_at, id;").fetchall()
        for row in feature_rows:
            updates = normalize_feature_row_updates(row)
            if not updates:
                continue
            assignments = ", ".join(f"{column} = ?" for column in updates)
            values = list(updates.values()) + [row["id"]]
            conn.execute(
                f"UPDATE features SET {assignments}, updated_at = CURRENT_TIMESTAMP WHERE id = ?;",
                values,
            )
            updated = fetch_feature(conn, row["id"])
            record_history(conn, "feature", row["id"], "updated", row_to_dict(updated))
            feature_updates += 1

        work_item_rows = conn.execute("SELECT * FROM work_items ORDER BY created_at, id;").fetchall()
        for row in work_item_rows:
            updates = normalize_work_item_row_updates(row)
            if not updates:
                continue
            assignments = ", ".join(f"{column} = ?" for column in updates)
            values = list(updates.values()) + [row["id"]]
            conn.execute(
                f"UPDATE work_items SET {assignments}, updated_at = CURRENT_TIMESTAMP WHERE id = ?;",
                values,
            )
            updated = fetch_work_item(conn, row["id"])
            record_history(conn, "work_item", row["id"], "updated", row_to_dict(updated))
            work_item_updates += 1

        conn.commit()
    except Exception:
        conn.rollback()
        raise
    finally:
        conn.close()

    emit(
        {
            "status": "normalized",
            "db_path": str(db_path),
            "features_updated": feature_updates,
            "work_items_updated": work_item_updates,
        }
    )
    return 0


def delete_item_command(args: argparse.Namespace) -> int:
    db_path = resolve_db_path(args.db_path)
    conn = open_connection(db_path)
    entity_kind = entity_kind_for_id(args.id)
    try:
        begin_immediate(conn)
        if entity_kind == "feature":
            row = require_feature(conn, args.id)
            snapshot = row_to_dict(row)
            record_history(conn, "feature", args.id, "deleted", snapshot)
            conn.execute("DELETE FROM features WHERE id = ?;", (args.id,))
        else:
            row = require_work_item(conn, args.id)
            snapshot = row_to_dict(row)
            record_history(conn, "work_item", args.id, "deleted", snapshot)
            conn.execute("DELETE FROM work_items WHERE id = ?;", (args.id,))
        conn.commit()
    except Exception:
        conn.rollback()
        raise
    finally:
        conn.close()
    emit(
        {
            "deleted": {
                "entity_type": entity_kind,
                "entity_id": args.id,
                "snapshot": snapshot,
            },
            "db_path": str(db_path),
        }
    )
    return 0


def link_items_command(args: argparse.Namespace) -> int:
    db_path = resolve_db_path(args.db_path)
    conn = open_connection(db_path)
    try:
        begin_immediate(conn)
        require_work_item(conn, args.source_id)
        require_work_item(conn, args.target_id)
        cursor = conn.execute(
            """
            INSERT INTO work_item_links (
                source_work_item_id, target_work_item_id, relation_type, notes
            )
            VALUES (?, ?, ?, ?);
            """,
            (args.source_id, args.target_id, args.relation_type, args.notes),
        )
        link_id = int(cursor.lastrowid)
        created = fetch_link(conn, link_id)
        record_history(
            conn,
            "work_item_link",
            str(link_id),
            "linked",
            row_to_dict(created),
        )
        conn.commit()
    except Exception:
        conn.rollback()
        raise
    finally:
        conn.close()
    emit({"created": row_to_dict(created), "db_path": str(db_path)})
    return 0


def delete_link_command(args: argparse.Namespace) -> int:
    db_path = resolve_db_path(args.db_path)
    conn = open_connection(db_path)
    try:
        begin_immediate(conn)
        row = fetch_link(conn, args.link_id)
        if row is None:
            raise ValueError(f"Link '{args.link_id}' was not found.")
        snapshot = row_to_dict(row)
        record_history(
            conn,
            "work_item_link",
            str(args.link_id),
            "unlinked",
            snapshot,
        )
        conn.execute("DELETE FROM work_item_links WHERE id = ?;", (args.link_id,))
        conn.commit()
    except Exception:
        conn.rollback()
        raise
    finally:
        conn.close()
    emit({"deleted": snapshot, "db_path": str(db_path)})
    return 0


def get_item_command(args: argparse.Namespace) -> int:
    db_path = resolve_db_path(args.db_path)
    conn = open_connection(db_path)
    try:
        entity_kind = entity_kind_for_id(args.id)
        if entity_kind == "feature":
            feature = require_feature(conn, args.id)
            work_items = conn.execute(
                "SELECT * FROM work_items WHERE feature_id = ? ORDER BY created_at, id;",
                (args.id,),
            ).fetchall()
            payload = {
                "entity_type": "feature",
                "item": row_to_dict(feature),
                "work_items": [row_to_dict(row) for row in work_items],
            }
        else:
            work_item = require_work_item(conn, args.id)
            outgoing = conn.execute(
                "SELECT * FROM work_item_links WHERE source_work_item_id = ? ORDER BY id;",
                (args.id,),
            ).fetchall()
            incoming = conn.execute(
                "SELECT * FROM work_item_links WHERE target_work_item_id = ? ORDER BY id;",
                (args.id,),
            ).fetchall()
            payload = {
                "entity_type": "work_item",
                "item": row_to_dict(work_item),
                "outgoing_links": [row_to_dict(row) for row in outgoing],
                "incoming_links": [row_to_dict(row) for row in incoming],
            }
    finally:
        conn.close()
    payload["db_path"] = str(db_path)
    emit(payload)
    return 0


def list_features_command(args: argparse.Namespace) -> int:
    db_path = resolve_db_path(args.db_path)
    conn = open_connection(db_path)
    query = "SELECT * FROM features"
    clauses: list[str] = []
    parameters: list[Any] = []
    if args.status:
        clauses.append("status = ?")
        parameters.append(args.status)
    if args.priority:
        clauses.append("priority = ?")
        parameters.append(args.priority)
    if clauses:
        query += " WHERE " + " AND ".join(clauses)
    query += " ORDER BY created_at, id;"
    try:
        rows = conn.execute(query, parameters).fetchall()
    finally:
        conn.close()
    emit({"features": [row_to_dict(row) for row in rows], "db_path": str(db_path)})
    return 0


def list_work_items_command(args: argparse.Namespace) -> int:
    db_path = resolve_db_path(args.db_path)
    conn = open_connection(db_path)
    query = "SELECT * FROM work_items"
    clauses: list[str] = []
    parameters: list[Any] = []
    if args.feature_id:
        clauses.append("feature_id = ?")
        parameters.append(args.feature_id)
    if args.status:
        clauses.append("status = ?")
        parameters.append(args.status)
    if args.type:
        clauses.append("type = ?")
        parameters.append(args.type)
    if clauses:
        query += " WHERE " + " AND ".join(clauses)
    query += " ORDER BY created_at, id;"
    try:
        rows = conn.execute(query, parameters).fetchall()
    finally:
        conn.close()
    emit({"work_items": [row_to_dict(row) for row in rows], "db_path": str(db_path)})
    return 0


def history_command(args: argparse.Namespace) -> int:
    db_path = resolve_db_path(args.db_path)
    conn = open_connection(db_path)
    try:
        rows = conn.execute(
            """
            SELECT * FROM work_item_history
            WHERE entity_id = ?
            ORDER BY created_at DESC, id DESC
            LIMIT ?;
            """,
            (args.entity_id, args.limit),
        ).fetchall()
    finally:
        conn.close()
    emit({"history": [row_to_dict(row) for row in rows], "db_path": str(db_path)})
    return 0


def export_feature_command(args: argparse.Namespace) -> int:
    db_path = resolve_db_path(args.db_path)
    conn = open_connection(db_path)
    try:
        feature = require_feature(conn, args.feature_id)
        work_items = conn.execute(
            "SELECT * FROM work_items WHERE feature_id = ? ORDER BY created_at, id;",
            (args.feature_id,),
        ).fetchall()
        work_item_ids = [row["id"] for row in work_items]
        links: list[sqlite3.Row] = []
        if work_item_ids:
            placeholders = ", ".join("?" for _ in work_item_ids)
            links = conn.execute(
                f"""
                SELECT * FROM work_item_links
                WHERE source_work_item_id IN ({placeholders})
                   OR target_work_item_id IN ({placeholders})
                ORDER BY id;
                """,
                [*work_item_ids, *work_item_ids],
            ).fetchall()
    finally:
        conn.close()

    lines = [
        f"# {feature['id']} - {feature['title']}",
        "",
        f"- status: {feature['status']}",
        f"- priority: {feature['priority']}",
        f"- slug: {feature['slug']}",
        "",
        "## Summary",
        feature["summary"] or "_No summary_",
        "",
        "## Architecture",
        feature["architecture"] or "_Not set_",
        "",
        "## Dependencies",
        feature["dependencies"] or "_Not set_",
        "",
        "## Risks",
        feature["risks"] or "_Not set_",
        "",
        "## Acceptance Criteria",
        feature["acceptance_criteria"] or "_Not set_",
        "",
        "## Implementation Phases",
        feature["implementation_phases"] or "_Not set_",
        "",
        "## Impacted Files or Modules",
        feature["impacted_files_modules"] or "_Not set_",
        "",
        "## Work Items",
    ]

    if not work_items:
        lines.append("_No work items_")
    else:
        for row in work_items:
            lines.extend(
                [
                    f"### {row['id']} - {row['title']}",
                    f"- type: {row['type']}",
                    f"- status: {row['status']}",
                    f"- priority: {row['priority']}",
                    f"- parent: {row['parent_work_item_id'] or 'none'}",
                    f"- summary: {row['summary'] or 'n/a'}",
                    f"- notes: {row['implementation_notes'] or 'n/a'}",
                    "",
                ]
            )

    lines.append("## Links")
    if not links:
        lines.append("_No links_")
    else:
        for row in links:
            lines.append(
                f"- #{row['id']}: {row['source_work_item_id']} {row['relation_type']} {row['target_work_item_id']}"
            )
            if row["notes"]:
                lines.append(f"  - notes: {row['notes']}")

    markdown = "\n".join(lines).rstrip() + "\n"
    output_path = None
    if args.output:
        output_path = Path(args.output).expanduser()
        if not output_path.is_absolute():
            output_path = (Path.cwd() / output_path).resolve()
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(markdown, encoding="utf-8")

    emit(
        {
            "feature_id": args.feature_id,
            "output_path": str(output_path) if output_path else None,
            "markdown": markdown,
            "db_path": str(db_path),
        }
    )
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Manage tracker features and work items in a local SQLite database.",
    )
    parser.add_argument(
        "--db-path",
        default=str(DEFAULT_DB_PATH),
        help="Path to the SQLite database. Defaults to skills\\tracker\\tracker.db.",
    )
    parser.add_argument(
        "--docs-root",
        default=str(Path.cwd() / "docs"),
        help="Path to the docs root used by import-docs. Defaults to the repository docs folder.",
    )
    subparsers = parser.add_subparsers(dest="command", required=True)

    init_parser = subparsers.add_parser("init", help="Initialize the tracker database.")
    init_parser.set_defaults(func=init_command)

    import_docs = subparsers.add_parser(
        "import-docs",
        help="Import docs\\features and docs\\backlog into the tracker database.",
    )
    import_docs.set_defaults(func=import_docs_command)

    create_feature = subparsers.add_parser("create-feature", help="Create a feature.")
    create_feature.add_argument("--title", required=True)
    create_feature.add_argument("--slug")
    create_feature.add_argument("--summary")
    create_feature.add_argument("--status", choices=STATUSES, default="backlog")
    create_feature.add_argument("--priority", choices=PRIORITIES, default="medium")
    create_feature.add_argument("--source-request")
    create_feature.add_argument("--architecture")
    create_feature.add_argument("--dependencies")
    create_feature.add_argument("--risks")
    create_feature.add_argument("--acceptance-criteria")
    create_feature.add_argument("--implementation-phases")
    create_feature.add_argument("--impacted-files-modules")
    create_feature.set_defaults(func=create_feature_command)

    create_work_item = subparsers.add_parser(
        "create-work-item",
        help="Create a task, bug, change request, or research item.",
    )
    create_work_item.add_argument("--feature-id", required=True)
    create_work_item.add_argument("--parent-id")
    create_work_item.add_argument("--type", choices=tuple(WORK_ITEM_TYPES), required=True)
    create_work_item.add_argument("--title", required=True)
    create_work_item.add_argument("--summary")
    create_work_item.add_argument("--status", choices=STATUSES, default="backlog")
    create_work_item.add_argument("--priority", choices=PRIORITIES, default="medium")
    create_work_item.add_argument("--source-request")
    create_work_item.add_argument("--implementation-notes")
    create_work_item.set_defaults(func=create_work_item_command)

    update_item = subparsers.add_parser("update-item", help="Update a feature or work item.")
    update_item.add_argument("--id", required=True)
    update_item.add_argument("--title")
    update_item.add_argument("--slug")
    update_item.add_argument("--summary")
    update_item.add_argument("--status", choices=STATUSES)
    update_item.add_argument("--priority", choices=PRIORITIES)
    update_item.add_argument("--source-request")
    update_item.add_argument("--architecture")
    update_item.add_argument("--dependencies")
    update_item.add_argument("--risks")
    update_item.add_argument("--acceptance-criteria")
    update_item.add_argument("--implementation-phases")
    update_item.add_argument("--impacted-files-modules")
    update_item.add_argument("--parent-id")
    update_item.add_argument("--clear-parent", action="store_true")
    update_item.add_argument("--implementation-notes")
    update_item.set_defaults(func=update_item_command)

    normalize_text = subparsers.add_parser(
        "normalize-text",
        help="Normalize persisted text fields for features and work items.",
    )
    normalize_text.set_defaults(func=normalize_text_command)

    delete_item = subparsers.add_parser("delete-item", help="Delete a feature or work item.")
    delete_item.add_argument("--id", required=True)
    delete_item.set_defaults(func=delete_item_command)

    link_items = subparsers.add_parser("link-items", help="Create a relation between work items.")
    link_items.add_argument("--source-id", required=True)
    link_items.add_argument("--target-id", required=True)
    link_items.add_argument("--relation-type", choices=RELATION_TYPES, required=True)
    link_items.add_argument("--notes")
    link_items.set_defaults(func=link_items_command)

    delete_link = subparsers.add_parser("delete-link", help="Delete a work-item relation.")
    delete_link.add_argument("--link-id", type=int, required=True)
    delete_link.set_defaults(func=delete_link_command)

    get_item = subparsers.add_parser("get-item", help="Read a feature or work item.")
    get_item.add_argument("--id", required=True)
    get_item.set_defaults(func=get_item_command)

    list_features = subparsers.add_parser("list-features", help="List features.")
    list_features.add_argument("--status", choices=STATUSES)
    list_features.add_argument("--priority", choices=PRIORITIES)
    list_features.set_defaults(func=list_features_command)

    list_work_items = subparsers.add_parser("list-work-items", help="List work items.")
    list_work_items.add_argument("--feature-id")
    list_work_items.add_argument("--status", choices=STATUSES)
    list_work_items.add_argument("--type", choices=tuple(WORK_ITEM_TYPES))
    list_work_items.set_defaults(func=list_work_items_command)

    history = subparsers.add_parser("history", help="Read audit history for an entity.")
    history.add_argument("--entity-id", required=True)
    history.add_argument("--limit", type=int, default=20)
    history.set_defaults(func=history_command)

    export_feature = subparsers.add_parser(
        "export-feature",
        help="Export a readable Markdown summary for a feature.",
    )
    export_feature.add_argument("--feature-id", required=True)
    export_feature.add_argument("--output")
    export_feature.set_defaults(func=export_feature_command)

    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    try:
        return int(args.func(args))
    except Exception as exc:
        print(f"Error: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
