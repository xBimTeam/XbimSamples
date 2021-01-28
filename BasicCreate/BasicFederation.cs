using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace BasicExamples
{
    public class BasicFederationExample
    {
        public static void Run()
        {
            var log = XbimLogging.CreateLogger<FederationExample>();

            IfcStore.ModelProviderFactory.UseMemoryModelProvider();
            using (var modelA = IfcStore.Open("SampleHouse.ifc"))
            using (var modelB = IfcStore.Open("SampleHouseExtension.ifc"))
            {
                // just for the purpose of this example
                modelA.Model.Tag = "Model A";
                modelB.Model.Tag = "Model B";

                log.LogInformation("Creating basic federation");
                var instances = new BasicFederation(new[] { modelA, modelB });

                log.LogInformation("Starting entity caching and inverse caching to improve performance");
                using (instances.BeginEntityCaching())
                using (instances.BeginInverseCaching())
                {
                    // using IFC4 interfaces will retrieve data from both IFC2x3 and IFC4 models
                    var walls = instances.OfType<IIfcWall>();
                    foreach (var wall in walls)
                    {
                        var pSets = wall.IsDefinedBy
                            .SelectMany(r => r.RelatingPropertyDefinition.PropertySetDefinitions)
                            .OfType<IIfcPropertySet>()
                            .ToList();
                        log.LogInformation("Wall {name} from model '{model}' has {pSetsCount} property sets and {propertyCount} properties",
                            wall.Name, wall.Model.Tag, pSets.Count, pSets.Sum(ps => ps.HasProperties.Count));
                    }
                }
            }
        }
    }

    public sealed class BasicFederation : IEnumerable<IPersistEntity>
    {
        private readonly HashSet<IModel> models = new HashSet<IModel>();
        public IEnumerable<IModel> Models => models;

        public BasicFederation() { }

        public BasicFederation(IEnumerable<IModel> models) : this()
        {
            this.models = new HashSet<IModel>(models);
        }

        public bool Add(IModel model)
        {
            if (InverseCache != null || EntityCache != null)
                throw new Exception("Models can't be added while caching is on.");

            return models.Add(model);
        }

        private WeakReference<IInverseCache> inverseCache;

        public IInverseCache InverseCache
        {
            get
            {
                if (inverseCache == null)
                    return null;
                if (inverseCache.TryGetTarget(out IInverseCache cache))
                    return cache;
                return null;
            }
        }

        private WeakReference<IEntityCache> entityCache;
        public IEntityCache EntityCache
        {
            get
            {
                if (entityCache == null)
                    return null;
                if (entityCache.TryGetTarget(out IEntityCache cache))
                    return cache;
                return null;
            }
        }

        public long Count => models.Sum(m => m.Instances.Count);

        public IEntityCache BeginEntityCaching()
        {
            if (EntityCache != null)
                throw new Exception("Entity caching is already active. Make use to dispose it.");

            var c = new EntityCache(models);
            entityCache = new WeakReference<IEntityCache>(c);
            return c;
        }

        public IInverseCache BeginInverseCaching()
        {
            if (InverseCache != null)
                throw new Exception("Inverse caching is already active. Make use to dispose it.");

            var c = new InverseCache(models);
            inverseCache = new WeakReference<IInverseCache>(c);
            return c;
        }

        public long CountOf<T>() where T : IPersistEntity
        {
            return models.Sum(m => m.Instances.CountOf<T>());
        }

        public T FirstOrDefault<T>() where T : IPersistEntity
        {
            return models.SelectMany(m => m.Instances.OfType<T>()).FirstOrDefault();
        }

        public T FirstOrDefault<T>(Func<T, bool> condition) where T : IPersistEntity
        {
            return models
                .Select(m => m.Instances.FirstOrDefault<T>(condition))
                .Where(i => i != null)
                .FirstOrDefault();
        }

        public IEnumerator<IPersistEntity> GetEnumerator()
        {
            return models.SelectMany(m => m.Instances).GetEnumerator();
        }

        public IEnumerable<T> OfType<T>() where T : IPersistEntity
        {
            return models.SelectMany(m => m.Instances.OfType<T>());
        }

        public IEnumerable<T> OfType<T>(bool activate) where T : IPersistEntity
        {
            return models.SelectMany(m => m.Instances.OfType<T>(activate));
        }

        public IEnumerable<T> Where<T>(Func<T, bool> condition) where T : IPersistEntity
        {
            return models.SelectMany(m => m.Instances.Where<T>(condition));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal sealed class InverseCache : IInverseCache
    {
        private readonly List<IInverseCache> caches;

        public InverseCache(IEnumerable<IModel> models)
        {
            caches = models.Select(m => m.BeginInverseCaching()).ToList();
        }

        public int Size => caches.Sum(c => c.Size);

        public void Clear()
        {
            caches.ForEach(c => c.Clear());
        }

        public void Dispose()
        {
            caches.ForEach(c => c.Dispose());
            caches.Clear();
        }
    }

    internal sealed class EntityCache : IEntityCache
    {
        private readonly List<IEntityCache> caches;

        public EntityCache(IEnumerable<IModel> models)
        {
            caches = models.Select(m => m.BeginEntityCaching()).ToList();
        }

        public int Size => caches.Sum(c => c.Size);

        public bool IsActive => caches.Any(c => c.IsActive);

        public void Clear()
        {
            caches.ForEach(c => c.Clear());
        }

        public void Dispose()
        {
            caches.ForEach(c => c.Dispose());
            caches.Clear();
        }

        public void Start()
        {
            caches.ForEach(c => c.Start());
        }

        public void Stop()
        {
            caches.ForEach(c => c.Stop());
        }
    }
}
