package net.lipoyang.koshibridge;

import android.bluetooth.BluetoothGattCharacteristic;
import android.support.v7.app.ActionBarActivity;
import android.os.Bundle;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.TextView;
import android.widget.Toast;
import android.util.Log;
import android.os.Handler;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.net.InetAddress;
import java.net.NetworkInterface;
import java.net.ServerSocket;
import java.net.Socket;
import java.net.SocketException;
import java.net.SocketTimeoutException;
import java.util.Enumeration;

import com.uxxu.konashi.lib.Konashi;
import com.uxxu.konashi.lib.KonashiListener;
import com.uxxu.konashi.lib.KonashiManager;
import org.apache.http.conn.util.InetAddressUtils;
import org.jdeferred.DoneCallback;
import org.jdeferred.FailCallback;
import info.izumin.android.bletia.BletiaException;

public class KoshiServerUI extends ActionBarActivity implements Runnable, View.OnClickListener {

    // Konashi
    private KonashiManager mKonashiManager;
    // Bluetooth state
    public static final int STATE_DISCONNECTED = 0;
    public static final int STATE_CONNECTING = 1;
    public static final int STATE_CONNECTED = 2;
    private int btState = STATE_DISCONNECTED;

    // Widgets
    private Button buttonBleSearch;
    private Button buttonServerUpdate;
    private TextView textKoshianDev;
    private TextView textServerIp1;
    private TextView textServerIp2;
    private TextView textClientIp;
    private EditText editPortNum;

    // Debugging
    private static final String TAG = "KoshiBridge";
    private static final boolean DEBUGGING = true;
    private final KoshiServerUI self = this;

    // IP
    private static final int DEFAULT_PORT_NUM = 5678;
    private int portNum;
    private ServerSocket mServer;
    private Socket mClient;
    private BufferedReader reader;
    private BufferedWriter writer;
    int port = 8080;
    volatile Thread runner = null;
    Handler mHandler = new Handler();
    private boolean toQuit;
    private boolean toUpdate;
    private boolean isOpen;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_koshi_server_ui);
        if(DEBUGGING) Log.e(TAG, "++ ON CREATE ++");

        // Widgets
        buttonBleSearch = (Button)findViewById(R.id.buttonBleSearch);
        buttonServerUpdate = (Button)findViewById(R.id.buttonServerUpdate);
        buttonBleSearch.setOnClickListener(this);
        buttonServerUpdate.setOnClickListener(this);
        textKoshianDev = (TextView)findViewById(R.id.textKoshianDev);
        textServerIp1 = (TextView)findViewById(R.id.textServerIp1);
        textServerIp2 = (TextView)findViewById(R.id.textServerIp2);
        textClientIp = (TextView)findViewById(R.id.textClientIp);
        editPortNum = (EditText)findViewById(R.id.editPortNum);

        // Konashi
        mKonashiManager = new KonashiManager(getApplicationContext());

        // IP
        portNum = DEFAULT_PORT_NUM;
        editPortNum.setText(String.valueOf(portNum));
    }
    @Override
    public void onStart() {
        super.onStart();
        if(DEBUGGING) Log.e(TAG, "++ ON START ++");
    }
    @Override
    public synchronized void onResume() {
        super.onResume();
        if(DEBUGGING) Log.e(TAG, "+ ON RESUME +");

        // Konashi
        mKonashiManager.addListener(mKonashiListener);
        btState = mKonashiManager.isReady() ? STATE_CONNECTED : STATE_DISCONNECTED;
        viewBleConnection();

        // IP
        String[] ip = getIPAddress();
        textServerIp1.setText(ip[0]);
        textServerIp2.setText(ip[1]);
        //textServerIp1.setText(getIPAddress());
        toQuit = false;
        toUpdate = false;
        isOpen = false;
        //if(runner == null){
            runner = new Thread(this);
        //}
        runner.start();
    }
    @Override
    public synchronized void onPause() {
        // Konashi
        mKonashiManager.removeListener(mKonashiListener);

        // IP
        toQuit = true;

        super.onPause();
        if(DEBUGGING) Log.e(TAG, "- ON PAUSE -");
    }
    @Override
    public void onStop() {
        super.onStop();
        if(DEBUGGING) Log.e(TAG, "-- ON STOP --");
    }
    @Override
    public void onDestroy() {
        // Konashi
        new Thread(new Runnable() {
            @Override
            public void run() {
                if(mKonashiManager.isConnected()){
                    mKonashiManager.reset()
                            .then(new DoneCallback<BluetoothGattCharacteristic>() {
                                @Override
                                public void onDone(BluetoothGattCharacteristic result) {
                                    mKonashiManager.disconnect();
                                }
                            });
                }
            }
        }).start();
        super.onDestroy();
        if(DEBUGGING) Log.e(TAG, "--- ON DESTROY ---");
    }

    /**
     * Konashi's Event Listener
     */
    private final KonashiListener mKonashiListener = new KonashiListener() {
        @Override
        public void onConnect(KonashiManager manager) {
            // Connected!
            btState = STATE_CONNECTED;
            viewBleConnection();

            mKonashiManager.uartMode(Konashi.UART_ENABLE)
                    .then(new DoneCallback<BluetoothGattCharacteristic>() {
                        @Override
                        public void onDone(BluetoothGattCharacteristic result) {
                            mKonashiManager.uartBaudrate(Konashi.UART_RATE_38K4);
                            
                            if(isOpen){
                                try{
                                    writer.write("onConnect ");
                                    writer.write(mKonashiManager.getPeripheralName());
                                    writer.newLine();
                                    writer.flush();
                                }
                                catch(IOException ex){
                                    ;
                                }
                            }
                        }
                    })
                    .fail(new FailCallback<BletiaException>() {
                        @Override
                        public void onFail(BletiaException result) {
                            Toast.makeText(self, result.getMessage(), Toast.LENGTH_SHORT).show();
                        }
                    });
        }
        @Override
        public void onDisconnect(KonashiManager manager) {
            // Disconnected!
            btState = STATE_DISCONNECTED;
            viewBleConnection();
            
            if(isOpen){
                try{
                    writer.write("onDisconnect");
                    writer.newLine();
                    writer.flush();
                }
                catch(IOException ex){
                    ;
                }
            }
        }
        @Override
        public void onError(KonashiManager manager, BletiaException e) {

        }
        @Override
        public void onUpdatePioOutput(KonashiManager manager, int value) {

        }
        @Override
        public void onUpdateUartRx(KonashiManager manager, byte[] value) {
            if(isOpen){
                try{
                    writer.write("onUpdateUartRx ");
                    writer.write(new String(value));
                    writer.newLine();
                    writer.flush();
                }
                catch(IOException ex){
                    ;
                }
            }
        }
        @Override
        public void onUpdateBatteryLevel(KonashiManager manager, int level) {

        }
    };

    // This is just a last resort. Versiion 2 SDK has no onCancelKonashi
    @Override
    public void onWindowFocusChanged(boolean hasFocus) {
        if(hasFocus){
            Log.e(TAG, "onWindowFocusChanged [true]");
            String pName = mKonashiManager.getPeripheralName();
            Log.e(TAG, "pName = [" + pName + "]");

            if(btState == STATE_CONNECTING){
                if(mKonashiManager.getPeripheralName().equals("")){
                    btState = STATE_DISCONNECTED;
                    viewBleConnection();
                }
            }
        }
    }

    // On Clidk BUttons
    @Override
    public void onClick(View v) {
        // [BLE Device Search]
        if (v.getId() == R.id.buttonBleSearch) {
            // Connecting
            if(!mKonashiManager.isReady()){
                btState = STATE_CONNECTING;
                viewBleConnection();

                // search Koshian, and open a selection dialog
                mKonashiManager.find(this, true);
            }
            // Disconnecting
            else {
                // disconnect Koshian
                mKonashiManager.disconnect();
            }
        }
        // [Server Update]
        else if (v.getId() == R.id.buttonServerUpdate) {
            try{
                int val = Integer.parseInt(editPortNum.getText().toString());
                if(val<1024 || val>65535) throw new NumberFormatException();
                portNum = val;
                toUpdate = true;
            }
            catch(NumberFormatException ex)
            {
                Toast.makeText(this, "ポート番号が無効です", Toast.LENGTH_LONG).show();
            }
        }
    }

    // View BLE Connection
    private void viewBleConnection()
    {
        switch(btState){
            case STATE_CONNECTED:
                buttonBleSearch.setText("BLEデバイス切断");
                textKoshianDev.setText(mKonashiManager.getPeripheralName());
                break;
            case STATE_CONNECTING:
                buttonBleSearch.setText("BLEデバイス検索");
                textKoshianDev.setText("接続中...");
                break;
            case STATE_DISCONNECTED:
                buttonBleSearch.setText("BLEデバイス検索");
                textKoshianDev.setText("未接続");
                break;
        }
    }

    // Get IP Address
    private String[] getIPAddress(){
        String[] ip = new String[2];
        int ipCnt = 0;
        ip[0] = "---.---.---.---";
        ip[1] = "";

        try{
            for (Enumeration<NetworkInterface> interfaces = NetworkInterface.getNetworkInterfaces();
                interfaces.hasMoreElements();){
                NetworkInterface networkInterface = interfaces.nextElement();
                for (Enumeration<InetAddress> ipAddressEnum = networkInterface.getInetAddresses(); ipAddressEnum.hasMoreElements();){
                    InetAddress inetAddress = (InetAddress) ipAddressEnum.nextElement();
                    //---check that it is not a loopback address and it is ipv4---
                    if(!inetAddress.isLoopbackAddress() &&
                        InetAddressUtils.isIPv4Address(inetAddress.getHostAddress())){
                        if(ipCnt == 0){
                            ip[0] = inetAddress.getHostAddress();
                            ipCnt++;
                        }
                        else if(ipCnt == 1){
                            ip[1] = inetAddress.getHostAddress();
                            return ip;
                        }
                        //return inetAddress.getHostAddress();
                    }
                }
            }
        }catch (SocketException ex){
            Log.e("getLocalIpv4Address", ex.toString());

        }
        //return "---.---.---.---";
        return ip;
    }

    @Override
    public void run() {
        // スレッド終了指令まで
        while(!toQuit){
            try{
                mServer = new ServerSocket(portNum);
                mServer.setSoTimeout(3000); // timeout 3sec
                mClient = mServer.accept();
                //Toast.makeText(this, "accept!", Toast.LENGTH_LONG).show();

                mClient.setSoTimeout(2000); // timeout 2sec
                if(toQuit) return;

                // Show Client Address
                mHandler.post(new Runnable() {
                    @Override
                    public void run() {
                        String clientIp = mClient.getInetAddress().getHostAddress();
                        textClientIp.setText(clientIp);
                    }
                });

                reader = new BufferedReader(new InputStreamReader(mClient.getInputStream()));
                writer = new BufferedWriter(new OutputStreamWriter(mClient.getOutputStream()));
                isOpen = true;

                //Toast.makeText(this, "hoge!", Toast.LENGTH_LONG).show();

                // クライアントが閉じられるまで
                while(!toQuit && !toUpdate){
                    String data;
                    try {
                        data = reader.readLine();
                        if(data == null){
                            break;
                        }
                        // 受信したデータの処理
                        executeRecvData(data);
                    }
                    catch( SocketTimeoutException ex ) {
                        // System.out.println( "-> 受信タイムアウトが発生しました " );
                        continue;
                    }
                }
            }
            catch (IOException e) {
                e.printStackTrace();
            }
            isOpen = false;
            try{
                if(mClient!=null) mClient.close();
                if(mServer!=null) mServer.close();
            }
            catch(IOException e) {
                e.printStackTrace();
            }

            mHandler.post(new Runnable() {
                @Override
                public void run() {
                    textClientIp.setText("未接続");
                    String[] ip = getIPAddress();
                    textServerIp1.setText(ip[0]);
                    textServerIp2.setText(ip[1]);
                }
            });

            toUpdate = false;
        }
    }
    
    // 受信データの処理
    private void executeRecvData(String data)
    {
        // ネットワーク接続通知
        if(data.startsWith("open"))
        {
            if(mKonashiManager.isConnected()){
                if(isOpen){
                    try{
                        writer.write("onConnect ");
                        writer.write(mKonashiManager.getPeripheralName());
                        writer.newLine();
                        writer.flush();
                    }
                    catch(IOException ex){
                        ;
                    }
                }
            }
        }
        // UART送信
        else if(data.startsWith("uartWrite "))
        {
            if(mKonashiManager.isConnected()){
                byte [] bCommand=data.substring(10).getBytes();
                mKonashiManager.uartWrite(bCommand)
                        .fail(new FailCallback<BletiaException>() {
                            @Override
                            public void onFail(BletiaException result) {
                                //Toast.makeText(self, result.getMessage(), Toast.LENGTH_SHORT).show();
                            }
                        });
            }
        }
        // UARTボーレート設定
        else if(data.startsWith("uartBaudrate "))
        {
            if(mKonashiManager.isConnected()){
                try{
                    int val = Integer.parseInt(data.substring(13));

                    if(val == 9600){
                        mKonashiManager.uartBaudrate(Konashi.UART_RATE_9K6);
                    }
                    else if(val == 19200){
                        mKonashiManager.uartBaudrate(Konashi.UART_RATE_19K2);
                    }
                    else if(val == 38400){
                        mKonashiManager.uartBaudrate(Konashi.UART_RATE_38K4);
                    }
                }catch(NumberFormatException ex)
                {
                    ;
                }
            }
        }
    }
}
