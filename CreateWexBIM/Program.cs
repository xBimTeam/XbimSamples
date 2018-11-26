using Microsoft.Extensions.Logging;
using Serilog;
using System.IO;
using Xbim.Ifc;
using Xbim.ModelGeometry.Scene;

namespace CreateWexBIM
{
    class Program
    {
        public static void Main()
        {
            Log.Logger = new LoggerConfiguration()
               .Enrich.FromLogContext()
               .WriteTo.ColoredConsole()
               .CreateLogger();

            var lf = new LoggerFactory().AddSerilog();
            var log = lf.CreateLogger("WexbimCreation");
            log.LogInformation("Creating wexBIM file from IFC model.");

            const string fileName = @"SampleHouse.ifc";
            log.LogInformation($"File size: {new FileInfo(fileName).Length / 1e6}MB");

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
