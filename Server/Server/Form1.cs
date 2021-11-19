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
        public string Login { set; get; } = "";

        private BroadcastClient _client;

        private IPEndPoint _serverEndPoint;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _client = new BroadcastClient(Guid.NewGuid().ToString(), ListenPortBroadCast);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _client.StopDiscovery();
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
            _serverEndPoint = new IPEndPoint(endPoint.Address, data.Port);

            // отписываю метод, чтобы он отработал Единожды
            _client.ServerFound -= this.OnServerFound;
            _client.StopDiscovery();
        }
    }
}