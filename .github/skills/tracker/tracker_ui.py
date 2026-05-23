#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import mimetypes
from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from typing import Any
from urllib.parse import parse_qs, urlparse

from tracker_data import (
    DEFAULT_DB_PATH,
    get_feature_detail_data,
    get_history_data,
    get_overview_data,
    get_work_item_detail_data,
    list_features_data,
    list_work_items_data,
    open_connection,
    resolve_db_path,
)


SCRIPT_DIR = Path(__file__).resolve().parent
UI_DIR = SCRIPT_DIR / "ui"


def parse_int(raw_value: str | None, default: int) -> int:
    if raw_value is None:
        return default
    return max(1, int(raw_value))


def build_handler(db_path: Path) -> type[BaseHTTPRequestHandler]:
    class TrackerUiHandler(BaseHTTPRequestHandler):
        def do_GET(self) -> None:  # noqa: N802
            parsed = urlparse(self.path)
            try:
                if parsed.path.startswith("/api/"):
                    self.handle_api(parsed)
                    return
                self.handle_static(parsed.path)
            except Exception as exc:
                self.send_json(
                    HTTPStatus.INTERNAL_SERVER_ERROR,
                    {"error": str(exc)},
                )

        def handle_api(self, parsed: Any) -> None:
            query = parse_qs(parsed.query)
            with open_connection(db_path) as conn:
                if parsed.path == "/api/overview":
                    limit = parse_int(query.get("history_limit", [None])[0], 20)
                    payload = get_overview_data(conn, limit)
                elif parsed.path == "/api/features":
                    status = query.get("status", [None])[0]
                    priority = query.get("priority", [None])[0]
                    payload = {
                        "features": list_features_data(conn, status, priority),
                        "db_path": str(db_path),
                    }
                elif parsed.path == "/api/work-items":
                    payload = {
                        "work_items": list_work_items_data(
                            conn,
                            feature_id=query.get("feature_id", [None])[0],
                            status=query.get("status", [None])[0],
                            work_item_type=query.get("type", [None])[0],
                        ),
                        "db_path": str(db_path),
                    }
                elif parsed.path.startswith("/api/features/"):
                    feature_id = parsed.path.rsplit("/", 1)[-1]
                    payload = get_feature_detail_data(conn, feature_id)
                    payload["db_path"] = str(db_path)
                elif parsed.path.startswith("/api/work-items/"):
                    work_item_id = parsed.path.rsplit("/", 1)[-1]
                    payload = get_work_item_detail_data(conn, work_item_id)
                    payload["db_path"] = str(db_path)
                elif parsed.path.startswith("/api/history/"):
                    entity_id = parsed.path.rsplit("/", 1)[-1]
                    limit = parse_int(query.get("limit", [None])[0], 20)
                    payload = {
                        "history": get_history_data(conn, entity_id, limit),
                        "db_path": str(db_path),
                    }
                else:
                    self.send_json(HTTPStatus.NOT_FOUND, {"error": "Endpoint not found."})
                    return

            self.send_json(HTTPStatus.OK, payload)

        def handle_static(self, raw_path: str) -> None:
            path = raw_path if raw_path != "/" else "/index.html"
            if path not in {"/index.html", "/app.js", "/styles.css"}:
                self.send_json(HTTPStatus.NOT_FOUND, {"error": "Page not found."})
                return

            file_path = UI_DIR / path.lstrip("/")
            if not file_path.exists():
                self.send_json(HTTPStatus.NOT_FOUND, {"error": "Asset not found."})
                return

            content_type, _ = mimetypes.guess_type(file_path.name)
            payload = file_path.read_bytes()
            self.send_response(HTTPStatus.OK)
            self.send_header(
                "Content-Type",
                f"{content_type or 'application/octet-stream'}; charset=utf-8",
            )
            self.send_header("Content-Length", str(len(payload)))
            self.end_headers()
            self.wfile.write(payload)

        def send_json(self, status: HTTPStatus, payload: dict[str, object]) -> None:
            body = json.dumps(payload, indent=2, ensure_ascii=True).encode("utf-8")
            self.send_response(status)
            self.send_header("Content-Type", "application/json; charset=utf-8")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        def log_message(self, format: str, *args: object) -> None:
            return

    return TrackerUiHandler


def serve_command(args: argparse.Namespace) -> int:
    db_path = resolve_db_path(args.db_path)
    handler = build_handler(db_path)
    server = ThreadingHTTPServer((args.host, args.port), handler)
    print(
        json.dumps(
            {
                "status": "serving",
                "url": f"http://{args.host}:{args.port}",
                "db_path": str(db_path),
            },
            indent=2,
            ensure_ascii=True,
        )
    )
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Serve a read-only HTML UI for the tracker SQLite database.",
    )
    parser.add_argument(
        "--db-path",
        default=str(DEFAULT_DB_PATH),
        help="Path to the SQLite database. Defaults to skills\\tracker\\tracker.db.",
    )
    subparsers = parser.add_subparsers(dest="command", required=True)

    serve_parser = subparsers.add_parser("serve", help="Start the local tracker UI server.")
    serve_parser.add_argument("--host", default="127.0.0.1")
    serve_parser.add_argument("--port", type=int, default=8765)
    serve_parser.set_defaults(func=serve_command)

    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    return int(args.func(args))


if __name__ == "__main__":
    raise SystemExit(main())
