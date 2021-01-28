using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace BasicExamples
{
    class GetMaterialsAndContainmentExample
    {
        public static void Run()
        {
            const string separator = ";";
            IfcStore.ModelProviderFactory.UseMemoryModelProvider();
            using (var model = IfcStore.Open("SampleHouse.ifc"))
            {
                var containments = new Dictionary<string, string>();
                foreach (var rel in model.Instances.OfType<IIfcRelContainedInSpatialStructure>())
                {
                    foreach (var element in rel.RelatedElements)
                    {
                        if (containments.TryGetValue(element.GlobalId, out string containedIn))
                        {
                            containedIn += separator + rel.RelatingStructure.Name;
                            containments[element.GlobalId] = containedIn;
                        }
                        else
                        {
                            containedIn = rel.RelatingStructure.Name;
                            containments.Add(element.GlobalId, containedIn);
                        }
                    }
                }

                var materials = new Dictionary<string, string>();
                foreach (var rel in model.Instances.OfType<IIfcRelAssociatesMaterial>())
                {
                    var matString = GetMaterialString(rel.RelatingMaterial);
                    foreach (var obj in rel.RelatedObjects)
                    {
                        var guid = ((IIfcRoot)obj).GlobalId;
                        if (materials.TryGetValue(guid, out string exist))
                        {
                            exist += separator + matString;
                            materials[guid] = exist;
                        }
                        else
                        {
                            materials.Add(guid, matString);
                        }
                    }
                }
            }
        }

        private static string GetMaterialString(IIfcMaterialSelect select)
        {
            if (select == null)
                return string.Empty;

            if (select is IIfcMaterial material)
                return material.Name;

            if (select is IIfcMaterialLayerSetUsage usage)
                return GetMaterialString(usage.ForLayerSet);

            if (select is IIfcMaterialLayer layer)
                return GetMaterialString(layer.Material);

            if (select is IIfcMaterialLayerSet layerSet)
            {
                var names = layerSet.MaterialLayers.Select(m => GetMaterialString(m));
                if (names == null)
                    return "";
                return string.Join("/", names);
            }
            if (select is IIfcMaterialList list)
            {
                var names = list.Materials.Select(m => m.Name.ToString());
                if (names == null)
                    return "";
                return string.Join("/", names);
            }

            if (select is IIfcMaterialProfile profile)
                return GetMaterialString(profile.Material);

            if (select is IIfcMaterialProfileSet profileSet)
            {
                var names = profileSet.MaterialProfiles.Select(m => GetMaterialString(m));
                if (names == null)
                    return "";
                return string.Join("/", names);
            }

            if (select is IIfcMaterialConstituent constituent)
                return GetMaterialString(constituent.Material);

            if (select is IIfcMaterialConstituentSet constSet)
            {
                var names = constSet.MaterialConstituents.Select(m => GetMaterialString(m));
                if (names == null)
                    return "";
                return string.Join("/", names);
            }

            if (select is IIfcMaterialProfileSetUsage profUsage)
                return GetMaterialString(profUsage.ForProfileSet);

            throw new ArgumentException(nameof(select), "Unexpected type of material");
        }
    }
}
