using System.IO;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.IO.Step21;

namespace BasicExamples
{
    internal class CustomStep21Writer
    {
        public static void Save(TextWriter writer, IModel model)
        {
            Part21Writer.WriteHeader(model.Header, writer, "IFC4");
            var metadata = model.Metadata;
            foreach (var instance in model.Instances)
                WriteEntity(instance, writer, metadata);
            Part21Writer.WriteFooter(writer);
        }

        private static void WriteEntity(IPersistEntity entity, TextWriter output, ExpressMetaData metadata)
        {
            var expressType = metadata.ExpressType(entity);
            output.Write("#{0}={1}(", entity.EntityLabel, expressType.ExpressNameUpper);

            var first = true;

            foreach (var ifcProperty in expressType.Properties.Values)
            //only write out persistent attributes, ignore inverses
            {
                if (ifcProperty.EntityAttribute.State == EntityAttributeState.DerivedOverride)
                {
                    if (!first)
                        output.Write(',');
                    output.Write('*');
                    first = false;
                }
                else
                {
                    // workaround for IfcCartesianPointList3D from IFC4x1
                    if (entity is IfcCartesianPointList3D && ifcProperty.Name == "TagList")
                        continue;

                    var propType = ifcProperty.PropertyInfo.PropertyType;
                    var propVal = ifcProperty.PropertyInfo.GetValue(entity, null);
                    if (!first)
                        output.Write(',');
                    Part21Writer.WriteProperty(propType, propVal, output, null, metadata);
                    first = false;
                }
            }
            output.Write(");");
        }
    }
}
