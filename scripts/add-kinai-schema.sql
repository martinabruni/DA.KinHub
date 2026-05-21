-- =============================================================================
-- KinHub - Add KinAi Schema (chat conversations, messages, tool calls)
-- Schema: kinai
-- Run against an existing KinHub PostgreSQL database.
-- After running, use EF Core Power Tools to reverse-engineer the updated entities.
-- =============================================================================

BEGIN;

-- ---------------------------------------------------------------------------
-- Schema
-- ---------------------------------------------------------------------------

CREATE SCHEMA IF NOT EXISTS kinai;

-- ---------------------------------------------------------------------------
-- kinai."ChatConversationEntity"
-- ---------------------------------------------------------------------------

CREATE TABLE kinai."ChatConversationEntity"
(
    "Id"             UUID         NOT NULL DEFAULT gen_random_uuid(),
    "FamilyMemberId" UUID         NOT NULL,
    "Title"          VARCHAR(200) NOT NULL,
    "CreatedAt"      TIMESTAMPTZ  NOT NULL DEFAULT now(),
    "UpdatedAt"      TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT "PK_kinai_ChatConversationEntity" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_kinai_ChatConversationEntity_FamilyMemberId"
        FOREIGN KEY ("FamilyMemberId") REFERENCES core."FamilyMemberEntity" ("Id")
        ON DELETE CASCADE
);

CREATE INDEX "IX_kinai_ChatConversationEntity_FamilyMemberId"
    ON kinai."ChatConversationEntity" ("FamilyMemberId");

-- ---------------------------------------------------------------------------
-- kinai."ChatMessageEntity"
-- ---------------------------------------------------------------------------

CREATE TABLE kinai."ChatMessageEntity"
(
    "Id"             UUID        NOT NULL DEFAULT gen_random_uuid(),
    "ConversationId" UUID        NOT NULL,
    "Role"           VARCHAR(20) NOT NULL,  -- 'User' | 'Assistant' | 'Tool'
    "Content"        TEXT        NOT NULL,
    "CreatedAt"      TIMESTAMPTZ NOT NULL DEFAULT now(),
    "UpdatedAt"      TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT "PK_kinai_ChatMessageEntity" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_kinai_ChatMessageEntity_ConversationId"
        FOREIGN KEY ("ConversationId") REFERENCES kinai."ChatConversationEntity" ("Id")
        ON DELETE CASCADE
);

CREATE INDEX "IX_kinai_ChatMessageEntity_ConversationId"
    ON kinai."ChatMessageEntity" ("ConversationId");

-- ---------------------------------------------------------------------------
-- kinai."ChatToolCallEntity"
-- ---------------------------------------------------------------------------

CREATE TABLE kinai."ChatToolCallEntity"
(
    "Id"           UUID         NOT NULL DEFAULT gen_random_uuid(),
    "MessageId"    UUID         NOT NULL,
    "ToolName"     VARCHAR(100) NOT NULL,
    "ArgumentsJson" TEXT        NOT NULL,
    "Status"       VARCHAR(20)  NOT NULL,  -- 'Pending' | 'Confirmed' | 'Rejected'
    "CreatedAt"    TIMESTAMPTZ  NOT NULL DEFAULT now(),
    "UpdatedAt"    TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT "PK_kinai_ChatToolCallEntity" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_kinai_ChatToolCallEntity_MessageId"
        FOREIGN KEY ("MessageId") REFERENCES kinai."ChatMessageEntity" ("Id")
        ON DELETE CASCADE
);

CREATE INDEX "IX_kinai_ChatToolCallEntity_MessageId"
    ON kinai."ChatToolCallEntity" ("MessageId");

COMMIT;
