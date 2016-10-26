using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Ifc;

namespace BasicValidation
{
    internal class Program
    {
        private static void Main()
        {
            using (var model = IfcStore.Open("SampleHouse.ifc"))
            {
                Validator.Validate(model);
            }
        }
    }
}
