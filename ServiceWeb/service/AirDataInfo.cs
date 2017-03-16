using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WeatherServiceWeb
{
    public class AirDataInfo
    {
        private int m_AQI;
        /// <summary>
        /// 综合空气指数
        /// </summary>
        public int AQI
        {
            get { return m_AQI; }
            set { m_AQI = value; }
        }

        private DateTime m_UpdateTime;
        /// <summary>
        /// 空气质量信息更新时间
        /// </summary>
        public DateTime UpdateTime
        {
            get { return m_UpdateTime; }
            set { m_UpdateTime = value; }
        }

        private int m_PM25;
        /// <summary>
        /// PM2.5指数
        /// </summary>
        public int PM25
        {
            get { return m_PM25; }
            set { m_PM25 = value; }
        }

        private int m_PM10;
        /// <summary>
        /// PM10 指数
        /// </summary>
        public int PM10
        {
            get { return m_PM10; }
            set { m_PM10 = value; }
        }

        private string m_PollutionIndex;
        /// <summary>
        /// 文字描述的污染指数
        /// </summary>
        public string PollutionIndex
        {
            get { return m_PollutionIndex; }
            set { m_PollutionIndex = value; }
        }

        private string m_FL;
        /// <summary>
        /// 风力
        /// </summary>
        public string FL
        {
            get { return m_FL; }
            set { m_FL = value; }
        }

        private int m_FS;
        /// <summary>
        /// 风速信息
        /// </summary>
        public int FS
        {
            get { return m_FS; }
            set { m_FS = value; }
        }

        private int m_Pressure;
        /// <summary>
        /// 气压信息 
        /// </summary>
        public int Pressure
        {
            get { return m_Pressure; }
            set { m_Pressure = value; }
        }

        private string m_NowWeatherText;
        /// <summary>
        /// 当前天气文字描述信息
        /// </summary>
        public string NowWeatherText
        {
            get { return m_NowWeatherText; }
            set { m_NowWeatherText = value; }
        }

        private string m_CityName;
        /// <summary>
        /// 城市名称
        /// </summary>
        public string CityName
        {
            get { return m_CityName; }
            set { m_CityName = value; }
        }

        private int m_Temp;
        /// <summary>
        /// 当前温度
        /// </summary>
        public int Temp
        {
            get { return m_Temp; }
            set { m_Temp = value; }
        }

        private int m_Hum;
        /// <summary>
        /// 当前湿度
        /// </summary>
        public int Hum
        {
            get { return m_Hum; }
            set { m_Hum = value; }
        }

        private int m_Week;
        /// <summary>
        /// 该条天气的星期信息
        /// </summary>
        public int Week
        {
            get { return m_Week; }
            set { m_Week = value; }
        }
    }
}
