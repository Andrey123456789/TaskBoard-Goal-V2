# TaskBoard.Web

Angular single-screen dashboard for TaskBoard v2. See the repository
[README](../../README.md) for the full local setup.

```bash
npm install
npm start          # http://localhost:4200, calls the API at http://localhost:5080
npm run build      # production build in dist/
npm test -- --watch=false
```

Source layout:

```text
src/app/
  core/        API base URL, HTTP error notification, snack-bar notifier
  shared/      field limits (CUSTOM_SETTINGS.md), validators, confirm dialog, dialog styles
  features/
    dashboard/ board page and its signal-based store
    tasks/     task API, workflow helpers, card, detail drawer, create dialog
    users/     user API and management dialog
    projects/  project API and management dialog
```
