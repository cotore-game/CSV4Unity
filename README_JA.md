# CSV4Unity

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Version](https://img.shields.io/badge/version-0.3.0-brightgreen)](https://github.com/cotore-game/CSV4Unity/releases/tag/v0.3.0)

Enumによる列指定、条件付きValidation、Unity Editor用Viewerを備えたCSVライブラリです。

行を固定クラスへ一括変換せず、必要なセルだけ型変換します。ADVシナリオのように、同じ引数列の型や意味が行ごとに変わるデータを扱えます。

[English](README.md)

> [!IMPORTANT]
> 現在は1.0未満です。APIは変更される可能性があるため、導入時はタグまたはコミットを固定してください。

## 目次

- [インストール](#インストール)
- [使い始める](#使い始める)
- [主な機能](#主な機能)
- [ドキュメント](#ドキュメント)
- [コントリビュート](#コントリビュート)
- [ライセンス](#ライセンス)

## インストール

Unity Package Managerの **Add package from git URL...** に指定します。

```text
https://github.com/cotore-game/CSV4Unity.git?path=/Assets/Plugins/CSVLoader#v0.3.0
```

パッケージの指定はUnity 6000.0以降です。開発・テストにはUnity **6000.0.73f1**を使用しています。

## 使い始める

[英語版READMEの最小例](README.md#quick-start)で、CSVの準備からMonoBehaviourでの取得まで確認できます。詳細な使い方は[日本語利用ガイド](docs/ja/guide.md)を参照してください。行番号はヘッダーを除いて0から始まり、値の型変換は実行時に行います。

## 主な機能

- RFC 4180形式のクォート、カンマ、二重引用符、セル内改行の解析
- Enum・ヘッダー名・列番号による行、列、セルアクセスとヘッダー名補正
- 明示的な型変換と検索インデックス
- Enum属性によるValidationと、行条件による制約の適用
- Inspectorでのスキーマ選択、Validation、文字コード検査、UTF-8変換と復元
- 検索・ズーム・コピーに対応した読み取り専用Viewer

読み込みとValidationは別々に実行します。CSV編集とCSV間の外部キー検証は未対応です。

## ドキュメント

- [日本語利用ガイド](docs/ja/guide.md)
- [APIリファレンス](https://cotore-game.github.io/CSV4Unity/api/CSV4Unity.html)
- [開発者向けコア設計](docs/ja/architecture.md)
- [開発・保守の手順（英語）](docs/README.md)
- [リリースノート](https://github.com/cotore-game/CSV4Unity/releases)

## コントリビュート

不具合報告やPull Requestを歓迎します。環境構築、テスト、PRの手順は[CONTRIBUTING.md](CONTRIBUTING.md)を参照してください。

## ライセンス

[MIT](LICENSE)
