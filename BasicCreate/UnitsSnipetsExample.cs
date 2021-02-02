using Xbim.Common;
using Xbim.Ifc4.MeasureResource;
using Xbim.IO.Memory;

namespace BasicExamples
{
    internal static class UnitsSnipetsExample
    {
        public static void Run()
        {
            using (var source = new MemoryModel(new Xbim.Ifc4.EntityFactoryIfc4()))
            {
                var i = source.Instances;
                IfcUnit unit;
                using (var txn = source.BeginTransaction("Creation"))
                {
                    unit = i.New<IfcDerivedUnit>(u =>
                    {
                        u.UnitType = Xbim.Ifc4.Interfaces.IfcDerivedUnitEnum.LINEARVELOCITYUNIT;
                        u.Elements.AddRange(new[] {
                        i.New<IfcDerivedUnitElement>(e => {
                            e.Exponent = 1;
                            e.Unit = i.New<IfcSIUnit>(m => {
                                m.UnitType = Xbim.Ifc4.Interfaces.IfcUnitEnum.LENGTHUNIT;
                                m.Name = Xbim.Ifc4.Interfaces.IfcSIUnitName.METRE;
                                m.Prefix = null;
                            });
                        }),
                        i.New<IfcDerivedUnitElement>(e => {
                            e.Exponent = -2;
                            e.Unit = i.New<IfcSIUnit>(s => {
                                s.UnitType = Xbim.Ifc4.Interfaces.IfcUnitEnum.TIMEUNIT;
                                s.Name = Xbim.Ifc4.Interfaces.IfcSIUnitName.SECOND;
                                s.Prefix = null;
                            });
                        })
                    });
                    });
                    txn.Commit();
                }

                using (var target = new MemoryModel(new Xbim.Ifc4.EntityFactoryIfc4()))
                {
                    // if you do more inserts, this makes sure that entities once copied over will be reused
                    var map = new XbimInstanceHandleMap(source, target);
                    using (var txn = target.BeginTransaction("Inserting unit"))
                    {
                        // inserts unit and all direct attributes, no inverse attributes, doesn't do any special transformations 
                        // and creates new local IDs (entity labels (#4564)) for inserted entities (this makes sure that it doesn't clash with the
                        // data already existing in the model)
                        target.InsertCopy(unit, map, null, false, false);
                        txn.Commit();
                    }
                }
            }
        }
    }
}
