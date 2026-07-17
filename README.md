# Unity_TemplateRepo
Unity6 テンプレートリポジトリ
Windows ビルド・インストーラー作成・WebGL デプロイ・Discord 通知を自動化する Actions つきです。

## ディレクトリ構成

```
.github/
├── project.yml                    # プロジェクト設定（ここだけ編集してください）
├── index/
│   ├── meta.yml                   # WebGL PagesのOGP設定
│   └── icon.png                   # OGP・favicon用画像
├── inno/
│   └── installer.iss.template     # Inno Setupスクリプトテンプレート
├── scripts/
│   ├── cleanup-build.ps1          # ビルド成果物の不要フォルダ削除
│   ├── generate-iss.ps1           # .issファイル生成
│   └── inject-ogp.py              # index.htmlへのOGP タグ注入
└── workflows/
    ├── release.yml                # メインワークフロー
    ├── build-unity.yml            # Unityビルド（再利用）
    ├── create-installer.yml       # Inno Setupインストーラー作成（再利用）
    ├── deploy-webgl.yml           # GitHub Pagesデプロイ（再利用）
    └── notify-discord.yml         # Discord 通知（再利用）
```

---

## セットアップ

### 1. プロジェクト設定

`.github/project.yml` を編集します。**このファイルだけ触れば基本的な設定は完了します。**

```yaml
# 表示名（スタートメニュー・通知）日本語可
app_name: "My Game"
# 実行ファイル名（.exe なし・ASCII文字のみ）
app_exe_name: "MyGame"
# インストール先フォルダ名・レジストリキー（ASCII文字のみ）
app_id: "MyGame"
# パブリッシャー名
app_publisher: "your-org"
# 要件
system_requirements: "Windows 10 / 11 (64-bit)"
# Discord 通知の見出し
workflow_display_name: "My Game"
```

> `app_name` には日本語を使えます。インストーラーのスタートメニュー・ショートカット表示名に使われます（例：原神 → `原神`、実行ファイルは `Genshin.exe`）。
> `app_exe_name` は ASCII のみにしてください。

### 2. GitHub Secrets の設定

リポジトリの Settings → Secrets and variables → Actions に以下を追加します。

| Secret 名 | 必須 | 内容 |
|---|---|---|
| `UNITY_EMAIL` | ◯ | Unity アカウントのメールアドレス |
| `UNITY_PASSWORD` | ◯ | Unity アカウントのパスワード |
| `UNITY_LICENSE` | ◯ | Unity ライセンスファイルの内容（`.ulf` の中身） |
| `DISCORD_WEBHOOK_URL` | － | Discord の Webhook URL（未設定なら通知をスキップ） |

### 3. GitHub Pages の有効化（WebGL デプロイを使う場合）

リポジトリの Settings → Pages → Source を **GitHub Actions** に設定します。

### 4. OGP 設定（WebGL デプロイを使う場合）

`.github/index/meta.yml` を編集します。

```yaml
enabled: true
title: "ゲームタイトル"
description: "ゲームの説明文"
icon: true
icon_file: "icon.png" # .github/index/ に画像を置く
# theme_color: "#1a1a2e" # ブラウザのテーマカラー（任意）
```

`meta.yml` が存在しない、または `enabled: false` の場合は OGP 注入をスキップします。

---

## 使い方

### タグをプッシュして自動リリース

`v` から始まるタグをプッシュすると、Windows・WebGL 両方をビルドして GitHub Release を作成し、GitHub Pages にデプロイします。

```bash
git tag v1.0.0
git push origin v1.0.0
```

タグ名に `alpha` / `beta` / `rc` が含まれる場合は自動的に Pre-release 扱いになります。

```bash
git tag v1.0.0-beta
git push origin v1.0.0-beta
```

### 手動実行（workflow_dispatch）

Actions タブ → **Build & Release** → **Run workflow** から実行できます。ビルドターゲットやリリース設定を個別に選択できます。

| 項目 | 説明 |
|---|---|
| Version | バージョン番号（例：`1.0.0`） |
| Build for Windows | Windows ビルドを行うか |
| Build for WebGL | WebGL ビルドを行うか |
| Create GitHub Release | GitHub Release を作成するか（Windows ビルド必須） |
| Deploy WebGL to GitHub Pages | GitHub Pages にデプロイするか（WebGL ビルド必須） |
| Create as draft release | ドラフトとして作成するか |
| Mark as pre-release | Pre-release としてマークするか |

---

## リリース成果物

Windows ビルド時に以下が生成されます。

| ファイル | 内容 |
|---|---|
| `{app_exe_name}-Setup-v{version}.exe` | Inno Setup インストーラー（推奨） |
| `{app_exe_name}-v{version}-Windows.zip` | ポータブル版（ZIP） |

---

## 注意事項

### Unity の Build Name

`project.yml` の `app_exe_name` は Unity プロジェクトの **Build Name**（Game-CI の `buildName` オプション）と一致させてください。Unity エディターの Build Settings の Product Name ではありません。

### cleanup-build.ps1

`Build & Release` ワークフローのインストーラー作成前に、開発用途のみのフォルダ（`DoNotShip` など）を削除するスクリプトです。必要に応じて削除対象を追加してください。
