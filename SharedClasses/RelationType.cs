using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedClasses
{
    public enum RelationType
    {
        OneToOne,
        OneToOneNullable,
        OneToMany,
        OneToManyNullable,
        ManyToOne,
        ManyToOneNullable,
        ManyToMany
    }
}
