using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedClasses
{
    public static class Extensions
    {
        public static string GetCamelCaseName(this string name)
        {
            return char.ToLower(name[0]) + name.Substring(1);
        }
        public static string GetPluralName(this string name)
        {
            return name.EndsWith("y") ? name[..^1] + "ies" : name + "s";
        }
        public static string GetCapitalName(this string name)
        {
            return name.ToUpper();
        }
    }
}
