using System;
using System.Linq;
using System.Xml;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.ExternalReferenceResource;
using Xbim.Ifc4.HvacDomain;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.SharedComponentElements;
using Xbim.IO;
using Xbim.IO.Xml;

namespace BasicExamples
{
    class ComplexPropertiesExample
    {
        private static IModel model;

        private static T New<T>(Action<T> init) where T : IInstantiableEntity
        {
            return model.Instances.New<T>(init);
        }

        public static void DynamicValues()
        {
            var credentials = new XbimEditorCredentials {
                ApplicationDevelopersName = "xBIM Team",
                ApplicationFullName = "xBIM Toolkit",
                ApplicationIdentifier = "xBIM",
                ApplicationVersion = "4.0",
                EditorsFamilyName = "Cerny",
                EditorsGivenName = "Martin",
                EditorsOrganisationName = "CAS"
            };

            using (var model = IfcStore.Create(credentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel))
            //using (var model = new MemoryModel(new Xbim.Ifc4.EntityFactoryIfc4()))
            {
                ComplexPropertiesExample.model = model;

                using (var txn = model.BeginTransaction("Example"))
                {
                    var lib = New<IfcProjectLibrary>(l => l.Name = "Air Terminal Library");
                    var hz = New<IfcSIUnit>(u => {
                        u.Name = Xbim.Ifc4.Interfaces.IfcSIUnitName.HERTZ;
                        u.UnitType = Xbim.Ifc4.Interfaces.IfcUnitEnum.FREQUENCYUNIT;
                    });
                    var watt = New<IfcSIUnit>(u => {
                        u.Name = Xbim.Ifc4.Interfaces.IfcSIUnitName.WATT;
                        u.UnitType = Xbim.Ifc4.Interfaces.IfcUnitEnum.ENERGYUNIT;
                    });
                    var db = New<IfcDerivedUnit>(u => {
                        u.UnitType = Xbim.Ifc4.Interfaces.IfcDerivedUnitEnum.SOUNDPRESSURELEVELUNIT;
                        u.Elements.Add(New<IfcDerivedUnitElement>(due => {
                            due.Exponent = 1;
                            due.Unit = watt;
                        }));
                        u.Elements.Add(New<IfcDerivedUnitElement>(due => {
                            due.Exponent = -1;
                            due.Unit = watt;
                        }));
                    });
                    lib.UnitsInContext = New<IfcUnitAssignment>(ua => {
                        ua.Units.Add(hz);
                        ua.Units.Add(db);
                    });

                    var type = New<IfcAirTerminalType>(t => t.Name = "Air Terminal");
                    New<IfcRelDeclares>(rel =>
                    {
                        rel.RelatingContext = lib;
                        rel.RelatedDefinitions.Add(type);
                    });
                    var pset = New<IfcPropertySet>(ps =>
                    {
                        ps.Name = "Air Terminal Properties";
                    });
                    type.HasPropertySets.Add(pset);

                    var prop = New<IfcPropertyTableValue>(t =>
                    {
                        t.Name = "Acustic Performance";
                        t.DefiningUnit = hz;
                        t.DefinedUnit = db;
                        t.DefiningValues.AddRange(new IfcValue[] {
                            new IfcFrequencyMeasure(63), 
                            new IfcFrequencyMeasure(125),
                            new IfcFrequencyMeasure(250),
                            new IfcFrequencyMeasure(500),
                            new IfcFrequencyMeasure(1000),
                            new IfcFrequencyMeasure(2000),
                            new IfcFrequencyMeasure(4000),
                            new IfcFrequencyMeasure(8000)

                        });
                        t.DefinedValues.AddRange(new IfcValue[] {
                            new IfcSoundPressureLevelMeasure(102),
                            new IfcSoundPressureLevelMeasure(99),
                            new IfcSoundPressureLevelMeasure(98),
                            new IfcSoundPressureLevelMeasure(98),
                            new IfcSoundPressureLevelMeasure(97),
                            new IfcSoundPressureLevelMeasure(95),
                            new IfcSoundPressureLevelMeasure(86),
                            new IfcSoundPressureLevelMeasure(81)
                        });
                    });
                    pset.HasProperties.Add(prop);

                    var docProp = New<IfcPropertyReferenceValue>(r => {
                        r.Name = "Acustic Performance";
                        r.PropertyReference = New<IfcDocumentReference>(doc => {
                            doc.Name = "Acustic Performance Documentation";
                            doc.Location = "https://www.daikinac.com/content/assets/DOC/EngineeringManuals/EDUS041501.pdf";
                        });
                    });
                    pset.HasProperties.Add(docProp);

                    var fce = New<IfcPropertySingleValue>(r => {
                        r.Name = "Acustic Performance Function";
                        r.NominalValue = new IfcText("0.0492424242 * Math.pow(x,4) - 1.0328282828 * Math.pow(x,3) + 6.8068181818 * Math.pow(x,2) - 17.753968254 * x + 114.14285714");
                        r.Description = "ISO/IEC 22275:2018";
                    });
                    pset.HasProperties.Add(fce);

                    txn.Commit();
                }

                model.SaveAs("properties.ifcxml");
                model.SaveAs("properties.ifc");
                // var w = new XbimXmlWriter4(XbimXmlSettings.IFC4Add2);
                // var xml = XmlWriter.Create("properties.xml", new XmlWriterSettings { Indent = true, IndentChars = "  ", CloseOutput = true});
                // w.Write(model, xml, );
            }
        }

        public static void ComplexProperty()
        {
            var credentials = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "xBIM Team",
                ApplicationFullName = "xBIM Toolkit",
                ApplicationIdentifier = "xBIM",
                ApplicationVersion = "4.0",
                EditorsFamilyName = "Cerny",
                EditorsGivenName = "Martin",
                EditorsOrganisationName = "CAS"
            };

            using (var model = IfcStore.Create(credentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel))
            //using (var model = new MemoryModel(new Xbim.Ifc4.EntityFactoryIfc4()))
            {
                ComplexPropertiesExample.model = model;

                using (var txn = model.BeginTransaction("Example"))
                {
                    var lib = New<IfcProjectLibrary>(l => l.Name = "Declaration of Performance");
                    var mm = New<IfcSIUnit>(u => {
                        u.Name = Xbim.Ifc4.Interfaces.IfcSIUnitName.METRE;
                        u.Prefix = Xbim.Ifc4.Interfaces.IfcSIPrefix.MILLI;
                        u.UnitType = Xbim.Ifc4.Interfaces.IfcUnitEnum.LENGTHUNIT;
                    });
                    lib.UnitsInContext = New<IfcUnitAssignment>(ua => {
                        ua.Units.Add(mm);
                    });

                    var declarations = New<IfcRelDeclares>(rel =>
                    {
                        rel.RelatingContext = lib;
                    }).RelatedDefinitions;

                    var psetTemplate = New<IfcPropertySetTemplate>(ps => {
                        ps.Name = "dimensions";
                        ps.ApplicableEntity = nameof(IfcElement);
                    });

                    declarations.Add(psetTemplate);

                    var lengthTemplate = New<IfcComplexPropertyTemplate>(c => {
                        c.Name = "length";
                        c.HasPropertyTemplates.AddRange(new[] {
                            New<IfcSimplePropertyTemplate>(v => {
                                v.Name = "Value";
                                v.TemplateType = Xbim.Ifc4.Interfaces.IfcSimplePropertyTemplateTypeEnum.P_SINGLEVALUE;
                                v.PrimaryUnit = mm;
                                v.PrimaryMeasureType = nameof(IfcLengthMeasure);
                            }),
                            New<IfcSimplePropertyTemplate>(v => {
                                v.Name = "ReferenceDocument";
                                v.TemplateType = Xbim.Ifc4.Interfaces.IfcSimplePropertyTemplateTypeEnum.P_REFERENCEVALUE;
                                v.PrimaryMeasureType = nameof(IfcDocumentReference);
                            })
                        });
                    });
                    psetTemplate.HasPropertyTemplates.Add(lengthTemplate);


                    var brick = New<IfcBuildingElementPart>(b => {
                        b.Name = "Porotherm 50 EKO+ Profi R";
                        b.PredefinedType = Xbim.Ifc4.Interfaces.IfcBuildingElementPartTypeEnum.USERDEFINED;
                        b.ObjectType = "BRICK";
                    });
                    declarations.Add(brick);

                    var pset = New<IfcPropertySet>(ps => {
                        ps.Name = psetTemplate.Name;
                        ps.HasProperties.Add(New<IfcComplexProperty>(c => {
                            c.Name = lengthTemplate.Name?.ToString();
                            c.HasProperties.AddRange(new IfcProperty[] {
                            New<IfcPropertySingleValue>(v => {
                                v.Name = "Value";
                                v.Unit = mm;
                                v.NominalValue = new IfcLengthMeasure(300);
                            }),
                            New<IfcPropertyReferenceValue>(v => {
                                v.Name = "ReferenceDocument";
                                v.PropertyReference = New<IfcDocumentReference>(d => {
                                    d.Identification = "EN 772-1";
                                });
                            })
                        });
                        }));
                    });
                    New<IfcRelDefinesByTemplate>(r => {
                        r.RelatedPropertySets.Add(pset);
                        r.RelatingTemplate = psetTemplate;
                    });
                    New<IfcRelDefinesByProperties>(r => {
                        r.RelatingPropertyDefinition = pset;
                        r.RelatedObjects.Add(brick);
                    });

                    txn.Commit();
                }

                //model.SaveAs("complex_length.ifcxml");
                model.SaveAs("complex_length.ifc");
                var w = new XbimXmlWriter4(XbimXmlSettings.IFC4Add2);
                var xml = XmlWriter.Create("complex_length.ifcxml", new XmlWriterSettings { Indent = true, IndentChars = "  "});
                var entities = new IPersistEntity[0]
                    .Concat(model.Instances.OfType<IfcProjectLibrary>())
                    .Concat(model.Instances);
                w.Write(model, xml, entities);
                xml.Close();
            }
        }
    }
}
