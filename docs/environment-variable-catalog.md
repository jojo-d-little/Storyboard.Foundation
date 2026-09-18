# Storyboard environment-variable catalog

The portability package understands any valid `%NAME%` process environment variable. Product-owned variables are documented by their owning subsystem. The initial catalog is:

| Variable | Scope | Status |
| --- | --- | --- |
| `STORYBOARD_DEVELOPMENT_ROOT` | local source workspace | Existing, development-only |
| `STORYBOARD_SAMPLE_PROJECTS_ROOT` | sample-project collection | Existing |
| `STORYBOARD_WEBPORTAL_ROOT` | static WebPortal distribution | Existing |
| `STORYBOARD_ASSET_SOURCE_ROOT` | primary authoring asset root | Existing, canonical |
| `STORYBOARD_ASSET_SOURCE_ROOT_<NAME>` | additional named authoring asset root | New |
| `STORYBOARD_RUNTIME_GAME_DISCOVERY_JSON_PATH` | explicit discovery registration override | Existing runtime override |
| `STORYBOARD_RUNTIME_IDENTITY_JSON_PATH` | explicit identity JSON override | Existing runtime override |

This package defines expansion and diagnostics; it does not define which product configuration boundaries consume a variable.
