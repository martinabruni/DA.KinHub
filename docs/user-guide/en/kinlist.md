---
slug: kinlist
locale: en
title: KinList
description: Correct post-login routing, required onboarding, and a safe offline shell.
---

## What happens after sign-in

KinHub always checks authoritatively whether your profile has an active family membership. If the check finds an active family, the `/kinlist` route also performs a dedicated server-side availability check for the `kinlist` service on the authorized family. If the family is missing or the membership is no longer active, you stay on `/kinlist` and only see KinHub onboarding.

## Creating the first family

Choose **Create a family** to open a form with just the name field. The name accepts 1 to 100 characters after trimming and whitespace compression, keeps valid casing and Unicode characters, and is never stored in the browser. When the request succeeds, KinHub creates the family and the creator membership together and leaves you directly in KinList.

If the request is retried or races with another one, KinHub still returns the same authoritative family context without creating duplicates.

## What is not shown

When there is no active membership, KinHub does not show members, list data, or any other shared information. Denied access also stays distinct from an empty state and does not reveal whether the service is unknown, inactive, or simply unavailable to the family.

## Offline and privacy

Only the public PWA shell stays available offline. KinList does not keep personal data in cache, does not call authenticated APIs, and does not queue remote operations. The entered family name also stays only in memory while the page is active.

## Current feature scope

This slice connects KinList to KinHub's shared bootstrap, enables atomic first-family creation, and uses the new persisted family-service catalog. Join by code and the shared list arrive in later features.
