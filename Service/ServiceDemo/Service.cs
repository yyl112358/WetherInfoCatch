using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.IO;
using System.Net;
using System.Reflection;
using System.Net.NetworkInformation;

namespace ServiceDemo
{
    public class Service
    {
        //Service.ashx?accesskey=指定的ak&MAC=请求机器的MAC地址&requesttype=请求类型  (requesttype 有两种类型  getcitylist、updatecitydata)
        string m_baseUrl = string.Empty;
        string m_accessKey = string.Empty;
        string m_LoaclMac = string.Empty;
        Thread m_backThread;
        /// <summary>
        /// 开始后台服务的公开接口(给service的OnStart事件处理器调用)
        /// </summary>
        /// <param name="baseUrl">联系Server的域名基地址</param>
        /// <param name="accessKey">内部验证的AccessKey(太特么简陋了....   _(:зゝ∠)_   )</param>
        public void StartService(string baseUrl, string accessKey)
        {
            m_baseUrl = baseUrl;
            m_accessKey = accessKey;
            m_LoaclMac = GetLoacalMAC();
            m_backThread = new Thread(ThreadMethodHandle);
            m_backThread.IsBackground = true;
            m_backThread.Start();
        }
        /// <summary>
        /// 后台线程入口函数
        /// </summary>
        private void ThreadMethodHandle()
        {
            while (true)
            {
                try
                {
                    string responseCityListStr = GetCityListFromServer();
                    bool isHaveCity;
                    string currentAK = string.Empty;
                    string[] cityList = GetCityList(responseCityListStr, out isHaveCity, out currentAK);
                    List<AirDataInfo> cityInfoList = null;
                    if (isHaveCity)
                    {
                        cityInfoList = GetCityInfo(cityList, currentAK);
                    }
                    else
                    {
                        LogInfoHelper.WriteRunTimeMessage("请求城市列表但城市列表为空,无城市可刷新数据", RunTimeMessageType.Log, true);
                    }
                    try
                    {
                        if (isHaveCity && cityInfoList != null && cityInfoList.Count > 0)
                        {
                            SendCityInfo(cityInfoList);
                        }
                    }
                    catch (Exception exp)
                    {
                        LogInfoHelper.WriteRunTimeMessage(string.Format("向服务器提交城市数据时发生异常\r\n 异常消息为->{0}\r\n异常堆栈信息->{1}", exp.Message, exp.StackTrace), RunTimeMessageType.Exception, true);
                    }
                }
                catch (Exception exp)
                {
                    LogInfoHelper.WriteRunTimeMessage(string.Format("最外层捕获到的异常信息,最后的查找点  异常信息为 {0}  堆栈信息为{1}",exp.Message,exp.StackTrace),RunTimeMessageType.Exception,true);
                    continue;
                }
                Thread.Sleep(600000);
            }
        }
        /// <summary>
        /// 从服务器上 获取此次要请求信息的城市列表
        /// </summary>
        /// <returns></returns>
        private string GetCityListFromServer()
        {
            string requrl = string.Format("{0}?accesskey={1}&MAC={2}&requesttype={3}", m_baseUrl, m_accessKey, m_LoaclMac, "getcitylist");
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(requrl);
            request.Method = "GET";
            request.Timeout = 120000;
            WebResponse response = request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            if (responseStream.CanSeek && responseStream.CanRead)
            {
                responseStream.Seek(0, SeekOrigin.Begin);
            }
            StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
            string responseStr = reader.ReadToEnd();
            if (!string.IsNullOrEmpty(responseStr))
            {
                LogInfoHelper.WriteRunTimeMessage(string.Format("发出请求城市列表请求 URL为{0} \r\n 服务器相应数据为{1}", requrl, responseStr), RunTimeMessageType.Log, true);
            }
            else
            {
                LogInfoHelper.WriteRunTimeMessage(string.Format("服务器返回数据接收错误   发出请求城市列表请求 URL为{0}", requrl), RunTimeMessageType.Exception, true);
            }
            return responseStr;
        }
        /// <summary>
        /// 根据服务器返回的字符串信息返回此次分配的城市列表，并返回是否存在城市可以请求的bool值 、如果有则返回此次请求分配的key
        /// </summary>
        /// <param name="responseStr">服务器返回的城市列表字符串</param>
        /// <param name="isHaveCity">返回是否存在城市可以请求</param>
        /// <param name="currentKey">如果有城市可以请求则返回此次请求分配使用key</param>
        /// <returns></returns>
        private string[] GetCityList(string responseStr, out bool isHaveCity, out string currentKey)
        {
            string[] result = null;
            isHaveCity = false;
            currentKey = string.Empty;
            try
            {
                string[] splitArr = responseStr.Split(new string[] { "\r\n" }, 10, StringSplitOptions.RemoveEmptyEntries);
                if (splitArr.Length == 3)
                {
                    string[] strArr = splitArr[0].Split(':');
                    #region  Status 部分
                    if (strArr.Length == 2 && strArr[0] == "status")
                    {
                        if (strArr[1] == "OK")
                        {
                            isHaveCity = true;
                        }
                        else
                        {
                            return result;
                        }
                    }
                    else
                    {
                        WriteAnalyseException(responseStr);
                    }
                    #endregion

                    strArr = splitArr[1].Split(':');
                    #region  AK部分
                    if (strArr.Length == 2 && strArr[0] == "ak")
                    {
                        if (!string.IsNullOrEmpty(strArr[1]) && strArr[1].Length == 32)
                        {
                            currentKey = strArr[1];
                        }
                    }
                    else
                    {
                        WriteAnalyseException(responseStr);
                    }
                    #endregion

                    strArr = splitArr[2].Split(':');
                    #region  CityList 部分
                    if (strArr.Length == 2 && strArr[0] == "message")
                    {
                        if (!string.IsNullOrEmpty(strArr[1]))
                        {
                            string cityListStr = strArr[1];
                            string[] singleCityNameArr = cityListStr.Split(',');
                            if (singleCityNameArr.Length > 0)
                            {
                                result = singleCityNameArr;
                            }
                        }
                    }
                    else
                    {
                        WriteAnalyseException(responseStr);
                    }
                    #endregion
                }
                else
                {
                    WriteAnalyseException(responseStr);
                }
            }
            catch (Exception exp)
            {
                LogInfoHelper.WriteRunTimeMessage(string.Format("分析城市列表过程中发生CLR异常\r\n 异常消息为 {0}\r\n 异常调用栈为 {1}\r\n 响应体信息为{2}\r\n ", exp.Message, exp.StackTrace, responseStr), RunTimeMessageType.Exception, true);
            }
            return result;
        }
        /// <summary>
        /// 使用指定的AK和城市列表返回城市列表中的城市的天气信息集合
        /// </summary>
        /// <param name="cityList">城市名称的数组</param>
        /// <param name="currentAk">用于此次请求的AK信息</param>
        /// <returns></returns>
        private List<AirDataInfo> GetCityInfo(string[] cityList, string currentAk)
        {
            List<AirDataInfo> result = new List<AirDataInfo>();
            foreach (string cityName in cityList)
            {
                string Url = "https://free-api.heweather.com/v5/weather?key=@key&city=@city";
                Url = Url.Replace("@key", currentAk);
                Url = Url.Replace("@city", cityName);
                GetHTTPInfo getHttp = new GetHTTPInfo();
                string airInfoResponse = getHttp.GetHttpResponse(Url);
                IAnalyseWeatherAble analyse = new AnalyseHeFeng();
                AirDataInfo airInfo;
                try
                {
                    airInfo = analyse.GetAirDataInfo(cityName, airInfoResponse);
                    result.Add(airInfo);
                }
                catch (Exception exp)
                {
                    LogInfoHelper.WriteRunTimeMessage(string.Format("解析天气字符信息时出现异常 \r\r 异常信息为:{0}\r\n  调用堆栈信息为 :{1}",exp.Message,exp.StackTrace),RunTimeMessageType.Exception,true);
                }
            }
            return result;
        }
        /// <summary>
        /// 根据城市信息集合获取应该发送给服务器的form数据字符串
        /// </summary>
        /// <param name="cityInfoList">获取到天气信息的城市信息集合</param>
        /// <returns></returns>
        private string GetFormStr(List<AirDataInfo> cityInfoList)
        {
            string result = string.Empty;
            Type type = typeof(AirDataInfo);
            PropertyInfo[] properties= type.GetProperties();
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("{");
            //遍历城市信息
            foreach (AirDataInfo dataInfo in cityInfoList)
            {
                //最后会产生一个 单个城市信息字符串[key=value,key=value,key=value、、、、]格式的字符串
                string singleCityStr = string.Empty;
                StringBuilder sb = new StringBuilder();
                sb.Append("[");
                foreach (PropertyInfo singleProperty in properties)
                {
                    string key = singleProperty.Name;
                    string value = singleProperty.GetValue(dataInfo, null).ToString();
                    sb.Append(string.Format("{0}={1},", key, value));
                }
                string temp = sb.ToString().TrimEnd(',');
                singleCityStr = temp + "]|";
                stringBuilder.Append(singleCityStr);
            }
            string temp2 = stringBuilder.ToString().TrimEnd(',');
            result = temp2 + "}";
            return result;
        }
        /// <summary>
        /// 向服务器中发送此次获取到的城市数据信息  并返回服务器返回的响应信息
        /// </summary>
        /// <param name="cityInfoList">获取到的城市信息集合 </param>
        /// <returns></returns>
        private bool SendCityInfo(List<AirDataInfo> cityInfoList)
        {
            string requrl = string.Format("{0}?accesskey={1}&MAC={2}&requesttype={3}", m_baseUrl, m_accessKey, m_LoaclMac, "updatecitydata");
            string formDate = GetFormStr(cityInfoList);
            //string encodedFormData = HttpUtility.UrlEncode(formDate,Encoding.GetEncoding("gb2312"));
            byte[] formDateBytes = Encoding.UTF8.GetBytes(formDate);
            System.Net.
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(requrl);
            request.Method = "POST";
            request.ContentType = "multipart/form-data";
            request.Timeout = 120000;
            request.ContentLength = formDateBytes.Length;
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(formDateBytes, 0, formDateBytes.Length);
            }
            using (WebResponse wr = request.GetResponse())
            {
                using (Stream stream = wr.GetResponseStream())
                {
                    if (stream.CanSeek)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        StreamReader reader = new StreamReader(stream);
                        string responseStr = reader.ReadToEnd();
                        LogInfoHelper.WriteRunTimeMessage(string.Format("服务器响应更新城市数据的字符信息 {0}", responseStr), RunTimeMessageType.Log, true);
                    }
                    else
                    {
                        StreamReader reader = new StreamReader(stream);
                        string responseStr = reader.ReadToEnd();
                        LogInfoHelper.WriteRunTimeMessage(string.Format("服务器响应更新城市数据的字符信息 {0}", responseStr), RunTimeMessageType.Log, true);
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// 封装专职用于写入解析server返回数据异常的方法
        /// </summary>
        /// <param name="responseStr"></param>
        private void WriteAnalyseException(string responseStr)
        {
            LogInfoHelper.WriteRunTimeMessage(string.Format("分析城市列表响应结果时出现异常 \r\n返回的字符串信为{0}", responseStr), RunTimeMessageType.Exception, true);
        }
        /// <summary>
        /// 获取本地网络适配器的一个Mac
        /// </summary>
        /// <returns></returns>
        private string GetLoacalMAC()
        {
            string Mac = string.Empty;
            NetworkInterface[] NICArr = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface nic in NICArr)
            {
                Mac = nic.GetPhysicalAddress().ToString();
                if (!string.IsNullOrEmpty(Mac))
                {
                    m_LoaclMac = Mac;
                    break;
                }
            }
            return Mac;
        }
    }
}
