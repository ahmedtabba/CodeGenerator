
using SharedClasses;
using System.Collections.Generic;

namespace DomainGenerator
{
    public static class Domain
    {
        public static void GenerateEntityClass(string entityName, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, bool hasLocalization, List<Relation> relations)
        {
            string fileName = $"{entityName}.cs";
            string filePath = Path.Combine(path, fileName);

            var props = string.Join(Environment.NewLine, properties.Select(p => $"        public {p.Type} {p.Name} {{ get; set; }}"));

            string content = null!;
            if (!hasLocalization) 
            {
                    content = $@"using Domain.Common;
using System;
using System.Collections.Generic;

namespace Domain.Entities
{{
    public class {entityName} : BaseAuditableEntity
    {{
        public {entityName}() : base()
        {{

        }}
{props}
    }}
}}";

                    File.WriteAllText(filePath, content);
                
            }
            else
            {
                content = $@"using Domain.Common;
using System;
using System.Collections.Generic;

namespace Domain.Entities
{{
    public class {entityName} : BaseAuditableEntity
    {{
        public {entityName}() : base()
        {{

        }}
{props}
        public virtual ICollection<{entityName}Localization> {entityName}Localizations {{ get; set; }} = new List<{entityName}Localization>();
    }}
}}";

                File.WriteAllText(filePath, content);
                GenerateEntityLocalizationClass(entityName, path);
                UpdateLanguageClass($"{entityName}Localization", path);
            }
            if (relations.Count > 0)
                UpdateRelations(entityName,relations, path);


        }

        static void GenerateEntityLocalizationClass(string entityName, string path)
        {
            string fileName = $"{entityName}Localization.cs";
            string filePath = Path.Combine(path, fileName);

            string content = $@"using Domain.Common;
using System;

namespace Domain.Entities
{{
    public class {entityName}Localization : BaseAuditableEntity
    {{
        public {entityName}Localization() : base()
        {{

        }}
        public Guid {entityName}Id {{ get; set; }}
        public Guid LanguageId {{ get; set; }}
        public string Value {{ get; set; }} = null!;
    }}
}}";
            File.WriteAllText(filePath, content);
        }

        static void UpdateLanguageClass(string entityName,string domainPath)
        {
            string languagePath = Path.Combine(domainPath, "Language.cs");
            if (!File.Exists(languagePath))
            {
                Console.WriteLine("⚠️ Language.cs not found.");
                return;
            }
            string ICollection = $"\t\tpublic virtual ICollection<{entityName}> {entityName}s {{ get; set; }} = new List<{entityName}>();" +
                //$"\n\t\tpublic DbSet<{entityName}Localization> {entityName}sLocalization => Set<{entityName}Localization>();" +
                $"\n\t\t//Generate Here";
            var lines = File.ReadAllLines(languagePath).ToList();
            var index = lines.FindIndex(line => line.Contains("//Generate Here"));

            if (index >= 0)
            {
                lines[index] = ICollection;
                File.WriteAllLines(languagePath, lines);
                Console.WriteLine("✅ Language updated.");
            }
        }

        static void UpdateRelations(string entityName, List<Relation> relations,string domainPath)
        {
            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";
            string fileEntityPath = Path.Combine(domainPath, $"{entityName}.cs");
            string contentEntity = File.ReadAllText(fileEntityPath);
            int insertEntityIndex = contentEntity.LastIndexOf("}") -3;
            foreach (var relation in relations)
            {
                var fileRelatedEntityPath =Path.Combine(domainPath, $"{relation.RelatedEntity}.cs") ;
                if (!File.Exists(fileRelatedEntityPath))
                {
                    Console.WriteLine($"⚠️ {relation.RelatedEntity}.cs not found.");
                    return;
                }
                string contentRelatedEntity = File.ReadAllText(fileRelatedEntityPath);
                // Add before the last closing brace
                int insertRelatedEntityIndex = contentRelatedEntity.LastIndexOf("}") -3;
                string entityRelatedPlural = relation.RelatedEntity.EndsWith("y") ? relation.RelatedEntity[..^1] + "ies" : relation.RelatedEntity + "s";
                switch (relation.Type)
                {
                    case RelationType.OneToOne:
                        contentRelatedEntity = contentRelatedEntity.Insert(insertRelatedEntityIndex, "\n" + "\t\t" + $"public virtual {entityName} {entityName} {{ get; set; }}" + "\n" + "\t\t" + $"public Guid {entityName}Id {{ get; set; }}" + "\n\t");
                        File.WriteAllText(fileRelatedEntityPath, contentRelatedEntity);
                        break;
                    case RelationType.OneToOneNullable:
                        contentRelatedEntity = contentRelatedEntity.Insert(insertRelatedEntityIndex, "\n" + "\t\t" + $"public virtual {entityName}? {entityName} {{ get; set; }}" + "\n" + "\t\t" + $"public Guid? {entityName}Id {{ get; set; }}" + "\n\t");
                        File.WriteAllText(fileRelatedEntityPath, contentRelatedEntity);
                        break;
                    case RelationType.OneToMany:
                        contentRelatedEntity = contentRelatedEntity.Insert(insertRelatedEntityIndex, "\n" + "\t\t" + $"public virtual {entityName} {entityName} {{ get; set; }}" + "\n" + "\t\t" + $"public Guid {entityName}Id {{ get; set; }}" + "\n\t");
                        contentEntity = contentEntity.Insert(insertEntityIndex, "\n" + "\t\t" + $"public virtual ICollection<{relation.RelatedEntity}> {entityRelatedPlural} {{ get; set; }} = new List<{relation.RelatedEntity}>();" + "\n\t");
                        File.WriteAllText(fileRelatedEntityPath, contentRelatedEntity);
                        File.WriteAllText(fileEntityPath, contentEntity);
                        break;
                    case RelationType.OneToManyNullable:
                        contentRelatedEntity = contentRelatedEntity.Insert(insertRelatedEntityIndex, "\n" + "\t\t" + $"public virtual {entityName}? {entityName} {{ get; set; }}" + "\n" + "\t\t" + $"public Guid? {entityName}Id {{ get; set; }}" + "\n\t");
                        contentEntity = contentEntity.Insert(insertEntityIndex, "\n" + "\t\t" + $"public virtual ICollection<{relation.RelatedEntity}> {entityRelatedPlural} {{ get; set; }} = new List<{relation.RelatedEntity}>();" + "\n\t");
                        File.WriteAllText(fileRelatedEntityPath, contentRelatedEntity);
                        File.WriteAllText(fileEntityPath, contentEntity);
                        break;
                    case RelationType.ManyToOne:
                        contentRelatedEntity = contentRelatedEntity.Insert(insertRelatedEntityIndex, "\n" + "\t\t" + $"public virtual ICollection<{entityName}> {entityPlural} {{ get; set; }} = new List<{entityName}>();" + "\n\t");
                        contentEntity = contentEntity.Insert(insertEntityIndex, "\n" + "\t\t" + $"public virtual {relation.RelatedEntity} {relation.RelatedEntity} {{ get; set; }}" + "\n" + "\t\t" + $"public Guid {relation.RelatedEntity}Id {{ get; set; }}" + "\n\t");
                        File.WriteAllText(fileRelatedEntityPath, contentRelatedEntity);
                        File.WriteAllText(fileEntityPath, contentEntity);
                        break;
                    case RelationType.ManyToOneNullable:
                        contentRelatedEntity = contentRelatedEntity.Insert(insertRelatedEntityIndex, "\n" + "\t\t" + $"public virtual ICollection<{entityName}> {entityPlural} {{ get; set; }} = new List<{entityName}>();" + "\n\t");
                        contentEntity = contentEntity.Insert(insertEntityIndex, "\n" + "\t\t"+$"public virtual {relation.RelatedEntity}? {relation.RelatedEntity} {{ get; set; }}" + "\n" + "\t\t"+$"public Guid? {relation.RelatedEntity}Id {{ get; set; }}" + "\n\t");
                        File.WriteAllText(fileRelatedEntityPath, contentRelatedEntity);
                        File.WriteAllText(fileEntityPath, contentEntity);
                        break;
                    case RelationType.ManyToMany:
                        contentRelatedEntity = contentRelatedEntity.Insert(insertRelatedEntityIndex, "\n" + "\t\t"+ $"public virtual ICollection<{entityName}> {entityPlural} {{ get; set; }} = new List<{entityName}>();" + "\n\t");
                        contentEntity = contentEntity.Insert(insertEntityIndex, "\n" + "\t\t"+$"public virtual ICollection<{relation.RelatedEntity}> {entityRelatedPlural} {{ get; set; }} = new List<{relation.RelatedEntity}>();"+"\n\t");
                        File.WriteAllText(fileRelatedEntityPath, contentRelatedEntity);
                        File.WriteAllText(fileEntityPath, contentEntity);
                        break;
                    default: 
                        break;
                }

            }
        }
    }


}
