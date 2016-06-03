using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Collections;

namespace CommonLib
{
    /// <summary>
    /// 消息体接口
    /// </summary>
    public interface IMessageBody
    {
        /// <summary>
        /// 发送广播
        /// </summary>
        /// <param name="e"></param>
        [System.Runtime.Remoting.Messaging.OneWay]
        void Broadcast(BroadcastEventArgs e);
        /// <summary>
        /// 添加广播要触发的事件
        /// </summary>
        /// <param name="strKey"></param>
        /// <param name="cEvent"></param>
        void AddEvent(string strKey, MessageEvent cEvent);
        /// <summary>
        /// 移除事件
        /// </summary>
        /// <param name="strKey"></param>
        void RemoveEvent(string strKey);
    }
    /// <summary>
    /// 消息事件类
    /// </summary>
    public class MessageEvent : MarshalByRefObject
    {
        /// <summary>
        /// 接收到消息的事件
        /// </summary>
        public event EventHandler<BroadcastEventArgs> Received;

        //[System.Runtime.Remoting.Messaging.OneWay]
        /// <summary>
        /// 接收到消息时要执行的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnReceived(object sender, BroadcastEventArgs e)
        {
            if (Received != null)
            {
                Received(this, e);
            }
            //如果消息到达太频繁（每秒上万条），应该做缓存（Buffer）处理。
        }
    }
    /// <summary>
    /// 消息服务类
    /// </summary>
    public class MessageServer<T> where T : MarshalByRefObject
    {
        /// <summary>
        /// 根据默认的配置文档(ServerCfg.config)启动服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
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
                //RemotingConfiguration.ApplicationName = typeof(T).Name;//按值传送
                //RemotingConfiguration.RegisterActivatedServiceType(typeof(T));//按值传送
            }
            catch (Exception e)
            {
                Logs.Error("启动消息服务失败：" + e.Message);
            }
            return reg;
       }
        /// <summary>
        /// 注销指定的服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
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
    /// <summary>
    /// 消息体类
    /// </summary>
    public class MessageBody : MarshalByRefObject, IMessageBody
    {
        /// <summary>
        /// 接收到消息的事件
        /// </summary>
        public event EventHandler<BroadcastEventArgs> Received;

        private IDictionary<string, EventHandler<BroadcastEventArgs>> eventList = new Dictionary<string, EventHandler<BroadcastEventArgs>>();
        /// <summary>
        /// 广播消息
        /// </summary>
        /// <param name="e"></param>
        public void Broadcast(BroadcastEventArgs e)
        {
            Subscribe();
            //激发客户端的消息到达事件。
            if (Received != null)
            {
                EventHandler<BroadcastEventArgs> receicve = null;
                IEnumerator enumerator = Received.GetInvocationList().GetEnumerator();
                while (enumerator.MoveNext())
                {
                    Delegate handler = (Delegate)enumerator.Current;
                    try
                    {
                        receicve = (EventHandler<BroadcastEventArgs>)handler;
                        receicve(this, e);
                    }
                    catch (Exception ex)
                    {
                        RemoveEvent(receicve);
                        Logs.Error("广播时出错：" + ex.Message);
                    }
                    Received -= receicve;
                }
            }
        }
        /// <summary>
        /// 添加广播的事件
        /// </summary>
        /// <param name="handler"></param>
        public void AddEvent(EventHandler<BroadcastEventArgs> handler)
        {
            lock (this)
            {
                eventList.Add("BroadcastEvent", handler);
            }
        }
        /// <summary>
        /// 添加广播的事件
        /// </summary>
        /// <param name="strKey"></param>
        /// <param name="cEvent"></param>
        public void AddEvent(string strKey, CommonLib.MessageEvent cEvent)
        {
            lock (this)
            {
                eventList.Add(strKey, new EventHandler<BroadcastEventArgs>(cEvent.OnReceived));
            }
        }
        /// <summary>
        /// 移除事件
        /// </summary>
        /// <param name="handler"></param>
        public void RemoveEvent(EventHandler<BroadcastEventArgs> handler)
        {
            lock (this)
            {
            foreach (string key in eventList.Keys)
            {
                if (eventList[key].Equals(handler))
                {
                    eventList.Remove(key);
                    return;
                }
            }
            }
        }
        /// <summary>
        /// 移除事件
        /// </summary>
        /// <param name="strKey"></param>
        public void RemoveEvent(string strKey)
        {
            lock (this)
            {
            eventList.Remove(strKey);
            }
        }

        private void Subscribe()
        {
            foreach (string key in eventList.Keys)
            {
                this.Received += eventList[key];
            }
        }
    }
}
