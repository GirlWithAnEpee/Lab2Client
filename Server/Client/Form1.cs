using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{
    public partial class Form1 : Form
    {
        public const int ListenPortBroadCast = 1111;
        public const int SendPortBroadCast = 1010;
        public const int ListenPort = 8888;
        public const int SendPort = 8080;
        public string Login { set; get; } = "";

        private BroadcastClient _client;

        private IPEndPoint _serverEndPoint;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _client = new BroadcastClient(SendPortBroadCast);
            var data = new BroadcastData()
            {
                CommunicationPort = ListenPort,
                BroadcastPort = _client.BroadcastPort,
                Name = Guid.NewGuid().ToString()
            };
            _client.ServerFound += OnServerFound;
            _client.StartDiscovery(data);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _client.StopDiscovery();
            Application.Exit();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            if (textBoxLogin.Text == "")
            {
                MessageBox.Show("Логин не может быть пустым!");
                return;
            }

            if (_serverEndPoint == null)
            {
                MessageBox.Show("Сервер не был найден");
                return;
            }

            Login = textBoxLogin.Text;
            Form2 calc = new Form2(_serverEndPoint);
            calc.Login = this.Login;
            calc.ShowDialog();
            this.Close();
        }

        private void OnServerFound(IPEndPoint endPoint, BroadcastData data)
        {
            _serverEndPoint = new IPEndPoint(endPoint.Address, data.CommunicationPort);

            // отписываю метод, чтобы он отработал Единожды
            _client.ServerFound -= this.OnServerFound;
            _client.StopDiscovery();
        }
    }
}