using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using System.IO.Compression;

namespace ServiceDemo
{
    public class GetHTTPInfo
    {
        /// <summary>
        /// 连接指定的域名若连接成功返回成功的Socket，失败 返回NULL(自行保证主机DNS可用)
        /// </summary>
        /// <param name="DomainName">要进行连接的Domain</param>
        /// <returns></returns>
        private Socket ConnectDomain(string DomainName)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            #region  解析域名并尝试连接解析到的IP地址
            IPHostEntry iphost = Dns.GetHostEntry(@DomainName);
            foreach (IPAddress ip in iphost.AddressList)
            {
                IPEndPoint ipe = new IPEndPoint(ip, 80);
                socket.Connect(ipe);
                if (socket.Connected)
                {
                    break;
                }
                else
                {
                    continue;
                }
            }
            if (socket.Connected == true)
            {
                return socket;
            }
            else
            {
                return null;
            }
            #endregion
        }
        /// <summary>
        /// 获取对指定的远程的服务器的请求的返回的响应体
        /// </summary>
        /// <param name="remoteHost">要访问的远程服务器域名或IP地址</param>
        /// <param name="HTTP_Head">进行请求的HTTP报文字符串</param>
        /// <returns></returns>
        private string ConnectAndGetResponse(string remoteHost, String HTTP_Head, out string ResponseHead)
        {
            Socket socket = ConnectDomain(remoteHost);
            if (socket == null)
            {
                throw new Exception("连接远程主机失败!!");
            }
            byte[] headBuff = System.Text.Encoding.UTF8.GetBytes(HTTP_Head);
            byte[] recvBuff;
            int sendSucessByteNum = 0;

            //向服务器发送HTTP请求报文
            try
            {
                sendSucessByteNum = socket.Send(headBuff);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            //报文字节发送完成后转入接收状态
            if (sendSucessByteNum == headBuff.Length)     //发送成功状态
            {
                string responseHead = "";
                recvBuff = RecvBytes(socket);
                Stream stream = null;
                string body = GetHTTPBody(recvBuff, ref responseHead, ref stream);
                ResponseHead = responseHead;
                return body;
            }
            else
            {
                Exception e = new Exception("HTTP头发送失败！！");
                throw e;
            }
        }
        /// <summary>
        /// 获取对指定的远程的服务器的请求的返回的响应体的原始流
        /// </summary>
        /// <param name="remoteHost">要访问的远程服务器域名或IP地址</param>
        /// <param name="HTTP_Head">进行请求的HTTP报文字符串</param>
        /// <returns></returns>
        private Stream ConnectAndGetResponseStream(string remoteHost, String HTTP_Head, out string ResponseHead)
        {
            Socket socket = ConnectDomain(remoteHost);
            if (socket == null)
            {
                throw new Exception("连接远程主机失败!!");
            }
            byte[] headBuff = System.Text.Encoding.UTF8.GetBytes(HTTP_Head);
            byte[] recvBuff;
            int sendSucessByteNum = 0;

            //向服务器发送HTTP请求报文
            try
            {
                sendSucessByteNum = socket.Send(headBuff);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            //报文字节发送完成后转入接收状态
            if (sendSucessByteNum == headBuff.Length)     //发送成功状态
            {
                string responseHead = "";
                recvBuff = RecvBytes(socket);
                Stream stream = null;
                string body = GetHTTPBody(recvBuff, ref responseHead, ref stream);
                ResponseHead = responseHead;
                return stream;
            }
            else
            {
                Exception e = new Exception("HTTP头发送失败！！");
                throw e;
            }
        }
        /// <summary>
        /// 接收服务器响应的字节数组(返回的是完整的HTTP响应报文包含HTTP响应头部分)
        /// </summary>
        /// <returns>服务器返回的响应数组</returns>
        private byte[] RecvBytes(Socket socket)
        {
            bool isChunkedTransfer = false;
            byte[] chunkedTransferResult = new byte[1];
            //HTTP响应头里面指定的 返回体的大小
            using (MemoryStream ms = new MemoryStream())
            {
                //http 返回头字符串
                string httpHead = "";
                int ContentLenght = 0, httpHeadLength = 0;
                bool isGetedContentLength = false;
                int recvByteNum = 0;
                byte[] Buff = new byte[4096];
                //重复在网络中读取数据并 写入到内存流中以便控制和将来返回
                try
                {
                    do
                    {
                        //sw.Start();
                        recvByteNum = socket.Receive(Buff, Buff.Length, 0);
                        //sw.Stop();
                        //httpHead = Encoding.UTF8.GetString(Buff);

                        ms.Write(Buff, 0, recvByteNum);
                        //当前还没接收到返回体长度信息
                        if (!isGetedContentLength)
                        {
                            StreamReader sr = new StreamReader(ms, Encoding.UTF8);
                            //将流的指针指向流的开始
                            ms.Seek(0, SeekOrigin.Begin);
                            httpHead = sr.ReadToEnd();
                            //HTTP头部信息接收完整(此时即可判断出是否为chunked编码方式)
                            if ((httpHeadLength = httpHead.IndexOf("\r\n\r\n")) > 0)
                            {
                                httpHead = httpHead.Split(new string[] { "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)[0];

                                ContentLenght = GetContentLength(httpHead);
                                if (ContentLenght == -1)
                                {
                                    ms.Seek(0, SeekOrigin.Begin);
                                    isChunkedTransfer = true;
                                    //该方法被调用时已经能确保以完整的接收到了响应头的全部信息
                                    byte[] byteTemp = ChunkedRecv(socket, ms, httpHeadLength);
                                    byte[] HTTPHeadByte = Encoding.UTF8.GetBytes(httpHead + "\r\n\r\n");
                                    using (MemoryStream memtemp = new MemoryStream())
                                    {
                                        memtemp.Write(HTTPHeadByte, 0, HTTPHeadByte.Length);
                                        memtemp.Write(byteTemp, 0, byteTemp.Length);
                                        chunkedTransferResult = memtemp.ToArray();
                                    }
                                    break;
                                }
                                else
                                {
                                    isGetedContentLength = true;
                                }
                            }
                            //将流的指针指向流的末尾以接着下一次的接收
                            ms.Seek(0, SeekOrigin.End);
                        }
                        //数据以接收完毕跳出当前循环
                        if (ms.Length == ContentLenght + httpHeadLength + 4)
                        {
                            break;
                        }
                        //totle += sw.Elapsed.TotalMilliseconds;
                        //i++;
                    }
                    while (recvByteNum > 0);
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                catch (Exception e)
                {
                    throw e;
                }
                // string Elapsed = totle.ToString();
                //int a = i;
                if (isChunkedTransfer)
                {
                    return chunkedTransferResult;
                }
                else
                {
                    return ms.ToArray();
                }
            }
        }
        /// <summary>
        /// 根据http响应头的字符串获取当前响应体的长度
        /// </summary>
        /// <param name="httpHead">http的响应头字符串</param>
        /// <returns>当前响应的响应体长度 (服务器使用Chunked返回数据时返回-1)</returns>
        private int GetContentLength(string httpHead)
        {
            //服务器使用了分段传输
            if (httpHead.IndexOf("chunked") > 0)
            {
                return -1;
            }
            string temp = httpHead.Substring(httpHead.IndexOf("Content-Length:") + 15);
            string length = temp.Substring(0, temp.IndexOf("\r\n"));
            int contentLength = 0;
            try
            {
                contentLength = int.Parse(length);
            }
            catch (Exception e)
            {
                throw e;
            }
            return contentLength;
        }
        /// <summary>
        /// 根据响应返回的字节数组获取响应的响应体
        /// </summary>
        /// <param name="recvResponseHeadbuff">接受的响应的字节流</param>
        /// <returns>响应的响应体的字符串</returns>
        private string GetHTTPBody(byte[] recvBuff, ref string ResponseHead, ref Stream ResponseBodyStream)
        {
            MemoryStream ms = new MemoryStream();
            ms.Write(recvBuff, 0, recvBuff.Length);
            ms.Seek(0, SeekOrigin.Begin);     //将读取流内容的指针指向流的开头

            Encoding utf8 = Encoding.GetEncoding("utf-8");

            string ResponseStr = new StreamReader(ms, utf8).ReadToEnd();
            //分割HTTP响应头和响应体
            string[] Array = ResponseStr.Split(new string[] { "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            ResponseHead = Array[0];
            int charsetBegin = 0;
            string charset = "utf-8";
            //获取响应体的编码方式
            if ((charsetBegin = Array[0].IndexOf("charset=")) != -1)          //正文部分为其他编码格式
            {
                charset = Array[0].Substring(charsetBegin + 8);
                charset = charset.Substring(0, charset.IndexOf("\r\n"));
            }
            //响应体不是Gzip压取字符流 分割后缩流直接获返回
            if (Array[0].IndexOf("Content-Encoding: gzip") == -1)
            {
                string response = "";
                if (charset.ToLower() == "utf-8")
                {
                    int entityIndex = Encoding.UTF8.GetBytes(ResponseHead).Length + 4;
                    ResponseBodyStream = new MemoryStream(recvBuff, entityIndex, recvBuff.Length - entityIndex);
                    return Array[1];
                }
                response = Encoding.GetEncoding(charset).GetString(recvBuff);            //重新获取字符流
                return response.Substring(response.IndexOf("\r\n\r\n") + 4).Replace("\0", "");
            }
            string temp = Array[0].Substring(Array[0].IndexOf("Content-Length:") + 15);
            //获取当前响应体的长度
            string ContentLength = temp.Substring(0, temp.IndexOf("\r\n"));
            //实体开始位置
            int begin = 0;
            if (ResponseHead.IndexOf("Transfer-Encoding: chunked") > 0)
            {
                //chunked方式实体部分的开始 计算
                begin = Encoding.UTF8.GetByteCount(ResponseHead) + 4;
            }
            else
            {
                //非chunked方式实体部分计算方式为流长度减去响应体的长度就是响应体开始的位置
                begin = (int)(ms.Length - Convert.ToInt32(ContentLength));
            }

            ms.Seek(begin, SeekOrigin.Begin);
            string Content="";
            using (MemoryStream msm = new MemoryStream())
            {
                byte[] buff =new byte[ms.Length-begin];
                ms.Read(buff, begin, buff.Length);
                msm.Write(buff,0,buff.Length);
                Content = DecodeGzipStream.DecompressGzip(msm, charset);
                ms.Seek(begin, SeekOrigin.Begin);
                ResponseBodyStream = DecodeGzipStream.DecompressGzip(msm);
                ms.Close();
                ms.Dispose();
            }
            return Content;
        }
        #region chunked接收处理部分
        /// <summary>
        /// chunked编码接收方式
        /// </summary>
        /// <param name="socket">用于继续接收传输信息的socket</param>
        /// <param name="recvMemBuff">当前以接收到的部分信息的内存流</param>
        /// <returns></returns>
        private byte[] ChunkedRecv(Socket socket, MemoryStream recvMemBuff, int headLen)
        {
            byte[] headerEntitySpliterArr = new byte[] { 13, 10, 13, 10 };
            byte[] firstRecvByteArr = recvMemBuff.ToArray();
            int headerEnitySpliterIndex = indexByteArr(firstRecvByteArr, headerEntitySpliterArr);
            MemoryStream rawDataMemStream = new MemoryStream();
            MemoryStream entityMemStream = new MemoryStream();
            //当前接收的实体部分全部写到流中
            rawDataMemStream.Write(firstRecvByteArr, headerEnitySpliterIndex + 4, firstRecvByteArr.Length - headerEnitySpliterIndex - 4);
            do
            {
                //剔除HTTP头部分后的字节数组
                byte[] entityByteArr = rawDataMemStream.ToArray();
                //分析出chunked块大小后紧跟的实体数据数组
                byte[] nearEntityBytes = new byte[1];
                //chunked块大小
                int ChunkedLength = 0;
                //标记chunked块大小的hex字符串长度
                int hexStrLen = 0;
                //标记当前是否处理了下一个chunked数据块的数据(下次socket接收数据时依赖做判断)
                bool isHasNextChunkedBlockInfo = false;
                try
                {
                    ChunkedLength = GetChunkedBlockLength(entityByteArr, out nearEntityBytes, out hexStrLen);
                }
                catch (Exception exp)
                {
                    throw exp;
                }
                if (ChunkedLength == 0)
                {
                    break;
                }
                if (nearEntityBytes.Length >= ChunkedLength)
                {
                    //从字节数组的开始写chunkedSize长度的字节到实体流中
                    entityMemStream.Write(nearEntityBytes, 0, ChunkedLength);
                    byte[] nowEntityByteArr = entityMemStream.ToArray();
                    string nowEntityStr = Encoding.UTF8.GetString(nowEntityByteArr);
                    //当前流中有下一个chunked块的信息
                    if (ChunkedLength + 4 + hexStrLen < rawDataMemStream.Length)
                    {
                        //搬移rawMemStream剩余的字节
                        rawDataMemStream.Seek(ChunkedLength + 4 + hexStrLen, SeekOrigin.Begin);
                        long overFlowLen = rawDataMemStream.Length - 4 - ChunkedLength - hexStrLen;
                        //中间数组
                        byte[] byteTemp = new byte[overFlowLen];

                        rawDataMemStream.Read(byteTemp, 0, byteTemp.Length);
                        string byteTempStr = Encoding.UTF8.GetString(byteTemp);
                        //重新构造内存流文件
                        rawDataMemStream = new MemoryStream();
                        rawDataMemStream.Write(byteTemp, 0, byteTemp.Length);
                        isHasNextChunkedBlockInfo = true;
                    }
                }
                //当前socket可读
                if (socket.Poll(10, SelectMode.SelectRead))
                {
                    byte[] byteTmp = new byte[4096];
                    int realRecvLen = socket.Receive(byteTmp);
                    //rawDataMemStream流中有处理过的数据需要紧跟着写入
                    if (isHasNextChunkedBlockInfo == true)
                    {
                        rawDataMemStream.Seek(0, SeekOrigin.End);
                        rawDataMemStream.Write(byteTmp, 0, realRecvLen);
                    }
                    else
                    {
                        rawDataMemStream = new MemoryStream();
                        rawDataMemStream.Write(byteTmp, 0, realRecvLen);
                    }
                }
            } while (true);
            rawDataMemStream.Close();
            rawDataMemStream.Dispose();
            byte[] result = entityMemStream.ToArray();
            string resultstr = Encoding.UTF8.GetString(result);
            entityMemStream.Close();
            entityMemStream.Dispose();
            return result;
        }
        /// <summary>
        /// 获取当前chunkedBlock的大小且返回chunked块大小后面跟随的实体内容(以剔除\r\n)以及chunked块16进制字符串长度(外部计算使用)
        /// </summary>
        /// <param name="entityArr">以chunkedBlock长度16进制字符串开头的字节数组</param>
        /// <param name="entityByte">分析chunked编码后解析出来后面chunked负载数据字节数组</param>
        /// <param name="hexStrLens">返回hex字符串的长度(外部判断使用)</param>
        /// <returns></returns>
        private int GetChunkedBlockLength(byte[] entityArr, out byte[] entityByte, out int hexStrLens)
        {
            string entityStr = Encoding.UTF8.GetString(entityArr);
            byte[] splitByteArr = new byte[] { 13, 10 };
            int splitIndex = indexByteArr(entityArr, splitByteArr);
            if (splitIndex == -1)
                throw new Exception("chunked Body 分析出错");
            hexStrLens = splitIndex;
            string lengthHEX = Encoding.UTF8.GetString(entityArr, 0, splitIndex);
            int length = Convert.ToInt32(lengthHEX, 16);
            using (MemoryStream stream = new MemoryStream())
            {
                //剔除Chunked块大小信息后的字节数组(不包含\r\n)
                stream.Write(entityArr, splitIndex + 2, length);
                entityByte = stream.ToArray();
            }
            return length;
        }

        /// <summary>
        /// 返回一个字节数组在另一个数组中的索引位置(如果不匹配返回-1)
        /// </summary>
        /// <param name="SourceArr">要查找索引的数组</param>
        /// <param name="targetArr">进行匹配的数组索引</param>
        /// <returns></returns>
        private int indexByteArr(byte[] SourceArr, byte[] targetArr)
        {
            string sourceStr
                = Encoding.UTF8.GetString(SourceArr);
            string targetStr = Encoding.UTF8.GetString(targetArr);
            if (SourceArr.Length < targetArr.Length)
            {
                throw new Exception("源数组的长度小于要查找的字节数组的长度");
            }
            //源字节数组游标
            bool isFindOk = false;
            int sourceArrIndex = 0;
            int splitArrIndex = 0;
            foreach (byte singleByte in SourceArr)
            {
                //第一个字节匹配成功
                if (singleByte == targetArr[0])
                {
                    //split字节数组游标
                    splitArrIndex = 0;
                    foreach (byte splitArrByte in targetArr)
                    {
                        if (SourceArr[sourceArrIndex + splitArrIndex] == targetArr[splitArrIndex])
                        {
                            splitArrIndex++;
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (splitArrIndex == targetArr.Length)
                    {
                        isFindOk = true;
                        break;
                    }
                }
                sourceArrIndex++;
            }
            if (isFindOk)
            {
                return sourceArrIndex;
            }
            else
            {
                return -1;
            }
        }
        #endregion
        #region   public 重载方法
        /// <summary>
        /// 获取远程服务器的返回响应的响应体
        /// </summary>
        /// <param name="RequestDomain">远程服务器地址(请确保没有HTTP://字符)</param>
        /// <param name="RelativeUrl">相对URL(querystring自己处理)</param>
        /// <returns></returns>
        public string GetHttpResponseStr(string RequestDomain, string RelativeUrl)
        {
            if (RelativeUrl == "")
                RelativeUrl = "/";
            string httpHeader = @"GET " + RelativeUrl;
            httpHeader += " HTTP/1.1\r\nHost: " + RequestDomain + "\r\nCache-Control: max-age=0\r\nAccept: text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8\r\nUpgrade-Insecure-Requests: 1\r\nUser-Agent: Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.101 Safari/537.36\r\nAccept-Language: zh-CN,zh;q=0.8\r\n\r\n";
            string responseHead = "";
            return ConnectAndGetResponse(RequestDomain, httpHeader, out responseHead);
        }
        /// <summary>
        /// 获取远程服务器的返回响应并返回响应的响应头  (改方法仅返回HTTP响应体)
        /// </summary>
        /// <param name="RequestDomain">远程服务器地址(请确保没有HTTP://字符)</param>
        /// <param name="RelativeUrl">相对URL(querystring自己处理)</param>
        /// <returns></returns>
        public string GetHttpResponseStr(string RequestDomain, string RelativeUrl, out string ResponseHead)
        {
            if (RelativeUrl == "")
                RelativeUrl = "/";
            string httpHeader = @"GET " + RelativeUrl;
            httpHeader += " HTTP/1.1\r\nHost: " + RequestDomain + "\r\nCache-Control: max-age=0\r\nAccept: text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8\r\nUpgrade-Insecure-Requests: 1\r\nUser-Agent: Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.101 Safari/537.36\r\nAccept-Language: zh-CN,zh;q=0.8\r\n\r\n";
            return ConnectAndGetResponse(RequestDomain, httpHeader, out ResponseHead);
        }

        /// <summary>
        /// 获取远程服务器的返回的原始响应流并返回响应的响应头  (改方法返回HTTP响应的原始响应流)
        /// </summary>
        /// <param name="RequestDomain">远程服务器地址(请确保没有HTTP://字符)</param>
        /// <param name="RelativeUrl">相对URL(querystring自己处理)</param>
        /// <returns></returns>
        public Stream GetHttpResponseStream(string RequestDomain, string RelativeUrl, out string ResponseHeader)
        {
            if (RelativeUrl == "")
                RelativeUrl = "/";
            string httpHeader = @"GET " + RelativeUrl;
            httpHeader += " HTTP/1.1\r\nHost: " + RequestDomain + "\r\nCache-Control: max-age=0\r\nAccept: text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8\r\nUpgrade-Insecure-Requests: 1\r\nUser-Agent: Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.101 Safari/537.36\r\nAccept-Language: zh-CN,zh;q=0.8\r\n\r\n";
            return ConnectAndGetResponseStream(RequestDomain, httpHeader, out ResponseHeader);
        }
        /// <summary>
        /// 使用WebClient实现的获取远程服务器响应的方法
        /// </summary>
        /// <param name="remoteAddress">远程服务器的完整url（请确保URL中以包括QueryString且包含schema 如  HTTP://  HTTPS://）</param>
        /// <returns></returns>
        public string GetHttpResponse(string remoteAddress)
        {
            Uri uri = new Uri(remoteAddress);
            GZipWebClient webClient = new GZipWebClient();
            //使用 Using退出代码段时立即释放掉stream对象
            using (Stream resultStream = webClient.OpenRead(uri))
            {
                string resultStr = string.Empty;
                using (MemoryStream mem = new MemoryStream())
                {
                    int len = 0;
                    byte[] bytes = new byte[1024];
                    while ((len = resultStream.Read(bytes, 0, bytes.Length)) > 0)
                    {
                        mem.Write(bytes, 0, len);
                    }
                    resultStr = Encoding.UTF8.GetString(mem.ToArray());
                }
                return resultStr;
            }
        }
        #endregion
    }
    /// <summary>
    /// 支持Gzip解压的WebClient
    /// </summary>
    internal class GZipWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest webrequest = (HttpWebRequest)base.GetWebRequest(address);
            webrequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            return webrequest;
        }
    }

    /// <summary>
    /// 用于解压Gzip压缩流的类
    /// </summary>
    internal class DecodeGzipStream
    {
        /// <summary>
        /// 以指定的编码方式解码Gzip压缩的内存流返回流内的字符串
        /// </summary>
        /// <param name="stm">要进行解码的压缩流</param>
        /// <param name="encodingType">指定解压后解码的编码方式</param>
        /// <returns></returns>
        public static string DecompressGzip(MemoryStream stm, string encodingType)
        {
            string result = "";
            encodingType = encodingType == "" ? "utf-8" : encodingType;
            using (MemoryStream mem = new MemoryStream())
            {
                stm.CopyTo(mem);
                GZipStream gzip = new GZipStream(mem, CompressionMode.Decompress);

                using (StreamReader reader = new StreamReader(mem, Encoding.GetEncoding(encodingType)))
                {
                    result = reader.ReadToEnd();
                }
            }
            return result;
        }
        /// <summary>
        /// 解码Gzip压缩的内存流并返回解压后的内存流
        /// </summary>
        /// <param name="stm">要进行解码的压缩流</param>
        /// <returns></returns>
        public static Stream DecompressGzip(MemoryStream stm)
        {
            GZipStream gzip = new GZipStream(stm, CompressionMode.Decompress);
            MemoryStream stream = new MemoryStream();
            gzip.CopyTo(stream);
            return stream;
        }
    }
}
