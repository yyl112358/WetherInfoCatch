using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceDemo
{
    /// <summary>
    /// 分析天气信息的处理类的接口
    /// </summary>
    interface IAnalyseWeatherAble
    {
        /// <summary>
        /// 根据包含空气质量信息的字符串和天气信息实例判断并返回当前的最新天气信息
        /// </summary>
        /// /// <param name="city">获取城市的cityInfo实例</param>
        /// <param name="AirInfoStrings">包含天气信息的字符串</param>
        /// <returns>如果信息新鲜返回新天气信息否则返回Null</returns>
        AirDataInfo GetAirDataInfo(string cityName, string AirInfoStrings);
    }
}
