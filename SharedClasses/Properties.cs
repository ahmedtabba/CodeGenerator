using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedClasses
{
    public class Properties
    {
        public List<(string Type, string Name, PropertyValidation Validation)> PropertiesList { get; set; } = new List<(string Type, string Name, PropertyValidation Validation)>();
        public List<string> LocalizedProp { get; set; } = new List<string> ();
        public List<(string prop, List<string> enumValues)> EnumProps { get; set; } = new List<(string prop, List<string> enumValues)> ();
    }
}
