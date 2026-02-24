using BD.Standard.MM.ListServicePlugInzs.Form;
using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.ComponentModel;

namespace BD.Standard.MM.ListServicePlugIncs2
{
    [Description("报价单表单插件")]
    [Kingdee.BOS.Util.HotUpdate]
    public class Quotation : AbstractDynamicFormPlugIn
    {

        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            int row = e.Row;
            if (e.Field.FieldName.EqualsIgnoreCase("F_Material"))
            {
                object newValue = e.NewValue;

                DynamicObject FOrgId = (DynamicObject)this.View.Model.GetValue("F_OrgId");

                System.Collections.Generic.List<DynamicObject> dynamicObjects = HighVersionBomDatas.HighVersionBomData(this.Context, Convert.ToInt64(newValue), Convert.ToInt64(FOrgId[0].ToString()), 0);

                if (dynamicObjects.Count > 0)
                {
                    this.View.Model.SetItemValueByID("F_BOM", dynamicObjects[0]["id"], row);
                }
                else
                {
                    this.View.Model.SetItemValueByID("F_BOM", 0, row);
                }

            }
        }

    }
}
