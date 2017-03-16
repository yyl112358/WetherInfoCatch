using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Net;
using System.Reflection;


namespace WeatherServiceWeb
{
    /// <summary>
    /// Business 的摘要说明
    /// </summary>
    public class Business:IDisposable
    {
        // 次程序内部自己使用的accessKey
        private static string m_AccessKey = "8A1F86548CDD406D880F631507650159";
        private DatabaseAccess m_access = new DatabaseAccess();
        public Business()
        {
        }
        /// <summary>
        /// 验证当前接入Key是否合法
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool AccessKeyValidate(string key)
        {
            if (key == m_AccessKey)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取城市列表请求的返回字符串
        /// </summary>
        /// <param name="clientMac">客户端Mac地址</param>
        /// <returns></returns>
        public string GetCityList(string clientMac)
        {
            /// 返回数据格式(！！！重要！！！)
            /// status:状态信息\r\n              //此态次请求结果的状信息(OK/NULL)
            /// ak:api的acesskeyInfo\r\n
            /// message:负载数据信息部分\r\n   //此次请求所返回的城市列表(若有数据返回)
            /// 负载信息部分 格式 城市1，城市2，城市3.......\r\n


            //从数据库获取分配的城市列表
            string akStr = string.Empty;

            //LogInfoHelper.WriteRunTimeMessage(string.Format("clientMac{0}  RequestCityList", clientMac), RunTimeMessageType.Log, true);
            
            List<string> cityList = m_access.GetNoFreashedCityList(clientMac, out akStr);
            //当前无城市可分配
            if (cityList == null || cityList.Count == 0)
            {
                string result = string.Format("status:{0}\r\nak:{1}\r\nmessage:{2}\r\n", "NULL", "", "");
                return result;
            }
            else

            {
                string result = "status:OK\r\nak:" + akStr + "\r\nmessage:";
                foreach (string cityName in cityList)
                {
                    result += cityName + ",";
                }
                result = result.TrimEnd(',') + "\r\n";
                return result;
            }
        }

        /// <summary>
        /// 向数据库中插入客户端拿到的数据 并返回要向客户端报告的执行结果
        /// </summary>
        /// <returns></returns>
        public string InsertDate(string remoteIp,string clientMac, string formData)
        {
            string result = string.Empty;
            LogInfoHelper.WriteRunTimeMessage(string.Format("远程客户端IP:[{0}]  ,   MAC:[{1}] 提交了formData\r\n{2}", remoteIp, clientMac.ToUpper(), formData), RunTimeMessageType.Log, true);
            //分析from中的cityList信息
            List<AirDataInfo> cityList = AnayFormData(formData);
            string[] insertedCityName =null;
            try
            {
                insertedCityName= m_access.InsertCityListInfo(cityList, clientMac);
            }
            catch (Exception exp)
            {
                LogInfoHelper.WriteRunTimeMessage(string.Format("插入客户端提交过来的数据时发生异常,异常信息为{0}\r\n调用堆栈为{1}",exp.Message,exp.StackTrace), RunTimeMessageType.Exception, true);
            }
            if (insertedCityName.Length > 0)
            {
                LogInfoHelper.WriteRunTimeMessage(string.Format("远程客户端IP:{0},MAC:[{1}]此次提交成功插入城市{2}个", remoteIp, clientMac.ToUpper(), insertedCityName.Length), RunTimeMessageType.Log, true);
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("status:OK\r\nak:\r\nmessage:成功接收到城市数据 且成功插入到数据库的城市数据有:\r\n");
                foreach (string cityName in insertedCityName)
                {
                    sb.Append(cityName + ",");
                }
                result = sb.ToString().TrimEnd(',')+"\r\n";
            }
            else
            {
                result =string.Format("status:{0}\r\nak:{1}\r\nmessage:{2}\r\n", "OK", "", "成功接收数据但是没有插入数据");
            }
            return result;
        }
        /// <summary>
        /// 将form中的城市信息解析为城市天气信息的实例集合
        /// </summary>
        /// <param name="formDate">客户端服务提交上来的额form信息</param>
        /// <returns></returns>
        private List<AirDataInfo> AnayFormData(string formDate)
        {
            List<AirDataInfo> resultList = new List<AirDataInfo>();
            try
            {
                //form中数据格式为 data = {[cityInfo],[cityInfo],[cityInfo].....}   [cityInfo]格式 [key=value,key=value,key=value........]
                formDate = formDate.Trim('{');
                formDate = formDate.Trim('}');
                string[] singleCityInfoArr = formDate.Split(new string[]{"|"},StringSplitOptions.RemoveEmptyEntries);
                //singleCityInfoArr中 格式应为 1. [cityInfo]    2. [cityInfo]  、、、、
                foreach (string singleCityInfoStr in singleCityInfoArr)
                {
                    string cityInfo = singleCityInfoStr.TrimStart('[').TrimEnd(']');
                    string[] keyValueArr = cityInfo.Split(',');
                    //[cityInfo]中单个属性的格式为  [key=value,key=value,key=value、、、、]
                    //keyValueArr 中格式应为 key=value,key=value,key=value、、、、、
                    Dictionary<string, string> airInfoDic = new Dictionary<string, string>();
                    foreach (string keyValueInfo in keyValueArr)
                    {
                        string key = keyValueInfo.Split('=')[0];
                        string value = keyValueInfo.Split('=')[1];
                        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                        {
                            airInfoDic.Add(key, value);
                        }
                    }
                    AirDataInfo newCityInfo = new AirDataInfo();
                    Type airInfoType = newCityInfo.GetType();
                    PropertyInfo[] properties = airInfoType.GetProperties();                  
                    //反射的方式循环设置相应属性的值
                    foreach (PropertyInfo property in properties)
                    {
                        try
                        {
                            //该属性为int类型
                            if (property.PropertyType == typeof(int))
                            {
                                int value = int.Parse(airInfoDic[property.Name]);
                                property.SetValue(newCityInfo, value, null);
                            }
                            else if (property.PropertyType == typeof(DateTime))
                            {
                                DateTime value = DateTime.Parse(airInfoDic[property.Name]);
                                property.SetValue(newCityInfo, value, null);
                            }
                            //该属性为string类型
                            else
                            {
                                property.SetValue(newCityInfo, airInfoDic[property.Name], null);
                            }
                        }
                        catch (Exception exp)
                        {
                            LogInfoHelper.WriteRunTimeMessage(string.Format("反射设置对象属性值时发生异常 属性名为{0}  dic中value为{1}   exp.Messge 为{2}", property.Name, airInfoDic[property.Name], exp.Message), RunTimeMessageType.Exception,true);
                        }
                    }
                    resultList.Add(newCityInfo);
                }//第一层Foreach
            }
            catch (Exception exp)
            {
                string message = string.Format("解析客户端提交上来的form数据异常 form数据为{0}  \r\n 异常信息为 {1}", formDate, exp.Message);
                LogInfoHelper.WriteRunTimeMessage(message,RunTimeMessageType.Exception,true);
            }
            return resultList;
        }

        public void Dispose()
        {
            m_access.Dispose();
        }
    }
}