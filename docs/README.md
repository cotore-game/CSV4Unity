# CSV4Unity 開発ドキュメント

CSV4Unityを開発・保守する人向けの資料です。

- [日本語: コア設計](ja/architecture.md)
- [English: Core architecture](en/architecture.md)

現時点では日本語版を正本として更新します。英語版は設計が安定した段階で追従させます。

## APIリファレンス

公開APIのリファレンスは、RuntimeコードのXMLドキュメントコメントからDocFXで生成します。

- 設定: `docs/docfx.json`
- 解析用プロジェクト: `docs/CSV4Unity.Docs.csproj`
- GitHub Actions: `.github/workflows/docs.yml`
- 公開先: <https://cotore-game.github.io/CSV4Unity/>

GitHubではPR時にビルドだけを検証し、`main`へのpush、Release公開、手動実行時にGitHub Pagesへデプロイします。リポジトリのSettingsから、PagesのSourceを `GitHub Actions` に設定する必要があります。

### ローカル生成

.NET SDK 8.0以降とDocFX 2.78.5を用意し、Unity 6000.0.73f1のインストール先を指定して実行します。

```powershell
dotnet tool install --global docfx --version 2.78.5
./docs/build.ps1
```

Unityを標準以外の場所へインストールしている場合は、`Editor/Data`を指定します。

```powershell
./docs/build.ps1 -UnityEditorContents "D:\Unity\6000.0.73f1\Editor\Data"
```

生成物は `docs/_site/` に出力され、Git管理対象には含まれません。
