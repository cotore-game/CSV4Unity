# Contributing to CSV4Unity

Thank you for helping improve CSV4Unity. Bug reports, documentation fixes, and focused pull requests are welcome.

## Contents

- [Report an issue](#report-an-issue)
- [Set up the project](#set-up-the-project)
- [Make a change](#make-a-change)
- [Run tests](#run-tests)
- [Documentation](#documentation)
- [Submit a pull request](#submit-a-pull-request)

## Report an issue

Use [GitHub Issues](https://github.com/cotore-game/CSV4Unity/issues). For a bug, include the Unity version, CSV4Unity tag or commit, a minimal CSV and code sample, and the expected and actual behavior. Remove private data from examples and logs.

For a substantial feature or API change, open an issue to discuss the use case before implementing it. CSV4Unity is pre-1.0, so APIs may change; explain any compatibility impact.

## Set up the project

1. Fork the repository if you do not have write access, then clone your fork.
2. Open the repository root as a Unity project using **Unity 6000.0.73f1**.
3. Create a topic branch from the latest `main`, for example `docs/improve-guide` or `fix/header-mapping`.

The UPM package lives in `Assets/Plugins/CSVLoader`. Examples, development tests, and fixtures are outside that package. See the [development documentation](docs/README.md) for the layout.

## Make a change

- Keep a pull request focused on one problem.
- Keep the Runtime assembly independent of Unity Editor APIs. Preserve the boundaries described in the [architecture notes](docs/en/architecture.md).
- Use English API identifiers and English sample data and code examples.
- Existing public XML comments and implementation comments use Japanese; follow the surrounding convention. The English README is the primary public entry point, with Japanese documentation linked separately.
- Add or adjust regression tests for behavior changes. Documentation-only changes do not require new tests.
- Do not commit `Library`, generated API pages, test output, credentials, or personal Editor settings. Include matching `.meta` files when adding, moving, or deleting Unity assets.

## Run tests

Open **Window > General > Test Runner**, select **EditMode**, and run all tests. Report the result in your PR. If you cannot run Unity locally, state that limitation.

The EditMode assembly lives in `Assets/Scripts/Tests/EditMode`. Manual checks live in `Assets/Scripts/Tests/Manual`; use the relevant CSV fixtures in `Assets/TestData/CSV4Unity` when checking Inspector or Viewer changes.

Every PR targeting `main` starts the Unity test workflow, and `EditMode tests` must pass before merging. Fork PRs do not receive repository secrets, so the licensed Unity job may need maintainer assistance. Do not work around this by running untrusted PR code with `pull_request_target` or exposing credentials. Contributors do not need to configure the upstream repository's secrets.

## Documentation

- Keep the root README concise; put detailed usage in `docs/en/guide.md` and `docs/ja/guide.md`.
- Update both languages for changed behavior when possible; mention translation gaps in the PR.
- Use English inside code fences, including comments and sample values, in both language versions.
- Link generated API documentation to the [published API site](https://cotore-game.github.io/CSV4Unity/api/CSV4Unity.html). Generated HTML is not tracked in this repository.
- Follow the [local build instructions](docs/README.md#build-the-api-site) when changing DocFX configuration or public XML comments. Report an unavailable local build in the PR.

## Submit a pull request

Target `main`. Explain the problem, the resulting behavior, relevant issue links, and how you checked the change. Include screenshots for Inspector or Viewer changes when useful.

`main` requires a PR, passing EditMode tests, an up-to-date branch, and resolved review conversations. The maintainer reviews contributions. The repository administrator can bypass the approval requirement on their own PRs, but cannot bypass the required test rule. Both `main` and `develop` are protected from deletion and force pushes.

By contributing, you agree that your contributions are provided under the project's [MIT license](LICENSE).
