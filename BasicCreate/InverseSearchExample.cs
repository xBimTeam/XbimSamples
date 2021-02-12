using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.Common.Model;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.SharedBldgElements;

namespace BasicExamples
{
    class InverseSearchExample
    {
        public static void Run()
        {
            using (var w = File.CreateText("InverseSearchResults.csv"))
            {
                // var objCounts = new[] { 100, 200, 500, 1000, 2000};
                var objCounts = new[] { 100, 200, 500, 1000};
                var header = new List<string>();
                foreach (var count in objCounts)
                {
                    using (var model = CreateModel(count, 50))
                    {
                        var stats = RunInverseSearch(model);
                        if (header.Count == 0)
                        { 
                            header = stats.Keys.ToList();
                            w.WriteLine($"count," + string.Join(",", header.Select(h => $"\"{h}\"")));
                        }

                        w.WriteLine($"{count},{string.Join(",", stats.Values)}");
                    }
                }
            }
        }

        private static IModel CreateModel(int numberOfObjects, int propertySetsPerObject)
        {
            var model = new StepModel(new Xbim.Ifc4.EntityFactoryIfc4());
            var i = model.Instances;
            using (var txn = model.BeginTransaction("Model creation"))
            {
                foreach (var objectNumber in Enumerable.Range(1, numberOfObjects))
                {
                    var o = i.New<IfcBuildingElementProxy>(e =>
                    {
                        e.GlobalId = Guid.NewGuid();
                        e.Name = $"Object #{objectNumber}";
                    });

                    foreach (var psetNumber in Enumerable.Range(1, propertySetsPerObject))
                    {
                        i.New<IfcRelDefinesByProperties>(r =>
                        {
                            r.GlobalId = Guid.NewGuid();
                            r.RelatedObjects.Add(o);
                            r.RelatingPropertyDefinition = i.New<IfcPropertySet>(ps =>
                            {
                                ps.Name = $"Property set #{psetNumber}";
                                ps.HasProperties.Add(i.New<IfcPropertySingleValue>(p =>
                                {
                                    p.Name = $"Property #{psetNumber}";
                                }));
                            });
                        });
                    }
                }
                txn.Commit();
            }
            return model;
        }

        private static Dictionary<string, long> RunInverseSearch(IModel model)
        {
            var results = new Dictionary<string, long>();

            int usingInverseAttributes()
            {
                var noObjects = 0;
                var noRelations = 0;
                foreach (var obj in model.Instances.OfType<IIfcObject>())
                {
                    var relCount = obj.IsDefinedBy.Count();
                    if (relCount > 0)
                    {
                        noObjects++;
                        noRelations += relCount;
                    }
                }
                return noObjects;
            }

            int notUsingInverseAttributes()
            {
                var result = new HashSet<int>();
                foreach (var rel in model.Instances.OfType<IIfcRelDefinesByProperties>())
                {
                    foreach (var obj in rel.RelatedObjects.OfType<IIfcObject>())
                    {
                        result.Add(obj.EntityLabel);
                    }
                }
                return result.Count;
            }

            var w = new Stopwatch();
            using (var cache = model.BeginInverseCaching())
            {
                w.Start();
                var c = usingInverseAttributes();
                w.Stop();
                results.Add("WITH inverse caching, USING inverse attributes, cache DOESN'T exist", w.ElapsedMilliseconds);
                Log.Information($"{c}: Task duration WITH inverse caching, using inverse attributes: {w.ElapsedMilliseconds}ms");

                // repeat to get the time after the cache has been created
                w.Restart();
                c = usingInverseAttributes();
                w.Stop();
                results.Add("WITH inverse caching, USING inverse attributes, cache EXISTS", w.ElapsedMilliseconds);
                Log.Information($"{c}: Task duration WITH inverse caching, using inverse attributes: {w.ElapsedMilliseconds}ms");
            }

            using (var cache = model.BeginInverseCaching())
            {
                w.Restart();
                var c = notUsingInverseAttributes();
                w.Stop();
                results.Add("WITH inverse caching, NOT using inverse attributes", w.ElapsedMilliseconds);
                Log.Information($"{c}: Task duration WITH inverse caching, NOT using inverse attributes: {w.ElapsedMilliseconds}ms");
            }

            w.Restart();
            var co = usingInverseAttributes();
            w.Stop();
            results.Add("WITHOUT caching, USING inverse attributes", w.ElapsedMilliseconds);
            Log.Information($"{co}: Task duration WITHOUT caching, using inverse attributes: {w.ElapsedMilliseconds}ms");

            w.Restart();
            co = notUsingInverseAttributes();
            w.Stop();
            results.Add("WITHOUT caching, NOT using inverse attributes", w.ElapsedMilliseconds);
            Log.Information($"{co}: Task duration WITHOUT caching, NOT using inverse attributes: {w.ElapsedMilliseconds}ms");

            return results;
        }
    }
}
