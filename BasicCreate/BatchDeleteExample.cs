using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.Common.Model;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace BasicExamples
{
    class BatchDeleteExample
    {
        public static void Run()
        {
            var sizes = new[] { 1, 1, 5, 10, 20, 50 };
            using (var source = IfcStore.Open("SampleHouse.ifc"))
            {
                foreach (var size in sizes)
                {
                    using (var model = GetModel(source, size))
                    {
                        var products = model.Instances.OfType<IIfcProduct>().ToList();
                        Console.WriteLine($"{products.Count} products to delete");
                        var w = Stopwatch.StartNew();
                        using (var txn = model.BeginTransaction("Delete"))
                        {
                            foreach (var product in products)
                                model.Delete(product);
                            txn.Commit();
                        }
                        w.Stop();
                        Console.WriteLine($"{w.ElapsedMilliseconds}ms to delete one by one.");
                    }

                    using (var model = GetModel(source, size))
                    {
                        var w = Stopwatch.StartNew();
                        var products = model.Instances.OfType<IIfcProduct>().ToArray();
                        model.Delete(products, true);
                        w.Stop();
                        Console.WriteLine($"{w.ElapsedMilliseconds}ms to delete in batch.");
                    }
                }
            }
        }

        private static StepModel GetModel(IModel original, int size)
        {
            var model = new StepModel(new Xbim.Ifc4.EntityFactoryIfc4());
            // create model with increasing size
            Enumerable.Range(0, size).ToList().ForEach(_ =>
            {
                var map = new XbimInstanceHandleMap(original, model);
                foreach (var instance in original.Instances)
                {
                    model.InsertCopy(instance, map, null, includeInverses: false, keepLabels: false, noTransaction: true);
                }
            });

            return model;
        }
    }
}
