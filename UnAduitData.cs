using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace BD.Standard.MM.ListServicePlugInzs
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("单据反审核插件操作服务插件")]
    public class UnAduitData : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(Kingdee.BOS.Core.DynamicForm.PlugIn.Args.PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            List<Field> file = this.BusinessInfo.GetFieldList();
            foreach (Field item in file)
            {
                e.FieldKeys.Add(item.Key);
            }
        }
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            try
            {
                IOperationResult operationResult = new OperationResult();
                foreach (DynamicObject entity in e.DataEntitys)
                {
                    //获取当前表单fid，单据编号、单据标识
                    string fid = entity[0].ToString();
                    string fbillno = entity["billno"].ToString();
                    string fromid = entity["FFormId"].ToString();
                    //获取传输状态，成功状态才会调用取消同步
                    //组装拆卸/直接调拨
                    if (StringUtils.EqualsIgnoreCase(fromid, "STK_AssembledApp") || StringUtils.EqualsIgnoreCase(fromid, "STK_TransferDirect"))
                    {
                        string FAffairType = "";
                        if (entity.TryGetValue("AffairType", out object value))
                        {
                            FAffairType = value.ToString();
                        }
                        operationResult = MWUTILS.MWData(this.Context, operationResult, "UnAudit", fromid + FAffairType + "R", fbillno, fid, "");
                        if (operationResult == null) { operationResult = new OperationResult(); }

                        operationResult = MWUTILS.MWData(this.Context, operationResult, "UnAudit", fromid + FAffairType + "C", fbillno, fid, "");
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
                        operationResult = MWUTILS.MWData(this.Context, operationResult, "UnAudit", fromid, fbillno, fid, "");
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
