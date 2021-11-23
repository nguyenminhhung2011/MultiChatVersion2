﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientChat
{
    public partial class Client : Form
    {
        Socket client;
        IPEndPoint ipe;
        Thread threadConnectServer;
        string name;
        /// <summary>
        /// BIến này là biến để kiểm tra xem là thằng server nó còn online hay k 
        /// Kiểu đkm tk server nó mà tắt rối ấy t thấy cái ràng buộc client.send nó bắt buộc gửi 
        /// nên bị lỗi ngầm mặc dù k hiện rõ ra nhưng nó vẫn là lỗi
        /// nên tao thêm biến này 1 -> server còn connect 0 -> không còn
        /// </summary>
        int checkServerOn = 0;
        List<ClientOnline> listClientOnline;
        public Client()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
        }

        private void btnConnectServer_Click(object sender, EventArgs e)
        {
            threadConnectServer = new Thread(new ThreadStart(ConnectServer));
            threadConnectServer.IsBackground = true;
            threadConnectServer.Start();
        }

        private void ConnectServer()
        {
            try
            {
                ipe = new IPEndPoint(IPAddress.Parse(tbIP.Text), int.Parse(tbPort.Text));
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(ipe);
                checkServerOn = 1;
                btnSignIn.Enabled = true;
                btnRegister.Enabled = true;
                btnDisConnect.Enabled = true;
                btnConnectServer.Enabled = false;

                Thread listerServer = new Thread(ReceiveMessage);
                listerServer.IsBackground = true;
                listerServer.Start();
            }
            catch
            {
                MessageBox.Show("Can't connect to server", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //Hiện ra lỗi mỗi khi Server ngắt kết nối và trở về trang login
        private void showErrorWhenServerDis() {
            MessageBox.Show("Server is disconnected", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            checkServerOn = 0;
            this.Invoke(new Action(() =>
            {
                flowLayoutPanel2.Controls.Clear();
                flpListClient.Controls.Clear();
                ((Control)mess).Enabled = false;
                ((Control)login).Enabled = true;
                ((Control)creat).Enabled = true;
                btnConnectServer.Enabled = true;
                btnDisConnect.Enabled = false;
                metroTabControl1.SelectedTab = metroTabControl1.TabPages["login"];
                Username.Text = Password.Text = string.Empty;
                name = string.Empty;
            }));
        }
        private void ReceiveMessage()
        {
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[1024 * 5000];
                    int rec = client.Receive(buffer);
                    string mess = (String)Deserialize(buffer);
                    MessageFromServer.Text += $"Server:{mess}{Environment.NewLine}";
                    CheckMessage(mess);
                }
            }
            catch
            {
                showErrorWhenServerDis();
            }
        }

        //hàm check message từ server
        private void CheckMessage(string message)
        {
            //login thành công
            if (message[0] == '1')
            {
                name = Username.Text;

                this.Invoke(new Action(() =>
                {
                    metroTabControl1.SelectedTab = metroTabControl1.TabPages["mess"];

                    ((Control)mess).Enabled = true;
                    ((Control)login).Enabled = false;
                    ((Control)creat).Enabled = false;

                }));
                //load người dùng
                client.Send(Serialize($"4{Username.Text}"));
            }
            //login không thành công
            else if (message[0] == '2')
            {
                MessageBox.Show("Invalid password or username","Message",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
            //register thành công
            else if (message[0] == '3')
            {
                MessageBox.Show("Register successfully");
                this.Invoke(new Action(() =>{
                    UsernameRegister.Clear();
                    PasswordRegister.Clear();
                    RePassRegister.Clear();
                }));
            }
            //register không thành công
            else if (message[0] == '4')
            {
                MessageBox.Show("Your username has been already exist");
            //add client online into flow layout panel
            }
            //Kiểm tra server bị nhấn vào nút disconnect
            else if (message[0] == '5')
            {
                showErrorWhenServerDis();
            }
            else if(message[0] == '6')
            {
                this.Invoke(new Action(() => {
                    string mess = message.Substring(1);
                    string[] listTmp = mess.Split('@');
                    listClientOnline = new List<ClientOnline>();
                    flpListClient.Controls.Clear();

                    foreach(string s in listTmp)
                    {
                        if (s != "" && s != name) {
                            ClientOnline clientOnline = new ClientOnline();
                            clientOnline.lbName.Text = s;
                            clientOnline.Click += ClientOnline_Click;
                            listClientOnline.Add(clientOnline);
                        }
                    }
                    int i = 0;
                    foreach (ClientOnline item in listClientOnline)
                    {
                        if (i == 0) OpText.Text = item.lbName.Text;
                        flpListClient.Controls.Add(item);
                        i++;
                    }
                }));
            }
        }

        private void ClientOnline_Click(object sender, EventArgs e)
        {
            string s = (sender as ClientOnline).Tag as string;
            OpText.Text = s;
        }

        byte[] Serialize(object obj)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();

            formatter.Serialize(stream, obj);

            return stream.ToArray();
        }
        object Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryFormatter formatter = new BinaryFormatter();

            return formatter.Deserialize(stream);
        }

        private void btnSignIn_Click(object sender, EventArgs e)
        {
            if(!string.IsNullOrEmpty(Username.Text) && !string.IsNullOrEmpty(Password.Text))
            {
                client.Send(Serialize($"1{Username.Text}@{Password.Text}"));
            }
            else
            {
                MessageBox.Show("Username or password can't be empty");
            }
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(UsernameRegister.Text) && !string.IsNullOrEmpty(PasswordRegister.Text) && PasswordRegister.Text == RePassRegister.Text)
            {
                client.Send(Serialize($"2{UsernameRegister.Text}@{PasswordRegister.Text}"));
            }
            else
            {
                MessageBox.Show("Username or password can't be empty");
            }
        }

        private void btnDisConnect_Click(object sender, EventArgs e)
        {
            if (checkServerOn == 1)
            {
                if (name != "")
                {
                    client.Send(Serialize($"3{name}"));
                    client.Close();
                    Application.Exit();
                }
                else
                {
                    client.Send(Serialize($"3{name}"));
                    client.Close();
                    Application.Exit();
                }
            }
        }

        private void guna2ControlBox1_Click(object sender, EventArgs e)
        {
            if (checkServerOn == 1) { 
                if(name != "") {
                    client.Send(Serialize($"3{name}"));
                    client.Close();
                    Application.Exit();
                }
                else
                {
                    client.Send(Serialize($"3{name}"));
                    client.Close();
                    Application.Exit();
                }
            }
        }
    }
}
