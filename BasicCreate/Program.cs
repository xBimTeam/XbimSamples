using Microsoft.Extensions.Logging;
using Serilog;

namespace BasicExamples
{
    public class Program
    {

        public static void Main()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.ColoredConsole()
                .CreateLogger();

            var lf = new LoggerFactory().AddSerilog();
            var log = lf.CreateLogger("ApplicationLogging");

            log.LogInformation("Examples are just about to start.");

            QuickStart.Start();

            BasicModelOperationsExample.Create();
            BasicModelOperationsExample.Retrieve();
            BasicModelOperationsExample.Update();
            BasicModelOperationsExample.Delete();

            log.LogWarning("Always use LINQ instead of general iterations!");

            LinqExample.SelectionWithLinq();
            LinqExample.SelectionWithoutLinq();
            LinqExample.SelectionWithLinqLanguage();

            log.LogError("This is how the error would be logged with log4net.");

            FederationExample.CreateFederation();
            ChangeLogExample.CreateLog();
            StepToXmlExample.Convert();

            SpatialStructureExample.Show();

            NestedCartesianPointListExample.Run();

            log.LogInformation("All examples finished.");
        }
    }
}
