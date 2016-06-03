using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Configuration;
using System.Diagnostics;
using CommonLib;

namespace ClientService
{
    public partial class MsgInfo : Form
    {
        public MsgInfo(MsgForm owner, Button btn)
        {
            InitializeComponent();
            this.owner = owner;
            this.btn = btn;
            EndPointWrap epw = btn.Tag as EndPointWrap;
            if (epw != null)
            {
                this.Text = "消息详情 - " + epw.Message.Source + "  时间：" +　epw.Message.Time.ToString("yyyy-MM-dd HH:mm:ss");
                this.lblMsg.Text = epw.Message.Message;
                this.SetLink(epw.Message.Link);
            }
        }

        MsgForm owner;
        Button btn;

        private void SetLink(string value)
        {
            linkLabel1.Text = value;
            int len = value.Length;
            if (linkLabel1.Links.Count == 0)
                linkLabel1.Links.Add(new LinkLabel.Link(0, len, value));
            else
                linkLabel1.Links[0] = new LinkLabel.Link(0, len, value);
        }

        private void MsgInfo_FormClosing(object sender, FormClosingEventArgs e)
        {
            string loop = ConfigurationManager.AppSettings["loop"];
            bool once = string.IsNullOrEmpty(loop) || loop == "0";
            if (once && MessageBox.Show("确定要关闭消息详情窗口吗？", "关闭提醒", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != System.Windows.Forms.DialogResult.OK)
            {
                e.Cancel = true;
                return;
            }
            if (once)
            {
                if (MessageBox.Show("是否要保留该消息以便再次查看？", "友情帮助", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != System.Windows.Forms.DialogResult.OK)
                {
                    if (owner.InvokeRequired)
                        owner.Invoke(new MethodInvoker(delegate { owner.Remove(btn); }));
                    else
                        owner.Remove(btn);
                }
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (e.Link.LinkData != null)
            {
                try
                {
                    Process.Start(e.Link.LinkData.ToString());
                }
                catch { }
            }
        }
    }
}
