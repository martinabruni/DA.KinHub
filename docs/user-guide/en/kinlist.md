---
slug: kinlist
locale: en
title: KinList
description: Correct post-login routing, required onboarding, and a safe offline shell.
---

## What happens after sign-in

KinList always checks authoritatively whether your profile has an active family membership. If the check finds an active family, the PWA routes you into KinList. If the family is missing or the membership is no longer active, you only see onboarding with **Create a family** and **Join with a code**.

## What is not shown

When there is no active membership, KinHub does not show a family name, members, list data, or any other shared information. Denied access also stays distinct from an empty state.

## Offline and privacy

Only the public PWA shell stays available offline. KinList does not keep personal data in cache, does not call authenticated APIs, and does not queue remote operations.

## Current feature scope

This slice introduces access, bootstrap, and `Family` authorization. Family creation, join, and the shared list arrive in later KinList backlog features.
