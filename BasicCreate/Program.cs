using Microsoft.Extensions.Logging;
using Serilog;
using Xbim.Common;

namespace BasicExamples
{
    public class Program
    {

        public static void Main()
        {
            // set up Serilog or any other provider which imlements Microsoft.Extensions.Logging.ILogger
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            // set up binding to MS Abstractions
            var lf = new LoggerFactory().AddSerilog();
            var log = lf.CreateLogger("ApplicationLogging");

            // make it available to xBIM
            XbimLogging.LoggerFactory = lf;

            // use the log yourself
            log.LogInformation("Examples are just about to start.");

            QuickStart.Start();

            BasicModelOperationsExample.Create();
            BasicModelOperationsExample.Retrieve();
            BasicModelOperationsExample.Update();
            BasicModelOperationsExample.Delete();

            log.LogWarning("Always use LINQ instead of general iterations!");

            CubeWithColourExample.Run();

            LinqExample.SelectionWithLinq();
            LinqExample.SelectionWithoutLinq();
            LinqExample.SelectionWithLinqLanguage();

            log.LogError("This is how the error would be logged with Serilog.");

            BasicFederationExample.Run();

            InverseSearchExample.Run();

            FederationExample.CreateFederation();
            ChangeLogExample.CreateLog();
            StepToXmlExample.Convert();

            SpatialStructureExample.Show();

            NestedCartesianPointListExample.Run();

            SingleObjectExample.Run();

            GetMaterialsAndContainmentExample.Run();

            log.LogInformation("All examples finished.");
        }
    }
}
