using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.RepresentationResource;

namespace BasicExamples
{
    public class GeoreferencingExample
    {
        public static void Run(IModel model, ILogger<GeoreferencingExample> logger)
        {
            var project = model.Instances.FirstOrDefault<IIfcProject>();
            if (project == null)
            {
                logger.LogInformation("No project in the model. This is probably not a project file.");
                return;
            }

            var sites = model.Instances.OfType<IIfcSite>().ToList();
            if (sites.Count == 0)
            {
                logger.LogWarning("No site in the model");
                return;
            }
            if (sites.Count > 1)
                logger.LogWarning("Only one site expected. Found {sitesCount} sites. All root locations will be enhanced.", sites.Count);

            using (var txn = model.BeginTransaction("Origin enhancement"))
            {
                foreach (var site in sites)
                {
                    if (!(site.ObjectPlacement is IIfcLocalPlacement localPlacement))
                    {
                        logger.LogWarning("Site {siteName} doesn't have a local placement.", site.Name);
                        continue;
                    }
                    if (!(localPlacement.RelativePlacement is IIfcPlacement placement))
                    {
                        logger.LogWarning("Site {siteName} local placement is not a 3D placement.", site.Name);
                        continue;
                    }

                    var location = placement.Location;
                    if (location == null)
                    {
                        logger.LogWarning("Site {siteName} local placement doesn't have a 3D definition point.", site.Name);
                        continue;
                    }

                    // move the location
                    location.Coordinates[0] += 1000000;
                    location.Coordinates[1] += 500000;
                }

                // define GIS coordinate reference system
                if (model.SchemaVersion == Xbim.Common.Step21.XbimSchemaVersion.Ifc4)
                {
                    var geomContext = project.RepresentationContexts.FirstOrDefault(c => c.ContextType == "Model") as IfcGeometricRepresentationContext;
                    model.Instances.New<IfcMapConversion>(mc => {
                        mc.SourceCRS = geomContext;
                        mc.TargetCRS = model.Instances.New<IfcProjectedCRS>(proj => {
                            proj.Name = "EPSG:27700";
                            proj.Description = "British National Grid";
                            proj.GeodeticDatum = "OSGB 1936";
                            proj.MapProjection = "Transverse Mercator";
                        });
                    });
                }

                txn.Commit();
            }

        }

    }
}
