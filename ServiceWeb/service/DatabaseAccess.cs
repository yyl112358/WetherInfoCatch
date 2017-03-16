using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MySql.Data.MySqlClient;
using System.Data;
using System.Configuration;
using System.Data.Common;
using System.Data.Sql;

namespace WeatherServiceWeb
{
    /*
     *该类为 数据库操作 相关类 供business类实例化调用 
     *@ PS 此类 实例化时 即会打开数据库连接 内部所有函数将 共享该连接实例以节约系统资源且为同一个session保证事务的ACID
     *business 完成一个业务逻辑时 务必 调用DisconnectDatabase() 方法 释放连接
     */

    /// <summary>
    /// DatabaseAccess 的摘要说明
    /// </summary>
    public class DatabaseAccess:IDisposable
    {
        //连接字符串   (备忘: 部署时将服务器上 host文件设置 以优化 dns查询过程)
        private string constr = string.Empty;
        private MySqlConnection conn;
        public DatabaseAccess()
        {
            constr = ConfigurationManager.ConnectionStrings["mysql"].ConnectionString;
            conn = new MySqlConnection(constr);
            conn.Open();
        }
        /// <summary>
        /// 获取最新的一小时没有被刷新数据的城市列表 且返回此次请求所使用的akStr
        /// (PS:此时对表加了write锁以控制并发  非长久之计！！高并发情况下要首先考虑优化此处)
        /// </summary>
        /// <returns></returns>
        public List<string> GetNoFreashedCityList(string RemoteMac, out string akStr)
        {
            List<string> cityList = new List<string>();
            akStr = "";
            LockTable(new string[] { "citylistinfo", "akinfo" }, SqlLockType.write);
            #region   获取当前还有多少城市没有被更新
            //当前小时时段内没有被更新的或者分配过机器去更新但是5分钟没有被更新的数量
            string countsql = @"-- 20170216版
select count(*) as count from citylistinfo 
where  
-- 且如果上一次更新时间的日期不等于当前时间的日期时上一次的更新时间不是在23点(跨天bug)
((day(LastFreashTime)!= day(now()) and hour(LastFreashTime)<23) 
or 
(day(LastFreashTime)!=day(now()) and hour(AssignTime)=0 and hour(now())>0)
or
-- 或者 上一次更新日期等于当前系统日期 但是上一次更新早于当前的系统时间的小时部分
((day(LastFreashTime)=day(now())) 
and 
abs(hour(lastfreashtime)-hour(now()))>1))
or
--  上面的部分bool值 或者 分配给某个MAC地址的城市5分钟后还没有回送上一个小时的数据
((TIMESTAMPDIFF(MINUTE,assigntime,now())>5) and (TIMESTAMPDIFF(HOUR,LastFreashTime,AssignTime)>0))
";
            MySqlCommand cmd = new MySqlCommand(countsql, conn);
            MySqlDataReader reader = cmd.ExecuteReader();
            reader.Read();
            int noFreashCityCount = int.Parse(reader["count"].ToString());
            reader.Close();
            //当前一个小时内 所有城市数据都是新的
            if (noFreashCityCount == 0)
            {
                UnLockTable();
                return cityList;
            }
            #endregion
            #region  计算该次请求要分配的城市列表数量
            int nowMinute = DateTime.Now.Minute / 10;
            int assignCount = 0;
            //按当前时间在一小时内所处的位置分发任务量(此处有CAST丢失精度问题 ！！格外注意)
            if (noFreashCityCount < 5)
            {
                assignCount = noFreashCityCount;
            }
            else
            {
                //nowminute 处理
                nowMinute = nowMinute == 0 ? 1 : nowMinute;
                nowMinute = nowMinute == 5 ? 4 : nowMinute;

                assignCount = (int)(noFreashCityCount * ((nowMinute + 1) / 5.0));
            }
            #endregion
            #region 获取和更新数据
            //当前小时时段内没有被更新的或者分配过机器去更新但是5分钟没有被更新的城市列表
            string sql = @"-- 20170311版
select * from citylistinfo 
where  
-- 且如果上一次更新时间的日期不等于当前时间的日期时上一次的更新时间不是在23点(跨天bug)
((day(LastFreashTime)!= day(now()) and hour(LastFreashTime)<23) 
or 
(day(LastFreashTime)!=day(now()) and hour(AssignTime)=0 and hour(now())>0)
or
-- 或者 上一次更新日期等于当前系统日期 但是上一次更新早于当前的系统时间的小时部分
((day(LastFreashTime)=day(now())) 
and 
abs(hour(lastfreashtime)-hour(now()))>1))
or
--  上面的部分bool值 或者 分配给某个MAC地址的城市5分钟后还没有回送上一个小时的数据
((TIMESTAMPDIFF(MINUTE,assigntime,now())>5) and (TIMESTAMPDIFF(HOUR,LastFreashTime,AssignTime)>0))
";
            //Adt 貌似是会自己关连接的
            /// MySqlDataAdapter adt = new MySqlDataAdapter(sql, conn);
            cmd.CommandText = sql;
            reader = cmd.ExecuteReader();
            int i = 0;
            while (reader.Read())
            {
                cityList.Add(reader["CityName"].ToString());
                i++;
                if (i == assignCount)
                {
                    break;
                }
            }
            reader.Close();
            if (cityList.Count > 0)
            {
                //获取ak
                string ak = GetAKByCount(cityList.Count);
                //out返回值
                akStr = ak;
                if (string.IsNullOrEmpty(ak))
                {
                    throw new Exception(string.Format("{0}  引发异常--> 没有满足请求count值的ak可用   请求的count值为{1}", DateTime.Now, cityList.Count));
                }
                //更新分配时间和MAC字段
                foreach (string cityName in cityList)
                {
                    cmd.CommandText = string.Format("update citylistinfo set assigntime=SYSDATE(),assignmachinemac ='{0}' where CityName='{1}'", RemoteMac, cityName);
                    int execResultNum = cmd.ExecuteNonQuery();
                    if (execResultNum == 1)
                    {
                        continue;
                    }
                    else
                    {
                        LogInfoHelper.WriteRunTimeMessage(string.Format("更新城市分配信息时发生异常 城市名称为{0}  客户端MAC地址为{1]", cityName, RemoteMac), RunTimeMessageType.Exception, true);
                    }
                }
            }
            #endregion
            //释放表级锁(！！重要！！)
            UnLockTable();
            //释放数据库连接(！！重要！！)
            DisconnectDatabase();
            return cityList;
        }

        /// <summary>
        /// 根据指定的城市数量获取API的AccessKey的值
        /// </summary>
        /// <param name="count">期望获取的城市的数量</param>
        /// <returns></returns>
        private string GetAKByCount(int count)
        {
            #region  判断count新鲜度信息
            //获取当前ak信息
            MySqlCommand cmd = new MySqlCommand("select lastFreashCountTime from akinfo limit 0,2", conn);
            DbDataReader reader = cmd.ExecuteReader();
            DateTime lastFreash = DateTime.Now;
            if (reader.Read())
            {
                DateTime.TryParse(reader["lastFreashCountTime"].ToString(), out lastFreash);
                reader.Close();
            }
            //count还是上一天的数据
            if (lastFreash.DayOfYear < DateTime.Now.DayOfYear)
            {
                FreashAkCountInfo();
            }
            #endregion

            cmd.CommandText = "select * from akinfo where count>" + count.ToString();
            reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                string result = reader["acKey"].ToString();
                string noUseCountStr = reader["count"].ToString();
                int noUseCount = int.Parse(noUseCountStr);
                int resultCount = noUseCount - count;
                reader.Close();
                //更新剩余count信息
                cmd.CommandText = "update akinfo set count=?count where acKey =?key;";
                cmd.Parameters.Add(new MySqlParameter("?count", resultCount));
                cmd.Parameters.Add(new MySqlParameter("?key", result));
                int resultNum = cmd.ExecuteNonQuery();
                if (resultNum == 1)
                {
                    return result;
                }
                else
                {
                    reader.Close();
                    throw new Exception("更新AK count信息时发生异常");
                }
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 刷新今天的AK表中 Count信息
        /// </summary>
        /// <returns></returns>
        private void FreashAkCountInfo()
        {
            MySqlCommand cmd = new MySqlCommand("update akinfo set count=3000,lastfreashcounttime=now();", conn);
            int result = cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 将指定客户端Mac地址提交上的CityInfo插入到数据库中并返回成功插入的城市的列表
        /// </summary>
        /// <param name="cityInfoList">客户端服务提交上来的城市数据列表</param>
        /// <param name="clientMac">客户端MAC地址</param>
        /// <returns></returns>
        public string[] InsertCityListInfo(List<AirDataInfo> cityInfoList, string clientMac)
        {
            LockTable(new string[] { "citylistinfo", "serviceairinfotable" }, SqlLockType.write);
            List<string> resultList = new List<string>();
            string[] noFreashCityArr = GetNoFreashCityListByClientMac(clientMac);
            if (noFreashCityArr.Length > 0)
            {
                foreach (string cityName in noFreashCityArr)
                {
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = conn;
                    IEnumerable<AirDataInfo> Enumer = cityInfoList.Where(t => t.CityName == cityName);
                    IEnumerator<AirDataInfo> itertor = Enumer.GetEnumerator();
                    bool haveThisCity =itertor.MoveNext();
                    //未刷新城市在当前List中存在
                    if (haveThisCity)
                    {
                        AirDataInfo airInfo = itertor.Current;
                        cmd.CommandText = "INSERT INTO serviceairinfotable(AQI,UpdateTime,PM25,PM10,PolText,FL,FS,Pressure,Text,cityName,Temp,ShiDu,Week) VALUES(?aqi,?updatetime,?pm25,?pm10,?pollutionindex,?fl,?fs,?pressure,?nowweathertext,?cityname,?temperature,?shidu,?week)";
                        MySqlParameter[] param = new MySqlParameter[]
                        {
                            new MySqlParameter("?aqi",airInfo.AQI),
                            new MySqlParameter("?updatetime",airInfo.UpdateTime),
                            new MySqlParameter("?pm25",airInfo.PM25),
                            new MySqlParameter("?pm10",airInfo.PM10),
                            new MySqlParameter("?pollutionindex",airInfo.PollutionIndex),
                            new MySqlParameter("?fl",airInfo.FL),
                            new MySqlParameter("?fs",airInfo.FS),
                            new MySqlParameter("?pressure",airInfo.Pressure),
                            new MySqlParameter("?nowweathertext",airInfo.NowWeatherText),
                            new MySqlParameter("?cityname",airInfo.CityName),
                            new MySqlParameter("?temperature",airInfo.Temp),
                            new MySqlParameter("?shidu",airInfo.Hum),
                            new MySqlParameter("?week",airInfo.Week)
                        };
                        cmd.Parameters.AddRange(param);
                        int resultNum = 0;
                        try
                        {
                            resultNum = cmd.ExecuteNonQuery();
                        }
                        catch (Exception exp)
                        {
                            LogInfoHelper.WriteRunTimeMessage("插入城市数据信息异常"+exp.Message+"\r\n"+exp.StackTrace,RunTimeMessageType.Exception,true);
                        }
                       
                        if (resultNum == 1)
                        {
                            cmd.CommandText = string.Format("update citylistinfo set LastFreashTime='{1}',AssignMachineMAC='' where CityName='{0}'", cityName, airInfo.UpdateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                            if (cmd.ExecuteNonQuery() != 1)
                            {
                                LogInfoHelper.WriteRunTimeMessage("更新LastFreashTime信息异常", RunTimeMessageType.Exception, true);
                            }
                            resultList.Add(cityName);
                        }
                    }
                    else
                    {
                        LogInfoHelper.WriteRunTimeMessage(string.Format("client :{0}提交上的城市列表信息中有未包含的城市信息  城市名称为{1}", clientMac, cityName), RunTimeMessageType.Exception, true);
                        continue;
                    }
                }
            }
            else
            {
                LogInfoHelper.WriteRunTimeMessage(string.Format("client {0} 被分配的城市列表全部被覆盖~_~!!!", clientMac), RunTimeMessageType.Log, true);
            }
            UnLockTable();
            DisconnectDatabase();
            return resultList.ToArray();
        }

        /// <summary>
        /// 根据客户端的clientMac 获取分配给该MAC的城市未被刷新的list
        /// </summary>
        /// <param name="clientMac">要获取列表的客户端的Mac地址</param>
        /// <returns></returns>
        private string[] GetNoFreashCityListByClientMac(string clientMac)
        {
            List<string> resultList = new List<string>();
            MySqlCommand cmd = new MySqlCommand("select CityName from citylistinfo where AssignMachineMAC =?Mac", conn);
            cmd.Parameters.Add(new MySqlParameter("?Mac", clientMac));
            MySqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                resultList.Add(reader["CityName"].ToString());
            }
            reader.Close();
            return resultList.ToArray();
        }
        #region  加解锁和释放数据库连接部分
        /// <summary>
        /// 将数据库中的指定表的集合加上读写锁
        /// </summary>
        /// <param name="tableName">要加锁的表的名字的集合</param>
        /// <param name="lockType">要对表加的锁的类型</param>
        private void LockTable(IEnumerable<string> tableNames, SqlLockType lockType)
        {
            string sql = "lock table ";
            string lockTypeStr = lockType == SqlLockType.write ? "write" : "read";
            foreach (string tableName in tableNames)
            {
                sql += string.Format(" {0} {1},", tableName, lockTypeStr);
            }
            sql = sql.TrimEnd(',');
            sql += ";";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }
        /// <summary>
        /// 释放当前连接 锁住的所有表
        /// </summary>
        private void UnLockTable()
        {
            string sql = "unlock tables;";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }
        /// <summary>
        /// 断开数据库连接(页面事务处理完成后最好调用一次改方法确保数据库连接已关闭)
        /// </summary>
        public void DisconnectDatabase()
        {
            if (conn.State == ConnectionState.Open)
            {
                conn.Close();
            }
        }
        //析构(加一个析构 保险 ~_~!!)
        ~DatabaseAccess()
        {
            Dispose();
        }
        #endregion

        public void Dispose()
        {
            if (conn.State == ConnectionState.Open)
            {
                UnLockTable();
                DisconnectDatabase();
            }
        }
    }

    public enum SqlLockType
    {
        write,
        read
    }
}