using BasicValidation;
using System;
using System.Linq;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.SharedBldgElements;

namespace BasicExamples
{
    public class Program
    {
        public static void Main()
        {
            var ex = new FederationExample();
            ex.CreateFederation();

            ChangeLogExample.CreateLog();

            Create();
            Retrieve();
            Update();
            Delete();
        }

        public static void Create()
        {
            var editor = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "xBIM Team",
                ApplicationFullName = "xBIM Toolkit",
                ApplicationIdentifier = "xBIM",
                ApplicationVersion = "4.0",
                EditorsFamilyName = "Santini Aichel",
                EditorsGivenName = "Johann Blasius",
                EditorsOrganisationName = "Independent Architecture"
            };
            using (var model = IfcStore.Create(editor, IfcSchemaVersion.Ifc4, XbimStoreType.InMemoryModel))
            {
                using (var txn = model.BeginTransaction("Hello Wall"))
                {
                    //there should always be one project in the model
                    var project = model.Instances.New<IfcProject>(p => p.Name = "Basic Creation");
                    //our shortcut to define basic default units
                    project.Initialize(ProjectUnits.SIUnitsUK);

                    //create simple object and use lambda initializer to set the name
                    var wall = model.Instances.New<IfcWall>(w => w.Name = "The very first wall");

                    //set a few basic properties
                    model.Instances.New<IfcRelDefinesByProperties>(rel => {
                        rel.RelatedObjects.Add(wall);
                        rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset => {
                            pset.Name = "Basic set of properties";
                            pset.HasProperties.AddRange(new[] {
                                model.Instances.New<IfcPropertySingleValue>(p => 
                                {
                                    p.Name = "Text property";
                                    p.NominalValue = new IfcText("Any arbitrary text you like");
                                }),
                                model.Instances.New<IfcPropertySingleValue>(p => 
                                {
                                    p.Name = "Length property";
                                    p.NominalValue = new IfcLengthMeasure(56.0);
                                }),
                                model.Instances.New<IfcPropertySingleValue>(p => 
                                {
                                    p.Name = "Number property";
                                    p.NominalValue = new IfcNumericMeasure(789.2);
                                }),
                                model.Instances.New<IfcPropertySingleValue>(p => 
                                {
                                    p.Name = "Logical property";
                                    p.NominalValue = new IfcLogical(true);
                                })
                            });
                        });
                    });

                    txn.Commit();
                }
                Validator.Validate(model);
                model.SaveAs("BasicWall.ifc");
            }
        }

        public static void Retrieve()
        {
            const string fileName = "SampleHouse.ifc";
            using (var model = IfcStore.Open(fileName))
            {
                //get all doors in the model (using IFC4 interface of IfcDoor this will work both for IFC2x3 and IFC4)
                var allDoors = model.Instances.OfType<IIfcDoor>();

                //get only doors with defined IIfcTypeObject
                var someDoors = model.Instances.Where<IIfcDoor>(d => d.IsTypedBy.Any());

                //get one single door 
                var id = "3cUkl32yn9qRSPvBJVyWYp";
                var theDoor = model.Instances.FirstOrDefault<IfcDoor>(d => d.GlobalId == id);
                Console.WriteLine($"Door ID: {theDoor.GlobalId}, Name: {theDoor.Name}");

                //get all single-value properties of the door
                var properties = theDoor.IsDefinedBy
                    .Where(r => r.RelatingPropertyDefinition is IIfcPropertySet)
                    .SelectMany(r => ((IIfcPropertySet)r.RelatingPropertyDefinition).HasProperties)
                    .OfType<IIfcPropertySingleValue>();
                foreach (var property in properties)
                    Console.WriteLine($"Property: {property.Name}, Value: {property.NominalValue}");
            }
        }

        public static void Update()
        {
            const string fileName = "SampleHouse.ifc";
            var editor = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "xBIM Team",
                ApplicationFullName = "xBIM Toolkit",
                ApplicationIdentifier = "xBIM",
                ApplicationVersion = "4.0",
                EditorsFamilyName = "Santini Aichel",
                EditorsGivenName = "Johann Blasius",
                EditorsOrganisationName = "Independent Architecture"
            };

            using (var model = IfcStore.Open(fileName, editor, true))
            {
                //get existing door from the model
                var id = "3cUkl32yn9qRSPvBJVyWYp";
                var theDoor = model.Instances.FirstOrDefault<IfcDoor>(d => d.GlobalId == id);

                //open transaction for changes
                using (var txn = model.BeginTransaction("Doors update"))
                {
                    //create new property set with two properties
                    var pSetRel = model.Instances.New<IfcRelDefinesByProperties>(r =>
                    {
                        r.GlobalId = Guid.NewGuid();
                        r.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pSet =>
                        {
                            pSet.Name = "New property set";
                            //all collections are always initialized
                            pSet.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                            {
                                p.Name = "First property";
                                p.NominalValue = new IfcLabel("First value");
                            }));
                            pSet.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                            {
                                p.Name = "Second property";
                                p.NominalValue = new IfcLengthMeasure(156.5);
                            }));
                        });
                    });

                    //change the name of the door
                    theDoor.Name += "_checked";
                    //add properties to the door
                    pSetRel.RelatedObjects.Add(theDoor);
                    Validator.Validate(model);

                    //commit changes
                    txn.Commit();
                }

            }
        }

        public static void Delete()
        {
            const string fileName = "SampleHouse.ifc";
            using (var model = IfcStore.Open(fileName))
            {
                //get existing door from the model
                var id = "3cUkl32yn9qRSPvBJVyWYp";
                var theDoor = model.Instances.FirstOrDefault<IIfcDoor>(d => d.GlobalId == id);

                //open transaction for changes
                using (var txn = model.BeginTransaction("Delete the door"))
                {
                    //delete the door
                    model.Delete(theDoor);
                    //commit changes
                    txn.Commit();
                }

            }

        }

        public static void CreateConnections()
        {
            var editor = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "Your Name",
                ApplicationFullName = "Your Application",
                ApplicationIdentifier = "ID of your application",
                ApplicationVersion = "0.1",
                EditorsFamilyName = "Santini Aichel",
                EditorsGivenName = "Johann Blasius",
                EditorsOrganisationName = "Independent Architecture"
            };
            using (var model = IfcStore.Create(editor, IfcSchemaVersion.Ifc4, XbimStoreType.InMemoryModel))
            {
                using (var txn = model.BeginTransaction("Creating connections"))
                {
                    var pointA = model.Instances.New<IfcCartesianPoint>(c => c.SetXYZ(123, 456, 789));
                    var pointB = model.Instances.New<IfcCartesianPoint>(c => c.SetXYZ(321, 654, 987));
                    var connection = model.Instances.New<IfcConnectionPointGeometry>(c =>
                    {
                        c.PointOnRelatedElement = pointA;
                        c.PointOnRelatingElement = pointB;
                    });
                    txn.Commit();
                }
                model.SaveAs("model.ifc");
            }
        }
    }
}
