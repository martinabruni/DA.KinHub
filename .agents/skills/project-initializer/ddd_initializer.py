#!/usr/bin/env python3
from __future__ import annotations

import argparse
import re
import shutil
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path, PurePosixPath
from typing import Iterable, Sequence
import xml.etree.ElementTree as ET


# Removed CS_PROJ_TEMPLATE - now using dotnet CLI to generate projects


@dataclass(frozen=True)
class ProjectKind:
    folder: str
    suffix: str | None
    typed: bool


PROJECT_KINDS = {
    "domains": ProjectKind("src/Domains", "Domain", False),
    "businesses": ProjectKind("src/Businesses", "Business", False),
    "presentations": ProjectKind("src/Presentations", None, True),
    "infrastructures": ProjectKind("src/Infrastructures", None, True),
    "tests": ProjectKind("src/Tests", None, True),
}

KIND_ALIASES = {
    "domain": "domains",
    "business": "businesses",
    "presentation": "presentations",
    "infrastructure": "infrastructures",
    "test": "tests",
}

MIN_PYTHON_VERSION = (3, 10)


def normalize_segment(value: str | None) -> str:
    if not value:
        return ""
    return re.sub(r"[^0-9A-Za-z]", "", value)


def parse_types(value: str | None) -> list[str]:
    if not value:
        return []
    return [
        seg for seg in (normalize_segment(part) for part in value.split(",")) if seg
    ]


def check_dotnet_availability() -> bool:
    """Check if dotnet CLI is available in PATH."""
    try:
        result = subprocess.run(
            ["dotnet", "--version"],
            capture_output=True,
            text=True,
            check=False,
        )
        return result.returncode == 0
    except FileNotFoundError:
        return False


def check_python_version() -> bool:
    return sys.version_info >= MIN_PYTHON_VERSION


def ensure_gitkeep(directory: Path) -> None:
    directory.mkdir(parents=True, exist_ok=True)
    (directory / ".gitkeep").touch(exist_ok=True)


def create_static_tree(root: Path) -> None:
    for parent, children in {
        "docs": ("specs", "researches", "plans"),
        "ops": ("iac", "pipelines"),
    }.items():
        for child in children:
            ensure_gitkeep(root / parent / child)


def resolve_kind_key(category: str) -> str:
    key = category.strip().lower()
    if not key:
        raise ValueError("Category cannot be empty.")
    key = KIND_ALIASES.get(key, key)
    if key not in PROJECT_KINDS:
        raise ValueError(f"Unknown project category '{category}'.")
    return key


def build_project_name(
    project_base: str, kind: ProjectKind, type_name: str | None
) -> str:
    if kind.typed:
        if not type_name:
            raise ValueError("Project type is required for typed categories.")
        return f"{project_base}.{type_name}"
    suffix = kind.suffix
    return f"{project_base}.{suffix}" if suffix else project_base


def build_project_path(root: Path, kind: ProjectKind, project_name: str) -> Path:
    return root / kind.folder / project_name / f"{project_name}.csproj"


def create_project_with_dotnet(project_dir: Path, project_name: str, tfm: str) -> Path:
    """Create a C# class library project using dotnet CLI."""
    project_dir.parent.mkdir(parents=True, exist_ok=True)
    csproj_path = project_dir / f"{project_name}.csproj"

    if csproj_path.exists():
        return csproj_path

    # Create project using dotnet CLI
    result = subprocess.run(
        [
            "dotnet",
            "new",
            "classlib",
            "-n",
            project_name,
            "-o",
            str(project_dir),
            "-f",
            tfm,
        ],
        capture_output=True,
        text=True,
        check=False,
    )

    if result.returncode != 0:
        raise RuntimeError(f"Failed to create project {project_name}: {result.stderr}")

    # Edit .csproj to ensure RootNamespace and AssemblyName are set correctly
    if csproj_path.exists():
        tree = ET.parse(csproj_path)
        root = tree.getroot()

        # Find or create PropertyGroup
        prop_group = root.find("PropertyGroup")
        if prop_group is None:
            prop_group = ET.SubElement(root, "PropertyGroup")

        # Ensure RootNamespace and AssemblyName
        root_ns = prop_group.find("RootNamespace")
        if root_ns is None:
            root_ns = ET.SubElement(prop_group, "RootNamespace")
        root_ns.text = project_name

        assembly_name = prop_group.find("AssemblyName")
        if assembly_name is None:
            assembly_name = ET.SubElement(prop_group, "AssemblyName")
        assembly_name.text = project_name

        tree.write(csproj_path, encoding="utf-8", xml_declaration=True)

    # Clean up default Class1.cs
    class1_path = project_dir / "Class1.cs"
    if class1_path.exists():
        class1_path.unlink()

    return csproj_path


def add_project(
    root: Path,
    project_base: str,
    tfm: str,
    category: str,
    type_name: str | None = None,
) -> Path:
    kind_key = resolve_kind_key(category)
    kind = PROJECT_KINDS[kind_key]
    clean_type = normalize_segment(type_name) if type_name else None
    if kind.typed and not clean_type:
        raise ValueError(
            f"Category '{category}' requires a project type (e.g. {category}:Api)."
        )
    if not kind.typed and clean_type:
        raise ValueError(f"Category '{category}' does not accept a project type.")
    project_name = build_project_name(project_base, kind, clean_type)
    project_path = build_project_path(root, kind, project_name)
    project_dir = project_path.parent
    return create_project_with_dotnet(project_dir, project_name, tfm)


def ensure_placeholder_for_category(root: Path, category: str) -> None:
    kind = PROJECT_KINDS[resolve_kind_key(category)]
    ensure_gitkeep(root / kind.folder)


def add_projects_from_types(
    root: Path,
    project_base: str,
    tfm: str,
    category: str,
    types: Iterable[str],
) -> None:
    types = [t for t in types if t]
    if not types:
        ensure_placeholder_for_category(root, category)
        return
    for project_type in types:
        add_project(root, project_base, tfm, category, project_type)


def parse_project_spec(value: str) -> tuple[str, str | None]:
    parts = value.split(":", 1)
    category = parts[0]
    project_type = parts[1] if len(parts) > 1 else None
    return category, project_type


def confirm(prompt: str) -> bool:
    response = input(f"{prompt} [y/N]: ").strip().lower()
    return response in {"y", "yes"}


def remove_project(
    root: Path,
    project_base: str,
    category: str,
    type_name: str | None,
    *,
    confirmation: bool = True,
) -> bool:
    kind_key = resolve_kind_key(category)
    kind = PROJECT_KINDS[kind_key]
    clean_type = normalize_segment(type_name) if type_name else None
    if kind.typed and not clean_type:
        raise ValueError(
            f"Category '{category}' requires a project type to remove (e.g. {category}:Api)."
        )
    if not kind.typed and clean_type:
        raise ValueError(f"Category '{category}' does not take a project type.")

    project_name = build_project_name(project_base, kind, clean_type)
    project_path = build_project_path(root, kind, project_name)
    if not project_path.exists():
        print(f"Project '{project_name}' not found at {project_path}.")
        return False

    if confirmation and not confirm(
        f"Delete project '{project_name}' and its contents?"
    ):
        print("Removal aborted.")
        return False

    project_dir = project_path.parent  # …/Infrastructures/Client.Project.SqlServer/
    shutil.rmtree(project_dir)
    category_dir = root / kind.folder
    if category_dir.exists():
        has_files = any(p.name != ".gitkeep" for p in category_dir.iterdir())
        if not has_files:
            ensure_gitkeep(category_dir)
    print(f"Removed project '{project_name}'.")
    return True


def collect_projects(root: Path) -> list[Path]:
    project_paths = sorted({p.resolve() for p in root.rglob("*.csproj")})
    return list(project_paths)


def collect_solution_directories_and_items(
    root: Path, folder_names: Sequence[str]
) -> tuple[list[Path], list[Path]]:
    directories: set[Path] = set()
    items: list[Path] = []
    for name in folder_names:
        base = (root / name).resolve()
        if not base.exists():
            continue
        directories.add(base)
        for path in base.rglob("*"):
            if path.is_dir():
                directories.add(path.resolve())
            elif path.is_file():
                items.append(path.resolve())
    return sorted(directories), sorted(items)


def to_pure_relative(path: Path, root: Path) -> PurePosixPath:
    try:
        rel = path.resolve().relative_to(root.resolve())
    except ValueError:
        rel = path.resolve()
    rel_str = rel.as_posix()
    if rel_str in ("", "."):
        return PurePosixPath(".")
    return PurePosixPath(rel_str)


def solution_csproj_path(rel: PurePosixPath) -> PurePosixPath:
    """Return the path as it should appear in the .slnx file.

    When the .csproj lives inside a same-named subfolder
    (e.g. Infrastructures/Foo.Bar/Foo.Bar.csproj) the solution entry
    should omit that subfolder (e.g. Infrastructures/Foo.Bar.csproj).
    """
    if rel.suffix.lower() == ".csproj" and rel.parent.name == rel.stem:
        return rel.parent.parent / rel.name
    return rel


def windows_path(rel: PurePosixPath) -> str:
    rel_str = rel.as_posix()
    if rel_str in ("", "."):
        return "."
    return rel_str.replace("/", "\\")


def create_solution(
    solution_path: Path,
    project_paths: Sequence[Path],
    folder_directories: Sequence[Path],
    folder_items: Sequence[Path],
) -> None:
    if not project_paths and not folder_items and not folder_directories:
        return

    solution_path.parent.mkdir(parents=True, exist_ok=True)
    root_dir = solution_path.parent.resolve()

    # Build a mapping: folder_posix_path -> {"projects": [...], "files": [...]}
    # Folders are flat (e.g. "src/Businesses"), projects/files go into
    # the deepest matching folder.
    folders: dict[str, dict] = {}

    def ensure_folder(key: str) -> dict:
        if key not in folders:
            folders[key] = {"projects": [], "files": []}
        return folders[key]

    # Ensure all declared directories appear as folders (even if empty)
    for directory in folder_directories:
        rel = to_pure_relative(directory, root_dir).as_posix()
        if rel not in ("", "."):
            ensure_folder(rel)

    # Place each project in its parent folder (using the solution path for
    # folder grouping, but the real filesystem path for the Path attribute)
    for project in project_paths:
        rel = to_pure_relative(project, root_dir)
        slnx_rel = solution_csproj_path(rel)
        parent = slnx_rel.parent.as_posix()
        if parent in ("", "."):
            parent = "__root__"
        node = ensure_folder(parent)
        node["projects"].append(rel)  # real path, with project subfolder

    # Place each file item in its parent folder
    for item in folder_items:
        rel = to_pure_relative(item, root_dir)
        parent = rel.parent.as_posix()
        if parent in ("", "."):
            parent = "__root__"
        ensure_folder(parent)["files"].append(rel)

    solution_elem = ET.Element("Solution")

    # Root-level projects (no folder)
    root_node = folders.pop("__root__", {"projects": [], "files": []})
    for proj in sorted(root_node["projects"], key=lambda p: p.as_posix().lower()):
        attrs = {"Path": windows_path(proj)}
        if proj.suffix.lower() == ".csproj":
            attrs["Type"] = "Classic C#"
        ET.SubElement(solution_elem, "Project", attrs)

    # All folders sorted so parents come before children
    for key in sorted(folders):
        node = folders[key]
        projects = sorted(node["projects"], key=lambda p: p.as_posix().lower())
        files = sorted(node["files"], key=lambda p: p.as_posix().lower())
        if not projects and not files:
            continue  # skip empty folders
        folder_name = f"/{key}/"
        folder_elem = ET.SubElement(solution_elem, "Folder", Name=folder_name)
        for proj in projects:
            attrs = {"Path": windows_path(proj)}
            if proj.suffix.lower() == ".csproj":
                attrs["Type"] = "Classic C#"
            ET.SubElement(folder_elem, "Project", attrs)
        for f in files:
            ET.SubElement(folder_elem, "File", {"Path": windows_path(f)})

    tree = ET.ElementTree(solution_elem)
    ET.indent(tree, space="  ")
    tree.write(solution_path, encoding="utf-8", xml_declaration=False)


def create_sln_solution(
    solution_path: Path,
    project_paths: Sequence[Path],
) -> None:
    """Create a classic .sln file using dotnet CLI."""
    if not project_paths:
        return

    solution_path.parent.mkdir(parents=True, exist_ok=True)

    # Create solution using dotnet CLI with classic .sln format
    result = subprocess.run(
        [
            "dotnet",
            "new",
            "sln",
            "-n",
            solution_path.stem,
            "-o",
            str(solution_path.parent),
            "--format",
            "sln",
        ],
        capture_output=True,
        text=True,
        check=False,
    )

    if result.returncode != 0:
        raise RuntimeError(f"Failed to create solution: {result.stderr}")

    # Add all projects to solution
    for project_path in project_paths:
        result = subprocess.run(
            ["dotnet", "sln", str(solution_path), "add", str(project_path)],
            capture_output=True,
            text=True,
            check=False,
        )

        if result.returncode != 0:
            print(f"Warning: Failed to add project {project_path}: {result.stderr}")


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Initialize or update a DDD-friendly folder tree with optional project scaffolding.",
    )
    parser.add_argument(
        "--root",
        default=".",
        help="Root directory to scaffold (default: current directory).",
    )
    parser.add_argument(
        "--client", required=True, help="Client identifier (alphanumeric)."
    )
    parser.add_argument(
        "--project", required=True, help="Project identifier (alphanumeric)."
    )
    parser.add_argument("--domain", help="Optional domain identifier (alphanumeric).")
    parser.add_argument(
        "--presentations", "-p", help="Comma separated presentation project types."
    )
    parser.add_argument(
        "--infrastructures", "-i", help="Comma separated infrastructure project types."
    )
    parser.add_argument("--tests", "-t", help="Comma separated test project types.")
    parser.add_argument(
        "--target-framework",
        "--tfm",
        default="net10.0",
        help="Target framework for generated csproj files.",
    )
    parser.add_argument(
        "--solution-format",
        choices=["slnx", "sln"],
        default="slnx",
        help="Solution file format: slnx (modern XML) or sln (classic). Default: slnx",
    )
    parser.add_argument(
        "--add-project",
        action="append",
        dest="additional_projects",
        help="Add a single project using the format Category[:Type] (e.g. Presentations:Api).",
    )
    parser.add_argument(
        "--remove-project",
        action="append",
        dest="projects_to_remove",
        help="Remove a project using the format Category[:Type] (e.g. Presentations:Api).",
    )

    args = parser.parse_args()

    if not check_python_version():
        min_version = ".".join(str(part) for part in MIN_PYTHON_VERSION)
        print(f"Error: Python {min_version}+ is required.")
        return 1

    # Check if dotnet CLI is available
    if not check_dotnet_availability():
        print("Error: dotnet CLI is not available. Please install .NET SDK.")
        print("Download from: https://dotnet.microsoft.com/download")
        return 1

    root = Path(args.root).expanduser().resolve()
    root.mkdir(parents=True, exist_ok=True)

    client = normalize_segment(args.client)
    project = normalize_segment(args.project)
    domain = normalize_segment(args.domain)
    if not client:
        parser.error("Client must contain at least one alphanumeric character.")
    if not project:
        parser.error("Project must contain at least one alphanumeric character.")

    namespace_parts = [client, project]
    if domain:
        namespace_parts.append(domain)
    project_base = ".".join(namespace_parts)

    create_static_tree(root)

    # Default core projects
    add_project(root, project_base, args.target_framework, "domains")
    add_project(root, project_base, args.target_framework, "businesses")

    # Optional project sets
    add_projects_from_types(
        root,
        project_base,
        args.target_framework,
        "presentations",
        parse_types(args.presentations),
    )
    add_projects_from_types(
        root,
        project_base,
        args.target_framework,
        "infrastructures",
        parse_types(args.infrastructures),
    )
    add_projects_from_types(
        root,
        project_base,
        args.target_framework,
        "tests",
        parse_types(args.tests),
    )

    # Additional ad-hoc project additions
    if args.additional_projects:
        for spec in args.additional_projects:
            category, project_type = parse_project_spec(spec)
            add_project(
                root, project_base, args.target_framework, category, project_type
            )

    # Project removals
    if args.projects_to_remove:
        for spec in args.projects_to_remove:
            category, project_type = parse_project_spec(spec)
            remove_project(root, project_base, category, project_type)

    project_paths = collect_projects(root)

    # Create solution based on format
    if args.solution_format == "sln":
        create_sln_solution(
            root / f"{project_base}.sln",
            project_paths,
        )
    else:  # slnx
        folder_dirs, folder_items = collect_solution_directories_and_items(
            root, ("docs", "ops")
        )
        create_solution(
            root / f"{project_base}.slnx", project_paths, folder_dirs, folder_items
        )

    print(f"\nSuccessfully initialized DDD project structure at: {root}")
    print(f"Solution file: {project_base}.{args.solution_format}")
    print(f"Projects created: {len(project_paths)}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
