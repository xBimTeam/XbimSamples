using System;
using Xbim.Common;
using Xbim.Common.Delta;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.IO.Step21;

namespace BasicExamples
{
    public class ChangeLogExample
    {
        public static void CreateLog()
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

            using (var model = IfcStore.Open("SampleHouse.ifc", editor))
            {
                using (var txn = model.BeginTransaction("Modification"))
                {
                    using (var log = new TransactionLog(txn))
                    {
                        //change to existing wall
                        var wall = model.Instances.FirstOrDefault<IIfcWall>();
                        wall.Name = "Unexpected name";
                        wall.GlobalId = Guid.NewGuid().ToPart21();
                        wall.Description = "New and more descriptive description";

                        //print all changes caused by this
                        PrintChanges(log);
                        txn.Commit();
                    }
                    Console.WriteLine();
                }
            }
        }

        private static void PrintChanges(TransactionLog log)
        {
            foreach (var change in log.Changes)
            {
                switch (change.ChangeType)
                {
                    case ChangeType.New:
                        Console.WriteLine(@"New entity: {0}", change.CurrentEntity);
                        break;
                    case ChangeType.Deleted:
                        Console.WriteLine(@"Deleted entity: {0}", change.OriginalEntity);
                        break;
                    case ChangeType.Modified:
                        Console.WriteLine(@"Changed Entity: #{0}={1}", change.Entity.EntityLabel, change.Entity.ExpressType.ExpressNameUpper);
                        foreach (var prop in change.ChangedProperties)
                            Console.WriteLine(@"        Property '{0}' changed from {1} to {2}", prop.Name, prop.OriginalValue, prop.CurrentValue);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
