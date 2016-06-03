using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Configuration;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Lifetime;
using System.Collections;

namespace CommonLib
{
    /// <summary>
    /// 广播事件参数类
    /// </summary>
    [Serializable]
    public class BroadcastEventArgs : EventArgs
    {
        IPEndPoint client;
        /// <summary>
        /// 消息来源
        /// </summary>
        public IPEndPoint Client
        {
            get { return client; }
            set { client = value; }
        }
        byte[] data;
        /// <summary>
        /// 消息数据
        /// </summary>
        public byte[] Data
        {
            get { return data; }
            set { data = value; }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="client"></param>
        /// <param name="data"></param>
        public BroadcastEventArgs(IPEndPoint client, byte[] data)
        {
            this.client = client;
            this.data = data;
        }
    }

    /// <summary>
    /// 远程对象接口
    /// </summary>
    public interface IRemoteObject
    {
        /// <summary>
        /// 广播事件
        /// </summary>
        event EventHandler<BroadcastEventArgs> Broadcasting;
        /// <summary>
        /// 发送广播
        /// </summary>
        /// <param name="arg"></param>
        void Broadcast(BroadcastEventArgs arg);
    }

    /// <summary>
     ///  广播事件类(终极类，即不可被继承)
     /// </summary>
    public sealed class BroadcastEvent : MarshalByRefObject, IRemoteObject
    {
        #region IRemoteObject 成员
        /// <summary>
        /// 广播事件
        /// </summary>
        public event EventHandler<BroadcastEventArgs> Broadcasting;

        /// <summary>
        /// 发送广播
        /// </summary>
        /// <param name="arg"></param>
        public void Broadcast(BroadcastEventArgs arg)
        {
            if (Broadcasting != null)
            {
                EventHandler<BroadcastEventArgs> tmpEvent = null;
                IEnumerator enumerator = Broadcasting.GetInvocationList().GetEnumerator() ;
                while(enumerator.MoveNext())
                {
                    Delegate handler = (Delegate)enumerator.Current;
                    try
                    {
                        tmpEvent = (EventHandler<BroadcastEventArgs>)handler;
                        tmpEvent(this, arg);
                    }
                    catch (Exception e)
                    {
                        Logs.Error(e.Message);
                        //注册的事件处理程序出错，删除
                        Broadcasting -= tmpEvent;
                    }
                }
            }
        }

        #endregion

        /// <summary>
         /// 重载InitializeLifetimeService
        /// </summary>
        /// <returns></returns>
        public override object InitializeLifetimeService()
        {
            return null;
            //ILease aLease = (ILease)base.InitializeLifetimeService();
            //if (aLease.CurrentState == LeaseState.Initial)
            //{
            //    aLease.InitialLeaseTime = TimeSpan.Zero;// 不过期
            //}
            //return aLease;
        }
        /*
         /// <summary>
         /// 事件字典
         /// </summary>
         private Dictionary<string, EventHandler<BroadcastEventArgs>> Handlers = new Dictionary<string, EventHandler<BroadcastEventArgs>>();
 
         /// <summary>
         /// 添加事件
         /// </summary>
         /// <param name="eventName"></param>
         /// <param name="handler"></param>
         public void AddEvent(string eventName, EventHandler<BroadcastEventArgs> handler)
         {
             if (Handlers.ContainsKey(eventName))
             {
                 Handlers[eventName] += handler;
             }
             else
             {
                 Handlers.Add(eventName, handler);
             }
         }
 
         /// <summary>
         /// 清除事件
         /// </summary>
         /// <param name="eventName"></param>
         /// <param name="handler"></param>
         public void RemoveEvent(string eventName, EventHandler<BroadcastEventArgs> handler)
         {
             if (Handlers.ContainsKey(eventName))
             {
                 Handlers[eventName] -= handler;
             }
         }
 
         /// <summary>
         /// 立即广播
         /// </summary>
         /// <param name="eventName"></param>
         /// <param name="arg"></param>
         public void Broadcast(string eventName, BroadcastEventArgs arg)
         {
             if (!string.IsNullOrEmpty(eventName))
             {
                 // 从字典中获取事件
                 EventHandler<BroadcastEventArgs> eh = null;
                 Handlers.TryGetValue(eventName, out eh);
                 // 执行事件
                 if (eh != null)
                 {
                     eh(this, arg);
                 }
             }
         }
         */
    }
    /// <summary>
    /// 远程对象包装类
    /// </summary>
    public class RemoteObjectWrapper : MarshalByRefObject
    {
        /// <summary>
        /// 远程对象包装对象的广播消息事件
        /// </summary>
        public event EventHandler<BroadcastEventArgs> WrapperBroadcastMessageEvent;
        /// <summary>
        /// 发送广播消息的包装函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arg"></param>
        public void WrapperBroadMessage(object sender, BroadcastEventArgs arg)
        {
            if (WrapperBroadcastMessageEvent != null)
            {
                WrapperBroadcastMessageEvent(this, arg);
            }
        }

        /// <summary>
        /// 重载生命周期函数，使之无限长
        /// </summary>
        /// <returns></returns>
        public override object InitializeLifetimeService()
        {
            return null;
        }
    }

    /// <summary>
    /// 服务注册器
    /// </summary>
    public class RegisterServer<T> where T : MarshalByRefObject
    {
        Dictionary<string, ObjRef> regObjects;
        /// <summary>
        /// 当前服务注册器中是否存在正在运行的服务
        /// </summary>
        public bool IsStarted
        {
            get
            {
                return regObjects.Count > 0;
            }
        }

        /// <summary>
        /// 是否已经注册指定的服务
        /// </summary>
        public bool IsRegister(string uri)
        {
            if (regObjects.Count > 0)
            {
                return regObjects.ContainsKey(uri);
            }
            return false;
        }

        /// <summary>
        /// 构造服务的字典容器
        /// </summary>
        public RegisterServer()
        {
            regObjects = new Dictionary<string, ObjRef>();
        }

        /// <summary>
        /// 注销当前服务注册器所有已经正在运行的服务
        /// </summary>
        ~RegisterServer()
        {
            if (regObjects.Count > 0)
            {
                foreach (string key in regObjects.Keys)
                {
                    Unregister(key);
                }
                regObjects.Clear();
            }
        }

        /// <summary>
        /// 往当前服务注册器中注册默认的服务
        /// </summary>
        /// <param name="sharedObject"></param>
        public void Register(T sharedObject)
        {
            if (sharedObject != null)
            {
                ObjRef objRef = RemotingServices.Marshal(sharedObject, typeof(T).Name, typeof(T));
                regObjects.Add(typeof(T).Name, objRef);
            }
        }

        /// <summary>
        /// 往当前服务注册器中注册指定的服务
        /// </summary>
        /// <param name="sharedObject"></param>
        /// <param name="ObjURI"></param>
        public void Register(T sharedObject, string ObjURI)
        {
            if (sharedObject != null)
            {
                ObjRef objRef = RemotingServices.Marshal(sharedObject, ObjURI, typeof(T));
                regObjects.Add(ObjURI, objRef);
            }
        }

        /// <summary>
        /// 在当前服务注册器中注销指定的服务
        /// </summary>
        /// <param name="ObjURI"></param>
        public void Unregister(string ObjURI)
        {
            if (regObjects.ContainsKey(ObjURI))
                RemotingServices.Unmarshal(regObjects[ObjURI]);
        }
    }

    /// <summary>
    /// TCP协议客户端泛型类(服务器是单件模式的发送端)
    /// </summary>
    public class Client<T> where T : MarshalByRefObject
    {
        IPAddress server;
        /// <summary>
        /// 事件中心服务器IP地址
        /// </summary>
        public IPAddress Server
        {
            get { return server; }
        }

        int port = 8888;
        /// <summary>
        /// 服务器端口号
        /// </summary>
        public int Port
        {
            get { return port; }
        }

        T sharedObject;
        /// <summary>
        /// 远程共享的对象
        /// </summary>
        public T SharedObject
        {
            get { return sharedObject; }
        }

        TcpChannel channel;

        /// <summary>
        /// 默认的构造函数
        /// </summary>
        public Client()
        {
            int localPort = 0;
            //string lp = ConfigurationManager.AppSettings["localPort"];
            //int RETLP;
            //if (!string.IsNullOrEmpty(lp) && int.TryParse(lp, out RETLP))
            //{
            //    localPort = RETLP;
            //}
            BinaryServerFormatterSinkProvider serverProv = new BinaryServerFormatterSinkProvider();
            serverProv.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
            BinaryClientFormatterSinkProvider clientProv = new BinaryClientFormatterSinkProvider();
            IDictionary props = new Hashtable();
            props["port"] = localPort;
            channel = new TcpChannel(props, clientProv, serverProv);
            //channel = new TcpClientChannel();

            ChannelServices.RegisterChannel(channel, false);

            string serverStr = ConfigurationManager.AppSettings["server"];
            IPAddress RETIP;
            if (!string.IsNullOrEmpty(serverStr) && IPAddress.TryParse(serverStr, out RETIP))
            {
                server = RETIP;
            }

            string portStr = ConfigurationManager.AppSettings["port"];
            int RET;
            if (!string.IsNullOrEmpty(portStr) && int.TryParse(portStr, out RET))
            {
                port = RET;
            }

            string fullUri = "tcp://" + server.ToString() + ":" + port + "/" + typeof(T).Name;
            RemotingConfiguration.RegisterWellKnownClientType(typeof(T), fullUri);
            sharedObject = (T)Activator.GetObject(typeof(T), fullUri);
            //RemotingConfiguration.RegisterActivatedClientType(typeof(T), fullUri);//按值传送
            //sharedObject = Activator.CreateInstance(typeof(T)) as T;//按值传送
        }

        /// <summary>
        /// 指定服务的构造函数
        /// </summary>
        public Client(string uri)
        {
            int localPort = 0;
            //string lp = ConfigurationManager.AppSettings["localPort"];
            //int RETLP;
            //if (!string.IsNullOrEmpty(lp) && int.TryParse(lp, out RETLP))
            //{
            //    localPort = RETLP;
            //}
            BinaryServerFormatterSinkProvider serverProv = new BinaryServerFormatterSinkProvider();
            serverProv.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
            BinaryClientFormatterSinkProvider clientProv = new BinaryClientFormatterSinkProvider();
            IDictionary props = new Hashtable();
            props["port"] = localPort;
            channel = new TcpChannel(props, clientProv, serverProv);
            //channel = new TcpClientChannel();

            ChannelServices.RegisterChannel(channel, true);

            string serverStr = ConfigurationManager.AppSettings["server"];
            IPAddress RETIP;
            if (!string.IsNullOrEmpty(serverStr) && IPAddress.TryParse(serverStr, out RETIP))
            {
                server = RETIP;
            }

            string portStr = ConfigurationManager.AppSettings["port"];
            int RET;
            if (!string.IsNullOrEmpty(portStr) && int.TryParse(portStr, out RET))
            {
                port = RET;
            }

            string fullUri = "tcp://" + server.ToString() + ":" + port + "/" + uri;
            RemotingConfiguration.RegisterWellKnownClientType(typeof(T), fullUri);
            sharedObject = (T)Activator.GetObject(typeof(T), fullUri);
        }
        /// <summary>
        /// 注销信道
        /// </summary>
        ~Client()
        {
            if (channel != null)
                ChannelServices.UnregisterChannel(channel);
        }
    }

    /// <summary>
    /// 消息服务类
    /// </summary>
    public class MessageServer<T> where T : MarshalByRefObject
    {
        /// <summary>
        /// 根据指定的配置文档(ServerCfg.config)启动服务
        /// </summary>
        /// <param name="msg"></param>
        public static RegisterServer<T> Init(T msg)
        {
            RegisterServer<T> reg = new RegisterServer<T>();
            try
            {
                RemotingConfiguration.Configure("ServerCfg.config", false);

                WellKnownServiceTypeEntry swte = new WellKnownServiceTypeEntry(typeof(T), typeof(T).Name, WellKnownObjectMode.Singleton);
                RemotingConfiguration.ApplicationName = typeof(T).Name;
                RemotingConfiguration.RegisterWellKnownServiceType(swte);
                reg.Register(msg);
                return reg;
            }
            catch (Exception e)
            {
                Logs.Error("启动消息服务失败：" + e.Message);
            }
            return reg;
        }
        /// <summary>
        /// 根据默认的配置文档(app.config)启动服务
        /// </summary>
        /// <param name="msg"></param>
        public static RegisterServer<T> Intialize(T msg)
        {
            int port = 8888;
            string portStr = ConfigurationManager.AppSettings["port"];
            int RET;
            if (!string.IsNullOrEmpty(portStr) && int.TryParse(portStr, out RET))
            {
                port = RET;
            }

            BinaryServerFormatterSinkProvider serverProv = new BinaryServerFormatterSinkProvider();
            serverProv.TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full;
            BinaryClientFormatterSinkProvider clientProv = new BinaryClientFormatterSinkProvider();
            IDictionary props = new Hashtable();
            props["port"] = port;
            TcpChannel servChannel = new TcpChannel(props, clientProv, serverProv);

            RegisterServer<T> reg = new RegisterServer<T>();
            try
            {
                ChannelServices.RegisterChannel(servChannel, false);
                WellKnownServiceTypeEntry swte = new WellKnownServiceTypeEntry(typeof(T), typeof(T).Name, WellKnownObjectMode.Singleton);
                RemotingConfiguration.ApplicationName = typeof(T).Name;
                RemotingConfiguration.RegisterWellKnownServiceType(swte);
                reg.Register(msg);
                return reg;
            }
            catch (Exception e)
            {
                Logs.Error("启动服务时出错：" + e.Message);
            }
            return reg;
        }
        /// <summary>
        /// 注销指定的服务
        /// </summary>
        /// <param name="reg"></param>
        public static void Dispose(RegisterServer<T> reg)
        {
            try
            {
                reg.Unregister(typeof(T).Name);
            }
            catch (Exception e)
            {
                Logs.Error("停止消息服务失败：" + e.Message);
            }
        }
    }
}
