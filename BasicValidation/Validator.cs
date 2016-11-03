using System;
using System.Collections;
using System.Linq;
using Xbim.Common;

namespace BasicValidation
{
    public class Validator
    {
        public static void Validate(IModel model)
        {
            var errCount = 0;
            foreach (var instance in model.Instances)
            {
                var type = instance.ExpressType;
                //check mandatory attributes
                var mProps = type.Properties.Where(p => p.Value.EntityAttribute.IsMandatory);
                foreach (var prop in mProps)
                {
                    var property = prop.Value;
                    var index = prop.Key;
                    var val = property.PropertyInfo.GetValue(instance);
                    var expVal = val as IExpressValueType;

                    if (val == null || (expVal != null && expVal.Value == null) )
                    {
                        errCount++;
                        Console.WriteLine($"#{instance.EntityLabel}={type.ExpressNameUpper}: missing attribute {property.Name} (no. {index})");
                    }
                }

                //check required number of items in lists
                var colProps = type.Properties.Where(p => p.Value.EnumerableType != null);
                foreach (var prop in colProps)
                {
                    var property = prop.Value;
                    var index = prop.Key;
                    var min = property.EntityAttribute.MinCardinality;
                    var max = property.EntityAttribute.MaxCardinality;

                    if (min > 0 || max > 0)
                    {
                        var value = property.PropertyInfo.GetValue(instance) as IList;
                        var count = value.Count;

                        //if it is uninitialized optional set continue
                        var optionalList = value as IOptionalItemSet;
                        if (optionalList != null && optionalList.Initialized == false)
                            continue;

                        if (min > 0 && count < min)
                        {
                            errCount++;
                            Console.WriteLine($"#{instance.EntityLabel}={type.ExpressNameUpper}: too few items in {property.Name} (no. {index}). Minimal count is {min}.");
                        }
                        if (max > 0 && count > max)
                        {
                            errCount++;
                            Console.WriteLine($"#{instance.EntityLabel}={type.ExpressNameUpper}: too many items in: {property.Name} (no. {index}). Maximum count is {max}.");
                        }
                    }
                }
            }
            if (errCount == 0)
                Console.WriteLine("No errors found.");
            else
                Console.WriteLine($"{errCount} errors found.");
        }
    }
}
