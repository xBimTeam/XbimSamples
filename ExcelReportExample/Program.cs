using System;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;

// ReSharper disable All

namespace ExcelReportExample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //initialize NPOI workbook from the template
            var workbook = new XSSFWorkbook("template.xlsx");
            var sheet = workbook.GetSheet("Spaces");

            //Create nice numeric formats with units. Units would need a LOT MORE care in real world. 
            //We just know that our current model has space areas in square meters and space volumes in cubic meters
            //Please note that the original data exported from Revit were wrong because volumes were 1000x bigger than they should be.
            //Data were fixed using xBIM for this example.

            var areaFormat = workbook.CreateDataFormat();
            var areaFormatId = areaFormat.GetFormat("# ##0.00 [$m²]");
            var areaStyle = workbook.CreateCellStyle();
            areaStyle.DataFormat = areaFormatId;

            var volumeFormat = workbook.CreateDataFormat();
            var volumeFormatId = volumeFormat.GetFormat("# ##0.00 [$m³]");
            var volumeStyle = workbook.CreateCellStyle();
            volumeStyle.DataFormat = volumeFormatId;


            //Open IFC model. We are not going to change anything in the model so we can leave editor credentials out.
            using (var model = IfcStore.Open("SampleHouse.ifc"))
            {
                //Get all spaces in the model. 
                //We use ToList() here to avoid multiple enumeration with Count() and foreach(){}
                var spaces = model.Instances.OfType<IIfcSpace>().ToList();
                //Set header content
                sheet.GetRow(0).GetCell(0)
                    .SetCellValue(string.Format("Space Report ({0} spaces)", spaces.Count));
                foreach (var space in spaces)
                {
                    //write report data
                    WriteSpaceRow(space, sheet, areaStyle, volumeStyle);
                }
            }

            //save report
            using (var stream = File.Create("spaces.xlsx"))
            {
                workbook.Write(stream);
                stream.Close();
            }

            //see the result if you have some SW associated with the *.xlsx
            Process.Start("spaces.xlsx");
        }

        private static void WriteSpaceRow(IIfcSpace space, ISheet sheet, ICellStyle areaStyle, ICellStyle volumeStyle)
        {
            var row = sheet.CreateRow(sheet.LastRowNum + 1);

            var name = space.Name;
            row.CreateCell(0).SetCellValue(name);

            var floor = GetFloor(space);
            row.CreateCell(1).SetCellValue(floor != null ? floor.Name.ToString() : "");

            var area = GetArea(space);
            if (area != null)
            {
                var cell = row.CreateCell(2);
                cell.CellStyle = areaStyle;

                //there is no guarantee it is a number if it came from property and not from a quantity
                if (area.UnderlyingSystemType == typeof(double))
                    cell.SetCellValue((double)(area.Value));
                else
                    cell.SetCellValue(area.ToString());
            }

            var volume = GetVolume(space);
            if (volume != null)
            {
                var cell = row.CreateCell(3);
                cell.CellStyle = volumeStyle;

                //there is no guarantee it is a number if it came from property and not from a quantity
                if (volume.UnderlyingSystemType == typeof(double))
                    cell.SetCellValue((double)(volume.Value));
                else
                    cell.SetCellValue(volume.ToString());
            }
        }

        private static IIfcBuildingStorey GetFloor(IIfcSpace space)
        {
            return
                //get all objectified relations which model decomposition by this space
                space.Decomposes

                //select decomposed objects (these might be either other space or building storey)
                .Select(r => r.RelatingObject)

                //get only storeys
                .OfType<IIfcBuildingStorey>()

                //get the first one
                .FirstOrDefault();
        }

        private static IIfcValue GetArea(IIfcProduct product)
        {
            //try to get the value from quantities first
            var area =
                //get all relations which can define property and quantity sets
                product.IsDefinedBy
                
                //Search across all property and quantity sets. 
                //You might also want to search in a specific quantity set by name
                .SelectMany(r => r.RelatingPropertyDefinition.PropertySetDefinitions)
                
                //Only consider quantity sets in this case.
                .OfType<IIfcElementQuantity>()
                
                //Get all quantities from all quantity sets
                .SelectMany(qset => qset.Quantities)
                
                //We are only interested in areas 
                .OfType<IIfcQuantityArea>()
                
                //We will take the first one. There might obviously be more than one area properties
                //so you might want to check the name. But we will keep it simple for this example.
                .FirstOrDefault().AreaValue;

            if (area != null)
                return area;

            //try to get the value from properties
            return GetProperty(product, "Area");
        }

        private static IIfcValue GetVolume(IIfcProduct product)
        {
            var volume = product.IsDefinedBy
                             .SelectMany(r => r.RelatingPropertyDefinition.PropertySetDefinitions)
                             .OfType<IIfcElementQuantity>()
                             .SelectMany(qset => qset.Quantities)
                             .OfType<IIfcQuantityVolume>()
                             .FirstOrDefault() != null ? product.IsDefinedBy
                .SelectMany(r => r.RelatingPropertyDefinition.PropertySetDefinitions)
                .OfType<IIfcElementQuantity>()
                .SelectMany(qset => qset.Quantities)
                .OfType<IIfcQuantityVolume>()
                .FirstOrDefault().VolumeValue : 0;
            if (volume != null)
                return volume;
            return GetProperty(product, "Volume");
        }

        private static IIfcValue GetProperty(IIfcProduct product, string name)
        {
            return
                //get all relations which can define property and quantity sets
                product.IsDefinedBy

                //Search across all property and quantity sets. You might also want to search in a specific property set
                .SelectMany(r => r.RelatingPropertyDefinition.PropertySetDefinitions)

                //Only consider property sets in this case.
                .OfType<IIfcPropertySet>()

                //Get all properties from all property sets
                .SelectMany(pset => pset.HasProperties)

                //lets only consider single value properties. There are also enumerated properties, 
                //table properties, reference properties, complex properties and other
                .OfType<IIfcPropertySingleValue>()

                //lets make the name comparison more fuzzy. This might not be the best practise
                .Where(p =>
                    string.Equals(p.Name, name, System.StringComparison.OrdinalIgnoreCase) ||
                    p.Name.ToString().ToLower().Contains(name.ToLower()))

                //only take the first. In reality you should handle this more carefully.
                .FirstOrDefault().NominalValue;
        }
    }
}
