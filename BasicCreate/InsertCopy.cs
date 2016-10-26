using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace BasicExamples
{
    class InsertCopy
    {
        public void CopyWallsOver()
        {
            const string original = "SampleHouse.ifc";
            const string inserted = "SampleHouseWalls.ifc";

            PropertyTranformDelegate semanticFilter = (property, parentObject) =>
            {
                //leave out geometry and placement
                if (parentObject is IIfcProduct &&
                    (property.PropertyInfo.Name == nameof(IIfcProduct.Representation) ||
                    property.PropertyInfo.Name == nameof(IIfcProduct.ObjectPlacement)))
                    return null;

                //leave out mapped geometry
                if (parentObject is IIfcTypeProduct && 
                    property.PropertyInfo.Name == nameof(IIfcTypeProduct.RepresentationMaps))
                    return null;


                //only bring over IsDefinedBy and IsTypedBy inverse relationships which will take over all properties and types
                if (property.EntityAttribute.Order < 0 && !(
                    property.PropertyInfo.Name == nameof(IIfcProduct.IsDefinedBy) ||
                    property.PropertyInfo.Name == nameof(IIfcProduct.IsTypedBy)
                    ))
                    return null;

                return property.PropertyInfo.GetValue(parentObject, null);
            };

            using (var model = IfcStore.Open(original))
            {
                var walls = model.Instances.OfType<IIfcWall>();
                using (var iModel = IfcStore.Create(model.IfcSchemaVersion, XbimStoreType.InMemoryModel))
                {
                    using (var txn = iModel.BeginTransaction("Insert copy"))
                    {
                        //single map should be used for all insertions between two models
                        var map = new XbimInstanceHandleMap(model, iModel);

                        foreach (var wall in walls)
                        {
                            iModel.InsertCopy(wall, map, semanticFilter, true, false);
                        }

                        txn.Commit();
                    }

                    iModel.SaveAs(inserted);
                }
            }
        }
    }
}
