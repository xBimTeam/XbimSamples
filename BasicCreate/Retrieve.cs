using System;
using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace BasicExamples
{
    public class RetrieveSample
    {
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
                var theDoor = model.Instances.FirstOrDefault<IIfcDoor>(d => d.GlobalId == id);
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
    }
}
