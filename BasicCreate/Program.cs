namespace BasicExamples
{
    public class Program
    {
        public static void Main()
        {
            QuickStart.Start();

            BasicModelOperationsExample.Create();
            BasicModelOperationsExample.Retrieve();
            BasicModelOperationsExample.Update();
            BasicModelOperationsExample.Delete();

            LinqExample.SelectionWithLinq();
            LinqExample.SelectionWithoutLinq();
            LinqExample.SelectionWithLinqLanguage();

            FederationExample.CreateFederation();
            ChangeLogExample.CreateLog();
            StepToXmlExample.Convert();

        }
    }
}
