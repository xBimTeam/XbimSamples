using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Common.Step21;
using Xbim.Common;

namespace BasicExamples
{
    public class TriangulatedFaceSet
    {
        public static void CreateTriangulatedFaceSet(IfcStore model)
        {
            // only available in IFC4
            if (((IModel)model).SchemaVersion != XbimSchemaVersion.Ifc4)
                return;

            using (var txn = model.BeginTransaction("IfcTriangulatedFaceSet"))
            {
                var coordinates = model.Instances.New<IfcCartesianPointList3D>();
                var mesh = model.Instances.New<IfcTriangulatedFaceSet>(fs =>
                {
                    fs.Closed = false;
                    fs.Coordinates = coordinates;
                });
                var points = new double[][] {
                        new [] {0d,0d,0d},
                        new [] {1d,0d,0d},
                        new [] {1d,1d,0d},
                        new [] {0d,1d,0d},
                        new [] {0d,0d,2d},
                        new [] {1d,0d,2d},
                        new [] {1d,1d,2d},
                        new [] {0d,1d,2d}
                    };
                var indices = new long[][] {
                        new [] {1L,6L,5L},
                        new [] {1L,2L,6L},
                        new [] {6L,2L,7L},
                        new [] {7L,2L,3L},
                        new [] {7L,8L,6L},
                        new [] {6L,8L,5L},
                        new [] {5L,8L,1L},
                        new [] {1L,8L,4L},
                        new [] {4L,2L,1L},
                        new [] {2L,4L,3L},
                        new [] {4L,8L,7L},
                        new [] {7L,3L,4L}
                    };
                for (int i = 0; i < points.Length; i++)
                {
                    var values = points[i].Select(v => new IfcLengthMeasure(v));
                    coordinates.CoordList.GetAt(i).AddRange(values);
                }
                for (int i = 0; i < indices.Length; i++)
                {
                    var values = indices[i].Select(v => new IfcPositiveInteger(v));
                    mesh.CoordIndex.GetAt(i).AddRange(values);
                }

                txn.Commit();
            }
        }
    }
}
