---
slug: family
locale: en
title: Family
description: Review the family name, active members, and active invitations without exposing secret codes.
---

## Family name

The **Family** page shows the current name of the authorized family. The route is rebuildable (`/settings/family`) and reloads data from the server whenever the account, family, or session changes.

## Active members

The Members section reads limited pages of active memberships. Each row shows only the minimum name and initials. If a profile does not have an approved name, KinHub uses the **Member** fallback and the avatar shows `?`.

## Active invitations

The Active invitations section shows only the creator, creation date, expiration date, and active status. The secret code and its fingerprint never appear on this page.

## Paging and recovery

Members and invitations use independent opaque cursors that are not stored in the browser. If a cursor is no longer valid, you can restart only the affected section from the beginning. If the family has no active invitations, the page shows a dedicated empty state.

## Online requirement

The Family page requires an active connection and a valid session. KinHub does not store the name, members, invitations, or cursors in local browser storage, so data is not available offline.
