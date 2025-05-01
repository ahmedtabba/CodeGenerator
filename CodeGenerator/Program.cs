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
        bool hasLocalization = false;
        bool hasPermissions = false;
        Console.WriteLine("Is entity has Localization? (y/n): ");
        var answer = Console.ReadLine();
        if (answer?.ToLower() == "y")
            hasLocalization = true;
        Console.WriteLine("Is entity has Permissions? (y/n): ");
        answer = Console.ReadLine();
        if (answer?.ToLower() == "y")
            hasPermissions = true;
        bool hasVersioning = false;
        Console.WriteLine("Is entity has Versioning? (y/n): ");
        answer = Console.ReadLine();
        if (answer?.ToLower() == "y")
            hasVersioning = true;
        bool hasNotification = false;
        Console.WriteLine("Is entity has Notification? (y/n): ");
        answer = Console.ReadLine();
        if (answer?.ToLower() == "y")
            hasNotification = true;
        bool hasUserAction = false;
        Console.WriteLine("Is entity has UserAction? (y/n): ");
        answer = Console.ReadLine();
        if (answer?.ToLower() == "y")
            hasUserAction = true;


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
        var properties = GetPropertiesFromUser(entityName,hasLocalization);
       
        var relations = GetRelationsFromUser();
        if (hasPermissions)
        {
            Infrastructure.GeneratePermission(entityName, domainPath);
        }
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
                Domain.GenerateEntityClass($"{entityName}{relation.RelatedEntity}", domainPath, (props,new List<string>()), false, new List<Relation>());
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
        if (hasVersioning)
            ApplicationAssistant.GenerateVersionNeeds(entityName, domainPath, properties.Item1, relations);
        if (hasNotification)
            ApplicationAssistant.GenerateNotificationNeeds(entityName, domainPath);
        if (hasUserAction)
            ApplicationAssistant.GenerateUserActionNeeds(entityName, domainPath);
        if(hasLocalization)
            Infrastructure.UpdateLocalizationService(entityName, domainPath,properties.localizedProp);
        if (hasNotification || hasVersioning || hasUserAction)
        {
            ApplicationAssistant.GenerateEvents(entityName, domainPath, hasVersioning);
            ApplicationAssistant.GenerateHandlers(entityName, domainPath,properties.Item1,relations,hasVersioning,hasUserAction,hasNotification);
        }
        Application.GenerateCreateCommand(entityName, entityPlural, createCommandPath, properties.Item1,hasLocalization,relations,hasVersioning,hasNotification,hasUserAction);
        Application.GenerateCreateCommandValidator(entityName, entityPlural, createCommandPath, properties.Item1, relations);

        Application.GenerateUpdateCommand(entityName, entityPlural, updateCommandPath, properties.Item1, hasLocalization, relations, hasVersioning, hasNotification, hasUserAction);
        Application.GenerateUpdateCommandValidator(entityName, entityPlural, updateCommandPath, properties.Item1, relations);


        Application.GenerateDeleteCommand(entityName, entityPlural, deleteCommandPath, properties.Item1, hasVersioning,hasNotification, hasUserAction);
        Application.GenerateDeleteCommandValidator(entityName, entityPlural, deleteCommandPath, properties.Item1);

         
        Application.GenerateGetByIdQuery(entityName, entityPlural, queryPath, hasLocalization, relations);

        Application.GenerateGetWithPaginationQuery(entityName, entityPlural, queryPath,hasLocalization,relations);
        Application.GenerateBaseDto(entityName, entityPlural, properties.Item1, solutionDir,relations);

        Api.GenerateNeededDtos(entityName, entityPlural, properties.Item1, solutionDir,hasLocalization,relations);

        Api.AddRoutesToApiRoutes(entityName, entityPlural, solutionDir);
      
        Api.GenerateController(entityName, entityPlural, properties.Item1, solutionDir,hasLocalization,hasPermissions);



        Console.WriteLine("✅ Done! All files were generated successfully.");
    }



    static (List<(string Type, string Name, PropertyValidation Validation)>,List<string> localizedProp) GetPropertiesFromUser(string entityName,bool hasLocalization)
    {
        var properties = new List<(string Type, string Name, PropertyValidation Validation)>();
        var localizedProp = new List<string>();
        while (true)
        {
            Console.Write("Add new property? (y/n): ");
            var answer = Console.ReadLine();
            if (answer?.ToLower() != "y") break;

            Console.Write(" - Property Name: ");
            var name = Console.ReadLine()?.Trim();
            string type = null!;

            PropertyValidation propValidation = new PropertyValidation();
            int numOfValidation = 0;

            Console.Write(" - Is Property Image or List of images or video ? enter 1 for Image, 2 for List of images, 3 for video, n if not : ");
            var isImage = Console.ReadLine()?.Trim();
            if (isImage?.ToLower() == "1" || isImage?.ToLower() == "2" || isImage?.ToLower() == "3")
            {
                type = isImage?.ToLower() == "1" ? "GPG" : isImage?.ToLower() == "2" ? "PNGs" : "VD";
                if (type != "VD")
                {
                    Console.Write(" - Is Image/Images Property Required? (y/n): ");
                    answer = Console.ReadLine();
                    if (answer?.ToLower() == "y")
                    { propValidation.Required = true; numOfValidation++; }
                }
                
            }
            else
            {
                if (hasLocalization)
                {
                    Console.Write(" - Is Property Localized? (y/n): ");
                    answer = Console.ReadLine();
                    if (answer?.ToLower() == "y")
                        localizedProp.Add(name);
                }
                Console.Write(" - Property Type: ");
                type = Console.ReadLine()?.Trim();
                Console.Write(" - Is Property Required? (y/n): ");

                answer = Console.ReadLine();
                if (answer?.ToLower() == "y")
                { propValidation.Required = true; numOfValidation++; }

                Console.Write(" - Is Property Unique? (y/n): ");
                answer = Console.ReadLine();
                if (answer?.ToLower() == "y")
                { propValidation.Unique = true; numOfValidation++; }

                Console.Write(" - Is Property has MinLength? (y/n): ");
                answer = Console.ReadLine();
                if (answer?.ToLower() == "y")
                {
                    Console.Write(" - enter MinLength: ");
                    answer = Console.ReadLine();
                    propValidation.MinLength = int.Parse(answer);
                    numOfValidation++;
                }
                Console.Write(" - Is Property has MaxLength? (y/n): ");
                answer = Console.ReadLine();
                if (answer?.ToLower() == "y")
                {
                    Console.Write(" - enter MaxLength: ");
                    answer = Console.ReadLine();
                    propValidation.MaxLength = int.Parse(answer);
                    numOfValidation++;
                }
                Console.Write(" - Is Property has MinRange? (y/n): ");
                answer = Console.ReadLine();
                if (answer?.ToLower() == "y")
                {
                    Console.Write(" - enter MinRange: ");
                    answer = Console.ReadLine();
                    propValidation.MinRange = int.Parse(answer);
                    numOfValidation++;
                }
                Console.Write(" - Is Property has MaxRange? (y/n): ");
                answer = Console.ReadLine();
                if (answer?.ToLower() == "y")
                {
                    Console.Write(" - enter MaxRange: ");
                    answer = Console.ReadLine();
                    propValidation.MaxRange = int.Parse(answer);
                    numOfValidation++;
                }
            }

            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(type))
                if(numOfValidation > 0)
                    properties.Add((type, name, propValidation));
                else
                    properties.Add((type, name, null));

        }

        return (properties,localizedProp);
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

            Console.Write(" - Relation Type:" +
                "\n" + "OneToOneSelfJoin : 0\r\nOneToOne : 1\r\nOneToOneNullable : 2\r\nOneToMany : 3\r\nOneToManyNullable : 4\r\nManyToOne : 5\r\nManyToOneNullable : 6\r\nManyToMany : 7 \n"
                + "enter number of relation : ");
            answer = Console.ReadLine();
            relation.Type = (RelationType)Int32.Parse(answer);

            relations.Add(relation);
        }

        return relations;
    }


}
