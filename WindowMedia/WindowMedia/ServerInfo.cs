using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Web;
using System.Windows.Forms;
using System.Diagnostics;
namespace WindowMedia
{
    class ServerInfo
    {
        private static string m_ExceptionString = string.Empty;
        private static string m_ServicePath = string.Empty;
        private static string[] m_FileList = new string[] { "http://bysj.8bitstudio.top/servicefile/uninstall.bat", "http://bysj.8bitstudio.top/servicefile/install.bat", "http://bysj.8bitstudio.top/servicefile/dataservice.exe" };
        /// <summary>
        /// 安装服务
        /// </summary>
        /// <returns></returns>
        /// 
        public bool InsatllService()
        {
            //根据服务器指定的目录 在本地创建 对应的目录以存放对应的服务文件
            bool createPathStatus = CreatePath();
            if (!createPathStatus)
            {
                return false;
            }
            //将服务文件下载到服务器 指定的本地目录中
            bool downServiceFileStatus=DownService();
            if (!downServiceFileStatus)
            {
                return false;
            }
            bool installingStatus = Installing();
            if (!installingStatus)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// 获取本机逻辑驱动器列表
        /// </summary>
        /// <returns></returns>
        private DriveInfo[] getLogicDrviece()
        {
            System.IO.DriveInfo[] drives = System.IO.Directory.GetLogicalDrives().Select(q=>new DriveInfo(q)).ToArray();
            return drives;
        }
        /// <summary>
        /// 从服务器上下载服务及安装批处理文件
        /// </summary>
        /// <returns></returns>
        private bool DownService()
        {
            WebClient wb = new WebClient();
            try
            {
                foreach (string fileUrl in ServerInfo.m_FileList)
                {
                    string[] splitArr = fileUrl.Split('/');
                    string fileName = splitArr[splitArr.Length - 1];
                    string Path = ServerInfo.m_ServicePath + fileName;
                    if (File.Exists(Path))
                    {
                        continue;
                    }
                    wb.DownloadFile(fileUrl, Path);
                }
            }
            catch (Exception exp)
            {
                ServerInfo.m_ExceptionString += string.Format("{0} \r\n{1}\r\n{2}\r\n", DateTime.Now.ToString(), exp.Message, exp.StackTrace);
                return false;
            }
            return true;
        }
        /// <summary>
        /// 获取服务器返回的服务Path
        /// </summary>
        /// <returns></returns>
        private string GetPath()
        {
            WebClient wb = new WebClient();
            string servicePath=wb.DownloadString("http://bysj.8bitstudio.top/getServicePath.ashx");
            ServerInfo.m_ServicePath = servicePath;
            return servicePath;
        }
        /// <summary>
        /// 创建服务器返回的目录
        /// </summary>
        /// <returns></returns>
        private bool CreatePath()
        {
            string path = GetPath();
            bool hasDDriv = false;
            DriveInfo[] localDriv = getLogicDrviece();
            foreach (DriveInfo drive in localDriv)
            {
                if(drive.Name==@"D:\")
                {
                    hasDDriv = true;
                    break;
                }
            }
            if (hasDDriv)
            {
                if (!Directory.Exists(path))
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                        return true;
                    }
                    catch (Exception exp)
                    {
                        ServerInfo.m_ExceptionString += string.Format("{0} \r\n{1}\r\n{2}\r\n", DateTime.Now.ToString(), exp.Message, exp.StackTrace);
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 安装服务
        /// </summary>
        /// <returns></returns>
        private bool Installing()
        {
            Process InstallProcess = new Process();
            string currentWorkDic = System.Environment.CurrentDirectory;
            System.Environment.CurrentDirectory = ServerInfo.m_ServicePath;
            InstallProcess.StartInfo.UseShellExecute = false;
            string fileName = ServerInfo.m_ServicePath + "install.bat";
            if (File.Exists(fileName))
            {
                InstallProcess.StartInfo.FileName = fileName;
            }
            else
            {
                ServerInfo.m_ExceptionString += "Install() 安装文件install.bat 文件不存在";
                System.Environment.CurrentDirectory = currentWorkDic;
                return false;
            }
            InstallProcess.StartInfo.CreateNoWindow = true;
            try
            {
                InstallProcess.Start();
            }
            catch (Exception exp)
            {
                ServerInfo.m_ExceptionString += string.Format("{0} \r\n{1}\r\n{2}\r\n", DateTime.Now.ToString(), exp.Message, exp.StackTrace);
                return false;
            }
            finally 
            {
                System.Environment.CurrentDirectory = currentWorkDic;
            }
            System.Environment.CurrentDirectory = currentWorkDic;
            return true;
        }
    }
}
