using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Web;

/// <summary>
/// 日志处理静态类
/// </summary>
public static class Logs
{
    static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    /// <summary>
    /// 写入错误级别的日志
    /// </summary>
    /// <param name="msg"></param>
    public static void Error(string msg)
    {
        try
        {
            logger.Error(string.Format("[{0}]:{1}" + Environment.NewLine, DateTime.Now, msg));
        }
        catch { }
    }
    /// <summary>
    /// 写入错误级别的日志到数据库(暂时还未使用)
    /// </summary>
    /// <param name="level"></param>
    /// <param name="appName"></param>
    /// <param name="moduleName"></param>
    /// <param name="procName"></param>
    /// <param name="logTitle"></param>
    /// <param name="logMessage"></param>
    public static void LogDataBase(NLog.LogLevel level, string appName, string moduleName, string procName, string logTitle, string logMessage)
    {
        NLog.LogEventInfo ei = new NLog.LogEventInfo(level, "", "");
        ei.Properties["appName"] = appName;
        ei.Properties["moduleName"] = moduleName;
        ei.Properties["procName"] = procName;
        ei.Properties["logLevel"] = level.Name.ToUpper();
        ei.Properties["logTitle"] = logTitle;
        ei.Properties["logMessage"] = logMessage;
        logger.Log(ei);
    }
    /// <summary>
    /// 写入跟踪级别的日志
    /// </summary>
    /// <param name="msg"></param>
    public static void Trace(string msg)
    {
        try
        {
            logger.Trace(string.Format("[{0}]:{1}" + Environment.NewLine, DateTime.Now, msg));
        }
        catch { }
    }
    /// <summary>
    /// 写入消息级别的日志
    /// </summary>
    /// <param name="msg"></param>
    public static void Info(string msg)
    {
        try
        {
            logger.Info(string.Format("[{0}]:{1}" + Environment.NewLine, DateTime.Now, msg));
        }
        catch { }
    }
    /// <summary>
    /// 应用程序启动的日志标识
    /// </summary>
    public static void Init()
    {
        try
        {
            logger.Info(string.Format("[{0}]:{1}" + Environment.NewLine, DateTime.Now, "应用程序启动中..."));
        }
        catch { }
    }
}
