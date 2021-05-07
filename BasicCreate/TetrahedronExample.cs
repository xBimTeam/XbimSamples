using Serilog;
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
using Xbim.Ifc4.SharedBldgElements;
using Xbim.Ifc4.SharedFacilitiesElements;
using Xbim.IO;

namespace BasicExamples
{
    internal class TetrahedronExample
    {
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

            using (var model = IfcStore.Create(credentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel))
            {
                using (var txn = model.BeginTransaction("Creating content"))
                {
                    var project = InitializeProject(model, "The Cube Project");
                    var building = CreateBuilding(model, project, "Building for the Cube");
                    var product = CreateProduct(model, building);
                    txn.Commit();
                }

                //write the Ifc File
                model.SaveAs("Tetrahedron.ifc", StorageType.Ifc);
            }
            Log.Information("Tetrahedron.ifc file created and saved.");
        }

        private static IfcBuilding CreateBuilding(IfcStore model, IfcProject project, string name)
        {
            var building = model.Instances.New<IfcBuilding>(b => b.Name = name);
            project.AddBuilding(building);
            return building;
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
                    c.Precision = 0.00001;
                    c.WorldCoordinateSystem = i.New<IfcAxis2Placement3D>(a => a.Location = i.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0)));
                }
            ));

            // create plan representation context
            project.RepresentationContexts.Add(i.New<IfcGeometricRepresentationContext>(c =>
                {
                    c.ContextType = "Plan";
                    c.ContextIdentifier = "Building Plan View";
                    c.CoordinateSpaceDimension = 2;
                    c.Precision = 0.00001;
                    c.WorldCoordinateSystem = i.New<IfcAxis2Placement2D>(a => a.Location = i.New<IfcCartesianPoint>(p => p.SetXY(0, 0)));
                }
            ));

            //now commit the changes, else they will be rolled back at the end of the scope of the using statement
            return project;

        }

        /// <summary>
        /// This creates a product and it's geometry, many geometric representations are possible. 
        /// This example uses triangulated face set
        /// </summary>
        /// <param name="model">Model</param>
        /// <returns>Product with placement and geometry representation</returns>
        private static IfcProduct CreateProduct(IModel model, IfcBuilding parent)
        {
            var i = model.Instances;
            
            // geometry as a triangulated face set
            var body = CreateTetrahedron(model);

            // create a Definition shape to hold the geometry
            var shape = i.New<IfcShapeRepresentation>(s => { 
                s.ContextOfItems = i.OfType<IfcGeometricRepresentationContext>().First();
                s.RepresentationType = "Tessellation";
                s.RepresentationIdentifier = "Body";
                s.Items.Add(body);
            });

            // IfcPresentationLayerAssignment is required for CAD presentation
            var ifcPresentationLayerAssignment = i.New<IfcPresentationLayerAssignment>(layer => { 
                layer.Name = "Furnishing Elements";
                layer.AssignedItems.Add(shape);
            });

            // create visual style
            i.New<IfcStyledItem>(styleItem =>
            {
                styleItem.Item = body;
                styleItem.Styles.Add(i.New<IfcSurfaceStyle>(style =>
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
                }));
            });

            var proxy = i.New<IfcBuildingElementProxy>(c => {
                c.Name = "The Tetrahedron";

                // create a Product Definition and add the model geometry to the cube
                c.Representation = i.New<IfcProductDefinitionShape>(r => r.Representations.Add(shape));

                // now place the object into the model
                c.ObjectPlacement = i.New<IfcLocalPlacement>(p => p.RelativePlacement = i.New<IfcAxis2Placement3D>(a => {
                    a.Location = i.New<IfcCartesianPoint>(cp => cp.SetXYZ(0, 0, 0));
                    a.RefDirection = i.New<IfcDirection>();
                    a.RefDirection.SetXYZ(0, 1, 0);
                    a.Axis = i.New<IfcDirection>();
                    a.Axis.SetXYZ(0, 0, 1);
                }));
            });


            parent.AddElement(proxy);
            return proxy;
        }

        /// <summary>
        /// Creates the simplest 3D triangulated face set
        /// </summary>
        /// <param name="model">Model</param>
        /// <returns>Tetrahedron as a triangulated face set</returns>
        private static IfcTriangulatedFaceSet CreateTetrahedron(IModel model)
        {
            return model.Instances.New<IfcTriangulatedFaceSet>(tfs => {
                tfs.Closed = true;
                tfs.Coordinates = model.Instances.New<IfcCartesianPointList3D>(pl => {
                    pl.CoordList.GetAt(0).AddRange(new IfcLengthMeasure[] { 0, 0, 0 });
                    pl.CoordList.GetAt(1).AddRange(new IfcLengthMeasure[] { 1, 0, 0 });
                    pl.CoordList.GetAt(2).AddRange(new IfcLengthMeasure[] { 0, 1, 0 });
                    pl.CoordList.GetAt(3).AddRange(new IfcLengthMeasure[] { 0, 0, 1 });
                });

                // Indices are 1 based in IFC!
                tfs.CoordIndex.GetAt(0).AddRange(new IfcPositiveInteger[] { 1, 3, 2 });
                tfs.CoordIndex.GetAt(1).AddRange(new IfcPositiveInteger[] { 1, 2, 4 });
                tfs.CoordIndex.GetAt(2).AddRange(new IfcPositiveInteger[] { 1, 4, 3 });
                tfs.CoordIndex.GetAt(3).AddRange(new IfcPositiveInteger[] { 2, 3, 4 });
            });
        }
    }
}
