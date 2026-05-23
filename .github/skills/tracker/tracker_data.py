from __future__ import annotations

import json
import re
import sqlite3
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


def row_to_dict(row: sqlite3.Row | None) -> dict[str, Any] | None:
    if row is None:
        return None
    return {key: row[key] for key in row.keys()}


def normalize_slug(value: str) -> str:
    slug = re.sub(r"[^a-z0-9]+", "-", value.strip().lower()).strip("-")
    if not slug:
        raise ValueError("Slug cannot be empty.")
    return slug


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


def feature_detail_data(row: sqlite3.Row | None) -> dict[str, Any] | None:
    feature = row_to_dict(row)
    if feature is None:
        return None
    feature["description"] = feature.get("summary")
    return feature


def work_item_detail_data(row: sqlite3.Row | None) -> dict[str, Any] | None:
    item = row_to_dict(row)
    if item is None:
        return None
    item["description"] = item.get("summary")
    return item


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


def list_features_data(
    conn: sqlite3.Connection,
    status: str | None = None,
    priority: str | None = None,
) -> list[dict[str, Any]]:
    query = "SELECT * FROM features"
    clauses: list[str] = []
    parameters: list[Any] = []
    if status:
        clauses.append("status = ?")
        parameters.append(status)
    if priority:
        clauses.append("priority = ?")
        parameters.append(priority)
    if clauses:
        query += " WHERE " + " AND ".join(clauses)
    query += " ORDER BY created_at, id;"
    rows = conn.execute(query, parameters).fetchall()
    return [row_to_dict(row) for row in rows if row is not None]


def list_work_items_data(
    conn: sqlite3.Connection,
    feature_id: str | None = None,
    status: str | None = None,
    work_item_type: str | None = None,
) -> list[dict[str, Any]]:
    query = "SELECT * FROM work_items"
    clauses: list[str] = []
    parameters: list[Any] = []
    if feature_id:
        clauses.append("feature_id = ?")
        parameters.append(feature_id)
    if status:
        clauses.append("status = ?")
        parameters.append(status)
    if work_item_type:
        clauses.append("type = ?")
        parameters.append(work_item_type)
    if clauses:
        query += " WHERE " + " AND ".join(clauses)
    query += " ORDER BY created_at, id;"
    rows = conn.execute(query, parameters).fetchall()
    return [row_to_dict(row) for row in rows if row is not None]


def get_history_data(
    conn: sqlite3.Connection,
    entity_id: str,
    limit: int = 20,
) -> list[dict[str, Any]]:
    rows = conn.execute(
        """
        SELECT * FROM work_item_history
        WHERE entity_id = ?
        ORDER BY created_at DESC, id DESC
        LIMIT ?;
        """,
        (entity_id, limit),
    ).fetchall()
    return [row_to_dict(row) for row in rows if row is not None]


def get_recent_history_data(
    conn: sqlite3.Connection,
    limit: int = 20,
) -> list[dict[str, Any]]:
    rows = conn.execute(
        """
        SELECT * FROM work_item_history
        ORDER BY created_at DESC, id DESC
        LIMIT ?;
        """,
        (limit,),
    ).fetchall()
    return [row_to_dict(row) for row in rows if row is not None]


def get_feature_links_data(
    conn: sqlite3.Connection,
    feature_id: str,
) -> list[dict[str, Any]]:
    work_items = conn.execute(
        "SELECT id FROM work_items WHERE feature_id = ? ORDER BY created_at, id;",
        (feature_id,),
    ).fetchall()
    work_item_ids = [row["id"] for row in work_items]
    if not work_item_ids:
        return []
    placeholders = ", ".join("?" for _ in work_item_ids)
    rows = conn.execute(
        f"""
        SELECT * FROM work_item_links
        WHERE source_work_item_id IN ({placeholders})
           OR target_work_item_id IN ({placeholders})
        ORDER BY id;
        """,
        [*work_item_ids, *work_item_ids],
    ).fetchall()
    return [row_to_dict(row) for row in rows if row is not None]


def get_feature_detail_data(
    conn: sqlite3.Connection,
    feature_id: str,
    history_limit: int = 50,
) -> dict[str, Any]:
    feature = row_to_dict(require_feature(conn, feature_id))
    work_items = list_work_items_data(conn, feature_id=feature_id)
    links = get_feature_links_data(conn, feature_id)

    history_parameters: list[Any] = [feature_id]
    history_query = """
        SELECT * FROM work_item_history
        WHERE (entity_type = 'feature' AND entity_id = ?)
    """
    if work_items:
        placeholders = ", ".join("?" for _ in work_items)
        history_query += f" OR entity_id IN ({placeholders})"
        history_parameters.extend(item["id"] for item in work_items)
    history_query += " ORDER BY created_at DESC, id DESC LIMIT ?;"
    history_parameters.append(history_limit)
    history_rows = conn.execute(history_query, history_parameters).fetchall()

    return {
        "entity_type": "feature",
        "item": feature,
        "work_items": work_items,
        "links": links,
        "history": [row_to_dict(row) for row in history_rows if row is not None],
    }


def get_work_item_detail_data(
    conn: sqlite3.Connection,
    work_item_id: str,
    history_limit: int = 50,
) -> dict[str, Any]:
    work_item_row = require_work_item(conn, work_item_id)
    work_item = work_item_detail_data(work_item_row)
    feature_row = fetch_feature(conn, work_item["feature_id"])
    feature = feature_detail_data(feature_row)
    feature_items = [
        work_item_detail_data(row)
        for row in conn.execute(
            "SELECT * FROM work_items WHERE feature_id = ? ORDER BY created_at, id;",
            (work_item["feature_id"],),
        ).fetchall()
    ]
    feature_items = [item for item in feature_items if item is not None and item["id"] != work_item_id]
    outgoing = conn.execute(
        "SELECT * FROM work_item_links WHERE source_work_item_id = ? ORDER BY id;",
        (work_item_id,),
    ).fetchall()
    incoming = conn.execute(
        "SELECT * FROM work_item_links WHERE target_work_item_id = ? ORDER BY id;",
        (work_item_id,),
    ).fetchall()

    outgoing_links = []
    for link in outgoing:
        link_data = row_to_dict(link)
        if link_data is None:
            continue
        target_item = work_item_detail_data(fetch_work_item(conn, link_data["target_work_item_id"]))
        if target_item is not None:
            link_data["target_item"] = target_item
        outgoing_links.append(link_data)

    incoming_links = []
    for link in incoming:
        link_data = row_to_dict(link)
        if link_data is None:
            continue
        source_item = work_item_detail_data(fetch_work_item(conn, link_data["source_work_item_id"]))
        if source_item is not None:
            link_data["source_item"] = source_item
        incoming_links.append(link_data)

    return {
        "entity_type": "work_item",
        "item": work_item,
        "feature": feature,
        "feature_items": feature_items,
        "outgoing_links": outgoing_links,
        "incoming_links": incoming_links,
        "history": get_history_data(conn, work_item_id, history_limit),
    }


def get_overview_data(
    conn: sqlite3.Connection,
    history_limit: int = 20,
) -> dict[str, Any]:
    features = list_features_data(conn)

    status_rows = conn.execute(
        """
        SELECT status, COUNT(*) AS item_count
        FROM work_items
        GROUP BY status
        ORDER BY status;
        """
    ).fetchall()
    type_rows = conn.execute(
        """
        SELECT type, COUNT(*) AS item_count
        FROM work_items
        GROUP BY type
        ORDER BY type;
        """
    ).fetchall()
    total_features = conn.execute("SELECT COUNT(*) AS item_count FROM features;").fetchone()
    total_work_items = conn.execute(
        "SELECT COUNT(*) AS item_count FROM work_items;"
    ).fetchone()

    return {
        "feature_count": int(total_features["item_count"]) if total_features else 0,
        "work_item_count": int(total_work_items["item_count"]) if total_work_items else 0,
        "features": features,
        "status_counts": [row_to_dict(row) for row in status_rows if row is not None],
        "type_counts": [row_to_dict(row) for row in type_rows if row is not None],
        "recent_history": get_recent_history_data(conn, history_limit),
    }


def build_feature_export_markdown(
    conn: sqlite3.Connection,
    feature_id: str,
) -> str:
    feature = require_feature(conn, feature_id)
    work_items = conn.execute(
        "SELECT * FROM work_items WHERE feature_id = ? ORDER BY created_at, id;",
        (feature_id,),
    ).fetchall()
    links = get_feature_links_data(conn, feature_id)

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

    return "\n".join(lines).rstrip() + "\n"
