using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                string itemPlural = entityName.EndsWith("y") ? item[..^1] + "ies" : item + "s";
                string entityPlural = entityName.EndsWith("y") ? item[..^1] + "ies" : item + "s";
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
    }
}
