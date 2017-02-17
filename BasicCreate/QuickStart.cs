using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace BasicExamples
{
    class QuickStart
    {
        public static void Start()
        {
            const string fileName = "SampleHouse.ifc";
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
            using (var model = IfcStore.Open(fileName, editor))
            {
                using (var txn = model.BeginTransaction("Quick start transaction"))
                {
                    //get all walls in the model
                    var walls = model.Instances.OfType<IIfcWall>();

                    //iterate over all the walls and change them
                    foreach (var wall in walls)
                    {
                        wall.Name = "Iterated wall: " + wall.Name;
                    }

                    //commit your changes
                    txn.Commit();
                }

                //save your changed model
                model.SaveAs("SampleHouse_Modified.ifc");
            }
        }
    }
}
