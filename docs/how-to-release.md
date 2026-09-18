# How to release `Storyboard.Foundation`

Releases are tag-driven. A normal push to `main` runs CI only. Pushing a numeric Git tag such as `0.1.1` starts the release workflow, which restores, builds, tests, packs the library, publishes the package to GitHub Packages, and creates a GitHub Release with the `.nupkg` attached.

The release workflow publishes to the existing Storyboard GitHub Packages NuGet feed using the workflow’s `GITHUB_TOKEN`; no `NUGET_API_KEY` secret is required.

For the fast path, run this from a clean checkout on `main`:

```powershell
.\scripts\Release.ps1 -Version 0.1.1
```

The script performs the version update, validation, commit, annotated tag, and atomic push automatically. The detailed steps below explain what it is doing.

## 1. Update the project version

Edit `src/Storyboard.Foundation/Storyboard.Foundation.csproj`:

```xml
<Version>0.1.1</Version>
```

The release tag must exactly match this value. The workflow rejects the release before packaging if they differ.

## 2. Validate locally

```powershell
dotnet restore tests/Storyboard.Foundation.Tests/Storyboard.Foundation.Tests.csproj --configfile NuGet.Config
dotnet build src/Storyboard.Foundation/Storyboard.Foundation.csproj --configuration Release --no-restore
dotnet build tests/Storyboard.Foundation.Tests/Storyboard.Foundation.Tests.csproj --configuration Release --no-restore
dotnet run --project tests/Storyboard.Foundation.Tests/Storyboard.Foundation.Tests.csproj --configuration Release --no-build
dotnet pack src/Storyboard.Foundation/Storyboard.Foundation.csproj --configuration Release --no-build --no-restore --output artifacts
```

Confirm that the package is named `artifacts/Storyboard.Foundation.0.1.1.nupkg`.

## 3. Commit and tag

```powershell
git add src/Storyboard.Foundation/Storyboard.Foundation.csproj
git commit -m "Prepare 0.1.1 release"
git tag -a 0.1.1 -m "Release 0.1.1"
git show --stat --oneline 0.1.1
git describe --exact-match --tags HEAD
```

The final command should print `0.1.1`.

## 4. Push the branch and tag

Because the tag is annotated and points to the commit on `main`, Git can discover it automatically:

```powershell
git push --atomic --follow-tags origin main
```

If the remote does not support atomic pushes:

```powershell
git push --follow-tags origin main
```

`--follow-tags` pushes annotated tags reachable from the branch being pushed. Pushing the tag is what starts the release workflow; creating it locally does not.

## 5. Watch the release

In GitHub’s **Actions** tab, select **Release**. It will:

1. Check out the tagged commit.
2. Confirm the tag is numeric SemVer.
3. Confirm the tag equals the project `<Version>`.
4. Restore, build, and run tests.
5. Create the NuGet package.
6. Publish the package to GitHub Packages.
7. Upload the package as a workflow artifact.
8. Create the GitHub Release and attach the package.

If build or test fails, nothing is published. If GitHub Packages accepts the package but GitHub Release creation fails afterward, rerun the workflow; `--skip-duplicate` makes the already-published package safe to retry.

## Failed release recovery

Fix the problem in a new commit and use a new version. Do not force-move a published tag.

```powershell
git commit -am "Fix release packaging"
git push origin main
git tag -a 0.1.2 -m "Release 0.1.2"
git push --follow-tags origin main
```

After GitHub Packages finishes indexing the package, consumers can reference it with:

```xml
<PackageReference Include="Storyboard.Foundation" Version="0.1.1" />
```
