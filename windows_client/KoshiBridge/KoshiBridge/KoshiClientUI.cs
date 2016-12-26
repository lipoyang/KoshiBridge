using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KoshiBridge
{
    public partial class KoshiClientUI : UserControl
    {
        KoshiClient koshiClient = null;
        bool isOpen = false;

        /// <summary>
        /// IPアドレス
        /// </summary>
        [Browsable(true)]
        public string IpAddress
        {
            get
            {
                return textIpAddr.Text;
            }
            set
            {
                textIpAddr.Text = value;
            }
        }

        /// <summary>
        /// ポート番号
        /// </summary>
        [Browsable(true)]
        public int PortNumber
        {
            get
            {
                int val = 0;
                int.TryParse(textPort.Text, out val);
                return val;
            }
            set
            {
                textPort.Text = value.ToString();
            }
        }

        public KoshiClientUI()
        {
            InitializeComponent();
        }

        /// <summary>
        /// KoshiClientを設定する
        /// </summary>
        /// <param name="client">KoshiClient</param>
        public void setKoshiClient(KoshiClient client)
        {
            koshiClient = client;

            client.onConnect += onConnect;
            client.onDisconnect += onDisconnect;
            client.onClosed += onClosed;
        }

        private bool isConnecting = false;
        private async void buttonOpen_Click(object sender, EventArgs e)
        {
            if (koshiClient == null) return;

            // 開いてたら閉じる
            if(isOpen)
            {
                koshiClient.close();

                isOpen = false;

                buttonOpen.Text = "開く";
                textStatus.Text = "未接続";

            }
            // 閉じていたら開く
            else
            {
                if (isConnecting) return;
                isConnecting = true;

                string ipAddr = textIpAddr.Text;
                int port = 0;
                if (!int.TryParse(textPort.Text, out port))
                {
                    MessageBox.Show("ポート番号が無効です");
                }

                bool ret = false;
                await Task.Run(() =>
                {
                    ret = koshiClient.open(ipAddr, port);
                });

                if (ret)
                {
                    isOpen = true;

                    buttonOpen.Text = "閉じる";
                    textStatus.Text = "未接続";
                }
                else
                {
                    MessageBox.Show("サーバに接続できません");
                }
                isConnecting = false;
            }
        }

        // デバイス接続通知ハンドラ
        private void onConnect(object sender, KoshiEventArgs e)
        {
            this.BeginInvoke((Action)(() => {
                string deviceName = e.data;
                textStatus.Text = "接続中: " + deviceName;
            }));
        }

        // デバイス切断通知ハンドラ
        private void onDisconnect(object sender, KoshiEventArgs e)
        {
            this.BeginInvoke((Action)(() => {
                textStatus.Text = "未接続";
            }));
        }

        // ネットワーク切断ハンドラ
        private void onClosed(object sender, KoshiEventArgs e)
        {
            this.BeginInvoke((Action)(() => {
                isOpen = false;
                buttonOpen.Text = "開く";
                textStatus.Text = "未接続";
            }));
        }
    }
}
