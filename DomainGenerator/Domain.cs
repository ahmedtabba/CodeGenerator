
using SharedClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace DomainGenerator
{
    public static class Domain
    {
        public static void GenerateEntityClass(string entityName, string path, (List<(string Type, string Name, PropertyValidation Validation)>,List<string> localizedProps, List<(string prop, List<string> enumValues)>) properties, bool hasLocalization, List<Relation> relations)
        {
            string fileName = $"{entityName}.cs";
            string filePath = Path.Combine(path, fileName);
            var propList = new List<string>();
            foreach(var prop in properties.Item1)
            {
                switch(prop.Type) 
                {
                    case "GPG":
                        if (prop.Validation != null && prop.Validation.Required)
                            propList.Add($"        public {prop.Type} {prop.Name} {{ get; set; }}");
                        else
                            propList.Add($"        public {prop.Type}? {prop.Name} {{ get; set; }}");
                        break;
                    case "PNGs":
                        propList.Add($"        public {prop.Type} {prop.Name} {{ get; set; }} = new {prop.Type}();");
                        break;
                    case "VD":
                        if (prop.Validation != null && prop.Validation.Required)
                            propList.Add($"        public {prop.Type} {prop.Name} {{ get; set; }}");
                        else
                            propList.Add($"        public {prop.Type}? {prop.Name} {{ get; set; }}");
                        break;
                    case "VDs":
                        propList.Add($"        public {prop.Type} {prop.Name} {{ get; set; }} = new {prop.Type}();");
                        break;
                    case "FL":
                        if (prop.Validation != null && prop.Validation.Required)
                            propList.Add($"        public {prop.Type} {prop.Name} {{ get; set; }}");
                        else
                            propList.Add($"        public {prop.Type}? {prop.Name} {{ get; set; }}");
                        break;
                    case "FLs":
                        propList.Add($"        public {prop.Type} {prop.Name} {{ get; set; }} = new {prop.Type}();");
                        break;

                    case var type when type.StartsWith("virtual"):
                        propList.Add($"        public {prop.Type} {prop.Name} {{ get; set; }} = new {prop.Name}();");
                        break;

                    default:
                        propList.Add($"        public {prop.Type} {prop.Name} {{ get; set; }}");
                        break;
                }
            }
            var tempProps = string.Join(Environment.NewLine, propList);

    //        var tempProps = string.Join(Environment.NewLine, properties.Item1.Select(p =>
    //    (p.Type == "PNGs")
    //     ? $"        public {p.Type} {p.Name} {{ get; set; }} = new {p.Type}();" 
    //     : (p.Type == "GPG" && p.Validation == null)
    //     ? $"        public {p.Type}? {p.Name} {{ get; set; }}"
    //     : $"        public {p.Type} {p.Name} {{ get; set; }}"
    //));

            var props = tempProps.Replace("GPG", "string").Replace("PNGs", "List<string>").Replace("VDs", "List<string>").Replace("VD", "string").Replace("FLs", "List<string>").Replace("FL", "string");
            //var propsList = properties.Item1.Any(p => p.Type == "VD") ? properties.Item1.Any(p => p.Type == "VD" && p.Validation != null) ? props.Replace("VD", "string") : props.Replace("VD", "string?") : props;
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
                GenerateEntityLocalizationClass(entityName, path,properties.Item2);
                UpdateLanguageClass($"{entityName}Localization", path);
            }

            if (properties.Item3.Any())
                GenerateEntityEnums(entityName, properties.Item3, path);
            if (relations.Count > 0)
                UpdateRelations(entityName,relations, path);


        }
        static void GenerateEntityEnums(string entityName, List<(string prop, List<string> enumValues)> enumProps, string path)
        {
            foreach (var enumProp in enumProps)
            {
                string filePropEnumName = $"{entityName}{enumProp.prop}.cs";
                string filePropEnumPath = Path.Combine(path, "..", "Enums");
                string fileEnumPath = Path.Combine(filePropEnumPath, filePropEnumName);
                StringBuilder values = new StringBuilder();
                foreach (var item in enumProp.enumValues)
                {
                    values.Append("\t\t" + item + ",");
                    values.AppendLine();
                }
                string enumContent = $@"using System;
namespace Domain.Enums
{{
    public enum {entityName}{enumProp.prop}
    {{
{values}
    }}
}}
";
                File.WriteAllText(fileEnumPath, enumContent);
            }
            
        }
        static void GenerateEntityLocalizationClass(string entityName, string path, List<string> localizedProp)
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
        public int FieldType {{ get; set; }}
    }}
}}";
            File.WriteAllText(filePath, content);

            string fileLocalizationAppName = $"{entityName}LocalizationApp.cs";
            string localizationAppPath = Path.Combine(path, "..", "..", "Application", "Common", "Models", "Localization");
            string fileLocalizationAppPath = Path.Combine(localizationAppPath, fileLocalizationAppName);
            string localizationAppContent = $@"using System;
using Domain.Enums;

namespace Application.Common.Models.Localization
{{
    public class {entityName}LocalizationApp
    {{
        public Guid LanguageId {{ get; set; }}
        public string Value {{ get; set; }} = null!;
        public {entityName}LocalizationFieldType FieldType {{ get; set; }}
    }}
}}
";
            File.WriteAllText(fileLocalizationAppPath, localizationAppContent);
            string fileLocalizationEnumName = $"{entityName}LocalizationFieldType.cs";
            string localizationEnumPath = Path.Combine(path, "..", "Enums");
            string fileLocalizationEnumPath = Path.Combine(localizationEnumPath, fileLocalizationEnumName);
            StringBuilder enumContent = new StringBuilder();
            foreach (var prop in localizedProp)
            {
                enumContent.Append(prop);
                enumContent.Append(", ");
            }
            string localizationEnumContent = $@"using System;
namespace Domain.Enums
{{
    public enum {entityName}LocalizationFieldType
    {{
        {enumContent}
    }}
}}
";
            File.WriteAllText(fileLocalizationEnumPath, localizationEnumContent);
        }

        static void UpdateLanguageClass(string entityName,string domainPath)
        {
            string languagePath = Path.Combine(domainPath, "Language.cs");
            if (!File.Exists(languagePath))
            {
                //Console.WriteLine("⚠️ Language.cs not found.");
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
                //Console.WriteLine("✅ Language updated.");
            }
        }

        static void UpdateRelations(string entityName, List<Relation> relations,string domainPath)
        {
            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";
            string fileEntityPath = Path.Combine(domainPath, $"{entityName}.cs");
            string contentEntity = File.ReadAllText(fileEntityPath);
            int insertEntityIndex = contentEntity.LastIndexOf("}") -3;
            string? contentRelatedEntity = null;
            int insertRelatedEntityIndex = 0;
            string entityRelatedPlural = null;
            foreach (var relation in relations)
            {
                string? fileRelatedEntityPath = null;
                if (relation.RelatedEntity != "User")
                {
                    fileRelatedEntityPath = Path.Combine(domainPath, $"{relation.RelatedEntity}.cs");
                    if (fileRelatedEntityPath != null && !File.Exists(fileRelatedEntityPath))
                    {
                        //Console.WriteLine($"⚠️ {relation.RelatedEntity}.cs not found.");
                        return;
                    }
                    contentRelatedEntity = File.ReadAllText(fileRelatedEntityPath);
                    // Add before the last closing brace
                    insertRelatedEntityIndex = contentRelatedEntity.LastIndexOf("}") - 3;
                    entityRelatedPlural = relation.RelatedEntity.EndsWith("y") ? relation.RelatedEntity[..^1] + "ies" : relation.RelatedEntity + "s";
                }
                switch (relation.Type)
                {
                    case RelationType.OneToOneSelfJoin:
                        contentEntity = contentEntity.Insert(insertEntityIndex, "\n" + "\t\t" + $"public virtual {entityName}? {entityName}Parent {{ get; set; }}" + "\n" + "\t\t" + $"public Guid? {entityName}ParentId {{ get; set; }}" + "\n\t");
                        File.WriteAllText(fileEntityPath, contentEntity);
                        break;
                    case RelationType.OneToOne:
                        contentRelatedEntity = contentRelatedEntity.Insert(insertRelatedEntityIndex, "\n" + "\t\t" + $"public virtual {entityName}? {entityName} {{ get; set; }}" + "\n\t");
                        contentEntity = contentEntity.Insert(insertEntityIndex, "\n" + "\t\t" + $"public virtual {relation.RelatedEntity} {relation.RelatedEntity} {{ get; set; }}" + "\n" + "\t\t" + $"public Guid {relation.RelatedEntity}Id {{ get; set; }}" + "\n\t");
                        File.WriteAllText(fileRelatedEntityPath, contentRelatedEntity);
                        File.WriteAllText(fileEntityPath, contentEntity);
                        break;
                    case RelationType.OneToOneNullable:
                        contentRelatedEntity = contentRelatedEntity.Insert(insertRelatedEntityIndex, "\n" + "\t\t" + $"public virtual {entityName}? {entityName} {{ get; set; }}" + "\n\t");
                        contentEntity = contentEntity.Insert(insertEntityIndex, "\n" + "\t\t" + $"public virtual {relation.RelatedEntity}? {relation.RelatedEntity} {{ get; set; }}" + "\n" + "\t\t" + $"public Guid? {relation.RelatedEntity}Id {{ get; set; }}" + "\n\t");
                        File.WriteAllText(fileRelatedEntityPath, contentRelatedEntity);
                        File.WriteAllText(fileEntityPath, contentEntity);
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
                    case RelationType.UserSingle:
                        contentEntity = contentEntity.Insert(insertEntityIndex, "\n" + "\t\t" + $"public string {relation.DisplayedProperty}Id {{ get; set; }}" + "\n\t");
                        File.WriteAllText(fileEntityPath, contentEntity);
                        break;
                    case RelationType.UserSingleNullable:
                        contentEntity = contentEntity.Insert(insertEntityIndex, "\n" + "\t\t" + $"public string? {relation.DisplayedProperty}Id {{ get; set; }}" + "\n\t");
                        File.WriteAllText(fileEntityPath, contentEntity);
                        break;
                    case RelationType.UserMany:
                        contentEntity = contentEntity.Insert(insertEntityIndex, "\n" + "\t\t" + $"public virtual ICollection<{entityName}{relation.DisplayedProperty}> {entityName}{relation.DisplayedProperty.GetPluralName()} {{ get; set; }} = new List<{entityName}{relation.DisplayedProperty}>();" + "\n\t");
                        File.WriteAllText(fileEntityPath, contentEntity);
                        break;
                    default: 
                        break;
                }

            }
        }
    }


}
