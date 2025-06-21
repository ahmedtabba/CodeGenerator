using SharedClasses;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace InfrastructureGenerator
{
    public static class Infrastructure
    {
        private static readonly Regex ClassPattern = new Regex(
    @"public\s+class\s+(\w+)\s*\{",
    RegexOptions.Multiline | RegexOptions.IgnoreCase);
        /*
         public: Matches the keyword literally
        \s+: One or more whitespace characters
        class: Matches the keyword literally
        (\w+): Captures one or more word characters (the class name)
        \s*: Zero or more whitespace characters
        \{: Matches the opening brace literally
        The RegexOptions.Multiline | RegexOptions.IgnoreCase ensures we handle multi-line code and case variations correctly.
         */
        public static void UpdateAppDbContext(string entityName, string domainPath)
        {
            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";
            string contextPath = Path.Combine(domainPath, "..", "..", "Infrastructure", "Data", "AppDbContext.cs");
            string IContextPath = Path.Combine(domainPath, "..", "..", "Application", "Common", "Interfaces", "Db", "IApplicationDbContext.cs");
            if (!File.Exists(IContextPath))
            {
                //Console.WriteLine("⚠️ IApplicationDbContext.cs not found.");
                return;
            }
            if (!File.Exists(contextPath))
            {
                //Console.WriteLine("⚠️ AppDbContext.cs not found.");
                return;
            }
            string IdbSet = $"\t\tpublic DbSet<{entityName}> {entityPlural} {{ get;}}" +
                //$"\n\t\tpublic DbSet<{entityName}Localization> {entityName}sLocalization => Set<{entityName}Localization>();" +
                $"\n\t\t//Generate Here";

            var lines = File.ReadAllLines(IContextPath).ToList();
            var index = lines.FindIndex(line => line.Contains("//Generate Here"));

            if (index >= 0)
            {
                lines[index] = IdbSet;
                File.WriteAllLines(IContextPath, lines);
                //Console.WriteLine("✅ IApplicationDbContext updated.");
            }
            lines.Clear();
            index = -1;
            string dbSet = $"\t\tpublic DbSet<{entityName}> {entityPlural} => Set<{entityName}>();" +
                //$"\n\t\tpublic DbSet<{entityName}Localization> {entityName}sLocalization => Set<{entityName}Localization>();" +
                $"\n\t\t//Generate Here";

            lines = File.ReadAllLines(contextPath).ToList();
            index = lines.FindIndex(line => line.Contains("//Generate Here"));

            if (index >= 0)
            {
                lines[index] = dbSet;
                File.WriteAllLines(contextPath, lines);
                //Console.WriteLine("✅ AppDbContext updated.");
            }
        }

        public static void GenerateRepository(string entityName, string path, List<Relation>? relations = null)
        {
            string fileName = $"{entityName}Repository.cs";
            string filePath = Path.Combine(path, fileName);
            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";
            string? GetOneWithInclude = null;
            string? GetAllWithInclude = null;
            if (relations != null && relations.Any())
            {
                List<string> includeLines = new List<string>();
                foreach (Relation rel in relations)
                {
                    string entityRelatedPlural = rel.RelatedEntity.EndsWith("y") ? rel.RelatedEntity[..^1] + "ies" : rel.RelatedEntity + "s";
                    switch (rel.Type)
                    {
                        case RelationType.OneToOneSelfJoin:
                            includeLines.Add($"Include(x => x.{rel.RelatedEntity}Parent)");
                            break;
                        case RelationType.OneToOne:
                            includeLines.Add($"Include(x => x.{rel.RelatedEntity})");
                            break;
                        case RelationType.OneToOneNullable:
                            includeLines.Add($"Include(x => x.{rel.RelatedEntity})");
                            break;
                        case RelationType.OneToMany:
                            includeLines.Add($"Include(x => x.{entityRelatedPlural})");
                            break;
                        case RelationType.OneToManyNullable:
                            includeLines.Add($"Include(x => x.{entityRelatedPlural})");
                            break;

                        case RelationType.ManyToOne:
                            includeLines.Add($"Include(x => x.{rel.RelatedEntity})");
                            break;
                        case RelationType.ManyToOneNullable:
                            includeLines.Add($"Include(x => x.{rel.RelatedEntity})");
                            break;
                        case RelationType.ManyToMany:
                            includeLines.Add($"Include(x => x.{entityRelatedPlural})");
                            break;
                        default:
                            break;
                    }
                }
                GetOneWithInclude = $@"
        public async Task<{entityName}> Get{entityName}(Guid id)
        {{
            return await DbContext.{entityPlural}.
                {string.Join(".",includeLines)}
                .SingleOrDefaultAsync(x => x.Id == id);
        }}";

                GetAllWithInclude = $@"
        public IQueryable<{entityName}> Get{entityPlural}()
        {{
            return DbContext.{entityPlural}.
                {string.Join(".", includeLines)}
                .AsQueryable();
        }}";
            }
                string content = $@"
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces.IRepositories;
using Domain.Entities;
using Infrastructure.Data;

namespace Infrastructure.Repositories
{{
    public class {entityName}Repository : BaseRepository<{entityName}>, I{entityName}Repository
    {{
        public {entityName}Repository(AppDbContext dbContext) : base(dbContext)
        {{

        }}
{GetOneWithInclude}
{GetAllWithInclude}

       //Add necessary functions here if needed
    }}
}}
";
            File.WriteAllText(filePath, content);
        }

        public static void UpdateDependencyInjection(string entityName, string domainPath)
        {
            string dependencyInjectionPath = Path.Combine(domainPath, "..", "..", "Infrastructure", "DependencyInjection.cs");
            if (!File.Exists(dependencyInjectionPath))
            {
                //Console.WriteLine("⚠️ Infrastructure DependencyInjection.cs not found.");
                return;
            }
            string register = $"\t\t\tservices.AddScoped<I{entityName}Repository, {entityName}Repository>();" +
                //$"\n\t\tpublic DbSet<{entityName}Localization> {entityName}sLocalization => Set<{entityName}Localization>();" +
                $"\n\t\t\t//Register Here";

            var lines = File.ReadAllLines(dependencyInjectionPath).ToList();
            var index = lines.FindIndex(line => line.Contains("//Register Here"));

            if (index >= 0)
            {
                lines[index] = register;
                File.WriteAllLines(dependencyInjectionPath, lines);
                //Console.WriteLine("✅ Infrastructure DependencyInjection updated.");
            }
        }

        public static void GenerateConfiguration(string entityName, string domainPath,List<string> breakEntitiesNames)
        {
            string configurationsPath = Path.Combine(domainPath, "..", "..", "Infrastructure", "Data", "Configurations");
            string fileName = $"{entityName}Configuration.cs";
            string filePath = Path.Combine(configurationsPath, fileName);
            StringBuilder contentBreakEntitiesNames = breakEntitiesNames.Count>0 ? new StringBuilder() : null!;

            foreach (var item in breakEntitiesNames)
            {
                string itemPlural = item.EndsWith("y") ? item[..^1] + "ies" : item + "s";
                string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";
                contentBreakEntitiesNames
                    .Append($"\t\t\tbuilder.HasMany(l => l.{itemPlural})\n")
                    .Append($"\t\t\t\t.WithMany(r => r.{entityPlural})\n")
                    .Append($"\t\t\t\t.UsingEntity<{entityName}{item}>();\n");
            }


            string content = $@"                           
using Domain.Entities;                                     
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.Configurations
{{
    public class {entityName}Configuration :IEntityTypeConfiguration<{entityName}>
    {{
        public void Configure(EntityTypeBuilder<{entityName}> builder)
        {{
            builder.HasKey(e => e.Id);
{contentBreakEntitiesNames}
        }}

    }}
}}

";
            File.WriteAllText(filePath, content);
        }

        public static void GeneratePermission(string entityName, string domainPath,bool haslocalization)
        {
            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";
            string roleConsistentPath = Path.Combine(domainPath, "..", "..", "Infrastructure", "Utilities", "RoleConsistent.cs");
            if (!File.Exists(roleConsistentPath))
            {
                //Console.WriteLine("⚠️ RoleConsistent.cs not found.");
                return;
            }

            string content = File.ReadAllText(roleConsistentPath);
            string className = $"public class {entityName}";
            string consistentClass = !haslocalization ? $@"
        public class {entityName}
        {{
            public const string Browse = @""{entityName}\Browse {entityName}"";
            public const string Delete = @""{entityName}\Delete {entityName}"";
            public const string Add = @""{entityName}\Add {entityName}"";
            public const string Edit = @""{entityName}\Edit {entityName}"";
            public List<string> Roles = [Delete, Add, Edit, Browse];
        }}
"
        : $@"
        public class {entityName}
        {{
            public const string Browse = @""{entityName}\Browse {entityName}"";
            public const string Delete = @""{entityName}\Delete {entityName}"";
            public const string Add = @""{entityName}\Add {entityName}"";
            public const string Edit = @""{entityName}\Edit {entityName}"";
            public const string BrowseWithLocalization = @""{entityName}\Browse {entityName} With Localization"";
            public List<string> Roles = [Delete, Add, Edit, Browse,BrowseWithLocalization];
        }}
";

            var matches = ClassPattern.Matches(content);
            foreach (Match match in matches)
            {
                if (match.Groups[1].Value == entityName)
                {
                    //Console.WriteLine($"⚠️ RoleConsistent already contains Roles for {entityName}.");
                    return;
                }
            }

            //if (content.Contains(className))
            //{
            //    Console.WriteLine($"⚠️ RoleConsistent already contains routes for {entityName}.");
            //    return;
            //}

            // Add before Dictionary
            int insertIndex = content.LastIndexOf("public static Dictionary") - 1;

            if (insertIndex < 0)
            {
                //Console.WriteLine("❌ Failed to find insertion point in Roles");
                return;
            }
            content = content.Insert(insertIndex, "\n" + consistentClass + "\n\t");
            File.WriteAllText(roleConsistentPath, content);

            string roleGroup = $"\t\t\tGroups.Add(\"{entityPlural}\", new List<string>() {{ \"{entityName}\" }});"
                + $"\n\t\t\t//Add To Group Here";
            var lines = File.ReadAllLines(roleConsistentPath).ToList();
            var index = lines.FindIndex(line => line.Contains("//Add To Group Here"));
            if (index >= 0)
            {
                lines[index] = roleGroup;
                File.WriteAllLines(roleConsistentPath, lines);
            }

            var initialiserPath = Path.Combine(domainPath, "..", "..", "Infrastructure", "Data", "ApplicationDbContextInitialiser.cs");
            if (!File.Exists(initialiserPath))
            {
                //Console.WriteLine("⚠️ ApplicationDbContextInitialiser.cs not found.");
                return;
            }
            
            string roleAdd = !haslocalization ? $@"
            #region {entityName}

            if (!roles.Exists(r => r.Name == RoleConsistent.{entityName}.Add))
                _identityContext.Roles.Add(new ApplicationRole {{Name = RoleConsistent.{entityName}.Add,NormalizedName = RoleConsistent.{entityName}.Add.ToUpper()}});

            if (!roles.Exists(r => r.Name == RoleConsistent.{entityName}.Edit))
                _identityContext.Roles.Add(new ApplicationRole {{Name = RoleConsistent.{entityName}.Edit,NormalizedName = RoleConsistent.{entityName}.Edit.ToUpper()}});

            if (!roles.Exists(r => r.Name == RoleConsistent.{entityName}.Browse))
                _identityContext.Roles.Add(new ApplicationRole {{Name = RoleConsistent.{entityName}.Browse,NormalizedName = RoleConsistent.{entityName}.Browse.ToUpper()}});

            if (!roles.Exists(r => r.Name == RoleConsistent.{entityName}.Delete))
                _identityContext.Roles.Add(new ApplicationRole {{Name = RoleConsistent.{entityName}.Delete,NormalizedName = RoleConsistent.{entityName}.Delete.ToUpper()}});

            #endregion
" + $"\n\t\t\t//Add Permission Here"
        : $@"
            #region {entityName}

            if (!roles.Exists(r => r.Name == RoleConsistent.{entityName}.Add))
                _identityContext.Roles.Add(new ApplicationRole {{Name = RoleConsistent.{entityName}.Add,NormalizedName = RoleConsistent.{entityName}.Add.ToUpper()}});

            if (!roles.Exists(r => r.Name == RoleConsistent.{entityName}.Edit))
                _identityContext.Roles.Add(new ApplicationRole {{Name = RoleConsistent.{entityName}.Edit,NormalizedName = RoleConsistent.{entityName}.Edit.ToUpper()}});

            if (!roles.Exists(r => r.Name == RoleConsistent.{entityName}.Browse))
                _identityContext.Roles.Add(new ApplicationRole {{Name = RoleConsistent.{entityName}.Browse,NormalizedName = RoleConsistent.{entityName}.Browse.ToUpper()}});

            if (!roles.Exists(r => r.Name == RoleConsistent.{entityName}.Delete))
                _identityContext.Roles.Add(new ApplicationRole {{Name = RoleConsistent.{entityName}.Delete,NormalizedName = RoleConsistent.{entityName}.Delete.ToUpper()}});

            if (!roles.Exists(r => r.Name == RoleConsistent.{entityName}.BrowseWithLocalization))
                _identityContext.Roles.Add(new ApplicationRole {{Name = RoleConsistent.{entityName}.BrowseWithLocalization,NormalizedName = RoleConsistent.{entityName}.BrowseWithLocalization.ToUpper()}});
            #endregion
" + $"\n\t\t\t//Add Permission Here";

            lines.Clear();
            lines = File.ReadAllLines(initialiserPath).ToList();
            index = lines.FindIndex(line => line.Contains("//Add Permission Here"));

            if (index >= 0)
            {
                lines[index] = roleAdd;
                File.WriteAllLines(initialiserPath, lines);
            }
            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            lines.Clear();
            lines = File.ReadAllLines(initialiserPath).ToList();
            index = lines.FindIndex(line => line.Contains("//Add To rolesAfterInitialize Here"));
            string rolesAfterInitialize = $"\t\t\tvar {lowerEntityName}Roles = rolesAfterInitialize.Where(r => r.Name.StartsWith(@\"{entityName}\\\"));"
                + $"\n\t\t\t//Add To rolesAfterInitialize Here";
            if (index >= 0)
            {
                lines[index] = rolesAfterInitialize;
                File.WriteAllLines(initialiserPath, lines);
            }

            lines.Clear();
            lines = File.ReadAllLines(initialiserPath).ToList();
            index = lines.FindIndex(line => line.Contains("//Except rolesAfterInitialize Here"));
            string exceptRolesAfterInitialize = $"\t\t\trolesAfterInitialize = rolesAfterInitialize.Except<IdentityRole>({lowerEntityName}Roles).ToList();"
                + $"\n\t\t\t//Except rolesAfterInitialize Here";
            if (index >= 0)
            {
                lines[index] = exceptRolesAfterInitialize;
                File.WriteAllLines(initialiserPath, lines);
            }

            lines.Clear();
            lines = File.ReadAllLines(initialiserPath).ToList();
            index = lines.FindIndex(line => line.Contains("//Add RoleConsistent Loop"));
            string roleConsistentLoop = $@"
            foreach (var role in {lowerEntityName}Roles)
            {{
                if (!new RoleConsistent.{entityName}().Roles.Contains(role.Name))
                {{
                    _identityContext.Roles.Remove(role);
                }}
            }}
" +  $"\n\t\t\t//Add RoleConsistent Loop";
            if (index >= 0)
            {
                lines[index] = roleConsistentLoop;
                File.WriteAllLines(initialiserPath, lines);
            }
        }

        public static void UpdateLocalizationService(string entityName, string domainPath,List<string> localizedProperties)
        {
            string ILocalizationServicePath = Path.Combine(domainPath, "..", "..", "Application", "Common", "Interfaces", "Services", "ILocalizationService.cs");
            if (!File.Exists(ILocalizationServicePath))
            {
                //Console.WriteLine("⚠️ ILocalizationService.cs not found.");
                return;
            }
            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";
            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            string usingNamespaces = $@"
using Application.{entityPlural}.Queries.Get{entityName}Query;
using Application.{entityPlural}.Queries.Get{entityPlural}WithPagination;
" + "\n//Add Using Here";
            string methodsDefine = $"\t\tpublic Task Fill{entityName}Localization(Get{entityName}Dto dto, Guid languageId);" + "\n" + $"\t\tpublic Task Fill{entityName}Localization(PaginatedList<Get{entityPlural}WithPaginationDto> list, Guid languageId);" + 
                $"\n\t\t//Define Localization Method Here";
            var lines = File.ReadAllLines(ILocalizationServicePath).ToList();
            var index = lines.FindIndex(line => line.Contains("Define Localization Method Here"));

            if (index >= 0)
            {
                lines[index] = methodsDefine;
                File.WriteAllLines(ILocalizationServicePath, lines);
            }

            lines.Clear();
            lines = File.ReadAllLines(ILocalizationServicePath).ToList();
            index = lines.FindIndex(line => line.Contains("//Add Using Here"));
            if (index >= 0)
            {
                lines[index] = usingNamespaces;
                File.WriteAllLines(ILocalizationServicePath, lines);
                //Console.WriteLine("✅ ILocalizationService updated.");
            }

            string LocalizationServicePath = Path.Combine(domainPath, "..", "..", "Infrastructure", "Services", "LocalizationService.cs");
            if (!File.Exists(LocalizationServicePath))
            {
                //Console.WriteLine("⚠️ LocalizationService.cs not found.");
                return;
            }
            string privateField = $"\t\tprivate readonly I{entityName}LocalizationRepository _{lowerEntityName}LocalizationRepository;" +
                "\n\t\t//Add Private Field Here";
            string injectService = $"\n\t\t\t\t\t\t\t\t\t,I{entityName}LocalizationRepository {lowerEntityName}LocalizationRepository" +
                "\n\t\t\t\t\t\t\t\t\t//Inject Service Here";
            string field = $"\t\t\t_{lowerEntityName}LocalizationRepository = {lowerEntityName}LocalizationRepository;" +
                "\n\t\t\t//Add Field Here";
            usingNamespaces = $@"
using Application.{entityPlural}.Queries.Get{entityName}Query;
using Application.{entityPlural}.Queries.Get{entityPlural}WithPagination;
" +
    "\n//Add Using Here";
            lines.Clear();
            lines = File.ReadAllLines(LocalizationServicePath).ToList();
            index = lines.FindIndex(line => line.Contains("//Add Private Field Here"));

            if (index >= 0)
            {
                lines[index] = privateField;
                File.WriteAllLines(LocalizationServicePath, lines);
            }
            lines.Clear();
            lines = File.ReadAllLines(LocalizationServicePath).ToList();
            index = lines.FindIndex(line => line.Contains("//Add Using Here"));

            if (index >= 0)
            {
                lines[index] = usingNamespaces;
                File.WriteAllLines(LocalizationServicePath, lines);
            }
            lines.Clear();
            lines = File.ReadAllLines(LocalizationServicePath).ToList();
            index = lines.FindIndex(line => line.Contains("//Inject Service Here"));

            if (index >= 0)
            {
                lines[index] = injectService;
                File.WriteAllLines(LocalizationServicePath, lines);
            }
            lines.Clear();
            lines = File.ReadAllLines(LocalizationServicePath).ToList();
            index = lines.FindIndex(line => line.Contains("//Add Field Here"));

            if (index >= 0)
            {
                lines[index] = field;
                File.WriteAllLines(LocalizationServicePath, lines);
            }

            StringBuilder localizedIfs = new StringBuilder();
            foreach (var prop in localizedProperties)
            {
                localizedIfs.Append($@"
                    dto.{prop} = {lowerEntityName}Localization.FirstOrDefault(x => x.FieldType == (int){entityName}LocalizationFieldType.{prop}) != null
                        ? {lowerEntityName}Localization.FirstOrDefault(x => x.FieldType == (int){entityName}LocalizationFieldType.{prop})!.Value
                        : dto.{prop};");
                localizedIfs.AppendLine();
            }

            string implementMethods = $@"
        public async Task Fill{entityName}Localization(Get{entityName}Dto dto, Guid languageId)
        {{
            var {lowerEntityName}Localization = await _{lowerEntityName}LocalizationRepository.GetAll()
                 .Where(x => x.{entityName}Id == dto.Id && x.LanguageId == languageId)
                 .ToListAsync();

            if ({lowerEntityName}Localization.Count > 0)
            {{
                {localizedIfs}
            }}
        }}

        public async Task Fill{entityName}Localization(PaginatedList<Get{entityPlural}WithPaginationDto> list, Guid languageId)
        {{
            foreach (var dto in list.Items)
            {{
                var {lowerEntityName}Localization = await _{lowerEntityName}LocalizationRepository.GetAll()
                 .Where(x => x.{entityName}Id == dto.Id && x.LanguageId == languageId)
                 .ToListAsync();

                if ({lowerEntityName}Localization.Count > 0)
                {{
                    {localizedIfs}
                }}
            }}
        }}
" +
    "\n\t\t//Implement Method Here";

            lines.Clear();
            lines = File.ReadAllLines(LocalizationServicePath).ToList();
            index = lines.FindIndex(line => line.Contains("//Implement Method Here"));

            if (index >= 0)
            {
                lines[index] = implementMethods;
                File.WriteAllLines(LocalizationServicePath, lines);
            }
        }
    }
}
