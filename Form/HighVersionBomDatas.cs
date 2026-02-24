using Kingdee.BOS;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.K3.MFG.ServiceHelper.ENG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BD.Standard.MM.ListServicePlugInzs.Form
{
    public static class HighVersionBomDatas
    {
        public static List<DynamicObject> HighVersionBomData(Context Context ,long FMaterialId,long OrgId, long auxPropId)
        {
            List<Tuple<long, long, long>> dicMasterOrgIds = new List<Tuple<long, long, long>>();
            dicMasterOrgIds.Add(new Tuple<long, long, long>(FMaterialId, OrgId, auxPropId));
            List<DynamicObject> highVersionBomDatas = BOMServiceHelper.GetHightVersionBom(Context, dicMasterOrgIds).ToList();
            return highVersionBomDatas;
        }

    }
}
