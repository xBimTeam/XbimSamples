using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xbim.Ifc2x3.ActorResource;
using Xbim.Ifc2x3.CostResource;
using Xbim.Ifc2x3.DateTimeResource;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.ExternalReferenceResource;
using Xbim.Ifc2x3.GeometricConstraintResource;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.PresentationOrganizationResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.ProfileResource;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.QuantityResource;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.TimeSeriesResource;
using Xbim.Ifc2x3.UtilityResource;
using Xbim.IO;
using Xbim.XbimExtensions.Interfaces;

// we need this to use extension methods in VS 2005


namespace HelloWall
{
    class Program 
    {
        /// <summary>
        /// This sample demonstrates the minimum steps to create a compliant IFC model that contains a single standard case wall
        /// </summary>
        static int Main()
        {
            //first create and initialise a model called Hello Wall
            Console.WriteLine("Initialising the IFC Project....");
            XbimModel model = CreateandInitModel("HelloWall");
            if (model != null)
            {
                IfcBuilding building = CreateBuilding(model, "Default Building");


                IfcWallStandardCase wall = CreateWall(model, 4000, 300, 2400);
                if (wall != null) AddPropertiesToWall(model, wall);
                using (XbimReadWriteTransaction txn = model.BeginTransaction("Add Wall"))
                {
                    building.AddElement(wall);
                    txn.Commit();
                }

                if (wall != null)
                {
                    try
                    {
                        Console.WriteLine("Standard Wall successfully created....");
                        //write the Ifc File
                        model.SaveAs("HelloWall.ifc",XbimStorageType.IFC);
                        Console.WriteLine("HelloWall.ifc has been successfully written");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to save HelloWall.ifc");
                        Console.WriteLine(e.Message);
                    }
                }
            }
            else
            {
                Console.WriteLine("Failed to initialise the model");
            }

            Console.WriteLine("Press any key to exit to view the IFC file....");
            Console.ReadKey();
            LaunchNotepad("HelloWall.ifc");
            return 0;
        }

        private static void LaunchNotepad(string fileName)
        {
            Process p = null;
            try
            {
       
                p = new Process {StartInfo = {FileName = fileName, CreateNoWindow = false}};
                p.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Occurred :{0},{1}",
                          ex.Message, ex.StackTrace);
            }
        }

        static private IfcBuilding CreateBuilding(XbimModel model, string name)
        {
            using (XbimReadWriteTransaction txn = model.BeginTransaction("Create Building"))
            {
                var building = model.Instances.New<IfcBuilding>();
                building.Name = name;
                building.OwnerHistory.OwningUser = model.DefaultOwningUser;
                building.OwnerHistory.OwningApplication = model.DefaultOwningApplication;
                //building.ElevationOfRefHeight = elevHeight;
                building.CompositionType = IfcElementCompositionEnum.ELEMENT;

                building.ObjectPlacement = model.Instances.New<IfcLocalPlacement>();
                var localPlacement = building.ObjectPlacement as IfcLocalPlacement;

                if (localPlacement != null && localPlacement.RelativePlacement == null)
                {

                    localPlacement.RelativePlacement = model.Instances.New<IfcAxis2Placement3D>();
                    var placement = localPlacement.RelativePlacement as IfcAxis2Placement3D;
                    placement.SetNewLocation(0.0, 0.0, 0.0);
                }

                model.IfcProject.AddBuilding(building);
                //validate and commit changes
                if (model.Validate(txn.Modified(), Console.Out) == 0)
                {
                    txn.Commit();
                    return building;
                }
                
            }
            return null;
        }
        


        /// <summary>
        /// Sets up the basic parameters any model must provide, units, ownership etc
        /// </summary>
        /// <param name="projectName">Name of the project</param>
        /// <returns></returns>
        static private XbimModel CreateandInitModel(string projectName)
        {
            XbimModel model = XbimModel.CreateModel(projectName + ".xBIM"); //create an empty model
            
            //Begin a transaction as all changes to a model are transacted
            using (XbimReadWriteTransaction txn = model.BeginTransaction("Initialise Model"))
            {
                //do once only initialisation of model application and editor values
                model.DefaultOwningUser.ThePerson.GivenName = "John";
                model.DefaultOwningUser.ThePerson.FamilyName = "Bloggs";
                model.DefaultOwningUser.TheOrganization.Name = "Department of Building";
                model.DefaultOwningApplication.ApplicationIdentifier = "Construction Software inc.";
                model.DefaultOwningApplication.ApplicationDeveloper.Name = "Construction Programmers Ltd.";
                model.DefaultOwningApplication.ApplicationFullName = "Ifc sample programme";
                model.DefaultOwningApplication.Version = "2.0.1";

                //set up a project and initialise the defaults

                var project = model.Instances.New<IfcProject>();
                project.Initialize(ProjectUnits.SIUnitsUK);
                project.Name = "testProject";
                project.OwnerHistory.OwningUser = model.DefaultOwningUser;
                project.OwnerHistory.OwningApplication = model.DefaultOwningApplication;

                //validate and commit changes
                if (model.Validate(txn.Modified(), Console.Out) == 0)
                {
                    txn.Commit();
                    return model;
                }
            }
            return null; //failed so return nothing

        }
        /// <summary>
        /// This creates a wall and it's geometry, many geometric representations are possible and extruded rectangular footprint is chosen as this is commonly used for standard case walls
        /// </summary>
        /// <param name="model"></param>
        /// <param name="length">Length of the rectangular footprint</param>
        /// <param name="width">Width of the rectangular footprint (width of the wall)</param>
        /// <param name="height">Height to extrude the wall, extrusion is vertical</param>
        /// <returns></returns>
        static private IfcWallStandardCase CreateWall(XbimModel model, double length, double width, double height)
        {
            //
            //begin a transaction
            using (XbimReadWriteTransaction txn = model.BeginTransaction("Create Wall"))
            {
                var wall = model.Instances.New<IfcWallStandardCase>();
                wall.Name = "A Standard rectangular wall";

                // required parameters for IfcWall
                wall.OwnerHistory.OwningUser = model.DefaultOwningUser;
                wall.OwnerHistory.OwningApplication = model.DefaultOwningApplication;

                //represent wall as a rectangular profile
                var rectProf = model.Instances.New<IfcRectangleProfileDef>();
                rectProf.ProfileType = IfcProfileTypeEnum.AREA;
                rectProf.XDim = width;
                rectProf.YDim = length;

                var insertPoint = model.Instances.New<IfcCartesianPoint>();
                insertPoint.SetXY(0, 400); //insert at arbitrary position
                rectProf.Position = model.Instances.New<IfcAxis2Placement2D>();
                rectProf.Position.Location = insertPoint;

                //model as a swept area solid
                var body = model.Instances.New<IfcExtrudedAreaSolid>();
                body.Depth = height;
                body.SweptArea = rectProf;
                body.ExtrudedDirection = model.Instances.New<IfcDirection>();
                body.ExtrudedDirection.SetXYZ(0, 0, 1);

                //parameters to insert the geometry in the model
                var origin = model.Instances.New<IfcCartesianPoint>();
                origin.SetXYZ(0, 0, 0);
                body.Position = model.Instances.New<IfcAxis2Placement3D>();
                body.Position.Location = origin;
             
                //Create a Definition shape to hold the geometry
                var shape = model.Instances.New<IfcShapeRepresentation>();
                shape.ContextOfItems = model.IfcProject.ModelContext();
                shape.RepresentationType = "SweptSolid";
                shape.RepresentationIdentifier = "Body";
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                var rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add(shape);                
                wall.Representation = rep;

                //now place the wall into the model
                var lp = model.Instances.New<IfcLocalPlacement>();
                var ax3D = model.Instances.New<IfcAxis2Placement3D>();
                ax3D.Location = origin;
                ax3D.RefDirection = model.Instances.New<IfcDirection>();
                ax3D.RefDirection.SetXYZ(0, 1, 0);
                ax3D.Axis = model.Instances.New<IfcDirection>();
                ax3D.Axis.SetXYZ(0, 0, 1);
                lp.RelativePlacement = ax3D;
                wall.ObjectPlacement = lp;


                // Where Clause: The IfcWallStandard relies on the provision of an IfcMaterialLayerSetUsage 
                var ifcMaterialLayerSetUsage = model.Instances.New<IfcMaterialLayerSetUsage>();
                var ifcMaterialLayerSet = model.Instances.New<IfcMaterialLayerSet>();
                var ifcMaterialLayer = model.Instances.New<IfcMaterialLayer>();
                ifcMaterialLayer.LayerThickness = 10;
                ifcMaterialLayerSet.MaterialLayers.Add(ifcMaterialLayer);
                ifcMaterialLayerSetUsage.ForLayerSet = ifcMaterialLayerSet;
                ifcMaterialLayerSetUsage.LayerSetDirection = IfcLayerSetDirectionEnum.AXIS2;
                ifcMaterialLayerSetUsage.DirectionSense = IfcDirectionSenseEnum.NEGATIVE;
                ifcMaterialLayerSetUsage.OffsetFromReferenceLine = 150;
                
                // Add material to wall
                var material = model.Instances.New<IfcMaterial>();
                material.Name = "some material";
                var ifcRelAssociatesMaterial = model.Instances.New<IfcRelAssociatesMaterial>();
                ifcRelAssociatesMaterial.RelatingMaterial = material;
                ifcRelAssociatesMaterial.RelatedObjects.Add(wall);

                ifcRelAssociatesMaterial.RelatingMaterial = ifcMaterialLayerSetUsage;

                // IfcPresentationLayerAssignment is required for CAD presentation in IfcWall or IfcWallStandardCase
                var ifcPresentationLayerAssignment = model.Instances.New<IfcPresentationLayerAssignment>();
                ifcPresentationLayerAssignment.Name = "some ifcPresentationLayerAssignment";
                ifcPresentationLayerAssignment.AssignedItems.Add(shape);


                // linear segment as IfcPolyline with two points is required for IfcWall
                var ifcPolyline = model.Instances.New<IfcPolyline>();
                var startPoint = model.Instances.New<IfcCartesianPoint>();
                startPoint.SetXY(0, 0);
                var endPoint = model.Instances.New<IfcCartesianPoint>();
                endPoint.SetXY(4000, 0);
                ifcPolyline.Points.Add(startPoint);
                ifcPolyline.Points.Add(endPoint);

                var shape2D = model.Instances.New<IfcShapeRepresentation>();
                shape2D.ContextOfItems = model.IfcProject.ModelContext();
                shape2D.RepresentationIdentifier = "Axis";
                shape2D.RepresentationType = "Curve2D";
                shape2D.Items.Add(ifcPolyline);
                rep.Representations.Add(shape2D);
                

                //validate write any errors to the console and commit if ok, otherwise abort
     
                if  (model.Validate(txn.Modified(),Console.Out)==0)
                {
                    txn.Commit();
                    return wall;
                }
            }
            return null;
        }

        /// <summary>
        /// Add some properties to the wall,
        /// </summary>
        /// <param name="model">XbimModel</param>
        /// <param name="wall"></param>
        static private void AddPropertiesToWall(XbimModel model, IfcWallStandardCase wall)
        {
            using (XbimReadWriteTransaction txn = model.BeginTransaction("Create Wall"))
            {
                IfcOwnerHistory ifcOwnerHistory = model.IfcProject.OwnerHistory; //we just use the project owner history for the properties, saves creating one
                CreateElementQuantity(model, wall, ifcOwnerHistory);
                CreateSimpleProperty(model, wall, ifcOwnerHistory);

                //if (model.Validate(txn.Modified(), Console.Out) == 0)
                //{
                    txn.Commit();
                //}   
            }
        }

        private static void CreateSimpleProperty(XbimModel model, IfcWallStandardCase wall, IfcOwnerHistory ifcOwnerHistory)
        {
            var ifcPropertySingleValue = model.Instances.New<IfcPropertySingleValue>(psv =>
            {
                psv.Name = "IfcPropertySingleValue:Time";
                psv.Description = "";
                psv.NominalValue = new IfcTimeMeasure(150.0);
                psv.Unit = model.Instances.New<IfcSIUnit>(siu =>
                {
                    siu.UnitType = IfcUnitEnum.TIMEUNIT;
                    siu.Name = IfcSIUnitName.SECOND;
                    siu.Dimensions = model.Instances.New<IfcDimensionalExponents>(de =>
                    {
                        de.LengthExponent = 0;
                        de.MassExponent = 0;
                        de.TimeExponent = 1;
                        de.ElectricCurrentExponent = 0;
                        de.ThermodynamicTemperatureExponent = 0;
                        de.AmountOfSubstanceExponent = 0;
                        de.LuminousIntensityExponent = 0;
                    });
                });
            });
            var ifcPropertyEnumeratedValue = model.Instances.New<IfcPropertyEnumeratedValue>(pev =>
            {
                pev.Name = "IfcPropertyEnumeratedValue:Music";
                pev.EnumerationReference = model.Instances.New<IfcPropertyEnumeration>(pe =>
                    {
                        pe.Name = "Notes";
                        pe.EnumerationValues.Add(new IfcLabel("Do"));
                        pe.EnumerationValues.Add(new IfcLabel("Re"));
                        pe.EnumerationValues.Add(new IfcLabel("Mi"));
                        pe.EnumerationValues.Add(new IfcLabel("Fa"));
                        pe.EnumerationValues.Add(new IfcLabel("So"));
                        pe.EnumerationValues.Add(new IfcLabel("La"));
                        pe.EnumerationValues.Add(new IfcLabel("Ti"));
                    });
                pev.EnumerationValues.Add(new IfcLabel("Do"));
                pev.EnumerationValues.Add(new IfcLabel("Re"));
                pev.EnumerationValues.Add(new IfcLabel("Mi"));

            });
            var ifcPropertyBoundedValue = model.Instances.New<IfcPropertyBoundedValue>(pbv => 
            {
                pbv.Name = "IfcPropertyBoundedValue:Mass";
                pbv.Description = "";
                pbv.UpperBoundValue = new IfcMassMeasure(5000.0);
                pbv.LowerBoundValue = new IfcMassMeasure(1000.0);
                pbv.Unit = model.Instances.New<IfcSIUnit>(siu =>
                {
                    siu.UnitType = IfcUnitEnum.MASSUNIT;
                    siu.Name = IfcSIUnitName.GRAM;
                    siu.Prefix = IfcSIPrefix.KILO;
                    siu.Dimensions = model.Instances.New<IfcDimensionalExponents>(de =>
                    {
                        de.LengthExponent = 0;
                        de.MassExponent = 1;
                        de.TimeExponent = 0;
                        de.ElectricCurrentExponent = 0;
                        de.ThermodynamicTemperatureExponent = 0;
                        de.AmountOfSubstanceExponent = 0;
                        de.LuminousIntensityExponent = 0;
                    });
                });
            });

            var definingValues = new List<IfcReal> { new IfcReal(100.0), new IfcReal(200.0), new IfcReal(400.0), new IfcReal(800.0), new IfcReal(1600.0), new IfcReal(3200.0), };
            var definedValues = new List<IfcReal> { new IfcReal(20.0), new IfcReal(42.0), new IfcReal(46.0), new IfcReal(56.0), new IfcReal(60.0), new IfcReal(65.0), };
            var ifcPropertyTableValue = model.Instances.New<IfcPropertyTableValue>(ptv =>
            {
                ptv.Name = "IfcPropertyTableValue:Sound";
                foreach (var item in definingValues)
	            {
                    ptv.DefiningValues.Add(item);
	            }
                foreach (var item in definedValues)
                {
                    ptv.DefinedValues.Add(item);
                }
                ptv.DefinedUnit = model.Instances.New<IfcContextDependentUnit>(cd =>
                {
                    cd.Dimensions = model.Instances.New<IfcDimensionalExponents>(de =>
                    {
                        de.LengthExponent = 0;
                        de.MassExponent = 0;
                        de.TimeExponent = 0;
                        de.ElectricCurrentExponent = 0;
                        de.ThermodynamicTemperatureExponent = 0;
                        de.AmountOfSubstanceExponent = 0;
                        de.LuminousIntensityExponent = 0;
                    });
                    cd.UnitType = IfcUnitEnum.FREQUENCYUNIT;
                    cd.Name = "dB";
                });
                
                
            });

            var listValues = new List<IfcLabel> { new IfcLabel("Red"), new IfcLabel("Green"), new IfcLabel("Blue"), new IfcLabel("Pink"), new IfcLabel("White"), new IfcLabel("Black"), };
            var ifcPropertyListValue = model.Instances.New<IfcPropertyListValue>(plv =>
            {
                plv.Name = "IfcPropertyListValue:Colours";
                foreach (var item in listValues)
                {
                    plv.ListValues.Add(item);
                }
            });

            var ifcMaterial = model.Instances.New<IfcMaterial>(m =>
            {
                m.Name = "Brick";
            });
            var ifcPrValueMaterial = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Material";
                prv.PropertyReference = ifcMaterial;
            });

            var ifcPrValuePerson = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Person";
                prv.PropertyReference = ifcOwnerHistory.OwningUser.ThePerson;
            });

            var ifcDateAndTime = model.Instances.New<IfcDateAndTime>(dt =>
            {
                dt.DateComponent = model.Instances.New<IfcCalendarDate>(cd =>
                    {
                        cd.DayComponent = 25;
                        cd.MonthComponent = 3;
                        cd.YearComponent = 2013;
                    });
                dt.TimeComponent = model.Instances.New<IfcLocalTime>(lt =>
                    {
                        lt.HourComponent = 10;
                        lt.MinuteComponent = 30;
                        lt.SecondComponent = 0;
                    });
            });
            var ifcPrValueDateTime = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:DateAndTime";
                prv.PropertyReference = ifcDateAndTime;
            });

            var ifcMaterialList = model.Instances.New<IfcMaterialList>(ml =>
                {
                    ml.Materials.Add(ifcMaterial);
                    ml.Materials.Add(model.Instances.New<IfcMaterial>(m =>{m.Name = "Cavity";}));
                    ml.Materials.Add(model.Instances.New<IfcMaterial>(m => { m.Name = "Block"; }));
                });
            var ifcPrValueMatList = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:MaterialList";
                prv.PropertyReference = ifcMaterialList;
            });

            var ifcPrValueOrg = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Organization";
                prv.PropertyReference = ifcOwnerHistory.OwningUser.TheOrganization;
            });

            var ifcPrValueDate = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Date";
                prv.PropertyReference = ifcDateAndTime.DateComponent;
            });

            var ifcPrValueTime = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Time";
                prv.PropertyReference = ifcDateAndTime.TimeComponent;
            });

            var ifcPrValuePersonOrg = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:PersonOrganization";
                prv.PropertyReference = ifcOwnerHistory.OwningUser;
            });

            var ifcMaterialLayer = model.Instances.New<IfcMaterialLayer>(ml =>
            {
                ml.Material = ifcMaterial;
                ml.LayerThickness = 100.0;
            });
            var ifcPrValueMatLayer = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:MaterialLayer";
                prv.PropertyReference = ifcMaterialLayer;
            });

            var ifcDocumentReference = model.Instances.New<IfcDocumentReference>(dr =>
            {
                dr.Name = "Document";
                dr.Location = "c://Documents//TheDoc.Txt";
            });
            var ifcPrValueRef = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Document";
                prv.PropertyReference = ifcDocumentReference;
            });

            var ifcTimeSeries = model.Instances.New<IfcRegularTimeSeries>(ts =>
            {
                ts.Name = "Regular Time Series";
                ts.Description = "Time series of events";
                ts.StartTime = model.Instances.New<IfcCalendarDate>(cd =>
                {
                    cd.DayComponent = 01;
                    cd.MonthComponent = 1;
                    cd.YearComponent = 2013;
                }); 
                ts.EndTime = model.Instances.New<IfcCalendarDate>(cd =>
                {
                    cd.DayComponent = 01;
                    cd.MonthComponent = 3;
                    cd.YearComponent = 2013;
                });
                ts.TimeSeriesDataType = IfcTimeSeriesDataTypeEnum.CONTINUOUS;
                ts.DataOrigin = IfcDataOriginEnum.MEASURED;
                ts.TimeStep = 604800; //7 days in secs
            });

            var ifcPrValueTimeSeries = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:TimeSeries";
                prv.PropertyReference = ifcTimeSeries;
            });

            var ifcAddress = model.Instances.New<IfcPostalAddress>(a =>
            {
                a.InternalLocation = "Room 101";
                a.SetAddressLines(new[] { "12 New road", "DoxField" });
                a.Town = "Sunderland";
                a.PostalCode = "DL01 6SX";
            });
            var ifcPrValueAddress = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Address";
                prv.PropertyReference = ifcAddress;
            });
            var ifcTelecomAddress = model.Instances.New<IfcTelecomAddress>(a =>
            {
                a.SetTelephoneNumbers(new[]{"01325 6589965"});
                a.SetElectronicMailAddress(new[]{"bob@bobsworks.com"});
            });
            var ifcPrValueTelecom = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Telecom";
                prv.PropertyReference = ifcTelecomAddress;
            });

            var ifcCostValue = model.Instances.New<IfcCostValue>(cv =>
            {
                cv.Name = "Cost Value";
                cv.Description = "";
                cv.Value = new IfcMonetaryMeasure(155.0);
                cv.ApplicableDate = model.Instances.New<IfcCalendarDate>(cd =>
                {
                    cd.DayComponent = 02;
                    cd.MonthComponent = 02;
                    cd.YearComponent = 2013;
                });
                cv.FixedUntilDate = model.Instances.New<IfcCalendarDate>(cd =>
                {
                    cd.DayComponent = 31;
                    cd.MonthComponent = 12;
                    cd.YearComponent = 2013;
                });
                cv.CostType = "Annual rate of return";
                cv.Condition = "";
            });
            var ifcPrValueCostValue = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:CostValue";
                prv.PropertyReference = ifcCostValue;
            });


            var ifcEnvironmentalImpactValue = model.Instances.New<IfcEnvironmentalImpactValue>(cv =>
            {
                cv.Name = "Environmental Impact";
                cv.Description = "";
                cv.Value = model.Instances.New<IfcMeasureWithUnit>(mwu =>
                    {
                        mwu.ValueComponent = new IfcReal(111.0);
                        mwu.UnitComponent = model.Instances.New<IfcSIUnit>(siu =>
                        {
                            siu.UnitType = IfcUnitEnum.LENGTHUNIT;
                            siu.Name = IfcSIUnitName.METRE;
                            siu.Dimensions = model.Instances.New<IfcDimensionalExponents>(de =>
                            {
                                de.LengthExponent = 1;
                                de.MassExponent = 0;
                                de.TimeExponent = 0;
                                de.ElectricCurrentExponent = 0;
                                de.ThermodynamicTemperatureExponent = 0;
                                de.AmountOfSubstanceExponent = 0;
                                de.LuminousIntensityExponent = 0;
                            });
                        });
                    });
                cv.ApplicableDate = model.Instances.New<IfcCalendarDate>(cd =>
                {
                    cd.DayComponent = 02;
                    cd.MonthComponent = 02;
                    cd.YearComponent = 2013;
                });
                cv.FixedUntilDate = model.Instances.New<IfcCalendarDate>(cd =>
                {
                    cd.DayComponent = 31;
                    cd.MonthComponent = 12;
                    cd.YearComponent = 2013;
                });
                cv.ImpactType = "Embodied energy";
                cv.Category = IfcEnvironmentalImpactCategoryEnum.MANUFACTURE;
            });
            var ifcPrValueEnvironmentalImpact = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:EnvironmentalImpact";
                prv.PropertyReference = ifcEnvironmentalImpactValue;
            });

            //lets create the IfcElementQuantity
            var ifcPropertySet = model.Instances.New<IfcPropertySet>(ps =>
            {
                ps.OwnerHistory = ifcOwnerHistory;
                ps.Name = "Test:IfcPropertySet";
                ps.Description = "Property Set";
                ps.HasProperties.Add(ifcPropertySingleValue);
                ps.HasProperties.Add(ifcPropertyEnumeratedValue);
                ps.HasProperties.Add(ifcPropertyBoundedValue);
                ps.HasProperties.Add(ifcPropertyTableValue);
                ps.HasProperties.Add(ifcPropertyListValue);
                ps.HasProperties.Add(ifcPrValueMaterial);
                ps.HasProperties.Add(ifcPrValuePerson);
                ps.HasProperties.Add(ifcPrValueDateTime);
                ps.HasProperties.Add(ifcPrValueMatList);
                ps.HasProperties.Add(ifcPrValueOrg);
                ps.HasProperties.Add(ifcPrValueDate);
                ps.HasProperties.Add(ifcPrValueTime);
                ps.HasProperties.Add(ifcPrValuePersonOrg);
                ps.HasProperties.Add(ifcPrValueMatLayer);
                ps.HasProperties.Add(ifcPrValueRef);
                ps.HasProperties.Add(ifcPrValueTimeSeries);
                ps.HasProperties.Add(ifcPrValueAddress);
                ps.HasProperties.Add(ifcPrValueTelecom);
                ps.HasProperties.Add(ifcPrValueCostValue);
                ps.HasProperties.Add(ifcPrValueEnvironmentalImpact);
                
            });

            //need to create the relationship
            model.Instances.New<IfcRelDefinesByProperties>(rdbp =>
            {
                rdbp.OwnerHistory = ifcOwnerHistory;
                rdbp.Name = "Property Association";
                rdbp.Description = "IfcPropertySet associated to wall";
                rdbp.RelatedObjects.Add(wall);
                rdbp.RelatingPropertyDefinition = ifcPropertySet;
            });
        }

        private static void CreateElementQuantity(XbimModel model, IfcWallStandardCase wall, IfcOwnerHistory ifcOwnerHistory)
        {
            //Create a IfcElementQuantity
            //first we need a IfcPhysicalSimpleQuantity,first will use IfcQuantityArea
            var ifcQuantityArea = model.Instances.New<IfcQuantityArea>(qa =>
            {
                qa.Name = "IfcQuantityArea:Area";
                qa.Description = "";
                qa.Unit = model.Instances.New<IfcSIUnit>(siu =>
                {
                    siu.UnitType = IfcUnitEnum.AREAUNIT;
                    siu.Prefix = IfcSIPrefix.MILLI;
                    siu.Name = IfcSIUnitName.SQUARE_METRE;
                    siu.Dimensions = model.Instances.New<IfcDimensionalExponents>(de =>
                    {
                        de.LengthExponent = 1;
                        de.MassExponent = 0;
                        de.TimeExponent = 0;
                        de.ElectricCurrentExponent = 0;
                        de.ThermodynamicTemperatureExponent = 0;
                        de.AmountOfSubstanceExponent = 0;
                        de.LuminousIntensityExponent = 0;
                    });

                });
                qa.AreaValue = 100.0;

            });
            //next quantity IfcQuantityCount using IfcContextDependentUnit
            var ifcContextDependentUnit = model.Instances.New<IfcContextDependentUnit>(cd =>
                {
                    cd.Dimensions = model.Instances.New<IfcDimensionalExponents>(de =>
                        {
                            de.LengthExponent = 1;
                            de.MassExponent = 0;
                            de.TimeExponent = 0;
                            de.ElectricCurrentExponent = 0;
                            de.ThermodynamicTemperatureExponent = 0;
                            de.AmountOfSubstanceExponent = 0;
                            de.LuminousIntensityExponent = 0;
                        });
                    cd.UnitType = IfcUnitEnum.LENGTHUNIT;
                    cd.Name = "Elephants";
                });
                var ifcQuantityCount = model.Instances.New<IfcQuantityCount>(qc =>
                {
                    qc.Name = "IfcQuantityCount:Elephant";
                    qc.CountValue = 12;
                    qc.Unit = ifcContextDependentUnit;
                });


             //next quantity IfcQuantityLength using IfcConversionBasedUnit
            var ifcConversionBasedUnit = model.Instances.New<IfcConversionBasedUnit>(cbu =>
            {
                cbu.ConversionFactor = model.Instances.New<IfcMeasureWithUnit>(mu =>
                {
                    mu.ValueComponent = new IfcRatioMeasure(25.4);
                    mu.UnitComponent = model.Instances.New<IfcSIUnit>(siu =>
                    {
                        siu.UnitType = IfcUnitEnum.LENGTHUNIT;
                        siu.Prefix = IfcSIPrefix.MILLI;
                        siu.Name = IfcSIUnitName.METRE;
                    });
                    
                });
                cbu.Dimensions = model.Instances.New<IfcDimensionalExponents>(de =>
                {
                    de.LengthExponent = 1;
                    de.MassExponent = 0;
                    de.TimeExponent = 0;
                    de.ElectricCurrentExponent = 0;
                    de.ThermodynamicTemperatureExponent = 0;
                    de.AmountOfSubstanceExponent = 0;
                    de.LuminousIntensityExponent = 0;
                });
                cbu.UnitType = IfcUnitEnum.LENGTHUNIT;
                cbu.Name = "Inch";
            });
            var ifcQuantityLength = model.Instances.New<IfcQuantityLength>(qa =>
            {
                qa.Name = "IfcQuantityLength:Length";
                qa.Description = "";
                qa.Unit = ifcConversionBasedUnit;
                qa.LengthValue = 24.0;
            });

            //lets create the IfcElementQuantity
            var ifcElementQuantity = model.Instances.New<IfcElementQuantity>(eq =>
            {
                eq.OwnerHistory = ifcOwnerHistory;
                eq.Name = "Test:IfcElementQuantity";
                eq.Description = "Measurement quantity";
                eq.Quantities.Add(ifcQuantityArea);
                eq.Quantities.Add(ifcQuantityCount);
                eq.Quantities.Add(ifcQuantityLength);
            });

            //need to create the relationship
            model.Instances.New<IfcRelDefinesByProperties>(rdbp =>
            {
                rdbp.OwnerHistory = ifcOwnerHistory;
                rdbp.Name = "Area Association";
                rdbp.Description = "IfcElementQuantity associated to wall";
                rdbp.RelatedObjects.Add(wall);
                rdbp.RelatingPropertyDefinition = ifcElementQuantity;
            });
        }

        

    }
}
