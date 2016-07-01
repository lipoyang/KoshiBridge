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

#if false
            client.setOnCennect(onConnect);
            client.setOnDisconnect(onDisconnect);
#else
            client.onConnect += onConnect;
            client.onDisconnect += onDisconnect;
#endif
        }

        private void buttonOpen_Click(object sender, EventArgs e)
        {
            if (koshiClient == null) return;

            // 開いてたら閉じる
            if(isOpen)
            {
                koshiClient.close();

                buttonOpen.Text = "開く";
                textStatus.Text = "未接続";

            }
            // 閉じていたら開く
            else
            {
                string ipAddr = textIpAddr.Text;
                int port = 0;
                if (!int.TryParse(textPort.Text, out port))
                {
                    MessageBox.Show("ポート番号が無効です");
                }
                if (koshiClient.open(ipAddr, port))
                {
                    buttonOpen.Text = "閉じる";
                    textStatus.Text = "未接続";
                }
            }
        }

        // デバイス接続通知ハンドラ
        private void onConnect(object sender, KoshiEventArgs e)
        {
            // TODO
            string deviceName = e.data;
            textStatus.Text = "接続中: " + deviceName;
        }

        // デバイス切断通知ハンドラ
        private void onDisconnect(object sender, KoshiEventArgs e)
        {
            // TODO
            textStatus.Text = "未接続";
        }
    }
}
