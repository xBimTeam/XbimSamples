using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.SharedBldgElements;

namespace BasicExamples
{
    internal static class ProductSizeExample
    {
        public static void AnalyzeProductSizes(IModel model)
        {
            long GetSize(IIfcProduct product)
            {
                var length = 0;
                IEnumerable<IPersistEntity> toProcess = new[] { product };
                while (toProcess.Any())
                {
                    length += toProcess.Sum(e => e.ToString().Length);
                    toProcess = toProcess
                        .OfType<IContainsEntityReferences>()
                        .SelectMany(e => e.References);
                }
                return length;
            }

            var largeProducts = model.Instances
                .OfType<IIfcProduct>()
                .Select(p => new { Product = p, Size = GetSize(p) })
                .Where(p => p.Size > 3E6)
                .OrderByDescending(p => p.Size);

            foreach (var result in largeProducts)
            {
                Console.WriteLine($"{result.Product.Name}:  {result.Size}");
            }
        }
    }
}
