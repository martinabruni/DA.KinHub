#!/usr/bin/env bash
#
# Static gate for the migration / IaC rollout contract.
# It fails when:
# - application hosts call Database.Migrate() instead of the dedicated runner;
# - the deprecated KINLIST_CORE_API_BASE_URL resurfaces;
# - the IaC/workflow stops proving the migration-job gate;
# - KinRecipe legacy shopping-list routes stop redirecting to KinList.

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

require_match() {
  local pattern="$1"
  local file="$2"
  local message="$3"

  if ! grep -qE "$pattern" "$file"; then
    echo "ERROR: ${message}" >&2
    exit 1
  fi
}

require_multiline_match() {
  local pattern="$1"
  local file="$2"
  local message="$3"

  if ! perl -0ne "exit((/${pattern}/s) ? 0 : 1)" "$file"; then
    echo "ERROR: ${message}" >&2
    exit 1
  fi
}

ensure_no_matches() {
  local pattern="$1"
  shift

  local matches
  matches="$(
    rg -n "$pattern" "$@" \
      -g '!**/bin/**' \
      -g '!**/obj/**' \
      -g '!**/dist/**' \
      -g '!**/node_modules/**' \
      -g '!scripts/verify-migration-iac.sh' \
      2>/dev/null || true
  )"
  if [ -n "$matches" ]; then
    echo "ERROR: unexpected matches found:" >&2
    echo "$matches" >&2
    exit 1
  fi
}

echo "Checking that only the migration runner applies EF migrations..."
matches="$(
  rg -n 'Database\.Migrate\(|\.MigrateAsync\(' src/Presentations src/Infrastructures \
    -g '!**/bin/**' \
    -g '!**/obj/**' \
    -g '!**/*.Designer.cs' \
    -g '!src/Presentations/Kin.KinHub.Migrations.Runner/Program.cs' \
    2>/dev/null || true
)"

if [ -n "$matches" ]; then
  echo "ERROR: found EF migration calls outside the dedicated runner:" >&2
  echo "$matches" >&2
  exit 1
fi

runner_program='src/Presentations/Kin.KinHub.Migrations.Runner/Program.cs'
runner_service='src/Presentations/Kin.KinHub.Migrations.Runner/MigrationRunnerService.cs'
require_match 'new\("IdentityDbContext", ApplyIdentityMigrationsAsync\)' "$runner_program" 'Migration runner must apply IdentityDbContext first.'
require_match 'new\("KinListDbContext", ApplyKinListMigrationsAsync\)' "$runner_program" 'Migration runner must apply KinListDbContext second.'
require_match 'new\("CoreDbContext", ApplyCoreMigrationsAsync\)' "$runner_program" 'Migration runner must apply CoreDbContext third.'
require_match 'Applying \{step\.Name\} migrations \(step \{index \+ 1\}/\{_steps\.Count\}\)' "$runner_service" 'Migration runner must log ordered step progress.'

echo "Checking removal of deprecated Core->KinList wiring..."
ensure_no_matches 'KINLIST_CORE_API_BASE_URL' .github ops src scripts
require_match 'FamilyContextApi__BaseUrl' 'ops/iac/modules/compute.bicep' 'IaC must derive FamilyContextApi__BaseUrl from Identity.'

echo "Checking migration rollout gate in CI/CD..."
backend_workflow='.github/workflows/backend.yml'
require_match 'Package Migration Runner Image' "$backend_workflow" 'Backend workflow must package the migration runner image.'
require_match 'az containerapp job execution show' "$backend_workflow" 'Backend workflow must poll migration job execution status.'
require_match 'Roll out backend revisions after migration success' "$backend_workflow" 'Backend workflow must roll out app images only after migration success.'

echo "Checking Key Vault based secret wiring in IaC..."
main_bicep='ops/iac/main.bicep'
require_multiline_match "name: 'db-connection-string'.*?keyVaultUrl:" "$main_bicep" 'db-connection-string must be a Key Vault reference.'
require_multiline_match "name: 'jwt-secret'.*?keyVaultUrl:" "$main_bicep" 'jwt-secret must be a Key Vault reference.'
require_multiline_match "name: 'openai-key'.*?keyVaultUrl:" "$main_bicep" 'openai-key must be a Key Vault reference.'
require_multiline_match "name: 'speech-key'.*?keyVaultUrl:" "$main_bicep" 'speech-key must be a Key Vault reference.'
require_multiline_match "name: 'ghcr-password'.*?keyVaultUrl:" "$main_bicep" 'ghcr-password must be a Key Vault reference.'

echo "Checking KinRecipe -> KinList legacy redirects..."
kinrecipe_routes='src/Presentations/Kin.KinHub.KinRecipe.React/src/router/routes.tsx'
kinrecipe_redirects='src/Presentations/Kin.KinHub.KinRecipe.React/src/features/shopping-lists/pages/ShoppingListRedirects.tsx'
kinrecipe_links='src/Presentations/Kin.KinHub.KinRecipe.React/src/config/appLinks.ts'

require_match "path: '/shopping-lists'.*ShoppingListsRedirect" "$kinrecipe_routes" 'KinRecipe /shopping-lists route must redirect to KinList.'
require_match "path: '/shopping-lists/:id'.*ShoppingListDetailRedirect" "$kinrecipe_routes" 'KinRecipe /shopping-lists/:id route must redirect to KinList detail.'
require_match 'window\.location\.assign\(buildKinListRootUrl\(\)\)' "$kinrecipe_redirects" 'Legacy shopping-list root must redirect to KinList root.'
require_match 'window\.location\.assign\(id \? buildKinListDetailUrl\(id\) : buildKinListRootUrl\(\)\)' "$kinrecipe_redirects" 'Legacy shopping-list detail must preserve the KinList id.'
require_match 'return buildKinListUrl\(`/lists/\$\{id\}`\)\.toString\(\)' "$kinrecipe_links" 'KinRecipe KinList detail link must target /lists/{id}.'

echo "Migration / IaC contract checks passed."
