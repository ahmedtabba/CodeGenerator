using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SharedClasses
{
    public class PropertyMetadata
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public PropertyValidation Validation { get; set; }
    }

    public class EntityMetadata
    {
        public string Name { get; set; }
        public string PluralName { get; set; }
        public bool HasLocalization { get; set; }
        public bool HasPermissions { get; set; }
        public bool HasVersioning { get; set; }
        public bool HasNotification { get; set; }
        public bool HasUserAction { get; set; }
        public bool HasBulk { get; set; }
        public List<PropertyMetadata> Properties { get; set; } = new List<PropertyMetadata>();
        public List<string> LocalizedProperties { get; set; } = new List<string>();
        public List<(string prop, List<string> enumValues)> EnumProperties { get; set; } = new List<(string prop, List<string> enumValues)>();
        public List<Relation> Relations { get; set; } = new List<Relation>();
        public DateTime GeneratedAt { get; set; }
        public bool? IsParent { get; set; }
        public bool? IsChild { get; set; }
        public string? ParentEntityName { get; set; }
    }

    public class ProjectMetadata
    {
        public string ProjectName { get; set; }
        public string ProjectPath { get; set; }
        public List<EntityMetadata> Entities { get; set; } = new List<EntityMetadata>();
        public DateTime LastUpdated { get; set; }
    }
} 