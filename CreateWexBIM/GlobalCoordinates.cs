using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Ifc4.Interfaces;
using Xbim.ModelGeometry.Scene;

namespace CreateWexBIM
{
    internal static class GlobalCoordinates
    {
        private static IDictionary<int, XbimMatrix3D> CrsCache = new Dictionary<int, XbimMatrix3D>();

        public static void Process(IModel model)
        {
            var context = new Xbim3DModelContext(model: model);

            context.CreateContext(
                progDelegate: null,
                adjustWcs: false  // Make sure we do NOT adjust the WCS to site-local coordinates
                );

            var geometry = model.GeometryStore.BeginRead();

            // any product with representation as an example
            var product = model.Instances.OfType<IIfcProduct>().FirstOrDefault(p => p.Representation != null);

            // get the shapes. One product can have multiple shape instances (e.g. a door made of panels and the frame)
            var shape = geometry.ShapeInstancesOfEntity(product).FirstOrDefault();

            var shapeContextId = shape.RepresentationContext;
            XbimMatrix3D crsMatrix = GetMapMatrix(model, shapeContextId);

            // combine the shape local transformation with the CRS transformation to get global coordinates
            var shapeTransform = crsMatrix * shape.Transformation;

            // apply the transformation to all points of the geometry as you currently do

        }

        private static XbimMatrix3D GetMapMatrix(IModel model, int shapeContextId)
        {
            // get or compute the CRS transformation matrix
            if (CrsCache.TryGetValue(shapeContextId, out var crsMatrix))
            {
                return crsMatrix;
            }

            // operation might be defined on the parent context(s)
            var operation = GetCoordinateOperation(model, shapeContextId);
            if (operation == null)
            {
                // worth caching for reuse. Likely only one CRS in a model
                CrsCache.Add(shapeContextId, crsMatrix);
                return crsMatrix;
            }

            if (operation is IIfcMapConversion mc)
            {
                var northingOffset = mc.Northings;
                var eastingOffset = mc.Eastings;
                var heightOffset = mc.OrthogonalHeight;
                var scale = mc.Scale ?? 1;
                // using atan2 to get the angle in radians, convering all the quadrants
                var rotationTheta = Math.Atan2(mc.XAxisOrdinate ?? 0.0D, mc.XAxisAbscissa ?? 1.0D);

                // apply rotation first
                crsMatrix.RotateAroundZAxis(rotationTheta);

                // then scaling - might be nonuniform scaling in the latest version of IFC4.3
                if (mc is Xbim.Ifc4x3.RepresentationResource.IfcMapConversionScaled mcs)
                {
                    var scaleX = mcs.FactorX;
                    var scaleY = mcs.FactorY;
                    var scaleZ = mcs.FactorZ;

                    crsMatrix.Scale(new XbimVector3D(scaleX, scaleY, scaleZ));
                }
                else
                {
                    crsMatrix.Scale(new XbimVector3D(scale, scale, scale));
                }

                // then translation
                crsMatrix.OffsetX = eastingOffset;
                crsMatrix.OffsetY = northingOffset;
                crsMatrix.OffsetZ = heightOffset;
            }
            else if (operation is Xbim.Ifc4x3.RepresentationResource.IfcRigidOperation ro)
            {
                // expect length measures
                if (ro.FirstCoordinate is Xbim.Ifc4x3.MeasureResource.IfcLengthMeasure dx &&
                     ro.SecondCoordinate is Xbim.Ifc4x3.MeasureResource.IfcLengthMeasure dy &&
                     ro.Height is Xbim.Ifc4x3.MeasureResource.IfcLengthMeasure dz
                    )
                {
                    // just translation
                    crsMatrix.OffsetX = dx;
                    crsMatrix.OffsetY = dy;
                    crsMatrix.OffsetZ = dz;
                }
                else
                {
                    throw new NotSupportedException("Type of the rigid operation not expected, likely using angle offset");
                }
            }
            else
            {
                throw new NotSupportedException("Type of the coordinate conversion not expected");
            }

            // worth caching for reuse. Likely only one CRS in a model
            CrsCache.Add(shapeContextId, crsMatrix);

            return crsMatrix;
        }

        private static IIfcCoordinateOperation GetCoordinateOperation(IModel model, int contextId)
        {
            var context = model.Instances[contextId] as IIfcGeometricRepresentationContext;
            var contexts = new List<IIfcGeometricRepresentationContext>(new[] { context });
            while (context is IIfcGeometricRepresentationSubContext sub && sub.ParentContext != null)
            {
                contexts.Add(sub.ParentContext);
                context = sub.ParentContext;
            }

            foreach (var ctx in contexts)
            {
                // there might be multiple coordinate operations applied. Select the one you expect.
                var operations = ctx.HasCoordinateOperation.ToList();
                var operation =
                    // IFCPROJECTEDCRS('EPSG:25832','ETRS89/UTM SONE 32N','EUREF89','NN2000','Gauss Kruger','ETRS89/UTM SONE 32N',$);
                    operations.FirstOrDefault(crs => crs.TargetCRS.Name.ToString().Equals("EPSG:25832", StringComparison.OrdinalIgnoreCase)) ??
                    // take the first one if it is the only one
                    operations.FirstOrDefault();

                if (operation != null) return operation;
            }

            return null;
        }
    }
}
