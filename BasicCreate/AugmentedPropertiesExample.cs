using System;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;

namespace BasicExamples
{
    public static class AugmentedPropertiesExample
    {
        private static readonly DataRecord[] data = new[] {
                new DataRecord (1752, 456.087),
                new DataRecord (2327, 123.456),
                new DataRecord (4880, 789.124)
            };


        public static void Run()
        {
            // you will use Steve's code to 'check-out' the model from the blob storage
            using (var model = IfcStore.Open("SampleHouse.ifc"))
            using (var txn = model.BeginTransaction("Extending with carbon data"))
            {
                var c = new Create(model);

                // this will assign mandatory feelds of IfcRoot objects
                model.EntityNew += OnEntityCreated;

                // create once, reference many times
                var kg = c.SIUnit(u =>
                {
                    u.Name = IfcSIUnitName.GRAM;
                    u.Prefix = IfcSIPrefix.KILO;
                    u.UnitType = IfcUnitEnum.MASSUNIT;
                });

                foreach (var record in data)
                {
                    if (!(model.Instances[record.EntityLabel] is IIfcObject product))
                    {
                        // TODO: handle mismatch in data
                        continue;
                    }

                    // create property set
                    var pset = c.PropertySet(ps => ps.Name = "FLEX_Carbon_assessment");

                    // add any number of properties
                    pset.HasProperties.Add(
                        c.PropertySingleValue(p =>
                        {
                            p.Name = "Embodied Carbon";
                            p.NominalValue = new IfcMassMeasure(record.EmbodiedCarbon);

                            // optional, if we know what it is
                            p.Unit = kg;

                            // fallback for numeric values of unknown type
                            // p.NominalValue = new IfcReal(record.EmbodiedCarbon); 
                        }));

                    // associate property set with the product
                    c.RelDefinesByProperties(r =>
                    {
                        r.RelatedObjects.Add(product);
                        r.RelatingPropertyDefinition = pset;
                    });
                }

                // commit changes or it will be reverted
                txn.Commit();
                model.EntityNew -= OnEntityCreated;
            }

            // TODO: check-in of the model back to blob storage
        }

        private static void OnEntityCreated(IPersistEntity entity)
        {
            if (!(entity is IIfcRoot root)) return;

            root.GlobalId = Guid.NewGuid();
            
            // TODO: assign owner history object representing xbim as a tool, current tenant as an organization and current user as the change author
        }

    }

    public class DataRecord
    {
        public int EntityLabel { get; set; }
        public double EmbodiedCarbon { get; set; }

        public DataRecord(int entityLabel, double embodiedCarbon)
        {
            EntityLabel = entityLabel;
            EmbodiedCarbon = embodiedCarbon;
        }
    }
}


