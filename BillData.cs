
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;

namespace BD.Standard.MM.ListServicePlugInzs
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("单据操作服务插件")]

    /*
     * 
     * 更新时间：2024年11月8日15:37:02
     * 
     * **/
    public class BillData : AbstractOperationServicePlugIn
    {
        /// <summary>
        /// 数据初始化
        /// </summary>
        /// <param name="e"></param>
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            List<Field> file = this.BusinessInfo.GetFieldList();
            foreach (Field item in file)
            {
                e.FieldKeys.Add(item.Key);
            }
        }
        /// <summary>
        /// 菜单操作方法
        /// </summary>
        /// <param name="e"></param>
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            try
            {
                string opera = this.FormOperation.Operation;
                IOperationResult operationResult = new OperationResult();
                foreach (DynamicObject entity in e.DataEntitys)
                {
                    //获取当前表单fid，单据编号、单据标识、当前操作用户
                    string fid = entity[0].ToString();
                    string fbillno = entity["billno"].ToString();
                    string fromid = entity["FFormId"].ToString();
                    string username = this.Context.UserName;
                    //组装拆卸
                    if (StringUtils.EqualsIgnoreCase(fromid, "STK_AssembledApp")|| StringUtils.EqualsIgnoreCase(fromid, "STK_TransferDirect"))
                    {
                        string FAffairType = "";
                        if(entity.TryGetValue("AffairType",out object value))
                        {
                            FAffairType=value.ToString();
                        }

                        //Assembly\Dassembly
                        operationResult = MWUTILS.MWData(this.Context, operationResult, "bill", fromid+ FAffairType+"C", fbillno, fid, username);
                        if (operationResult == null) { operationResult= new OperationResult(); }

                        operationResult = MWUTILS.MWData(this.Context, operationResult, "bill", fromid+ FAffairType+"R", fbillno, fid, username);
                        
                    }
                    else
                    {
                        if (StringUtils.EqualsIgnoreCase(fromid, "STK_INSTOCK"))
                        {
                            DynamicObjectCollection dynamics = entity["InStockFin"] as DynamicObjectCollection;
                            if (Convert.ToBoolean(dynamics[0]["ISGENFORIOS"]))
                            {
                                continue;
                            }     
                        }
                        if (StringUtils.EqualsIgnoreCase(fromid, "SAL_OUTSTOCK"))
                        {
                            DynamicObjectCollection dynamics = entity["SAL_OUTSTOCKFIN"] as DynamicObjectCollection;
                            if (Convert.ToBoolean(dynamics[0]["ISGENFORIOS"]))
                            {
                                continue;
                            }
                        }
                        if (StringUtils.EqualsIgnoreCase(fromid, "SAL_RETURNSTOCK"))
                        {
                            DynamicObjectCollection dynamics = entity["SAL_RETURNSTOCKFIN"] as DynamicObjectCollection;
                            if (Convert.ToBoolean(dynamics[0]["ISGENFORIOS"]))
                            {
                                continue;
                            }
                        }
                        if (StringUtils.EqualsIgnoreCase(fromid, "PUR_MRB"))
                        {
                            DynamicObjectCollection dynamics = entity["PUR_MRBFIN"] as DynamicObjectCollection;
                            if (Convert.ToBoolean(dynamics[0]["ISGENFORIOS"]))
                            {
                                continue;
                            }
                        }


                        


                        operationResult = MWUTILS.MWData(this.Context, operationResult, "bill", fromid, fbillno, fid, username);
                    }
 
                    if (operationResult == null) { continue; }
                    

                    
                }
                this.OperationResult.MergeResult(operationResult);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


    }
}
