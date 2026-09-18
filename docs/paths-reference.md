# `Storyboard.Foundation.Paths` reference

## Expression grammar

Portable expressions use percent-delimited environment-variable names:

```text
%STORYBOARD_SAMPLE_PROJECTS_ROOT%/WorkshopTutorial/GameRuntimeJson/WorkshopTutorial.sbr.runtime.json
```

Names begin with a letter or underscore and continue with letters, digits, or underscores. Multiple tokens are supported. Forward slashes are the canonical serialized separator; both slash forms are accepted when reading on Windows.

The resolver expands tokens before deciding whether the value is absolute, relative, or a URI. A malformed or undefined token is an error and never falls through as a relative path.

## Caller policy

`PathExpressionOptions` lets each caller decide whether absolute paths, relative paths, and file URIs are allowed. Relative paths resolve against the supplied `BaseDirectory`; if no base is supplied, the process current directory is used unless `RequireBaseDirectoryForRelativePaths` is enabled. The resolver does not impose containment rules on callers.

Non-file URI schemes are unsupported. File URIs require `AllowFileUris = true`.

## Diagnostics

Failures return a structured `PathExpressionResolutionResult` with the raw expression, expanded expression, diagnostic code/message, and no physical path. In particular, `UnresolvedVariable` is safe to surface to a user or configuration log without attempting filesystem access.

## Asset-root helpers

`AssetRootMatcher.FindContainingRoots` returns all configured roots containing a file, ordered by deepest root first and then by variable name using ordinal ordering. Callers should select the first match when the deepest match is unique, and treat equal-depth matches as ambiguous. The helper does not choose between equal-depth roots.
