using System;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.IO;
// ReSharper disable All

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
            using (var federation = IfcStore.Create(editor, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel))
            {

                federation.AddModelReference("SampleHouse.ifc", "Bob The Builder", "Original Constructor"); //IFC4
                federation.AddModelReference("SampleHouseExtension.ifc", "Tyna", "Extensions Builder"); //IFC2x3

                Console.WriteLine(string.Format("Model is federation: {0}", federation.IsFederation));
                Console.WriteLine(string.Format("Number of overall entities: {0}", federation.FederatedInstances.Count));
                Console.WriteLine(string.Format("Number of walls: {0}",
                    federation.FederatedInstances.CountOf<IIfcWall>()));
                foreach (var refModel in federation.ReferencedModels)
                {
                    Console.WriteLine();
                    Console.WriteLine(string.Format("    Referenced model: {0}", refModel.Name));
                    Console.WriteLine(string.Format("    Referenced model organization: {0}",
                        refModel.OwningOrganisation));
                    Console.WriteLine(string.Format("    Number of walls: {0}",
                        refModel.Model.Instances.CountOf<IIfcWall>()));

                }

                //you can save information about the federation for a future use
                federation.SaveAs("federation.ifc");
            }
        }
    }
}
