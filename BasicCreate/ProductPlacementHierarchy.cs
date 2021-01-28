using System;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace BasicExamples
{
    class ProductPlacementHierarchy
    {
        public static void Run(IModel model)
        {
            foreach (var product in model.Instances.OfType<IIfcProduct>())
            {
                var indent = "";
                Console.WriteLine($"Product #{product.EntityLabel}={product.GetType().Name.ToUpperInvariant()}");
                var placement = product.ObjectPlacement;
                while (placement != null)
                {
                    indent += "  ";
                    if (placement is IIfcGridPlacement gridPlacement)
                    {
                        // handle grid placement
                        Console.WriteLine($"{indent}Grid placement");
                    }
                    else if (placement is IIfcLocalPlacement localPlacement)
                    {
                        // handle local placement
                        if (localPlacement.RelativePlacement is IIfcAxis2Placement3D ap3d)
                        {
                            Console.WriteLine($"{indent}Placement 3D:");
                            Console.WriteLine($"{indent}Location: {ap3d.Location.ToString()}");
                            Console.WriteLine($"{indent}Orientation X: {ap3d.RefDirection.ToString()}");
                            Console.WriteLine($"{indent}Orientation Z: {ap3d.Axis.ToString()}");
                        }
                        else if (localPlacement.RelativePlacement is IIfcAxis2Placement2D ap2d)
                        {
                            Console.WriteLine($"{indent}Placement 2D:");
                            Console.WriteLine($"{indent}Location: {ap2d.Location.ToString()}");
                            Console.WriteLine($"{indent}Orientation X: {ap2d.RefDirection.ToString()}");
                        }

                        // walk up the placement tree
                        placement = localPlacement.PlacementRelTo;
                        continue;
                    }
                    break;
                }
            }
        }
    }
}
