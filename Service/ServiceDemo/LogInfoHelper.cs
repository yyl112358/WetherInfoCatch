using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ServiceDemo
{
    /// <summary>
    /// 日志文件帮助类
    /// </summary>
    public class LogInfoHelper
    {
        static string path = Path.Combine(System.Environment.CurrentDirectory, "RunTimeInfo");
        static string exceptionFileBaseName = "Excep";
        static string logFileBaseName = "Log";
        private static bool WriteRunTimeMessage(string Message, RunTimeMessageType messageType)
        {
            //判断文件夹是否存在
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            DateTime dtNow = DateTime.Now;
            string fileName = string.Empty;
            if (messageType == RunTimeMessageType.Exception)
            {
                fileName = exceptionFileBaseName + "-" + dtNow.ToShortDateString() + ".log";
            }
            else if (messageType == RunTimeMessageType.Log)
            {
                fileName = logFileBaseName + "-" + dtNow.ToShortDateString() + ".log";
            }
            string filePath = path + "\\" + fileName;
            filePath = filePath.Replace('/', '-');
            StreamWriter sw = new StreamWriter(filePath, true, Encoding.UTF8, 1024);
            FileInfo exceptionFileInfo = new FileInfo(filePath);
            long fileSize = exceptionFileInfo.Length / 1024 / 1024;
            //当天异常日志大于10M的时候停止继续写入异常信息（PS：万一异常超级多 硬盘岂不是要炸~_~  服务器上全天跑着鬼知道那儿有bug）
            if (fileSize > 10)
            {
                sw.Close();
                return false;
            }
            try
            {
                sw.WriteLine("------------------------------------------------------------");
                sw.WriteLine(Message);
            }
            catch (Exception exp)
            {
                throw exp;
            }
            finally
            {
                sw.Close();
                sw.Dispose();
            }
            return true;
        }

        /// <summary>
        /// 向日志文件中写入运行时的程序信息
        /// </summary>
        /// <param name="Message">要写入到日志文件中的信息</param>
        /// <param name="messageType">要写入的日志类型</param>
        /// <param name="isAutoAddTimeInfo">是否在写入的信息前加上当前系统时间</param> 
        /// <returns></returns>
        public static bool WriteRunTimeMessage(string Message, RunTimeMessageType messageType, bool isAutoAddTimeInfo)
        {
            string message = string.Empty;
            if (isAutoAddTimeInfo)
            {
                message = string.Format("Time->{0}\r\n {1}", DateTime.Now.ToString(), Message);
            }
            else
            {
                message = Message;
            }
            return WriteRunTimeMessage(message, messageType);
        }
    }

    /// <summary>
    /// 运行时需要写入到日志文件的信息类型
    /// </summary>
    public enum RunTimeMessageType
    {
        Exception,
        Log
    }
}

