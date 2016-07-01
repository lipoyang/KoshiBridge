using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;

namespace KoshiBridge
{
#if false
    /// <summary>
    /// デバイス接続通知
    /// </summary>
    public delegate void OnConnect();

    /// <summary>
    /// デバイス切断通知
    /// </summary>
    public delegate void OnDisconnect();

    /// <summary>
    /// UART受信通知
    /// </summary>
    /// <param name="data"></param>
    public delegate void OnUpdateUartRx(string data);
#endif
    
    public class KoshiEventArgs : EventArgs
    {
        public string data;
    }
    public delegate void KoshiEventHandler(object sender, KoshiEventArgs e);

    /// <summary>
    /// KoshiBridgeクライアント
    /// </summary>
    public class KoshiClient
    {
#if false
        private OnConnect onConnect = null;
        private OnDisconnect onDisconnect = null;
        private OnUpdateUartRx onUpdateUartRx = null;
#else
        /// <summary>
        /// デバイス接続時イベント
        /// </summary>
        public event KoshiEventHandler onConnect;
        /// <summary>
        /// デバイス切断時イベント
        /// </summary>
        public event KoshiEventHandler onDisconnect;
        /// <summary>
        /// UART受信時イベント
        /// </summary>
        public event KoshiEventHandler onUpdateUartRx;
#endif
        TcpClient tcpClient;
        NetworkStream stream;
        StreamWriter writer;
        StreamReader reader;
        //Encoding encoder;
        Thread recvTread;
        bool toQuit = false;
        string deviceName = "";

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public KoshiClient()
        {
           // encoder = System.Text.Encoding.UTF8;
        }

        /// <summary>
        /// KoshiBridgeサーバとの通信を開く
        /// </summary>
        /// <param name="ipAddress">IPアドレス</param>
        /// <param name="port">ポート番号</param>
        /// <returns>成否</returns>
        public bool open(string ipAddress, int port)
        {
            deviceName = "";
            toQuit = false;

            try
            {
                tcpClient = new TcpClient(ipAddress, port);
                stream = tcpClient.GetStream();
                writer = new StreamWriter(stream);
                reader = new StreamReader(stream);

                //読み書きのタイムアウトを3秒にする
                stream.ReadTimeout = 3000;
                stream.WriteTimeout = 3000;

                // 受信スレッドを生成
                recvTread = new Thread(new ThreadStart(recvThreadFunc));
                recvTread.Start();

                // "open"を送信
                string message = "open";
                writer.WriteLine(message);
                writer.Flush();

                return true;
            }
            catch
            {
                return false;
            }

        }
        /// <summary>
        /// KoshiBridgeサーバとの通信を開く
        /// </summary>
        /// <param name="port">ポート番号</param>
        /// <returns>成否</returns>
        public bool open(int port)
        {
            return open("127.0.0.1", port);
        }

        /// <summary>
        /// KoshiBridgeサーバとの通信を閉じる
        /// </summary>
        /// <returns>成否</returns>
        public bool close()
        {
            // 受信スレッドの終了
            toQuit = true;
            recvTread.Join();
            
            // 接続を閉じる
            stream.Close();
            tcpClient.Close();
            return true;
        }

#if false
        /// <summary>
        /// KoshiBridgeサーバに接続しているデバイス情報を取得する
        /// </summary>
        /// <returns>デバイス名</returns>
        public string getPeripheralName()
        {
            string message = "getPeripheralName";

            writer.WriteLine(message);
            writer.Flush();

            // TODO
            return "";
        }
#endif

        /// <summary>
        /// UARTでデータを送信する
        /// </summary>
        /// <param name="data">送信するデータ</param>
        /// <returns>成否</returns>
        public bool uartWrite(string data)
        {
            string message = "uartWrite " + data;

            writer.WriteLine(message);
            writer.Flush();

            return true;
        }
        
        /// <summary>
        /// UARTの通信速度を設定する
        /// </summary>
        /// <param name="baudrate">ボーレート</param>
        /// <returns></returns>
        public bool uartBaudrate(int baudrate)
        {
            string message = "uartBaudrate " + baudrate.ToString();

            writer.WriteLine(message);
            writer.Flush();

            return true;
        }

#if false
        /// <summary>
        /// デバイス接続通知コールバックの登録
        /// </summary>
        /// <param name="callback">コールバック関数</param>
        public void setOnCennect(OnConnect callback)
        {
            onConnect = callback;
        }

        /// <summary>
        /// デバイス切断通知コールバックの登録
        /// </summary>
        /// <param name="callback">コールバック関数</param>
        public void setOnDisconnect(OnDisconnect callback)
        {
            onDisconnect = callback;
        }

        /// <summary>
        /// UART受信通知コールバックの登録
        /// </summary>
        /// <param name="callback">コールバック関数</param>
        public void setOnUpdateUartRx(OnUpdateUartRx callback)
        {
            onUpdateUartRx = callback;
        }
#endif
        // 受信スレッド関数
        private void recvThreadFunc()
        {
            while(!toQuit)
            {
                if (stream.DataAvailable)
                {
                    try
                    {
                        //サーバーから送られたデータを受信する
                        string data = reader.ReadLine();
                        // 受信した文字列の処理
                        executeRecvData(data);
                    }
                    catch (TimeoutException ex)
                    {
                        ; // タイムアウト
                    }
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        // 受信データの処理
        private void executeRecvData(string data)
        {
            // デバイス接続通知
            if (data.StartsWith("onCennect "))
            {
                deviceName = data.Substring(10);

                //if (onConnect != null)
                {
                    KoshiEventArgs args = new KoshiEventArgs();
                    args.data = deviceName;
                    onConnect(this, args);
                    // onConnect();
                }
            }
            // デバイス切断通知
            else if(data.StartsWith("onDisconnect"))
            {
                //if (onDisconnect != null)
                {
                    KoshiEventArgs args = new KoshiEventArgs();
                    onDisconnect(this, args);
                    //onDisconnect();
                }
            }
            // UART受信通知
            else if (data.StartsWith("onUpdateUartRx "))
            {
                //if (onUpdateUartRx != null)
                {
                    string param = data.Substring(15);

                    KoshiEventArgs args = new KoshiEventArgs();
                    args.data = param;
                    onUpdateUartRx(this, args);
                    // onUpdateUartRx(param);
                }
            }
        }
    }
}
