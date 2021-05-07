using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PresentationAppearanceResource;
using Xbim.Ifc4.PresentationOrganizationResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.SharedFacilitiesElements;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;

namespace DeflectionExample
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
            XbimLogging.LoggerFactory.AddSerilog();

            Run();
        }

        /// <summary>
        /// This sample demonstrates the minimum steps to create a compliant IFC model that contains a single standard case cube
        /// </summary>
        public static void Run()
        {
            Log.Information("Initialising the IFC Project....");

            //first we need to set up some credentials for ownership of data in the new model
            var credentials = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "xBimTeam",
                ApplicationFullName = "Cube Example",
                ApplicationIdentifier = "CE",
                ApplicationVersion = "1.0",
                EditorsFamilyName = "Team",
                EditorsGivenName = "xBIM",
                EditorsOrganisationName = "xBimTeam"
            };

            IfcStore.ModelProviderFactory.UseMemoryModelProvider();
            var sizes = new List<double>();
            using (var model = IfcStore.Create(credentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel))
            {
                using (var txn = model.BeginTransaction("Creating content"))
                {
                    var project = InitializeProject(model, "The Cube Project");
                    var building = CreateBuilding(model, project, "Building for the Cube");
                    var style = CreateDefaultStyle(model);

                    var size = 100D / 1.2;
                    var offset = 0D;
                    for (int i = 0; i < 20; i++)
                    {
                        offset += size / 2.0;
                        size *= 1.2;
                        offset += size / 2.0;
                        CreateCubeFurniture(model, size, building, offset, style);
                        Log.Information($"Size: {size:N0} mm");
                        sizes.Add(size);
                    }

                    txn.Commit();
                }

                //write the Ifc File
                model.SaveAs("ThePipes.ifc", StorageType.Ifc);
                Log.Information("ThePipes.ifc file created and saved.");
            }

            using (var w = File.CreateText("report.csv"))
            {
                w.Write("sizes,");
                w.WriteLine(string.Join(",", sizes.Select(s => $"{s:########}")));

                // no impact of the angle
                CreateWexbim("ThePipes.ifc", 5,   359, w);
                CreateWexbim("ThePipes.ifc", 10,  359, w);
                CreateWexbim("ThePipes.ifc", 20,  359, w);
                CreateWexbim("ThePipes.ifc", 50,  359, w);
                CreateWexbim("ThePipes.ifc", 100, 359, w);
                CreateWexbim("ThePipes.ifc", 250, 359, w);

                // no impact of linear deflection
                CreateWexbim("ThePipes.ifc", 3000, 10, w);
                CreateWexbim("ThePipes.ifc", 3000, 20, w);
                CreateWexbim("ThePipes.ifc", 3000, 50, w);
                CreateWexbim("ThePipes.ifc", 3000, 100, w);
                CreateWexbim("ThePipes.ifc", 3000, 150, w);
                CreateWexbim("ThePipes.ifc", 3000, 200, w);
            }


            Log.Information("Wexbim files were created.");
        }

        private static void CreateWexbim(string path, double linearDeflection, double angularDeflection, TextWriter w)
        {
            using (var model = IfcStore.Open(path, null, -1))
            {
                Log.Information("Creating wexBIM file from IFC model.");

                // linear deflection in model units (for example milimetres if the model is in milimetres) (default = 5 mm)
                model.ModelFactors.DeflectionTolerance = linearDeflection;
                // angular deflection in radians (default = 28.6 DEG = 0.5 RAD)
                model.ModelFactors.DeflectionAngle = angularDeflection / 180.0 * Math.PI;

                var context = new Xbim3DModelContext(model);
                context.CreateContext(null, false);

                var wexBimFilename = Path.ChangeExtension($"L{linearDeflection:N0}_A{angularDeflection:N0}_{path}", "wexbim");
                using (var wexBimFile = File.Create(wexBimFilename))
                {
                    using (var wexBimBinaryWriter = new BinaryWriter(wexBimFile))
                    {
                        model.SaveAsWexBim(wexBimBinaryWriter);
                        wexBimBinaryWriter.Close();
                    }
                    wexBimFile.Close();
                    Log.Information($"Saved file: {wexBimFilename}");
                }

                var count = context.ShapeGeometries().Sum(g => g.TriangleCount);
                Log.Information($"Deflection angle: {angularDeflection}, Deflection tollerance: {linearDeflection}, Triangles count: {count}");


                var counts = context.ShapeGeometries().OrderBy(g => g.TriangleCount).Select(g => $"{g.TriangleCount:######}");
                w.Write($"\"L={linearDeflection:N0}, A={angularDeflection:N0}\",");
                w.WriteLine(string.Join(",", counts));
            }
        }

        private static IfcBuilding CreateBuilding(IfcStore model, IfcProject project, string name)
        {
            var building = model.Instances.New<IfcBuilding>(b => b.Name = name);
            project.AddBuilding(building);
            return building;
        }

        private static IfcSurfaceStyle CreateDefaultStyle(IModel model)
        {
            var i = model.Instances;
            return i.New<IfcSurfaceStyle>(style =>
            {
                style.Side = IfcSurfaceSide.BOTH;
                style.Styles.Add(i.New<IfcSurfaceStyleRendering>(rendering =>
                {
                    rendering.SurfaceColour = i.New<IfcColourRgb>(colour =>
                    {
                        colour.Name = "Orange";
                        colour.Red = 1.0;
                        colour.Green = 0.5;
                        colour.Blue = 0.0;
                    });
                }));
            });
        }

        /// <summary>
        /// Sets up the basic parameters any model must provide, units, ownership etc
        /// </summary>
        /// <param name="projectName">Name of the project</param>
        /// <returns></returns>
        private static IfcProject InitializeProject(IModel model, string projectName)
        {
            var i = model.Instances;

            //create a project
            var project = model.Instances.New<IfcProject>(p => p.Name = projectName);

            //set the units, at least length unit and plane angle units are needed for geometry definitions to work
            project.UnitsInContext = i.New<IfcUnitAssignment>(a =>
            {
                a.Units.AddRange(new[] {
                    i.New<IfcSIUnit>(u => {
                        u.UnitType = IfcUnitEnum.LENGTHUNIT;
                        u.Name = IfcSIUnitName.METRE;
                        u.Prefix = IfcSIPrefix.MILLI;
                    }),
                    i.New<IfcSIUnit>(u => {
                        u.UnitType = IfcUnitEnum.PLANEANGLEUNIT;
                        u.Name = IfcSIUnitName.RADIAN;
                    })
                });
            });

            // create model representation context
            project.RepresentationContexts.Add(i.New<IfcGeometricRepresentationContext>(c =>
            {
                c.ContextType = "Model";
                c.ContextIdentifier = "Building Model";
                c.CoordinateSpaceDimension = 3;
                c.Precision = 0.01;
                c.WorldCoordinateSystem = i.New<IfcAxis2Placement3D>(a => a.Location = i.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0)));
            }
            ));

            // create plan representation context
            project.RepresentationContexts.Add(i.New<IfcGeometricRepresentationContext>(c =>
            {
                c.ContextType = "Plan";
                c.ContextIdentifier = "Building Plan View";
                c.CoordinateSpaceDimension = 2;
                c.Precision = 0.01;
                c.WorldCoordinateSystem = i.New<IfcAxis2Placement2D>(a => a.Location = i.New<IfcCartesianPoint>(p => p.SetXY(0, 0)));
            }
            ));

            //now commit the changes, else they will be rolled back at the end of the scope of the using statement
            return project;

        }
        /// <summary>
        /// This creates a cube and it's geometry, many geometric representations are possible. This example uses extruded rectangular profile
        /// </summary>
        /// <param name="model"></param>
        /// <param name="size">Size of the cube</param>
        /// <returns></returns>
        static private IfcFurniture CreateCubeFurniture(IModel model, double size, IfcBuilding parent, double offset, IfcStyleAssignmentSelect style)
        {
            var i = model.Instances;
            var cube = i.New<IfcFurniture>(c => c.Name = $"The Cube {size:N2}");
            parent.AddElement(cube);

            // represent cube as a rectangular profile
            var rectProf = i.New<IfcCircleProfileDef>(pr =>
            {
                pr.ProfileType = IfcProfileTypeEnum.AREA;
                pr.Radius = size / 2.0;
                //insert at arbitrary position
                pr.Position = i.New<IfcAxis2Placement2D>(a2p => a2p.Location = i.New<IfcCartesianPoint>(p => p.SetXY(0, 0)));
            });


            // model as a swept area solid
            var body = i.New<IfcExtrudedAreaSolid>(b =>
            {
                b.Depth = size;
                b.SweptArea = rectProf;
                b.ExtrudedDirection = i.New<IfcDirection>();
                b.ExtrudedDirection.SetXYZ(0, 0, 1);
                //parameters to insert the geometry in the model
                b.Position = i.New<IfcAxis2Placement3D>(p => p.Location = i.New<IfcCartesianPoint>(c => c.SetXYZ(0, 0, 0)));
            });


            // create a Definition shape to hold the geometry
            var shape = i.New<IfcShapeRepresentation>(s =>
            {
                s.ContextOfItems = i.OfType<IfcGeometricRepresentationContext>().First();
                s.RepresentationType = "SweptSolid";
                s.RepresentationIdentifier = "Body";
                s.Items.Add(body);
            });

            // set visual style
            i.New<IfcStyledItem>(styleItem =>
            {
                styleItem.Item = body;
                styleItem.Styles.Add(style);
            });

            // create a Product Definition and add the model geometry to the cube
            cube.Representation = i.New<IfcProductDefinitionShape>(r => r.Representations.Add(shape));

            // now place the cube into the model
            cube.ObjectPlacement = i.New<IfcLocalPlacement>(p => p.RelativePlacement = i.New<IfcAxis2Placement3D>(a =>
            {
                a.Location = i.New<IfcCartesianPoint>(c => c.SetXYZ(offset, 0, 0));
                a.RefDirection = i.New<IfcDirection>();
                a.RefDirection.SetXYZ(0, 1, 0);
                a.Axis = i.New<IfcDirection>();
                a.Axis.SetXYZ(0, 0, 1);
            }));

            // IfcPresentationLayerAssignment is required for CAD presentation
            var ifcPresentationLayerAssignment = i.New<IfcPresentationLayerAssignment>(layer =>
            {
                layer.Name = "Furnishing Elements";
                layer.AssignedItems.Add(shape);
            });

            return cube;

        }
    }
}
