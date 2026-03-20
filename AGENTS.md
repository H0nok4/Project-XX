# Project-XX Agent Notes

## Runtime UI Rules

When a task involves runtime UI, read `Docs/UiProductionStandard.md` first and follow it as the source of truth.

Mandatory rules for new runtime UI work:

- New runtime UI must be authored as `UGUI prefabs`.
- Place runtime UI prefabs under `Assets/Resources/UI/...`.
- Use `ViewBase` / `WindowBase` for lifecycle and `*Template` scripts for prefab references.
- Instantiate prefabs and bind data/events in code; do not build full gameplay UI hierarchies in code at runtime.
- Do not add new runtime IMGUI implementations such as `OnGUI`, `GUI`, or `GUILayout`.
- Do not add new runtime-built UGUI hierarchies with `new GameObject`, `AddComponent<Text/Image/Button/InputField/ScrollRect/...>`, or large `PrototypeUiToolkit.Create*` layout construction.
- Mount runtime UI under `PrototypeRuntimeUiManager` layer roots instead of creating extra screen-space canvases per feature.

Legacy runtime-built UI may still exist in the project, but it is transitional technical debt and must not be used as the pattern for new features.

If a task significantly changes an old runtime-built UI screen, prefer converting it to the prefab-based UGUI workflow during that task instead of expanding the old pattern.

Editor-only tooling under `Assets/**/Editor` may still use IMGUI. This exception does not apply to gameplay/runtime UI.
