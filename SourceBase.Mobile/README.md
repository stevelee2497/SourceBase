# Jupiter — SourceBase Mobile

React Native (Expo) client for the [SourceBase](https://github.com/stevelee2497/SourceBase)
API. User-role app with five tabs: **Home, Wallets, Habits, Todos, Profile**. UI follows
Money Lover (wallets/transactions) and Microsoft To Do (todos).

## Stack

- Expo SDK 52 + expo-router (file-based, typed routes)
- TanStack Query (server state, infinite scroll, optimistic updates, offline persistence)
- expo-secure-store (tokens) · axios (interceptor-based refresh-retry)
- react-hook-form + zod · @gorhom/bottom-sheet · @shopify/flash-list
- react-native-gifted-charts (summary donut)

## Getting started

```bash
cd SourceBase.Mobile
npm install
cp .env.example .env        # set EXPO_PUBLIC_API_BASE_URL
npx expo start
```

The app expects the SourceBase API reachable over HTTPS at `EXPO_PUBLIC_API_BASE_URL`
(defaults to `https://api.quoctran.qzz.io`). The mobile client calls `/api/*` directly,
so the API must be publicly reachable and CORS must permit the app.

## Project structure

```
app/                       expo-router routes
  (auth)/                  login, register, forgot/reset password, confirm email
  (tabs)/                  tab navigator + screens
    index.tsx              Home (dashboard)
    wallets/               list + [id] detail (Stack)
    habits/                habit log timeline
    todos/                 tasks
    profile/               account
src/
  api/                     one module per feature (typed against the C# records)
  auth/                    token store + AuthContext + route guard
  components/              TransactionSheet, SummaryView, LogHabitSheet, shared UI
  hooks/                   react-query hooks per feature
  models/                  TS types + enums mirroring the API
  theme/                   colors, spacing, radius
  utils/                   money/date formatting, query keys, habit action meta
```

## API conventions

- Bearer auth; on 401 the axios interceptor refreshes once (single-flight) and retries,
  else signs out.
- Lists are paginated: `{ items, page, limit, total }`.
- `DateOnly` fields are `yyyy-MM-dd`; habit-log `DateTime` is ISO 8601.
- Updates are PATCH with partial semantics (only send changed fields).
- Enums travel as strings (`Income`/`Expense`, `Open`/`Completed`/`Archived`,
  `HabitStarted`/`Dismissed`/`Snoozed`/`SuppressedVideo`).
- Habit-log creation is batch-only: `POST /habit-logs { entries: [...] }`.

## To verify against `/swagger`

A few request bodies are named from the Blazor client and should be confirmed:
register / forgot-password / reset-password / confirm-email payloads, wallet + todo-list
create bodies, and the avatar `upload-url` route/response field names.

## Build (CI → APK)

`.github/workflows/mobile-publish.yml` mirrors the desktop pipeline. On every push to
`main` touching `SourceBase.Mobile/**` (or manual dispatch) it runs three jobs:

1. **version** — auto-increments from the latest `mobile-v1.x` git tag (first run = `1.0`).
2. **build** — installs deps, typechecks, `expo prebuild` to generate the native Android
   project, sets `versionName`/`versionCode`, runs `gradlew assembleRelease`, then aligns
   and signs the APK with a generated keystore. Produces `Jupiter-v<version>.apk`.
3. **release** — creates a GitHub Release tagged `mobile-v<version>` with the APK attached.

The APK is signed with an ephemeral CI keystore (fine for internal distribution). For a
stable signing identity or Play Store upload, replace the "Generate signing keystore"
step with a committed keystore restored from a secret, or switch to EAS Build.

## Build (EAS, alternative)

```bash
eas build -p android --profile preview
eas build -p ios --profile preview
eas update --branch production
```

App name is **Jupiter** (`app.config.ts`).
