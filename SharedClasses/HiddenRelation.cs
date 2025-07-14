using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedClasses
{
    public class HiddenRelation
    {
        public string RelatedEntityName { get; set; } = null!;
        public string DisplayedProperty { get; set; } = null!;
        public RelationType RelationType { get; set; } 
    }
}
