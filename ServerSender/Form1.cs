using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Configuration;
using CommonLib;

namespace ServerSender
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        Client<BroadcastEvent> send;
        //Client<MessageBody> send;

        private void Form1_Load(object sender, EventArgs e)
        {
            send = new Client<BroadcastEvent>();
            //send = new Client<MessageBody>();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (send.SharedObject != null)
                {
                    send.SharedObject.Broadcast(new BroadcastEventArgs(new IPEndPoint(Dns.GetHostAddresses("")[0], 8888), Common.GetData(new MsgClass(textBox2.Text, richTextBox1.Text, textBox3.Text.Trim()))));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
