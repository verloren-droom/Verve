#if UNITY_EDITOR

namespace VerveEditor.UniEx.AI
{
    using Verve.Unit;
    using System.Linq;
    using VerveUniEx.AI;
    
    
    public static class AIUnitExtension
    {
        public static void DrawGizmos(this AIUnit self)
        {
            for (int i = 0; i < self.GetAllBT().Count(); i++)
            {
                self.GetAllBT().ElementAt(i).DrawGizmos();
            }
        }
        
        // public static void DrawGizmos(this UnitRules self)
        // {
        //     foreach (var dependency in self.GetAllDependencies())
        //     {
        //         
        //     }
        // }
    }
}

#endif