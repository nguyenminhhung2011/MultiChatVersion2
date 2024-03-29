﻿using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace ClientChat
{
    public partial class Client : Form
    {
        #region Values
        System.Timers.Timer t;

        //List values
        List<ClientOnline> listClientOnline;
        List<GroupOnline> listGroup;
        Socket client;
        
        //Socket values
        IPEndPoint ipe;
        Thread threadConnectServer;

        // voice VAues
        WaveIn wave;
        WaveFileWriter writer;

        //String values
        public static string receivedPath = "C:/Users/ASUS/OneDrive/caro/OneDrive/Desktop/";
        public static string receivedPath2 = "D:/sql/MultiChatVersion2/imageTrash/";
        public static string receivedPath1 = "D:/project/MultiChatVersion2/imageTrash/";
        public static string allclie = "";
        public static string name;

        string outFileVoceRecord = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        string fileNamePath = "";
        string outputFileName;
        string nameVoice;

        // Int values
        int checkServerOn = 0;
        int choseAvt = 0;
        int deviceName =0;
        int checkmessGroup_person = 0;
        int s = 30;
 
        #endregion

        #region Load Form
        public Client()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
            metroTabControl1.SelectedTab = metroTabControl1.TabPages["login"];
            LoadListView();
            recordVoice.Hide();
            deviceName = LoadDevices();
            //tbSearch.Text = deviceName;
            t = new System.Timers.Timer();
            t.Interval = 1000;
            t.Elapsed += OntimeEvent;
        }
        private int LoadDevices()
        {
            int s =0;
            //for (int deviceId = 0; deviceId < WaveIn.DeviceCount; deviceId++){
            //    var deviceInfo = WaveIn.GetCapabilities(deviceId);
            //}
            if(WaveIn.DeviceCount > 0) { 
                if(WaveIn.DeviceCount > 1) {
                    for (int deviceId = 0; deviceId < WaveIn.DeviceCount; deviceId++){
                        var deviceInfo = WaveIn.GetCapabilities(deviceId);
                        if (deviceInfo.ProductName.Contains("DroidCam"))
                            continue;
                        else
                        {
                            s = deviceId;
                            break;
                        }
                    }
                }
                else {
                    s = 0;
                }
            }
            return s;
        }
        private void OntimeEvent(object sender, ElapsedEventArgs e){
            this.Invoke(new Action(() =>
            {
                s -= 1;
                if (s == 0) {
                    t.Stop();
                    stopRecording();
                }
                timeOut.Text = string.Format($"00:{s.ToString()}");
            }));
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
        #endregion
        
        #region Built Size for User control mess
        // CHỉnh size cho Send mess
        private void buitSizeSend(string s,Send uc) { 
            if(s.Length < 30){
                uc.guna2CustomGradientPanel1.Width = s.Length * 10 + 40;
                uc.label1.Width = s.Length*10+20;
                uc.label1.Text = s;
            }
            else {
                uc.label1.Text = string.Empty;
                for(int i = 0; i < s.Length; i++) {
                    if (i % 30 == 0&&i!=0) {
                        uc.Height += 17;
                        uc.guna2CustomGradientPanel1.Height += 17;
                        uc.label1.Height += 17;
                        uc.label1.Text += $"{Environment.NewLine}{s[i]}";
                    }
                    else
                        uc.label1.Text += s[i];
                }
            }
        }

        //Chỉnh size cho Rec mess
        private void buitSizeRec(string s,Recieve uc) { 
            if(s.Length < 30){
                uc.guna2CustomGradientPanel1.Width = s.Length * 10 + 40;
                uc.label1.Width = s.Length*10+20;
                uc.label1.Text = s;
            }
            else {
                uc.label1.Text = string.Empty;
                for(int i = 0; i < s.Length; i++) {
                    if (i % 30 == 0&&i!=0) {
                        uc.Height += 17;
                        uc.guna2CustomGradientPanel1.Height += 17;
                        uc.label1.Height += 17;
                        uc.label1.Text += $"{Environment.NewLine}{s[i]}";
                    }
                    else
                        uc.label1.Text += s[i];
                }
            }
        }
        #endregion

        #region send data 
        private void sendData(byte header,string s) {
            byte[] stringBye = Serialize(s);
            byte[] send = new byte[stringBye.Length + 1];
            send[0] = header;
            for (int i = 1; i <= stringBye.Length; i++)
                send[i] = stringBye[i - 1];
            client.Send(send);
        }

        #endregion
        
        #region checkString
        private void checkMessage1(string message)
        {
            if (message[0] == 1)
            {
                //tbSearch.Text = message;
            }
        }
        //hàm check message từ server
        List<mess> listMess = new List<mess>();
        private void CheckMessage(string message)
        {
            //login thành công
            if (message[0] == '1')
            {
                name = Username.Text;
                this.Invoke(new Action(() => {
                    metroTabControl1.SelectedTab = metroTabControl1.TabPages["mess"];
                    allEmoji.Hide();
                    panel1.Hide();
                    ((Control)mess).Enabled = true;
                    ((Control)login).Enabled = false;
                    ((Control)creat).Enabled = false;
                    nameCLient.Text = Username.Text;
                    string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                    Image image = Image.FromFile(path.Substring(0, path.Length - 9) + @"Avt\" + message.Substring(8));
                    var ms = new MemoryStream();
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    var bytes = ms.ToArray();
                    avtClient.Image = Image.FromStream(new MemoryStream(bytes));
                }));
                //load người dùng
                sendData(4, $"4{Username.Text}");
            }
            //login không thành công
            else if (message[0] == '2')
            {
                MessageBox.Show("Invalid password or username", "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            //register thành công
            else if (message[0] == '3')
            {
                MessageBox.Show("Register successfully");
                this.Invoke(new Action(() => {
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
            else if (message[0] == '6')
            {
                this.Invoke(new Action(() => {
                    string mess = message.Substring(1);
                    string[] listTmp = mess.Split(':');
                    if (listTmp.Length != 0)
                    {
                        string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                        listClientOnline = new List<ClientOnline>();
                        flpListClient.Controls.Clear();
                        for (int j = 0; j < listTmp.Length; j += 2)
                        {
                            if (listTmp[j] != "" && listTmp[j] != name)
                            {
                                ClientOnline clientOnline = new ClientOnline();
                                clientOnline.lbName.Text = listTmp[j];
                                clientOnline.CheckClick = 0;
                                clientOnline.NoRecDontSee = 0;
                                listClientOnline.Add(clientOnline);
                                Image image = Image.FromFile(path.Substring(0, path.Length - 9) + @"Avt\" + listTmp[j + 1]);
                                var ms = new MemoryStream();
                                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                var bytes = ms.ToArray();
                                clientOnline.avtClient.Image = Image.FromStream(new MemoryStream(bytes));
                                clientOnline.Tag = clientOnline;
                                clientOnline.Click += ClientOnline_Click;
                            }
                        }
                        int i = 0;
                        foreach (ClientOnline item in listClientOnline)
                        {
                            if (i == 0)
                            {
                                OpText.Text = item.lbName.Text;
                                opAvt.Image = item.avtClient.Image;
                            }
                            flpListClient.Controls.Add(item);
                            i++;
                        }
                        allMessage.Controls.Clear();
                        OpText.Text = string.Empty;
                        EnableImagePerson();
                        opAvt.Hide();
                        btnallUser.Enabled = false;
                        btnallGroup.Enabled = true;
                    }
                    sendData(12, $"3{Username.Text}");
                }));
            }
            else if (message[0] == '7')
            { //Nhận và load danh sách các tin nhắn
                this.Invoke(new Action(() => {
                    string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                    allMessage.Controls.Clear();
                    //List<mess> lism = messInstance.Instance.LoadMess(nameCLient.Text, OpText.Text, message);
                    listMess = messInstance.Instance.LoadMess(nameCLient.Text, OpText.Text, message);
                    foreach (mess item in listMess)
                    {
                        if (item.Content[0] == '0')
                        {
                            if (item.Type == 1)
                            {
                                Send f = new Send();
                                buitSizeSend(item.Content.Substring(1), f);
                                allMessage.Controls.Add(f);
                                int height = f.Location.Y + f.Size.Height;
                                f.Tag = height.ToString();
                                allMessage.ScrollControlIntoView(f);
                                item.Scrollx = f.Right - allMessage.AutoScrollPosition.X;
                                item.Scrolly = f.Left - allMessage.AutoScrollPosition.Y;
                            }
                            else if (item.Type == -1)
                            {
                                Recieve f = new Recieve();
                                f.guna2CirclePictureBox1.Image = opAvt.Image;
                                f.guna2CirclePictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                                buitSizeRec(item.Content.Substring(1), f);
                                allMessage.Controls.Add(f);
                                int height = f.Location.Y + f.Size.Height;
                                f.Tag = height.ToString();
                                allMessage.ScrollControlIntoView(f);
                                item.Scrollx = f.Right - allMessage.AutoScrollPosition.X;
                                item.Scrolly = f.Left - allMessage.AutoScrollPosition.Y;
                            }
                        }
                        else if (item.Content[0] == '1')
                        {
                            if (item.Type == 1)
                            {
                                Image image = Image.FromFile(path.Substring(0, path.Length - 9) + @"emoji\" + item.Content.Substring(1));
                                var ms = new MemoryStream();
                                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                var bytes = ms.ToArray();
                                var pic = new ImageMessSend();
                                pic.guna2PictureBox1.Image = Image.FromStream(new MemoryStream(bytes));
                                pic.Height -= 100;
                                pic.guna2PictureBox1.Height -= 100;
                                pic.guna2PictureBox1.Width -= 140;
                                pic.guna2PictureBox1.BorderRadius = 0;
                                allMessage.Controls.Add(pic);
                                int height = pic.Location.Y + pic.Size.Height;
                                pic.Tag = height.ToString();

                            }
                            else if (item.Type == -1)
                            {
                                Image image = Image.FromFile(path.Substring(0, path.Length - 9) + @"emoji\" + item.Content.Substring(1));
                                var ms = new MemoryStream();
                                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                var bytes = ms.ToArray();
                                var pic = new imageMessRec();
                                pic.guna2PictureBox1.Image = Image.FromStream(new MemoryStream(bytes));
                                pic.guna2CirclePictureBox1.Image = opAvt.Image;
                                pic.guna2CirclePictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                                pic.Height -= 100;
                                pic.guna2PictureBox1.Height -= 100;
                                pic.guna2PictureBox1.Width -= 140;
                                pic.guna2PictureBox1.BorderRadius = 0;
                                allMessage.Controls.Add(pic);
                                int height = pic.Location.Y + pic.Size.Height;
                                pic.Tag = height.ToString();

                            }
                        }
                        else if (item.Content[0] == '2')
                        {
                            if (item.Type == -1)
                            {
                                var pic = new voiceMessRec();
                                pic.outFileVoceRecord = path.Substring(0, path.Length - 20) + @"\voiceRecord\" + item.Content.Substring(1);
                                pic.Load();
                                pic.guna2CirclePictureBox1.Image = opAvt.Image;
                                pic.guna2CirclePictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                                allMessage.Controls.Add(pic);
                                allMessage.ScrollControlIntoView(pic);
                                int height = pic.Location.Y + pic.Size.Height;
                                pic.Tag = height.ToString();

                            }
                            else
                            {
                                var pic = new voieMessSend();
                                pic.outFileVoceRecord = path.Substring(0, path.Length - 20) + @"\voiceRecord\" + item.Content.Substring(1);
                                pic.Load();
                                allMessage.Controls.Add(pic);
                                allMessage.ScrollControlIntoView(pic);
                                int height = pic.Location.Y + pic.Size.Height;
                                pic.Tag = height.ToString();

                            }
                        }
                    }
                    //...
                    if (listMess.Count != 0)
                    {
                        var pic = new Send();
                        allMessage.Controls.Add(pic);
                        int height = pic.Location.Y + pic.Size.Height;
                        pic.Tag = height.ToString();

                        allMessage.ScrollControlIntoView(pic);
                        allMessage.AutoScrollPosition = new Point(pic.Right - allMessage.AutoScrollPosition.X,
                                                                        pic.Left - allMessage.AutoScrollPosition.Y);
                        allMessage.Controls.Remove(pic);

                    }
                }));
            }
            else if (message[0] == '8')
            {//mesage 
                this.Invoke(new Action(() =>
                {
                    int Index = message.IndexOf('@');
                    if (OpText.Text == message.Substring(1, Index - 1) && checkmessGroup_person == 0)
                    {
                        var pic = new Recieve();
                        buitSizeRec(message.Substring(Index + 1), pic);
                        pic.guna2CirclePictureBox1.Image = opAvt.Image;
                        pic.guna2CirclePictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                        allMessage.Controls.Add(pic);
                        int height = pic.Location.Y + pic.Size.Height;
                        pic.Tag = height.ToString();

                        allMessage.ScrollControlIntoView(pic);
                        allMessage.AutoScrollPosition = new Point(pic.Right - allMessage.AutoScrollPosition.X,
                                                                        pic.Left - allMessage.AutoScrollPosition.Y);

                    }
                    else
                    {
                        foreach (ClientOnline item in listClientOnline)
                        {
                            if (item.lbName.Text == message.Substring(1, Index - 1))
                            {
                                item.NoRecDontSee++; item.lbCount.Text = item.NoRecDontSee.ToString(); item.lbCount.Show();
                                break;
                            }
                        }
                    }
                }));
            }
            else if (message[0] == '9')
            {//Nhận Emoji              
                this.Invoke(new Action(() =>
                {
                    int Index = message.IndexOf('@');
                    if (OpText.Text == message.Substring(1, Index - 1))
                    {
                        string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                        Image image = Image.FromFile(path.Substring(0, path.Length - 9) + @"emoji\" + message.Substring(Index + 1));
                        var ms = new MemoryStream();
                        image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        var bytes = ms.ToArray();
                        var pic = new imageMessRec();
                        pic.guna2PictureBox1.Image = Image.FromStream(new MemoryStream(bytes));
                        pic.guna2CirclePictureBox1.Image = opAvt.Image;
                        pic.guna2CirclePictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                        pic.Height -= 100;
                        pic.guna2PictureBox1.Height -= 100;
                        pic.guna2PictureBox1.Width -= 140;
                        pic.guna2PictureBox1.BorderRadius = 0;
                        allMessage.Controls.Add(pic);
                        int height = pic.Location.Y + pic.Size.Height;
                        pic.Tag = height.ToString();

                        allMessage.ScrollControlIntoView(pic);
                        allMessage.AutoScrollPosition = new Point(pic.Right - allMessage.AutoScrollPosition.X,
                                                                        pic.Left - allMessage.AutoScrollPosition.Y);
                    }
                    else
                    {
                        foreach (ClientOnline item in listClientOnline)
                        {
                            if (item.lbName.Text == message.Substring(1, Index - 1))
                            {
                                item.NoRecDontSee++; item.lbCount.Text = item.NoRecDontSee.ToString(); item.lbCount.Show();
                                break;
                            }
                        }
                    }
                }));
            }
            else if (message[0] == '0')
            { //Nhận voice chat
                this.Invoke(new Action(() => {
                    int Index = message.IndexOf('@');
                    if (OpText.Text == message.Substring(1, Index - 1))
                    {
                        string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                        //Image image = Image.FromFile(path.Substring(0, path.Length - 9) + @"emoji\" + message.Substring(Index + 1));
                        var pic = new voiceMessRec();
                        pic.outFileVoceRecord = path.Substring(0, path.Length - 20) + @"\voiceRecord\" + message.Substring(Index + 1);
                        pic.Load();
                        pic.guna2CirclePictureBox1.Image = opAvt.Image;
                        pic.guna2CirclePictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                        allMessage.Controls.Add(pic);
                        int height = pic.Location.Y + pic.Size.Height;
                        pic.Tag = height.ToString();

                        allMessage.ScrollControlIntoView(pic);
                        allMessage.AutoScrollPosition = new Point(pic.Right - allMessage.AutoScrollPosition.X,
                                                                        pic.Left - allMessage.AutoScrollPosition.Y);

                    }
                    else
                    {
                        foreach (ClientOnline item in listClientOnline)
                        {
                            if (item.lbName.Text == message.Substring(1, Index - 1))
                            {
                                item.NoRecDontSee++; item.lbCount.Text = item.NoRecDontSee.ToString(); item.lbCount.Show();
                                break;
                            }
                        }
                    }
                }));
            }
            else if (message[0] == 'a')
            {//gửi tên group và danh sách các client trong group cho server
                allclie = message.Substring(1);
                FormGroup f = new FormGroup();
                f.ShowDialog();
                if (f.listCLientinGroup != "")
                {
                    string s = f.listCLientinGroup;
                    sendData(12, s);

                }
            }
            else if (message[0] == 'b')
            {
                this.Invoke(new Action(() => {
                    Thread trd = new Thread(new ThreadStart(() =>
                    {
                        string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                        string pat = path.Substring(0, path.Length - 9) + @"Avt\";
                        listGroup = new List<GroupOnline>();
                        if (message.Length > 2)
                        {
                            string m = message.Substring(1, message.Length - 2);
                            string[] listStr = m.Split(':');
                            for (int i = 0; i < listStr.Length;)
                            {
                                GroupOnline item = new GroupOnline();
                                item.idGroup = Int32.Parse(listStr[i++]);
                                item.lbName.Text = listStr[i++];
                                int noMem = Int32.Parse(listStr[i++]);
                                item.memGroup = new List<ClientOnline>();

                                for (int j = 0; j < noMem; j++)
                                {
                                    ClientOnline ite = new ClientOnline();
                                    ite.lbName.Text = listStr[i++];
                                    Image image = Image.FromFile(pat + listStr[i++]);
                                    var ms = new MemoryStream();
                                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                    var bytes = ms.ToArray();
                                    ite.avtClient.Image = Image.FromStream(new MemoryStream(bytes));
                                    item.memGroup.Add(ite);
                                }
                                item.CheckClick = 0;
                                item.NoRecDontSee = 0;
                                item.Tag = item;
                                item.Click += Click_Group;
                                item.pictureBox1.Image = item.memGroup[0].avtClient.Image;
                                item.pictureBox2.Image = item.memGroup[1].avtClient.Image;
                                listGroup.Add(item);
                                btnallGroup.Enabled = true;
                            }
                        }
                        else
                        {
                            btnallGroup.Enabled = true;
                            btnallUser.Enabled = false;
                        }
                    }));
                    trd.IsBackground = true;
                    trd.Start();

                }));
                //b1:AnhEm:3:hungmai:244644651_1188387245325270_2437869500730178936_n.jpgbababa:1200px-Premier_League_Logo.svg.pnganhem:174586777_206122801056993_1083275454970293522_n.jpg2:Djitme:5:hung:3b428fed44a72f7fa3e0a221c5c2ed1a.jpghung22:464074.jpghungmai:244644651_1188387245325270_2437869500730178936_n.jpgbababa:1200px-Premier_League_Logo.svg.pnganhem:174586777_206122801056993_1083275454970293522_n.jpg
            }
            else if (message[0] == 'c')
            {
                this.Invoke(new Action(() =>
                {
                    string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                    allMessage.Controls.Clear();
                    listMess = messInstance.Instance.LoadMessGroup(message);
                    foreach (mess item in listMess)
                    {
                        if (item.Content[0] == '0')
                        {
                            if (item.MemChat == nameCLient.Text)
                            {
                                Send f = new Send();
                                buitSizeSend(item.Content.Substring(1), f);
                                allMessage.Controls.Add(f);
                                int height = f.Location.Y + f.Size.Height;
                                f.Tag = height.ToString();
                                allMessage.ScrollControlIntoView(f);
                                item.Scrollx = f.Right - allMessage.AutoScrollPosition.X;
                                item.Scrolly = f.Left - allMessage.AutoScrollPosition.Y;
                            }
                            else
                            {
                                Recieve f = new Recieve();
                                buitSizeRec(item.Content.Substring(1), f);
                                addImageForClientMess(item.MemChat, f);
                                allMessage.Controls.Add(f);
                                int height = f.Location.Y + f.Size.Height;
                                f.Tag = height.ToString();
                                allMessage.ScrollControlIntoView(f);
                                item.Scrollx = f.Right - allMessage.AutoScrollPosition.X;
                                item.Scrolly = f.Left - allMessage.AutoScrollPosition.Y;
                            }
                        }
                        else if (item.Content[0] == '1')
                        {
                            if (item.MemChat == nameCLient.Text)
                            {
                                Image image = Image.FromFile(path.Substring(0, path.Length - 9) + @"emoji\" + item.Content.Substring(1));
                                var ms = new MemoryStream();
                                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                var bytes = ms.ToArray();
                                var pic = new ImageMessSend();
                                pic.guna2PictureBox1.Image = Image.FromStream(new MemoryStream(bytes));
                                pic.Height -= 100;
                                pic.guna2PictureBox1.Height -= 100;
                                pic.guna2PictureBox1.Width -= 140;
                                pic.guna2PictureBox1.BorderRadius = 0;
                                allMessage.Controls.Add(pic);
                                int height = pic.Location.Y + pic.Size.Height;
                                pic.Tag = height.ToString();

                            }
                            else
                            {
                                Image image = Image.FromFile(path.Substring(0, path.Length - 9) + @"emoji\" + item.Content.Substring(1));
                                var ms = new MemoryStream();
                                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                var bytes = ms.ToArray();
                                var pic = new imageMessRec();
                                pic.guna2PictureBox1.Image = Image.FromStream(new MemoryStream(bytes));
                                addImageForMessImage(item.MemChat, pic.guna2CirclePictureBox1);
                                pic.guna2CirclePictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                                pic.Height -= 100;
                                pic.guna2PictureBox1.Height -= 100;
                                pic.guna2PictureBox1.Width -= 140;
                                pic.guna2PictureBox1.BorderRadius = 0;
                                allMessage.Controls.Add(pic);
                                int height = pic.Location.Y + pic.Size.Height;
                                pic.Tag = height.ToString();

                            }
                        }
                        else if (item.Content[0] == '2')
                        {
                            if (item.MemChat != nameCLient.Text)
                            {
                                var pic = new voiceMessRec();
                                pic.outFileVoceRecord = path.Substring(0, path.Length - 20) + @"\voiceRecord\" + item.Content.Substring(1);
                                pic.Load();
                                addImageForMessImage(item.MemChat, pic.guna2CirclePictureBox1);
                                pic.guna2CirclePictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                                allMessage.Controls.Add(pic);
                                int height = pic.Location.Y + pic.Size.Height;
                                pic.Tag = height.ToString();

                                allMessage.ScrollControlIntoView(pic);
                            }
                            else
                            {
                                var pic = new voieMessSend();
                                pic.outFileVoceRecord = path.Substring(0, path.Length - 20) + @"\voiceRecord\" + item.Content.Substring(1);
                                pic.Load();
                                allMessage.Controls.Add(pic);
                                int height = pic.Location.Y + pic.Size.Height;
                                pic.Tag = height.ToString();

                                allMessage.ScrollControlIntoView(pic);
                            }
                        }
                        if (listMess.Count != 0)
                        {
                            var pic = new Recieve();
                            allMessage.Controls.Add(pic);
                            int height = pic.Location.Y + pic.Size.Height;
                            pic.Tag = height.ToString();

                            allMessage.ScrollControlIntoView(pic);
                            allMessage.AutoScrollPosition = new Point(pic.Right - allMessage.AutoScrollPosition.X,
                                                                            pic.Left - allMessage.AutoScrollPosition.Y);
                            allMessage.Controls.Remove(pic);
                        }
                    }
                }));
            }

            else if (message[0] == 'd')
            {
                this.Invoke(new Action(() =>
                {
                    string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                    string[] lism = message.Substring(1).Split(':');
                    GroupOnline addItem = new GroupOnline();
                    addItem.idGroup = Int32.Parse(lism[1]);
                    addItem.lbName.Text = lism[0];
                    addItem.memGroup = new List<ClientOnline>();
                    for (int i = 2; i < lism.Length; i += 2)
                    {
                        ClientOnline ite = new ClientOnline();
                        ite.lbName.Text = lism[i];
                        Image image = Image.FromFile(path.Substring(0, path.Length - 9) + @"Avt\" + lism[i + 1]);
                        var ms = new MemoryStream();
                        image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        var bytes = ms.ToArray();
                        ite.avtClient.Image = Image.FromStream(new MemoryStream(bytes));
                        addItem.memGroup.Add(ite);
                    }
                    addItem.CheckClick = 0;
                    addItem.NoRecDontSee = 0;
                    addItem.Tag = addItem;
                    addItem.Click += Click_Group;
                    addItem.pictureBox1.Image = addItem.memGroup[0].avtClient.Image;
                    addItem.pictureBox2.Image = addItem.memGroup[1].avtClient.Image;
                    listGroup.Add(addItem);
                    btnallUser.Enabled = true;
                    btnallGroup.Enabled = false;
                    flpListClient.Controls.Clear();
                    foreach (GroupOnline item in listGroup)
                    {
                        flpListClient.Controls.Add(item);
                    }
                }));
            }
            //Nhậm mesage
            else if (message[0] == 'e')
            {
                this.Invoke(new Action(() =>
                {
                    int index1 = message.IndexOf('@');
                    int index2 = message.IndexOf('@', index1 + 1);
                    int idGroup = Int32.Parse(message.Substring(1, index1 - 1));
                    string nameSend = "";
                    for (int i = index1 + 1; i < index2; i++)
                    {
                        nameSend += message[i];
                    }
                    string nameGroup = selecTNameGroupFromId(idGroup);
                    if (checkmessGroup_person == 1 && nameGroup == OpText.Text)
                    {
                        if (nameSend != nameCLient.Text)
                        {
                            Recieve f = new Recieve();
                            buitSizeRec(message.Substring(index2 + 1), f);
                            addImageForClientMess(nameSend, f);
                            allMessage.Controls.Add(f);
                            int height = f.Location.Y + f.Size.Height;
                            f.Tag = height.ToString();

                            allMessage.ScrollControlIntoView(f);
                            allMessage.AutoScrollPosition = new Point(f.Right - allMessage.AutoScrollPosition.X,
                                                                            f.Left - allMessage.AutoScrollPosition.Y);
                        }
                    }
                    else
                    {
                        foreach (GroupOnline item in listGroup)
                        {
                            if (item.lbName.Text == nameGroup)
                            {
                                item.NoRecDontSee++; item.lbCount.Text = item.NoRecDontSee.ToString(); item.lbCount.Show();
                                break;
                            }
                        }
                    }
                }));
            }
            //Nhận emoji group
            else if (message[0] == 'f')
            {
                this.Invoke(new Action(() =>
                {
                    int index1 = message.IndexOf('@');
                    int index2 = message.IndexOf('@', index1 + 1);
                    int idGroup = Int32.Parse(message.Substring(1, index1 - 1));
                    string nameSend = "";
                    for (int i = index1 + 1; i < index2; i++)
                    {
                        nameSend += message[i];
                    }
                    string nameGroup = selecTNameGroupFromId(idGroup);
                    if (checkmessGroup_person == 1 && nameGroup == OpText.Text)
                    {
                        if (nameSend != nameCLient.Text)
                        {
                            string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                            Image image = Image.FromFile(path.Substring(0, path.Length - 9) + @"emoji\" + message.Substring(index2 + 1));
                            var ms = new MemoryStream();
                            image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            var bytes = ms.ToArray();
                            var pic = new imageMessRec();
                            pic.guna2PictureBox1.Image = Image.FromStream(new MemoryStream(bytes));
                            addImageForMessImage(nameSend, pic.guna2CirclePictureBox1);
                            pic.guna2CirclePictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                            pic.Height -= 100;
                            pic.guna2PictureBox1.Height -= 100;
                            pic.guna2PictureBox1.Width -= 140;
                            pic.guna2PictureBox1.BorderRadius = 0;
                            allMessage.Controls.Add(pic);
                            int height = pic.Location.Y + pic.Size.Height;
                            pic.Tag = height.ToString();

                            allMessage.ScrollControlIntoView(pic);
                            allMessage.AutoScrollPosition = new Point(pic.Right - allMessage.AutoScrollPosition.X,
                                                                            pic.Left - allMessage.AutoScrollPosition.Y);
                        }
                    }
                    else
                    {
                        foreach (GroupOnline item in listGroup)
                        {
                            if (item.lbName.Text == nameGroup)
                            {
                                item.NoRecDontSee++; item.lbCount.Text = item.NoRecDontSee.ToString(); item.lbCount.Show();
                                break;
                            }
                        }
                    }
                }));
            }
            //Nhận âm thanh
            else if (message[0] == 'g')
            {
                this.Invoke(new Action(() =>
                {
                    int index1 = message.IndexOf(':');
                    int index2 = message.IndexOf(':', index1 + 1);
                    int idGroup = Int32.Parse(message.Substring(1, index1 - 1));
                    string nameSend = "";
                    for (int i = index1 + 1; i < index2; i++)
                    {
                        nameSend += message[i];
                    }
                    string nameGroup = selecTNameGroupFromId(idGroup);
                    if (checkmessGroup_person == 1 && nameGroup == OpText.Text)
                    {
                        if (nameSend != nameCLient.Text)
                        {
                            string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                            var pic = new voiceMessRec();
                            pic.outFileVoceRecord = path.Substring(0, path.Length - 20) + @"\voiceRecord\" + message.Substring(index2 + 1);
                            pic.Load();
                            addImageForMessImage(nameSend, pic.guna2CirclePictureBox1);
                            pic.guna2CirclePictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                            allMessage.Controls.Add(pic);
                            int height = pic.Location.Y + pic.Size.Height;
                            pic.Tag = height.ToString();

                            allMessage.ScrollControlIntoView(pic);
                            allMessage.AutoScrollPosition = new Point(pic.Right - allMessage.AutoScrollPosition.X,
                                                                            pic.Left - allMessage.AutoScrollPosition.Y);
                        }
                    }
                    else
                    {
                        foreach (GroupOnline item in listGroup)
                        {
                            if (item.lbName.Text == nameGroup)
                            {
                                item.NoRecDontSee++; item.lbCount.Text = item.NoRecDontSee.ToString(); item.lbCount.Show();
                                break;
                            }
                        }
                    }
                }));
            }
        }
        #endregion

        #region show error
        private void showErrorWhenServerDis() {
            MessageBox.Show("Server is disconnected", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            checkServerOn = 0;
            this.Invoke(new Action(() =>{
                allMessage.Controls.Clear();
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
        #endregion

        #region receive data from server
        //Nhận file 
        public void ReceiveFile(int receivedBytesLen,byte[] clientData) {
            try {         
                int fileNameLen = BitConverter.ToInt32(clientData, 0);
                string fileName = Encoding.UTF8.GetString(clientData, 4, fileNameLen);

                BinaryWriter bWrite = new BinaryWriter(File.Open(receivedPath1 + "/" + fileName, FileMode.Append));
                bWrite.Write(clientData, 4 + fileNameLen, receivedBytesLen - 4 - fileNameLen);
                bWrite.Close();
                if (fileName.Substring(fileName.Length - 3) == "jpg" || fileName.Substring(fileName.Length - 3) == "png") {
                    this.Invoke(new Action(() =>{
                        var pic = new imageMessRec();
                        Image image = Image.FromFile(receivedPath1+"/"+fileName);
                        var ms = new MemoryStream();
                        image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        var bytes = ms.ToArray();
                        pic.guna2PictureBox1.Image = Image.FromStream(new MemoryStream(bytes));
                        pic.guna2CirclePictureBox1.Image = opAvt.Image;
                        pic.guna2CirclePictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                        //pic.guna2Button4.Click += functionSave;
                        pic.clientData = clientData;
                        pic.receivedBytesLen = receivedBytesLen;
                        allMessage.Controls.Add(pic);
                        int height = pic.Location.Y + pic.Size.Height;
                        pic.Tag = height.ToString();

                        allMessage.ScrollControlIntoView(pic);
                        allMessage.AutoScrollPosition = new Point(pic.Right - allMessage.AutoScrollPosition.X,
                                                                        pic.Left - allMessage.AutoScrollPosition.Y);
                    }));
                }
                else {
                    this.Invoke(new Action(() =>
                    {
                        var pic = new filMessRec();
                        pic.guna2TextBox1.Text = fileName;
                        pic.receivedBytesLen = receivedBytesLen;
                        pic.clientData = clientData;
                        allMessage.Controls.Add(pic);
                        int height = pic.Location.Y + pic.Size.Height;
                        pic.Tag = height.ToString();

                        allMessage.ScrollControlIntoView(pic);
                        allMessage.AutoScrollPosition = new Point(pic.Right - allMessage.AutoScrollPosition.X,
                                                                        pic.Left - allMessage.AutoScrollPosition.Y);
                    }));
                }
            }
            catch {
                MessageBox.Show("File receive error", "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);    
            }
        }
        //Tải ảnh ve
        
        private void ReceiveMessage()
        {
            try{
                while (true){
                    byte[] buffer = new byte[1024 * 5000];
                    int rec = client.Receive(buffer);
                    if (buffer[0] != 11 && buffer[0] != 12) {
                        string mess = (String)Deserialize(buffer);
                        CheckMessage(mess);
                    }
                    else if(buffer[0] == 12) {
                        string mess = (String)Deserialize(buffer);
                        checkMessage1(mess);
                    }
                    else if (buffer[0] == 11){
                        byte[] data = new byte[1024 * 5000];
                        for(int i = 1; i < 1024 * 5000; i++) {
                            data[i - 1] = buffer[i];
                        }
                        ReceiveFile(rec - 1, data);
                    }
                    else if(buffer[0] == 13)
                    {
                        if(checkmessGroup_person == 1 && OpText.Text != string.Empty)
                        {
                            byte[] data = new byte[1024 * 5000];
                            for (int i = 1; i < 1024 * 5000; i++)
                            {
                                data[i - 1] = buffer[i];
                            }
                            ReceiveFile(rec - 1, data);
                        }
                    }
                }
            }
            catch
            {
                showErrorWhenServerDis();
            }
        }
        #endregion

        #region xu ly group
        private string selecTNameGroupFromId(int id)
        {
            string name = "";
            foreach(GroupOnline ite in listGroup)
            {
                if (id == ite.idGroup)
                {
                    name = ite.lbName.Text;
                    break;
                }
            }
            return name;
        }
        private void addImageForClientMess(string MemChat,Recieve f) {
            foreach (GroupOnline ite in listGroup)
            {
                if (ite.lbName.Text == OpText.Text)
                {
                    foreach (ClientOnline it in ite.memGroup)
                    {
                        if (it.lbName.Text == MemChat)
                        {
                            f.guna2CirclePictureBox1.Image = it.avtClient.Image;
                            f.guna2CirclePictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                            break;
                        }
                    }
                    break;
                }
            }
        }
        private void addImageForMessImage(string MemChat,Guna.UI2.WinForms.Guna2CirclePictureBox f)
        {
            foreach (GroupOnline ite in listGroup)
            {
                if (ite.lbName.Text == OpText.Text)
                {
                    foreach (ClientOnline it in ite.memGroup)
                    {
                        if (it.lbName.Text == MemChat)
                        {
                            f.Image = it.avtClient.Image;
                            f.SizeMode = PictureBoxSizeMode.StretchImage;
                            break;
                        }
                    }
                    break;
                }
            }
        }        
        private void ClientOnline_Click(object sender, EventArgs e)
        {
            ClientOnline ite = (sender as ClientOnline).Tag as ClientOnline;
            //string s = (sender as ClientOnline).Tag as string;
            OpText.Text = ite.lbName.Text;
            EnableImagePerson();
            opAvt.Show();
            opAvt.Image = ite.avtClient.Image;
            foreach(ClientOnline item in listClientOnline) { 
                if(item.lbName.Text == ite.lbName.Text) {
                    item.BackColor = Color.FromArgb(232,243,254);
                    item.CheckClick = 1;
                    item.lbName.BackColor = Color.FromArgb(232, 243, 254);
                    item.NoRecDontSee = 0; item.lbCount.Hide();
                }
                else
                {
                    item.CheckClick = 0;
                    item.BackColor = DefaultBackColor;
                    item.lbName.BackColor = DefaultBackColor;
                }
                
            }
            //client.Send(Serialize($"6{nameCLient.Text}@{s}"));
            sendData(6, $"6{nameCLient.Text}@{ite.lbName.Text}");
        }
         //set image for optext
        private void EnableImagePerson() {
            imageGroup.Hide();
            halfPic1.Hide();
            halfpic2.Hide();
        }
        
        private void Click_Group(object sender, EventArgs e){
            GroupOnline ite = (sender as GroupOnline).Tag as GroupOnline;
            OpText.Text = ite.lbName.Text;
            foreach(GroupOnline item in listGroup) { 
                if(item.lbName.Text == ite.lbName.Text) {
                    item.BackColor = Color.FromArgb(232,243,254);
                    item.CheckClick = 1;
                    item.lbName.BackColor = Color.FromArgb(232, 243, 254);
                    item.NoRecDontSee = 0; item.lbCount.Hide();
                }
                else
                {
                    item.CheckClick = 0;
                    item.BackColor = DefaultBackColor;
                    item.lbName.BackColor = DefaultBackColor;
                }
                
            }
            opAvt.Show();
            imageGroup.Show();
            halfPic1.Show();
            halfpic2.Show();
            halfPic1.Image = ite.memGroup[0].avtClient.Image;
            halfpic2.Image = ite.memGroup[1].avtClient.Image;
            sendData(12,$"4{nameCLient.Text}@{ite.idGroup}");
        }
        #endregion
        
        #region xu ly emoji
        private void LoadListView(){
            allEmoji.Controls.Clear();
            string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            for(int i = 1; i <= 25; i++) {
                Image image = Image.FromFile(path.Substring(0, path.Length - 9) + @"emoji\"+$"{i.ToString()}.png");
                var ms = new MemoryStream();
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                var bytes = ms.ToArray();
                Guna.UI2.WinForms.Guna2Button f = new Guna.UI2.WinForms.Guna2Button() { Width = 60,Height=60};
                f.Image = Image.FromStream(new MemoryStream(bytes));
                f.BackColor = Color.Transparent;
                f.Tag = i.ToString();
                f.Click += EmojiClick;
                f.ImageSize = new Size(60, 60);
                f.FillColor = Color.Transparent;
                allEmoji.Controls.Add(f);
            }
        }
        //Emoji click 
        private void EmojiClick(object sender, EventArgs e){
            string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string name = ((sender as Guna.UI2.WinForms.Guna2Button).Tag as string);
            Image image = Image.FromFile(path.Substring(0, path.Length - 9) + @"emoji\"+$"{name}.png");
            var ms = new MemoryStream();
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            var bytes = ms.ToArray();
            var pic = new ImageMessSend();
            pic.guna2PictureBox1.Image = Image.FromStream(new MemoryStream(bytes));
            pic.Height -= 100;
            pic.guna2PictureBox1.Height -= 100;
            pic.guna2PictureBox1.Width -= 140;
            if (this.InvokeRequired)
            {
                this.BeginInvoke((MethodInvoker)delegate ()
                {
                    allMessage.Controls.Add(pic);
                    int height = pic.Location.Y + pic.Size.Height;
                    pic.Tag = height.ToString();

                    allMessage.ScrollControlIntoView(pic);
                    allMessage.AutoScrollPosition = new Point(pic.Right - allMessage.AutoScrollPosition.X,
                                                                    pic.Left - allMessage.AutoScrollPosition.Y);
                });
            }
            else
            {
                allMessage.Controls.Add(pic);
                int height = pic.Location.Y + pic.Size.Height;
                pic.Tag = height.ToString();

                allMessage.ScrollControlIntoView(pic);
                allMessage.AutoScrollPosition = new Point(pic.Right - allMessage.AutoScrollPosition.X,
                                                                pic.Left - allMessage.AutoScrollPosition.Y);
            }
            allEmoji.Hide();
            if (checkmessGroup_person == 0)
            {
                sendData(7, $"{OpText.Text}@{name}.png");
            }
            else if(checkmessGroup_person == 1)
            {
                foreach (GroupOnline item in listGroup)
                {
                    if (item.lbName.Text == OpText.Text)
                    {
                        sendData(12, $"6{nameCLient.Text}@{item.idGroup.ToString()}@{name}.png");
                        break;
                    }
                }
            }
        }
        private void guna2Button4_Click(object sender, EventArgs e)
        {
            if (OpText.Text != string.Empty)
            {
                allEmoji.Show();
            }
        }
        #endregion

        #region changeData
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
        #endregion

        #region Sgin In Sign Un
        private void btnSignIn_Click(object sender, EventArgs e)
        {
            if(!string.IsNullOrEmpty(Username.Text) && !string.IsNullOrEmpty(Password.Text)){
                sendData(1, $"1{Username.Text}@{Password.Text}");
                //client.Send(Serialize($"1{Username.Text}@{Password.Text}"));
            }
            else{
                MessageBox.Show("Username or password can't be empty");
            }
        }
        private void btnRegister_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(UsernameRegister.Text) && !string.IsNullOrEmpty(PasswordRegister.Text) && PasswordRegister.Text == RePassRegister.Text){
                if(Avt.Image != null) { 
                    //sendData(2, $"2{UsernameRegister.Text}@{PasswordRegister.Text}");
                    sendFile(fileNamePath, $"{UsernameRegister.Text}@{PasswordRegister.Text}", 2);
                    //client.Send(Serialize($"2{UsernameRegister.Text}@{PasswordRegister.Text}"));
                }
                else {
                    MessageBox.Show("Please choose Avt for Account", "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else{
                MessageBox.Show("Username or password can't be empty");
            }
        }
        private void btnDisConnect_Click(object sender, EventArgs e)
        {
            if (checkServerOn == 1){
                if (name != ""){
                    sendData(3, $"3{name}");
                    //client.Send(Serialize($"3{name}"));
                    client.Close();
                    Application.Exit();
                }
                else{
                    sendData(3, $"3{name}");
                    //client.Send(Serialize($"3{name}"));
                    client.Close();
                    Application.Exit();
                }
            }
        }
        private void guna2ControlBox1_Click(object sender, EventArgs e)
        {
            if (checkServerOn == 1)
            {
                if (name != "")
                {
                    sendData(3, $"3{name}");
                    //client.Send(Serialize($"3{name}"));
                    client.Close();
                    t.Stop();
                    Application.Exit();
                }
                else
                {
                    sendData(3, $"3{name}");
                    //client.Send(Serialize($"3{name}"));
                    client.Close();
                    t.Stop();
                    Application.Exit();
                }
            }
        }
        #endregion

        #region send Mess
        //Gửi message
        private void messageText_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter) { 
                if (!string.IsNullOrEmpty(messageText.Text)) {
                    this.Invoke(new Action(() =>
                    {
                        if(checkmessGroup_person == 0) 
                            sendData(5, $"5{OpText.Text}@{messageText.Text}");
                        else if(checkmessGroup_person == 1) { 
                            foreach(GroupOnline item in listGroup) { 
                                if(item.lbName.Text == OpText.Text) {
                                    sendData(12, $"5{nameCLient.Text}@{item.idGroup.ToString()}@{messageText.Text}");
                                    break;
                                }
                            }
                        }
                        //client.Send(Serialize($"5{OpText.Text}@{messageText.Text}"));
                        var pic = new Send();
                        buitSizeSend(messageText.Text, pic);
                        allMessage.Controls.Add(pic);
                        int height = pic.Location.Y + pic.Size.Height;
                        pic.Tag = height.ToString();

                        allMessage.ScrollControlIntoView(pic);
                        allMessage.AutoScrollPosition = new Point(pic.Right - allMessage.AutoScrollPosition.X,
                                                                        pic.Left - allMessage.AutoScrollPosition.Y);
                        messageText.Text = "";
                    }));
                }
            }
        }

        //Gửi file
        public void sendFile(string fileName,string s,byte header) {
            try
            {
                string filePath = "";
                fileName = fileName.Replace("\\", "/");
                while (fileName.IndexOf("/") > -1)
                {
                    filePath += fileName.Substring(0, fileName.IndexOf("/") + 1);
                    fileName = fileName.Substring(fileName.IndexOf("/") + 1);
                }
                byte[] fileNameByte = Encoding.UTF8.GetBytes(fileName);
                if (fileNameByte.Length > (50 * 1024-100)){
                    MessageBox.Show("File size is more than 5Mb,please try with small file ","Message",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                    return;
                }
                string fullPath = filePath + fileName;
                byte[] fileData = File.ReadAllBytes(fullPath);
                byte[] clientData = new byte[4 + fileNameByte.Length + fileData.Length];
                byte[] fileNameLen = BitConverter.GetBytes(fileNameByte.Length);
                fileNameLen.CopyTo(clientData, 0);
                fileNameByte.CopyTo(clientData, 4);
                fileData.CopyTo(clientData, 4 + fileNameByte.Length);
                //Thêm Header vào cho việc gửi size
                byte[] send = new byte[clientData.Length + s.Length+2];
                send[0] = header;
                send[1] = (byte)s.Length;
                for(int i = 0; i < s.Length; i++) { 
                    send[i+2]=Convert.ToByte(s[i]);
                }
                int k = s.Length+2;
                for (int i = 0; i < clientData.Length; i++)
                { send[k] = clientData[i];k++; }
                client.Send(send, 0, send.Length, 0);
            }
            catch(Exception ex) {
                if (ex.Message == "No connection could be made because the target machine actively refused it")
                    MessageBox.Show("File Sending fail. Because server not running.","Message",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                else
                    MessageBox.Show("File Sending fail." + ex.Message,"Message",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }
        //Gửi file
        private void guna2Button3_Click(object sender, EventArgs e){
            if (OpText.Text != string.Empty)
            {
                Thread t = new Thread((ThreadStart)(() =>
                {
                    FileDialog fd = new OpenFileDialog();
                    if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string fileName = fd.FileName;
                        fileName = fileName.Replace("\\", "/");
                        while (fileName.IndexOf("/") > -1)
                        {
                            fileName = fileName.Substring(fileName.IndexOf("/") + 1);
                        }
                        FilMessSend pic = new FilMessSend();
                        pic.guna2TextBox1.Text = fileName;
                        if (this.InvokeRequired)
                        {
                            this.BeginInvoke((MethodInvoker)delegate ()
                            {
                                allMessage.Controls.Add(pic);
                                int height = pic.Location.Y + pic.Size.Height;
                                pic.Tag = height.ToString();

                                allMessage.ScrollControlIntoView(pic);
                                allMessage.AutoScrollPosition = new Point(pic.Right - allMessage.AutoScrollPosition.X,
                                                                                pic.Left - allMessage.AutoScrollPosition.Y);
                            });
                        }
                        else
                        {
                            allMessage.Controls.Add(pic);
                            int height = pic.Location.Y + pic.Size.Height;
                            pic.Tag = height.ToString();

                            allMessage.ScrollControlIntoView(pic);
                            allMessage.AutoScrollPosition = new Point(pic.Right - allMessage.AutoScrollPosition.X,
                                                                            pic.Left - allMessage.AutoScrollPosition.Y);
                        }
                        if (checkmessGroup_person == 0)
                        {
                            sendFile(fd.FileName, OpText.Text, 11);
                        }
                        else if (checkmessGroup_person == 1)
                        {
                            sendFile(fd.FileName, OpText.Text, 13);
                        }
                    }
                }));
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
                t.Join();
            }
        }
        //GỬi  hình ảnh
        private void guna2Button2_Click(object sender, EventArgs e)
        {
            if (OpText.Text != string.Empty)
            {
                Thread t = new Thread((ThreadStart)(() =>
                {
                    OpenFileDialog OD = new OpenFileDialog();
                    OD.FileName = "";
                    OD.Filter = "Supported Images |*.jpg;*.jpeg;*.png";
                    if (OD.ShowDialog() == DialogResult.OK)
                    {
                        var pic = new ImageMessSend();
                        pic.guna2PictureBox1.Load(OD.FileName);
                        pic.guna2PictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                        if (this.InvokeRequired)
                        {
                            this.BeginInvoke((MethodInvoker)delegate ()
                            {
                                allMessage.Controls.Add(pic);
                                int height = pic.Location.Y + pic.Size.Height;
                                pic.Tag = height.ToString();

                                allMessage.ScrollControlIntoView(pic);
                                allMessage.AutoScrollPosition = new Point(pic.Right - allMessage.AutoScrollPosition.X,
                                                                                pic.Left - allMessage.AutoScrollPosition.Y);
                            });
                        }
                        else
                        {
                            allMessage.Controls.Add(pic);
                            int height = pic.Location.Y + pic.Size.Height;
                            pic.Tag = height.ToString();

                            allMessage.ScrollControlIntoView(pic);
                            allMessage.AutoScrollPosition = new Point(pic.Right - allMessage.AutoScrollPosition.X,
                                                                            pic.Left - allMessage.AutoScrollPosition.Y);
                        }
                        if (checkmessGroup_person == 0)
                        {
                            sendFile(OD.FileName, OpText.Text, 11);
                        }
                        else if(checkmessGroup_person == 1)
                        {
                            sendFile(OD.FileName, OpText.Text, 13);
                        }
                    }
                }));
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
                t.Join();
            }
        }


        //Gửi like
        private void guna2Button1_Click(object sender, EventArgs e)
        {
            //Nhận đường link đến file Debug
            if (OpText.Text != string.Empty)
            {
                string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                Image image = Image.FromFile(path.Substring(0, path.Length - 9) + @"emoji\thumb-up.png");
                var ms = new MemoryStream();
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                var bytes = ms.ToArray();
                var pic = new ImageMessSend();
                pic.guna2PictureBox1.Image = Image.FromStream(new MemoryStream(bytes));
                pic.Height -= 100;
                pic.guna2PictureBox1.Height -= 100;
                pic.guna2PictureBox1.Width -= 140;
                pic.guna2PictureBox1.BorderRadius = 0;
                if (this.InvokeRequired)
                {
                    this.BeginInvoke((MethodInvoker)delegate ()
                    {
                        allMessage.Controls.Add(pic);
                        int height = pic.Location.Y + pic.Size.Height;
                        pic.Tag = height.ToString();

                        allMessage.ScrollControlIntoView(pic);
                        allMessage.AutoScrollPosition = new Point(pic.Right - allMessage.AutoScrollPosition.X,
                                                                        pic.Left - allMessage.AutoScrollPosition.Y);
                    });
                }
                else
                {
                    allMessage.Controls.Add(pic);
                    int height = pic.Location.Y + pic.Size.Height;
                    pic.Tag = height.ToString();

                    allMessage.ScrollControlIntoView(pic);
                    allMessage.AutoScrollPosition = new Point(pic.Right - allMessage.AutoScrollPosition.X,
                                                                    pic.Left - allMessage.AutoScrollPosition.Y);
                }
                if (checkmessGroup_person == 0)
                {
                    sendData(7, $"{OpText.Text}@thumb-up.png");
                }
                else if(checkmessGroup_person == 1)
                {
                    foreach (GroupOnline item in listGroup)
                    {
                        if (item.lbName.Text == OpText.Text)
                        {
                            sendData(12, $"6{nameCLient.Text}@{item.idGroup.ToString()}@thumb-up.png");
                            break;
                        }
                    }
                }
            }
        }
        #endregion

        #region add Avt for Account
        //Thêm Avt khi đăng kí
        private void btnAdd_Click(object sender, EventArgs e)
        {
            Thread t = new Thread((ThreadStart)(() => {
                OpenFileDialog OD = new OpenFileDialog();
                OD.FileName = "";
                OD.Filter = "Supported Images |*.jpg;*.jpeg;*.png";
                if (OD.ShowDialog() == DialogResult.OK){
                    choseAvt = 1;
                    Avt.Load(OD.FileName);
                    Avt.SizeMode = PictureBoxSizeMode.StretchImage;
                    fileNamePath = OD.FileName;
                }
            }));
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
        }
        
        //voiceRecord
        private void guna2Button5_Click(object sender, EventArgs e) {
            if (OpText.Text != string.Empty)
            {
                this.Invoke(new Action(() =>
                {
                    recordVoice.Show();
                    s = 30;
                    t.Start();
                    getVoice();
                }));
            }
        }
        #endregion

        #region voice
        private void getVoice() {
            DirectoryInfo d = new DirectoryInfo(outFileVoceRecord.Substring(0, outFileVoceRecord.Length - 20) + @"\voiceRecord"); //Assuming Test is your Folder
            FileInfo[] Files = d.GetFiles("*.mp3"); //Getting Text files
            outputFileName = outFileVoceRecord.Substring(0, outFileVoceRecord.Length - 20) + @"\voiceRecord\" + $"{(Files.Length + 1).ToString()}.mp3";
            nameVoice = (Files.Length + 1).ToString()+".mp3";
            wave = new WaveIn();
            wave.WaveFormat = new WaveFormat(44100, 1);
            wave.DeviceNumber = 1;
            wave.DataAvailable += Wave_DataAvailable;
            wave.RecordingStopped += Wave_RecordingStopped;
            writer = new WaveFileWriter(outputFileName, wave.WaveFormat);
            wave.StartRecording();
        }

        private void Wave_RecordingStopped(object sender, StoppedEventArgs e){
            writer.Dispose();
        }

        private void Wave_DataAvailable(object sender, WaveInEventArgs e){
            writer.Write(e.Buffer, 0, e.BytesRecorded);
        }

        private void guna2Button6_Click(object sender, EventArgs e){
            stopRecording();
        }
        private void stopRecording() { 
            this.Invoke(new Action(() =>{
                wave.StopRecording();
                if (outputFileName == null)
                    return;
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = Path.GetDirectoryName(outputFileName),
                    UseShellExecute = true
                };
                Process.Start(processStartInfo);
                sendVoice();
                recordVoice.Hide();
                var pic = new voieMessSend();
                pic.outFileVoceRecord = outputFileName;
                pic.Load();
                allMessage.Controls.Add(pic);
                int height = pic.Location.Y + pic.Size.Height;
                pic.Tag = height.ToString();

                allMessage.ScrollControlIntoView(pic);
                allMessage.AutoScrollPosition = new Point(pic.Right - allMessage.AutoScrollPosition.X,
                                                                pic.Left - allMessage.AutoScrollPosition.Y);
            }));
        }
        private void sendVoice() {
            if (checkmessGroup_person == 0)
            {
                sendData(8, $"{OpText.Text}:{nameVoice}");
            }
            else if(checkmessGroup_person == 1)
            {
                foreach (GroupOnline item in listGroup)
                {
                    if (item.lbName.Text == OpText.Text)
                    {
                        sendData(12, $"7{nameCLient.Text}:{item.idGroup.ToString()}:{nameVoice}");
                        break;
                    }
                }
            }
        }
        #endregion

        #region event click 3
        private void guna2Button8_Click(object sender, EventArgs e){
            this.Invoke(new Action(() => {
                checkmessGroup_person = 0;
                flpListClient.Controls.Clear();
                foreach(ClientOnline item in listClientOnline) { 
                    flpListClient.Controls.Add(item);
                }
               
                btnallUser.Enabled = false;
                btnallGroup.Enabled = true;
                allMessage.Controls.Clear();
                OpText.Text = string.Empty;
                EnableImagePerson();
                opAvt.Hide();
                guna2Button2.Enabled = true;
                guna2Button3.Enabled = true;
                viewAllMemGroup.Hide();
            }));
        }
        private void guna2Button7_Click(object sender, EventArgs e){
            this.Invoke(new Action(() => {
                checkmessGroup_person = 1;
                flpListClient.Controls.Clear();
                btnallGroup.Enabled = false;
                btnallUser.Enabled = true;
                foreach (GroupOnline item in listGroup.ToList()){
                    flpListClient.Controls.Add(item);
                }
                allMessage.Controls.Clear();
                OpText.Text = string.Empty;
                EnableImagePerson();
                opAvt.Hide();
                guna2Button2.Enabled = false;
                guna2Button3.Enabled = false;
                viewAllMemGroup.Show();
            }));
        }

        private void metroTabControl1_Click(object sender, EventArgs e){

        }
        
        private void addMemGroup_Click(object sender, EventArgs e){
            this.Invoke(new Action(() =>
            {
                foreach(GroupOnline item in listGroup)
                {
                    if(item.lbName.Text == OpText.Text)
                    {
                        viewMem f = new viewMem();
                        f.allClient = item.memGroup;
                        f.ShowDialog();
                        break;
                    }
                }
            }));
        }

        private void guna2CircleButton3_Click(object sender, EventArgs e)
        {
            this.Invoke(new Action(() => {
                sendData(12, "1group");
            }));
        }

        private void tbSearch_TextChanged(object sender, EventArgs e)
        {
            flpListClient.Controls.Clear();
            if(checkmessGroup_person == 0) { 
                foreach (ClientOnline item in listClientOnline)
                {
                    if (item.lbName.Text.Contains(tbSearch.Text))
                    {
                        flpListClient.Controls.Add(item);
                    }
                }
            }
            else if(checkmessGroup_person == 1)
            {
                foreach(GroupOnline item in listGroup)
                {
                    if(item.lbName.Text.Contains((tbSearch.Text)))
                    {
                        flpListClient.Controls.Add(item);
                    }
                }
            }
        }

        private void btnConnectServer_Click(object sender, EventArgs e)
        {
            threadConnectServer = new Thread(new ThreadStart(ConnectServer));
            threadConnectServer.IsBackground = true;
            threadConnectServer.Start();
        }
        #endregion

        #region findeMessage in chat
        int index = 0;
        List<KeyValuePair<int,int>> messageSearch = new List<KeyValuePair<int,int>>();
        private void tbSearchMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (!string.IsNullOrEmpty(tbSearchMessage.Text))
                {
                    FindMessage();
                    messageSearch = new List<KeyValuePair<int, int>>();index = 0;
                    foreach(mess item in listMess)
                    {
                        if (item.Content.Contains(tbSearchMessage.Text) && item.Content[0] == '0')
                        {
                            messageSearch.Add(new KeyValuePair<int, int>(item.Scrollx, item.Scrolly));
                        }
                    }
                    if (messageSearch.Count > 0)
                    {
                        allMessage.AutoScrollPosition = new Point(messageSearch[0].Key, messageSearch[0].Value);
                        //MessageBox.Show($"{messageSearch[0].Key}:{messageSearch[0].Value}", "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        highLightText();
                    }
                }
            }
        }
        
        private void highLightText()
        {
            for(int i = 0; i < listMess.Count; i++)
            {
                if(listMess[i].Scrollx == messageSearch[index].Key && listMess[i].Scrolly == messageSearch[index].Value && listMess[i].Content[0]=='0' && listMess[i].Content.Contains(tbSearchMessage.Text))
                {
                    Control c = allMessage.Controls[i];

                    if (c.GetType() == typeof(Recieve))
                    {
                        Recieve rec = (Recieve)c;
                        rec.label1.BackColor = Color.Yellow;
                    }

                    if (c.GetType() == typeof(Send))
                    {
                        Send send = (Send)c;
                        send.label1.BackColor = Color.Yellow;    
                    }
                }
                else
                {
                    Control c = allMessage.Controls[i];

                    if (c.GetType() == typeof(Recieve))
                    {
                        Recieve rec = (Recieve)c;
                        rec.label1.BackColor = Color.Transparent;
                    }

                    if (c.GetType() == typeof(Send))
                    {
                        Send send = (Send)c;
                        send.label1.BackColor = Color.Transparent;
                    }
                }
            }
        }
        int dem = 0;
        private void FindMessage()
        {
            dem = 0;   
            foreach(mess item in listMess)
            {
                if (item.Content[0] == '0')
                {
                    if (item.Content.Contains(tbSearchMessage.Text))
                    {
                        dem++;
                    }
                }
            }
            
            lbCount.Text = "1/"+dem.ToString() + " results";
            lbCount.Visible = true;
            if (dem > 1)
            {
                btnUp.Visible = true;
                btnDown.Visible = true;
                btnUp.Enabled = true;
            }
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            btnDown.Enabled = true;
            if(index > 0)
            {
                index--;
                lbCount.Text = $"{(index+1).ToString()}/{dem.ToString()} results";
                allMessage.AutoScrollPosition = new Point(messageSearch[index].Key, messageSearch[index].Value);
                highLightText();

            }
        }

        private void guna2CircleButton3_Click_1(object sender, EventArgs e)
        {
            pnlFIndMessage.Visible = true;
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            if(index < messageSearch.Count - 1)
            {
                index++;
                lbCount.Text = $"{(index+1).ToString()}/{dem.ToString()} results";
                allMessage.AutoScrollPosition = new Point(messageSearch[index].Key, messageSearch[index].Value);
                highLightText();
            }
        }

        private void guna2Button7_Click_1(object sender, EventArgs e)
        {
            tbSearchMessage.Clear();
            btnDown.Visible = false;
            btnUp.Visible = false;
            lbCount.Text = "";
            lbCount.Visible = false;
            pnlFIndMessage.Visible = false;
            index = 0;
            messageSearch.Clear();
            for (int i = 0; i < listMess.Count; i++)
            {
                Control c = allMessage.Controls[i];

                if (c.GetType() == typeof(Recieve))
                {
                    Recieve rec = (Recieve)c;
                    rec.label1.BackColor = Color.Transparent;
                }

                if (c.GetType() == typeof(Send))
                {
                    Send send = (Send)c;
                    send.label1.BackColor = Color.Transparent;
                }
            }
        }
        #endregion
    }
}
//end project
