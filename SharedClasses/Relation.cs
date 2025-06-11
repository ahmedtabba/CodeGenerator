using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedClasses
{
    public class Relation
    {
        public string RelatedEntity { get; set; } = null!;
        public RelationType Type { get; set; }
        public string DisplayedProperty { get; set; } = null!;
        public bool HiddenInTable { get; set; } = false;
    }
}
