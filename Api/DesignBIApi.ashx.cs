/* ==============================================================================
 * 功能描述：BI设计器接口
 * 创 建 者：赵云鹏
 * 创建日期：2020-12-31  
 * 修改人：赵云鹏
 * 修改日期：2020-12-31 
 * 修改备注：
 * 版本：1.0.0.0
* ==============================================================================*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using Lskj.Web.Core.Api;
using Lskj.Web.Core.Attributes;
//using Lskj.WebErp.Core.BLL.Impl;
using Lskj.WebErp.Core.BLL.Interface;
using Lskj.Web.Core.Util;
using Lskj.Web.Core.Log;
using Lskj.Data.ADO.Abstract;

using DesignBI.web;
using DesignBI.Impl;
using DesignBI.Impl.Entities;

namespace DesignBI.Api
{
    /// <summary>
    /// DesignBIApi 的摘要说明
    /// </summary>
    public class DesignBIApi : BaseHandler
    {
        private DesignBIImpl designImpl;
        public DesignBIApi() : base()
        {
            designImpl = new DesignBIImpl(DesignBI_Tabs, TableRanks);
        }
        protected LoginUser nowUser = null;

        protected override void InitSystemParams(HttpContext ctx)
        {
            try
            {
                ctx.Response.ContentType = "text/plain";//"text/json"


                this.Request = ctx.Request;
                this.Response = ctx.Response;

                language = bImpl.Request("lg") ?? "cn";

                nowUser = designImpl.GetUserInfoSession();

                //每次访问均赋值，当session过时，就都变为空字符串了
                if (nowUser != null)
                {
                    this.UserId = nowUser.userId;
                    this.UserName = nowUser.userName;
                }
                else
                {
                    //【=2=】未登录，但是提交了 用户名和 名称 暂不处理

                    //this.UserId = bImpl.Request("userid");
                    //this.UserName = bImpl.Request("username");
                    //if (!String.IsNullOrEmpty(UserId))
                    //{
                    //    nowUser = new LoginUser() { userId = UserId, userName = UserName };
                    //    ctx.Session[designImpl.userStr] = nowUser;
                    //}
                }
            }
            catch (Exception e)
            {
                this.Error("inierror", e);
            }
        }



        #region 初始与刷新 核心表数据
        public Dictionary<string, string> DesignBI_Tabs = new Dictionary<string, string>()
        {
            {"board", "P_designBI_BoardTab"},
            {"item", "P_designBI_InstanceTab"},
            {"data", "P_designBI_DataTab" },
            {"sqlSource", "P_designBI_SqlSource" },
            {"userInGroup", "P_designBI_UserInGroup" },
            {"message", "P_designBI_MessageTab" },
            {"menu", "P_designBI_MenuTab" },
            {"group", "P_designBI_UserGroup" }
        };
        public Dictionary<string, Hashtable> TableRanks = new Dictionary<string, Hashtable>()
        {
            {"board", new Hashtable() {
                {"AddOrUpd", "10" }, //已补充
                {"Delete", "10" }, //已补充
                {"select", "30" }, // 暂未
            } },

            {"item", new Hashtable() {
                {"AddOrUpd", "20" }, //已补充
                {"Delete", "20" }, //已补充
                {"select", "30" }, // 暂未
            } },

            {"menu", new Hashtable() {
                {"AddOrUpd", "10" },
                {"Delete", "10" }, //已补充
                {"select", "30" }, // 暂未
            } },

            {"data", new Hashtable() {
                {"AddOrUpd", "10" },
                {"Delete", "10" }, //已补充
                {"select", "30" }, // 暂未
            } },
        };

        [RequestCheck(CheckParams = "records,table", CheckLogin = true)]
        public void AddOrUpdTable()
        {
            string recsStr = bImpl.Request("records");
            string isAddStr = bImpl.Request("isAdd");
            string groupId = bImpl.Request("groupId") + "";
            bool? isAdd = null;
            if (!string.IsNullOrEmpty(isAddStr))
            {
                isAdd = isAddStr == "1" ? true : false;
            }
            string idField = bImpl.Request("idField");
            string table = bImpl.Request("table") + "";

            if (DesignBI_Tabs.Keys.Contains(table))
            {
                response = designImpl.CheckTableRank(groupId, table, "AddOrUpd", "新增或更新");
                if (!response.success)
                {
                    return;
                }

                response = designImpl.AddOrUpdTable(recsStr, DesignBI_Tabs[table], idField, isAdd);
            }
            else
            {
                response.success = false;
                response.msg = "错误的table参数";
            }
        }


        /// <summary>
        /// 树表的添加通用
        /// </summary>
        [RequestCheck(CheckParams = "records,table", CheckLogin = true)]
        public void AddNewTreeItem()
        {
            string indexStr = bImpl.Request("indexStr");
            string parentStr = bImpl.Request("parentStr");
            string folderStr = bImpl.Request("folderStr");
            string groupId = bImpl.Request("groupId") + "";

            if (string.IsNullOrEmpty(indexStr))
            {
                indexStr = "index";
            }
            if (string.IsNullOrEmpty(parentStr))
            {
                parentStr = "pIndex";
            }
            if (string.IsNullOrEmpty(folderStr))
            {
                folderStr = "isFolder";
            }

            string recsStr = bImpl.Request("records");
            string table = bImpl.Request("table") + "";

            if (DesignBI_Tabs.Keys.Contains(table))
            {
                response = designImpl.CheckTableRank(groupId, table, "AddOrUpd", "新增");
                if (!response.success)
                {
                    return;
                }

                response = designImpl.AddNewTreeItem(recsStr, DesignBI_Tabs[table], indexStr, parentStr, folderStr);
            }
            else
            {
                response.success = false;
                response.msg = "错误的table参数";
            }
        }

        [RequestCheck(CheckParams = "index,table", CheckLogin = true)]
        public void DeleteTreeItem()
        {
            string index = bImpl.Request("index");
            string table = bImpl.Request("table");
            string indexStr = bImpl.Request("indexStr");
            string groupId = bImpl.Request("groupId") + "";

            if (string.IsNullOrEmpty(indexStr))
            {
                indexStr = "index";
            }

            if (DesignBI_Tabs.Keys.Contains(table))
            {
                response = designImpl.CheckTableRank(groupId, table, "Delete", "删除");
                if (!response.success)
                {
                    return;
                }

                //++ 1 对本地数据的 数据库表删除 drop
                if (response.success && table == "data")
                {
                    string dataType = bImpl.Request("dataType");
                    if (dataType == "local")
                    {
                        string findSql = string.Format(@"select distinct tableName from [{0}] where [{1}] like '{2}-%' or [{1}]='{2}' and tableName<>''", DesignBI_Tabs[table], indexStr, index);
                        DataTable dataAndSubRecs = dbOperator.ExecuteDataTable(findSql);

                        foreach(DataRow oneRow in dataAndSubRecs.Rows)
                        {
                            string tableName = oneRow["tableName"] + "";

                            string delSql = string.Format(@" if exists (select 1 from dbo.sysobjects where id = object_id(N'[dbo].[{0}]') and OBJECTPROPERTY(id, N'IsUserTable') = 1)

                              begin

                              drop table [{0}]

                              end", tableName);
                            dbOperator.ExecuteDataTable(delSql);
                        }
                    }
                }

                response = designImpl.DeleteTreeItem(index, DesignBI_Tabs[table], indexStr);
            }
            else
            {
                response.success = false;
                response.msg = "错误的table参数";
            }
        }


        [RequestCheck(CheckParams = "ids,table", CheckLogin = true)]
        public void DeleteRecords()
        {
            string ids = bImpl.Request("ids");//要求是数组
            string idField = bImpl.Request("idField");
            string table = bImpl.Request("table") + "";
            string groupId = bImpl.Request("groupId") + "";

            ArrayList theIds = (ArrayList)JSON.Decode(ids);
            if (theIds.Count == 0)
            {
                response.success = false;
                response.msg = "未传递要删除数据的ID";
                return;
            }

            if (DesignBI_Tabs.Keys.Contains(table))
            {
                response = designImpl.CheckTableRank(groupId, table, "Delete", "删除");
                if (!response.success)
                {
                    return;
                }

                response.data = designImpl.DeleteRecords(DesignBI_Tabs[table], theIds, idField);
                response.success = true;
            }
            else
            {
                response.success = false;
                response.msg = "错误的table参数";
            }
        }

        [RequestCheck(CheckParams = "templateCode,groupId,index", CheckLogin = true)]
        public void DeleteBoard()
        {
            string code = bImpl.Request("templateCode");
            string groupId = bImpl.Request("groupId");
            string index = bImpl.Request("index");
            ArrayList codes = new ArrayList() { code };

            string rank = Session["g" + groupId] + "";
            if (string.IsNullOrEmpty(rank))
            {
                response.success = false;
                response.msg = "未检测到团队ID，请在团队中心中进行删除操作！";
                return;
            }
            if (rank.ToInt32() > 10)
            {
                response.success = false;
                response.msg = "非管理员权限不可删除绘板！";
                return;
            }

            Hashtable whereCond = new Hashtable()
            {
                {"ownerGroup", groupId }
            };

            response.other = designImpl.DeleteRecords(DesignBI_Tabs["item"], codes, "templateCode", whereCond);
            response.data = designImpl.DeleteRecords(DesignBI_Tabs["board"], codes, "templateCode", whereCond);
            //++ 2 补充树结构删除
            designImpl.DeleteTreeItem(index, DesignBI_Tabs["board"]);

            response.success = true;
            response.msg = "测试DeleteBoard ok";
        }

        [RequestCheck(CheckLogin = false)]
        public void GetBoardList()
        {
            string templateCode = bImpl.Request("templateCode");
            string index = bImpl.Request("index");

            response.data = designImpl.GetBoardList(templateCode, index);
            response.success = true;
            response.msg = "测试GetBoardList ok";
        }
        [RequestCheck(CheckLogin = true)]
        public void GetBoardListFolderMode()
        {
            string pIndex = bImpl.Request("pIndex");
            Int64 groupId = bImpl.Request("groupId").ToInt64();

            response.data = designImpl.GetBoardListFolderMode(pIndex, groupId);
            response.success = true;
            response.msg = "测试 GetBoardListFolderMode ok";
        }
        [RequestCheck(CheckParams = "templateCode", CheckLogin = false)]
        public void GetInstanceList()
        {
            string code = bImpl.Request("templateCode");
            response.data = designImpl.GetInstanceList(code);
            response.success = true;
            response.msg = "测试GetInstanceList ok";
        }

        [RequestCheck(CheckLogin = true, CheckParams = "groupId")]
        public void GetDataMenus()
        {
            string pIndex = bImpl.Request("index");
            string groupId = bImpl.Request("groupId");
            response.data = designImpl.GetDataMenus(pIndex, groupId);
            response.success = true;
            response.msg = "测试GetDataMenus ok";
        }

        [RequestCheck(CheckParams = "id", CheckLogin = false)]
        public void GetLinkData()
        {
            string id = bImpl.Request("id");

            response.data = designImpl.GetLinkData(id);
            response.success = true;
            response.msg = "测试 GetLinkData ok";
        }

        #region 弃用-精确选参
        //GetDataDetail 中 的内容

        /*
            //【=1=】针对存储过程和 参数SQL，共同组成 子查询 放入 tableName中
            if (dataSubType == "procedure" || dataSubType == "custom")
            {
                DataRow theData = designImpl.GetLinkData(id).Rows[0];
                string plStr = theData["paramList"] + "";

                if (string.IsNullOrEmpty(plStr))
                {
                    response.msg = "该控件所选数据参数列表信息丢失，请检查数据情况，数据读取失败！";
                    return;
                }


                ArrayList pl = (ArrayList)JSON.Decode(plStr);

                if (string.IsNullOrEmpty(paramValueList))
                {
                    response.msg = "带参数的数据类型，暂未给出参数列表，数据读取失败";
                    return;
                }

                ArrayList paramValueListA = (ArrayList)JSON.Decode(paramValueList);

                if (dataSubType == "procedure")
                {
                    List<string> paraL = new List<string>();
                    List<string> emptyL = new List<string>();

                    foreach (Hashtable oneParam in pl)
                    {
                        string mStr = oneParam["matchStr"] + "";
                        Hashtable paramVal = null;
                        foreach (Hashtable pv in paramValueListA)
                        {
                            if (pv["matchStr"] + "" == mStr)
                            {
                                paramVal = pv;
                                break;
                            }
                        }

                        if (paramVal == null)
                        {
                            response.msg = "存储过程参数匹配失败：" + mStr;
                            return;
                        }

                        paraL.Add(string.Format(" '{0}' ", paramVal["value"]));
                        emptyL.Add("''");
                    }

                    string cSql = string.Format(" exec [{0}] {1} ", tableName, paraL.SJoin(","));
                    string emptySql = string.Format(" exec [{0}] {1} ", tableName, emptyL.SJoin(","));

                    DataTable emptyDT = linkOP.ExecuteDataTable(emptySql);

                    string tempTableName;
                    string createSql = designImpl.GetBIProcedureTempTableSql_DT(emptyDT, tableName, out tempTableName);

                    preSql = string.Format(" {0}  insert {1} {2} ", createSql, tempTableName, cSql);
                    tableName = tempTableName;
                }
                else if (dataSubType == "custom")
                {
                    foreach (Hashtable oneParam in pl)
                    {
                        string mStr = oneParam["matchKey"] + "";
                        Hashtable paramVal = null;
                        foreach (Hashtable pv in paramValueListA)
                        {
                            if (pv["matchStr"] + "" == mStr)
                            {
                                paramVal = pv;
                                break;
                            }
                        }

                        if (paramVal == null)
                        {
                            response.msg = "自定义SQL参数匹配失败：" + mStr;
                            return;
                        }
                        else
                        {
                            paramVal["matchKey"] = paramVal["matchStr"];
                        }
                    }
                    string cusSql = theData["dataSource"] + "";

                    try
                    {
                        string biCusSql = designImpl.GetBICusSql(cusSql, paramValueListA);

                        tableName = string.Format(" ({0}) ", biCusSql);
                    }
                    catch (Exception ex)
                    {
                        response.msg = ex.Message;
                        return;
                    }

                }
            }
        */

        #endregion

        [RequestCheck(CheckParams = "id", CheckLogin = false)]
        public void GetDataDetail()
        {
            string id = bImpl.Request("id");
            string tableName = bImpl.Request("tableName");
            string conditions = bImpl.Request("conditions");
            string Dims = bImpl.Request("Dims");
            string Indices = bImpl.Request("Indices");
            string JoinTables = bImpl.Request("JoinTables");
            string type = bImpl.Request("type");

            string groupId = bImpl.Request("groupId");
            string serverName = bImpl.Request("serverName");
            string dbName = bImpl.Request("dbName");

            string dataSubType = bImpl.Request("dataSubType");
            string paramValueList = bImpl.Request("paramValueList");

            AdsOperater linkOP = dbOperator;
            string preSql = "";

            if (!string.IsNullOrEmpty(serverName))
            {
                linkOP = designImpl.GetLinkSourceDBOP(groupId, serverName, dbName);
            }

            //【=1=】针对存储过程和 参数SQL，共同组成 子查询 放入 tableName中
            if (dataSubType == "procedure" || dataSubType == "custom")
            {
                DataRow theData = designImpl.GetLinkData(id).Rows[0];
                string plStr = theData["paramList"] + "";

                if (string.IsNullOrEmpty(plStr))
                {
                    response.msg = "该控件所选数据参数列表信息丢失，请检查数据情况，数据读取失败！";
                    return;
                }

                ArrayList pl = (ArrayList)JSON.Decode(plStr);

                if (string.IsNullOrEmpty(paramValueList))
                {
                    response.msg = "带参数的数据类型，暂未给出参数列表，数据读取失败";
                    return;
                }

                Hashtable paramValueListH = (Hashtable)JSON.Decode(paramValueList);

                if (dataSubType == "procedure")
                {
                    List<string> paraL = new List<string>();
                    List<string> emptyL = new List<string>();

                    foreach (Hashtable oneParam in pl)
                    {
                        string mStr = oneParam["matchStr"] + "";
                        string mKey = mStr.StartsWith("@") ? mStr.Substring(1) : mStr;

                        string findVal = paramValueListH.ContainsKey(mKey) ? paramValueListH[mKey] + "" : "";

                        paraL.Add(string.Format(" '{0}' ", findVal));
                        emptyL.Add("''");
                    }

                    string cSql = string.Format(" exec [{0}] {1} ", tableName, paraL.SJoin(","));
                    string emptySql = string.Format(" exec [{0}] {1} ", tableName, emptyL.SJoin(","));

                    DataTable emptyDT = linkOP.ExecuteDataTable(emptySql);

                    string tempTableName;
                    string createSql = designImpl.GetBIProcedureTempTableSql_DT(emptyDT, tableName, out tempTableName);

                    preSql = string.Format(" {0}  insert {1} {2} ", createSql, tempTableName, cSql);
                    tableName = tempTableName;
                }
                else if (dataSubType == "custom")
                {
                    ArrayList valueLS = new ArrayList();

                    foreach (Hashtable oneParam in pl)
                    {
                        string mKey = oneParam["matchKey"] + "";
                        string findVal = paramValueListH.ContainsKey(mKey) ? paramValueListH[mKey] + "" : "";

                        Hashtable paramVal = new Hashtable()
                        {
                            {"matchKey", mKey },
                            {"value", findVal }
                        };
                        valueLS.Add(paramVal);
                    }
                    string cusSql = theData["dataSource"] + "";

                    try
                    {
                        string biCusSql = designImpl.GetBICusSql(cusSql, valueLS);

                        tableName = string.Format(" ({0}) ", biCusSql);
                    }
                    catch (Exception ex)
                    {
                        response.msg = ex.Message;
                        return;
                    }

                }
            }



            if (type == "treeTable")
            {
                response.data = designImpl.GetDataDetail_Tree(linkOP, tableName, id, conditions, Dims, Indices, JoinTables, preSql);
            }
            else if (type == "chart")
            {
                response.data = designImpl.GetDataDetail(linkOP, tableName, id, conditions, Dims, Indices, JoinTables, "", preSql);
            }
            else if (type == "detail")
            {
                try
                {
                    response.data = designImpl.GetDataDetail(linkOP, tableName, id, conditions, Dims, Indices, JoinTables, type, preSql);
                }
                catch (Exception ex)
                {
                    response.msg = ex.InnerException.Message + " ; " + ex.Message;
                    return;
                }
            }
            response.success = true;
            response.msg = "测试GetDataDetail ok";
        }
        [RequestCheck(CheckParams = "id,groupId", CheckLogin = false)]
        public void GetLinkDetailData()
        {
            string id = bImpl.Request("id");
            string groupId = bImpl.Request("groupId");

            DataTable linkDT = designImpl.GetLinkData(id);
            if (linkDT.Rows.Count > 0)
            {
                Hashtable rowHS = linkDT.Rows[0].ToHashTable(false, false);
                string dtype = rowHS["dataType"] + "";
                string tName = rowHS["tableName"] + "";
                string dbName = rowHS["dataBaseName"] + "";
                string sName = rowHS["sourceName"] + "";
                string dataSubType = rowHS["dataSubType"] + "";
                if (dataSubType != "procedure" && dataSubType != "custom")
                {
                    tName = string.Format("[{0}]", tName);
                    DataTable sourceDT = designImpl.GetDataDetailSimple(sName, dbName, groupId, tName, id);
                    rowHS.Add("dataTable", sourceDT);
                }
                response.data = rowHS;
            }
            response.success = true;
            response.msg = "测试 GetLinkData ok";
        }

        [RequestCheck(CheckLogin = false)]
        public void GetMenuItems()
        {
            string index = bImpl.Request("index");
            string linkCode = bImpl.Request("linkCode");
            string groupId = bImpl.Request("groupId");
            response.data = designImpl.GetMenuItems(index, linkCode, groupId);
            response.success = true;
            response.msg = "测试 GetMenuItems ok";
        }

        [RequestCheck(CheckParams = "dimensions,itype", CheckLogin = false)]
        public void GetDimensionsInfo()
        {
            string dimensionsStr = bImpl.Request("dimensions");
            string itype = bImpl.Request("itype");
            ArrayList dimensions = (ArrayList)JSON.Decode(dimensionsStr);

            response.data = designImpl.GetDimensionsInfo(dimensions, itype);
            response.success = true;
            response.msg = "测试 GetDimensionsInfo ok";
        }

        #endregion

        #region 本地表数据上传添加
        [RequestCheck(CheckParams = "DetailData,keySheet", CheckLogin = false)]
        public void CreateOrUpdTable()
        {
            string DetailDataStr = bImpl.Request("DetailData");
            string idField = bImpl.Request("idField");
            if (string.IsNullOrEmpty(idField))
            {
                idField = "id";
            }

            Hashtable DetailData = (Hashtable)JSON.Decode(DetailDataStr);
            string theId = DetailData[idField] + "";
            string tableName = DetailData["tableName"] + "";
            if (string.IsNullOrEmpty(tableName))
            {
                response.success = false;
                response.msg = "未传递tableName参数！";
                return;
            }

            //均为 array json数据
            string keySheetStr = bImpl.Request("keySheet");
            ArrayList dimension = (ArrayList)DetailData["dimension"];//【update】看是否跟着json解析
            if (dimension.Count > 0)
            {
                //ArrayList dimension = (ArrayList)JSON.Decode(dimensionStr);//这里必须传一个数组
                //# 1 是否存在表，不存在则创建
                designImpl.CheckAndCreateLocalTable(tableName, dimension);
            }
            //ArrayList keySheet = (ArrayList)JSON.Decode(keySheetStr);//这里必须传一个数组

            //# 2 更新【#1】表数据
            designImpl.AddOrUpdTable(keySheetStr, tableName);

            //# 3[暂无] 更新维度表数据【0107】暂未发现必要性

            //# 4 更新 数据条【main】的信息，用id来管理【#1】表
            if (string.IsNullOrEmpty(theId))
            {
                designImpl.AddNewTreeItem(new ArrayList() { DetailData }, DesignBI_Tabs["data"]);
            }
            else
            {
                designImpl.AddOrUpdTable(new ArrayList() { DetailData }, DesignBI_Tabs["data"]);
            }

            response.success = true;
            response.msg = "上传成功";
        }
        [RequestCheck(CheckParams = "record,keySheet,dataId,ifReUpload", CheckLogin = false)]
        public void UpdateLocalTable()
        {
            string recordStr = bImpl.Request("record");
            string keySheetStr = bImpl.Request("keySheet");
            string dataId = bImpl.Request("dataId");
            string ifReUploadStr = bImpl.Request("ifReUpload");

            string newDimsStr = bImpl.Request("newDims");

            bool ifReUpload = ifReUploadStr.ToBoolean();

            response = designImpl.UpdateLocalTable(recordStr, keySheetStr, dataId, ifReUpload, newDimsStr);

        }


            #endregion


            #region 数据库查询方式
            [RequestCheck(CheckLogin = false, CheckParams = "groupId")]
        public void GetSqlSourceList()
        {
            string groupId = bImpl.Request("groupId");
            DataTable SqlSourceDT = designImpl.GetSqlSourceList(groupId);
            response.success = true;
            response.data = SqlSourceDT;
            response.msg = "测试 GetSqlSourceList ok";
        }
        [RequestCheck(CheckParams = "name,ip,username,password,groupId", CheckLogin = false)]
        public void AddSqlSource()
        {
            string name = bImpl.Request("name");
            string ip = bImpl.Request("ip");
            string username = bImpl.Request("username");
            string password = bImpl.Request("password");
            string groupId = bImpl.Request("groupId");

            response = designImpl.AddSqlSource(name, ip, username, password, groupId);
            //response.success = true;
            //response.msg = "测试 GetSqlSourceList ok";
        }
        [RequestCheck(CheckParams = "name,groupId", CheckLogin = false)]
        public void DeleteSqlSource()
        {
            string name = bImpl.Request("name");
            string groupId = bImpl.Request("groupId");

            response.data = designImpl.DeleteSqlSource(name, groupId);
            response.success = true;
            response.msg = "测试 DeleteSqlSource ok";
        }
        [RequestCheck(CheckParams = "serverName,groupId", CheckLogin = false)]
        public void GetLinkSourceDBList()
        {
            string serverName = bImpl.Request("serverName");
            string query = bImpl.Request("query");
            string groupId = bImpl.Request("groupId");

            response.data = designImpl.GetLinkSourceDBList(serverName, query, groupId);
            response.success = true;
            response.msg = "测试 GetLinkSourceDBList ok";
        }
        [RequestCheck(CheckParams = "serverName,dbName,groupId", CheckLogin = false)]
        public void GetLinkSourceDBTableList()
        {
            string serverName = bImpl.Request("serverName");
            string dbName = bImpl.Request("dbName");
            string query = bImpl.Request("query");
            string groupId = bImpl.Request("groupId");

            response.data = designImpl.GetLinkSourceDBTableList(serverName, dbName, query, groupId);
            response.success = true;
            response.msg = "测试 GetLinkSourceDBTableList ok";
        }
        [RequestCheck(CheckParams = "serverName,dbName,groupId", CheckLogin = false)]
        public void GetLinkSourceDBViewList()
        {
            string serverName = bImpl.Request("serverName");
            string dbName = bImpl.Request("dbName");
            string query = bImpl.Request("query");
            string groupId = bImpl.Request("groupId");

            response.data = designImpl.GetLinkSourceDBViewList(serverName, dbName, query, groupId);
            response.success = true;
            response.msg = "测试 GetLinkSourceDBViewList ok";
        }
        [RequestCheck(CheckParams = "serverName,dbName,groupId", CheckLogin = false)]
        public void GetLinkSourceDBProcedureList()
        {
            string serverName = bImpl.Request("serverName");
            string dbName = bImpl.Request("dbName");
            string query = bImpl.Request("query");
            string groupId = bImpl.Request("groupId");

            response.data = designImpl.GetLinkSourceDBProcedureList(serverName, dbName, query, groupId);
            response.success = true;
            response.msg = "测试 GetLinkSourceDBProcedureList ok";
        }
        [RequestCheck(CheckParams = "serverName,dbName,groupId", CheckLogin = false)]
        public void GetLinkSourceDBTableDimList()
        {
            string serverName = bImpl.Request("serverName");
            string dbName = bImpl.Request("dbName");
            string tableName = bImpl.Request("tableName");
            string dataSubType = bImpl.Request("dataSubType");
            string groupId = bImpl.Request("groupId");

            string moreParamsStr = bImpl.Request("moreParams");
            Hashtable moreParams = null;
            if (!string.IsNullOrEmpty(moreParamsStr))
            {
                moreParams = (Hashtable)JSON.Decode(moreParamsStr);
            }

            response = designImpl.GetLinkSourceDBTableDimList(serverName, dbName, tableName, dataSubType, groupId, moreParams);
        }
        [RequestCheck(CheckParams = "serverName,dbName,tableName,dimension,groupId", CheckLogin = false)]
        public void GetLinkSourceDBTableData()
        {
            string serverName = bImpl.Request("serverName");
            string dbName = bImpl.Request("dbName");
            string tableName = bImpl.Request("tableName");
            string dimension = bImpl.Request("dimension");
            string groupId = bImpl.Request("groupId");

            ArrayList dimensionA = (ArrayList)JSON.Decode(dimension);
            if (dimensionA.Count == 0)
            {
                response.success = false;
                response.msg = "维度数量不可为0";
                return;
            }

            response.data = designImpl.GetLinkSourceDBTableData(serverName, dbName, tableName, dimensionA, groupId);
            response.success = true;
            response.msg = "测试 GetLinkSourceDBTableData ok";
        }

        #endregion

        #region 登录

        /// <summary>
        /// 通过stateServer来实现的session会话登录检测模式
        /// </summary>
        [RequestCheck(CheckLogin = false)]
        public override void CheckLogin()
        {
            response.success = (nowUser != null);
            response.data = nowUser;
            response.other = 1;
        }

        /// <summary>
        /// 登录
        /// </summary>
        [RequestCheck(CheckLogin = false, CheckParams = "userCode|username")]
        public void Login()
        {
            string userCode = bImpl.Request("userCode", bImpl.Request("username"));
            string password = bImpl.Request("password");
            response = designImpl.Login(userCode, password);
            if (response.success)
            {
                Session[designImpl.userStr] = (LoginUser)response.data;
            }
        }
        [RequestCheck(CheckLogin = false, CheckParams = "userCode")]
        public void LoginTest()
        {
            string userCode = bImpl.Request("userCode");
            string password = bImpl.Request("password");
            response = designImpl.Login(userCode, password);
        }

        /// <summary>
        /// 登出
        /// </summary>
        [RequestCheck(CheckLogin = false)]
        public void LoginOut()
        {
            designImpl.LoginOut();
            response.success = true;
            response.msg = "已登出";
        }


        /// <summary>
        /// 注册
        /// </summary>
        [RequestCheck(CheckLogin = false, CheckParams = "userCode")]
        public void Register()
        {
            string userCode = bImpl.Request("userCode");
            string password = bImpl.Request("password");
            response = designImpl.Register(userCode, password);
        }


        #endregion

        #region 团队

        /// <summary>
        /// 新团队
        /// </summary>
        [RequestCheck(CheckLogin = true, CheckParams = "groupName")]
        public void CreateNewGroup()
        {
            string groupName = bImpl.Request("groupName");
            LoginUser nowUser = designImpl.GetUserInfoSession();
            response = designImpl.CreateNewGroup(groupName, nowUser.userCode);
        }

        [RequestCheck(CheckLogin = true, CheckParams = "groupId")]
        public void UpdateUserDefaultGroup()
        {
            string groupId = bImpl.Request("groupId");
            response = designImpl.UpdateUserDefaultGroup(nowUser.userCode, groupId.ToInt64());
        }
        [RequestCheck(CheckLogin = true)]
        public void UpdateUserMainPageCode()
        {
            string mainPageCode = bImpl.Request("mainPageCode");
            response = designImpl.UpdateUserMainPageCode(nowUser.userCode, mainPageCode);
        }




        [RequestCheck(CheckLogin = true)]
        public void GetUserGroup()
        {
            string groupId = bImpl.Request("groupId");
            string userCode = bImpl.Request("userCode");
            LoginUser nowUser = designImpl.GetUserInfoSession();
            DataTable groupDT = designImpl.GetUserGroup(string.IsNullOrEmpty(userCode) ? nowUser.userCode : userCode, groupId);

            foreach (DataRow oneRow in groupDT.Rows)
            {
                string gid = oneRow["id"] + "";
                string rank = oneRow["userRank"] + "";

                //# 2 便于每次操作的时候检测权限
                Session["g" + gid] = rank;
            }

            response.data = groupDT;
            response.success = true;
        }

        [RequestCheck(CheckLogin = true, CheckParams = "groupId")]
        public void GetGroupUserList()
        {
            string groupId = bImpl.Request("groupId");
            string userCode = bImpl.Request("userCode");
            response.data = designImpl.GetGroupUserList(groupId, userCode);
            response.success = true;
        }

        [RequestCheck(CheckLogin = true, CheckParams = "query")]
        public void GetInviteUserList()
        {
            string query = bImpl.Request("query");
            response.data = designImpl.GetInviteUserList(query);
            response.success = true;
        }
        [RequestCheck(CheckLogin = true, CheckParams = "groupId,userCode")]
        public void DeleteGroup()
        {
            string groupId = bImpl.Request("groupId");
            string userCode = bImpl.Request("userCode");
            response = designImpl.DeleteGroup(groupId, userCode);
        }
        


        #endregion

        #region 消息传递

        [RequestCheck(CheckLogin = true, CheckParams = "type")]
        public void SendMessage()
        {
            response.success = false;
            string type = bImpl.Request("type");
            //【=1=】检测类型中是否有
            MessageMeta findMeta;
            if (!MessageMeta.messageMetas.TryGetValue(type, out findMeta))
            {
                response.msg = "错误的消息类型！";
                return;
            }

            //【=2=】看 目标 user 和 目标 Group，两者只能有一个是有的。
            string targetUsers = bImpl.Request("targetUsers");
            string targetGroups = bImpl.Request("targetGroups");
            bool hasTU = !string.IsNullOrEmpty(targetUsers);
            bool hasTG = !string.IsNullOrEmpty(targetGroups);
            if (hasTU && hasTG)
            {
                response.msg = "目标用户和目标团队一次只能选一个发送，不能同时赋值！";
                return;
            }
            else if (!hasTU && !hasTG)
            {
                response.msg = "未传递消息传递的目标用户或团队！";
            }

            //【=3=】首先是基本的其他信息填充
            string message = bImpl.Request("message");
            bool needReply = bImpl.Request("needReply").ToBoolean(); //检查
            string fromUser = bImpl.Request("fromUser");
            string fromGroup = bImpl.Request("fromGroup");
            DateTime sendTime = DateTime.Now;
            string sendParams = bImpl.Request("sendParams");
            bool endFlag = false;

            //【=4=】再者是组合成 复数的 消息
            ArrayList waitSendMsgs = new ArrayList();
            if (hasTU)
            {
                ArrayList targetUsersA = (ArrayList)JSON.Decode(targetUsers);
                foreach (string userCode in targetUsersA)
                {
                    waitSendMsgs.Add(new Hashtable()
                    {
                        { "type", type },
                        { "typeCode", findMeta.typeCode },
                        { "tag", JSON.Encode(findMeta.tags) },
                        { "desp", findMeta.desp },

                        { "message", message },
                        { "needReply", needReply },
                        { "fromUser", fromUser },
                        { "fromGroup", fromGroup },

                        { "targetUser", userCode },
                        { "targetGroup", "" },

                        { "sendTime", sendTime },
                        { "sendParams", sendParams },
                        { "endFlag", endFlag },
                    });
                }
            }
            else if (hasTG)
            {
                ArrayList targetGroupsA = (ArrayList)JSON.Decode(targetGroups);
                foreach (object _groupId in targetGroupsA)
                {
                    string groupId = _groupId + "";
                    waitSendMsgs.Add(new Hashtable()
                    {
                        { "type", type },
                        { "typeCode", findMeta.typeCode },
                        { "tag", JSON.Encode(findMeta.tags) },
                        { "desp", findMeta.desp },

                        { "message", message },
                        { "needReply", needReply },
                        { "fromUser", fromUser },
                        { "fromGroup", fromGroup },

                        { "targetUser", "" },
                        { "targetGroup", groupId },

                        { "sendTime", sendTime },
                        { "sendParams", sendParams },
                        { "endFlag", endFlag },
                    });
                }
            }

            //【=5=】然后进行数据添加！
            response = designImpl.AddOrUpdTable(waitSendMsgs, "P_designBI_MessageTab");
        }


        [RequestCheck(CheckLogin = true, CheckParams = "userCode")]
        public void GetMessages()
        {
            long groupId = bImpl.Request("groupId").ToInt64();
            string userCode = bImpl.Request("userCode");

            response = designImpl.GetMessages(userCode, groupId);
        }

        #endregion


    }
}