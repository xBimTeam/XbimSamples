using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common.Metadata;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace BasicExamples
{
    internal static class FindPropertiesExample
    {
        public static void Run()
        {
            using (var model = IfcStore.Open("IFC4_ADD2_Properties.ifc"))
            using (var cache = model.BeginInverseCaching())
            {
                var attrCache = BuildAttributeMap();
                using (var w = File.CreateText("properties_map.csv"))
                {
                    w.WriteLine($"\"Property Name\",\"Property Set Name\"");
                    foreach (var pName in PropertyNames)
                    {
                        var pTemplates = model.Instances
                            .Where<IIfcPropertyTemplate>(p => string.Equals(p.Name, pName, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        if (pTemplates.Any())
                        {
                            var psets = pTemplates.SelectMany(p => p.PartOfPsetTemplate).ToList();
                            if (psets.Any())
                            {
                                var psetNames = string.Join(", ", psets.Select(p => p.Name.ToString()));
                                w.WriteLine($"\"{pName}\",\"{psetNames}\"");
                                continue;
                            }
                        }

                        if (attrCache.TryGetValue(pName.ToUpperInvariant(), out List<ExpressType> types))
                        {
                            var typeNames = string.Join(", ", types.Select(t => t.Name));
                            w.WriteLine($"\"{pName}\",\"attribute of: {typeNames}\"");
                            continue;
                        }

                        w.WriteLine($"\"{pName}\",\"\"");
                    }
                }
            }
        }

        private static Dictionary<string, List<ExpressType>> BuildAttributeMap()
        {
            var cache = new Dictionary<string, List<ExpressType>>();
            var metadata = ExpressMetaData.GetMetadata(typeof(Xbim.Ifc4.Kernel.IfcContext).Module);
            foreach (var type in metadata.Types())
            {
                foreach (var prop in type.Properties.Values.Union(type.Inverses))
                {
                    var pName = prop.Name.ToUpperInvariant();
                    if (cache.TryGetValue(pName, out List<ExpressType> types))
                        types.Add(type);
                    else
                        cache.Add(pName, new List<ExpressType> { type });
                }
            }
            return cache;
        }

        private static readonly string[] PropertyNames = new string[] {
            "AccessoriesCode",
            "AcousticRatingCW",
            "AirPermeabilityClass",
            "AirTightness",
            "ApplianceNumber",
            "BearingCapacity",
            "CeilingBeamType",
            "CeilingThickness",
            "CompressiveStrengthMansory",
            "CompressiveStrengthMortar",
            "ConcreteDescription",
            "ConstructionMaterial",
            "CoveringLocation",
            "DiffusionResistanceFactor",
            "DocumentationLocation",
            "DoorCloserType",
            "DoorFrameCode",
            "DoorFrameFinishCode",
            "DoorFrameMaterial",
            "DoorLeafCode",
            "DoorStopType",
            "ExecutionTechnology",
            "ExpandedWidth",
            "FeelingTemperature",
            "FireReaction",
            "FittingCode",
            "FittingMaterial",
            "FloorID",
            "FMAccessories",
            "FootfallAcoustigRating",
            "GapColor",
            "GapWidth",
            "GlassHeight",
            "GlassWidth",
            "GlazingType",
            "GravelFraction",
            "GypsumBoardTyoe",
            "HasAccessControlSystems",
            "HasElectricalFireAlarms",
            "HasElectronicSecuritySystems",
            "HasEmbankment",
            "HasGeneralKeySystem",
            "HasKeyCard",
            "HasLowVoltageConnection",
            "HasMeasurementControlConnection",
            "HydroisolationType",
            "InsertType",
            "InsulationMassDensity",
            "InsulationThickness",
            "IsFireSpace",
            "LayerConnectionType",
            "LightingType",
            "LoadTransfer",
            "LockType",
            "NetArea",
            "NetVolume",
            "NetWeight",
            "NumberOfAnchorPoints",
            "OcupancyNumberPerTime",
            "ParkingSpaceID",
            "PointLoadCapacity",
            "ProfileLength",
            "ProfileType",
            "RasterPitch",
            "ReinforcementType",
            "ReinforcementWeight",
            "ResistanceBlast",
            "ResistanceBurglar",
            "ResistanceImpact",
            "ResistanceMechanicalDamage",
            "ResistanceOuterFire",
            "ResistanceRepeatedOpeningAndClosing",
            "ResistanceShooting",
            "ResistanceSnowLoad",
            "ResistanceWindLoad",
            "RoofAnchoring",
            "SillHeight",
            "SlopeLayer",
            "SteelClass",
            "SubstrateMaterialCode",
            "SubstructureWaterproofSystem",
            "SurfaceMaterialCode",
            "SurfaceObverseID",
            "SurfaceReverseID",
            "ThermalInsulationMaterialCode",
            "ThermalTransmittanceFrame",
            "ThermalTransmittanceGlazing",
            "UvResistance",
            "WaterproofClass",
            "WeightedApparentSoundReductionIndex",
            "WindowFrameFinishExterior",
            "WindowFrameFinishInterior",
            "WindowFrameReferenceProduct",
            "WindowOpeningType"
        };
    }
}
