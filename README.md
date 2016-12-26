# KoshiBridge
## 概要
Koshian/KonashiのUARTをBLEからWiFiに中継するAndroidアプリ

## ファイル
- android_server/KoshiBridge/ Android側サーバアプリ
- windows_client/KoshiBridge/ Windows側クライアント(C#クラス)
  - KoshiClient.cs   KoshiBridgeクライアントクラス
  - KoshiClientUI.cs KoshiBridgeクライアントのUIを提供するクラス

## 使い方
- Android端末にKoshiBridgeサーバアプリをインストールする。
- KoshiBridgeサーバアプリを起動する。
- アプリでKoshian/Konashiのデバイスを検索し、接続する。
- WindowsでKoshiBridgeクライアントクラスを組み込んだアプリを作成する。
- Windowsアプリを起動する。
- Android端末のIPアドレスとポート番号を指定して、接続を開く。
- Android端末を経由してWindowsアプリとKoshian/Konashiが接続される。
- WindowsアプリはKoshiBridgeクライアントクラスを介してKoshian/Konashiとシリアル通信ができる。

## スクリーンショット
### KoshiBridgeサーバアプリ
### KoshiBridgeクライアントUI

## KoshiBridgeクラスのAPI
|メソッド|説明|
|:--|:--|
|`bool open(string ipAddress, int port)`|KoshiBridgeサーバとの通信を開く。|
|`bool close()`|KoshiBridgeサーバとの通信を閉じる。|
|`bool uartBaudrate(int baudrate)`|Koshian/Konashiにボーレートを設定する。|
|`bool uartWrite(string data)`|Koshian/Konashiに文字列をUARTに送信する。|

|イベント|説明|
|:--|:--|
|`void onConnect (object sender, KoshiEventArgs args)`|サーバアプリがKoshian/Konashiに接続した時に呼ばれる。`args.data`(`string`型)にはKoshian/Konashiのデバイス名が格納される。|
|`void onDisconnect (object sender, KoshiEventArgs args)`|サーバアプリが接続中のKoshian/Konashiから接続した時に呼ばれる。|
|`void onUpdateUartRx (object sender, KoshiEventArgs args)`|サーバアプリがKoshian/KonashiからUARTで文字列を受信したときに呼ばれる。`args.data`(`string`型)には受信した文字列が格納される。|
|`void onClosed (object sender, KoshiEventArgs args)`|サーバアプリとの通信が切断されたときに呼ばれる。|

## KoshiBridgeUIクラスのAPI
|プロパティ|説明|
|:--|:--|
|`string IpAddress`|IPアドレス|
|`int PortNumber`|ポート番号|

|メソッド|説明|
|:--|:--|
|`void setKoshiClient(KoshiClient client)`|KoshiClientオブジェクトを設定する。|
