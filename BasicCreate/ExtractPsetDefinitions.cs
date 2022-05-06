using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Ifc;
using Xbim.Ifc4.Kernel;

namespace BasicExamples
{
    class ExtractPsetDefinitions
    {
        public static void Run()
        {
            using (var model = IfcStore.Open("IFC4_ADD2_Properties.ifc"))
            using (var cache = model.BeginInverseCaching())
            {
                var records = model.Instances.OfType<IfcPropertySetTemplate>()
                    .SelectMany(pst => pst.HasPropertyTemplates.Select(pt => new Record
                    {
                        SetName = pst.Name,
                        ApplicableClass = pst.ApplicableEntity,
                        Name = pt.Name,
                        Description = pt.Description
                    }));

                using (var w = File.CreateText("properties.csv"))
                using (var csv = new CsvWriter(w, new CsvConfiguration (CultureInfo.InvariantCulture)))
                {
                    csv.WriteRecords(records);
                }

            }
        }

        private class Record
        {
            public string SetName { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string ApplicableClass { get; set; }
        }
    }
}
