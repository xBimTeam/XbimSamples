using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.CobieExpress;
using Xbim.Common;
using Xbim.IO.Memory;

namespace BasicExamples
{
    class ExpandExample
    {
        public static void ExpandAttributes()
        {
            using (var cobie = new MemoryModel(new EntityFactory()))
            {
                using (var txn = cobie.BeginTransaction("Test"))
                {
                    var component1 = cobie.Instances.New<CobieComponent>(c => c.Name = "Chair");
                    var component2 = cobie.Instances.New<CobieComponent>(c => c.Name = "Chair");
                    var a1 = cobie.Instances.New<CobieAttribute>(c =>
                    {
                        c.Name = "A";
                        c.Value = new StringValue("Value A");
                    });
                    var a2 = cobie.Instances.New<CobieAttribute>(c =>
                    {
                        c.Name = "B";
                        c.Value = new StringValue("Value B");
                    });
                    component1.Attributes.AddRange(new[] { a1, a2 });
                    component2.Attributes.AddRange(new[] { a1, a2 });

                    Expand(cobie, (CobieComponent c) => c.Attributes);

                    if (component1.Attributes.Any(a => component2.Attributes.Contains(a)))
                        throw new Exception("Didn't work!");
                }
            }
        }

        private static void Expand<IParentEntity, IUniqueEntity>(IModel model, Func<IParentEntity, ICollection<IUniqueEntity>> accessor) where IParentEntity : IPersistEntity where IUniqueEntity : IPersistEntity
        {
            //get duplicates in one go to avoid exponential search
            var candidates = new Dictionary<IUniqueEntity, List<IParentEntity>>();
            foreach (var entity in model.Instances.OfType<IParentEntity>())
            {
                foreach (var val in accessor(entity))
                {
                    List<IParentEntity> assets;
                    if (!candidates.TryGetValue(val, out assets))
                    {
                        assets = new List<IParentEntity>();
                        candidates.Add(val, assets);
                    }
                    assets.Add(entity);
                }
            }

            var multi = candidates.Where(a => a.Value.Count > 1);
            var map = new XbimInstanceHandleMap(model, model);

            foreach (var kvp in multi)
            {
                var value = kvp.Key;
                var entities = kvp.Value;

                //skip the first
                for (int i = 1; i < entities.Count; i++)
                {
                    //clear map to create complete copy every time
                    map.Clear();
                    var copy = model.InsertCopy(value, map, null, false, false);

                    //remove original and add fresh copy
                    var entity = entities[i];
                    var collection = accessor(entity);
                    collection.Remove(value);
                    collection.Add(copy);
                }
            }
        }
    }
}
