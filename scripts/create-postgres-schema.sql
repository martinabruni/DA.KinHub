-- =============================================================================
-- KinHub - PostgreSQL Unified DDL Script (CamelCaseEntity convention)
-- Schemas: identity | core | kinrecipe
-- Naming:  Table names and column names use quoted PascalCase identifiers
--          to match domain model property names exactly (Mapster zero-config).
-- Requires: pgvector extension for vector(1536) columns in kinrecipe.
-- Run against an empty KinHub PostgreSQL database.
-- =============================================================================

BEGIN;

-- ---------------------------------------------------------------------------
-- Extensions
-- ---------------------------------------------------------------------------

CREATE EXTENSION IF NOT EXISTS vector;

-- ---------------------------------------------------------------------------
-- Schemas
-- ---------------------------------------------------------------------------

CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS core;
CREATE SCHEMA IF NOT EXISTS kinrecipe;

-- =============================================================================
-- IDENTITY SCHEMA
-- =============================================================================

-- identity."KinUserEntity"
CREATE TABLE identity."KinUserEntity"
(
    "Id"               UUID         NOT NULL DEFAULT gen_random_uuid(),
    "Email"            VARCHAR(256) NOT NULL,
    "DisplayName"      VARCHAR(200) NULL,
    "IsEmailVerified"  BOOLEAN      NOT NULL DEFAULT FALSE,
    "Status"           VARCHAR(50)  NOT NULL DEFAULT 'Active',
    -- Active | Locked | Suspended
    "IsDeleted"        BOOLEAN      NOT NULL DEFAULT FALSE,
    "CreatedAt"        TIMESTAMPTZ  NOT NULL DEFAULT now(),
    "UpdatedAt"        TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT "PK_identity_KinUserEntity"       PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_identity_KinUserEntity_Email" UNIQUE ("Email")
);

-- identity."ProviderEntity"  (config table — pre-populated)
-- Id: 1=KinHub, 2=Google, 3=GitHub, 4=Microsoft
CREATE TABLE identity."ProviderEntity"
(
    "Id"        INTEGER      NOT NULL,
    "Name"      VARCHAR(100) NOT NULL,
    "Label"     VARCHAR(200) NOT NULL,
    "IsActive"  BOOLEAN      NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMPTZ  NOT NULL DEFAULT now(),
    "UpdatedAt" TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT "PK_identity_ProviderEntity"      PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_identity_ProviderEntity_Name" UNIQUE ("Name")
);

-- identity."UserCredentialEntity"
CREATE TABLE identity."UserCredentialEntity"
(
    "Id"           UUID        NOT NULL DEFAULT gen_random_uuid(),
    "UserId"       UUID        NOT NULL,
    "PasswordHash" TEXT        NOT NULL,
    "IsDeleted"    BOOLEAN     NOT NULL DEFAULT FALSE,
    "CreatedAt"    TIMESTAMPTZ NOT NULL DEFAULT now(),
    "UpdatedAt"    TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT "PK_identity_UserCredentialEntity"        PRIMARY KEY ("Id"),
    CONSTRAINT "FK_identity_UserCredentialEntity_UserId"
        FOREIGN KEY ("UserId") REFERENCES identity."KinUserEntity" ("Id"),
    CONSTRAINT "UQ_identity_UserCredentialEntity_UserId" UNIQUE ("UserId")
);

-- identity."UserProviderEntity"
CREATE TABLE identity."UserProviderEntity"
(
    "Id"             UUID         NOT NULL DEFAULT gen_random_uuid(),
    "UserId"         UUID         NOT NULL,
    "ProviderId"     INTEGER      NOT NULL,
    "ProviderUserId" VARCHAR(256) NOT NULL,
    "IsDeleted"      BOOLEAN      NOT NULL DEFAULT FALSE,
    "CreatedAt"      TIMESTAMPTZ  NOT NULL DEFAULT now(),
    "UpdatedAt"      TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT "PK_identity_UserProviderEntity"  PRIMARY KEY ("Id"),
    CONSTRAINT "FK_identity_UserProviderEntity_UserId"
        FOREIGN KEY ("UserId")     REFERENCES identity."KinUserEntity" ("Id"),
    CONSTRAINT "FK_identity_UserProviderEntity_ProviderId"
        FOREIGN KEY ("ProviderId") REFERENCES identity."ProviderEntity" ("Id"),
    CONSTRAINT "UQ_identity_UserProviderEntity_UserProvider" UNIQUE ("UserId", "ProviderId"),
    CONSTRAINT "UQ_identity_UserProviderEntity_ProviderUser" UNIQUE ("ProviderId", "ProviderUserId")
);

-- identity."RefreshTokenEntity"
CREATE TABLE identity."RefreshTokenEntity"
(
    "Id"           UUID         NOT NULL DEFAULT gen_random_uuid(),
    "UserId"       UUID         NOT NULL,
    "Token"        VARCHAR(512) NOT NULL,
    "ExpiresAtUtc" TIMESTAMPTZ  NOT NULL,
    "Revoked"      BOOLEAN      NOT NULL DEFAULT FALSE,
    "CreatedAt"    TIMESTAMPTZ  NOT NULL DEFAULT now(),
    "UpdatedAt"    TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT "PK_identity_RefreshTokenEntity"       PRIMARY KEY ("Id"),
    CONSTRAINT "FK_identity_RefreshTokenEntity_UserId"
        FOREIGN KEY ("UserId") REFERENCES identity."KinUserEntity" ("Id"),
    CONSTRAINT "UQ_identity_RefreshTokenEntity_Token" UNIQUE ("Token")
);

CREATE INDEX "IX_identity_RefreshTokenEntity_UserId"
    ON identity."RefreshTokenEntity" ("UserId");

-- =============================================================================
-- CORE SCHEMA
-- =============================================================================

-- core."KinHubServiceEntity"  (config table — pre-populated)
CREATE TABLE core."KinHubServiceEntity"
(
    "Id"          INTEGER      NOT NULL,
    "Name"        VARCHAR(200) NOT NULL,
    "BaseUrl"     VARCHAR(500) NOT NULL,
    "IsActive"    BOOLEAN      NOT NULL DEFAULT TRUE,
    "CreatedAt"   TIMESTAMPTZ  NOT NULL DEFAULT now(),
    "UpdatedAt"   TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT "PK_core_KinHubServiceEntity" PRIMARY KEY ("Id")
);

-- core."FamilyEntity"
CREATE TABLE core."FamilyEntity"
(
    "Id"            UUID         NOT NULL DEFAULT gen_random_uuid(),
    "Name"          VARCHAR(200) NOT NULL,
    "UserId"        UUID         NOT NULL,
    "IsDeleted"     BOOLEAN      NOT NULL DEFAULT FALSE,
    "CreatedAt"     TIMESTAMPTZ  NOT NULL DEFAULT now(),
    "UpdatedAt"     TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT "PK_core_FamilyEntity" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_core_FamilyEntity_UserId"
        FOREIGN KEY ("UserId") REFERENCES identity."KinUserEntity" ("Id")
);

CREATE INDEX "IX_core_FamilyEntity_UserId"
    ON core."FamilyEntity" ("UserId");

-- core."FamilyMemberEntity"
CREATE TABLE core."FamilyMemberEntity"
(
    "Id"        UUID         NOT NULL DEFAULT gen_random_uuid(),
    "Name"      VARCHAR(200) NOT NULL,
    "FamilyId"  UUID         NOT NULL,
    "IsDeleted" BOOLEAN      NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMPTZ  NOT NULL DEFAULT now(),
    "UpdatedAt" TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT "PK_core_FamilyMemberEntity" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_core_FamilyMemberEntity_FamilyId"
        FOREIGN KEY ("FamilyId") REFERENCES core."FamilyEntity" ("Id"),
    CONSTRAINT "UQ_core_FamilyMemberEntity_FamilyName" UNIQUE ("FamilyId", "Name")
);

CREATE INDEX "IX_core_FamilyMemberEntity_FamilyId"
    ON core."FamilyMemberEntity" ("FamilyId");

-- core."FamilyServiceEntity"
CREATE TABLE core."FamilyServiceEntity"
(
    "Id"        UUID        NOT NULL DEFAULT gen_random_uuid(),
    "FamilyId"  UUID        NOT NULL,
    "ServiceId" INTEGER     NOT NULL,
    "IsActive"  BOOLEAN     NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT now(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT "PK_core_FamilyServiceEntity" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_core_FamilyServiceEntity_FamilyId"
        FOREIGN KEY ("FamilyId")  REFERENCES core."FamilyEntity" ("Id"),
    CONSTRAINT "FK_core_FamilyServiceEntity_ServiceId"
        FOREIGN KEY ("ServiceId") REFERENCES core."KinHubServiceEntity" ("Id"),
    CONSTRAINT "UQ_core_FamilyServiceEntity_FamilyService" UNIQUE ("FamilyId", "ServiceId")
);

CREATE INDEX "IX_core_FamilyServiceEntity_FamilyId"
    ON core."FamilyServiceEntity" ("FamilyId");

-- =============================================================================
-- KINRECIPE SCHEMA
-- =============================================================================

-- kinrecipe."RecipeBookEntity"
CREATE TABLE kinrecipe."RecipeBookEntity"
(
    "Id"          UUID          NOT NULL DEFAULT gen_random_uuid(),
    "Name"        VARCHAR(200)  NOT NULL,
    "Description" VARCHAR(1000) NULL,
    "FamilyId"    UUID          NOT NULL,
    "IsDeleted"   BOOLEAN       NOT NULL DEFAULT FALSE,
    "CreatedAt"   TIMESTAMPTZ   NOT NULL DEFAULT now(),
    "UpdatedAt"   TIMESTAMPTZ   NOT NULL DEFAULT now(),

    CONSTRAINT "PK_kinrecipe_RecipeBookEntity" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_kinrecipe_RecipeBookEntity_FamilyId"
    ON kinrecipe."RecipeBookEntity" ("FamilyId") WHERE "IsDeleted" = FALSE;

-- kinrecipe."RecipeEntity"
CREATE TABLE kinrecipe."RecipeEntity"
(
    "Id"           UUID          NOT NULL DEFAULT gen_random_uuid(),
    "Name"         VARCHAR(200)  NOT NULL,
    "Backstory"    VARCHAR(2000) NULL,
    "FinalTime"    INTERVAL      NOT NULL,
    "Portions"     INTEGER       NOT NULL CHECK ("Portions" > 0),
    "RecipeBookId" UUID          NOT NULL,
    "IsDeleted"    BOOLEAN       NOT NULL DEFAULT FALSE,
    "CreatedAt"    TIMESTAMPTZ   NOT NULL DEFAULT now(),
    "UpdatedAt"    TIMESTAMPTZ   NOT NULL DEFAULT now(),

    CONSTRAINT "PK_kinrecipe_RecipeEntity" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_kinrecipe_RecipeEntity_RecipeBookId"
        FOREIGN KEY ("RecipeBookId") REFERENCES kinrecipe."RecipeBookEntity" ("Id")
);

CREATE INDEX "IX_kinrecipe_RecipeEntity_RecipeBookId"
    ON kinrecipe."RecipeEntity" ("RecipeBookId") WHERE "IsDeleted" = FALSE;

-- kinrecipe."RecipeIngredientEntity"
CREATE TABLE kinrecipe."RecipeIngredientEntity"
(
    "Id"          UUID           NOT NULL DEFAULT gen_random_uuid(),
    "Name"        VARCHAR(200)   NOT NULL,
    "MeasureUnit" VARCHAR(50)    NOT NULL,
    "Quantity"    NUMERIC(18, 4) NOT NULL CHECK ("Quantity" > 0),
    "RecipeId"    UUID           NOT NULL,
    "Embedding"   vector(1536)   NULL,
    "IsDeleted"   BOOLEAN        NOT NULL DEFAULT FALSE,
    "CreatedAt"   TIMESTAMPTZ    NOT NULL DEFAULT now(),
    "UpdatedAt"   TIMESTAMPTZ    NOT NULL DEFAULT now(),

    CONSTRAINT "PK_kinrecipe_RecipeIngredientEntity" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_kinrecipe_RecipeIngredientEntity_RecipeId"
        FOREIGN KEY ("RecipeId") REFERENCES kinrecipe."RecipeEntity" ("Id")
);

CREATE INDEX "IX_kinrecipe_RecipeIngredientEntity_RecipeId"
    ON kinrecipe."RecipeIngredientEntity" ("RecipeId") WHERE "IsDeleted" = FALSE;

CREATE INDEX "IX_kinrecipe_RecipeIngredientEntity_Embedding"
    ON kinrecipe."RecipeIngredientEntity" USING ivfflat ("Embedding" vector_cosine_ops)
    WHERE "Embedding" IS NOT NULL;

-- kinrecipe."RecipeStepEntity"
CREATE TABLE kinrecipe."RecipeStepEntity"
(
    "Id"          UUID          NOT NULL DEFAULT gen_random_uuid(),
    "Order"       INTEGER       NOT NULL CHECK ("Order" > 0),
    "Description" VARCHAR(2000) NOT NULL,
    "RecipeId"    UUID          NOT NULL,
    "IsDeleted"   BOOLEAN       NOT NULL DEFAULT FALSE,
    "CreatedAt"   TIMESTAMPTZ   NOT NULL DEFAULT now(),
    "UpdatedAt"   TIMESTAMPTZ   NOT NULL DEFAULT now(),

    CONSTRAINT "PK_kinrecipe_RecipeStepEntity" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_kinrecipe_RecipeStepEntity_RecipeId"
        FOREIGN KEY ("RecipeId") REFERENCES kinrecipe."RecipeEntity" ("Id")
);

CREATE INDEX "IX_kinrecipe_RecipeStepEntity_RecipeId"
    ON kinrecipe."RecipeStepEntity" ("RecipeId") WHERE "IsDeleted" = FALSE;

-- kinrecipe."FridgeEntity"
CREATE TABLE kinrecipe."FridgeEntity"
(
    "Id"        UUID         NOT NULL DEFAULT gen_random_uuid(),
    "Name"      VARCHAR(200) NOT NULL,
    "FamilyId"  UUID         NOT NULL,
    "IsDeleted" BOOLEAN      NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMPTZ  NOT NULL DEFAULT now(),
    "UpdatedAt" TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT "PK_kinrecipe_FridgeEntity" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_kinrecipe_FridgeEntity_FamilyId"
    ON kinrecipe."FridgeEntity" ("FamilyId") WHERE "IsDeleted" = FALSE;

-- kinrecipe."FridgeIngredientEntity"
CREATE TABLE kinrecipe."FridgeIngredientEntity"
(
    "Id"          UUID           NOT NULL DEFAULT gen_random_uuid(),
    "Name"        VARCHAR(200)   NOT NULL,
    "MeasureUnit" VARCHAR(50)    NOT NULL,
    "Quantity"    NUMERIC(18, 4) NOT NULL CHECK ("Quantity" > 0),
    "FridgeId"    UUID           NOT NULL,
    "Embedding"   vector(1536)   NULL,
    "IsDeleted"   BOOLEAN        NOT NULL DEFAULT FALSE,
    "CreatedAt"   TIMESTAMPTZ    NOT NULL DEFAULT now(),
    "UpdatedAt"   TIMESTAMPTZ    NOT NULL DEFAULT now(),

    CONSTRAINT "PK_kinrecipe_FridgeIngredientEntity" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_kinrecipe_FridgeIngredientEntity_FridgeId"
        FOREIGN KEY ("FridgeId") REFERENCES kinrecipe."FridgeEntity" ("Id")
);

CREATE INDEX "IX_kinrecipe_FridgeIngredientEntity_FridgeId"
    ON kinrecipe."FridgeIngredientEntity" ("FridgeId") WHERE "IsDeleted" = FALSE;

CREATE INDEX "IX_kinrecipe_FridgeIngredientEntity_Embedding"
    ON kinrecipe."FridgeIngredientEntity" USING ivfflat ("Embedding" vector_cosine_ops)
    WHERE "Embedding" IS NOT NULL;

-- =============================================================================
-- SEED DATA
-- =============================================================================

-- identity."ProviderEntity"
INSERT INTO identity."ProviderEntity"
    ("Id", "Name", "Label", "IsActive", "CreatedAt", "UpdatedAt")
VALUES
    (1, 'kinhub',    'Accedi con KinHub',    TRUE,  '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z'),
    (2, 'google',    'Accedi con Google',    FALSE, '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z'),
    (3, 'github',    'Accedi con GitHub',    FALSE, '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z'),
    (4, 'microsoft', 'Accedi con Microsoft', FALSE, '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z');

-- core."KinHubServiceEntity"
INSERT INTO core."KinHubServiceEntity"
    ("Id", "Name", "BaseUrl", "IsActive", "CreatedAt", "UpdatedAt")
VALUES
    (1, 'KinConsole', '/kin-console', TRUE,  '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z'),
    (2, 'KinRecipe',  '/kin-recipe',  TRUE, '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z');

COMMIT;
