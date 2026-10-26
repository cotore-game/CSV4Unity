# Development documentation

For installation and usage, start with the [README](../README.md) or the [user guide](en/guide.md). This page covers development and maintenance.

## Contents

- [Repository layout](#repository-layout)
- [Language policy](#language-policy)
- [Tests and CI](#tests-and-ci)
- [Build the API site](#build-the-api-site)

## Repository layout

| Path | Purpose |
|---|---|
| `Assets/Plugins/CSVLoader/Runtime` | Runtime API and CSV core |
| `Assets/Plugins/CSVLoader/Editor` | Inspector, encoding tools, and Viewer |
| `Assets/Scripts/Examples` | Usage examples |
| `Assets/Scripts/Tests/EditMode` | Automated development tests |
| `Assets/Scripts/Tests/Manual` | Manual checks |
| `Assets/TestData/CSV4Unity` | CSV fixtures |
| `docs/en`, `docs/ja` | User guides and architecture notes |

Only `Assets/Plugins/CSVLoader` is installed by the UPM Git URL. Development tests and fixtures are not included in that package.

See [CONTRIBUTING.md](../CONTRIBUTING.md) for the contribution process and [core architecture](en/architecture.md) for design boundaries.

## Language policy

- English is the main README and documentation entry language; Japanese pages are linked separately.
- Public API identifiers, code examples, comments inside examples, and sample values use English.
- Public API XML comments remain in Japanese for now. This README reorganization does not translate Runtime source comments.
- Keep existing implementation comment conventions. Place long examples in the user guides instead of duplicating them in XML comments.

The DocFX-specific project treats missing public XML comments, invalid parameter tags, malformed XML, and invalid `cref` references as build errors.

## Tests and CI

Run EditMode tests using **Window > General > Test Runner** in Unity **6000.0.73f1**.

- `.github/workflows/unity-tests.yml` runs on every PR targeting `main`, relevant file pushes to `main`, and manual dispatch. `EditMode tests` is required for merging.
- `.github/workflows/docs.yml` builds on relevant file PRs targeting `main`, relevant file pushes to `main` or `develop`, and manual dispatch. Deployment runs only on pushes to `main`.
- Documentation builds use .NET 8 and DocFX 2.78.5 in the configured Unity container.

Maintainers configure the Unity test job with `UNITY_LICENSE` or `UNITY_SERIAL`, plus `UNITY_EMAIL` and `UNITY_PASSWORD`, in repository secrets. See the [GameCI activation instructions](https://game.ci/docs/github/activation/). Never commit these values. Fork PRs cannot access upstream secrets and need maintainer assistance for licensed test execution.

The workflow uploads available test XML and Editor logs as `unity-editmode-test-results`, including on failure. Missing output is reported as a warning rather than masking the original failure.

## Build the API site

Install .NET SDK 8.0 or later and DocFX 2.78.5. With Unity 6000.0.73f1 installed at the default location, run from the repository root:

```powershell
dotnet tool install --global docfx --version 2.78.5
./docs/build.ps1
```

For a custom Unity installation, specify the `Editor/Data` directory:

```powershell
./docs/build.ps1 -UnityEditorContents "D:/Unity/6000.0.73f1/Editor/Data"
```

Configuration is in `docs/docfx.json`; `docs/CSV4Unity.Docs.csproj` exposes Runtime sources to DocFX. Generated metadata in `docs/api/` and HTML in `docs/_site/` are ignored by Git. Do not commit generated pages.

The published API is at [GitHub Pages](https://cotore-game.github.io/CSV4Unity/api/CSV4Unity.html). Markdown pages read directly on GitHub must link there, rather than to an untracked `docs/api/*.html` file. The DocFX `toc.yml` uses a relative HTML path within the generated site.

Repository Pages settings must use **GitHub Actions** as the source for deployment.
