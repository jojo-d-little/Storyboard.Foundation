# How to release `Storyboard.Foundation`

Releases are tag-driven. A normal push to `main` runs CI only. Pushing a numeric Git tag such as `0.1.1` starts the release workflow, which restores, builds, tests, packs the library, and creates a GitHub Release with the `.nupkg` attached.

## 1. Update the project version

Edit `src/Storyboard.Foundation/Storyboard.Foundation.csproj`:

```xml
<Version>0.1.1</Version>
```

The release tag must exactly match this value. The workflow rejects the release before packaging if they differ.

Use a new version for every published release. Do not reuse or move a published version tag.

## 2. Validate locally

From the repository root:

```powershell
dotnet restore tests/Storyboard.Foundation.Tests/Storyboard.Foundation.Tests.csproj --configfile NuGet.Config
dotnet build src/Storyboard.Foundation/Storyboard.Foundation.csproj --configuration Release --no-restore
dotnet build tests/Storyboard.Foundation.Tests/Storyboard.Foundation.Tests.csproj --configuration Release --no-restore
dotnet run --project tests/Storyboard.Foundation.Tests/Storyboard.Foundation.Tests.csproj --configuration Release --no-build
dotnet pack src/Storyboard.Foundation/Storyboard.Foundation.csproj --configuration Release --no-build --no-restore --output artifacts
```

Confirm that the package is named `artifacts/Storyboard.Foundation.0.1.1.nupkg`.

## 3. Commit the version change

```powershell
git add src/Storyboard.Foundation/Storyboard.Foundation.csproj
git commit -m "Prepare 0.1.1 release"
```

## 4. Create the release tag

Create an annotated tag on the release commit:

```powershell
git tag -a 0.1.1 -m "Release 0.1.1"
```

Check the tag before pushing:

```powershell
git show --stat --oneline 0.1.1
git describe --exact-match --tags HEAD
```

The second command should print `0.1.1`.

## 5. Push the branch and tag

Push both refs together:

```powershell
git push --atomic origin main 0.1.1
```

If the remote does not support atomic pushes, use:

```powershell
git push origin main 0.1.1
```

Pushing the tag is what starts the release workflow. Creating the tag locally does not start it.

## 6. Watch the release

On GitHub, open the repository’s **Actions** tab and select the **Release** workflow. It will:

1. Check out the tagged commit.
2. Confirm the tag is numeric SemVer.
3. Confirm the tag equals the project `<Version>`.
4. Restore, build, and run tests.
5. Create the NuGet package.
6. Upload the package as a workflow artifact.
7. Create the GitHub Release and attach the package.

If any step fails, no GitHub Release is created.

## Failed release recovery

Fix the problem in a new commit and use a new version. Do not force-move a tag that has already been published.

For example, if `0.1.1` failed:

```powershell
git commit -am "Fix release packaging"
git push origin main
git tag -a 0.1.2 -m "Release 0.1.2"
git push origin 0.1.2
```

The package is currently attached to the GitHub Release. Publishing to an external NuGet feed can be added as a protected release-workflow step once the target feed and credentials are chosen.
