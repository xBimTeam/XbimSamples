using System;
using System.Collections.Generic;
using System.IO;
using Xbim.BCF;
using Xbim.BCF.XMLNodes;

namespace CreateBCF
{
    class Program
    {
        static void Main(string[] args)
        {
            
            var bcf = new BCF {
                Project = new ProjectXMLFile {
                    Project = new BCFProject {
                        Name = "Sample Project",
                        ProjectId = "0HE7wY7irAE9b6u8RhVcUT"
                    }
                },
                Version = new VersionXMLFile("1.0"),
                Topics = new List<Topic> {
                    new Topic {
                        Markup = new MarkupXMLFile {
                            Header = new BCFHeader {
                                Files = new List<BCFFile> {
                                    new BCFFile {
                                        Date = DateTime.Now,
                                        Filename = "Sample.ifc",
                                        IfcProject = "0HE7wY7irAE9b6u8RhVcUT",
                                        IfcSpatialStructureElement = "3sYjbGNsP7hQb8RP1A$gnO"
                                    }
                                }
                            },
                            Comments = new List<BCFComment> {
                                new BCFComment(
                                    Guid.NewGuid(), 
                                    Guid.NewGuid(), 
                                    "open", 
                                    DateTime.Now, 
                                    "Blaseius Aichel", 
                                    "This needs to be replaced completely!") {
                                    Topic = new AttrIDNode(new Guid("9B981279-4B63-46A0-ABF0-432E27B5ADC0"))
                                }
                            },
                            Topic = new BCFTopic(new Guid("9B981279-4B63-46A0-ABF0-432E27B5ADC0"), "Sample Topic"),
                            Viewpoints = new List<BCFViewpoint> {
                                new BCFViewpoint(Guid.NewGuid()) { Snapshot = "snapshot01.png", Viewpoint = "Base Viewpoint"}
                            }
                        },
                        Snapshots = new List<KeyValuePair<string, byte[]>> {
                            new KeyValuePair<string, byte[]>( "snapshot01.png", File.ReadAllBytes("snapshot01.png"))
                        },
                        Visualization = new VisualizationXMLFile {
                            
                        }
                    }
                }
            };

            using (var output = File.Create("sample.bcf"))
            {
                var data = bcf.Serialize();
                data.CopyTo(output);
                output.Close();
            }
        }
    }
}
