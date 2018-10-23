using System.IO;
using Xbim.Ifc;
using Xbim.ModelGeometry.Scene;

namespace CreateWexBIM
{
    class Program
    {
        public static void Main()
        {
            const string fileName = @"SampleHouse.ifc";
            using (var model = IfcStore.Open(fileName, null, -1))
            {
                var context = new Xbim3DModelContext(model);
                context.CreateContext();


                var wexBimFilename = Path.ChangeExtension(fileName, "wexbim");
                using (var wexBimFile = File.Create(wexBimFilename))
                {
                    using (var wexBimBinaryWriter = new BinaryWriter(wexBimFile))
                    {
                        model.SaveAsWexBim(wexBimBinaryWriter);
                        wexBimBinaryWriter.Close();
                    }
                    wexBimFile.Close();
                }
            }
        }
    }
}
