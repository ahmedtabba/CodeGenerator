using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedClasses
{
    public class PropertyInfo
    {
        public (string Type, string Name, PropertyValidation Validation) GeneralInfo { get; set; } = new();
        public bool Localized { get; set; } = false;
        public (string prop, List<string> enumValues) EnumValues { get; set; } = new();
        public bool IsSaved { get; set; } = false;
    }
}
