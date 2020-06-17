using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.IO;

namespace BasicExamples
{
    public class NestedCartesianPointListExample
    {
        public static void Run()
        {
            using (var model = IfcStore.Create(XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel))
            {
                using (var txn = model.BeginTransaction())
                {
                    var pointList = model.Instances.New<IfcCartesianPointList3D>(cpl =>
                    {
                        cpl.CoordList.GetAt(0).AddRange(new IfcLengthMeasure[] { 122.544, 445.151, 13.673 });
                        cpl.CoordList.GetAt(1).AddRange(new IfcLengthMeasure[] { 137.671, 442.768, 13.7401 });
                        cpl.CoordList.GetAt(2).AddRange(new IfcLengthMeasure[] { 142.393, 462.543, 11.4145 });
                    });
                    txn.Commit();
                }
                model.SaveAs("IfcCartesianPointList3D.ifc");
            }
        }
    }
}
