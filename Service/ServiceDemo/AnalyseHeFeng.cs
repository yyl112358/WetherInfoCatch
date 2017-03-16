using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceDemo
{
    public class AnalyseHeFeng : IAnalyseWeatherAble
    {
        /// <summary>
        /// 获取的HTTP中包含的空气污染相关指数信息(若数据无更新则返回null)
        /// </summary>
        /// <param name="city">城市信息(主要用于对比当前数据的新鲜度)</param>
        /// <param name="HttpResponseBody">请求到的HTTP响应体</param>
        /// <returns></returns>
        public AirDataInfo GetAirDataInfo(string cityName, string AirInfoStrings)
        {
            AirDataInfo airInfo = new AirDataInfo();
            airInfo.CityName = cityName;
            string updateEndStr = AirInfoStrings.Substring(AirInfoStrings.IndexOf("loc"), 100);
            string update = updateEndStr.Substring(6, 16);
            DateTime dt_update = DateTime.Parse(update);
            try
            {
                //仅包含当前天气信息预处理过字符串
                string nowInfoStr = AirInfoStrings.Substring(AirInfoStrings.IndexOf("now"));
                int intValue;
                string stringValue;
                getIntTypePullutionInfo("aqi", AirInfoStrings, out intValue);
                airInfo.AQI = intValue;
                getIntTypePullutionInfo("pm25", AirInfoStrings, out intValue);
                airInfo.PM25 = intValue;
                getIntTypePullutionInfo("pm10", AirInfoStrings, out intValue);
                airInfo.PM10 = intValue;
                getStringTypePullutionInfo("qlty", AirInfoStrings, out stringValue);
                airInfo.PollutionIndex = stringValue;
                getIntTypePullutionInfo("pres", nowInfoStr, out intValue);
                airInfo.Pressure = intValue;
                getStringTypePullutionInfo("txt", nowInfoStr, out stringValue);
                airInfo.NowWeatherText = stringValue;
                getStringTypePullutionInfo("sc", nowInfoStr, out stringValue);
                airInfo.FL = stringValue;
                getIntTypePullutionInfo("spd", nowInfoStr, out intValue);
                airInfo.FS = intValue;
                getIntTypePullutionInfo("hum", nowInfoStr, out intValue);
                airInfo.Hum = intValue;
                getIntTypePullutionInfo("tmp", nowInfoStr, out intValue);
                airInfo.Temp = intValue;
                airInfo.UpdateTime = dt_update;
                airInfo.Week = (int)airInfo.UpdateTime.DayOfWeek;
                //更新时间为本次获取的更新时间以！！！！重要！！！！用于下次判断数据新鲜度使用
            }
            catch (Exception exp)
            {
                //所有异常的写文件处理放在请求线程
                throw exp;
            }
            return airInfo;
        }

        #region   解析不同空气指数函数部分
        /// <summary>
        /// 返回响应体中需要的int类型的污染指数信息 （PS：可解析aqi、pm25、pm10、气压）
        /// </summary>
        /// <param name="pullutionName">污染指数的名称</param>
        /// <param name="httpResponse">http响应体字符串</param>
        /// <returns></returns>
        private void getIntTypePullutionInfo(string pullutionName, string httpResponse, out int value)
        {
            value = -404;
            //返回天气信息中不包含当前信息 抛出异常
            if (httpResponse.LastIndexOf(pullutionName) < 0)
            {
                throw new Exception("当前响应流中不包含" + pullutionName + "的信息,相应信息为----->" + httpResponse);
            }
            //以污染指数名称字符开头以后的15个字符串
            string PullutionNameEndStr = httpResponse.Substring(httpResponse.LastIndexOf(pullutionName), 15);
            //污染指数名称数字部分开头的字符串
            string PullutionNameNumStartStr = PullutionNameEndStr.Substring((pullutionName.Length + 3));
            //污染指数名称对应的字符串
            string PullutionNameNumStr = PullutionNameNumStartStr.Substring(0, PullutionNameNumStartStr.IndexOf("\""));
            int PullutionNum;
            try
            {
                PullutionNum = int.Parse(PullutionNameNumStr);
                value = PullutionNum;
            }
            catch (Exception exp)
            {
                PullutionNum = -1;
                throw exp;
            }
        }
        /// <summary>
        /// 返回响应体中需要的string类型的污染指数信息 （PS：可解析 污染指数、风力、当前天气信息）
        /// </summary>
        /// <param name="pullutionName"></param>
        /// <param name="httpResponse"></param>
        /// <returns></returns>
        private void getStringTypePullutionInfo(string pullutionName, string httpResponse, out string value)
        {
            value = "没有找到该字段╮(╯▽╰)╭Call Admin";
            if (httpResponse.IndexOf(pullutionName) < 0)
            {
                throw new Exception("没有找到该字段{" + pullutionName + "}╮(╯▽╰)╭Call Admin");
            }
            string PullutionIndexEndStr = httpResponse.Substring(httpResponse.IndexOf(pullutionName), 20);
            //污染指数名称数字部分开头的字符串
            string PullutionIndexStartStr = PullutionIndexEndStr.Substring(pullutionName.Length + 3);
            //污染指数名称对应的字符串
            string PullutionIndexStr = PullutionIndexStartStr.Substring(0, PullutionIndexStartStr.IndexOf("\""));
            value = PullutionIndexStr;
        }
        #endregion
    }
}
