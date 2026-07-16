#!/usr/bin/env bash
set -euo pipefail

environment="${1:-Development}"
configuration="Release"
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$root/src/backend/applications/AdvancedFrontier.Functions/AdvancedFrontier.Functions.csproj"
artifact_root="$root/artifacts/backend"
publish_dir="$artifact_root/publish"
case "$publish_dir" in "$root"/artifacts/backend/*) ;; *) echo "Artifact path escaped repository" >&2; exit 1 ;; esac

version="$(tr -d '\r\n' < "$root/VERSION")"
sha="$(git -C "$root" rev-parse --short=12 HEAD 2>/dev/null || printf local)"
build_date="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
artifact_name="kinhub-backend-$version-$sha"
zip_path="$artifact_root/$artifact_name.zip"

mkdir -p "$artifact_root"
rm -rf "$publish_dir"
mkdir -p "$publish_dir"
rm -f "$zip_path" "$zip_path.sha256"

properties=("-p:Version=$version" "-p:CommitSha=$sha" "-p:BuildDate=$build_date" "-p:BuildEnvironment=$environment" "-p:UseAppHost=false")
dotnet restore "$project"
dotnet build "$project" --configuration "$configuration" --no-restore "${properties[@]}"
dotnet publish "$project" --configuration "$configuration" --no-restore --output "$publish_dir" "${properties[@]}"

test -f "$publish_dir/host.json" || { echo "host.json missing from publish root" >&2; exit 1; }
test -f "$publish_dir/AdvancedFrontier.Functions.dll" || { echo "Function assembly missing from publish root" >&2; exit 1; }
if find "$publish_dir" -type f \( -name 'local.settings.json' -o -name '.env*' \) -print -quit | grep -q .; then
  echo "Forbidden local configuration found in publish output" >&2
  exit 1
fi

(cd "$publish_dir" && zip -q -r "$zip_path" .)
checksum="$(sha256sum "$zip_path" | awk '{print $1}')"
printf '%s  %s\n' "$checksum" "$(basename "$zip_path")" > "$zip_path.sha256"
printf '{\n  "appName": "KinHub",\n  "component": "backend",\n  "version": "%s",\n  "commitSha": "%s",\n  "buildDate": "%s",\n  "environment": "%s",\n  "artifact": "%s",\n  "sha256": "%s"\n}\n' \
  "$version" "$sha" "$build_date" "$environment" "$(basename "$zip_path")" "$checksum" > "$artifact_root/build-manifest.json"
printf '%s\n' "$zip_path"
