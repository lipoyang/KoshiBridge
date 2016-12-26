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
        /// <summary>
        /// ネットワーク切断時イベント
        /// </summary>
        public event KoshiEventHandler onClosed;


        TcpClient tcpClient;
        NetworkStream stream;
        StreamWriter writer;
        StreamReader reader;
        //Encoding encoder;
        Thread recvTread;
        bool toQuit = false;
        bool isOpen = false;
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
            }
            catch
            {
                if (stream != null) stream.Close();
                if(tcpClient!=null) tcpClient.Close();
                return false;
            }
            // 受信スレッドを生成
            recvTread = new Thread(new ThreadStart(recvThreadFunc));
            recvTread.Start();

            isOpen = true;

            // "open"を送信
            string message = "open";
            writer.WriteLine(message);
            writer.Flush();

            return true;
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
            isOpen = false;

            // 受信スレッドの終了
            toQuit = true;
            recvTread.Join();
            
            // 接続を閉じる
            stream.Close();
            tcpClient.Close();
            return true;
        }

        /// <summary>
        /// UARTでデータを送信する
        /// </summary>
        /// <param name="data">送信するデータ</param>
        /// <returns>成否</returns>
        public bool uartWrite(string data)
        {
            if (!isOpen) return false;

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
            if (!isOpen) return false;

            string message = "uartBaudrate " + baudrate.ToString();

            writer.WriteLine(message);
            writer.Flush();

            return true;
        }

        // 受信スレッド関数
        private void recvThreadFunc()
        {
            while(!toQuit)
            {
                //if (stream.DataAvailable)
                //{
                    try
                    {
                        //サーバーから送られたデータを受信する
                        string data = reader.ReadLine();
                        if (data == null)
                        {
                            isOpen = false;

                            stream.Close();
                            tcpClient.Close();
                            KoshiEventArgs args = new KoshiEventArgs();
                            onClosed(this, args);
                            break;
                        }
                        // 受信した文字列の処理
                        executeRecvData(data);
                    }
                    catch (IOException ex)// TimeoutException ex)
                    {
                        ; // タイムアウト
                    }
                //}
                //else
                //{
                //    Thread.Sleep(1);
                //}
            }
        }

        // 受信データの処理
        private void executeRecvData(string data)
        {
            // デバイス接続通知
            if (data.StartsWith("onConnect "))
            {
                deviceName = data.Substring(10);
                
                KoshiEventArgs args = new KoshiEventArgs();
                args.data = deviceName;
                onConnect(this, args);
            }
            // デバイス切断通知
            else if(data.StartsWith("onDisconnect"))
            {
                KoshiEventArgs args = new KoshiEventArgs();
                onDisconnect(this, args);
            }
            // UART受信通知
            else if (data.StartsWith("onUpdateUartRx "))
            {
                string param = data.Substring(15);
                
                KoshiEventArgs args = new KoshiEventArgs();
                args.data = param;
                onUpdateUartRx(this, args);
            }
        }
    }
}
