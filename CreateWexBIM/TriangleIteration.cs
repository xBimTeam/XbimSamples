using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common.Geometry;
using Xbim.Common.XbimExtensions;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.ModelGeometry.Scene;

namespace CreateWexBIM
{
    public static class TriangleIteration
    {
        public static void Run()
        {
            var fileName = "model.ifc";
            using (var model = IfcStore.Open(fileName))
            {
                Xbim3DModelContext context = new Xbim3DModelContext(model);
                context.CreateContext();

                IEnumerable<IIfcProduct> walls = model.Instances.OfType<IIfcWall>();
                var count = walls.Count();
                Console.WriteLine(count);

                foreach (var wall in walls)
                {
                    Console.WriteLine("\nID: " + wall.GlobalId + "; Nome do Elemento: " + wall.Name);

                    var productShape = context.ShapeInstancesOf(wall);

                    var todas_faces = new List<XbimFaceTriangulation>();

                    foreach (var shape in productShape)
                    {
                        var transform = shape.Transformation;
                        var geometry = context.ShapeGeometry(shape);
                        var data = ((IXbimShapeGeometryData)geometry).ShapeData;

                        using (var stream = new MemoryStream(data))
                        {
                            using (var reader = new BinaryReader(stream))
                            {
                                var mesh = reader.ReadShapeTriangulation();
                                mesh = mesh.Transform(transform);

                                foreach (XbimFaceTriangulation face in mesh.Faces)
                                {
                                    // iterate over triangles
                                    for (int i = 0; i < face.TriangleCount; i++)
                                    {
                                        // get indices pointing to list of vertices
                                        var idx1 = face.Indices[i * 3];
                                        var idx2 = face.Indices[i * 3 + 1];
                                        var idx3 = face.Indices[i * 3 + 2];

                                        // get vertices for the triangle
                                        var p1 = mesh.Vertices[idx1];
                                        var p2 = mesh.Vertices[idx2];
                                        var p3 = mesh.Vertices[idx3];

                                        // here is a single triangle
                                        var triangle = new[] { p1, p2, p3 };
                                    }

                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
