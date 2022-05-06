using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common.Metadata;
using Xbim.Ifc4.Kernel;

namespace BasicExamples
{
    internal class ExtractProductHierarchy
    {
        public static void Run()
        {
            var root = typeof(IfcProduct);
            var metadata = ExpressMetaData.GetMetadata(root.Assembly.Modules.First());

            using (var w = File.CreateText("product_hierarchy.csv"))
            {
                // header
                w.WriteLine("Name,Type,Parent");

                void write(ExpressType node, ExpressType parent)
                {
                    var parentName = parent?.Name ?? "";
                    w.WriteLine($"{node.Name},entity-type,{parentName}");

                    // predefined type children
                    var predefTypeProp = node.Properties.Values.FirstOrDefault(p => p.Name == "PredefinedType");
                    if (predefTypeProp != null)
                    {
                        var predefType = predefTypeProp.PropertyInfo.PropertyType;
                        // unwrap nullable if necessary
                        if (predefType.IsGenericType)
                            predefType = predefType.GetGenericArguments()[0];

                        var predefNames = Enum.GetNames(predefType);
                        foreach (var item in predefNames)
                        {
                            w.WriteLine($"{item},predefined-type,{node.Name}");
                        }
                    }

                    // sub types
                    foreach (var children in node.SubTypes)
                    {
                        write(children, node);
                    }

                }

                var eType = metadata.ExpressType(root);
                write(eType, null);
            }
        }
    }
}
