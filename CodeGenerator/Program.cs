using ApiGenerator;
using ApplicationGenerator;
using DomainGenerator;
using InfrastructureGenerator;
using SharedClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;

class Program
{
    static void Main(string[] args)
    {
        Console.Write("Enter Entity Name (e.g., City): ");
        var entityName = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(entityName))
        {
            Console.WriteLine("❌ Entity name is required.");
            return;
        }

        string solutionDir = "F:\\Boulevard\\TestGenerator\\TestGenerator";
        //Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName;

        // Pluralize (simplified)
        string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";

        var domainPath = Path.Combine(solutionDir, "Domain", "Entities");
        var repoInterfacePath = Path.Combine(solutionDir, "Application", "Common", "Interfaces", "IRepositories");
        var repoPath = Path.Combine(solutionDir, "Infrastructure", "Repositories");
        var createCommandPath = Path.Combine(solutionDir, "Application", entityPlural, "Commands", $"Create{entityName}");
        var updateCommandPath = Path.Combine(solutionDir, "Application", entityPlural, "Commands", $"Update{entityName}");
        var deleteCommandPath = Path.Combine(solutionDir, "Application", entityPlural, "Commands", $"Delete{entityName}");
        var queryPath = Path.Combine(solutionDir, "Application", entityPlural, "Queries");


        Directory.CreateDirectory(domainPath);
        Directory.CreateDirectory(repoInterfacePath);
        Directory.CreateDirectory(createCommandPath);
        Directory.CreateDirectory(updateCommandPath);
        Directory.CreateDirectory(deleteCommandPath);

        // Get properties
        var properties = GetPropertiesFromUser(entityName);
        bool hasLocalization = false;
        Console.WriteLine("Is entity has Localization? (y/n): ");
        var answer = Console.ReadLine();
        if (answer?.ToLower() == "y")
            hasLocalization = true;
        var relations = GetRelationsFromUser();
        Domain.GenerateEntityClass(entityName, domainPath, properties, hasLocalization, relations);
        
        //GenerateEntityLocalizationClass(entityName, domainPath);
        Infrastructure.UpdateAppDbContext(entityName, domainPath);
        if (hasLocalization)
            Infrastructure.UpdateAppDbContext($@"{entityName}Localization", domainPath);
        List<string> relatedEntitiesList = new List<string>();
        foreach (var relation in relations)
        {
            if (relation.Type == RelationType.ManyToMany)
            {
                relatedEntitiesList.Add(relation.RelatedEntity);
                List<(string Type, string Name, PropertyValidation Validation)> props = new List<(string Type, string Name, PropertyValidation Validation)>
                {
                    ("Guid",$"{entityName}Id",new PropertyValidation()),
                    ("Guid",$"{relation.RelatedEntity}Id",new PropertyValidation())
                };
                Domain.GenerateEntityClass($"{entityName}{relation.RelatedEntity}", domainPath, props, false, new List<Relation>());
                Infrastructure.UpdateAppDbContext($"{entityName}{relation.RelatedEntity}", domainPath);
                Application.GenerateIRepositoryInterface($"{entityName}{relation.RelatedEntity}", repoInterfacePath);
                Infrastructure.GenerateRepository($"{entityName}{relation.RelatedEntity}", repoPath);
                Infrastructure.UpdateDependencyInjection($"{entityName}{relation.RelatedEntity}", domainPath);
            }
        }
        Infrastructure.GenerateConfiguration(entityName, domainPath, relatedEntitiesList);
        Application.GenerateIRepositoryInterface(entityName, repoInterfacePath);
        if (hasLocalization)
            Application.GenerateIRepositoryInterface($@"{entityName}Localization", repoInterfacePath);
        Infrastructure.GenerateRepository(entityName, repoPath);
        if (hasLocalization)
            Infrastructure.GenerateRepository($@"{entityName}Localization", repoPath);
        Infrastructure.UpdateDependencyInjection(entityName, domainPath);
        if (hasLocalization)
            Infrastructure.UpdateDependencyInjection($@"{entityName}Localization", domainPath);

        Application.GenerateCreateCommand(entityName, entityPlural, createCommandPath, properties);
        Application.GenerateCreateCommandValidator(entityName, entityPlural, createCommandPath, properties);

        Application.GenerateUpdateCommand(entityName, entityPlural, updateCommandPath, properties);
        Application.GenerateUpdateCommandValidator(entityName, entityPlural, updateCommandPath, properties);


        Application.GenerateDeleteCommand(entityName, entityPlural, deleteCommandPath, properties);
        Application.GenerateDeleteCommandValidator(entityName, entityPlural, deleteCommandPath, properties);


        Application.GenerateGetByIdQuery(entityName, entityPlural, queryPath);

        Application.GenerateGetWithPaginationQuery(entityName, entityPlural, queryPath);
        Application.GenerateBaseDto(entityName, entityPlural, properties, solutionDir);

        Api.GenerateNeededDtos(entityName, entityPlural, properties, solutionDir);

        Api.AddRoutesToApiRoutes(entityName, entityPlural, solutionDir);

        Api.GenerateController(entityName, entityPlural, properties, solutionDir);



        Console.WriteLine("✅ Done! All files were generated successfully.");
    }



    static List<(string Type, string Name, PropertyValidation Validation)> GetPropertiesFromUser(string entityName)
    {
        var properties = new List<(string Type, string Name, PropertyValidation Validation)>();

        while (true)
        {
            Console.Write("Add new property? (y/n): ");
            var answer = Console.ReadLine();
            if (answer?.ToLower() != "y") break;

            Console.Write(" - Property Name: ");
            var name = Console.ReadLine()?.Trim();

            Console.Write(" - Property Type: ");
            var type = Console.ReadLine()?.Trim();

            PropertyValidation propValidation = new PropertyValidation();
            Console.Write(" - Is Property Required? (y/n): ");
            answer = Console.ReadLine();
            if (answer?.ToLower() == "y")
                propValidation.Required = true;

            Console.Write(" - Is Property has MinLength? (y/n): ");
            answer = Console.ReadLine();
            if (answer?.ToLower() == "y")
            {
                Console.Write(" - enter MinLength: ");
                answer = Console.ReadLine();
                propValidation.MinLength = int.Parse(answer);
            }
            Console.Write(" - Is Property has MaxLength? (y/n): ");
            answer = Console.ReadLine();
            if (answer?.ToLower() == "y")
            {
                Console.Write(" - enter MaxLength: ");
                answer = Console.ReadLine();
                propValidation.MaxLength = int.Parse(answer);
            }
            Console.Write(" - Is Property has MinRange? (y/n): ");
            answer = Console.ReadLine();
            if (answer?.ToLower() == "y")
            {
                Console.Write(" - enter MinRange: ");
                answer = Console.ReadLine();
                propValidation.MinRange = int.Parse(answer);
            }
            Console.Write(" - Is Property has MaxRange? (y/n): ");
            answer = Console.ReadLine();
            if (answer?.ToLower() == "y")
            {
                Console.Write(" - enter MaxRange: ");
                answer = Console.ReadLine();
                propValidation.MaxRange = int.Parse(answer);
            }

            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(type))
                properties.Add((type, name, propValidation));
        }

        return properties;
    }
    static List<Relation> GetRelationsFromUser()
    {
        var relations = new List<Relation>();

        while (true)
        {
            Console.Write("Dose entity have relations? (y/n): ");
            var answer = Console.ReadLine();
            if (answer?.ToLower() != "y") break;
            var relation = new Relation();

            Console.Write(" - Entity Name: ");
            answer = Console.ReadLine();

            relation.RelatedEntity = answer;

            Console.Write(" - Relation Type: ");
            answer = Console.ReadLine();
            relation.Type = (RelationType)Int32.Parse(answer);

            relations.Add(relation);
        }

        return relations;
    }


}
