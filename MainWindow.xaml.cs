using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Windows;

/**
* @author enbiya.kocbiyik
*
* @date - 4/17/2021 4:24:58 PM 
*/

namespace CevreselAlgilayici
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Dictionary<String, String> infoMap = new Dictionary<String, String>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            clearComponents();
            txtLogs.Text = "";
            btnConnect.IsEnabled = false;
            startTcpClient(txtTargetIp.Text, Int32.Parse(txtTargetPort.Text));
        }

        private void startTcpClient(string targetIp, int targetPort)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                TcpClient client = null;
                NetworkStream stream = null;

                try
                {
                    client = new TcpClient(targetIp, targetPort);
                    stream = client.GetStream();
                    stream.ReadTimeout = 3000;

                    Byte[] mawsData = new Byte[26];
                    int index = 0;

                    writeLogs("MAWS bağlantısı kuruldu!");
                    updateComponents();

                    while (true)
                    {
                        Byte[] data = new Byte[1];
                        Int32 bytes = stream.Read(data, 0, data.Length);
                        if (data[0] == 85)
                        {
                            decodeMawsData(mawsData);
                            mawsData[0] = data[0];
                            index = 1;
                        }
                        else
                        {
                            mawsData[index] = data[0];
                            index++;
                        }
                    }
                }
                catch (Exception e)
                {
                    writeLogs("MAWS bağlantısı kesildi!");
                    writeLogs(e.ToString());
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Close();
                    }
                    if (client != null)
                    {
                        client.Close();
                    }
                    clearComponents();
                }
            }).Start();
        }

        private void decodeMawsData(Byte[] mawsData)
        {
            infoMap["lblWindSpeed"] = ConvertHighLowByteToFloat(new byte[] { mawsData[5], mawsData[6] }) + " m/s";
            infoMap["lblWindDirection"] = ConvertHighLowByteToFloat(new byte[] { mawsData[7], mawsData[8] }) + " °";
            infoMap["lblAirTemperature"] = ConvertHighLowByteToFloat(new byte[] { mawsData[9], mawsData[10] }) + " °C";
            infoMap["lblBarometricPressure"] = ConvertHighLowByteToFloat(new byte[] { mawsData[11], mawsData[12] }) + " hPa";
            infoMap["lblHumidity"] = ConvertHighLowByteToFloat(new byte[] { mawsData[13], mawsData[14] }) + " %";
            infoMap["lblCompas"] = ConvertHighLowByteToFloat(new byte[] { mawsData[15], mawsData[16] }) + " °";
            infoMap["lblLatitude"] = ConvertHighMediumLowByteToFloat(new byte[] { mawsData[17], mawsData[18], mawsData[19] }) + " °";
            infoMap["lblLongtitude"] = ConvertHighMediumLowByteToFloat(new byte[] { mawsData[20], mawsData[21], mawsData[22] }) + " °";
            infoMap["lblExternalTemp"] = ConvertHighLowByteToFloat(new byte[] { mawsData[23], mawsData[24] }) + " °";
        }

        private void updateComponents()
        {
            new Thread(() =>
            {
                bool isEnable = false;
                while (!isEnable)
                {
                    Thread.Sleep(1000);
                    this.Dispatcher.Invoke(() =>
                    {
                        lblWindSpeed.Content = infoMap[lblWindSpeed.Name];
                        //lblWindDirection.Content = infoMap[lblWindDirection.Name];
                        lblAirTemperature.Content = infoMap[lblAirTemperature.Name];
                        lblBarometricPressure.Content = infoMap[lblBarometricPressure.Name];
                        lblHumidity.Content = infoMap[lblHumidity.Name];
                        //lblCompas.Content = infoMap[lblCompas.Name];
                        lblLatitude.Content = infoMap[lblLatitude.Name];
                        lblLongtitude.Content = infoMap[lblLongtitude.Name];
                        lblExternalTemp.Content = infoMap[lblExternalTemp.Name];

                        var compDir = infoMap[lblCompas.Name];
                        compasAngle.Angle = Convert.ToDouble(compDir);
                        lblCompas.Content = compDir;

                        var windDir = infoMap[lblWindDirection.Name];
                        lblWindDirection.Content = infoMap[lblWindDirection.Name];
                        windAngle.Angle = (Convert.ToDouble(compDir) + Convert.ToDouble(windDir)) % 360;


                        isEnable = btnConnect.IsEnabled;
                    });
                }
            }).Start();
        }

        private void clearComponents()
        {
            this.Dispatcher.Invoke(() =>
            {
                infoMap["lblWindSpeed"] = "--------";
                infoMap["lblWindDirection"] = "--------";
                infoMap["lblAirTemperature"] = "--------";
                infoMap["lblBarometricPressure"] = "--------";
                infoMap["lblHumidity"] = "--------";
                infoMap["lblCompas"] = "--------";
                infoMap["lblLatitude"] = "--------";
                infoMap["lblLongtitude"] = "--------";
                infoMap["lblExternalTemp"] = "--------";

                btnConnect.IsEnabled = true;
            });
        }

        private void writeLogs(string log)
        {
            this.Dispatcher.Invoke(() =>
            {
                txtLogs.Text += log + "\n";
                logScroll.ScrollToBottom();
            });
        }

        public static float ConvertHighLowByteToFloat(Byte[] data)
        {
            return ((data[0] * 256) + data[1]) / 16.0f;
        }

        public static float ConvertHighMediumLowByteToFloat(Byte[] data)
        {
            return (((data[0] & 127) * 65536) + (data[1] * 256) + data[2]) / (512 * 60.0f);
        }
    }
}
