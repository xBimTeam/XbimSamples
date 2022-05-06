using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Ifc.Extensions;
using Xbim.Ifc4.Interfaces;

namespace BasicExamples
{
    class GetBeamEndpoinsExample
    {
        internal static void Run(IModel model)
        {
            foreach (var beam in model.Instances.OfType<IIfcBeam>())
            {
                var representation = beam.Representation?.Representations?
                    .FirstOrDefault(r => string.Equals(r.RepresentationIdentifier, "body", StringComparison.OrdinalIgnoreCase));

                // no 3D geometry
                if (representation == null || representation.Items.Count == 0)
                {
                    model.Logger.LogWarning($"Skipped beam #{beam.EntityLabel}. No 3D representation.");
                    continue;
                }

                // more than one item. This would require further handling to identify the points
                if (representation.Items.Count > 1)
                {
                    model.Logger.LogWarning($"Skipped beam #{beam.EntityLabel}. More than 1 representation items.");
                    continue;
                }

                var item = representation.Items[0];
                if (!(item is IIfcExtrudedAreaSolid extrusion))
                {
                    // other representation item types would require different handling. It might be triangulated geometry, or sweep or something else
                    model.Logger.LogWarning($"Skipped beam #{beam.EntityLabel}. Representation item is {item.GetType().Name}");
                    continue;
                }

                // transformation matrix computed from the placement hierarchy
                var transformation = beam.ObjectPlacement.ToMatrix3D();

                // swept area is the most common 3D representation of beams
                var profileDef = extrusion.SweptArea;
                if (!(profileDef is IIfcParameterizedProfileDef paramProfile) || paramProfile.Position == null)
                {
                    // other representation types would require other processing
                    continue;
                }

                var point = paramProfile.Position.Location;
                var xpoint = new XbimPoint3D(point.X, point.Y, point.Z);
                var start = transformation.Transform(xpoint);

                model.Logger.LogInformation($"Beam starts at [{start.X}, {start.Y}, {start.Z}]");

                var direction = extrusion.ExtrudedDirection;
                var xdirection = new XbimVector3D(direction.X, direction.Y, direction.Z);
                var translation = xdirection.Normalized() * extrusion.Depth;
                var end = start + translation;

                model.Logger.LogInformation($"Beam ends at [{end.X}, {end.Y}, {end.Z}]");
            }
        }
    }
}
