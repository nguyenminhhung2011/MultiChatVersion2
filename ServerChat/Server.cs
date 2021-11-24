﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace ServerChat
{
    public partial class Server : Form
    {
        //Bien toan cuc
        public static List<Client> listCList = new List<Client>();

        public Server()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
            textIP.Text = "127.0.0.1";
        }
        //Lay dia chia IP Hien co
        IPEndPoint IP;
        Socket Server1;
        List<Socket> ClientList;
        //Hàm gởi data

        int checkPortInListClient(string ipPort) {
            foreach(Client item in listCList) {
                if (ipPort.Substring(ipPort.IndexOf(':') + 1) == item.IpPort)
                    return -1;
            }
            return 0;
        }
        private void sendString(string s,Socket clien) { 
            foreach(Socket item in ClientList) {
                if (item.RemoteEndPoint.ToString() == clien.RemoteEndPoint.ToString()) {
                    clien.Send(Serialize(s));
                    break;
                }
            }
        }
        //Hàm tạo mới IP Socket và connect với người dùng
        private void Connect() {
            ClientList = new List<Socket>();
            IP = new IPEndPoint(IPAddress.Any, Int32.Parse(textPort.Text));
            Server1 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Server1.Bind(IP);
            ClientList.Clear();
            listCList.Clear();
            Thread Listen = new Thread(() =>
            {
                try {
                    while (true) {
                        Server1.Listen(100);
                        Socket clien = Server1.Accept();
                        ClientList.Add(clien);
                        textName.Text = clien.RemoteEndPoint.ToString();
                        Thread rec = new Thread(Receive);
                        rec.IsBackground = true;
                        rec.Start(clien);
                    }
                }
                catch {
                    IP = new IPEndPoint(IPAddress.Any, Int32.Parse(textPort.Text));
                    Server1 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                }
            });
            Listen.IsBackground = true;
            Listen.Start();
        }
        //Hàm kiểm tra người dùng có còn kết nối hay không
        bool SocketConnected(Socket s) {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if (part1 && part2)
                return false;
            else
                return true;
        }
        //Hàm nhận data từ người dùng
        private void Receive(object obj)
        {
            Socket clien = (Socket)obj;
            try
            {
                while (true) {
                    byte[] data = new byte[1024 * 5000];
                    clien.Receive(data);
                    string s = (string)Deserialize(data);
                    checkString1(s, clien);
                    textName.Text = s;
                }
            }
            catch { }
        }
        //Xoa người dùng đã out ra khỏi listClient
        public void removeListClient(string username,sql_manage f) {
            listClient.Rows.Clear();
            listClient.Refresh();
            f.updateActi(username, 0);int i = 0;
            foreach(Client item in listCList) {
                if (item.Name == username) break;
                i++;
            }
            listCList.RemoveAt(i);
            f.Loaddata(listClient, "none", username, 1);
        }
        //Load bảng list clien mỗi khi có client khác kết nối
        private void LoadDatGridView(string username,sql_manage f,Socket clien) {
            listClient.Rows.Clear();
            listClient.Refresh();
            f.updateActi(username, 1);
            int index = clien.RemoteEndPoint.ToString().IndexOf(':');
            f.Loaddata(listClient, clien.RemoteEndPoint.ToString().Substring(index + 1), username, 0);
        }
        private void checkString1(string s,Socket clien) {
            sql_manage f = new sql_manage();
            if (s[0] == '1') {//Kiểm tra người dùng đăng nhập thành công 
                int i = 1;
                string userName = "";string password = "";
                while (true) {
                    if(s[i]!='@')userName += s[i];
                    else{i++;break;}
                    i++;
                }
                while (i < s.Length) { 
                    password += s[i];i++;
                }
                textName.Text = $"{userName}   {password}";
                int check = f.returnNo(userName, password, 1);
                if (check == -1) {
                    sendString("1success", clien);
                }
                else
                    sendString("2unsuccess", clien);
            }
            else if (s[0] == '2') {//Kiểm tra người dùng đăng kí thành công
                int i = 1;
                string username = "";string password = "";string name = "";
                while (true){
                    if (s[i] != '@') username += s[i];
                    if (s[i] == '@'){i++; break;}i++;
                }
                while (i < s.Length) { 
                    password += s[i]; i++;
                }
                int check = f.returnNo(username, password, 2);
                if (check == 0){
                    f.inserAccount(username, password, "");
                    sendString("3success", clien);
                }
                else
                    sendString("4unsuccess", clien);
            }
            else if (s[0] == '3') { //KIểm tra người dùng bị out 
                string userName = s.Substring(1);
                if (!string.IsNullOrEmpty(userName)) { 
                    removeListClient(userName, f);
                    string listClien = f.getListClientActi(s.Substring(1));
                    foreach(Socket item in ClientList) {
                        if (SocketConnected(item)&&checkPortInListClient(item.RemoteEndPoint.ToString())==-1) {
                            item.Send(Serialize(listClien));
                        }    
                        else
                            continue;
                    }
                }
            }
            else if (s[0] == '4') {//Load danh sách người đang online cho người dùng
                string username = "";
                for (int i = 1; i < s.Length; i++)
                    username += s[i];
                LoadDatGridView(username, f,clien);
                string listClien = f.getListClientActi(username);
                foreach (Socket item in ClientList) {
                    if (SocketConnected(item)&&checkPortInListClient(item.RemoteEndPoint.ToString())==-1)
                        item.Send(Serialize(listClien));
                }
            }
            else if (s[0] == '5') {//Gửi và nhận tin nhắn
                int Index = s.IndexOf('@');
                string userName = s.Substring(1, Index - 1);
                string nameSend = "";
                string ipPortRec = "";
                foreach(Client item in listCList) {
                    if ($"{textIP.Text}:{item.IpPort}" == clien.RemoteEndPoint.ToString()) {
                        f.InsertMess(item.Name, userName, s.Substring(Index + 1));
                        nameSend = item.Name;
                        //break;
                    }
                    if (userName == item.Name)
                        ipPortRec = item.IpPort;
                }
                foreach(Socket item in ClientList) {
                    if (SocketConnected(item)&&(item.RemoteEndPoint.ToString().Substring(item.RemoteEndPoint.ToString().IndexOf(':')+1))==ipPortRec) {
                        item.Send(Serialize($"8{nameSend}@{s.Substring(Index + 1)}"));
                        break;
                    }
                    else
                        continue;
                }
            }
            else if (s[0] == '6') { //Nhận và in ra danh sách các tin nhắn
                int Index = s.IndexOf('@');
                string sendString = f.LoadMess(s.Substring(1, Index - 1), s.Substring(Index + 1));
                clien.Send(Serialize(sendString));
            }
        }
        byte[] Serialize(object obj)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();

            formatter.Serialize(stream, obj);
            return stream.ToArray();// stream tra ra 1 day byte
        }
        // gom manh
        object Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryFormatter formatter = new BinaryFormatter();

            return formatter.Deserialize(stream);
        }

        private void BtnConnect_Click(object sender, EventArgs e){
            if (!string.IsNullOrEmpty(textIP.Text) && !string.IsNullOrEmpty(textPort.Text) && !string.IsNullOrEmpty(textName.Text)) {
                Connect();
                BtnConnect.Enabled = false;
                btnOUT.Enabled = true;
            }
            else
                MessageBox.Show("IP or Port is Empty","Message",MessageBoxButtons.OK,MessageBoxIcon.Error);
        }

        private void btnOUT_Click(object sender, EventArgs e){
            //Server1.Shutdown(SocketShutdown.Both);
            sql_manage f = new sql_manage();
            foreach(Client item in listCList) {
                f.updateActi(item.Name, 0);
            }
            foreach(Socket item in ClientList)
            {
                if (SocketConnected(item) && checkPortInListClient(item.RemoteEndPoint.ToString()) == -1)
                {
                    item.Send(Serialize("5Disconnect"));
                }
                else
                    continue;
            }
            listCList.Clear();
            Server1.Close();
            listClient.Rows.Clear();
            BtnConnect.Enabled = true;
            btnOUT.Enabled = false;
        }
        private void guna2ControlBox1_Click(object sender, EventArgs e)
        {
            listCList.Clear();
            sql_manage f = new sql_manage();
            foreach (Client item in listCList)
            {
                f.updateActi(item.Name, 0);
            }
        }
    }
}
