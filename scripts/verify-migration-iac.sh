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

echo "Checking that EF migrations only run at application startup..."
matches="$(
  rg -n 'Database\.Migrate\(|\.MigrateAsync\(' src/Presentations src/Infrastructures \
    -g '!**/bin/**' \
    -g '!**/obj/**' \
    -g '!**/*.Designer.cs' \
    -g '!src/Presentations/Kin.KinHub.Identity.Api/Program.cs' \
    -g '!src/Presentations/Kin.KinHub.App.Functions/Program.cs' \
    -g '!src/Shared/Kin.KinHub.Shared.Kernel/Extensions/DbContextMigrationExtensions.cs' \
    2>/dev/null || true
)"

if [ -n "$matches" ]; then
  echo "ERROR: found EF migration calls outside application startup:" >&2
  echo "$matches" >&2
  exit 1
fi

identity_program='src/Presentations/Kin.KinHub.Identity.Api/Program.cs'
functions_program='src/Presentations/Kin.KinHub.App.Functions/Program.cs'
require_match 'RunMigrationsOnStartup' "$identity_program" 'Identity.Api must gate startup migrations behind RunMigrationsOnStartup.'
require_match 'RunMigrationsOnStartup' "$functions_program" 'App.Functions must gate startup migrations behind RunMigrationsOnStartup.'
require_match 'ApplyPendingMigrationsAsync' "$identity_program" 'Identity.Api must apply IdentityDbContext migrations at startup.'
require_match 'ApplyPendingMigrationsAsync' "$functions_program" 'App.Functions must apply Core/KinList/KinRecipe migrations at startup.'

echo "Checking removal of deprecated Core->KinList wiring..."
ensure_no_matches 'KINLIST_CORE_API_BASE_URL' .github ops src scripts

echo "Checking Key Vault based secret wiring in IaC..."
compute_bicep='ops/iac/modules/compute.bicep'
require_multiline_match "name: 'db-connection-string'.*?keyVaultUrl:" "$compute_bicep" 'db-connection-string must be a Key Vault reference.'
require_multiline_match "name: 'jwt-secret'.*?keyVaultUrl:" "$compute_bicep" 'jwt-secret must be a Key Vault reference.'
require_multiline_match "name: 'ghcr-password'.*?keyVaultUrl:" "$compute_bicep" 'ghcr-password must be a Key Vault reference.'
require_match "'ConnectionStrings__KinHub'" "$compute_bicep" 'Function App must resolve ConnectionStrings__KinHub from Key Vault.'
require_match "'OpenAi__Endpoint'" "$compute_bicep" 'Function App must resolve OpenAi__Endpoint from Key Vault.'
require_match "'Speech__Endpoint'" "$compute_bicep" 'Function App must resolve Speech__Endpoint from Key Vault.'

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
