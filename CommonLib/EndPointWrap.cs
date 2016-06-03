using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace CommonLib
{
    /// <summary>
    /// 终结点与消息的封装类
    /// </summary>
    [Serializable]
    public class EndPointWrap
    {
        EndPoint ep;
        /// <summary>
        /// 终结点
        /// </summary>
        public EndPoint EndPoint
        {
            get { return ep; }
            set { ep = value; }
        }
        MsgClass msg;
        /// <summary>
        /// 消息
        /// </summary>
        public MsgClass Message
        {
            get { return msg; }
            set { msg = value; }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="msg"></param>
        public EndPointWrap(EndPoint ep, MsgClass msg)
        {
            this.ep = ep;
            this.msg = msg;
        }
    }
}
