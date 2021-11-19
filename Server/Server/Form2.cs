using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace Server
{
    public partial class Form2 : Form
    {
        static IPEndPoint serverEndPoint;
        static DiscoveryNode _node;
        static UdpClient clientUDP;
        private const int listenPortBC = 1111;
        private const int sendPortBC = 1010;
        private const int listenPort = 8888;
        private const int sendPort = 8080;
        string operation;
        int ch1, ch2;

        public string Login { set; get; }

        public Form2()
        {
            InitializeComponent();
            _node = new DiscoveryNode(Guid.NewGuid().ToString(), listenPortBC, sendPortBC); //1111 и 1010
            _node.ClientFound += this.OnServerFound;
            _node.StartDiscovery(revealSelf: false, discover: true);
            operation = "+";
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e) =>
            _node.StopDiscovery();


        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
                operation = "+";
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
                operation = "-";
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked)
                operation = "*";
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton4.Checked)
            {
                label2.Visible = false;
                textBox2.Visible = false;
                operation = "!";
            }
            else
            {
                textBox2.Visible = true;
                label2.Visible = true;
            }
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton5.Checked)
            {
                groupBox2.Visible = false;
                operation = "stop";
            }
            else
            {
                groupBox2.Visible = true;
            }
        }

        private void buttonResult_Click(object sender, EventArgs e)
        {
            if (serverEndPoint == null) return;

            if (int.TryParse(textBox1.Text, out ch1) || groupBox2.Visible == false)
            {
                if ((textBox2.Visible == true && int.TryParse(textBox2.Text, out ch2)) || textBox2.Visible == false
                                                                                       || groupBox2.Visible == false)
                {
                    textBoxResult.Text = "";
                    string message = Login + ";" + operation + ";";
                    if (operation != "stop")
                    {
                        message += ch1.ToString() + ";";
                        if (operation != "!")
                            message += ch2.ToString() + ";";
                    }

                    byte[] data = Encoding.Unicode.GetBytes(message);

                    try
                    {
                        clientUDP.Send(data, data.Length, serverEndPoint);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
            else
                MessageBox.Show("Можно вводить только целые числа!");
        }

        private void ReceiveMessage()
        {
            try
            {
                while (true)
                {
                    var endPoint = new IPEndPoint(IPAddress.Any, listenPort); //8888
                    byte[] data = clientUDP.Receive(ref endPoint);
                    if (endPoint.Equals(serverEndPoint))
                    {
                        var result = Encoding.Unicode.GetString(data);
                        textBoxResult.Invoke((MethodInvoker) (() => displayResult(result)));
                    }
                }
            }
            catch (SocketException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void displayResult(string result) =>
            textBoxResult.Text = result;

        private void OnServerFound(IPEndPoint endPoint, BroadcastData data)
        {
            // отписываю метод, чтобы он отработал Единожды
            _node.ClientFound -= this.OnServerFound;

            serverEndPoint = new IPEndPoint(endPoint.Address, sendPort); //8080
            clientUDP = new UdpClient(listenPort); //8888
            // запускаем новый поток для получения данных
            Task.Factory.StartNew(ReceiveMessage);
        }
    }
}