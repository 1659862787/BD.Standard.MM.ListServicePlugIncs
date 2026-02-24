
using BD.Standard.MM.ListServicePlugInzs.Form;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.K3.SCM.Sal.Business.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace BD.Standard.MM.ListServicePlugIncs2
{
    [Description("模拟报价单表单插件")]
    [Kingdee.BOS.Util.HotUpdate]
    public class MoniQuotation : SimulateQuoteEdit
    {

        public override void AfterCreateNewData(EventArgs e)
        {
            base.AfterCreateNewData(e);

            //第一次下推生成子项明细资料
            if (Convert.ToBoolean(this.View.Model.GetValue("F_CheckBoxPush").ToString()))
            {
                DynamicObject FQuoteOrgId = (DynamicObject)this.View.Model.GetValue("FQuoteOrgId");
                DynamicObject FMaterialId = (DynamicObject)this.View.Model.GetValue("FMaterialId");
                DynamicObject FBomId = (DynamicObject)this.View.Model.GetValue("FBomId");
                decimal fqty = (decimal)this.View.Model.GetValue("FQTY");

                long QuoteOrgId = Convert.ToInt64(FQuoteOrgId[0].ToString());
                long BomId = Convert.ToInt64(FBomId[0].ToString());


                NewMethod("0", 0, QuoteOrgId, BomId, fqty);

                //this.View.GetMainMenu().ItemClick("tbReCalc");

                this.View.Model.SetValue("F_CheckBoxPush", false);

                DBUtils.Execute(Context, $"update t_quotationEntry set F_referencePrice='{this.View.Model.GetValue("FTaxPrice", 0)}',F_referenceAmount='{this.View.Model.GetValue("FQuoteCost", 0)}' where FEntryID='{this.View.Model.GetValue("F_srcfentry", 0)}' ");
                DBUtils.Execute(Context, $"update t_quotation set F_ModifyDate=getdate(),F_ApproveDate=getdate()  where fid='{this.View.Model.GetValue("F_srcfid", 0)}' ");

            }
        }

        private void NewMethod(string parentid, int FBOMLevel, long QuoteOrgId, long BomId, decimal fqty, int row2 = 0)
        {
            FBOMLevel += 1;

            string bomsql = $"/*dialect*/ SELECT        bom.FID AS FID,       bom.FNUMBER AS BomNO,        m2.FMATERIALID AS WLID,       m2.FNUMBER AS WLNUMBER,       m2l.FNAME AS WLNAME, bomc.FNUMERATOR,bomc.FDENOMINATOR,FErpClsID,m2b.FBASEUNITID  FROM T_ENG_BOM bom   left JOIN T_ENG_BOMCHILD bomc ON bom.FID = bomc.FID   left JOIN T_BD_MATERIAL m2 ON bomc.FMATERIALID = m2.FMATERIALID   left JOIN T_BD_MATERIAL_L m2l ON m2.FMATERIALID = m2l.FMATERIALID   left JOIN T_BD_MATERIALBASE m2b ON m2.FMATERIALID = m2b.FMATERIALID   WHERE bom.FID={BomId} and bom.FUSEORGID={QuoteOrgId}";

            DynamicObjectCollection bomdys = DBUtils.ExecuteDynamicObject(Context, bomsql);
            if (bomdys == null) return;
            decimal parentprice = 0;
            foreach (DynamicObject bomdy in bomdys)
            {
                decimal qtysum = decimal.Divide(Convert.ToDecimal(bomdy["FNUMERATOR"]), Convert.ToDecimal(bomdy["FDENOMINATOR"])) * fqty;
                this.Model.CreateNewEntryRow("FDetailEntity");
                int row = this.Model.GetEntryRowCount("FDetailEntity");
                row -= 1;
                this.View.Model.SetItemValueByNumber("FCMatlId", bomdy["WLNUMBER"].ToString(), row);
                this.View.Model.SetItemValueByID("FCSupplyOrgId", QuoteOrgId, row);
                this.View.Model.SetValue("FBOMLevel", FBOMLevel, row);
                this.View.Model.SetValue("FCIsMrpRun", true, row);
                this.View.Model.SetValue("FCQty", qtysum, row);
                this.View.InvokeFieldUpdateService("FCMatlId", row);
                this.View.InvokeFieldUpdateService("FCQty", row);
                this.View.Model.SetValue("FCSTDQty", qtysum, row);
                this.View.Model.SetValue("FCIssueType", "1", row);
                this.View.Model.SetValue("FCBaseUnitQty", qtysum, row);
                this.View.Model.SetItemValueByID("FCBaseUnitID", bomdy["FBASEUNITID"], row);



                this.View.InvokeFieldUpdateService("FCBaseUnitQty", row);
                if (!parentid.Equals("0"))
                {
                    this.View.Model.SetValue("FPARENTROWID", parentid, row);
                }
                string FPARENTROWID = this.View.Model.GetValue("FPARENTROWID", 0).ToString();
                string FROWID = this.View.Model.GetValue("FROWID", row).ToString();
                List<DynamicObject> dynamicObjects = HighVersionBomDatas.HighVersionBomData(this.Context, Convert.ToInt64(bomdy["WLID"]), QuoteOrgId, 0);
                if (dynamicObjects.Count > 0)
                {
                    NewMethod(FROWID, FBOMLevel, QuoteOrgId, Convert.ToInt64(dynamicObjects[0]["ID"]), qtysum, row);
                }
                else
                {
                    this.View.Model.SetValue("FIsEndLevel", true, row);

                    #region 2026年1月13日新逻辑

                    //取采购价目表价格（自定义模拟报价单采购价目表）
                    if (parentid.Equals("0"))
                    {
                        this.View.Model.SetItemValueByNumber("FCMatPriceFrom_B", "RAWPRICE02", row);//材料单价来源-采购价目表

                        string sql = $"select FMATERIALID,FTAXPRICE from  t_PUR_PriceList  p left join  t_PUR_PriceListEntry pe on p.FID=pe.FID where F_MoniQuotation=1 AND FMATERIALID='{Convert.ToInt64(bomdy["WLID"])}' order by FTAXPRICE desc";
                        DynamicObjectCollection pricedys = DBUtils.ExecuteDynamicObject(Context, sql);
                        if (pricedys != null)
                        {
                            this.View.Model.SetValue("FCMatPrice", pricedys[0]["FTAXPRICE"], row);
                            this.View.InvokeFieldUpdateService("FCMatPrice", row);
                            parentprice+=Convert.ToDecimal(this.View.Model.GetValue("FCMatAmount", row));
                        }

                    }
                    #endregion 2026年1月13日新逻辑    


                    #region 自定义逻辑
                    ////物料属性：外购
                    //if (bomdy["FErpClsID"].Equals("1"))
                    //{
                    //    this.View.Model.SetItemValueByNumber("FCMatPriceFrom_B", "RAWPRICE03", row);//材料单价来源-采购入库价
                    //}
                    ////物料属性：不为自制。上级赋值
                    //string FPARENTROWID2 = this.View.Model.GetValue("FPARENTROWID", row).ToString();
                    //if (!FPARENTROWID2.Equals(FPARENTROWID) && !bomdy["FErpClsID"].Equals("2"))
                    //{

                    //    this.View.Model.SetItemValueByNumber("FCMatPriceFrom_B", "SELFPRICE03", row2);//材料单价来源-bom子项卷算
                    //    this.View.Model.SetItemValueByNumber("FCMATREFERPRICEFROM_B", "SELFPRICE03", row2);//材料参考来源-BOM子项卷算
                    //    this.View.Model.SetItemValueByNumber("FCSUBPRICEFROM_B", "SUBPRICE03", row2);//外委取价来源-采购订单价

                    //}
                    #endregion 自定义逻辑
                }
            }

            this.View.Model.SetValue("FMaterialCost", parentprice, 0);
            this.View.Model.SetValue("FQuoteCost", parentprice, 0);
            this.View.InvokeFieldUpdateService("FQuoteCost", 0);
        }

        public override void AfterDoOperation(AfterDoOperationEventArgs e)
        {
            base.AfterDoOperation(e);
            String Operation = e.Operation.Operation;
            StringBuilder json = new StringBuilder();
            try
            {
                if ((string.Equals(Operation, "Save", StringComparison.OrdinalIgnoreCase)|| string.Equals(Operation, "Submit", StringComparison.OrdinalIgnoreCase)) && e.OperationResult.IsSuccess)
                {
                   decimal F_referenceAmount=decimal.Multiply(Convert.ToDecimal(View.Model.GetValue("FQty", 0)), Convert.ToDecimal(View.Model.GetValue("FTaxPrice", 0)));

                    DBUtils.Execute(Context, $"update t_quotationEntry set F_referencePrice='{this.View.Model.GetValue("FTaxPrice", 0)}',F_referenceAmount='{F_referenceAmount}' where FEntryID='{this.View.Model.GetValue("F_srcfentry", 0)}' ");
                    DBUtils.Execute(Context, $"update t_quotation set F_ModifyDate=getdate(),F_ApproveDate=getdate()  where fid='{this.View.Model.GetValue("F_srcfid", 0)}' ");

                }
            }
            catch (Exception ex)
            {
            }
         }
    }
}
