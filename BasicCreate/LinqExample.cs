using System;
using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace BasicExamples
{
    public class LinqExample
    {
        public static void SelectionWithLinq()
        {
            const string ifcFilename = "SampleHouse.ifc";
            var model = IfcStore.Open(ifcFilename);

            var requiredProducts = new IIfcProduct[0]
                .Concat(model.Instances.OfType<IIfcWallStandardCase>())
                .Concat(model.Instances.OfType<IIfcDoor>())
                .Concat(model.Instances.OfType<IIfcWindow>());

            //This will only iterate over entities you really need (9 in this case)
            foreach (var product in requiredProducts)
            {
                //Do anything you want here...
            }
        }

        //THIS IS A WRONG PRACTISE. DO NOT PROGRAM THIS WAY! IT IS ABOUT 4.5x SLOWER THAN PREVIOUS EXAMPLE WHICH DOES EXACTLY THE SAME!
        public static void SelectionWithoutLinq()
        {
            const string ifcFilename = "SampleHouse.ifc";
            var model = IfcStore.Open(ifcFilename);
            //this will iterate over 47309 entities instead of just 9 you need in this case!
            foreach (var entity in model.Instances)
            {
                if (entity is IIfcWallStandardCase ||
                    entity is IIfcDoor ||
                    entity is IIfcWindow)
                {
                    //You may want to do something here. Please DON'T!
                }
            }
        }

        public static void SelectionWithLinqLanguage()
        {
            var model = IfcStore.Open("SampleHouse.ifc");

            //expression using LINQ
            var idsOfWallsWithOpenings =
                from wall in model.Instances.OfType<IIfcWall>()
                where wall.HasOpenings.Any()
                select wall.GlobalId;

            foreach (var id in idsOfWallsWithOpenings)
                Console.WriteLine(id);

            //equivalent expression using chained extensions of IEnumerable and lambda expressions
            idsOfWallsWithOpenings =
                model.Instances
                .Where<IIfcWall>(wall => wall.HasOpenings.Any())
                .Select(wall => wall.GlobalId);

            foreach (var id in idsOfWallsWithOpenings)
                Console.WriteLine(id);
        }
    }
}
