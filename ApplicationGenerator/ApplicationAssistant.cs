using SharedClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ApplicationGenerator
{
    public static class ApplicationAssistant
    {
        public static void GenerateVersionNeeds(string entityName, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<Relation> relations)
        {
            string versionEntityTypePath = Path.Combine(path, "..", "Enums", "VersionEntityType.cs");
            if (!File.Exists(versionEntityTypePath))
            {
                //Console.WriteLine("⚠️ VersionEntityType.cs not found.");
                return;
            }
            string enumAdd = $"\t\t{entityName}," +
                $"\n\t\t//Add Here";
            var lines = File.ReadAllLines(versionEntityTypePath).ToList();
            var index = lines.FindIndex(line => line.Contains("//Add Here"));
            if (index >= 0)
            {
                lines[index] = enumAdd;
                File.WriteAllLines(versionEntityTypePath, lines);
                //Console.WriteLine("✅ VersionEntityType updated.");
            }

            #region Create VersioningDto
            string versioningDtoPath = Path.Combine(path, "..", "..", "Application", "Common", "Models", "Versioning", $"{entityName}VersioningDTO.cs");
            List<string> propList = new List<string>();
            properties.ForEach(p => propList.Add((p.Type == "GPG" && p.Validation == null)
            ? $"public {p.Type}? {p.Name} {{ get; set; }}"
            : $"public {p.Type} {p.Name} {{ get; set; }}"));

            var tempList = new List<string>();
            foreach (var item in propList)
            {
                var s = item.Replace("GPG", "string").Replace("PNGs", "List<string>").Replace("VD", "string?");
                tempList.Add(s);
            }
            tempList.ForEach(p => p.Replace("GPG", "string").Replace("PNGs", "List<string>").Replace("VD", "string?"));

            foreach (var relation in relations)
            {

                switch (relation.Type)
                {
                    case RelationType.OneToOneSelfJoin:
                        tempList.Add($"\t\tpublic Guid? {entityName}ParentId {{ get; set; }}");
                        break;
                    case RelationType.OneToOne:
                        tempList.Add($"\t\tpublic Guid {relation.RelatedEntity}Id {{ get; set; }}");
                        break;
                    case RelationType.OneToOneNullable:
                        tempList.Add($"\t\tpublic Guid? {relation.RelatedEntity}Id {{ get; set; }}");
                        break;
                    case RelationType.ManyToOne:
                        tempList.Add($"\t\tpublic Guid {relation.RelatedEntity}Id {{ get; set; }}");
                        break;
                    case RelationType.ManyToOneNullable:
                        tempList.Add($"\t\tpublic Guid? {relation.RelatedEntity}Id {{ get; set; }}");
                        break;
                    case RelationType.ManyToMany:
                        tempList.Add($"\t\tpublic List<Guid> {relation.RelatedEntity}Ids {{ get; set; }}");
                        break;
                    default:
                        break;
                }

            }

            var props = string.Join(Environment.NewLine, tempList);

            var dtoContent = $@"using System;
using Application.Common.Interfaces.Services.Versioning;
using Application.Utilities.Attributes;

namespace Application.Common.Models.Versioning
{{
    [Versioned]

    public class {entityName}VersioningDTO : EntityDTO, IVersionable
    {{
        //TODO:AfterGenerateCode: Add [EmbeddedList] annotation to list properties i needed to be versioned
        //public Guid Id {{ get; set; }} // from EntityDTO and it has [IgnoreVersioning] attribute
        public string UniqueIdentifier {{ get; set; }} = null!;// from IVersionable and it has [IgnoreVersioning] attribute
        {props}
    }}
}}
";

            File.WriteAllText(versioningDtoPath, dtoContent);
            #endregion
        }

        public static void GenerateEvents(string entityName, string path,bool hasVersioning,bool bulk)
        {
            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";
            string lowerEntityPlural = char.ToLower(entityPlural[0]) + entityPlural.Substring(1);
            string inheritEvent = hasVersioning ? "BaseEvent, IBaseVersionInfo" : "BaseEvent";
            string? baseVersionInfoProp = !hasVersioning ? null : $@"
        public Type EntityType {{ get; set; }} = null!;
        public string? RollbackedToVersionId {{ get; set; }}
        public dynamic OldEntity {{ get; set; }} = null!;
        public dynamic NewEntity {{ get; set; }} = null!;
        public int ChangeType {{ get; set; }}// Added ,Modified ,Deleted
        public string? ExternalVersioningReasonId {{ get; set; }}
        public int? ExternalVersioningReasonType {{ get; set; }}
        public int? VersionOperation {{ get; set; }}
        public bool ToBePublished {{ get; set; }} = true;
        public bool IsVersionedCommand {{ get; set; }} = false;
";
            string eventCreateClassName = $"{entityName}CreatedEvent";
            string eventCreateClassNameBulk = $"{entityName}CreatedBulkEvent";
            string eventEditClassName = $"{entityName}EditedEvent";
            string eventEditClassNameBulk = $"{entityName}EditedBulkEvent";
            string eventDeletedClassName = $"{entityName}DeletedEvent";
            string eventDeletedClassNameBulk = $"{entityName}DeletedBulkEvent";
            string eventDirectory = Path.Combine(path, "..", "Events", $"{entityName}Events");
            string eventCreatePath = Path.Combine(path, "..", "Events", $"{entityName}Events", $"{eventCreateClassName}.cs");
            string eventCreateBulkPath = Path.Combine(path, "..", "Events", $"{entityName}Events", $"{eventCreateClassNameBulk}.cs");
            string eventEditPath = Path.Combine(path, "..", "Events", $"{entityName}Events", $"{eventEditClassName}.cs");
            string eventEditBulkPath = Path.Combine(path, "..", "Events", $"{entityName}Events", $"{eventEditClassNameBulk}.cs");
            string eventDeletePath = Path.Combine(path, "..", "Events", $"{entityName}Events", $"{eventDeletedClassName}.cs");
            string eventDeleteBulkPath = Path.Combine(path, "..", "Events", $"{entityName}Events", $"{eventDeletedClassNameBulk}.cs");

            Directory.CreateDirectory(eventDirectory);

            var createContent = $@"using System;

namespace Domain.Events.{entityName}Events
{{
    public class {entityName}CreatedEvent : {inheritEvent}
    {{
        public {entityName}CreatedEvent({entityName} {lowerEntityName})
        {{
            {entityName} = {lowerEntityName};
        }}
        public {entityName} {entityName} {{ get; }}
{baseVersionInfoProp}
    }}              
}}
";

            var editContent = $@"using System;

namespace Domain.Events.{entityName}Events
{{
    public class {entityName}EditedEvent : {inheritEvent}
    {{
        public {entityName}EditedEvent({entityName} old{entityName}, {entityName} new{entityName})
        {{
            Old{entityName} = old{entityName};
            New{entityName} = new{entityName};
        }}
        public {entityName} Old{entityName} {{ get; }}
        public {entityName} New{entityName} {{ get; }}
{baseVersionInfoProp}
    }}              
}}
";

            var deletedContent = $@"using System;

namespace Domain.Events.{entityName}Events
{{
    public class {entityName}DeletedEvent :{inheritEvent}
    {{
        public {entityName}DeletedEvent({entityName} {lowerEntityName})
        {{
            {entityName} = {lowerEntityName};
        }}
        public {entityName} {entityName} {{ get; }}
{baseVersionInfoProp}
    }}              
}}
";
            File.WriteAllText(eventCreatePath, createContent);
            File.WriteAllText(eventEditPath, editContent);
            File.WriteAllText(eventDeletePath, deletedContent);
            if (bulk)
            {
                var createBulkContent = $@"using System;

namespace Domain.Events.{entityName}Events
{{
    public class {entityName}CreatedBulkEvent : {inheritEvent}
    {{
        public {entityName}CreatedBulkEvent(List<{entityName}> {lowerEntityPlural})
        {{
            {entityPlural} = {lowerEntityPlural};
        }}
        public List<{entityName}> {entityPlural} {{ get; }}
{baseVersionInfoProp}
    }}              
}}
";
                var editBulkContent = $@"using System;

namespace Domain.Events.{entityName}Events
{{
    public class {entityName}EditedBulkEvent : {inheritEvent}
    {{
        public {entityName}EditedBulkEvent(List<{entityName}> old{entityPlural}, List<{entityName}> new{entityPlural})
        {{
            Old{entityPlural} = old{entityPlural};
            New{entityPlural} = new{entityPlural};
        }}
        public List<{entityName}> Old{entityPlural} {{ get; }}
        public List<{entityName}> New{entityPlural} {{ get; }}
{baseVersionInfoProp}
    }}              
}}
";
                var deleteBulkContent = $@"using System;

namespace Domain.Events.{entityName}Events
{{
    public class {entityName}DeletedBulkEvent : {inheritEvent}
    {{
        public {entityName}DeletedBulkEvent(List<{entityName}> {lowerEntityPlural})
        {{
            {entityPlural} = {lowerEntityPlural};
        }}
        public List<{entityName}> {entityPlural} {{ get; }}
{baseVersionInfoProp}
    }}              
}}
";

                File.WriteAllText(eventCreateBulkPath, createBulkContent);
                File.WriteAllText(eventEditBulkPath, editBulkContent);
                File.WriteAllText(eventDeleteBulkPath, deleteBulkContent);
            }
        }

        public static void GenerateNotificationNeeds(string entityName, string path)
        {
            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";
            #region Add NotificationObjectTypes
            string notificationObjectTypesPath = Path.Combine(path, "..", "Enums", "NotificationObjectTypes.cs");
            if (!File.Exists(notificationObjectTypesPath))
            {
                //Console.WriteLine("⚠️ NotificationObjectTypes.cs not found.");
                return;
            }
            string enumAdd = $"\t\t{entityName}," +
                $"\n\t\t//Add Here";
            var lines = File.ReadAllLines(notificationObjectTypesPath).ToList();
            var index = lines.FindIndex(line => line.Contains("//Add Here"));
            if (index >= 0)
            {
                lines[index] = enumAdd;
                File.WriteAllLines(notificationObjectTypesPath, lines);
                //Console.WriteLine("✅ NotificationObjectTypes updated.");
            }

            #endregion

            #region Add NotificationConsistent
            string notificationConsistentPath = Path.Combine(path, "..", "..", "Application", "Utilities", "NotificationConsistent.cs");

            if (!File.Exists(notificationConsistentPath))
            {
                //Console.WriteLine("❌ NotificationConsistent.cs not found.");
                return;
            }
            
            string content = File.ReadAllText(notificationConsistentPath);
            string className = $"public class {entityPlural}";
            string consistentClass = $@"
        public class {entityPlural}
        {{
            public const string Add = @""{entityName}\Add {entityName}"";
            public const string Edit = @""{entityName}\Edit {entityName}"";
            public const string Delete = @""{entityName}\Delete {entityName}"";
            public const string Restore = @""{entityName}\Restore {entityName}"";
        }}
";

            if (content.Contains(className))
            {
                //Console.WriteLine($"⚠️ NotificationConsistent already contains Notification for {entityName}.");
                return;
            }

            // Add before Dictionary
            int insertIndex = content.LastIndexOf("public static Dictionary") - 1;

            if (insertIndex < 0)
            {
                //Console.WriteLine("❌ Failed to find insertion point in Notification");
                return;
            }

            content = content.Insert(insertIndex, "\n" + consistentClass + "\n\t");
            File.WriteAllText(notificationConsistentPath, content);

            string consistentGroup = $"\t\t\tGroups.Add(\"{entityPlural}\", new List<string>() {{ \"{entityName}\" }});"
                + $"\n\t\t\t//Add To Group Here"; ;
            lines.Clear();
            lines = File.ReadAllLines(notificationConsistentPath).ToList();
            index = lines.FindIndex(line => line.Contains("//Add To Group Here"));
            if (index >= 0)
            {
                lines[index] = consistentGroup;
                File.WriteAllLines(notificationConsistentPath, lines);
            }

            string notificationAdd = $@"
            #region {entityName}

            if (!notifications.Exists(n => n.Name == NotificationConsistent.{entityPlural}.Add))
                _context.Notifications.Add(new Notification {{Name = NotificationConsistent.{entityPlural}.Add}});

            if (!notifications.Exists(n => n.Name == NotificationConsistent.{entityPlural}.Edit))
                _context.Notifications.Add(new Notification {{Name = NotificationConsistent.{entityPlural}.Edit}});

            if (!notifications.Exists(n => n.Name == NotificationConsistent.{entityPlural}.Delete))
                _context.Notifications.Add(new Notification {{Name = NotificationConsistent.{entityPlural}.Delete}});

            if (!notifications.Exists(n => n.Name == NotificationConsistent.{entityPlural}.Restore))
                _context.Notifications.Add(new Notification {{Name = NotificationConsistent.{entityPlural}.Restore}});

            #endregion
" + $"\n\t\t\t//Add Notifications Here";
            lines.Clear();
            var initialiserPath = Path.Combine(path, "..", "..", "Infrastructure", "Data", "ApplicationDbContextInitialiser.cs");
            if (!File.Exists(initialiserPath))
            {
                //Console.WriteLine("⚠️ ApplicationDbContextInitialiser.cs not found.");
                return;
            }
            lines = File.ReadAllLines(initialiserPath).ToList();
            index = lines.FindIndex(line => line.Contains("//Add Notifications Here"));

            if (index >= 0)
            {
                lines[index] = notificationAdd;
                File.WriteAllLines(initialiserPath, lines);
            }
            #endregion
        }
        public static void GenerateUserActionNeeds(string entityName, string path)
        {
            #region Add UserActionEntityType
            string userActionEntityTypePath = Path.Combine(path, "..", "Enums", "UserActionEntityType.cs");
            if (!File.Exists(userActionEntityTypePath))
            {
                //Console.WriteLine("⚠️ UserActionEntityType.cs not found.");
                return;
            }
            string enumAdd = $"\t\t{entityName}," +
                $"\n\t\t//Add Here";
            var lines = File.ReadAllLines(userActionEntityTypePath).ToList();
            var index = lines.FindIndex(line => line.Contains("//Add Here"));
            if (index >= 0)
            {
                lines[index] = enumAdd;
                File.WriteAllLines(userActionEntityTypePath, lines);
                //Console.WriteLine("✅ UserActionEntityType updated.");
            }

            #endregion
        }

        public static void GenerateHandlers(string entityName, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<Relation> relations,bool versioning, bool userActon, bool notification,bool bulk)
        {
            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";
            string handlersDirectory = Path.Combine(path, "..", "..", "Application", $"{entityPlural}", "EventHandlers");
            Directory.CreateDirectory(handlersDirectory);

            GenerateCreatedHandler(entityName,entityPlural,path,properties,relations,versioning,userActon,notification);
            GenerateUpdateHandler(entityName, entityPlural, path, properties, relations, versioning, userActon, notification);
            GenerateDeleteHandler(entityName, entityPlural, path, properties, relations, versioning, userActon, notification);
            if (bulk)
            {
                GenerateCreatedBulkHandler(entityName, entityPlural, path, properties, relations, versioning, userActon, notification);
                GenerateUpdatedBulkHandler(entityName, entityPlural, path, properties, relations, versioning, userActon, notification);
                GenerateDeleteBulkHandler(entityName, entityPlural, path, properties, relations, versioning, userActon, notification);
            }
        }
    
        static void GenerateCreatedHandler (string entityName,string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<Relation> relations, bool versioning, bool userActon, bool notification)
        {
            string handlerCreateClassName = $"Created{entityName}EventHandler";
            string handlerCreatePath = Path.Combine(path, "..", "..", "Application", $"{entityPlural}", "EventHandlers", $"{handlerCreateClassName}.cs");
            string? HandleVersioning = !versioning ? null : $"var versionId = await HandleVersioning(notification);";
            string? HandleUserActon = !userActon ? null 
                : !versioning ? "await HandleUserAction(notification);"
                : $@"
            if (!notification.IsVersionedCommand)
                await HandleUserAction(notification, versionId);";
            string? HandleNotification = !notification ? null : $"await HandleNotification(notification, cancellationToken);";

            var propList = GetVersionDTOProp(properties, relations);
            StringBuilder versioningDTOBuilder = new StringBuilder();
            foreach (var item in propList)
            {
                if (item.EndsWith("Ids"))
                {
                    var temp = item.Remove(item.Length - 3);
                    string? relatedEntityManyPlural = relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.EndsWith("y") ? relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[..^1] + "ies" : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity + "s";
                    var tempPlural = temp.EndsWith("y") ? temp[..^1] + "ies" : temp + "s";
                    versioningDTOBuilder.AppendLine($"\t\t\t\t{item} = obj.{tempPlural}.Select(x => x.Id).ToList(),");
                }
                else
                    versioningDTOBuilder.AppendLine($"\t\t\t\t{item} = obj.{item},");
            }

            string? HandleVersioningMethod = !versioning ? null : $@"
        private async Task<string> HandleVersioning({entityName}CreatedEvent notification)
        {{
            var obj = notification.{entityName};
            //prepare VersioningDTO 
            {entityName}VersioningDTO versioningDTO = new {entityName}VersioningDTO
            {{
                Id = obj.Id,
                {versioningDTOBuilder}
                UniqueIdentifier = obj.Id.ToString()
            }};

            notification.ChangeType = 0; // Added
            notification.EntityType = typeof({entityName}VersioningDTO);
            notification.OldEntity = null!;
            notification.NewEntity = versioningDTO;

            return await _versioningService.AddVersion<{entityName}VersioningDTO>((VersionChangeType)notification.ChangeType, notification.{entityName}.Id.ToString(),
                                                                        VersionEntityType.{entityName}, userId: null!, ({entityName}VersioningDTO)notification.OldEntity,
                                                                       ({entityName}VersioningDTO)notification.NewEntity, notification.RollbackedToVersionId);
        }}
";
            string HandleNotificationMethodVersionCase = versioning ? $@"
            if (!notification.IsVersionedCommand)// case of normal create
            {{
                notificationConsistent = NotificationConsistent.{entityPlural}.Add;
                messageBuilder.Append("" has been added 🎉🎉"");
            }}
            else// case of restore create
            {{
                notificationConsistent = NotificationConsistent.{entityPlural}.Restore;
                messageBuilder.Append("" has been restored"");
            }}
"
:
$@"
            notificationConsistent = NotificationConsistent.{entityPlural}.Add;
            messageBuilder.Append("" has been added 🎉🎉"");
";
            string? HandleNotificationMethod = !notification ? null : $@"
        private async Task HandleNotification({entityName}CreatedEvent notification, CancellationToken cancellationToken, List<string> specificNotifiedUsers = null)
        {{
            // check notification.IsVersionedCommand to determine if notification is from end user or restore prosses, and push it
            (string NotificationMessage, List<string> UsersIsd) signalRMessage;
            string notificationConsistent;

            StringBuilder messageBuilder = new StringBuilder(""{entityName} : "");
            messageBuilder.Append(notification.{entityName}.Id); //TODO:AfterGenerateCode:Replace Id with the proper property
            {HandleNotificationMethodVersionCase}

            signalRMessage = await _userNotificationService.Push(NotificationObjectTypes.{entityName}, notification.{entityName}.Id,
                                                                 notificationConsistent, notificationMessage: messageBuilder.ToString(),
                                                                 cancellationToken, specificNotifiedUsers);

            // Send notification by SignalR
            if (signalRMessage.UsersIsd.Any())
                await _notificationService.SendNotification(message: signalRMessage.NotificationMessage, signalRMessage.UsersIsd);
        }}
";
            string? HandleUserActonMethod = !userActon ? null : $@"
        private async Task HandleUserAction({entityName}CreatedEvent notification, string? versionId = null)
        {{
            if (versionId != null)
                await _userActionService.AddUserAction(UserActionType.Create, UserActionEntityType.{entityName}, notification.{entityName}.Id.ToString(), versionId);
            else
                await _userActionService.AddUserAction(UserActionType.Create, UserActionEntityType.{entityName}, notification.{entityName}.Id.ToString());
        }}
";

            var handlerCreateContent = $@"using System;
using Microsoft.Extensions.Logging;
using System.Text;
using Application.Common.Interfaces.Services;
using Application.Common.Interfaces.Services.Versioning;
using Application.Common.Models.AssistantModels;
using Application.Common.Models.Versioning;
using Application.Utilities;
using Domain.Enums;
using Domain.Events.{entityName}Events;

namespace Application.{entityPlural}.EventHandlers
{{
    public class Create{entityName}EventHandler : INotificationHandler<{entityName}CreatedEvent>
    {{
        private readonly ILogger<Create{entityName}EventHandler> _logger;
        private readonly INotificationService _notificationService;
        private readonly IUserNotificationService _userNotificationService;
        private readonly IVersioningService _versioningService;
        private readonly IUserActionService _userActionService;

        public Create{entityName}EventHandler(ILogger<Create{entityName}EventHandler> logger,
                                        INotificationService notificationService,
                                        IUserNotificationService userNotificationService,
                                        IVersioningService versioningService,
                                        IUserActionService userActionService)
        {{
            _logger = logger;
            _notificationService = notificationService;
            _userNotificationService = userNotificationService;
            _versioningService = versioningService;
            _userActionService = userActionService;
        }}
        public async Task Handle({entityName}CreatedEvent notification, CancellationToken cancellationToken)
        {{
            {HandleVersioning}

            {HandleUserActon}

            {HandleNotification}
        }}
{HandleVersioningMethod}
{HandleNotificationMethod}
{HandleUserActonMethod}
    }}
    
    
}}
";
            File.WriteAllText(handlerCreatePath, handlerCreateContent);
        }

        static void GenerateCreatedBulkHandler(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<Relation> relations, bool versioning, bool userActon, bool notification)
        {
            string handlerCreateClassName = $"CreatedBulk{entityName}EventHandler";
            string handlerCreatePath = Path.Combine(path, "..", "..", "Application", $"{entityPlural}", "EventHandlers", $"{handlerCreateClassName}.cs");
            string? HandleVersioning = !versioning ? null : $"var versionId = await HandleVersioning(notification);";
            string? HandleUserActon = !userActon ? null
                : !versioning ? "await HandleUserAction(notification);"
                : $@"
            if (!notification.IsVersionedCommand)
                await HandleUserAction(notification, versionId);";
            string? HandleNotification = !notification ? null : $"await HandleNotification(notification, cancellationToken);";

            string aggregator = relations.First(r => r.Type == RelationType.ManyToOne || r.Type == RelationType.ManyToOneNullable).RelatedEntity;
            string nullableAggregatorNot = relations.First(r => r.Type == RelationType.ManyToOne || r.Type == RelationType.ManyToOneNullable).Type == RelationType.ManyToOne ?
                                           $"notification.{entityPlural}[0].{aggregator}Id" : $"notification.{entityPlural}[0].{aggregator}Id.Value";
            var propList = GetVersionDTOProp(properties, relations);
            StringBuilder versioningDTOBuilder = new StringBuilder();
            foreach (var item in propList)
            {
                if (item.EndsWith("Ids"))
                {
                    var temp = item.Remove(item.Length - 3);
                    string? relatedEntityManyPlural = relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.EndsWith("y") ? relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[..^1] + "ies" : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity + "s";
                    var tempPlural = temp.EndsWith("y") ? temp[..^1] + "ies" : temp + "s";
                    versioningDTOBuilder.AppendLine($"\t\t\t\t{item} = item.{tempPlural}.Select(x => x.Id).ToList(),");
                }
                else
                    versioningDTOBuilder.AppendLine($"\t\t\t\t{item} = item.{item},");
            }

            string? HandleVersioningMethod = !versioning ? null : $@"
        private async Task<string> HandleVersioning({entityName}CreatedBulkEvent notification)
        {{
            var objects = notification.{entityPlural};
            //prepare VersioningDTO 
            List<{entityName}VersioningDTO> versioningDTOs = new List<{entityName}VersioningDTO>();
            foreach (var item in objects)
            {{
                {entityName}VersioningDTO versioningDTO = new {entityName}VersioningDTO
                {{
                    Id = item.Id,
                    {versioningDTOBuilder}
                    UniqueIdentifier = item.Id.ToString()
                }};
                 versioningDTOs.Add(versioningDTO);
            }}
            notification.ChangeType = 0; // Added
            notification.EntityType = typeof(List<{entityName}VersioningDTO>);
            notification.OldEntity = null!;
            notification.NewEntity = versioningDTOs;

            return await _versioningService.AddVersion<{entityName}VersioningDTO>((VersionChangeType)notification.ChangeType, objects[0].{aggregator}Id.ToString(),
                                                                        VersionEntityType.{entityName}, userId: null!, (List<{entityName}VersioningDTO>)notification.OldEntity,
                                                                       (List<{entityName}VersioningDTO>)notification.NewEntity, notification.RollbackedToVersionId);
        }}
";
            string HandleNotificationMethodVersionCase = versioning ? $@"
            if (!notification.IsVersionedCommand)// case of normal create
            {{
                notificationConsistent = NotificationConsistent.{entityPlural}.Add;
                messageBuilder.Append("" has been added 🎉🎉"");
            }}
            else// case of restore create
            {{
                notificationConsistent = NotificationConsistent.{entityPlural}.Restore;
                messageBuilder.Append("" has been restored"");
            }}
"
:
$@"
            notificationConsistent = NotificationConsistent.{entityPlural}.Add;
            messageBuilder.Append("" has been added 🎉🎉"");
";
            string? HandleNotificationMethod = !notification ? null : $@"
        private async Task HandleNotification({entityName}CreatedBulkEvent notification, CancellationToken cancellationToken, List<string> specificNotifiedUsers = null)
        {{
            // check notification.IsVersionedCommand to determine if notification is from end user or restore prosses, and push it
            (string NotificationMessage, List<string> UsersIsd) signalRMessage;
            string notificationConsistent;

            StringBuilder messageBuilder = new StringBuilder(""List of {entityName} for {aggregator} : "");
            messageBuilder.Append(notification.{entityPlural}[0].{aggregator}Id); //TODO:AfterGenerateCode:Replace Id with the proper property
            {HandleNotificationMethodVersionCase}

            signalRMessage = await _userNotificationService.Push(NotificationObjectTypes.{entityName}, {nullableAggregatorNot},
                                                                 notificationConsistent, notificationMessage: messageBuilder.ToString(),
                                                                 cancellationToken, specificNotifiedUsers);

            // Send notification by SignalR
            if (signalRMessage.UsersIsd.Any())
                await _notificationService.SendNotification(message: signalRMessage.NotificationMessage, signalRMessage.UsersIsd);
        }}
";
            string? HandleUserActonMethod = !userActon ? null : $@"
        private async Task HandleUserAction({entityName}CreatedBulkEvent notification, string? versionId = null)
        {{
            if (versionId != null)
                await _userActionService.AddUserAction(UserActionType.CreateBulk, UserActionEntityType.{entityName}, notification.{entityPlural}[0].{aggregator}Id.ToString(), versionId);
            else
                await _userActionService.AddUserAction(UserActionType.CreateBulk, UserActionEntityType.{entityName}, notification.{entityPlural}[0].{aggregator}Id.ToString());
        }}
";

            var handlerCreateBulkContent = $@"using System;
using Microsoft.Extensions.Logging;
using System.Text;
using Application.Common.Interfaces.Services;
using Application.Common.Interfaces.Services.Versioning;
using Application.Common.Models.AssistantModels;
using Application.Common.Models.Versioning;
using Application.Utilities;
using Domain.Enums;
using Domain.Events.{entityName}Events;

namespace Application.{entityPlural}.EventHandlers
{{
    public class CreateBulk{entityName}EventHandler : INotificationHandler<{entityName}CreatedBulkEvent>
    {{
        private readonly ILogger<CreateBulk{entityName}EventHandler> _logger;
        private readonly INotificationService _notificationService;
        private readonly IUserNotificationService _userNotificationService;
        private readonly IVersioningService _versioningService;
        private readonly IUserActionService _userActionService;

        public CreateBulk{entityName}EventHandler(ILogger<CreateBulk{entityName}EventHandler> logger,
                                        INotificationService notificationService,
                                        IUserNotificationService userNotificationService,
                                        IVersioningService versioningService,
                                        IUserActionService userActionService)
        {{
            _logger = logger;
            _notificationService = notificationService;
            _userNotificationService = userNotificationService;
            _versioningService = versioningService;
            _userActionService = userActionService;
        }}
        public async Task Handle({entityName}CreatedBulkEvent notification, CancellationToken cancellationToken)
        {{
            {HandleVersioning}

            {HandleUserActon}

            {HandleNotification}
        }}
{HandleVersioningMethod}
{HandleNotificationMethod}
{HandleUserActonMethod}
    }}
    
    
}}
";
            File.WriteAllText(handlerCreatePath, handlerCreateBulkContent);
        }

        static void GenerateUpdateHandler(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<Relation> relations, bool versioning, bool userActon, bool notification)
        {
            string handlerUpdateClassName = $"Updated{entityName}EventHandler";
            string handlerUpdatePath = Path.Combine(path, "..", "..", "Application", $"{entityPlural}", "EventHandlers", $"{handlerUpdateClassName}.cs");
            string? HandleVersioning = !versioning ? null : $"var versionId = await HandleVersioning(notification);";
            string? HandleUserActon = !userActon ? null
                : !versioning ? "await HandleUserAction(notification);"
                : $@"
            if (!notification.IsVersionedCommand)
                await HandleUserAction(notification, versionId);";
            string? HandleNotification = !notification ? null : $"await HandleNotification(notification, cancellationToken);";

            var propList = GetVersionDTOProp(properties, relations);
            StringBuilder oldVersioningDTOBuilder = new StringBuilder();
            foreach (var item in propList)
            {
                if (item.EndsWith("Ids"))
                {
                    var temp = item.Remove(item.Length - 3);
                    string? relatedEntityManyPlural = relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.EndsWith("y") ? relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[..^1] + "ies" : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity + "s";
                    var tempPlural = temp.EndsWith("y") ? temp[..^1] + "ies" : temp + "s";
                    oldVersioningDTOBuilder.AppendLine($"\t\t\t\t{item} = oldObj.{tempPlural}.Select(x => x.Id).ToList(),");
                }
                else
                    oldVersioningDTOBuilder.AppendLine($"\t\t\t\t{item} = oldObj.{item},");
            }

            StringBuilder newVersioningDTOBuilder = new StringBuilder();
            foreach (var item in propList)
            {
                if (item.EndsWith("Ids"))
                {
                    var temp = item.Remove(item.Length - 3);
                    string? relatedEntityManyPlural = relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.EndsWith("y") ? relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[..^1] + "ies" : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity + "s";
                    var tempPlural = temp.EndsWith("y") ? temp[..^1] + "ies" : temp + "s";
                    newVersioningDTOBuilder.AppendLine($"\t\t\t\t{item} = newObj.{tempPlural}.Select(x => x.Id).ToList(),");
                }
                else
                    newVersioningDTOBuilder.AppendLine($"\t\t\t\t{item} = newObj.{item},");
            }

            string? HandleVersioningMethod = !versioning ? null : $@"
        private async Task<string> HandleVersioning({entityName}EditedEvent notification)
        {{
            var oldObj = notification.Old{entityName};
            var newObj = notification.New{entityName};
            //prepare VersioningDTO 
            {entityName}VersioningDTO oldVersioningDTO = new {entityName}VersioningDTO
            {{
                Id = oldObj.Id,
                {oldVersioningDTOBuilder}
                UniqueIdentifier = oldObj.Id.ToString()
            }};

            {entityName}VersioningDTO newVersioningDTO = new {entityName}VersioningDTO
            {{
                Id = newObj.Id,
                {newVersioningDTOBuilder}
                UniqueIdentifier = newObj.Id.ToString()
            }};

            notification.ChangeType = 1; // Modified
            notification.EntityType = typeof({entityName}VersioningDTO);
            notification.OldEntity = oldVersioningDTO;
            notification.NewEntity = newVersioningDTO;

            return await _versioningService.AddVersion<{entityName}VersioningDTO>((VersionChangeType)notification.ChangeType, notification.Old{entityName}.Id.ToString(),
                                                                        VersionEntityType.{entityName}, userId: null!, ({entityName}VersioningDTO)notification.OldEntity,
                                                                       ({entityName}VersioningDTO)notification.NewEntity, notification.RollbackedToVersionId);
        }}
";
            string HandleNotificationMethodVersionCase = versioning ? $@"
            if (!notification.IsVersionedCommand)// case of normal update
            {{
                notificationConsistent = NotificationConsistent.{entityPlural}.Edit;
                messageBuilder.Append("" has been modified"");
            }}
            else// case of restore update
            {{
                notificationConsistent = NotificationConsistent.{entityPlural}.Restore;
                messageBuilder.Append("" has been restored"");
            }}
"
:
$@"
            notificationConsistent = NotificationConsistent.{entityPlural}.Edit;
            messageBuilder.Append("" has been modified"");
";

            string? HandleNotificationMethod = !notification ? null : $@"
        private async Task HandleNotification({entityName}EditedEvent notification, CancellationToken cancellationToken, List<string> specificNotifiedUsers = null)
        {{
            // check notification.IsVersionedCommand to determine if notification is from end user or restore prosses, and push it
            (string NotificationMessage, List<string> UsersIsd) signalRMessage;
            string notificationConsistent;

            StringBuilder messageBuilder = new StringBuilder(""{entityName} : "");
            messageBuilder.Append(notification.Old{entityName}.Id); //TODO:AfterGenerateCode:Replace Id with the proper property
            {HandleNotificationMethodVersionCase}

            signalRMessage = await _userNotificationService.Push(NotificationObjectTypes.{entityName}, notification.Old{entityName}.Id,
                                                                 notificationConsistent, notificationMessage: messageBuilder.ToString(),
                                                                 cancellationToken, specificNotifiedUsers);

            // Send notification by SignalR
            if (signalRMessage.UsersIsd.Any())
                await _notificationService.SendNotification(message: signalRMessage.NotificationMessage, signalRMessage.UsersIsd);
        }}
";
            string? HandleUserActonMethod = !userActon ? null : $@"
        private async Task HandleUserAction({entityName}EditedEvent notification, string? versionId = null)
        {{
            if (versionId != null)
                await _userActionService.AddUserAction(UserActionType.Update, UserActionEntityType.{entityName}, notification.Old{entityName}.Id.ToString(), versionId);
            else
                await _userActionService.AddUserAction(UserActionType.Update, UserActionEntityType.{entityName}, notification.Old{entityName}.Id.ToString());
        }}
";

            var handlerUpdateContent = $@"using System;
using Microsoft.Extensions.Logging;
using System.Text;
using Application.Common.Interfaces.Services;
using Application.Common.Interfaces.Services.Versioning;
using Application.Common.Models.AssistantModels;
using Application.Common.Models.Versioning;
using Application.Utilities;
using Domain.Enums;
using Domain.Events.{entityName}Events;
using Application.Common.Extensions;

namespace Application.{entityPlural}.EventHandlers
{{
    public class Update{entityName}EventHandler : INotificationHandler<{entityName}EditedEvent>
    {{
        private readonly ILogger<Update{entityName}EventHandler> _logger;
        private readonly INotificationService _notificationService;
        private readonly IUserNotificationService _userNotificationService;
        private readonly IVersioningService _versioningService;
        private readonly IUserActionService _userActionService;

        public Update{entityName}EventHandler(ILogger<Update{entityName}EventHandler> logger,
                                        INotificationService notificationService,
                                        IUserNotificationService userNotificationService,
                                        IVersioningService versioningService,
                                        IUserActionService userActionService)
        {{
            _logger = logger;
            _notificationService = notificationService;
            _userNotificationService = userNotificationService;
            _versioningService = versioningService;
            _userActionService = userActionService;
        }}
        public async Task Handle({entityName}EditedEvent notification, CancellationToken cancellationToken)
        {{
            {HandleVersioning}

            {HandleUserActon}

            {HandleNotification}
        }}
{HandleVersioningMethod}
{HandleNotificationMethod}
{HandleUserActonMethod}
    }}
    
    
}}
";
            File.WriteAllText(handlerUpdatePath, handlerUpdateContent);
        }
        static void GenerateUpdatedBulkHandler(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<Relation> relations, bool versioning, bool userActon, bool notification)
        {
            string handlerUpdatedClassName = $"UpdatedBulk{entityName}EventHandler";
            string handlerUpdatePath = Path.Combine(path, "..", "..", "Application", $"{entityPlural}", "EventHandlers", $"{handlerUpdatedClassName}.cs");
            string? HandleVersioning = !versioning ? null : $"var versionId = await HandleVersioning(notification);";
            string? HandleUserActon = !userActon ? null
                : !versioning ? "await HandleUserAction(notification);"
                : $@"
            if (!notification.IsVersionedCommand)
                await HandleUserAction(notification, versionId);";
            string? HandleNotification = !notification ? null : $"await HandleNotification(notification, cancellationToken);";

            string aggregator = relations.First(r => r.Type == RelationType.ManyToOne || r.Type == RelationType.ManyToOneNullable).RelatedEntity;
            string nullableAggregatorNot = relations.First(r => r.Type == RelationType.ManyToOne || r.Type == RelationType.ManyToOneNullable).Type == RelationType.ManyToOne ?
                                           $"notification.Old{entityPlural}[0].{aggregator}Id" : $"notification.Old{entityPlural}[0].{aggregator}Id.Value";
            var propList = GetVersionDTOProp(properties, relations);
            StringBuilder versioningDTOBuilder = new StringBuilder();
            foreach (var item in propList)
            {
                if (item.EndsWith("Ids"))
                {
                    var temp = item.Remove(item.Length - 3);
                    string? relatedEntityManyPlural = relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.EndsWith("y") ? relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[..^1] + "ies" : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity + "s";
                    var tempPlural = temp.EndsWith("y") ? temp[..^1] + "ies" : temp + "s";
                    versioningDTOBuilder.AppendLine($"\t\t\t\t{item} = item.{tempPlural}.Select(x => x.Id).ToList(),");
                }
                else
                    versioningDTOBuilder.AppendLine($"\t\t\t\t{item} = item.{item},");
            }

            string? HandleVersioningMethod = !versioning ? null : $@"
        private async Task<string> HandleVersioning({entityName}EditedBulkEvent notification)
        {{
            var oldObjects = notification.Old{entityPlural};
            var newObjects = notification.New{entityPlural};
            //prepare VersioningDTO 
            List<{entityName}VersioningDTO> oldVersioningDTOs = new List<{entityName}VersioningDTO>();
            foreach (var item in oldObjects)
            {{
                {entityName}VersioningDTO versioningDTO = new {entityName}VersioningDTO
                {{
                    Id = item.Id,
                    {versioningDTOBuilder}
                    UniqueIdentifier = item.Id.ToString()
                }};
                 oldVersioningDTOs.Add(versioningDTO);
            }}
            List<{entityName}VersioningDTO> newVersioningDTOs = new List<{entityName}VersioningDTO>();
            foreach (var item in newObjects)
            {{
                {entityName}VersioningDTO versioningDTO = new {entityName}VersioningDTO
                {{
                    Id = item.Id,
                    {versioningDTOBuilder}
                    UniqueIdentifier = item.Id.ToString()
                }};
                 newVersioningDTOs.Add(versioningDTO);
            }}
            notification.ChangeType = 1; // Modified
            notification.EntityType = typeof(List<{entityName}VersioningDTO>);
            notification.OldEntity = oldVersioningDTOs;
            notification.NewEntity = newVersioningDTOs;

            return await _versioningService.AddVersion<{entityName}VersioningDTO>((VersionChangeType)notification.ChangeType, oldObjects[0].{aggregator}Id.ToString(),
                                                                        VersionEntityType.{entityName}, userId: null!, (List<{entityName}VersioningDTO>)notification.OldEntity,
                                                                       (List<{entityName}VersioningDTO>)notification.NewEntity, notification.RollbackedToVersionId);
        }}
";
            string HandleNotificationMethodVersionCase = versioning ? $@"
            if (!notification.IsVersionedCommand)// case of normal update
            {{
                notificationConsistent = NotificationConsistent.{entityPlural}.Edit;
                messageBuilder.Append("" has been modified"");
            }}
            else// case of restore update
            {{
                notificationConsistent = NotificationConsistent.{entityPlural}.Restore;
                messageBuilder.Append("" has been restored"");
            }}
"
:
$@"
            notificationConsistent = NotificationConsistent.{entityPlural}.Edit;
            messageBuilder.Append("" has been modified"");
";
            string? HandleNotificationMethod = !notification ? null : $@"
        private async Task HandleNotification({entityName}EditedBulkEvent notification, CancellationToken cancellationToken, List<string> specificNotifiedUsers = null)
        {{
            // check notification.IsVersionedCommand to determine if notification is from end user or restore prosses, and push it
            (string NotificationMessage, List<string> UsersIsd) signalRMessage;
            string notificationConsistent;

            StringBuilder messageBuilder = new StringBuilder(""List of {entityName} for {aggregator} : "");
            messageBuilder.Append(notification.Old{entityPlural}[0].{aggregator}Id); //TODO:AfterGenerateCode:Replace Id with the proper property
            {HandleNotificationMethodVersionCase}

            signalRMessage = await _userNotificationService.Push(NotificationObjectTypes.{entityName}, {nullableAggregatorNot},
                                                                 notificationConsistent, notificationMessage: messageBuilder.ToString(),
                                                                 cancellationToken, specificNotifiedUsers);

            // Send notification by SignalR
            if (signalRMessage.UsersIsd.Any())
                await _notificationService.SendNotification(message: signalRMessage.NotificationMessage, signalRMessage.UsersIsd);
        }}
";
            string? HandleUserActonMethod = !userActon ? null : $@"
        private async Task HandleUserAction({entityName}EditedBulkEvent notification, string? versionId = null)
        {{
            if (versionId != null)
                await _userActionService.AddUserAction(UserActionType.UpdateBulk, UserActionEntityType.{entityName}, notification.Old{entityPlural}[0].{aggregator}Id.ToString(), versionId);
            else
                await _userActionService.AddUserAction(UserActionType.UpdateBulk, UserActionEntityType.{entityName}, notification.Old{entityPlural}[0].{aggregator}Id.ToString());
        }}
";

            var handlerUpdatedBulkContent = $@"using System;
using Microsoft.Extensions.Logging;
using System.Text;
using Application.Common.Interfaces.Services;
using Application.Common.Interfaces.Services.Versioning;
using Application.Common.Models.AssistantModels;
using Application.Common.Models.Versioning;
using Application.Utilities;
using Domain.Enums;
using Domain.Events.{entityName}Events;

namespace Application.{entityPlural}.EventHandlers
{{
    public class UpdateBulk{entityName}EventHandler : INotificationHandler<{entityName}EditedBulkEvent>
    {{
        private readonly ILogger<UpdateBulk{entityName}EventHandler> _logger;
        private readonly INotificationService _notificationService;
        private readonly IUserNotificationService _userNotificationService;
        private readonly IVersioningService _versioningService;
        private readonly IUserActionService _userActionService;

        public UpdateBulk{entityName}EventHandler(ILogger<UpdateBulk{entityName}EventHandler> logger,
                                        INotificationService notificationService,
                                        IUserNotificationService userNotificationService,
                                        IVersioningService versioningService,
                                        IUserActionService userActionService)
        {{
            _logger = logger;
            _notificationService = notificationService;
            _userNotificationService = userNotificationService;
            _versioningService = versioningService;
            _userActionService = userActionService;
        }}
        public async Task Handle({entityName}EditedBulkEvent notification, CancellationToken cancellationToken)
        {{
            {HandleVersioning}

            {HandleUserActon}

            {HandleNotification}
        }}
{HandleVersioningMethod}
{HandleNotificationMethod}
{HandleUserActonMethod}
    }}
    
    
}}
";
            File.WriteAllText(handlerUpdatePath, handlerUpdatedBulkContent);
        }

        static void GenerateDeleteHandler(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<Relation> relations, bool versioning, bool userActon, bool notification)
        {
            string handlerDeleteClassName = $"Deleted{entityName}EventHandler";
            string handlerDeletePath = Path.Combine(path, "..", "..", "Application", $"{entityPlural}", "EventHandlers", $"{handlerDeleteClassName}.cs");
            string? HandleVersioning = !versioning ? null : $"var versionId = await HandleVersioning(notification);";
            string? HandleUserActon = !userActon ? null
                : !versioning ? "await HandleUserAction(notification);"
                : $@"
            if (!notification.IsVersionedCommand)
                await HandleUserAction(notification, versionId);";
            string? HandleNotification = !notification ? null : $"await HandleNotification(notification, cancellationToken);";

            var propList = GetVersionDTOProp(properties, relations);
            StringBuilder versioningDTOBuilder = new StringBuilder();
            foreach (var item in propList)
            {
                if (item.EndsWith("Ids"))
                {
                    var temp = item.Remove(item.Length - 3);
                    string? relatedEntityManyPlural = relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.EndsWith("y") ? relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[..^1] + "ies" : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity + "s";
                    var tempPlural = temp.EndsWith("y") ? temp[..^1] + "ies" : temp + "s";
                    versioningDTOBuilder.AppendLine($"\t\t\t\t{item} = obj.{tempPlural}.Select(x => x.Id).ToList(),");
                }
                else
                    versioningDTOBuilder.AppendLine($"\t\t\t\t{item} = obj.{item},");
            }

            string? HandleVersioningMethod = !versioning ? null : $@"
        private async Task<string> HandleVersioning({entityName}DeletedEvent notification)
        {{
            var obj = notification.{entityName};
            //prepare VersioningDTO 
            {entityName}VersioningDTO versioningDTO = new {entityName}VersioningDTO
            {{
                Id = obj.Id,
                {versioningDTOBuilder}
                UniqueIdentifier = obj.Id.ToString()
            }};

            notification.ChangeType = 2; // Deleted
            notification.EntityType = typeof({entityName}VersioningDTO);
            notification.OldEntity = versioningDTO;
            notification.NewEntity = null!;

            return await _versioningService.AddVersion<{entityName}VersioningDTO>((VersionChangeType)notification.ChangeType, notification.{entityName}.Id.ToString(),
                                                                        VersionEntityType.{entityName}, userId: null!, ({entityName}VersioningDTO)notification.OldEntity,
                                                                       ({entityName}VersioningDTO)notification.NewEntity, notification.RollbackedToVersionId);
        }}
";

            string HandleNotificationMethodVersionCase = versioning ? $@"
            if (!notification.IsVersionedCommand)// case of normal delete
            {{
                notificationConsistent = NotificationConsistent.{entityPlural}.Delete;
                messageBuilder.Append("" has been deleted "");
            }}
            else// case of restore delete
            {{
                notificationConsistent = NotificationConsistent.{entityPlural}.Restore;
                messageBuilder.Append("" has been restored"");
            }}
"
:
$@"
            notificationConsistent = NotificationConsistent.{entityPlural}.Delete;
            messageBuilder.Append("" has been deleted"");
";
            string? HandleNotificationMethod = !notification ? null : $@"
        private async Task HandleNotification({entityName}DeletedEvent notification, CancellationToken cancellationToken, List<string> specificNotifiedUsers = null)
        {{
            // check notification.IsVersionedCommand to determine if notification is from end user or restore prosses, and push it
            (string NotificationMessage, List<string> UsersIsd) signalRMessage;
            string notificationConsistent;

            StringBuilder messageBuilder = new StringBuilder(""{entityName} : "");
            messageBuilder.Append(notification.{entityName}.Id); //TODO:AfterGenerateCode:Replace Id with the proper property
            {HandleNotificationMethodVersionCase}

            signalRMessage = await _userNotificationService.Push(NotificationObjectTypes.{entityName}, notification.{entityName}.Id,
                                                                 notificationConsistent, notificationMessage: messageBuilder.ToString(),
                                                                 cancellationToken, specificNotifiedUsers);

            // Send notification by SignalR
            if (signalRMessage.UsersIsd.Any())
                await _notificationService.SendNotification(message: signalRMessage.NotificationMessage, signalRMessage.UsersIsd);
        }}
";
            string? HandleUserActonMethod = !userActon ? null : $@"
        private async Task HandleUserAction({entityName}DeletedEvent notification, string? versionId = null)
        {{
            if (versionId != null)
                await _userActionService.AddUserAction(UserActionType.Delete, UserActionEntityType.{entityName}, notification.{entityName}.Id.ToString(), versionId);
            else
                await _userActionService.AddUserAction(UserActionType.Delete, UserActionEntityType.{entityName}, notification.{entityName}.Id.ToString());
        }}
";

            var handlerDeleteContent = $@"using System;
using Microsoft.Extensions.Logging;
using System.Text;
using Application.Common.Interfaces.Services;
using Application.Common.Interfaces.Services.Versioning;
using Application.Common.Models.AssistantModels;
using Application.Common.Models.Versioning;
using Application.Utilities;
using Domain.Enums;
using Domain.Events.{entityName}Events;

namespace Application.{entityPlural}.EventHandlers
{{
    public class Delete{entityName}EventHandler : INotificationHandler<{entityName}DeletedEvent>
    {{
        private readonly ILogger<Delete{entityName}EventHandler> _logger;
        private readonly INotificationService _notificationService;
        private readonly IUserNotificationService _userNotificationService;
        private readonly IVersioningService _versioningService;
        private readonly IUserActionService _userActionService;

        public Delete{entityName}EventHandler(ILogger<Delete{entityName}EventHandler> logger,
                                        INotificationService notificationService,
                                        IUserNotificationService userNotificationService,
                                        IVersioningService versioningService,
                                        IUserActionService userActionService)
        {{
            _logger = logger;
            _notificationService = notificationService;
            _userNotificationService = userNotificationService;
            _versioningService = versioningService;
            _userActionService = userActionService;
        }}
        public async Task Handle({entityName}DeletedEvent notification, CancellationToken cancellationToken)
        {{
            {HandleVersioning}

            {HandleUserActon}

            {HandleNotification}
        }}
{HandleVersioningMethod}
{HandleNotificationMethod}
{HandleUserActonMethod}
    }}
    
    
}}
";
            File.WriteAllText(handlerDeletePath, handlerDeleteContent);
        }
        static void GenerateDeleteBulkHandler(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<Relation> relations, bool versioning, bool userActon, bool notification)
        {
            string handlerDeleteClassName = $"DeletedBulk{entityName}EventHandler";
            string handlerDeletePath = Path.Combine(path, "..", "..", "Application", $"{entityPlural}", "EventHandlers", $"{handlerDeleteClassName}.cs");
            string? HandleVersioning = !versioning ? null : $"var versionId = await HandleVersioning(notification);";
            string? HandleUserActon = !userActon ? null
                : !versioning ? "await HandleUserAction(notification);"
                : $@"
            if (!notification.IsVersionedCommand)
                await HandleUserAction(notification, versionId);";
            string? HandleNotification = !notification ? null : $"await HandleNotification(notification, cancellationToken);";

            string aggregator = relations.First(r => r.Type == RelationType.ManyToOne || r.Type == RelationType.ManyToOneNullable).RelatedEntity;
            string nullableAggregatorNot = relations.First(r => r.Type == RelationType.ManyToOne || r.Type == RelationType.ManyToOneNullable).Type == RelationType.ManyToOne ?
                                           $"notification.Old{entityPlural}[0].{aggregator}Id" : $"notification.Old{entityPlural}[0].{aggregator}Id.Value";
            
            var propList = GetVersionDTOProp(properties, relations);
            StringBuilder versioningDTOBuilder = new StringBuilder();
            foreach (var item in propList)
            {
                if (item.EndsWith("Ids"))
                {
                    var temp = item.Remove(item.Length - 3);
                    string? relatedEntityManyPlural = relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.EndsWith("y") ? relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[..^1] + "ies" : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity + "s";
                    var tempPlural = temp.EndsWith("y") ? temp[..^1] + "ies" : temp + "s";
                    versioningDTOBuilder.AppendLine($"\t\t\t\t{item} = item.{tempPlural}.Select(x => x.Id).ToList(),");
                }
                else
                    versioningDTOBuilder.AppendLine($"\t\t\t\t{item} = item.{item},");
            }

            string? HandleVersioningMethod = !versioning ? null : $@"
        private async Task<string> HandleVersioning({entityName}DeletedBulkEvent notification)
        {{
            var objects = notification.{entityPlural};
            //prepare VersioningDTO 
            List<{entityName}VersioningDTO> versioningDTOs = new List<{entityName}VersioningDTO>();
            foreach (var item in objects)
            {{
                {entityName}VersioningDTO versioningDTO = new {entityName}VersioningDTO
                {{
                    Id = item.Id,
                    {versioningDTOBuilder}
                    UniqueIdentifier = item.Id.ToString()
                }};
                 versioningDTOs.Add(versioningDTO);
            }}

            notification.ChangeType = 2; // Deleted
            notification.EntityType = typeof(List<{entityName}VersioningDTO>);
            notification.OldEntity = versioningDTOs;
            notification.NewEntity = null!;

            return await _versioningService.AddVersion<{entityName}VersioningDTO>((VersionChangeType)notification.ChangeType, objects[0].{aggregator}Id.ToString(),
                                                                        VersionEntityType.{entityName}, userId: null!, (List<{entityName}VersioningDTO>)notification.OldEntity,
                                                                       (List<{entityName}VersioningDTO>)notification.NewEntity, notification.RollbackedToVersionId);
        }}
";

            string HandleNotificationMethodVersionCase = versioning ? $@"
            if (!notification.IsVersionedCommand)// case of normal delete
            {{
                notificationConsistent = NotificationConsistent.{entityPlural}.Delete;
                messageBuilder.Append("" has been deleted "");
            }}
            else// case of restore delete
            {{
                notificationConsistent = NotificationConsistent.{entityPlural}.Restore;
                messageBuilder.Append("" has been restored"");
            }}
"
:
$@"
            notificationConsistent = NotificationConsistent.{entityPlural}.Delete;
            messageBuilder.Append("" has been deleted"");
";
            string? HandleNotificationMethod = !notification ? null : $@"
        private async Task HandleNotification({entityName}DeletedBulkEvent notification, CancellationToken cancellationToken, List<string> specificNotifiedUsers = null)
        {{
            // check notification.IsVersionedCommand to determine if notification is from end user or restore prosses, and push it
            (string NotificationMessage, List<string> UsersIsd) signalRMessage;
            string notificationConsistent;

            StringBuilder messageBuilder = new StringBuilder(""List of {entityName} for {aggregator}: "");
            messageBuilder.Append(notification.{entityPlural}[0].{aggregator}.Id); //TODO:AfterGenerateCode:Replace Id with the proper property
            {HandleNotificationMethodVersionCase}

            signalRMessage = await _userNotificationService.Push(NotificationObjectTypes.{entityName}, notification.{entityPlural}[0].{aggregator}.Id,
                                                                 notificationConsistent, notificationMessage: messageBuilder.ToString(),
                                                                 cancellationToken, specificNotifiedUsers);

            // Send notification by SignalR
            if (signalRMessage.UsersIsd.Any())
                await _notificationService.SendNotification(message: signalRMessage.NotificationMessage, signalRMessage.UsersIsd);
        }}
";
            string? HandleUserActonMethod = !userActon ? null : $@"
        private async Task HandleUserAction({entityName}DeletedBulkEvent notification, string? versionId = null)
        {{
            if (versionId != null)
                await _userActionService.AddUserAction(UserActionType.DeleteBulk, UserActionEntityType.{entityName}, notification.{entityPlural}[0].{aggregator}.Id.ToString(), versionId);
            else
                await _userActionService.AddUserAction(UserActionType.DeleteBulk, UserActionEntityType.{entityName}, notification.{entityPlural}[0].{aggregator}.Id.ToString());
        }}
";

            var handlerDeleteBulkContent = $@"using System;
using Microsoft.Extensions.Logging;
using System.Text;
using Application.Common.Interfaces.Services;
using Application.Common.Interfaces.Services.Versioning;
using Application.Common.Models.AssistantModels;
using Application.Common.Models.Versioning;
using Application.Utilities;
using Domain.Enums;
using Domain.Events.{entityName}Events;

namespace Application.{entityPlural}.EventHandlers
{{
    public class DeleteBulk{entityName}EventHandler : INotificationHandler<{entityName}DeletedBulkEvent>
    {{
        private readonly ILogger<DeleteBulk{entityName}EventHandler> _logger;
        private readonly INotificationService _notificationService;
        private readonly IUserNotificationService _userNotificationService;
        private readonly IVersioningService _versioningService;
        private readonly IUserActionService _userActionService;

        public DeleteBulk{entityName}EventHandler(ILogger<DeleteBulk{entityName}EventHandler> logger,
                                        INotificationService notificationService,
                                        IUserNotificationService userNotificationService,
                                        IVersioningService versioningService,
                                        IUserActionService userActionService)
        {{
            _logger = logger;
            _notificationService = notificationService;
            _userNotificationService = userNotificationService;
            _versioningService = versioningService;
            _userActionService = userActionService;
        }}
        public async Task Handle({entityName}DeletedBulkEvent notification, CancellationToken cancellationToken)
        {{
            {HandleVersioning}

            {HandleUserActon}

            {HandleNotification}
        }}
{HandleVersioningMethod}
{HandleNotificationMethod}
{HandleUserActonMethod}
    }}
    
    
}}
";
            File.WriteAllText(handlerDeletePath, handlerDeleteBulkContent);
        }
        static List<string> GetVersionDTOProp(List<(string Type, string Name, PropertyValidation Validation)> properties,List<Relation> relations)
        {
            List<string> propList = new List<string>();
            properties.ForEach(p => propList.Add(p.Name));

            foreach (var relation in relations)
            {

                switch (relation.Type)
                {
                    case RelationType.OneToOneSelfJoin:
                        propList.Add($"{relation.RelatedEntity}ParentId");
                        break;
                    case RelationType.OneToOne:
                        propList.Add($"{relation.RelatedEntity}Id");
                        break;
                    case RelationType.OneToOneNullable:
                        propList.Add($"{relation.RelatedEntity}Id");
                        break;
                    case RelationType.ManyToOne:
                        propList.Add($"{relation.RelatedEntity}Id");
                        break;
                    case RelationType.ManyToOneNullable:
                        propList.Add($"{relation.RelatedEntity}Id");
                        break;
                    case RelationType.ManyToMany:
                        propList.Add($"{relation.RelatedEntity}Ids");
                        break;
                    default:
                        break;
                }
            }
            return propList;
        }
    }
}
