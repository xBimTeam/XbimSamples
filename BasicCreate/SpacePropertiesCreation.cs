using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Ifc;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.PropertyResource;
using Xbim.IO;

namespace BasicExamples
{
    class SpacePropertiesCreation
    {
        public static void AttachSpaceProperties()
        {
            var editor = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "xbim developer",
                ApplicationFullName = "xbim toolkit",
                ApplicationIdentifier = "xbim",
                ApplicationVersion = "4.0",
                EditorsFamilyName = "Santini Aichel",
                EditorsGivenName = "Johann Blasius",
                EditorsOrganisationName = "Independent Architecture"
            };

            using (var model = IfcStore.Create(editor, Xbim.Common.Step21.XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel))
            {
                using (var cache = model.BeginInverseCaching())
                {
                    // do all analytics to keep fast search indices
                }

                var i = model.Instances;
                using (var txn = model.BeginTransaction("Spaces creation"))
                {
                    // create 100 testing spaces
                    var spaces = Enumerable.Range(1, 100)
                        .Select(n => i.New<IfcSpace>(s => s.Name = $"Space {n:D3}"))
                        .ToList();

                    var psetNames = Enumerable.Range(0, 100)
                        .Select(n => $"XBIM_Automated_Test_{n:D3}")
                        .ToList();
                    var pNames = Enumerable.Range(0, 100)
                        .Select(n => $"Property {n:D3}")
                        .ToList();
                    var pValues = Enumerable.Range(0, 100)
                        .Select(n => $"Value {n:D3}")
                        .ToList();
                    var pNameValue = pNames
                                .Zip(pValues, (n, v) => (new { name = n, value = v }))
                                .ToList();

                    // start measuring the time
                    var w = Stopwatch.StartNew();
                    foreach (var space in spaces)
                    {
                        foreach (var psetName in psetNames)
                        {
                            var properties = pNameValue
                                .Select(property => i.New<IfcPropertySingleValue>(p =>
                                {
                                    p.Name = property.name;
                                    p.NominalValue = new IfcLabel(property.value);
                                }))
                                .ToList();
                            i.New<IfcRelDefinesByProperties>(r =>
                            {
                                r.RelatedObjects.Add(space);
                                r.RelatingPropertyDefinition = i.New<IfcPropertySet>(ps =>
                                {
                                    ps.Name = psetName;
                                    ps.HasProperties.AddRange(properties);
                                });
                            });
                        }
                    }
                    w.Stop();
                    Console.WriteLine($"{spaces.Count * psetNames.Count} property sets and {spaces.Count * psetNames.Count * psetNames.Count} properties created in {w.ElapsedMilliseconds}ms");
                    txn.Commit();
                }

                var fileName = "spaces_with_properties.ifc";
                var sw = Stopwatch.StartNew();
                model.SaveAs(fileName);
                sw.Stop();

                var info = new FileInfo(fileName);
                Console.WriteLine($"Model saved in {sw.ElapsedMilliseconds}ms. File size {info.Length}B");
            }
        }
    }
}
