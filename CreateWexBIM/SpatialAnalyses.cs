using System;
using System.Linq;
using Xbim.Common;
using Xbim.Geometry.Engine.Interop;
using Xbim.Ifc4.Interfaces;
using Microsoft.Extensions.Logging;
using Xbim.Ifc.Extensions;
using Xbim.Common.Geometry;

namespace CreateWexBIM
{
    internal class SpatialAnalyses
    {
        public static void AnalyseIntersections(IModel model)
        {
            // validation tollerance might be based on something else
            var volumeTollerance = Math.Pow(model.ModelFactors.PrecisionBoolean, 3);

            var logger = XbimLogging.LoggerFactory.CreateLogger<XbimGeometryEngine>();
            var engine = new XbimGeometryEngine(logger);
            var walls = model.Instances.OfType<IIfcWall>();
            foreach (var wall in walls)
            {
                // assuming wall only has one shape and no openings or other features
                var wallSolid = GetFirstGeometry(engine, wall, logger);
                if (wallSolid == null || !wallSolid.IsValid)
                {
                    //TODO: handle other cases
                    logger.LogWarning("Skipping wall #{wallId} as there is no solid geometry.", wall.EntityLabel);
                    continue;
                }

                // note this is not the most efficient way to get nested objects even though it is the least code impl.
                var bars = wall.Nests.SelectMany(r => r.RelatedObjects).OfType<IIfcReinforcingBar>();
                foreach (var bar in bars)
                {
                    var barGeom = GetFirstGeometry(engine, bar, logger);
                    var barVolume = barGeom.Volume;
                    if (barGeom == null || !barGeom.IsValid)
                    {
                        // TODO: handle other cases
                        logger.LogWarning("Skipping reinforcing bar #{barId} as there is no solid geometry.", bar.EntityLabel);
                        continue;
                    }

                    var intersectionParts = wallSolid.Intersection(barGeom, model.ModelFactors.PrecisionBoolean, logger);
                    var intersectionVolume = intersectionParts.Sum(s => s.Volume);
                    var volumeDiff = Math.Abs(intersectionVolume - barVolume);

                    if (volumeDiff > volumeTollerance)
                    {
                        logger.LogInformation("Bar #{barId} is declared to be withing wall #{wallId} but the volume difference is {volumeDiff}", bar.EntityLabel, wall.EntityLabel, volumeDiff);
                    }
                }
            }
        }

        private static IIfcGeometricRepresentationItem GetFirstGeometryRepresentation(IIfcProduct product)
        {
            // this is a naive approach only for demonstration purposes
            var items = product.Representation?.Representations?.Where(r =>
                    string.Equals("body", r.RepresentationIdentifier?.Value.ToString(), StringComparison.OrdinalIgnoreCase))
                    .SelectMany(r => r.Items).OfType<IIfcGeometricRepresentationItem>()
                    .ToList();
            if (items.Count > 1)
                // TODO: implement more complex scenarios
                throw new NotSupportedException();

            return items[0];
        }

        private static IXbimSolid GetFirstGeometry(IXbimGeometryEngine engine, IIfcProduct product, ILogger<XbimGeometryEngine> logger)
        {
            // this is naive, only usable to demonstrate the functionality. Not considering maps, features and other aspects
            var representation = GetFirstGeometryRepresentation(product);
            var geometry = engine.Create(representation, logger);
            var placement = product.ObjectPlacement.ToMatrix3D();

            if (geometry is IXbimGeometryObjectSet set)
            {
                var solids = set.OfType<IXbimSolid>();
                if (solids.Count() > 1)
                {
                    // TODO: handle multiple solids as a result
                    throw new NotSupportedException();
                }
                var first = solids.FirstOrDefault();
                return first.Transform(placement) as IXbimSolid;
            }
            else if (geometry is IXbimSolid solid)
                return solid.Transform(placement) as IXbimSolid;

            // TODO: handle mores cases
            throw new NotSupportedException();
        }
    }
}
