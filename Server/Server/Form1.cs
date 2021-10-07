using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{
    public partial class Form1 : Form
    {
        string login = "";
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
        public Form1()
        {
            InitializeComponent();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            if (textBoxLogin.Text != "")
            {
                login = textBoxLogin.Text;
                Form2 calc = new Form2();
                calc.Login = this.Login;
                calc.ShowDialog();
                this.Close();
            }
            else
            {
                MessageBox.Show("Логин не может быть пустым!");
            }
        }
    }
}
