using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace InfrastructureGenerator
{
    public static class Infrastructure
    {
        public static void UpdateAppDbContext(string entityName, string domainPath)
        {
            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";
            string contextPath = Path.Combine(domainPath, "..", "..", "Infrastructure", "Data", "AppDbContext.cs");
            string IContextPath = Path.Combine(domainPath, "..", "..", "Application", "Common", "Interfaces", "Db", "IApplicationDbContext.cs");
            if (!File.Exists(IContextPath))
            {
                Console.WriteLine("⚠️ IApplicationDbContext.cs not found.");
                return;
            }
            if (!File.Exists(contextPath))
            {
                Console.WriteLine("⚠️ AppDbContext.cs not found.");
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
                Console.WriteLine("✅ IApplicationDbContext updated.");
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
                Console.WriteLine("✅ AppDbContext updated.");
            }
        }

        public static void GenerateRepository(string entityName, string path)
        {
            string fileName = $"{entityName}Repository.cs";
            string filePath = Path.Combine(path, fileName);

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
                Console.WriteLine("⚠️ Infrastructure DependencyInjection.cs not found.");
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
                Console.WriteLine("✅ Infrastructure DependencyInjection updated.");
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

        public static void GeneratePermission(string entityName, string domainPath)
        {
            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";
            string roleConsistentPath = Path.Combine(domainPath, "..", "..", "Infrastructure", "Utilities", "RoleConsistent.cs");
            if (!File.Exists(roleConsistentPath))
            {
                Console.WriteLine("⚠️ RoleConsistent.cs not found.");
                return;
            }

            string content = File.ReadAllText(roleConsistentPath);
            string className = $"public class {entityName}";
            string consistentClass = $@"
        public class {entityName}
        {{
            public const string Browse = @""{entityName}\Browse {entityName}"";
            public const string Delete = @""{entityName}\Delete {entityName}"";
            public const string Add = @""{entityName}\Add {entityName}"";
            public const string Edit = @""{entityName}\Edit {entityName}"";
            public List<string> Roles = [Delete, Add, Edit, Browse];
        }}
";

            if (content.Contains(className))
            {
                Console.WriteLine($"⚠️ RoleConsistent already contains Roles for {entityName}.");
                return;
            }

            // Add before Dictionary
            int insertIndex = content.LastIndexOf("public static Dictionary") - 1;

            if (insertIndex < 0)
            {
                Console.WriteLine("❌ Failed to find insertion point in Roles");
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
                Console.WriteLine("⚠️ ApplicationDbContextInitialiser.cs not found.");
                return;
            }
            
            string roleAdd = $@"
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
    }
}
