---
slug: kinlist
locale: en
title: KinList
description: Shared paginated list with stable ordering, author, essential categories, and previous/next navigation.
---

## What KinList shows

KinHub always checks authoritatively whether your profile has an active family membership. When the check succeeds, the `/kinlist` route also performs a dedicated server-side availability check for the `kinlist` service on the authorized family and then reads only visible `Active` items.

Ordering is stable: newer groups first, then the original item position inside the group. Each page shows the name, up to three categories, an optional `+N`, and the author. When a profile still has no display name, the author falls back to the accessible label **Member** and a `?` avatar.

## Moving through pages and refresh

The first page reads 50 items at a time. You can use **Back** and **Next** to move through pages without a page number or total. **Refresh** always restarts from the first page.

During refresh or navigation, the last valid page stays readable. If a page cursor is no longer valid, KinList does not show partial new data: it preserves the current view and offers a way back to the start.

## Creating the first family

Choose **Create a family** to open a form with just the name field. The name accepts 1 to 100 characters after trimming and whitespace compression, keeps valid casing and Unicode characters, and is never stored in the browser. When the request succeeds, KinHub creates the family and the creator membership together and leaves you directly in KinList.

If the request is retried or races with another one, KinHub still returns the same authoritative family context without creating duplicates.

## What is not shown

When there is no active membership, KinHub does not show members, list data, or any other shared information. Denied access also stays distinct from an empty state and does not reveal whether the service is unknown, inactive, or simply unavailable to the family.

This slice still does not include manual item creation, microphone input, category filtering, drawers, selection, or completion.

## Offline and privacy

Only the public PWA shell stays available offline. KinList does not keep items, pages, or cursors in cache, `localStorage`, `sessionStorage`, IndexedDB, or the service worker. The entered family name also stays only in memory while the page is active.
