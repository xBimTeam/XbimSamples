using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.IO;

namespace BasicExamples
{
    class InsertCopy
    {
        public void CopyWallsOver()
        {
            const string original = "SampleHouse.ifc";
            const string inserted = "SampleHouseWalls.ifc";

            object semanticFilter(Xbim.Common.Metadata.ExpressMetaProperty property, object parentObject)
            {
                //leave out geometry and placement
                if (parentObject is IIfcProduct &&
                    (property.PropertyInfo.Name == "Representation" || // nameof() removed to allow for VS2013 compatibility
                    property.PropertyInfo.Name == "ObjectPlacement")) // nameof() removed to allow for VS2013 compatibility
                    return null;

                //leave out mapped geometry
                if (parentObject is IIfcTypeProduct &&
                    property.PropertyInfo.Name == "RepresentationMaps") // nameof() removed to allow for VS2013 compatibility
                    return null;


                //only bring over IsDefinedBy and IsTypedBy inverse relationships which will take over all properties and types
                if (property.EntityAttribute.Order < 0 && !(
                    property.PropertyInfo.Name == "IsDefinedBy" || // nameof() removed to allow for VS2013 compatibility
                    property.PropertyInfo.Name == "IsTypedBy"      // nameof() removed to allow for VS2013 compatibility
                    ))
                    return null;

                return property.PropertyInfo.GetValue(parentObject, null);
            }

            IfcStore.ModelProviderFactory.UseMemoryModelProvider();
            using (var model = IfcStore.Open(original))
            {
                var walls = model.Instances.OfType<IIfcWall>();
                using (var iModel = IfcStore.Create(((IModel)model).SchemaVersion, XbimStoreType.InMemoryModel))
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
