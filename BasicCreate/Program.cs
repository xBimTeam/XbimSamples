using Xbim.Common.Logging;

namespace BasicExamples
{
    public class Program
    {
        
        public static void Main()
        {
            var log = LoggerFactory.GetLogger(); 

            log.Info("Examples are just about to start.");

            QuickStart.Start();

            BasicModelOperationsExample.Create();
            BasicModelOperationsExample.Retrieve();
            BasicModelOperationsExample.Update();
            BasicModelOperationsExample.Delete();

            log.Warn("Always use LINQ instead of general iterations!");

            LinqExample.SelectionWithLinq();
            LinqExample.SelectionWithoutLinq();
            LinqExample.SelectionWithLinqLanguage();

            log.Error("This is how the error would be logged with log4net.");

            FederationExample.CreateFederation();
            ChangeLogExample.CreateLog();
            StepToXmlExample.Convert();

            log.Info("All examples finished.");
        }
    }
}
