using System;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace BasicExamples
{
    public class FederationExample
    {
        public static void CreateFederation()
        {
            var editor = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "You",
                ApplicationFullName = "Your app",
                ApplicationIdentifier = "Your app ID",
                ApplicationVersion = "4.0",
                //your user
                EditorsFamilyName = "Santini Aichel",
                EditorsGivenName = "Johann Blasius",
                EditorsOrganisationName = "Independent Architecture"
            };
            using (var federation = IfcStore.Create(editor, IfcSchemaVersion.Ifc4, XbimStoreType.InMemoryModel))
            {

                federation.AddModelReference("SampleHouse.ifc", "Bob The Builder", "Original Constructor"); //IFC4
                federation.AddModelReference("SampleHouseExtension.ifc", "Tyna", "Extensions Builder"); //IFC2x3

                Console.WriteLine($"Model is federation: {federation.IsFederation}");
                Console.WriteLine($"Number of overall entities: {federation.FederatedInstances.Count}");
                Console.WriteLine($"Number of walls: {federation.FederatedInstances.CountOf<IIfcWall>()}");
                foreach (var refModel in federation.ReferencedModels)
                {
                    Console.WriteLine();
                    Console.WriteLine($"    Referenced model: {refModel.Name}");
                    Console.WriteLine($"    Referenced model organization: {refModel.OwningOrganisation}");
                    Console.WriteLine($"    Number of walls: {refModel.Model.Instances.CountOf<IIfcWall>()}");

                }

                //you can save information about the federation for a future use
                federation.SaveAs("federation.ifc");
            }
        }
    }
}
