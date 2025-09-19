namespace Verve
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Collections.Generic;


    public static class GameFeatureComponentExtensions
    {
        public static void FindParameters(this IGameFeatureComponent self, List<IGameFeatureParameter> parameters, Func<FieldInfo, bool> filter = null)
        {
            if (self == null)
                return;

            parameters ??= new List<IGameFeatureParameter>();
            var fields = self.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .OrderBy(t => t.MetadataToken);

            foreach (var field in fields)
            {
                if (field.FieldType.IsSubclassOf(typeof(IGameFeatureParameter)))
                {
                    if (filter?.Invoke(field) ?? true)
                        parameters.Add((IGameFeatureParameter)field.GetValue(self));
                }
                else if (!field.FieldType.IsArray && field.FieldType.IsClass && field.FieldType != typeof(string))
                {
                    if (field.GetValue(self) is IGameFeatureComponent nested)
                    {
                        nested.FindParameters(parameters, filter);
                    }
                }
            }
        }
    }
}