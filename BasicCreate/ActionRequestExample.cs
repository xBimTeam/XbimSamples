using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.ActorResource;
using Xbim.Ifc4.ExternalReferenceResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.ProcessExtension;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.SharedMgmtElements;
using Xbim.IO;

namespace BasicExamples
{
    class ActionRequestExample
    {
        public static void Run()
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

            IfcStore.ModelProviderFactory.UseMemoryModelProvider();
            using (var model = IfcStore.Create(editor, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel))
            {
                using (var txn = model.BeginTransaction())
                {
                    // LOIN = level of information need for specific purpose, role, milestone and classification
                    // root object type, there might be many of them
                    var loin = model.Instances.New<IfcProjectLibrary>(lib =>
                    {
                        lib.Name = "Level of Information Need";
                    });

                    // Purpose of the data requirement/exchange
                    model.Instances.New<IfcRelAssignsToControl>(rel =>
                    {
                        rel.RelatedObjects.Add(loin);
                        rel.RelatingControl = model.Instances.New<IfcActionRequest>(r =>
                        {
                            r.Name = "Thermal Analysis Information Exchange Request";
                            r.ObjectType = "INFORMATION_REQUEST";
                            r.PredefinedType = IfcActionRequestTypeEnum.USERDEFINED;
                        });
                    });

                    // Actor / Role = Who in interested in the data
                    model.Instances.New<IfcRelAssignsToActor>(r =>
                    {
                        r.ActingRole = model.Instances.New<IfcActorRole>(ar => ar.Role = IfcRoleEnum.CLIENT);
                        r.RelatedObjects.Add(loin);
                        r.RelatingActor = model.Instances.New<IfcActor>(a =>
                        {
                            a.TheActor = model.Instances.New<IfcPerson>(p =>
                            {
                                p.FamilyName = "Builder";
                                p.GivenName = "Bob";
                            });
                        }); ;
                    });

                    // Milestone = point in time
                    model.Instances.New<IfcRelAssignsToProcess>(rel =>
                    {
                        rel.RelatedObjects.Add(loin);
                        rel.RelatingProcess = model.Instances.New<IfcTask>(t =>
                        {
                            t.Name = "Initial design";
                            t.IsMilestone = true;
                        });
                    });


                    // Classification = subject of interest
                    model.Instances.New<IfcRelAssociatesClassification>(rel =>
                    {
                        rel.RelatedObjects.Add(loin);
                        rel.RelatingClassification =
                            model.Instances.New<IfcClassificationReference>(c => {
                                c.Identification = "NF2.3";
                                c.ReferencedSource = model.Instances.New<IfcClassification>(cs => {
                                    cs.Name = "Uniclass 2015";
                                    cs.Description = "";
                                });
                            });
                    });

                    // Declared data requirements / templates
                    model.Instances.New<IfcRelDeclares>(decl =>
                    {
                        decl.RelatingContext = loin;
                        decl.RelatedDefinitions.Add(model.Instances.New<IfcPropertySetTemplate>(ps =>
                        {
                            ps.Name = "Performance Data";
                            ps.ApplicableEntity = nameof(IfcCivilElement);
                            ps.HasPropertyTemplates.AddRange(new[] {
                                model.Instances.New<IfcSimplePropertyTemplate>(p => {
                                    p.Name = "Finish Grade";
                                    p.PrimaryMeasureType = nameof(IfcIdentifier);
                                    p.TemplateType = IfcSimplePropertyTemplateTypeEnum.P_SINGLEVALUE;
                                }),
                                model.Instances.New<IfcSimplePropertyTemplate>(p => {
                                    p.Name = "Fire Resistance";
                                    p.PrimaryMeasureType = nameof(IfcTimeMeasure);
                                    p.TemplateType = IfcSimplePropertyTemplateTypeEnum.P_SINGLEVALUE;
                                })
                                // .... add all properties here
                            });
                        }));
                        // ... add all property sets here
                    });

                    txn.Commit();
                }
                model.SaveAs("LevelOfInformationNeed.ifc");
            }
        }
    }
}
