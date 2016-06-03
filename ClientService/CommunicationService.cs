using System;
using System.Collections.Generic;
using System.Text;
using CommonLib;
using System.Windows.Forms;
using System.Net;
using System.Media;
using System.Configuration;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Collections;

namespace ClientService
{
    internal class CommunicationService
    {
        static BroadcastEvent broadcast;
        //static MessageBody broad;
        static RemoteObjectWrapper wrapperRemoteObject;
        static MsgForm msg;
        static string soundLocation = ConfigurationManager.AppSettings["sound"];
        static SoundPlayer player;

        public static bool Start(MsgForm msgForm)
        {
            msg = msgForm;
            try
            {
                IPAddress serv = IPAddress.Loopback;
                string serverStr = ConfigurationManager.AppSettings["server"];
                IPAddress RETIP;
                if (!string.IsNullOrEmpty(serverStr) && IPAddress.TryParse(serverStr, out RETIP))
                {
                    serv = RETIP;
                }
                int port = 8888;
                string portStr = ConfigurationManager.AppSettings["port"];
                int RET;
                if (!string.IsNullOrEmpty(portStr) && int.TryParse(portStr, out RET))
                {
                    port = RET;
                }
                string service = "BroadcastEvent";
                string svr = ConfigurationManager.AppSettings["service"];
                if (!string.IsNullOrEmpty(svr))
                    service = svr;
                BinaryServerFormatterSinkProvider serverProv = new BinaryServerFormatterSinkProvider();
                serverProv.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
                BinaryClientFormatterSinkProvider clientProv = new BinaryClientFormatterSinkProvider();
                IDictionary props = new Hashtable();
                props["port"] = 0;
                TcpChannel channel = new TcpChannel(props, clientProv, serverProv);
                ChannelServices.RegisterChannel(channel, false);
                string fullUri = "tcp://" + serv.ToString() + ":" + port + "/" + service;
                wrapperRemoteObject = new RemoteObjectWrapper();
                wrapperRemoteObject.WrapperBroadcastMessageEvent += new EventHandler<BroadcastEventArgs>(BroadcastEvent_Received);
                RemotingConfiguration.RegisterWellKnownClientType(typeof(BroadcastEvent), fullUri);
                //broad = (MessageBody)Activator.GetObject(typeof(MessageBody), fullUri);
                //broad.AddEvent(wrapperRemoteObject.WrapperBroadMessage);
                broadcast = (BroadcastEvent)Activator.GetObject(typeof(BroadcastEvent), fullUri);
                broadcast.Broadcasting += new EventHandler<BroadcastEventArgs>(wrapperRemoteObject.WrapperBroadMessage);
                return true;
            }
            catch (Exception e) { Logs.Error("开始服务监听失败：" + e.Message); return false; }
        }

        static void BroadcastEvent_Received(object sender, BroadcastEventArgs arg)
        {
            MsgClass m = Common.GetMsg(arg.Data);
            if (m != null)
            {
                try
                {
                    if (msg.InvokeRequired)
                    {
                        msg.Invoke(new MethodInvoker(delegate
                        {
                            msg.SetSource(m.Source);
                            msg.SetLastTime(m.Time);
                            msg.AddMessage(m, arg.Client);
                            msg.ShowMessage();
                        }));
                    }
                    else
                    {
                        msg.SetSource(m.Source);
                        msg.SetLastTime(m.Time);
                        msg.AddMessage(m, arg.Client);
                        msg.ShowMessage();
                    }
                    string loop = ConfigurationManager.AppSettings["loop"];
                    if (player == null)
                        player = new SoundPlayer(soundLocation);
                    if (!string.IsNullOrEmpty(loop) && loop != "0")
                        player.PlayLooping();
                    else
                        player.Play();
                }
                catch (Exception ex)
                {
                    Logs.Error("弹出提示窗体以及发声报警时出错" + ex.Message);
                }
            }
        }

        public static bool Stop()
        {
            if (broadcast != null)
            //if (broad != null)
            {
                try
                {
                    wrapperRemoteObject.WrapperBroadcastMessageEvent -= new EventHandler<BroadcastEventArgs>(BroadcastEvent_Received);
                    //broad.RemoveEvent(wrapperRemoteObject.WrapperBroadMessage);
                    broadcast.Broadcasting -= new EventHandler<BroadcastEventArgs>(wrapperRemoteObject.WrapperBroadMessage);
                    return true;
                }
                catch (Exception e) { Logs.Error("停止服务监听失败：" + e.Message); }
            }
            return false;
        }
    }
}
