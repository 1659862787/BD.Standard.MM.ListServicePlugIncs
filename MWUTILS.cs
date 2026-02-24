using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; 
using System;
using System.Collections;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace BD.Standard.MM.ListServicePlugInzs
{
    /// <summary>
    /// 工具类
    /// </summary>
    public class MWUTILS
    {
        /// <summary>
        /// 封装正向json数据
        /// </summary>
        /// <param name="datas">表数据 表1:配置信息，表二:基本信息，表三:明细信息</param>
        /// <returns>多张单据json组成的数组</returns>
        public static string WMSJson(DataSet datas,int i,string types)
        {

            JObject data = new JObject();
            JObject header = new JObject();
            JArray jsonHeader = new JArray();

            JObject jobH = null;
            if (datas.Tables[1].Rows.Count == 0) return null;
            //批量传输,表头明细的关联字段
            string id = string.Empty;
            foreach (DataRow dataH in datas.Tables[1].Rows)
            {
                jobH = new JObject();
                foreach (DataColumn columns in dataH.Table.Columns)
                {
                    string column = columns.ColumnName.ToString();
                    if (column.Equals("id"))
                    {
                        id = dataH["id"].ToString();
                        continue;
                    }
                    else jobH.Add(new JProperty(column, dataH[column].ToString()));
                    if (i==0)
                    {
                        data["item"] = jobH;
                    }
                    else
                    {
                        header[types+"Header"] = jobH;
                        data["header"]= header;
                    }

                }
                if (datas.Tables.Count == 3)
                {
                    JArray jsonEntry = new JArray();
                    
                    foreach (DataRow dataE in datas.Tables[2].Rows)
                    {
                        jobH = new JObject();
                        if (!id.Equals(dataE["id"].ToString())) continue;
                        JObject jobE = new JObject();
                        foreach (DataColumn columns in dataE.Table.Columns)
                        {
                            string column = columns.ColumnName.ToString();
                            if (column.Equals("id")) continue;
                            else if (column.Equals("shelfLife") || column.Equals("qtyOrdered"))
                            {
                                jobE.Add(new JProperty(column, Convert.ToDecimal(dataE[column].ToString())));
                            }
                            else jobE.Add(new JProperty(column, dataE[column]));
                            
                        }
                        jobH[types + "Detail"] = jobE;
                        jsonEntry.Add(jobH);
                    }
                    data["details"] = jsonEntry;
                }
                
            }
            return data.ToString();
        }

        /// <summary>
        /// 封装反向json数据
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        public static string WMSUnJson(DataSet datas)
        {
            JObject jobH = new JObject();
            if (datas.Tables[1].Rows.Count == 0) return null;
            foreach (DataRow dataH in datas.Tables[1].Rows)
            {
                foreach (DataColumn columns in dataH.Table.Columns)
                {
                    string column = columns.ColumnName.ToString();
                    jobH.Add(new JProperty(column, dataH[column].ToString()));
                }
            }
            return jobH.ToString();
        }

        private static readonly string Logpath = @"D:\ERPPostLog\" + DateTime.Now.ToString("yyyyMM");


        /// <summary>
        /// 传输方法类
        /// </summary>
        /// <param name="context">当前上下文</param>
        /// <param name="operationResult">操作结果</param>
        /// <param name="type">存储类型</param>
        /// <param name="fromid">表单标识</param>
        /// <param name="fnumber">表单编码</param>
        /// <param name="fid">表单主键</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static IOperationResult MWData(Context context, IOperationResult operationResult,string type,string fromid,string fnumber,string fid,string username)
        {
            //获取配置信息
            DataSet config = DBUtils.ExecuteDataSet(context, string.Format("exec {0}", "MingMai_config"));
            //启用插件
            string IsEnable = config.Tables[0].Rows[0].ItemArray[8].ToString();
            if (!Convert.ToBoolean(IsEnable)) return operationResult;


            string http = config.Tables[0].Rows[0].ItemArray[0].ToString();
            string format = config.Tables[0].Rows[0].ItemArray[1].ToString();
            string app_key = config.Tables[0].Rows[0].ItemArray[2].ToString();
            string v = config.Tables[0].Rows[0].ItemArray[3].ToString();
            string sign_method = config.Tables[0].Rows[0].ItemArray[4].ToString();
            string customerId = config.Tables[0].Rows[0].ItemArray[5].ToString();
            string timestamp = config.Tables[0].Rows[0].ItemArray[6].ToString();
            string secret = config.Tables[0].Rows[0].ItemArray[7].ToString();

            //根据单据编号和单据标识获取对应的取消数据
            DataSet dy=new DataSet();
            string json = string.Empty;
            string status = string.Empty;
            switch (type)
            {
                //基础资料
                case "basic":
                    dy = DBUtils.ExecuteDataSet(context, string.Format("exec {0} {1}", "MingMai_" + fromid, fid));
                    json = MWUTILS.WMSJson(dy,0,"");
                    status = "同步成功";
                    break;
                //单据
                case "bill":
                    dy = DBUtils.ExecuteDataSet(context, string.Format("exec {0} {1}", "MingMai_" + fromid, fid));
                    string types = dy.Tables[0].Rows[0].ItemArray[0].ToString().Equals("deliveryorder.create") ? "shipment" : "receipt";
                    json = MWUTILS.WMSJson(dy,1, types);
                    status = "同步成功";
                    break;
                //反审核、撤销操作
                case "UnAudit":
                    dy = DBUtils.ExecuteDataSet(context, string.Format("exec {0} '{1}','{2}'", "MingMai_UnAudit", fnumber, fromid));
                    json = MWUTILS.WMSUnJson(dy);
                    status = "取消成功";
                    break;
            }
            //dy获取表数据，表一：method：请求方法，table：基础资料表名，
            string method = dy.Tables[0].Rows[0].ItemArray[0].ToString();
            string table = dy.Tables[0].Rows[0].ItemArray[1].ToString();
            string tableid = dy.Tables[0].Rows[0].ItemArray[2].ToString();
            if (json == null) return null;

            #region 生成sign
            Hashtable ht = new Hashtable();
            ht.Add("app_key", app_key);
            ht.Add("customerId", customerId);
            ht.Add("format", format);
            ht.Add("method", method);
            ht.Add("sign_method", sign_method);
            ht.Add("timestamp", timestamp);
            ht.Add("v", v);
            string sign = CreateSign(ht,secret);
            #endregion 生成sign

            //拼接地址，同步数据
            string url1 = http + "?method=" + method + "&format=" + format + "&app_key=" + app_key + "&v=" + v + "&sign=" + sign + "&sign_method=" + sign_method + "&customerId=" + customerId ;

            Logger logger = new Logger(Logpath, DateTime.Now.ToString("yyyy-MM-dd") + ".txt");
            logger.WriteLog("url1：" + url1);
            logger.WriteLog("json：" + json);

            string ss =HttpPost(url1, json);

            ss = ss.Replace("0E-8", "0");
            JObject jo = (JObject)JsonConvert.DeserializeObject(ss);
            DateTime date = DateTime.Now;
            //同步成功，输出消息，更改单据同步状态字段，记录日志
            if (jo["code"].ToString().Equals("0"))
            {
                operationResult.OperateResult.Add(new OperateResult()
                {
                    SuccessStatus = true,
                    Name = "同步消息",
                    Message = string.Format(fnumber + ":WMS"+ status)+"\r\n"+ json,
                    MessageType = MessageType.Normal,
                    PKValue = 0,
                });
                string sql = string.Format("update {0} set F_STATUS='{3}' where {1}={2}", table, tableid, fid, status);
                
                DBUtils.Execute(context, sql);

            }
            else
            {
                throw new Exception("失败信息：" + ss + "\r\n请求json:" + json);
            }
            return operationResult;
            
        }

        /// <summary>
        /// http请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string HttpPost(string url, string data)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.Method = "POST";
            using (StreamWriter streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(data);
                streamWriter.Close();
            }
            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            string text = httpWebResponse.ContentEncoding;
            bool flag = text == null || text.Length < 1;
            if (flag)
            {
                text = "UTF-8";
            }
            StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.GetEncoding(text));
            return streamReader.ReadToEnd();
        }

        /// <summary>
        /// MD5加密
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string GetMD5_32(string s)
        {
            MD5 mD = MD5.Create();
            byte[] array = mD.ComputeHash(Encoding.GetEncoding("utf-8").GetBytes(s));
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < array.Length; i++)
            {
                stringBuilder.Append(array[i].ToString("x").PadLeft(2, '0'));
            }

            return stringBuilder.ToString().ToUpper();
        }

        /// <summary>
        /// 生成sign
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string CreateSign(Hashtable param,string secret)
        {
            ArrayList keys = new ArrayList(param.Keys);
            keys.Sort(); //按字母顺序进行排序
            string resultStr = "";
            foreach (string key in keys)
            {
                string value = param[key].ToString();
                if (key != "sign" && value != null && value != "")
                {
                    resultStr = resultStr + key + value;
                }
            }
            resultStr = secret + resultStr+ secret;       
            return GetMD5_32(resultStr);

        }



    }

}
