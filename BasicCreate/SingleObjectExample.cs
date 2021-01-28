using System;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc2x3.ActorResource;
using Xbim.IO;

namespace BasicExamples
{
    class SingleObjectExample
    {
        public static void Run()
        {
            using (var model = IfcStore.Create(XbimSchemaVersion.Ifc2X3, XbimStoreType.InMemoryModel))
            {
                using (var txn = model.BeginTransaction())
                {
                    model.Instances.New<IfcPerson>(p =>
                    {
                        p.Addresses.Add(model.Instances.New<IfcTelecomAddress>(a =>
                        {
                            a.Purpose = IfcAddressTypeEnum.OFFICE;
                            a.Description = "Hlavni adresa";
                            a.ElectronicMailAddresses.Add("martin.cerny@xbim.cz");
                            a.FacsimileNumbers.Add("X456ER78");
                            a.TelephoneNumbers.Add("+420 721 910 190");
                            a.WWWHomePageURL = "https://www.xbim.cz";
                        }));
                        p.FamilyName = "Cerny";
                        p.GivenName = "Martin";
                        p.Id = Guid.NewGuid().ToString();
                        p.PrefixTitles.Add("Ing.");
                        p.SuffixTitles.Add("Ph.D.");
                    });
                    txn.Commit();
                }

                model.SaveAs("person_and_address.ifc");
                model.SaveAs("person_and_address.ifcXML");
            }
        }
    }
}
