using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedClasses
{
    public class PropertyValidation
    {
        public bool Required { get; set; } = false;
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public int? MinRange { get; set; }
        public int? MaxRange { get; set; }
    }
}
