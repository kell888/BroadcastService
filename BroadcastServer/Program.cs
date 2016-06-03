using System;
using System.Collections.Generic;
using System.Text;
using CommonLib;

namespace BroadcastServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Logs.Init();
            //MessageBody msg = new MessageBody();
            //RegisterServer<MessageBody> reg = CommonLib.MessageServer<MessageBody>.Init(msg);//Intialize(msg);
            BroadcastEvent be = new BroadcastEvent();
            RegisterServer<BroadcastEvent> reg = CommonLib.MessageServer<BroadcastEvent>.Intialize(be);
            Console.WriteLine("服务已启动....");
            Console.Write("回车立即退出>>>");
            Console.Read();
            //CommonLib.MessageServer<MessageBody>.Dispose(reg);
            CommonLib.MessageServer<BroadcastEvent>.Dispose(reg);
        }
    }
}
