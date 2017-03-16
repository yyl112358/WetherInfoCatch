using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WeatherServiceWeb
{
    /// <summary>
    /// Service 的摘要说明
    /// </summary>
    public class Service : IHttpHandler
    {
        /*请求URL格式
         * Service.ashx?accesskey=指定的ak&MAC=请求机器的MAC地址&requesttype=请求类型  (requesttype 有两种类型  getcitylist、updatecitydata)
         * form中数据格式为 data = {[cityInfo],[cityInfo],[cityInfo].....}
         */
        public void ProcessRequest(HttpContext context)
        {
            Business business = new Business();
            try
            {
                LogInfoHelper.WriteRunTimeMessage("客户端请求 原始信息 ->" + context.Request.RawUrl, RunTimeMessageType.Log, true);
                context.Response.ContentType = "text/plain";
                HttpRequest request = context.Request;
                HttpResponse response = context.Response;
                string accesskey = request.QueryString["accesskey"];
                #region   接入key 判断部分
                if (accesskey == null)
                {
                    responseInfo(response, "error", "access key is null");
                    return;
                }
                if (!business.AccessKeyValidate(accesskey))
                {
                    responseInfo(response, "error", "access key illegal");
                    return;
                }
                #endregion

                #region  业务部分
                //MAC 获取部分
                string clinetMac = request.QueryString["MAC"].ToLower();
                if (clinetMac == null)
                {
                    responseInfo(response, "error", "clientMAC is null");
                    return;
                }
                //此次请求的业务类型
                string requestType = request.QueryString["requesttype"].ToLower();
                if (requestType == null)
                {
                    responseInfo(response, "error", "requesttype is null");
                    return;
                }
                switch (requestType)
                {
                    case "getcitylist":

                        string result = business.GetCityList(clinetMac);
                        LogInfoHelper.WriteRunTimeMessage(string.Format("clientMac:[{0}]的获取城市列表Business层返回的字符结果为\r\n{1}", clinetMac.ToUpper(), result), RunTimeMessageType.Log, true);
                        response.Write(result);
                        response.Flush();
                        break;
                    case "updatecitydata":
                        //form中数据格式为 data = {[cityInfo],[cityInfo],[cityInfo].....}
                        System.IO.Stream inputStream = context.Request.InputStream;
                        string formData = GetFormStr(inputStream);
                        string insResult = business.InsertDate(context.Request.UserHostAddress, clinetMac, formData);
                        response.Write(insResult);
                        response.Flush();
                        break;
                    default:
                        responseInfo(response, "error", "requesttype is illegal");
                        return;
                }
                #endregion
            }
            catch (Exception exp)
            {
                LogInfoHelper.WriteRunTimeMessage(string.Format("Process 函数内捕捉到的异常 \r\n  Message :{0}  \r\n  堆栈信息:{1}", exp.Message, exp.StackTrace), RunTimeMessageType.Exception, true);
            }
            finally
            {
                business.Dispose();
            }
            
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
        /// <summary>
        /// 封装的标准响应的函数 
        /// </summary>
        /// <param name="status">响应的状态</param>
        /// <param name="message">响应的信息内容</param>
        private void responseInfo(HttpResponse response, string status, string message)
        {
            response.Write(string.Format("status:{0}\r\nmessage:{1}", status, message));
            response.Flush();
        }

        /// <summary>
        ///获取输入的Form信息
        /// </summary>
        /// <param name="stream">request的输入流信息</param>
        /// <returns></returns>
        private string GetFormStr(System.IO.Stream stream)
        {
            if (stream.CanSeek)
            {
                stream.Seek(0, System.IO.SeekOrigin.Begin);
                System.IO.StreamReader reader = new System.IO.StreamReader(stream,System.Text.Encoding.UTF8);
                string formStr = reader.ReadToEnd();
                return formStr;
            }
            return null;
        }
    }
}