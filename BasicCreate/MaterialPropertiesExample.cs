using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.MaterialResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.IO;

namespace BasicExamples
{
    internal class MaterialPropertiesExample
    {
        public void AddMaterialProperties()
        {
            var editor = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "xBIM Team",
                ApplicationFullName = "xBIM Toolkit",
                ApplicationIdentifier = "xBIM",
                ApplicationVersion = "4.0",
                EditorsFamilyName = "Santini Aichel",
                EditorsGivenName = "Johann Blasius",
                EditorsOrganisationName = "Independent Architecture"
            };

            using (var model = IfcStore.Create(editor, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel))
            {
                var i = model.Instances;

                using (var txn = model.BeginTransaction())
                {
                    var material = i.New<IfcMaterial>(m => m.Name = "Carbon");
                    var pset = i.New<IfcMaterialProperties>(ps => {
                        ps.Name = "Pset_MaterialCommon";
                        ps.Material = material;
                        ps.Properties.AddRange(new[] { 
                            i.New<IfcPropertySingleValue>(p => {
                                p.Name = "MolecularWeight";
                                p.NominalValue = new IfcMolecularWeightMeasure(44.0098);
                            }),
                            i.New<IfcPropertySingleValue>(p => {
                                p.Name = "Porosity";
                                p.NominalValue = new IfcNormalisedRatioMeasure(0.0);
                            }),
                            i.New<IfcPropertySingleValue>(p => {
                                p.Name = "MassDensity";
                                p.NominalValue = new IfcMassDensityMeasure(2180);
                            })
                        });
                    });
                    txn.Commit();
                }
            }
        }
    }
}
