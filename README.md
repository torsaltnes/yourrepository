```md
# Vaktliste

En enkel SPA for vaktplanlegging. Modulen lar deg legge ansatte på morgen- og ettermiddagsvakter, hindrer dobbelbooking av samme dato/vakt og viser planen i en tabell med et ryddig Tailwind-basert grensesnitt.

## Teknisk stack
- Vite 6 + TypeScript
- Tailwind CSS 4 via `@tailwindcss/vite`
- Vitest + jsdom for testene
- Native DOM-APIer og hash-basert routing
- Lagring i en `sessionStorage`-basert repository med in-memory snapshot i appen

## Installasjon
1. Installer avhengigheter:
   ```bash
   npm install
   ```

2. Start utviklingsserver:
   ```bash
   npm run dev
   ```

3. Kjør tester:
   ```bash
   npm test
   ```

4. Bygg for produksjon:
   ```bash
   npm run build
   ```

På macOS/Linux kan du også bruke `./bootstrap.sh`, og på Windows `.\bootstrap.ps1`.

## API-oversikt
- `createApp(rootEl)` – monterer app-shell og starter hash-routeren.
- `Router` – velger mellom oversikt, vaktliste og 404-visning.
- `parseShiftDate(raw)` – validerer datoer på formatet `YYYY-MM-DD`.
- `scheduleAssignment(state, command)` – ren domenefunksjon som blokkerer to ansatte på samme `dato + vakt`.
- `createRepository()` – lager en `sessionStorage`-basert repository for planstate.
- `createVaktlisteStore()` – eksponerer snapshot, submit-status og mutasjoner for UI-et.
- `createOverviewPage()`, `createVaktlistePage()`, `createNotFoundPage()` – bygger sidene som routeren viser.
```