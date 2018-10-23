using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xbim.Common.Metadata;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PropertyResource;

namespace BasicExamples
{
    public class IfcValueEnum
    {
        public static void Export()
        {
            var valType = typeof(IfcValue);
            var types = valType.Assembly.GetTypes()
                .Where(t => t.IsValueType && valType.IsAssignableFrom(t))
                .OrderBy(t => t.Name);
            using (var w = File.CreateText("MeasureTypeEnum.csv"))
            {
                // header
                w.WriteLine($"Identifier, Name");

                foreach (var t in types)
                {
                    w.WriteLine($"{t.Name}, {Format(t.Name)}");
                }
            }

            types = valType.Assembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(IfcObjectReferenceSelect).IsAssignableFrom(t))
                .OrderBy(t => t.Name);
            using (var w = File.CreateText("ReferenceTypeEnum.csv"))
            {
                // header
                w.WriteLine($"Identifier, Name");

                foreach (var t in types)
                {
                    w.WriteLine($"{t.Name}, {Format(t.Name)}");
                }
            }

            var meta = ExpressMetaData.GetMetadata(valType.Module);
            var product = meta.ExpressType("IFCPRODUCT");
            var element = meta.ExpressType("IFCELEMENT");
            var spatial = meta.ExpressType("IFCSPATIALELEMENT");
            var stack = new Stack<ExpressType>(new [] { element, spatial });
            var enumCounter = 5000;

            using (var w = File.CreateText("ApplicableIfcTypes.csv"))
            {
                // header
                w.WriteLine($"ID, Identifier, Name, Parent");

                // root
                w.WriteLine($"{product.TypeId}, {product.Name}, {Format(product.Name)}, NULL");

                while (stack.Count != 0)
                {
                    var type = stack.Pop();
                    if (type.SubTypes != null && type.SubTypes.Any())
                    {
                        foreach (var t in type.SubTypes)
                        {
                            stack.Push(t);
                        }
                    }
                    w.WriteLine($"{type.TypeId}, {type.Name}, {Format(type.Name)}, {type.SuperType.TypeId}");

                    // predefined types
                    var pdt = type.Properties.Select(kvp => kvp.Value).FirstOrDefault(p => string.Equals(p.Name, "PredefinedType"));
                    if (pdt == null)
                        continue;
                    var pdtType = pdt.PropertyInfo.PropertyType;
                    if (pdtType.IsGenericType)
                        pdtType = Nullable.GetUnderlyingType(pdtType);
                    var values = Enum.GetNames(pdtType);
                    foreach (var value in values)
                    {
                        w.WriteLine($"{++enumCounter}, {value}, {value}, {type.TypeId}");
                    }
                }
            }
        }

        private static readonly Regex CamelCaseRegex = new Regex("([a-z]+)([A-Z]+)");

        private static string Format(string name)
        {
            // strip 'Ifc' prefix
            name = name.Substring(3);
            name = CamelCaseRegex.Replace(name, "$1 $2");
            return name;
        }
    }
}
