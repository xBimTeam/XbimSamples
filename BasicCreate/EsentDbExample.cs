using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.IO.Esent;
using Xbim.IO.Memory;

namespace BasicExamples
{
    class EsentDbExample
    {
        public static void Run()
        {
            const string file = "SampleModel.ifc";
            const string db = "sample.xbim";
            var schema = MemoryModel.GetSchemaVersion(file);
            IEntityFactory factory = null;
            switch (schema)
            {
                case XbimSchemaVersion.Ifc4:
                    factory = new Xbim.Ifc4.EntityFactoryIfc4();
                    break;
                case XbimSchemaVersion.Ifc4x1:
                    factory = new Xbim.Ifc4.EntityFactoryIfc4x1();
                    break;
                case XbimSchemaVersion.Ifc2X3:
                    factory = new Xbim.Ifc2x3.EntityFactoryIfc2x3();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(schema));
            }
            using (var model = new EsentModel(factory))
            {
                model.CreateFrom(file, db);
                model.Close();
            }

            IfcStore.ModelProviderFactory.UseEsentModelProvider();
            using (var model = IfcStore.Open(db))
            {
                // ... do anything you need to do ...
            }
        }
    }
}
