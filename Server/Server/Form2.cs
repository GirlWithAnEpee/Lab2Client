﻿using System;
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
        static IPAddress remoteAddress; // хост для отправки данных
        static Socket sendSocket;
        static IPAddress sendAdress;
        static IPEndPoint sendEndPoint;
        //порты для получения и отправки данных

        private const int listenPort = 11000;
        private const int sendPort = 11100;
        string login, operation;
        int ch1, ch2;
        public string Login
        {
            set
            {
                login = value;
            }
            get
            {
                return login;
            }
        }

        private static string LocalIPAddress()
        {
            string localIP = "";
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
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

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                operation = "+";
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                operation = "-";
            }
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked)
            {
                operation = "*";
            }
        }
        private void Form2_Load(object sender, EventArgs e)
        {
            operation = "+";
        }

        private void buttonResult_Click(object sender, EventArgs e)
        {
            if (int.TryParse(textBox1.Text, out ch1) || groupBox2.Visible == false)
            {
                if ((textBox2.Visible == true && int.TryParse(textBox2.Text, out ch2)) || textBox2.Visible == false
                    || groupBox2.Visible == false)
                {
                    textBoxResult.Text = "";
                    sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    sendAdress = IPAddress.Loopback;
                    sendEndPoint = new IPEndPoint(sendAdress, sendPort);

                    try
                    {
                        // запускаем новый поток для получения данных
                        Thread receiveThread = new Thread(new ThreadStart(ReceiveMessage));
                        receiveThread.Start(); //старт потока
                        SendMessage();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    finally
                    {
                        //sendSocket.Close();
                        sendSocket.Dispose();
                    }
                }
            }
            else
                MessageBox.Show("Можно вводить только целые числа!");
        }

        // отправка сообщений
        private void SendMessage()
        {
            while (true)
            {
                //sendSocket.EnableBroadcast = true;
                string message = login + ";" + operation + ";";
                if (operation != "stop")
                {
                    message += ch1.ToString() + ";";
                    if (operation != "!")
                        message += ch2.ToString() + ";";
                }
                
                byte[] data = Encoding.Unicode.GetBytes(message);
                try
                {
                    sendSocket.SendTo(data, sendEndPoint);
                }
                catch (SocketException ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    sendSocket.Close();
                }
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

        private void ReceiveMessage()
           {
            UdpClient receiver = new UdpClient(listenPort);
            //ответ можно получить от любого айпишника
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, listenPort);
            string localAddress = LocalIPAddress();

            try
            {
                while (true)
                {
                    byte[] data = receiver.Receive(ref endPoint);
                    if (endPoint.Address.ToString().Equals(localAddress))
                        continue;
                    textBoxResult.Text = Encoding.Unicode.GetString(data);
                }
            }
            catch (SocketException ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                receiver.Close();
            }
        }

        public Form2()
        {
            InitializeComponent();
        }
    }
}
