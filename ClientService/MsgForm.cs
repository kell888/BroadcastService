using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Data.Common;
using System.IO;
using System.Threading;
using System.Configuration;
using CommonLib;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;

namespace ClientService
{
    public partial class MsgForm : Form
    {
        private MsgForm()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            bool flag = CommunicationService.Start(this);
            if (flag)
                Logs.Info("服务启动成功！");
            else
                Logs.Info("服务启动失败！");
        }

        static MsgForm instance;
        /// <summary>
        /// 保存当前类只有一个实例在运行（单件模式）
        /// </summary>
        /// <returns></returns>
        public static MsgForm GetInstance()
        {
            if (instance == null)
                instance = new MsgForm();
            return instance;
        }

        delegate void ShowHandler();
        /// <summary>
        /// 可以跨线程显示窗体
        /// </summary>
        public void ShowCrossThread()
        {
            if (this.InvokeRequired)
                this.Invoke(new ShowHandler(this.Show));
            else
                this.Show();
        }

        bool sure;
        /// <summary>
        /// 设置消息来源
        /// </summary>
        public void SetSource(string value)
        {
            try
            {
                lblSystem.Text = value;
                if (!timer1.Enabled)
                    timer1.Start();
            }
            catch { }
        }

        public void SetLastTime(DateTime time)
        {
            try
            {
                lblTime.Text = time.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch { }
        }
        /// <summary>
        /// 添加消息
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="client"></param>
        public void AddMessage(MsgClass msg, EndPoint client)
        {
            try
            {
                Button btn = new Button();
                btn.AutoEllipsis = true;
                btn.FlatStyle = FlatStyle.Popup;
                btn.Cursor = Cursors.Hand;
                btn.TextAlign = ContentAlignment.MiddleCenter;
                btn.Size = new System.Drawing.Size(100, 30);
                if (!string.IsNullOrEmpty(msg.Message))
                    btn.Text = msg.Message;
                else
                    btn.Text = "【新消息" + Convert.ToString(panel2.Controls.Count + 1) + "】";
                btn.Tag = new EndPointWrap(client, msg);
                btn.Click += new EventHandler(btn_Click);
                if (this.InvokeRequired)
                {
                    this.Invoke(new MethodInvoker(delegate { this.panel2.Controls.Add(btn); }));
                }
                else
                {
                    this.panel2.Controls.Add(btn);
                }
                LayoutPanel2();
            }
            catch (Exception e) { MessageBox.Show(e.Message); }
        }

        void btn_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            EndPointWrap epw = btn.Tag as EndPointWrap;
            if (epw != null)
            {
                MsgInfo msg = new MsgInfo(this, btn);
                msg.ShowDialog();
                //CommunicationService.Send(epw.EndPoint, Common.HadReaded);
            }
        }

        public void Remove(Button btn)
        {
            panel2.Controls.Remove(btn);
            LayoutPanel2();
            if (panel2.Controls.Count == 0)
            {
                timer1.Stop();
                notifyIcon1.Icon = ClientService.Properties.Resources.msg;
            }
        }

        public void LayoutPanel2()
        {
            int i = 0;
            foreach (Control c in panel2.Controls)
            {
                int offsetX = (c.Width + 4) * (i % 3);
                int offsetY = (c.Height + 4) * (i / 3);
                int x = 4 + offsetX;
                int y = 4 + offsetY;
                c.Location = new Point(x, y);
                i++;
            }
        }

        private void 查看消息ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowMessage();
        }

        public void ShowMessage()
        {
            this.WindowState = FormWindowState.Normal;
            this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Right - this.Width, Screen.PrimaryScreen.WorkingArea.Bottom - this.Height);
            this.ShowInTaskbar = true;
            if (!this.Visible)
                this.Show();
            this.BringToFront();
            this.Focus();
        }

        private void 退出服务ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sure = true;
            this.Close();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (sure)
            {
                if (MessageBox.Show("确定要退出本服务吗？", "退出提醒", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.OK)
                {
                    SaveUnreadMessages();
                    CommunicationService.Stop();
                    Environment.Exit(0);
                }
                else
                {
                    sure = false;
                    e.Cancel = true;
                }
            }
            else
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
            }
        }

        private void SaveUnreadMessages()
        {
            try
            {
                List<EndPointWrap> ms = new List<EndPointWrap>();
                foreach (Control c in panel2.Controls)
                {
                    Button btn = c as Button;
                    EndPointWrap epw = btn.Tag as EndPointWrap;
                    if (epw != null)
                    {
                        ms.Add(epw);
                    }
                }
                BinaryFormatter bf = new BinaryFormatter();
                using (FileStream fs = new FileStream("unread.bin", FileMode.Create, FileAccess.Write))
                {
                    bf.Serialize(fs, ms);
                }
            }
            catch (Exception e)
            {
                Logs.Error("保存当前未读消息时出错" + e.Message);
            }
        }

        private List<EndPointWrap> LoadLastUnreadMessages()
        {
            List<EndPointWrap> ms = new List<EndPointWrap>();
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                using (FileStream fs = new FileStream("unread.bin", FileMode.OpenOrCreate, FileAccess.Read))
                {
                    if (fs.Length > 0)
                    {
                        ms = (List<EndPointWrap>)bf.Deserialize(fs);
                    }
                }
            }
            catch (Exception e)
            {
                Logs.Error("载入上次未读消息时出错" + e.Message);
            }
            return ms;
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowMessage();
        }

        private void MsgForm_Shown(object sender, EventArgs e)
        {
            this.Hide();
            List<EndPointWrap> ms = LoadLastUnreadMessages();
            foreach (EndPointWrap m in ms)
            {
                AddMessage(m.Message, m.EndPoint);
            }
            if (ms.Count > 0)
            {
                EndPointWrap m = ms[ms.Count - 1];
                SetSource(m.Message.Source);
                SetLastTime(m.Message.Time);
            }
            if (ms.Count > 0)
            {
                timer1.Start();
            }
        }

        bool i;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (i)
                notifyIcon1.Icon = ClientService.Properties.Resources.msg;
            else
                notifyIcon1.Icon = ClientService.Properties.Resources.empty;
            i = !i;
        }
    }
}
