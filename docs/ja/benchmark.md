# CSV4Unity ベンチマーク

## 目的

`CsvPerformanceBenchmark`は、CSV4Unityの変更前後で処理時間と割り当て量を比較するための開発用ベンチマークです。

既定では6列・10,000行のCSVをメモリ上で生成し、次の操作を個別に測定します。

- `CsvParser.Parse`によるCSV解析
- `CsvEnumSchema<TField>`の生成
- 型変換を含む全行走査
- 文字列列の`CsvIndex<string>`生成
- 作成済みインデックスからの検索

## Unityでの実行

1. Scene内のGameObjectへ`CsvPerformanceBenchmark`をアタッチします。
2. `Row Count`、`Measurement Iterations`、`Indexed Lookup Count`を指定します。
3. Componentのコンテキストメニューから`Run CSV4Unity Benchmark`を実行します。
4. Consoleへ出力された結果を保存します。

`Run On Start`を有効にすると、Play Mode開始時に自動実行できます。

## 測定条件

- CSV文字列の生成時間は含みません。
- `TextAsset`やファイルのストレージ読み込み時間は含みません。
- 各操作は一度ウォームアップしてから測定します。
- 測定前にGCを実行し、複数回測定した中央値を表示します。
- 割り当て量は、実行環境が対応している場合だけ`GC.GetAllocatedBytesForCurrentThread`で測定します。
- スレッド割り当てカウンターが機能しない環境では`unavailable`と表示します。
- `GC.GetTotalMemory(false)`によるManaged Heap差分も併記しますが、GCとヒープ状態の影響を受ける概算値です。
- Parseの割り当て量とManaged Heap差分には一時オブジェクトも含まれるため、Documentの保持メモリ量そのものではありません。

Editor、Mono、IL2CPP、CPU、Development Build、Profiler接続状態で結果は変わります。比較時はUnityバージョンと実行環境を揃えてください。

## ストレージを含む測定

CSV4UnityのParserは文字列を入力として受け取るため、ファイルやAddressablesなどの読み込み時間はライブラリ外の責務です。

ストレージ込みの時間を測る場合は、利用するロード方式の開始前から`CSVLoader.LoadDocument`完了後までをアプリケーション側で測定し、Parser単体の結果と分けて記録してください。
