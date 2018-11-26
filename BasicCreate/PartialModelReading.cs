using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Ifc4;
using Xbim.Ifc4.Interfaces;

namespace BasicExamples
{
    static class PartialModelReading
    {
        public static void Run()
        {
            var readTypes = new[] {
                typeof(IIfcSite),
                typeof(IIfcPostalAddress)
            };
            var ignoreTypes = typeof(EntityFactoryIfc4)
                .Assembly.GetTypes()
                .Where(t => t.IsClass && t.IsPublic && !t.IsAbstract && !readTypes.Any(rt => rt.IsAssignableFrom(t)))
                .Select(t => t.Name.ToUpperInvariant())
                .ToArray();
        }
    }
}
