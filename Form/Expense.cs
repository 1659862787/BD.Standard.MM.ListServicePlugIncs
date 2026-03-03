using BD.Standard.MM.ListServicePlugInzs.Form;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace BD.Standard.MM.ListServicePlugIncs2
{
    [Description("费用报销单表单插件")]
    [Kingdee.BOS.Util.HotUpdate]
    public class Expense : AbstractDynamicFormPlugIn
    {
        public override void BeforeF7Select(BeforeF7SelectEventArgs e)

        {

            base.BeforeF7Select(e);

            // 给某个辅助资料字段的查询附加过滤条件
            //FTOCONTACTUNIT
            if (e.FieldKey.EqualsIgnoreCase("FCONTACTUNIT") && e.FormId.Equals("BD_Supplier"))
            {
                DynamicObject dept = (DynamicObject)this.View.Model.GetValue("FRequestDeptID");
                if (dept != null)
                {
                    string deptname = dept["Name"].ToString();

                    //明迈而开配置表
                    DynamicObjectCollection dys = DBUtils.ExecuteDynamicObject(this.Context, $"select F_WMQB_GROUP_XHK from   WMQB_t_GroupConfig  where F_WMQB_DEPTMENT='ALL' or F_WMQB_DEPTMENT LIKE '%{deptname}%'");
                    if (dys.Count > 0)
                    {
                        string group = string.Join(",", dys.Select(x => x["F_WMQB_GROUP_XHK"].ToString()));

                        e.ListFilterParameter.Filter = e.ListFilterParameter.Filter.JoinFilterString($" FPRIMARYGROUP IN ( {group})");

                        return;
                    }
                }
            }
            if (e.FieldKey.EqualsIgnoreCase("FTOCONTACTUNIT") && e.FormId.Equals("FIN_OTHERS"))
            {

                DynamicObject dept = (DynamicObject)this.View.Model.GetValue("FDeptID");
                if (dept != null)
                {
                    string deptname = dept["Name"].ToString();

                    //明迈而开配置表
                    DynamicObjectCollection dys = DBUtils.ExecuteDynamicObject(this.Context, $"select F_WMQB_GROUP_5RN from   WMQB_t_GroupConfig1  where F_WMQB_DEPTMENT2='ALL' or F_WMQB_DEPTMENT2 LIKE '%{deptname}%'");

                    if (dys.Count > 0)
                    {
                        string group = string.Join(",", dys.Select(x => x["F_WMQB_GROUP_5RN"].ToString()));

                        e.ListFilterParameter.Filter = e.ListFilterParameter.Filter.JoinFilterString($" F_WMQB_GROUP_OZX IN ( {group})");
                        return;
                    }
                }
            }
        }

    }
}
