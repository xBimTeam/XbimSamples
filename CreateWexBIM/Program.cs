using Microsoft.Extensions.Logging;
using Serilog;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;

namespace CreateWexBIM
{
    class Program
    {
        public static void Main(string[] args)
        {
            var typeInterfaces = typeof(IIfcRoot).Assembly.GetTypes()
                .Where(t => typeof(IIfcTypeObject).IsAssignableFrom(t) && t.IsInterface)
                .ToList();

            Log.Logger = new LoggerConfiguration()
               .Enrich.FromLogContext()
               .WriteTo.Console()
               .CreateLogger();

            // set up xBIM logging. It will use your providers.
            XbimLogging.LoggerFactory.AddSerilog();
            var log = XbimLogging.LoggerFactory.CreateLogger("WexbimCreation");

            var a = 0;
            var s = a.ToString("G", CultureInfo.InvariantCulture);


            var fileName = args.Length > 0 ? args[0] : GetFileName();
            if (string.IsNullOrWhiteSpace(fileName))
            {
                log.LogInformation($"No file selected.");
                return;
            }

            if (!File.Exists(fileName))
            {
                log.LogInformation($"File {fileName} not found.");
                return;
            }
            log.LogInformation($"File name: {fileName}");
            log.LogInformation($"File size: {new FileInfo(fileName).Length / 1e6:N}MB");

            var w = Stopwatch.StartNew();
            IfcStore.ModelProviderFactory.UseHeuristicModelProvider();
            using (var model = IfcStore.Open(fileName, null, -1))
            {
                // model.ModelFactors.DeflectionAngle *= 5;

                log.LogInformation("Creating wexBIM file from IFC model.");
                var context = new Xbim3DModelContext(model);
                context.CreateContext(null, false);

                IVector3D translation = null;
                using (var store = model.GeometryStore.BeginRead())
                {
                    var regions = store.ContextRegions.SelectMany(r => r).ToList();
                    var biggest = regions.OrderByDescending(r => r.Population).FirstOrDefault();
                    if (biggest != null)
                    {
                        var c = biggest.Centre;
                        translation = new XbimVector3D(-c.X, -c.Y, -c.Z);
                    }
                }

                var wexBimFilename = Path.ChangeExtension(fileName, "wexbim");
                using (var wexBimFile = File.Create(wexBimFilename))
                {
                    using (var wexBimBinaryWriter = new BinaryWriter(wexBimFile))
                    {
                        model.SaveAsWexBim(wexBimBinaryWriter, null, translation);
                        wexBimBinaryWriter.Close();
                    }
                    wexBimFile.Close();
                    w.Stop();
                    log.LogInformation($"Processing completed in {w.ElapsedMilliseconds / 1e3:N}s");
                    log.LogInformation($"Saved file: {wexBimFilename}");
                }
            }
        }

        

        private static IfcStore GetSubModel(IfcStore model, params int[] ids)
        {
            var result = IfcStore.Create(model.SchemaVersion, XbimStoreType.InMemoryModel);
            using (var txn = result.BeginTransaction())
            {
                var map = new XbimInstanceHandleMap(model, result);
                var toInsert = ids.Select(id => model.Instances[id]).OfType<IIfcProduct>();
                result.InsertCopy(toInsert, true, true, map);
                txn.Commit();
            }

            return result;
        }

        private static string GetFileName()
        {
            var filename = "";
            Thread t = new Thread(() =>
            {
                var dlg = new OpenFileDialog
                {
                    CheckFileExists = true,
                    Filter = "*.ifc|*.ifc|*.ifcxml|*.ifcxml|*.ifczip|*.ifczip",
                    AddExtension = false,
                    FilterIndex = 0,
                    Multiselect = false,
                    Title = "Select IFC file"
                };
                if (dlg.ShowDialog() == DialogResult.OK)
                    filename = dlg.FileName;
            });
            t.TrySetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            return filename;
        }
        
    }
}
