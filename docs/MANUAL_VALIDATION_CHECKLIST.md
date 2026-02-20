# Manual Validation Checklist (Revit 2025)

1. Ribbon tab `BirdTools` appears and contains `Run Clash` and `Open Pane`.
2. Dockable pane opens from both ribbon buttons.
3. Config CRUD works (create, duplicate, delete) and persists after Revit restart.
4. Model A/B selection supports host-host, host-link, and link-link configs.
5. Category pair rules can be added, removed, enabled, and disabled.
6. Manual run produces clash rows and updates metrics/status.
7. Auto mode reruns after model edits with debounce delay.
8. `Focus in 3D` selects/shows clash context.
9. `Isolate` temporarily isolates clash context in the active view.
10. `Export CSV` writes expected columns and data for current results.
