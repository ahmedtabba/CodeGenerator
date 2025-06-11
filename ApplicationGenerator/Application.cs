using SharedClasses;
using System;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;


namespace ApplicationGenerator
{
    public static class Application
    {
        public static void GenerateIRepositoryInterface(string entityName, string path)
        {
            string fileName = $"I{entityName}Repository.cs";
            string filePath = Path.Combine(path, fileName);

            string content = $@"using Domain.Entities;

namespace Application.Common.Interfaces.IRepositories
{{
    public interface I{entityName}Repository : IRepositoryAsync<{entityName}>
    {{
    }}
}}";
            File.WriteAllText(filePath, content);
        }
        public static void GenerateCreateCommand(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, bool hasLocalization, List<Relation> relations, bool hasVersioning, bool hasNotification, bool hasUserAction)
        {
            string className = $"Create{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            string entityRepoName = $"_{lowerEntityName}";
            var inheritVersion = hasVersioning ? "VersionRequestOfTBase," : null;
            var eventVersionCode = !hasVersioning ? null : $@"
                {lowerEntityName}Event.RollbackedToVersionId = request.RollbackedToVersionId;
                {lowerEntityName}Event.IsVersionedCommand = request.IsVersionedCommand;
";
            var neededUsing = (hasVersioning || hasNotification || hasUserAction) ? $"using Domain.Events.{entityName}Events;using Application.Common.Models.Versioning;" : null;
            var eventCode = !(hasVersioning || hasNotification || hasUserAction) ? null :
                $@"
                var {lowerEntityName}Event = new {entityName}CreatedEvent({entityName.ToLower()});
                {eventVersionCode}
                {entityName.ToLower()}.AddDomainEvent({lowerEntityName}Event);
";

            var propList = new List<string>();
            StringBuilder mapperEnum = new StringBuilder();
            StringBuilder imageCode = new StringBuilder();
            StringBuilder videoCode = new StringBuilder();
            foreach (var prop in properties)
            {
                if (prop.Type == "GPG")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        propList.Add($"\t\tpublic FileDto {prop.Name}File {{ get; set; }} = new FileDto();");
                        imageCode.Append($@"
                {entityName.ToLower()}.{prop.Name} =  await _fileService.UploadFileAsync(request.{prop.Name}File);

");
                    }
                    else
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");
                        imageCode.Append($@"
                {entityName.ToLower()}.{prop.Name} = request.{prop.Name}File != null ? await _fileService.UploadFileAsync(request.{prop.Name}File) : null;

");
                    }
                }
                else if (prop.Type == "PNGs")
                {
                    propList.Add($"\t\tpublic List<FileDto> {prop.Name}Files {{ get; set; }} = new List<FileDto>();");
                    imageCode.Append($@"
                foreach (var item in request.{prop.Name}Files)
                    {{
                        var path = await _fileService.UploadFileAsync(item);
                        {entityName.ToLower()}.{prop.Name}.Add(path);
                    }}

");
                }
                else if (prop.Type == "VD")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        propList.Add($"\t\tpublic FileDto {prop.Name}File {{ get; set; }} = new FileDto();");
                        videoCode.Append($@"
                {entityName.ToLower()}.{prop.Name} =  await _fileService.UploadFileAsync(request.{prop.Name}File);

");
                    }
                    else
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");
                        videoCode.Append($@"
                {entityName.ToLower()}.{prop.Name} = request.{prop.Name}File != null ? await _fileService.UploadFileAsync(request.{prop.Name}File) : null;

");

                    }
                }
                else
                {
                    if (enumProps.Any(p => p.prop == prop.Name))
                    {
                        if (prop.Validation != null && prop.Validation.Required)
                        {
                            propList.Add($"\t\tpublic {entityName}{prop.Name} {prop.Name} {{ get; set; }}");
                            mapperEnum.Append($".ForMember(dest => dest.{prop.Name}, opt => opt.MapFrom(src => (int)src.{prop.Name}))");
                            mapperEnum.AppendLine();
                        }
                        else
                        {
                            propList.Add($"\t\tpublic {entityName}{prop.Name}? {prop.Name} {{ get; set; }}");
                            mapperEnum.Append($".ForMember(dest => dest.{prop.Name}, opt => opt.MapFrom(src => (int?)src.{prop.Name}))");
                            mapperEnum.AppendLine();
                        }
                    }
                    else
                        propList.Add($"\t\tpublic {prop.Type} {prop.Name} {{ get; set; }}");
                }

            }
            var props = string.Join(Environment.NewLine, propList);
            List<string> relationPropsList = new List<string>();
            foreach (var relation in relations)
            {

                string prop = null!;

                switch (relation.Type)
                {
                    case RelationType.OneToOneSelfJoin:
                        prop = $"\t\tpublic Guid? {relation.RelatedEntity}ParentId {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;

                    case RelationType.OneToOne:
                        prop = $"\t\tpublic Guid {relation.RelatedEntity}Id {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;

                    case RelationType.OneToOneNullable:
                        prop = $"\t\tpublic Guid? {relation.RelatedEntity}Id {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;

                    case RelationType.OneToMany:
                        break;

                    case RelationType.OneToManyNullable:
                        break;

                    case RelationType.ManyToOne:
                        prop = $"\t\tpublic Guid {relation.RelatedEntity}Id {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;

                    case RelationType.ManyToOneNullable:
                        prop = $"\t\tpublic Guid? {relation.RelatedEntity}Id {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;
                    case RelationType.ManyToMany:
                        prop = $"\t\tpublic List<Guid> {relation.RelatedEntity}Ids {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;
                    default:
                        break;
                }
            }

            string relationProps = string.Join(Environment.NewLine, relationPropsList);
            string? injectCTORMany1 = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $",I{relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity}Repository {char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository";
            string? injectCTORMany2 = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $"_{char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository = {char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository;";
            string? injectCTORMany3 = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $"private readonly I{relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity}Repository _{char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository;";
            string? relatedEntityManyPlural = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.EndsWith("y") ? relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[..^1] + "ies" : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity + "s";
            string? relatedEntityManyRepo = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $"_{char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository";
            string ? relationManyToManyCode = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $@"
                var objs = await {relatedEntityManyRepo}.GetAllAsTracking()
                        .Where(x => request.{relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity}Ids.Contains(x.Id))
                        .ToListAsync();                

                foreach (var obj in objs)
                {{
                    {entityName.ToLower()}.{relatedEntityManyPlural}.Add(obj);
                }}";

            string? localizationIRepo = hasLocalization ? $",I{entityName}LocalizationRepository {lowerEntityName}LocalizationRepository" : null;
            string? localizationInjectIRepo = hasLocalization ? $"_{lowerEntityName}LocalizationRepository = {lowerEntityName}LocalizationRepository;" : null;
            string? localizationFieldIRepo = hasLocalization ? $"private readonly I{entityName}LocalizationRepository _{lowerEntityName}LocalizationRepository;" : null;
            string? localizationList = hasLocalization ? $"\t\tpublic List<{entityName}LocalizationApp> {entityName}LocalizationApps {{ get; set; }} = new List<{entityName}LocalizationApp>();" : null;

            string? localizationCode = !hasLocalization ? null : $@"
                foreach (var localization in request.{entityName}LocalizationApps)
                {{
                    {entityName}Localization localizationToAdd = new {entityName}Localization
                    {{
                        LanguageId = localization.LanguageId,
                        {entityName}Id = {entityName.ToLower()}.Id,
                        Value = localization.Value,
                        FieldType = (int)localization.FieldType
                    }};
                    await _{lowerEntityName}LocalizationRepository.AddAsync(localizationToAdd);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }}
";
            string content = $@"using Amazon.Runtime.Internal.Util;
using Application.Common.Interfaces.Db;
using Application.Common.Models.Assets;
using Application.Common.Interfaces.Assets;
using Application.Common.Interfaces.IRepositories;
using Application.Common.Models.Localization;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
{neededUsing}
                            
namespace Application.{entityPlural}.Commands.Create{entityName}
{{
    public class {className} : {inheritVersion} IRequest<string> 
    {{
        {props}
        {localizationList}
        {relationProps}
        public class Mapping : Profile
            {{
                public Mapping()
                {{
                    CreateMap<Create{entityName}Command, {entityName}>()
                    {mapperEnum}
                    ;
                }}
            }}
    }}
                            
    public class {className}Handler : IRequestHandler<{className}, string>
    {{
        private readonly ILogger<{className}Handler> _logger;
        private readonly IMapper _mapper;
        private readonly IUnitOfWorkAsync _unitOfWork;
        private readonly IFileService _fileService;
        private readonly I{entityName}Repository {entityRepoName}Repository;
        {localizationFieldIRepo}
        {injectCTORMany3}

        public {className}Handler(ILogger<{className}Handler> logger,
                                        IMapper mapper,
                                        IUnitOfWorkAsync unitOfWork,
                                        IFileService fileService,
                                        I{entityName}Repository repository{localizationIRepo}{injectCTORMany1})
        {{
            {entityRepoName}Repository = repository;
            _mapper=mapper;
            _unitOfWork=unitOfWork;
            _logger=logger;
            _fileService = fileService;
            {localizationInjectIRepo}
            {injectCTORMany2}
        }}
    
        public async Task<string> Handle({className} request, CancellationToken cancellationToken)
        {{
          try
            {{
                await _unitOfWork.BeginTransactionAsync();
                var {entityName.ToLower()} = _mapper.Map<{entityName}>(request);
                {imageCode}
                {videoCode}
                {relationManyToManyCode}
                await {entityRepoName}Repository.AddAsync({entityName.ToLower()});
                {eventCode}
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                {localizationCode}
                //TODO : logic for events here
    
                await _unitOfWork.CommitAsync();
                return {entityName.ToLower()}.Id.ToString();
            }}
            catch (Exception)
            {{
                await _unitOfWork.RollbackAsync();
                throw;
            }}
        }}
    }}
}}";

            File.WriteAllText(filePath, content);
        }
        public static void GenerateCreateBulkCommand(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, bool hasLocalization, List<Relation> relations, bool hasVersioning, bool hasNotification, bool hasUserAction)
        {
            string className = $"CreateBulk{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            string lowerEntityPlural = char.ToLower(entityPlural[0]) + entityPlural.Substring(1);
            string entityRepoName = $"_{lowerEntityName}";

            var inheritVersion = hasVersioning ? "VersionRequestOfTBase," : null;

            var eventVersionCode = !hasVersioning ? null
             : $@"{lowerEntityName}Event.RollbackedToVersionId = request.RollbackedToVersionId;
                {lowerEntityName}Event.IsVersionedCommand = request.IsVersionedCommand;
";

            var neededUsing = (hasVersioning || hasNotification || hasUserAction) ? $"using Domain.Events.{entityName}Events;using Application.Common.Models.Versioning;" : null;

            var eventCode = !(hasVersioning || hasNotification || hasUserAction) ? null :
                $@"
                var {lowerEntityName}Event = new {entityName}CreatedBulkEvent({lowerEntityPlural});
                {eventVersionCode}
                {lowerEntityPlural}.First().AddDomainEvent({lowerEntityName}Event);
";

            string aggregator = relations.First(r => r.Type == RelationType.ManyToOne || r.Type == RelationType.ManyToOneNullable).RelatedEntity;
            string aggregatorField = relations.First(r => r.Type == RelationType.ManyToOne || r.Type == RelationType.ManyToOneNullable).Type == RelationType.ManyToOne ? $"public Guid {aggregator}Id {{ get; set; }}" : $"public Guid? {aggregator}Id {{ get; set; }}";


            StringBuilder imageCode = new StringBuilder();
            StringBuilder videoCode = new StringBuilder();
            foreach (var prop in properties)
            {
                if (prop.Type == "GPG")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        imageCode.Append($@"
                for (int i = 0; i < {lowerEntityPlural}.Count; i++)
                {{
                    {lowerEntityPlural}[i].{prop.Name} = await _fileService.UploadFileAsync(request.Bulk{entityPlural}[i].{prop.Name}File);
                }}
");
                    }
                    else
                    {
                        imageCode.Append($@"
                for (int i = 0; i < {lowerEntityPlural}.Count; i++)
                {{
                    {lowerEntityPlural}[i].{prop.Name} = request.Bulk{entityPlural}[i].{prop.Name}File != null ? await _fileService.UploadFileAsync(request.Bulk{entityPlural}[i].{prop.Name}File) : null;
                }}
");
                    }
                }
                else if (prop.Type == "PNGs")
                {
                    imageCode.Append($@"
                for (int i = 0; i < {lowerEntityPlural}.Count; i++)
                {{
                    foreach (var item in request.Bulk{entityPlural}[i].{prop.Name}Files)
                    {{
                        var path = await _fileService.UploadFileAsync(item);
                        {lowerEntityPlural}[i].{prop.Name}.Add(path);
                    }}
                }}
");
                }
                else if(prop.Type == "VD")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        videoCode.Append($@"
                for (int i = 0; i < {lowerEntityPlural}.Count; i++)
                {{
                    {lowerEntityPlural}[i].{prop.Name} = await _fileService.UploadFileAsync(request.Bulk{entityPlural}[i].{prop.Name}File);
                }}
");
                    }
                    else
                    {
                        videoCode.Append($@"
                for (int i = 0; i < {lowerEntityPlural}.Count; i++)
                {{
                    {lowerEntityPlural}[i].{prop.Name} = request.Bulk{entityPlural}[i].{prop.Name}File != null ? await _fileService.UploadFileAsync(request.Bulk{entityPlural}[i].{prop.Name}File) : null;
                }}
");
                    }
                }

            }
            string? injectCTORMany1 = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $",I{relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity}Repository {char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository";
            string? injectCTORMany2 = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $"_{char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository = {char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository;";
            string? injectCTORMany3 = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $"private readonly I{relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity}Repository _{char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository;";
            string? relatedEntityManyPlural = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.EndsWith("y") ? relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[..^1] + "ies" : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity + "s";
            string? relatedEntityManyRepo = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $"_{char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository";
            string? relationManyToManyCode = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $@"
                for (int i = 0; i < {lowerEntityPlural}.Count; i++)
                {{
                    var objs = await {relatedEntityManyRepo}.GetAllAsTracking()
                        .Where(x => request.Bulk{entityPlural}[i].{relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity}Ids.Contains(x.Id))
                        .ToListAsync();
                    foreach (var obj in objs)
                    {{
                        {lowerEntityPlural}[i].{relatedEntityManyPlural}.Add(obj);
                    }}
                }}";

            string? localizationIRepo = hasLocalization ? $",I{entityName}LocalizationRepository {lowerEntityName}LocalizationRepository" : null;
            string? localizationInjectIRepo = hasLocalization ? $"_{lowerEntityName}LocalizationRepository = {lowerEntityName}LocalizationRepository;" : null;
            string? localizationFieldIRepo = hasLocalization ? $"private readonly I{entityName}LocalizationRepository _{lowerEntityName}LocalizationRepository;" : null;

            string? localizationCode = !hasLocalization ? null : $@"
            for (int i = 0; i < request.Bulk{entityPlural}.Count; i++)
            {{
                foreach (var localization in request.Bulk{entityPlural}[i].{entityName}LocalizationApps)
                {{
                    {entityName}Localization localizationToAdd = new {entityName}Localization
                    {{
                       LanguageId = localization.LanguageId,
                       {entityName}Id = {lowerEntityPlural}[i].Id,
                       Value = localization.Value,
                       FieldType = (int)localization.FieldType
                    }};
                    await _{lowerEntityName}LocalizationRepository.AddAsync(localizationToAdd);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }}
            }}
";
            string content = $@"using Amazon.Runtime.Internal.Util;
using Application.Common.Interfaces.Db;
using Application.Common.Models.Assets;
using Application.Common.Interfaces.Assets;
using Application.Common.Interfaces.IRepositories;
using Application.Common.Models.Localization;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
{neededUsing}
                            
namespace Application.{entityPlural}.Commands.CreateBulk{entityName}
{{
    public class {className} : {inheritVersion} IRequest 
    {{
        public List<Single{entityName}> Bulk{entityPlural} {{ get; set; }} = new List<Single{entityName}>();
        {aggregatorField}
    }}
                            
    public class {className}Handler : IRequestHandler<{className}>
    {{
        private readonly ILogger<{className}Handler> _logger;
        private readonly IMapper _mapper;
        private readonly IUnitOfWorkAsync _unitOfWork;
        private readonly IFileService _fileService;
        private readonly I{entityName}Repository {entityRepoName}Repository;
        {localizationFieldIRepo}
        {injectCTORMany3}

        public {className}Handler(ILogger<{className}Handler> logger,
                                        IMapper mapper,
                                        IUnitOfWorkAsync unitOfWork,
                                        IFileService fileService,
                                        I{entityName}Repository repository{localizationIRepo}{injectCTORMany1})
        {{
            {entityRepoName}Repository = repository;
            _mapper=mapper;
            _unitOfWork=unitOfWork;
            _logger=logger;
            _fileService = fileService;
            {localizationInjectIRepo}
            {injectCTORMany2}
        }}
    
        public async Task Handle({className} request, CancellationToken cancellationToken)
        {{
          try
            {{
                await _unitOfWork.BeginTransactionAsync();
                var {lowerEntityPlural} = _mapper.Map<List<{entityName}>>(request.Bulk{entityPlural});
                {imageCode}
                {videoCode}
                {relationManyToManyCode}
                await {entityRepoName}Repository.AddBulk({lowerEntityPlural});
                {eventCode}
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                {localizationCode}
                //TODO : logic for events here
    
                await _unitOfWork.CommitAsync();
            }}
            catch (Exception)
            {{
                await _unitOfWork.RollbackAsync();
                throw;
            }}
        }}
    }}
}}";

            File.WriteAllText(filePath, content);
        }
        public static void GenerateCreateCommandValidator(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<Relation> relations)
        {
            string className = $"Create{entityName}CommandValidator";
            string commandName = $"Create{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            string? methodsUnique = string.Empty;
            StringBuilder injectCTOR1 = new($"ILogger<{className}> logger");
            foreach (var relation in relations)
            {
                if (relation.Type != RelationType.OneToMany || relation.Type != RelationType.OneToManyNullable)
                    injectCTOR1.Append($",I{relation.RelatedEntity}Repository {char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository");
            }
            StringBuilder injectCTOR2 = new StringBuilder($"\t\t\t_logger = logger;");
            foreach (var relation in relations)
            {
                if (relation.Type != RelationType.OneToMany || relation.Type != RelationType.OneToManyNullable)
                    injectCTOR2.AppendLine($"_{char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository = {char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository;");
            }
            StringBuilder injectCTOR3 = new($"\t\tprivate readonly ILogger<{className}> _logger;");
            foreach (var relation in relations)
            {
                if (relation.Type != RelationType.OneToMany || relation.Type != RelationType.OneToManyNullable)
                    injectCTOR3.AppendLine($"private readonly I{relation.RelatedEntity}Repository _{char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository;");
            }
            StringBuilder rulesStore = new StringBuilder();
            foreach (var item in properties)
            {
                string? rule = GeneratePropertyRules(item);
                if (rule != null)
                    rulesStore.AppendLine(rule);
                if (item.Validation != null && item.Validation.Unique == true)
                {
                    if (!injectCTOR1.ToString().Contains($"I{entityName}Repository {char.ToLower(entityName[0]) + entityName.Substring(1)}Repository"))
                    {
                        injectCTOR1.Append($",I{entityName}Repository {char.ToLower(entityName[0]) + entityName.Substring(1)}Repository");
                        injectCTOR2.Append($"_{char.ToLower(entityName[0]) + entityName.Substring(1)}Repository = {char.ToLower(entityName[0]) + entityName.Substring(1)}Repository;");
                        injectCTOR3.Append($"private readonly I{entityName}Repository _{char.ToLower(entityName[0]) + entityName.Substring(1)}Repository;");
                    }

                    string methodUnique = $@"
        public async Task<bool> Is{item.Name}Unique(Create{entityName}Command command)
        {{
            return !await _{char.ToLower(entityName[0]) + entityName.Substring(1)}Repository.GetAll()
            .AnyAsync(x => x.{item.Name} == command.{item.Name});
        }}
";
                    methodsUnique += methodUnique;
                }
            }
            rulesStore.AppendLine(GenerateRelationRules(relations));
            string rules = string.Join(Environment.NewLine, rulesStore.ToString());

            string content = $@"
using FluentValidation;
using Application.Common.Interfaces.IRepositories;
using Microsoft.Extensions.Logging;

namespace Application.{entityPlural}.Commands.Create{entityName}
{{
    public class {className} : AbstractValidator<{commandName}>
    {{
{injectCTOR3}
        public {className}({injectCTOR1})
        {{
{injectCTOR2}
{rules}           
        }}
{methodsUnique}
{GenerateRelationMethods(relations, commandName)}
    }}
}}";

            File.WriteAllText(filePath, content);
        }
        public static void GenerateCreateBulkCommandValidator(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<Relation> relations)
        {
            string className = $"CreateBulk{entityName}CommandValidator";
            string commandName = $"CreateBulk{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            StringBuilder injectCTOR1 = new($"ILogger<{className}> logger");
            foreach (var relation in relations)
            {
                injectCTOR1.Append($",I{relation.RelatedEntity}Repository {char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository");
            }
            StringBuilder injectCTOR2 = new StringBuilder($"\t\t\t_logger = logger;");
            foreach (var relation in relations)
            {
                injectCTOR2.AppendLine($"_{char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository = {char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository;");
            }
            StringBuilder injectCTOR3 = new($"\t\tprivate readonly ILogger<{className}> _logger;");
            foreach (var relation in relations)
            {
                injectCTOR3.AppendLine($"private readonly I{relation.RelatedEntity}Repository _{char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository;");
            }

            string aggregator = relations.First(r => r.Type == RelationType.ManyToOne || r.Type == RelationType.ManyToOneNullable).RelatedEntity;
            string? aggregatorNullableCheck = relations.First(r => r.Type == RelationType.ManyToOne || r.Type == RelationType.ManyToOneNullable).Type == RelationType.ManyToOne ? null
                : $@"
            if (command.{aggregator}Id is null)
                return true;
";
            string? aggregatorNullableValue = relations.First(r => r.Type == RelationType.ManyToOne || r.Type == RelationType.ManyToOneNullable).Type == RelationType.ManyToOne ? null
                : $".Value";
            string aggregatorRule = $@"
                RuleFor(x => x.{aggregator}Id)
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                    if (!await Is{aggregator}Existed(context.InstanceToValidate))
                    {{
                        context.AddFailure(""{aggregator} is not found"");
                    }}
                }});
";
            string aggregatorMethod = $@"
        public async Task<bool> Is{aggregator}Existed(CreateBulk{entityName}Command command)
        {{
            {aggregatorNullableCheck}
            var {char.ToLower(aggregator[0]) + aggregator.Substring(1)} = await _{char.ToLower(aggregator[0]) + aggregator.Substring(1)}Repository.GetByIdAsync(command.{aggregator}Id{aggregatorNullableValue});

            if ({char.ToLower(aggregator[0]) + aggregator.Substring(1)} is null)
            {{
                return false;
            }}

            return true;
        }}
";
            string? rules = GenerateBulkPropertyRules(properties);
            string? methods = GenerateBulkPropertyMethods(properties, entityName, entityPlural, "Create");
            string? ruleLine = properties.Any(p => p.Validation != null) ? $"RuleFor(x => x.Bulk{entityPlural})" : null;

            string content = $@"
using FluentValidation;
using Application.Common.Interfaces.IRepositories;
using Microsoft.Extensions.Logging;

namespace Application.{entityPlural}.Commands.CreateBulk{entityName}
{{
    public class {className} : AbstractValidator<{commandName}>
    {{
{injectCTOR3}
        public {className}({injectCTOR1})
        {{
{injectCTOR2}
{ruleLine}
{rules} 
{aggregatorRule}
        }}
{methods}
{aggregatorMethod}
    }}
}}";

            File.WriteAllText(filePath, content);
        }

        public static void GenerateUpdateCommand(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, bool hasLocalization, List<Relation> relations, bool hasVersioning, bool hasNotification, bool hasUserAction)
        {
            string className = $"Update{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            string entityRepoName = $"_{lowerEntityName}";

            var inheritVersion = hasVersioning ? "VersionRequestOfTBase," : null;
            var eventVersionCode = !hasVersioning ? null : $@"
                {lowerEntityName}Event.RollbackedToVersionId = request.RollbackedToVersionId;
                {lowerEntityName}Event.IsVersionedCommand = request.IsVersionedCommand;
";
            var neededUsing = (hasVersioning || hasUserAction || hasNotification) ? $"using Domain.Events.{entityName}Events;using Application.Common.Models.Versioning;" : null;
            var deepCopyCode = (hasVersioning || hasUserAction || hasNotification) ? $"var old{entityName} = existingObj.DeepCopyJsonDotNet();" : null;
            var eventCode = !(hasVersioning || hasNotification || hasUserAction) ? null :
                $@"
                var {lowerEntityName}Event = new {entityName}EditedEvent(old{entityName},existingObj);
                {eventVersionCode}
                existingObj.AddDomainEvent({lowerEntityName}Event);
";

            var propList = new List<string>();
            StringBuilder imageCode = new StringBuilder();
            StringBuilder videoCode = new StringBuilder();
            string? oldImageUrl = string.Empty;
            string? oldVideoUrl = string.Empty;
            StringBuilder oldImageToDeleteCode = new StringBuilder();
            StringBuilder oldVideoToDeleteCode = new StringBuilder();
            StringBuilder oldImagesToDeleteCode = new StringBuilder();
            StringBuilder mapperEnum = new StringBuilder();
            foreach (var prop in properties)
            {
                if (prop.Type == "GPG")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");
                        propList.Add($"\t\tpublic string? Old{prop.Name}Url {{ get; set; }}");
                        oldImageUrl = $"var oldImageUrl = existingObj.{prop.Name};";
                        imageCode.Append($@"
                if (request.{prop.Name}File != null)
                    existingObj.{prop.Name} = await _fileService.UploadFileAsync(request.{prop.Name}File);
                else
                    existingObj.{prop.Name} = request.Old{prop.Name}Url!;

");
                        oldImageToDeleteCode.Append($@"
                if (request.{prop.Name}File != null)
                    await _fileService.DeleteFileAsync(oldImageUrl);

");
                    }
                    else
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");
                        propList.Add($"\t\tpublic bool? DeleteOld{prop.Name} {{ get; set; }}");
                        oldImageUrl = $"var oldImageUrl = existingObj.{prop.Name};";
                        imageCode.Append($@"
                if (request.{prop.Name}File != null)
                    existingObj.{prop.Name} = await _fileService.UploadFileAsync(request.{prop.Name}File);
                if (request.DeleteOld{prop.Name} != null && request.DeleteOld{prop.Name}.Value)
                    existingObj.{prop.Name} = null;

");
                        oldImageToDeleteCode.Append($@"
                if (request.{prop.Name}File != null || (request.DeleteOld{prop.Name} != null && request.DeleteOld{prop.Name}.Value))
                    if(oldImageUrl != null )
                    await _fileService.DeleteFileAsync(oldImageUrl);

");
                    }
                }
                else if (prop.Type == "PNGs")
                {
                    propList.Add($"\t\tpublic List<FileDto>? {prop.Name}Files {{ get; set; }} = new List<FileDto>();");
                    propList.Add($"\t\tpublic List<string>? Deleted{prop.Name}URLs {{ get; set; }}");
                    imageCode.Append($@"
                if (request.{prop.Name}Files != null && request.{prop.Name}Files.Any())
                {{

                    //Save old urls
                    var oldImagesURLs = new List<string>();
                    foreach (var item in existingObj.{prop.Name})
                    {{
                        oldImagesURLs.Add(item);
                    }}
                    existingObj.{prop.Name}.Clear();
                    //Add new photos
                    foreach (var image in request.{prop.Name}Files)
                    {{
                        var imageUrl = await _fileService.UploadFileAsync(image);
                        // Add the new URL
                        existingObj.{prop.Name}.Add(imageUrl);
                    }}
                    //Add old photos to entity
                    if (request.Deleted{prop.Name}URLs != null)
                        foreach (var item in oldImagesURLs)
                        {{
                            if (!request.Deleted{prop.Name}URLs.Contains(item))
                                existingObj.{prop.Name}.Add(item);
                        }}
                    else
                        foreach (var item in oldImagesURLs)
                        {{
                            existingObj.{prop.Name}.Add(item);
                        }}
                }}
                else
                {{
                    if (request.Deleted{prop.Name}URLs != null && request.Deleted{prop.Name}URLs.Any())
                    {{
                        var remainingPhotosURLs = new List<string>();
                        foreach (var item in existingObj.{prop.Name})
                        {{
                            if (!request.Deleted{prop.Name}URLs.Contains(item))
                            {{
                                remainingPhotosURLs.Add(item);
                            }}
                        }}
                        existingObj.{prop.Name} = remainingPhotosURLs;
                    }}
                }}

");
                    oldImagesToDeleteCode.Append($@"
                if(request.Deleted{prop.Name}URLs != null)
                    foreach (var path in request.Deleted{prop.Name}URLs)
                    {{
                        await _fileService.DeleteFileAsync(path);
                    }}

");
                }
                else if (prop.Type == "VD")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");
                        propList.Add($"\t\tpublic string? Old{prop.Name}Url {{ get; set; }}");
                        oldVideoUrl = $"var oldVideoUrl = existingObj.{prop.Name};";
                        videoCode.Append($@"
                if (request.{prop.Name}File != null)
                    existingObj.{prop.Name} = await _fileService.UploadFileAsync(request.{prop.Name}File);
                else
                    existingObj.{prop.Name} = request.Old{prop.Name}Url!;

");
                        oldVideoToDeleteCode.Append($@"
                if (request.{prop.Name}File != null)
                    await _fileService.DeleteFileAsync(oldVideoUrl);

");
                    }
                    else
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");
                        propList.Add($"\t\tpublic bool? DeleteOld{prop.Name} {{ get; set; }}");
                        oldVideoUrl = $"var oldVideoUrl = existingObj.{prop.Name};";
                        videoCode.Append($@"
                if (request.{prop.Name}File != null)
                    existingObj.{prop.Name} = await _fileService.UploadFileAsync(request.{prop.Name}File);
                if (request.DeleteOld{prop.Name} != null && request.DeleteOld{prop.Name}.Value)
                    existingObj.{prop.Name} = null;

");
                        oldVideoToDeleteCode.Append($@"
                if (request.{prop.Name}File != null || (request.DeleteOld{prop.Name} != null && request.DeleteOld{prop.Name}.Value))
                    if(oldVideoUrl != null )
                    await _fileService.DeleteFileAsync(oldVideoUrl);

");
                    }
                }
                else
                {
                    if (enumProps.Any(p => p.prop == prop.Name))
                    {
                        if (prop.Validation != null && prop.Validation.Required)
                        {
                            propList.Add($"\t\tpublic {entityName}{prop.Name} {prop.Name} {{ get; set; }}");
                            mapperEnum.Append($".ForMember(dest => dest.{prop.Name}, opt => opt.MapFrom(src => (int)src.{prop.Name}))");
                            mapperEnum.AppendLine();
                        }
                        else
                        {
                            propList.Add($"\t\tpublic {entityName}{prop.Name}? {prop.Name} {{ get; set; }}");
                            mapperEnum.Append($".ForMember(dest => dest.{prop.Name}, opt => opt.MapFrom(src => (int?)src.{prop.Name}))");
                            mapperEnum.AppendLine();
                        }

                    }
                    else
                        propList.Add($"\t\tpublic {prop.Type} {prop.Name} {{ get; set; }}");
                }

            }
            var props = string.Join(Environment.NewLine, propList);

            List<string> relationPropsList = new List<string>();
            foreach (var relation in relations)
            {

                string prop = null!;

                switch (relation.Type)
                {
                    case RelationType.OneToOneSelfJoin:
                        prop = $"\t\tpublic Guid? {relation.RelatedEntity}ParentId {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;

                    case RelationType.OneToOne:
                        prop = $"\t\tpublic Guid {relation.RelatedEntity}Id {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;

                    case RelationType.OneToOneNullable:
                        prop = $"\t\tpublic Guid? {relation.RelatedEntity}Id {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;

                    case RelationType.OneToMany:
                        break;

                    case RelationType.OneToManyNullable:
                        break;

                    case RelationType.ManyToOne:
                        prop = $"\t\tpublic Guid {relation.RelatedEntity}Id {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;

                    case RelationType.ManyToOneNullable:
                        prop = $"\t\tpublic Guid? {relation.RelatedEntity}Id {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;
                    case RelationType.ManyToMany:
                        prop = $"\t\tpublic List<Guid> {relation.RelatedEntity}Ids {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;
                    default:
                        break;
                }
            }
            string relationProps = string.Join(Environment.NewLine, relationPropsList);
            string? injectCTORMany1 = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $",I{relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity}Repository {char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository";
            string? injectCTORMany2 = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $"_{char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository = {char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository;";
            string? injectCTORMany3 = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $"private readonly I{relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity}Repository _{char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository;";
            string? relatedEntityManyName = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity;
            string? relatedEntityManyPlural = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.EndsWith("y") ? relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[..^1] + "ies" : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity + "s";
            string? relatedEntityManyRepo = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $"_{char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository";
            string? relationManyToManyCode = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $@"
                // Get the current {relatedEntityManyPlural} and new IDs
                var current{relatedEntityManyPlural} = existingObj.{relatedEntityManyPlural}.ToList();
                var new{relatedEntityManyName}Ids = request.{relatedEntityManyName}Ids;

                // Get the new {relatedEntityManyPlural} from the repository
                var new{relatedEntityManyPlural} = await {relatedEntityManyRepo}.GetAllAsTracking()
                        .Where(x => new{relatedEntityManyName}Ids.Contains(x.Id))
                        .ToListAsync();

                // Add new {relatedEntityManyPlural} if not already in the existing collection
                foreach (var new{relatedEntityManyName} in new{relatedEntityManyPlural})
                {{
                    if (!current{relatedEntityManyPlural}.Any(x => x.Id == new{relatedEntityManyName}.Id))
                    {{
                        existingObj.{relatedEntityManyPlural}.Add(new{relatedEntityManyName});
                    }}
                }}

                // Remove {{relatedEntityManyPlural}} that are no longer in the updated list
                foreach (var existing{relatedEntityManyName} in current{relatedEntityManyPlural})
                {{
                    if (!new{relatedEntityManyName}Ids.Contains(existing{relatedEntityManyName}.Id))
                    {{
                        existingObj.{relatedEntityManyPlural}.Remove(existing{relatedEntityManyName});
                    }}
                }}";
            string? localizationIRepo = hasLocalization ? $",I{entityName}LocalizationRepository {lowerEntityName}LocalizationRepository" : null;
            string? localizationInjectIRepo = hasLocalization ? $"_{lowerEntityName}LocalizationRepository = {lowerEntityName}LocalizationRepository;" : null;
            string? localizationFieldIRepo = hasLocalization ? $"private readonly I{entityName}LocalizationRepository _{lowerEntityName}LocalizationRepository;" : null;
            string? localizationList = hasLocalization ? $"\t\tpublic List<{entityName}LocalizationApp> {entityName}LocalizationApps {{ get; set; }} = new List<{entityName}LocalizationApp>();" : null;

            string? localizationCode = !hasLocalization ? null : $@"
                //Delete old Localization
                var oldLocalization = await _{lowerEntityName}LocalizationRepository.GetAllAsTracking()
                    .Where(v => v.{entityName}Id == existingObj.Id).ToListAsync();

                foreach (var localization in oldLocalization)
                {{
                    await _{lowerEntityName}LocalizationRepository.DeleteAsync(localization);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }}
                //Add new Localization
                foreach (var localization in request.{entityName}LocalizationApps)
                {{
                    {entityName}Localization localizationToAdd = new {entityName}Localization
                    {{
                        LanguageId = localization.LanguageId,
                        {entityName}Id = existingObj.Id,
                        Value = localization.Value,
                        FieldType = (int)localization.FieldType
                    }};
                    await _{lowerEntityName}LocalizationRepository.AddAsync(localizationToAdd);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }}
";
            string? oldImageUrlLine = hasVersioning ? null : oldImageUrl;
            string? oldVideoUrlLine = hasVersioning ? null : oldVideoUrl;
            string? oldImageToDeleteCodeLine = hasVersioning ? null : oldImageToDeleteCode.ToString();
            string? oldVideoToDeleteCodeLine = hasVersioning ? null : oldVideoToDeleteCode.ToString();
            string? oldImagesToDeleteCodeLine = hasVersioning ? null : oldImagesToDeleteCode.ToString();

            string content = $@"
using Microsoft.Extensions.Logging;
using System;
using Application.Common.Models.Assets;
using Application.Common.Interfaces.Assets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces.Db;
using Application.Common.Interfaces.IRepositories;
using Domain.Entities;
using Domain.Enums;
using Application.Common.Models.Localization;
using Application.Common.Extensions;
{neededUsing}

namespace Application.{entityPlural}.Commands.Update{entityName}
{{
    public class {className} : {inheritVersion} IRequest 
    {{
        public Guid {entityName}Id {{ get; set; }}
        {props}
        {localizationList}
        {relationProps}
        public class Mapping : Profile
        {{
            public Mapping()
            {{
                CreateMap<{className}, {entityName}>()
                {mapperEnum}    
                ;
            }}
        }}
    }}

    public class {className}Handler : IRequestHandler<{className}>
    {{
        private readonly ILogger<{className}Handler> _logger;
        private readonly IMapper _mapper;
        private readonly I{entityName}Repository {entityRepoName}Repository;
        private readonly IUnitOfWorkAsync _unitOfWork;
        private readonly IFileService _fileService;
        {localizationFieldIRepo}
        {injectCTORMany3}

        public {className}Handler(ILogger<{className}Handler> logger,
                                            IMapper mapper,
                                            IUnitOfWorkAsync unitOfWork,
                                            IFileService fileService,
                                            I{entityName}Repository repository{localizationIRepo}{injectCTORMany1})
                                            
        {{
            _logger = logger;
            _mapper = mapper;
            {entityRepoName}Repository = repository;
            _fileService = fileService;
            _unitOfWork = unitOfWork;
            {localizationInjectIRepo}
            {injectCTORMany2}
        }}
        public async Task Handle({className} request, CancellationToken cancellationToken)
        {{
            try
            {{
                await _unitOfWork.BeginTransactionAsync();
                var existingObj = await {entityRepoName}Repository.GetByIdAsync(request.{entityName}Id);
                {deepCopyCode}
                {oldImageUrlLine}
                {oldVideoUrlLine}
                _mapper.Map(request, existingObj);
                {localizationCode}

                {imageCode}
                {videoCode}
                {relationManyToManyCode}
                {eventCode}

                await {entityRepoName}Repository.UpdateAsync(existingObj);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _unitOfWork.CommitAsync();

                {oldImageToDeleteCodeLine}
                {oldVideoToDeleteCodeLine}
                
                {oldImagesToDeleteCodeLine}
            }}
            catch (Exception)
            {{
                await _unitOfWork.RollbackAsync();
                throw;
            }}
        }}
    }}
}}

";

            File.WriteAllText(filePath, content);
        }
        public static void GenerateUpdateBulkCommand(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, bool hasLocalization, List<Relation> relations, bool hasVersioning, bool hasNotification, bool hasUserAction)
        {
            string className = $"UpdateBulk{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            string lowerEntityPlural = char.ToLower(entityPlural[0]) + entityPlural.Substring(1);
            string entityRepoName = $"_{lowerEntityName}";

            var inheritVersion = hasVersioning ? "VersionRequestBase," : null;

            var eventVersionCode = !hasVersioning ? null
             : $@"{lowerEntityName}Event.RollbackedToVersionId = request.RollbackedToVersionId;
                {lowerEntityName}Event.IsVersionedCommand = request.IsVersionedCommand;
";

            var neededUsing = (hasVersioning || hasNotification || hasUserAction) ? $"using Domain.Events.{entityName}Events;using Application.Common.Models.Versioning;" : null;

            var eventCode = !(hasVersioning || hasNotification || hasUserAction) ? null :
                $@"
                var {lowerEntityName}Event = new {entityName}EditedBulkEvent(old{entityPlural}, newObjects);
                {eventVersionCode}
                existingObjects.First().AddDomainEvent({lowerEntityName}Event);
";

            string aggregator = relations.First(r => r.Type == RelationType.ManyToOne || r.Type == RelationType.ManyToOneNullable).RelatedEntity;
            string aggregatorField = relations.First(r => r.Type == RelationType.ManyToOne || r.Type == RelationType.ManyToOneNullable).Type == RelationType.ManyToOne ? $"public Guid {aggregator}Id {{ get; set; }}" : $"public Guid? {aggregator}Id {{ get; set; }}";

            string? imageDeleteLine = null;
            string? imageDeleteUpdateLine = null;
            string? videoDeleteUpdateLine = null;
            string? imageListDeleteLine = null;
            string? imageUpdateCode = null;
            string? imageListUpdateCode = null;
            string? deletedImagesDeclaration = hasVersioning ? null : !properties.Any(p => p.Type == "GPG" || p.Type == "PNGs" || p.Type == "VD") ? null : "List<string> deletedImages = new List<string>();";
            string? videoDeleteLine = null;
            string? videoUpdateCode = null;


            string? deleteOldImageCode = hasVersioning? null : !properties.Any(p => p.Type == "GPG" || p.Type == "PNGs" || p.Type == "VD") ? null : $@"
                foreach (var item in deletedImages)
                {{
                    if(item != null)
                        await _fileService.DeleteFileAsync(item);
                }}
";
            StringBuilder imageCode = new StringBuilder();
            StringBuilder videoCode = new StringBuilder();

            foreach (var prop in properties)
            {
                if (prop.Type == "GPG")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        imageCode.Append($@"{lowerEntityName}ToAdd.{prop.Name} = await _fileService.UploadFileAsync({lowerEntityName}.{prop.Name}File);
");
                        imageDeleteLine = hasVersioning ? null : $"deletedImages.Add({lowerEntityName}.{prop.Name});";
                        imageDeleteUpdateLine = hasVersioning ? null : $"deletedImages.Add({lowerEntityName}ToUpdate.{prop.Name});";
                        imageUpdateCode = $@"
                        if ({lowerEntityName}.{prop.Name}File != null)
                        {{
                            {imageDeleteUpdateLine}
                            {lowerEntityName}ToUpdate.{prop.Name} = await _fileService.UploadFileAsync({lowerEntityName}.{prop.Name}File);
                        }}
                        else
                            {lowerEntityName}ToUpdate.{prop.Name} = {lowerEntityName}.Old{prop.Name}Url!;
";
                    }
                    else
                    {
                        imageCode.Append($@"{lowerEntityName}ToAdd.{prop.Name} = {lowerEntityName}.{prop.Name}File != null ? await _fileService.UploadFileAsync({lowerEntityName}.{prop.Name}File) : null;
");
                        imageDeleteLine = hasVersioning ? null : $"deletedImages.Add({lowerEntityName}.{prop.Name});";
                        imageDeleteUpdateLine = hasVersioning ? null : $"deletedImages.Add({lowerEntityName}ToUpdate.{prop.Name});";
                        imageUpdateCode = $@"
                        if ({lowerEntityName}.{prop.Name}File != null)
                        {{
                            {imageDeleteUpdateLine}
                            {lowerEntityName}ToUpdate.{prop.Name} = await _fileService.UploadFileAsync({lowerEntityName}.{prop.Name}File);
                        }}
                        if ({lowerEntityName}.DeleteOld{prop.Name} != null)
                        {{
                            {imageDeleteUpdateLine}
                            {lowerEntityName}ToUpdate.{prop.Name} = null;
                        }}

";
                    }
                }
                else if (prop.Type == "VD")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        videoCode.Append($@"{lowerEntityName}ToAdd.{prop.Name} = await _fileService.UploadFileAsync({lowerEntityName}.{prop.Name}File);
");
                        videoDeleteLine = hasVersioning ? null : $"deletedImages.Add({lowerEntityName}.{prop.Name});";
                        videoDeleteUpdateLine = hasVersioning ? null : $"deletedImages.Add({lowerEntityName}ToUpdate.{prop.Name});";
                        videoUpdateCode = $@"
                        if ({lowerEntityName}.{prop.Name}File != null)
                        {{
                            {videoDeleteUpdateLine}
                            {lowerEntityName}ToUpdate.{prop.Name} = await _fileService.UploadFileAsync({lowerEntityName}.{prop.Name}File);
                        }}
                        else
                            {lowerEntityName}ToUpdate.{prop.Name} = {lowerEntityName}.Old{prop.Name}Url!;
";
                    }
                    else
                    {
                        videoCode.Append($@"{lowerEntityName}ToAdd.{prop.Name} = {lowerEntityName}.{prop.Name}File != null ? await _fileService.UploadFileAsync({lowerEntityName}.{prop.Name}File) : null;
");
                        videoDeleteLine = hasVersioning ? null : $"deletedImages.Add({lowerEntityName}.{prop.Name});";
                        videoDeleteUpdateLine = hasVersioning ? null : $"deletedImages.Add({lowerEntityName}ToUpdate.{prop.Name});";
                        videoUpdateCode = $@"
                        if ({lowerEntityName}.{prop.Name}File != null)
                        {{
                            {videoDeleteUpdateLine}
                            {lowerEntityName}ToUpdate.{prop.Name} = await _fileService.UploadFileAsync({lowerEntityName}.{prop.Name}File);
                        }}
                        if ({lowerEntityName}.DeleteOld{prop.Name} != null)
                        {{
                            {videoDeleteUpdateLine}
                            {lowerEntityName}ToUpdate.{prop.Name} = null;
                        }}

";
                    }
                }
                else if (prop.Type == "PNGs")
                {
                    imageCode.Append($@"
                        foreach (var item in {lowerEntityName}.{prop.Name}Files)
                        {{
                            var path = await _fileService.UploadFileAsync(item);
                            {lowerEntityName}ToAdd.{prop.Name}.Add(path);
                        }}
");
                    string? imagesDeleteRange = hasVersioning ? null : $@"
                        if ({lowerEntityName}.Deleted{prop.Name}URLs != null)
                            deletedImages.AddRange({lowerEntityName}.Deleted{prop.Name}URLs);
";
                    imageListDeleteLine = hasVersioning ? null :  $"{lowerEntityName}.{prop.Name}.ForEach(deletedImages.Add);";
                    imageListUpdateCode = $@"
                        if ({lowerEntityName}.{prop.Name}Files != null && {lowerEntityName}.{prop.Name}Files.Any())
                        {{

                            //Save old urls
                            var oldImagesURLs = new List<string>();
                            foreach (var item in {lowerEntityName}ToUpdate.{prop.Name})
                            {{
                                oldImagesURLs.Add(item);
                            }}
                            {lowerEntityName}ToUpdate.{prop.Name}.Clear();
                            //Add new photos
                            foreach (var image in {lowerEntityName}.{prop.Name}Files)
                            {{
                                var imageUrl = await _fileService.UploadFileAsync(image);
                                // Add the new URL
                                {lowerEntityName}ToUpdate.{prop.Name}.Add(imageUrl);
                            }}
                            //Add old photos to entity
                            if ({lowerEntityName}.Deleted{prop.Name}URLs != null)
                                foreach (var item in oldImagesURLs)
                                {{
                                    if (!{lowerEntityName}.Deleted{prop.Name}URLs.Contains(item))
                                        {lowerEntityName}ToUpdate.{prop.Name}.Add(item);
                                }}
                            else
                                foreach (var item in oldImagesURLs)
                                {{
                                    {lowerEntityName}ToUpdate.{prop.Name}.Add(item);
                                }}
                        }}
                        else
                        {{
                            if ({lowerEntityName}.Deleted{prop.Name}URLs != null && {lowerEntityName}.Deleted{prop.Name}URLs.Any())
                            {{
                                var remainingPhotosURLs = new List<string>();
                                foreach (var item in {lowerEntityName}ToUpdate.{prop.Name})
                                {{
                                    if (!{lowerEntityName}.Deleted{prop.Name}URLs.Contains(item))
                                    {{
                                        remainingPhotosURLs.Add(item);
                                    }}
                                }}
                                {lowerEntityName}ToUpdate.{prop.Name} = remainingPhotosURLs;
                            }}
                        }}
                        {imagesDeleteRange}
";
                }

            }

            string? injectCTORMany1 = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $",I{relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity}Repository {char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository";
            string? injectCTORMany2 = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $"_{char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository = {char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository;";
            string? injectCTORMany3 = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $"private readonly I{relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity}Repository _{char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository;";
            string? relatedEntityManyPlural = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.EndsWith("y") ? relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[..^1] + "ies" : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity + "s";
            string? relatedEntityManyName = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity;
            string? relatedEntityManyRepo = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $"_{char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository";

            string? relationManyToManyAddCode = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $@"
                        var objs = await {relatedEntityManyRepo}.GetAllAsTracking()
                        .Where(x => {lowerEntityName}.{relatedEntityManyName}Ids.Contains(x.Id))
                        .ToListAsync();                

                        foreach (var obj in objs)
                        {{
                            {lowerEntityName}ToAdd.{relatedEntityManyPlural}.Add(obj);
                        }}";

            string? relationManyToManyUpdateCode = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $@"
                
                        // Get the current many objects and new many objects IDs
                        var currentObjs = {lowerEntityName}ToUpdate.{relatedEntityManyPlural}.ToList();
                        var newObjIds = {lowerEntityName}.{relatedEntityManyName}Ids;

                        // Get the new many objects from the repository
                        var newObjs = await {relatedEntityManyRepo}.GetAllAsTracking()
                            .Where(x => newObjIds.Contains(x.Id))
                            .ToListAsync();

                        // Add new ones if not already in the existing collection
                        foreach (var newObj in newObjs)
                        {{
                            if (!currentObjs.Any(x => x.Id == newObj.Id))
                            {{
                                {lowerEntityName}ToUpdate.{relatedEntityManyPlural}.Add(newObj);
                            }}
                        }}

                        // Remove ones that are no longer in the updated list
                        foreach (var existingObj in currentObjs)
                        {{
                            if (!newObjIds.Contains(existingObj.Id))
                            {{
                                {lowerEntityName}ToUpdate.{relatedEntityManyPlural}.Remove(existingObj);
                            }}
                        }}";


            string? localizationIRepo = hasLocalization ? $",I{entityName}LocalizationRepository {lowerEntityName}LocalizationRepository" : null;
            string? localizationInjectIRepo = hasLocalization ? $"_{lowerEntityName}LocalizationRepository = {lowerEntityName}LocalizationRepository;" : null;
            string? localizationFieldIRepo = hasLocalization ? $"private readonly I{entityName}LocalizationRepository _{lowerEntityName}LocalizationRepository;" : null;

            string? localizationAddCode = !hasLocalization ? null : $@"
                        var currentObj = request.Bulk{entityPlural}.First(x => x.{entityName}Id == {lowerEntityName}.{entityName}Id);
                        foreach (var localization in currentObj.{entityName}LocalizationApps)
                        {{
                            {entityName}Localization localizationToAdd = new {entityName}Localization
                            {{
                                LanguageId = localization.LanguageId,
                                {entityName}Id = {lowerEntityName}ToAdd.Id,
                                Value = localization.Value,
                                FieldType = (int)localization.FieldType
                            }};
                            await _{lowerEntityName}LocalizationRepository.AddAsync(localizationToAdd);
                        }}
";

            string? localizationUpdateCode = !hasLocalization ? null : $@"
                        var currentObj = request.Bulk{entityPlural}.First(x => x.{entityName}Id == {lowerEntityName}.{entityName}Id);
                        foreach (var localization in currentObj.{entityName}LocalizationApps)
                        {{
                            {entityName}Localization localizationToAdd = new {entityName}Localization
                            {{
                                LanguageId = localization.LanguageId,
                                {entityName}Id = {lowerEntityName}ToUpdate.Id,
                                Value = localization.Value,
                                FieldType = (int)localization.FieldType
                            }};
                            await _{lowerEntityName}LocalizationRepository.AddAsync(localizationToAdd);
                        }}
";

            string? localizationDeleteCode = !hasLocalization ? null : $@"
            //Delete old localization
            List<Guid> {lowerEntityPlural}Ids = new List<Guid>();
            request.Bulk{entityPlural}.ForEach(x => {lowerEntityPlural}Ids.Add(x.{entityName}Id));
            var oldLocalization = await _{lowerEntityName}LocalizationRepository.GetAllAsTracking()
                .Where(x => {lowerEntityPlural}Ids.Any(id => id == x.{entityName}Id))
                .ToListAsync();
            foreach (var localization in oldLocalization)
            {{
                await _{lowerEntityName}LocalizationRepository.DeleteAsync(localization);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }}
";

            string content = $@"using Amazon.Runtime.Internal.Util;
using Application.Common.Interfaces.Db;
using Application.Common.Models.Assets;
using Application.Common.Interfaces.Assets;
using Application.Common.Interfaces.IRepositories;
using Application.Common.Models.Localization;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Extensions;
{neededUsing}
                            
namespace Application.{entityPlural}.Commands.UpdateBulk{entityName}
{{
    public class {className} : {inheritVersion} IRequest 
    {{
        public List<SingleUpdated{entityName}> Bulk{entityPlural} {{ get; set; }} = new List<SingleUpdated{entityName}>();
        {aggregatorField}
    }}
                            
    public class {className}Handler : IRequestHandler<{className}>
    {{
        private readonly ILogger<{className}Handler> _logger;
        private readonly IMapper _mapper;
        private readonly IUnitOfWorkAsync _unitOfWork;
        private readonly IFileService _fileService;
        private readonly I{entityName}Repository {entityRepoName}Repository;
        {localizationFieldIRepo}
        {injectCTORMany3}

        public {className}Handler(ILogger<{className}Handler> logger,
                                        IMapper mapper,
                                        IUnitOfWorkAsync unitOfWork,
                                        IFileService fileService,
                                        I{entityName}Repository repository{localizationIRepo}{injectCTORMany1})
        {{
            {entityRepoName}Repository = repository;
            _mapper=mapper;
            _unitOfWork=unitOfWork;
            _logger=logger;
            _fileService = fileService;
            {localizationInjectIRepo}
            {injectCTORMany2}
        }}
    
        public async Task Handle({className} request, CancellationToken cancellationToken)
        {{
          try
            {{
                await _unitOfWork.BeginTransactionAsync();
                var existingObjects = await _{lowerEntityName}Repository.GetAllAsTracking()
                    .Where(x => x.{aggregator}Id == request.{aggregator}Id)
                    .ToListAsync();

                var old{entityPlural} = existingObjects.DeepCopyJsonDotNet();
                {deletedImagesDeclaration}

                {localizationDeleteCode}

                //Add ones that dose not existing
                foreach (var {lowerEntityName} in request.Bulk{entityPlural})
                {{
                    if (!existingObjects.Any(x => x.Id == {lowerEntityName}.{entityName}Id))
                    {{
                        {entityName} {lowerEntityName}ToAdd = _mapper.Map<{entityName}>({lowerEntityName});
                        {imageCode}
                        {videoCode}
                        {relationManyToManyAddCode}
                        await _{lowerEntityName}Repository.AddAsync({lowerEntityName}ToAdd);
                        {localizationAddCode}
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }}
                }}

                //Delete ones that are no longer existed
                foreach (var {lowerEntityName} in existingObjects)
                {{
                    if (!request.Bulk{entityPlural}.Any(x => x.{entityName}Id == {lowerEntityName}.Id))
                    {{
                        {imageDeleteLine}
                        {videoDeleteLine}
                        {imageListDeleteLine}
                        await _{lowerEntityName}Repository.DeleteAsync({lowerEntityName});
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }}
                }}

                //Update existed ones
                foreach (var {lowerEntityName} in request.Bulk{entityPlural})
                {{
                    if (existingObjects.Any(x => x.Id == {lowerEntityName}.{entityName}Id))
                    {{
                        var {lowerEntityName}ToUpdate = existingObjects.First(x => x.Id == {lowerEntityName}.{entityName}Id);
                        _mapper.Map({lowerEntityName}, {lowerEntityName}ToUpdate);
                        {imageUpdateCode}
                        {videoUpdateCode}
                        {imageListUpdateCode}
                        {relationManyToManyUpdateCode}
                        await _{lowerEntityName}Repository.UpdateAsync({lowerEntityName}ToUpdate);
                        {localizationUpdateCode}
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }}
                }}
                var newObjects = await _{lowerEntityName}Repository.GetAll()
                    .Where(x => x.{aggregator}Id == request.{aggregator}Id)
                    .ToListAsync();

                {eventCode}
    
                await _unitOfWork.CommitAsync();
                
                {deleteOldImageCode}
            }}
            catch (Exception)
            {{
                await _unitOfWork.RollbackAsync();
                throw;
            }}
        }}
    }}
}}";

            File.WriteAllText(filePath, content);
        }
        public static void GenerateUpdateCommandValidator(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<Relation> relations)
        {
            string className = $"Update{entityName}CommandValidator";
            string commandName = $"Update{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            string? methodsUnique = string.Empty;
            StringBuilder injectCTOR1 = new($"ILogger<{className}> logger,I{entityName}Repository {char.ToLower(entityName[0]) + entityName.Substring(1)}Repository");
            foreach (var relation in relations)
            {
                if (relation.Type != RelationType.OneToMany || relation.Type != RelationType.OneToManyNullable || relation.Type != RelationType.OneToOneSelfJoin)
                    injectCTOR1.Append($",I{relation.RelatedEntity}Repository {char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository");
            }
            StringBuilder injectCTOR2 = new StringBuilder($"\t\t\t_logger = logger;_{char.ToLower(entityName[0]) + entityName.Substring(1)}Repository = {char.ToLower(entityName[0]) + entityName.Substring(1)}Repository;");
            foreach (var relation in relations)
            {
                if (relation.Type != RelationType.OneToMany || relation.Type != RelationType.OneToManyNullable || relation.Type != RelationType.OneToOneSelfJoin)
                    injectCTOR2.AppendLine($"_{char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository = {char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository;");
            }
            StringBuilder injectCTOR3 = new($"\t\tprivate readonly ILogger<{className}> _logger; private readonly I{entityName}Repository _{char.ToLower(entityName[0]) + entityName.Substring(1)}Repository;");
            foreach (var relation in relations)
            {
                if (relation.Type != RelationType.OneToMany || relation.Type != RelationType.OneToManyNullable || relation.Type != RelationType.OneToOneSelfJoin)
                    injectCTOR3.AppendLine($"private readonly I{relation.RelatedEntity}Repository _{char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository;");
            }
            StringBuilder rulesStore = new StringBuilder();
            foreach (var item in properties)
            {
                string? rule = GeneratePropertyRules(item);
                if (rule != null)
                    rulesStore.AppendLine(rule);
                if (item.Validation != null && item.Validation.Unique == true)
                {
                    string methodUnique = $@"
        public async Task<bool> Is{item.Name}Unique(Update{entityName}Command command)
        {{
            return !await _{char.ToLower(entityName[0]) + entityName.Substring(1)}Repository.GetAll()
            .AnyAsync(x => x.{item.Name} == command.{item.Name} && x.Id != command.{entityName}Id);
        }}
";
                    methodsUnique += methodUnique;
                }

            }
            rulesStore.AppendLine(GenerateRelationRules(relations));
            string rules = string.Join(Environment.NewLine, rulesStore.ToString());

            string content = $@"
using FluentValidation;
using Application.Common.Interfaces.IRepositories;
using Microsoft.Extensions.Logging;
using Application.{entityPlural}.Commands.Update{entityName};

namespace Application.{entityPlural}.Commands.Update{entityName}
{{
    public class {className} : AbstractValidator<{commandName}>
    {{
{injectCTOR3}
        public {className}({injectCTOR1})
        {{
{injectCTOR2}   
{rules}

            RuleFor(l => l.{entityName}Id)
                .NotEmpty().WithMessage(""Id Must be passed"")
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                    if (!await Is{entityName}Existed(context.InstanceToValidate))
                    {{
                        context.AddFailure(""Delete {entityName}"", ""{entityName} is not found !"");
                    }}
                }});
        }}

        public async Task<bool> Is{entityName}Existed({commandName} command)
        {{
            var {entityName.ToLower()} = await _{char.ToLower(entityName[0]) + entityName.Substring(1)}Repository.GetByIdAsync(command.{entityName}Id);

            if ({entityName.ToLower()} is null)
            {{
                return false;
            }}
            
            return true;
        }}
{methodsUnique}
{GenerateRelationMethods(relations, commandName)}
    }}
}}
";

            File.WriteAllText(filePath, content);
        }
        public static void GenerateUpdateBulkCommandValidator(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<Relation> relations)
        {
            string className = $"UpdateBulk{entityName}CommandValidator";
            string commandName = $"UpdateBulk{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            StringBuilder injectCTOR1 = new($"ILogger<{className}> logger,I{entityName}Repository {char.ToLower(entityName[0]) + entityName.Substring(1)}Repository");
            foreach (var relation in relations)
            {
                injectCTOR1.Append($",I{relation.RelatedEntity}Repository {char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository");
            }
            StringBuilder injectCTOR2 = new StringBuilder($"\t\t\t_logger = logger;_{char.ToLower(entityName[0]) + entityName.Substring(1)}Repository = {char.ToLower(entityName[0]) + entityName.Substring(1)}Repository;");
            foreach (var relation in relations)
            {
                injectCTOR2.AppendLine($"_{char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository = {char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository;");
            }
            StringBuilder injectCTOR3 = new($"\t\tprivate readonly ILogger<{className}> _logger; private readonly I{entityName}Repository _{char.ToLower(entityName[0]) + entityName.Substring(1)}Repository;");
            foreach (var relation in relations)
            {
                injectCTOR3.AppendLine($"private readonly I{relation.RelatedEntity}Repository _{char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository;");
            }

            string aggregator = relations.First(r => r.Type == RelationType.ManyToOne || r.Type == RelationType.ManyToOneNullable).RelatedEntity;
            string? aggregatorNullableCheck = relations.First(r => r.Type == RelationType.ManyToOne || r.Type == RelationType.ManyToOneNullable).Type == RelationType.ManyToOne ? null
                : $@"
            if (command.{aggregator}Id is null)
                return true;
";
            string? aggregatorNullableValue = relations.First(r => r.Type == RelationType.ManyToOne || r.Type == RelationType.ManyToOneNullable).Type == RelationType.ManyToOne ? null
                : $".Value";
            string aggregatorRule = $@"
                RuleFor(x => x.{aggregator}Id)
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                    if (!await Is{aggregator}Existed(context.InstanceToValidate))
                    {{
                        context.AddFailure(""{aggregator} is not found"");
                    }}
                }});
";
            string aggregatorMethod = $@"
        public async Task<bool> Is{aggregator}Existed(UpdateBulk{entityName}Command command)
        {{
            {aggregatorNullableCheck}
            var {char.ToLower(aggregator[0]) + aggregator.Substring(1)} = await _{char.ToLower(aggregator[0]) + aggregator.Substring(1)}Repository.GetByIdAsync(command.{aggregator}Id{aggregatorNullableValue});

            if ({char.ToLower(aggregator[0]) + aggregator.Substring(1)} is null)
            {{
                return false;
            }}

            return true;
        }}
";
            string idsRule = $@"
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                    if (!await Are{entityPlural}Existed(context.InstanceToValidate))
                    {{
                        context.AddFailure(""some entities are not found"");
                    }}
                }})
";

            string idsMethod = $@"
        public async Task<bool> Are{entityPlural}Existed(UpdateBulk{entityName}Command command)
        {{
            foreach (var item in command.Bulk{entityPlural})
            {{
                var idString = item.{entityName}Id.ToString();
                if (!idString.EndsWith(""-020304050607""))
                    return await _{char.ToLower(entityName[0]) + entityName.Substring(1)}Repository.GetByIdAsync(item.{entityName}Id) != null;
            }}
            return await Task.FromResult(true);
        }}
";
            string? rules = GenerateBulkPropertyRules(properties);
            string? methods = GenerateBulkPropertyMethods(properties, entityName, entityPlural, "Update");
            string? ruleLine = properties.Any(p => p.Validation != null) ? $"RuleFor(x => x.Bulk{entityPlural})" : null;

            string content = $@"
using FluentValidation;
using Application.Common.Interfaces.IRepositories;
using Microsoft.Extensions.Logging;

namespace Application.{entityPlural}.Commands.UpdateBulk{entityName}
{{
    public class {className} : AbstractValidator<{commandName}>
    {{
{injectCTOR3}
        public {className}({injectCTOR1})
        {{
{injectCTOR2}
{ruleLine}
{idsRule}
{rules} 
{aggregatorRule}
        }}
{methods}
{idsMethod}
{aggregatorMethod}
    }}
}}";

            File.WriteAllText(filePath, content);
        }

        public static void GenerateDeleteCommand(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, bool hasVersioning, bool hasNotification, bool hasUserAction)
        {
            string className = $"Delete{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            string entityRepoName = $"_{lowerEntityName}";

            var inheritVersion = hasVersioning ? "VersionRequestOfTBase," : null;
            var eventVersionCode = !hasVersioning ? null : $@"
                {lowerEntityName}Event.RollbackedToVersionId = request.RollbackedToVersionId;
                {lowerEntityName}Event.IsVersionedCommand = request.IsVersionedCommand;
";
            var neededUsing = (hasVersioning || hasUserAction || hasNotification) ? $"using Domain.Events.{entityName}Events;using Application.Common.Models.Versioning;" : null;
            var eventCode1 = !(hasVersioning || hasUserAction || hasNotification) ? null :
                $@"
                var {lowerEntityName}Event = new {entityName}DeletedEvent({entityName.ToLower()});
                {eventVersionCode}
";
            var eventCode2 = !(hasVersioning || hasUserAction || hasNotification) ? null :
                $@"
                {entityName.ToLower()}.AddDomainEvent({lowerEntityName}Event);
";

            string? deletedImagesVar = hasVersioning ? null : properties.Any(p => (p.Type == "GPG" || p.Type == "PNGs" || p.Type == "VD")) ? $"var deletedImagesPaths = new List<string>();" : null;
            StringBuilder ImageSaveCode = new StringBuilder();
            string? DeleteImagesCode = hasVersioning ? null : properties.Any(p => (p.Type == "GPG" || p.Type == "PNGs" || p.Type == "VD")) ? $@"
                foreach (var path in deletedImagesPaths)
                    {{
                        if (path != null)
                            await _fileService.DeleteFileAsync(path);
                    }}"

                : null;
            foreach (var prop in properties)
            {
                if (prop.Type == "GPG")
                    ImageSaveCode.Append($@"
                deletedImagesPaths.Add({entityName.ToLower()}.{prop.Name});

");
                if (prop.Type == "VD")
                    ImageSaveCode.Append($@"
                deletedImagesPaths.Add({entityName.ToLower()}.{prop.Name});

");
                if (prop.Type == "PNGs")
                    ImageSaveCode.Append($@"
                foreach(var path in {entityName.ToLower()}.{prop.Name})
                    deletedImagesPaths.Add(path);

");
            }

            string? ImageSaveCodeLine = hasVersioning ? null : ImageSaveCode.ToString();
            string content = $@"
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces.Db;
using Application.Common.Interfaces.IRepositories;
using Application.Common.Interfaces.Assets;
using Application.Common.Extensions;
{neededUsing}

namespace Application.{entityPlural}.Commands.Delete{entityName}
{{
    public class {className} : {inheritVersion} IRequest
    {{
        public Guid {entityName}Id {{ get; set; }}
    }}

    public class {className}Handler : IRequestHandler<{className}>
    {{
        private readonly ILogger<{className}Handler> _logger;
        private readonly I{entityName}Repository {entityRepoName}Repository;
        private readonly IUnitOfWorkAsync _unitOfWork;
        private readonly IFileService _fileService;

        public {className}Handler(ILogger<{className}Handler> logger,
                                            I{entityName}Repository {lowerEntityName}Repository,
                                            IUnitOfWorkAsync unitOfWork,
                                            IFileService fileService)
        {{
            _logger = logger;
            {entityRepoName}Repository = {lowerEntityName}Repository;
            _unitOfWork = unitOfWork;
            _fileService = fileService;
        }}

        public async Task Handle({className} request, CancellationToken cancellationToken)
        {{
            try
            {{
                await _unitOfWork.BeginTransactionAsync();
                var {entityName.ToLower()} = await {entityRepoName}Repository.GetByIdAsync(request.{entityName}Id);
                {deletedImagesVar}
                {ImageSaveCodeLine}
                {eventCode1}

                await {entityRepoName}Repository.DeleteAsync({entityName.ToLower()});
                {eventCode2}

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitAsync();
                {DeleteImagesCode}
            }}
            catch (Exception)
            {{
                await _unitOfWork.RollbackAsync();
                throw;
            }}
        }}
    }}
}}


";

            File.WriteAllText(filePath, content);
        }
        public static void GenerateDeleteBulkCommand(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, bool hasLocalization, List<Relation> relations, bool hasVersioning, bool hasNotification, bool hasUserAction)

        {
            string className = $"DeleteBulk{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            string lowerEntityPlural = char.ToLower(entityPlural[0]) + entityPlural.Substring(1);
            string entityRepoName = $"_{lowerEntityName}";

            string? deletedImagesDeclaration = hasVersioning? null : !properties.Any(p => p.Type == "GPG" || p.Type == "PNGs" || p.Type == "VD") ? null : "List<string> deletedImages = new List<string>();";
            string? deleteOldImageCode = hasVersioning? null : !properties.Any(p => p.Type == "GPG" || p.Type == "PNGs" || p.Type == "VD") ? null : $@"
                foreach (var item in deletedImages)
                {{
                    if(item != null)
                        await _fileService.DeleteFileAsync(item);
                }}
";

            var inheritVersion = hasVersioning ? "VersionRequestBase," : null;

            var eventVersionCode = !hasVersioning ? null
             : $@"{lowerEntityName}Event.RollbackedToVersionId = request.RollbackedToVersionId;
                {lowerEntityName}Event.IsVersionedCommand = request.IsVersionedCommand;
";

            var neededUsing = (hasVersioning || hasNotification || hasUserAction) ? $"using Domain.Events.{entityName}Events;using Application.Common.Models.Versioning;" : null;

            var eventCode1 = !(hasVersioning || hasNotification || hasUserAction) ? null :
                $@"
                var {lowerEntityName}Event = new {entityName}DeletedBulkEvent({lowerEntityPlural});
                {eventVersionCode}
";
            var eventCode2 = !(hasVersioning || hasNotification || hasUserAction) ? null :
                $@"
                {lowerEntityPlural}.First().AddDomainEvent({lowerEntityName}Event);
";

            StringBuilder imageCode1 = new StringBuilder();
            foreach (var prop in properties)
            {
                if (prop.Type == "GPG")
                {
                    imageCode1.Append($"deletedImages.Add(item.{prop.Name});\n");
                }
                else if (prop.Type == "VD")
                {
                    imageCode1.Append($"deletedImages.Add(item.{prop.Name});\n");
                }
                else if (prop.Type == "PNGs")
                {
                    imageCode1.Append($"item.{prop.Name}.ForEach(deletedImages.Add);\n");
                }
            }
            string? imageCode = hasVersioning? null : !properties.Any(p => p.Type == "GPG" || p.Type == "PNGs" || p.Type == "VD") ? null : $@"
                foreach (var item in {lowerEntityPlural})
                {{
                    {imageCode1}
                }}
";

            string content = $@"using Amazon.Runtime.Internal.Util;
using Application.Common.Interfaces.Db;
using Application.Common.Models.Assets;
using Application.Common.Interfaces.Assets;
using Application.Common.Interfaces.IRepositories;
using Application.Common.Models.Localization;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
{neededUsing}
                            
namespace Application.{entityPlural}.Commands.DeleteBulk{entityName}
{{
    public class {className} : {inheritVersion} IRequest 
    {{
        public List<Guid> {entityPlural}Ids {{ get; set; }} = new List<Guid>();
    }}
                            
    public class {className}Handler : IRequestHandler<{className}>
    {{
        private readonly ILogger<{className}Handler> _logger;
        private readonly IUnitOfWorkAsync _unitOfWork;
        private readonly IFileService _fileService;
        private readonly I{entityName}Repository {entityRepoName}Repository;

        public {className}Handler(ILogger<{className}Handler> logger,
                                        IUnitOfWorkAsync unitOfWork,
                                        IFileService fileService,
                                        I{entityName}Repository repository)
        {{
            {entityRepoName}Repository = repository;
            _unitOfWork=unitOfWork;
            _logger=logger;
            _fileService = fileService;
        }}
    
        public async Task Handle({className} request, CancellationToken cancellationToken)
        {{
          try
            {{
                await _unitOfWork.BeginTransactionAsync();
                var {lowerEntityPlural} = await {entityRepoName}Repository.GetAllAsTracking()
                    .Where(x => request.{entityPlural}Ids.Any(id => id == x.Id))
                    .ToListAsync();
                {deletedImagesDeclaration}
                {imageCode}
                {eventCode1}
                await {entityRepoName}Repository.DeleteBulk({lowerEntityPlural});
                {eventCode2}

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitAsync();
                {deleteOldImageCode}
            }}
            catch (Exception)
            {{
                await _unitOfWork.RollbackAsync();
                throw;
            }}
        }}
    }}
}}";

            File.WriteAllText(filePath, content);
        }
        public static void GenerateDeleteCommandValidator(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties)
        {
            string className = $"Delete{entityName}CommandValidator";
            string commandName = $"Delete{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            string entityRepoName = $"_{lowerEntityName}";
            string content = $@"
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces.Identity;
using Application.Common.Interfaces.IRepositories;

namespace Application.{entityPlural}.Commands.Delete{entityName}
{{
    public class {className} : AbstractValidator<{commandName}>
    {{
        private readonly ILogger<{className}> _logger;
        private readonly I{entityName}Repository {entityRepoName}Repository;


        public {className}(ILogger<{className}> logger,
                           I{entityName}Repository repository)
        {{
            _logger = logger;
            {entityRepoName}Repository = repository;
            RuleFor(l => l.{entityName}Id)
                .NotEmpty().WithMessage(""Id Must be passed"")
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                    if (!await Is{entityName}Existed(context.InstanceToValidate))
                    {{
                        context.AddFailure(""Delete {entityName}"", ""{entityName} is not found !"");
                    }}
                }});
        }}

        public async Task<bool> Is{entityName}Existed({commandName} command)
        {{
            var {entityName.ToLower()} = await {entityRepoName}Repository.GetByIdAsync(command.{entityName}Id);

            if ({entityName.ToLower()} is null)
            {{
                return false;
            }}
            
            return true;
        }}
    }}
}}
";
            File.WriteAllText(filePath, content);
        }
        public static void GenerateDeleteBulkCommandValidator(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties)
        {
            string className = $"DeleteBulk{entityName}CommandValidator";
            string commandName = $"DeleteBulk{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            string entityRepoName = $"_{lowerEntityName}";
            string content = $@"
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces.Identity;
using Application.Common.Interfaces.IRepositories;

namespace Application.{entityPlural}.Commands.DeleteBulk{entityName}
{{
    public class {className} : AbstractValidator<{commandName}>
    {{
        private readonly ILogger<{className}> _logger;
        private readonly I{entityName}Repository {entityRepoName}Repository;


        public {className}(ILogger<{className}> logger,
                           I{entityName}Repository repository)
        {{
            _logger = logger;
            {entityRepoName}Repository = repository;
            RuleFor(x => x.{entityPlural}Ids)
                .NotEmpty().WithMessage(""Ids Must be passed"")
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                    if (!await Are{entityPlural}Existed(context.InstanceToValidate))
                    {{
                        context.AddFailure(""some {entityPlural} are not found !"");
                    }}
                }});
        }}

        public async Task<bool> Are{entityPlural}Existed({commandName} command)
        {{
            foreach (var item in command.{entityPlural}Ids)
            {{
                if (item != Guid.Empty)
                    return await {entityRepoName}Repository.GetByIdAsync(item) != null;
            }}
            return await Task.FromResult(true);
        }}
    }}
}}
";
            File.WriteAllText(filePath, content);
        }

        public static void GenerateGetByIdQuery(string entityName, string entityPlural, string path, bool hasLocalization, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, List<Relation> relations)
        {
            var folderPath = Path.Combine(path, $"Get{entityName}");
            Directory.CreateDirectory(folderPath);

            GenerateGetByIdDto(entityName, entityPlural, folderPath, properties, enumProps, relations);
            GenerateGetByIdQueryFile(entityName, entityPlural, folderPath, hasLocalization);
            GenerateGetByIdValidator(entityName, entityPlural, folderPath);
        }
        static void GenerateGetByIdDto(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, List<Relation> relations)
        {
            string fileName = $"Get{entityName}Dto.cs";
            string filePath = Path.Combine(path, fileName);
            StringBuilder mapperEnum = new StringBuilder();
            foreach (var prop in properties)
            {
                if (enumProps.Any(p => p.prop == prop.Name))
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        mapperEnum.Append($".ForMember(dest => dest.{prop.Name}, opt => opt.MapFrom(src => ({entityName}{prop.Name})src.{prop.Name}))");
                        mapperEnum.AppendLine();
                    }
                    else
                    {
                        mapperEnum.Append($".ForMember(dest => dest.{prop.Name}, opt => opt.MapFrom(src => ({entityName}{prop.Name}?)src.{prop.Name}))");
                        mapperEnum.AppendLine();
                    }

                }
            }
            string? relationManyMapp = null;
            foreach(var rel in relations)
            {
                if(rel.Type == RelationType.ManyToMany)
                {
                    var relatedEntityName = relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity;
                    string? relatedEntityManyPlural = relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.EndsWith("y") ? relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[..^1] + "ies" : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity + "s";
                    relationManyMapp = $".ForMember(dest => dest.{relatedEntityName}Ids, opt => opt.MapFrom(src => src.{relatedEntityManyPlural}.Select(x => x.Id)))";
                }
            }

            string content = $@"using Domain.Entities;
using Application.Common.Models.AssistantModels;
using Domain.Enums;
namespace Application.{entityPlural}.Queries.Get{entityName}Query
{{
    public class Get{entityName}Dto : {entityName}BaseDto
    {{
        //TODO:AfterGenerateCode: add properties of related entities and update mapper 
        public class Mapping : Profile
        {{
            public Mapping()
            {{
                CreateMap<{entityName}, Get{entityName}Dto>()
                {mapperEnum}
                {relationManyMapp}
                ;
            }}
        }}
    }}
}}";
            File.WriteAllText(filePath, content);
        }
        static void GenerateGetByIdQueryFile(string entityName, string entityPlural, string path, bool hasLocalization)
        {
            string fileName = $"Get{entityName}Query.cs";
            string filePath = Path.Combine(path, fileName);
            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            string entityRepoName = $"_{lowerEntityName}";
            string? languageIdProp = hasLocalization ? $"public Guid? LanguageId {{ get; set; }}" : null;
            string? localizationCode = !hasLocalization ? null : $@"
            if (request.LanguageId != null) 
                await _localizationService.Fill{entityName}Localization(dto, request.LanguageId.Value);";

            string content = $@"
using Microsoft.Extensions.Logging;
using Application.Common.Interfaces.IRepositories;
using Application.Common.Interfaces.Services;

namespace Application.{entityPlural}.Queries.Get{entityName}Query
{{
    public class Get{entityName}Query : IRequest<Get{entityName}Dto>
    {{
        public Guid {entityName}Id {{ get; set; }}
        {languageIdProp}
    }}

    public class Get{entityName}QueryHandler : IRequestHandler<Get{entityName}Query, Get{entityName}Dto>
    {{
        private readonly ILogger<Get{entityName}QueryHandler> _logger;
        private readonly IMapper _mapper;
        private readonly I{entityName}Repository {entityRepoName}Repository;
        private readonly ILocalizationService _localizationService;

        public Get{entityName}QueryHandler(ILogger<Get{entityName}QueryHandler> logger,
                                           IMapper mapper,
                                           ILocalizationService localizationService,
                                           I{entityName}Repository {lowerEntityName}Repository)
        {{
            _logger = logger;
            _mapper = mapper;
            _localizationService = localizationService;
            {entityRepoName}Repository = {lowerEntityName}Repository;
        }}

        public async Task<Get{entityName}Dto> Handle(Get{entityName}Query request, CancellationToken cancellationToken)
        {{
            var {entityName.ToLower()} = await {entityRepoName}Repository.GetByIdAsync(request.{entityName}Id);//TODO:AfterGenerateCode: add method to repository to get object include Navigations if existed
            var dto = _mapper.Map<Get{entityName}Dto>({entityName.ToLower()});

            {localizationCode}
            return dto;
        }}
    }}
}}";
            File.WriteAllText(filePath, content);
        }
        static void GenerateGetByIdValidator(string entityName, string entityPlural, string path)
        {
            string fileName = $"Get{entityName}QueryValidator.cs";
            string filePath = Path.Combine(path, fileName);

            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);

            string content = $@"
using FluentValidation;
using Microsoft.Extensions.Logging;
using Application.Common.Interfaces.IRepositories;

namespace Application.{entityPlural}.Queries.Get{entityName}Query
{{
    public class Get{entityName}QueryValidator : AbstractValidator<Get{entityName}Query>
    {{
        private readonly ILogger<Get{entityName}QueryValidator> _logger;
        private readonly I{entityName}Repository _{lowerEntityName}Repository;

        public Get{entityName}QueryValidator(ILogger<Get{entityName}QueryValidator> logger,
                                             I{entityName}Repository {lowerEntityName}Repository)
        {{
            _logger = logger;
            _{lowerEntityName}Repository = {lowerEntityName}Repository;

            RuleFor(x => x.{entityName}Id)
                .NotEmpty().WithMessage(""{entityName} Id must not be empty"")
                .CustomAsync(async (id, context, cancellationToken) =>
                {{
                    if (!await Is{entityName}Exists(context.InstanceToValidate))
                    {{
                        context.AddFailure(""Get {entityName}"", ""{entityName} not found"");
                    }}
                }});
        }}

        private async Task<bool> Is{entityName}Exists(Get{entityName}Query query)
        {{
            return await _{lowerEntityName}Repository.GetByIdAsync(query.{entityName}Id) != null;
        }}
    }}
}}";
            File.WriteAllText(filePath, content);
        }
        public static void GenerateGetWithLocalizationQuery(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, List<Relation> relations)
        {
            var folderPath = Path.Combine(path, $"Get{entityName}WithLocalization");
            Directory.CreateDirectory(folderPath);
            GenerateGetWithLocalizationDto(entityName, entityPlural, folderPath, properties, enumProps, relations);
            GenerateGetWithLocalizationQueryFile(entityName, entityPlural, folderPath);
            GenerateGetWithLocalizationValidator(entityName, entityPlural, folderPath);
        }
        static void GenerateGetWithLocalizationDto(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, List<Relation> relations)
        {
            string fileName = $"Get{entityName}WithLocalizationDto.cs";
            string filePath = Path.Combine(path, fileName);
            StringBuilder mapperEnum = new StringBuilder();
            foreach (var prop in properties)
            {
                if (enumProps.Any(p => p.prop == prop.Name))
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        mapperEnum.Append($".ForMember(dest => dest.{prop.Name}, opt => opt.MapFrom(src => ({entityName}{prop.Name})src.{prop.Name}))");
                        mapperEnum.AppendLine();
                    }
                    else
                    {
                        mapperEnum.Append($".ForMember(dest => dest.{prop.Name}, opt => opt.MapFrom(src => ({entityName}{prop.Name}?)src.{prop.Name}))");
                        mapperEnum.AppendLine();
                    }

                }
            }
            string? relationManyMapp = null;
            foreach (var rel in relations)
            {
                if (rel.Type == RelationType.ManyToMany)
                {
                    var relatedEntityName = relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity;
                    string? relatedEntityManyPlural = relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.EndsWith("y") ? relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[..^1] + "ies" : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity + "s";
                    relationManyMapp = $".ForMember(dest => dest.{relatedEntityName}Ids, opt => opt.MapFrom(src => src.{relatedEntityManyPlural}.Select(x => x.Id)))";
                }
            }
            string content = $@"using Domain.Entities;
using Application.Common.Models.AssistantModels;
using Domain.Enums;
namespace Application.{entityPlural}.Queries.Get{entityName}WithLocalization
{{
    public class Get{entityName}WithLocalizationDto : {entityName}BaseDto
    {{
        //TODO:AfterGenerateCode: add properties of related entities and update mapper 
        public List<{entityName}LocalizationDto> {entityName}Localizations {{ get; set; }} = new List<{entityName}LocalizationDto>();

        public class Mapping : Profile
        {{
            public Mapping()
            {{
                CreateMap<{entityName}, Get{entityName}WithLocalizationDto>()
                {mapperEnum}
                {relationManyMapp}
                ;
            }}
        }}
    }}
}}";
            File.WriteAllText(filePath, content);

        }
        static void GenerateGetWithLocalizationQueryFile(string entityName, string entityPlural, string path)
        {
            string fileName = $"Get{entityName}WithLocalizationQuery.cs";
            string filePath = Path.Combine(path, fileName);
            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            string entityRepoName = $"_{lowerEntityName}";

            string content = $@"using System;
using Microsoft.Extensions.Logging;
using Application.Common.Interfaces.IRepositories;

namespace Application.{entityPlural}.Queries.Get{entityName}WithLocalization
{{
    public class Get{entityName}WithLocalizationQuery : IRequest<Get{entityName}WithLocalizationDto>
    {{
        public Guid {entityName}Id {{ get; set; }}
    }}

    public class Get{entityName}WithLocalizationQueryHandler : IRequestHandler<Get{entityName}WithLocalizationQuery, Get{entityName}WithLocalizationDto>
    {{
        private readonly ILogger<Get{entityName}WithLocalizationQueryHandler> _logger;
        private readonly IMapper _mapper;
        private readonly I{entityName}Repository {entityRepoName}Repository;

        public Get{entityName}WithLocalizationQueryHandler(ILogger<Get{entityName}WithLocalizationQueryHandler> logger,
                                           IMapper mapper,
                                           I{entityName}Repository {lowerEntityName}Repository)
        {{
            _logger = logger;
            _mapper = mapper;
            {entityRepoName}Repository = {lowerEntityName}Repository;
        }}

        public async Task<Get{entityName}WithLocalizationDto> Handle(Get{entityName}WithLocalizationQuery request, CancellationToken cancellationToken)
        {{
            var {entityName.ToLower()} = await {entityRepoName}Repository.GetByIdAsync(request.{entityName}Id);//TODO:AfterGenerateCode: add method to repository to get object include Localization
            var result = _mapper.Map<Get{entityName}WithLocalizationDto>({entityName.ToLower()});

            return result;
        }}
    }}
}}";
            File.WriteAllText(filePath, content);
        }

        static void GenerateGetWithLocalizationValidator(string entityName, string entityPlural, string path)
        {
            string fileName = $"Get{entityName}WithLocalizationQueryValidator.cs";
            string filePath = Path.Combine(path, fileName);

            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);

            string content = $@"
using FluentValidation;
using Microsoft.Extensions.Logging;
using Application.Common.Interfaces.IRepositories;

namespace Application.{entityPlural}.Queries.Get{entityName}WithLocalization
{{
    public class Get{entityName}WithLocalizationQueryValidator : AbstractValidator<Get{entityName}WithLocalizationQuery>
    {{
        private readonly ILogger<Get{entityName}WithLocalizationQueryValidator> _logger;
        private readonly I{entityName}Repository _{lowerEntityName}Repository;

        public Get{entityName}WithLocalizationQueryValidator(ILogger<Get{entityName}WithLocalizationQueryValidator> logger,
                                             I{entityName}Repository {lowerEntityName}Repository)
        {{
            _logger = logger;
            _{lowerEntityName}Repository = {lowerEntityName}Repository;

            RuleFor(x => x.{entityName}Id)
                .NotEmpty().WithMessage(""{entityName} Id must not be empty"")
                .CustomAsync(async (id, context, cancellationToken) =>
                {{
                    if (!await Is{entityName}Exists(context.InstanceToValidate))
                    {{
                        context.AddFailure(""Get {entityName}"", ""{entityName} not found"");
                    }}
                }});
        }}

        private async Task<bool> Is{entityName}Exists(Get{entityName}WithLocalizationQuery query)
        {{
            return await _{lowerEntityName}Repository.GetByIdAsync(query.{entityName}Id) != null;
        }}
    }}
}}";
            File.WriteAllText(filePath, content);
        }
        ////
        public static void GenerateGetWithPaginationQuery(string entityName, string entityPlural, string path, bool hasLocalization, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, List<Relation> relations)
        {
            var folderPath = Path.Combine(path, $"Get{entityPlural}WithPagination");
            Directory.CreateDirectory(folderPath);

            GenerateGetWithPaginationDto(entityName, entityPlural, folderPath, properties, enumProps, relations);
            GenerateGetWithPaginationQueryFile(entityName, entityPlural, folderPath, hasLocalization, relations);
            GenerateGetWithPaginationValidator(entityName, entityPlural, folderPath);
        }
        static void GenerateGetWithPaginationDto(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, List<Relation> relations)
        {
            string fileName = $"Get{entityPlural}WithPaginationDto.cs";
            string filePath = Path.Combine(path, fileName);

            StringBuilder mapperEnum = new StringBuilder();
            foreach (var prop in properties)
            {
                if (enumProps.Any(p => p.prop == prop.Name))
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        mapperEnum.Append($".ForMember(dest => dest.{prop.Name}, opt => opt.MapFrom(src => ({entityName}{prop.Name})src.{prop.Name}))");
                        mapperEnum.AppendLine();
                    }
                    else
                    {
                        mapperEnum.Append($".ForMember(dest => dest.{prop.Name}, opt => opt.MapFrom(src => ({entityName}{prop.Name}?)src.{prop.Name}))");
                        mapperEnum.AppendLine();
                    }

                }
            }
            string? relationManyMapp = null;
            foreach (var rel in relations)
            {
                if (rel.Type == RelationType.ManyToMany)
                {
                    var relatedEntityName = relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity;
                    string? relatedEntityManyPlural = relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.EndsWith("y") ? relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[..^1] + "ies" : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity + "s";
                    relationManyMapp = $".ForMember(dest => dest.{relatedEntityName}Ids, opt => opt.MapFrom(src => src.{relatedEntityManyPlural}.Select(x => x.Id)))";
                }
            }
            string content = $@"
using Domain.Entities;
using Domain.Enums;

namespace Application.{entityPlural}.Queries.Get{entityPlural}WithPagination
{{
    public class Get{entityPlural}WithPaginationDto : {entityName}BaseDto
    {{
        public class Mapping : Profile
        {{ //TODO:AfterGenerateCode: add properties of related entities and update mapper 
            public Mapping()
            {{
                CreateMap<{entityName}, Get{entityPlural}WithPaginationDto>()
                {mapperEnum}
                {relationManyMapp}
                ;
            }}
        }}
    }}
}}";
            File.WriteAllText(filePath, content);
        }
        static void GenerateGetWithPaginationQueryFile(string entityName, string entityPlural, string path, bool hasLocalization, List<Relation> relations)
        {
            string fileName = $"Get{entityPlural}WithPaginationQuery.cs";
            string filePath = Path.Combine(path, fileName);

            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            string? languageIdProp = hasLocalization ? $"public Guid? LanguageId {{ get; set; }}" : null;
            string? localizationCode = !hasLocalization ? null : $@"
            if (request.LanguageId != null) 
                await _localizationService.Fill{entityName}Localization(result, request.LanguageId.Value);";

            StringBuilder filtersProps = new StringBuilder();
            List<string> filtersList = new List<string>();
            StringBuilder filters = new StringBuilder();
            foreach (var relation in relations)
            {
                if (relation.Type == RelationType.OneToOneSelfJoin || relation.Type == RelationType.OneToOne || relation.Type == RelationType.OneToOneNullable ||
                    relation.Type == RelationType.ManyToOne || relation.Type == RelationType.ManyToOneNullable || relation.Type == RelationType.ManyToMany)
                {
                    filtersProps.Append($"public Guid? {relation.RelatedEntity}Id {{get; set; }}\n");
                    if (relation.Type == RelationType.ManyToMany)
                    {
                        string? relatedEntityManyPlural = relation.RelatedEntity.EndsWith("y") ? relation.RelatedEntity[..^1] + "ies" : relation.RelatedEntity + "s";
                        filters.Append($@"
            if (request.{relation.RelatedEntity}Id != null)
                query = query
                    .Where(y => y.{relatedEntityManyPlural}.Any(x => x.Id == request.{relation.RelatedEntity}Id));
");
                    }
                    else
                        filtersList.Add($"{relation.RelatedEntity}Id");

                }
            }
            foreach (var prop in filtersList)
            {
                filters.Append($@"
            if (request.{prop} != null)
                query = query
                    .Where(x => x.{prop} == request.{prop});

");
            }
            string content = $@"
using Microsoft.Extensions.Logging;
using Application.Common.Interfaces.IRepositories;
using Application.Common.Models;
using Application.Utilities;
using AutoMapper.QueryableExtensions;
using Application.Common.Mappings;
using Application.Common.Interfaces.Services;
using Application.Common.Extensions;

namespace Application.{entityPlural}.Queries.Get{entityPlural}WithPagination
{{
    public class Get{entityPlural}WithPaginationQuery : IRequest<PaginatedList<Get{entityPlural}WithPaginationDto>>
    {{
        public int PageNumber {{ get; init; }} = 1;
        public int PageSize {{ get; init; }} = 10;
        public string? SearchText {{ get; set; }}
        public string? Sort {{ get; set; }}
        {languageIdProp}
        {filtersProps}
        public List<FilterCriteria> Filters {{ get; set; }} = new();
    }}

    public class Get{entityPlural}WithPaginationQueryHandler : IRequestHandler<Get{entityPlural}WithPaginationQuery, PaginatedList<Get{entityPlural}WithPaginationDto>>
    {{
        private readonly ILogger<Get{entityPlural}WithPaginationQueryHandler> _logger;
        private readonly IMapper _mapper;
        private readonly I{entityName}Repository _{lowerEntityName}Repository;
        private readonly ILocalizationService _localizationService;

        public Get{entityPlural}WithPaginationQueryHandler(ILogger<Get{entityPlural}WithPaginationQueryHandler> logger,
                                                           IMapper mapper,
                                                           ILocalizationService localizationService,
                                                           I{entityName}Repository repository)
        {{
            _logger = logger;
            _mapper = mapper;
            _localizationService = localizationService;
            _{lowerEntityName}Repository = repository;
        }}

        public async Task<PaginatedList<Get{entityPlural}WithPaginationDto>> Handle(Get{entityPlural}WithPaginationQuery request, CancellationToken cancellationToken)
        {{
            var query = _{lowerEntityName}Repository.GetAll();//TODO:AfterGenerateCode: add method to repository to get Queryable<object> include Navigations for filters

            //if (!string.IsNullOrWhiteSpace(request.SearchText)) //TODO:AfterGenerateCode: replace Name with proper property to apply SearchText filter
            //    query = query.Where(x => x.Name.ToLower().Contains(request.SearchText.ToLower()));

            {filters}

            var result = await query
                .ProjectTo<Get{entityPlural}WithPaginationDto>(_mapper.ConfigurationProvider)
                .ApplyFilters(request.Filters)
                .OrderBy(request.Sort)
                .PaginatedListAsync(request.PageNumber, request.PageSize);

            {localizationCode}

            return result;
        }}
    }}
}}";
            File.WriteAllText(filePath, content);
        }
        static void GenerateGetWithPaginationValidator(string entityName, string entityPlural, string path)
        {
            string fileName = $"Get{entityPlural}WithPaginationQueryValidator.cs";
            string filePath = Path.Combine(path, fileName);

            string content = $@"
using Microsoft.Extensions.Logging;

namespace Application.{entityPlural}.Queries.Get{entityPlural}WithPagination
{{
    public class Get{entityPlural}WithPaginationQueryValidator : AbstractValidator<Get{entityPlural}WithPaginationQuery>
    {{
        private readonly ILogger<Get{entityPlural}WithPaginationQueryValidator> _logger;

        public Get{entityPlural}WithPaginationQueryValidator(ILogger<Get{entityPlural}WithPaginationQueryValidator> logger)
        {{
            _logger = logger;

            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1).WithMessage(""PageNumber must be >= 1"");

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(-1).WithMessage(""PageSize must be >= -1"");
        }}
    }}
}}";
            File.WriteAllText(filePath, content);
        }
        public static void GenerateBaseDto(string entityName, string entityPlural, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, string solutionDir, List<Relation> relations, bool hasLocalization)
        {
            var filePath = Path.Combine(solutionDir, "Application", entityPlural, "Queries", $"{entityName}BaseDto.cs");
            if (File.Exists(filePath))
            {
                //Console.WriteLine($"ℹ️ {entityName}BaseDto.cs already exists. Skipping...");
                return;
            }
            List<string> relationsProps = new List<string>();
            foreach (var relation in relations)
            {
                switch (relation.Type)
                {
                    case RelationType.OneToOneSelfJoin:
                        relationsProps.Add($"\t\tpublic Guid? {relation.RelatedEntity}Id {{  get; set; }}\n");
                        relationsProps.Add($"\t\tpublic string? {relation.RelatedEntity}{relation.DisplayedProperty} {{  get; set; }}\n");
                        break;
                    case RelationType.OneToOne:
                        relationsProps.Add($"\t\tpublic Guid {relation.RelatedEntity}Id {{  get; set; }}\n");
                        relationsProps.Add($"\t\tpublic string {relation.RelatedEntity}{relation.DisplayedProperty} {{  get; set; }}\n");

                        break;
                    case RelationType.OneToOneNullable:
                        relationsProps.Add($"\t\tpublic Guid? {relation.RelatedEntity}Id {{  get; set; }}\n");
                        relationsProps.Add($"\t\tpublic string? {relation.RelatedEntity}{relation.DisplayedProperty} {{  get; set; }}\n");

                        break;
                    case RelationType.ManyToOne:
                        relationsProps.Add($"\t\tpublic Guid {relation.RelatedEntity}Id {{  get; set; }}\n");
                        relationsProps.Add($"\t\tpublic string {relation.RelatedEntity}{relation.DisplayedProperty} {{  get; set; }}\n");

                        break;
                    case RelationType.ManyToOneNullable:
                        relationsProps.Add($"\t\tpublic Guid? {relation.RelatedEntity}Id {{  get; set; }}\n");
                        relationsProps.Add($"\t\tpublic string? {relation.RelatedEntity}{relation.DisplayedProperty} {{  get; set; }}\n");

                        break;
                    case RelationType.ManyToMany:
                        string displayedPropertyPlural = relation.DisplayedProperty.EndsWith("y") ? relation.DisplayedProperty[..^1] + "ies" : relation.DisplayedProperty + "s";
                        relationsProps.Add($"\t\tpublic List<Guid> {relation.RelatedEntity}Ids {{  get; set; }}\n");
                        relationsProps.Add($"\t\tpublic List<string> {relation.RelatedEntity}{displayedPropertyPlural} {{  get; set; }}\n");

                        break;
                    default:
                        break;
                }
            }
            var relationsPropsList = string.Join(Environment.NewLine, relationsProps);
            //
            var propList = new List<string>();
            foreach (var prop in properties)
            {
                if (prop.Type == "GPG")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        propList.Add($"\t\tpublic string {prop.Name} {{ get; set; }}");
                    }
                    else
                    {
                        propList.Add($"\t\tpublic string? {prop.Name} {{ get; set; }}");
                    }
                }
                else if (prop.Type == "PNGs")
                {
                    propList.Add($"\t\tpublic List<string> {prop.Name} {{ get; set; }} = new List<string>();");
                }
                else if (prop.Type == "VD")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        propList.Add($"\t\tpublic string {prop.Name} {{ get; set; }}");
                    }
                    else
                    {
                        propList.Add($"\t\tpublic string? {prop.Name} {{ get; set; }}");
                    }
                }
                else
                {
                    if (enumProps.Any(p => p.prop == prop.Name))
                    {
                        if (prop.Validation != null && prop.Validation.Required)
                        {
                            propList.Add($"\t\tpublic {entityName}{prop.Name} {prop.Name} {{ get; set; }}");
                        }
                        else
                        {
                            propList.Add($"\t\tpublic {entityName}{prop.Name}? {prop.Name} {{ get; set; }}");
                        }
                    }
                    else
                        propList.Add($"\t\tpublic {prop.Type} {prop.Name} {{ get; set; }}");
                }

            }
            //
            var props = string.Join(Environment.NewLine, propList);

            string content = $@"
using System;
using Domain.Enums;
namespace Application.{entityPlural}.Queries
{{
    public class {entityName}BaseDto
    {{
        public Guid Id {{ get; set; }}
{props}
{relationsPropsList}
    }}
}}";

            File.WriteAllText(filePath, content);
            //Console.WriteLine($"✅ {entityName}BaseDto.cs created.");


            string? fileLocalizationDtoPath = !hasLocalization ? null : Path.Combine(solutionDir, "Application", entityPlural, "Queries", $"{entityName}LocalizationDto.cs");
            string? localizationDtoContent = !hasLocalization ? null : $@"using System;
using Domain.Entities;
using Domain.Enums;

namespace Application.{entityPlural}.Queries
{{
    public class {entityName}LocalizationDto
    {{
        public Guid Id {{ get; set; }}
        public Guid LanguageId {{ get; set; }}
        public Guid {entityName}Id {{ get; set; }}
        public {entityName}LocalizationFieldType {entityName}LocalizationFieldType {{ get; set; }}
        public string Value {{ get; set; }} = null!;

        public class Mapping : Profile
        {{
            public Mapping() 
            {{
                CreateMap<{entityName}Localization, {entityName}LocalizationDto>()
                    .ForMember(dest => dest.{entityName}LocalizationFieldType, opt => opt.MapFrom(src =>({entityName}LocalizationFieldType) src.FieldType));
            }}
        }}
    }}
}}";
            if (hasLocalization)
            {
                File.WriteAllText(fileLocalizationDtoPath!, localizationDtoContent);
                //Console.WriteLine($"✅ {entityName}LocalizationDto.cs created.");
            }
        }

        static string? GeneratePropertyRules((string Type, string Name, PropertyValidation? Validation) property)
        {
            var rules = new StringBuilder();
            if (property.Validation == null)
            {
                return null;
            }
            // Start with the RuleFor declaration
            if (property.Type == "GPG")
            {
                rules.AppendLine($"            RuleFor(x => x.{property.Name}File)");
            }
            else if (property.Type == "PNGs")
            {
                rules.AppendLine($"            RuleFor(x => x.{property.Name}Files)");
            }
            else if (property.Type == "VD")
            {
                rules.AppendLine($"            RuleFor(x => x.{property.Name}File)");
            }
            else
                rules.AppendLine($"            RuleFor(x => x.{property.Name})");

            // Handle required validation differently based on type
            if (property.Validation.Required)
            {
                //rules.Append(".");

                // Use NotNull() for numeric types, NotEmpty() for strings
                if (property.Type == "int" || property.Type == "decimal" || property.Type == "float" || property.Type == "double" || property.Type == "GPG" || property.Type == "VD")
                {
                    rules.AppendLine($"\t\t\t\t.NotNull().WithMessage(\"{property.Name} is required.\")");
                }
                else if (property.Type == "string" || property.Type == "PNGs" || property.Type.Contains("List<") || property.Type.Contains("Date") || property.Type.Contains("Time"))
                {
                    rules.AppendLine($"\t\t\t\t.NotEmpty().WithMessage(\"{property.Name} is required.\")");
                }
                else if (property.Type == "bool")
                    rules.AppendLine($"\t\t\t\t.Must(x => x == true || x == false).WithMessage(\"{property.Name} is required.\")");
            }

            // Handle string-specific validations
            if (property.Type == "string")
            {
                if (property.Validation.MinLength.HasValue)
                {
                    rules.AppendLine($"\t\t\t\t.MinimumLength({property.Validation.MinLength.Value}).WithMessage(\"Minimum Length of {property.Name} is {property.Validation.MinLength.Value} char.\")");
                }

                if (property.Validation.MaxLength.HasValue)
                {
                    rules.AppendLine($"\t\t\t\t.MaximumLength({property.Validation.MaxLength.Value}).WithMessage(\"Maximum Length of {property.Name} is {property.Validation.MaxLength.Value} char.\")");
                }
            }
            // Handle numeric validations
            else if (property.Type == "int" || property.Type == "decimal" || property.Type == "float" || property.Type == "double")
            {
                if (property.Validation.MinRange.HasValue)
                {
                    rules.AppendLine($"\t\t\t\t.GreaterThanOrEqualTo({property.Validation.MinRange.Value}).WithMessage(\"{property.Name} must be Greater Than Or Equal To {property.Validation.MinRange}.\")");
                }

                if (property.Validation.MaxRange.HasValue)
                {
                    rules.AppendLine($"\t\t\t\t.LessThanOrEqualTo({property.Validation.MaxRange.Value}).WithMessage(\"{property.Name} must Less Than Or Equal To {property.Validation.MaxRange}.\")");
                }
            }

            if (property.Validation.Unique)
            {
                rules.AppendLine($@"
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                    if (!await Is{property.Name}Unique(context.InstanceToValidate))
                    {{
                        context.AddFailure(""{property.Name} must be unique."");
                    }}
                }})");
            }
            //Add ;
            if (!string.IsNullOrWhiteSpace(rules.ToString().TrimEnd()))
            {
                rules.Append(";");
            }

            return rules.ToString();
        }
        static string? GenerateBulkPropertyRules(List<(string Type, string Name, PropertyValidation Validation)> properties)
        {
            var rules = new StringBuilder();
            foreach (var property in properties)
            {
                if (property.Validation == null)
                {
                    continue;
                }

                // Handle required validation differently based on type
                if (property.Validation.Required)
                {
                    //rules.Append(".");

                    // numeric types or DateTime don't need validation, string / Images / Lists need
                    if (property.Type == "GPG")
                    {
                        rules.AppendLine($@"
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                    if (!await Is{property.Name}Valid(context.InstanceToValidate))
                    {{
                        context.AddFailure(""some entries have invalid {property.Name} required value"");
                    }}
                }})
");
                    }
                    else if (property.Type == "VD")
                    {
                        rules.AppendLine($@"
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                    if (!await Is{property.Name}Valid(context.InstanceToValidate))
                    {{
                        context.AddFailure(""some entries have invalid {property.Name} required value"");
                    }}
                }})
");
                    }
                    else if (property.Type == "string")
                    {
                        rules.AppendLine($@"
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                    if (!await Is{property.Name}Valid(context.InstanceToValidate))
                    {{
                        context.AddFailure(""some entries have invalid {property.Name} required value"");
                    }}
                }})
");
                    }
                    else if (property.Type == "PNGs")
                    {
                        rules.AppendLine($@"
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                    if (!await Is{property.Name}Valid(context.InstanceToValidate))
                    {{
                        context.AddFailure(""some entries have invalid {property.Name} required value"");
                    }}
                }})
");
                    }
                    else if (property.Type.Contains("List<"))
                    {
                        rules.AppendLine($@"
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                    if (!await Is{property.Name}Valid(context.InstanceToValidate))
                    {{
                        context.AddFailure(""some entries have invalid {property.Name} required value"");
                    }}
                }})
");
                    }
                    else if (property.Type == "bool")
                        rules.AppendLine($@"
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                    if (!await Is{property.Name}Valid(context.InstanceToValidate))
                    {{
                        context.AddFailure(""some entries have invalid {property.Name} required value"");
                    }}
                }})
");
                }

                // Handle string-specific validations
                if (property.Type == "string")
                {
                    if (property.Validation.MinLength.HasValue)
                    {
                        rules.AppendLine($@"
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                    if (!await Is{property.Name}MinLengthValid(context.InstanceToValidate))
                    {{
                        context.AddFailure(""some entries have invalid {property.Name} MinLength value"");
                    }}
                }})
");
                    }

                    if (property.Validation.MaxLength.HasValue)
                    {
                        rules.AppendLine($@"
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                    if (!await Is{property.Name}MaxLengthValid(context.InstanceToValidate))
                    {{
                        context.AddFailure(""some entries have invalid {property.Name} MaxLength value"");
                    }}
                }})
");
                    }

                }
                // Handle numeric validations
                else if (property.Type == "int" || property.Type == "decimal" || property.Type == "float" || property.Type == "double")
                {
                    if (property.Validation.MinRange.HasValue)
                    {
                        rules.AppendLine($@"
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                    if (!await Is{property.Name}MinRangeValid(context.InstanceToValidate))
                    {{
                        context.AddFailure(""some entries have invalid {property.Name} MinRange value"");
                    }}
                }})
");
                    }

                    if (property.Validation.MaxRange.HasValue)
                    {
                        rules.AppendLine($@"
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                    if (!await Is{property.Name}MaxRangeValid(context.InstanceToValidate))
                    {{
                        context.AddFailure(""some entries have invalid {property.Name} MaxRange value"");
                    }}
                }})
");
                    }
                }

                if (property.Validation.Unique)
                {
                    rules.AppendLine($@"
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                    if (!await Is{property.Name}Unique(context.InstanceToValidate))
                    {{
                        context.AddFailure(""some entries have invalid {property.Name} Unique value"");
                    }}
                }})");
                }
            }
            //Add ;
            if (!string.IsNullOrWhiteSpace(rules.ToString().TrimEnd()))
            {
                rules.Append(";");
                return rules.ToString();
            }
            else
                return null;


        }
        static string? GenerateBulkPropertyMethods(List<(string Type, string Name, PropertyValidation Validation)> properties, string entityName, string entityPlural, string command)
        {
            var methods = new StringBuilder();
            foreach (var property in properties)
            {
                if (property.Validation == null)
                {
                    continue;
                }

                // Handle required validation differently based on type
                if (property.Validation.Required)
                {
                    //rules.Append(".");

                    // numeric types or DateTime don't need validation, string / Images / Lists need
                    if (property.Type == "GPG" && command == "Create")
                    {
                        methods.AppendLine($@"
        public async Task<bool> Is{property.Name}Valid({command}Bulk{entityName}Command command)
        {{
            foreach (var item in command.Bulk{entityPlural})
            {{
                if (item.{property.Name}File is null)
                    return false;
            }}
            return await Task.FromResult(true);
        }}
");
                    }
                    else if (property.Type == "GPG" && command == "Update")
                    {
                        methods.AppendLine($@"
        public async Task<bool> Is{property.Name}Valid({command}Bulk{entityName}Command command)
        {{
            foreach (var item in command.Bulk{entityPlural})
            {{
                if (item.{property.Name}File is null && item.Old{property.Name}Url == null)
                    return false;

                if (item.{property.Name}File != null && item.Old{property.Name}Url != null)
                    return false;
            }}
            return await Task.FromResult(true);
        }}
");
                    }
                    else if (property.Type == "VD" && command == "Create")
                    {
                        methods.AppendLine($@"
        public async Task<bool> Is{property.Name}Valid({command}Bulk{entityName}Command command)
        {{
            foreach (var item in command.Bulk{entityPlural})
            {{
                if (item.{property.Name}File is null)
                    return false;
            }}
            return await Task.FromResult(true);
        }}
");
                    }
                    else if (property.Type == "VD" && command == "Update")
                    {
                        methods.AppendLine($@"
        public async Task<bool> Is{property.Name}Valid({command}Bulk{entityName}Command command)
        {{
            foreach (var item in command.Bulk{entityPlural})
            {{
                if (item.{property.Name}File is null && item.Old{property.Name}Url == null)
                    return false;

                if (item.{property.Name}File != null && item.Old{property.Name}Url != null)
                    return false;
            }}
            return await Task.FromResult(true);
        }}
");
                    }
                    else if (property.Type == "string")
                    {
                        methods.AppendLine($@"
        public async Task<bool> Is{property.Name}Valid({command}Bulk{entityName}Command command)
        {{
            foreach (var item in command.Bulk{entityPlural})
            {{
                if (string.IsNullOrEmpty(item.{property.Name}))
                    return false;
            }}
            return await Task.FromResult(true);
        }}
");
                    }
                    else if (property.Type == "PNGs")
                    {
                        methods.AppendLine($@"
        public async Task<bool> Is{property.Name}Valid({command}Bulk{entityName}Command command)
        {{
            foreach (var item in command.Bulk{entityPlural})
            {{
                if (item.{property.Name} == null || !item.{property.Name}.Any())
                    return false;
            }}
            return await Task.FromResult(true);
        }}
");
                    }
                    else if (property.Type.Contains("List<"))
                    {
                        methods.AppendLine($@"
        public async Task<bool> Is{property.Name}Valid({command}Bulk{entityName}Command command)
        {{
            foreach (var item in command.Bulk{entityPlural})
            {{
                if (item.{property.Name} == null || !item.{property.Name}.Any())
                    return false;
            }}
            return await Task.FromResult(true);
        }}
");
                    }
                    else if (property.Type == "bool")
                        methods.AppendLine($@"
        public async Task<bool> Is{property.Name}Valid({command}Bulk{entityName}Command command)
        {{
            foreach (var item in command.Bulk{entityPlural})
            {{
                if (!(item.{property.Name} == true || item.{property.Name} == false))
                    return false;
            }}
            return await Task.FromResult(true);
        }}
");
                }

                // Handle string-specific validations
                if (property.Type == "string")
                {
                    if (property.Validation.MinLength.HasValue)
                    {
                        methods.AppendLine($@"
        public async Task<bool> Is{property.Name}MinLengthValid({command}Bulk{entityName}Command command)
        {{
            foreach (var item in command.Bulk{entityPlural})
            {{
                if (item.{property.Name}.Length > {property.Validation.MinLength})
                    return false;
            }}
            return await Task.FromResult(true);
        }}
");
                    }

                    if (property.Validation.MaxLength.HasValue)
                    {
                        methods.AppendLine($@"
        public async Task<bool> Is{property.Name}MaxLengthValid({command}Bulk{entityName}Command command)
        {{
            foreach (var item in command.Bulk{entityPlural})
            {{
                if (item.{property.Name}.Length < {property.Validation.MaxLength})
                    return false;
            }}
            return await Task.FromResult(true);
        }}
");
                    }

                }
                // Handle numeric validations
                else if (property.Type == "int" || property.Type == "decimal" || property.Type == "float" || property.Type == "double")
                {
                    if (property.Validation.MinRange.HasValue)
                    {
                        methods.AppendLine($@"
        public async Task<bool> Is{property.Name}MinRangeValid({command}Bulk{entityName}Command command)
        {{
            foreach (var item in command.Bulk{entityPlural})
            {{
                if (item.{property.Name} < {property.Validation.MinRange})
                    return false;
            }}
            return await Task.FromResult(true);
        }}
");
                    }

                    if (property.Validation.MaxRange.HasValue)
                    {
                        methods.AppendLine($@"
        public async Task<bool> Is{property.Name}MaxRangeValid({command}Bulk{entityName}Command command)
        {{
            foreach (var item in command.Bulk{entityPlural})
            {{
                if (item.{property.Name} > {property.Validation.MaxRange})
                    return false;
            }}
            return await Task.FromResult(true);
        }}
");
                    }
                }

                if (property.Validation.Unique)
                {
                    methods.AppendLine($@"
        public async Task<bool> Is{property.Name}Unique({command}Bulk{entityName}Command command)
        {{
            foreach (var item in command.Bulk{entityPlural})
            {{
                if(command.Bulk{entityPlural}.Count(x => x == item) > 1)
                    return false;
            }}
            return await Task.FromResult(true);
        }}
");
                }
            }

            return methods.ToString();
        }

        static string GenerateRelationRules(List<Relation> relations)
        {
            var rules = new StringBuilder();
            foreach (var relation in relations)
            {
                string ruleNotNullable = $@" 
            RuleFor(l => l.{relation.RelatedEntity}Id)
                .NotEmpty().WithMessage(""{relation.RelatedEntity}Id Must be passed"")
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                   if (!await Is{relation.RelatedEntity}Existed(context.InstanceToValidate))
                   {{
                       context.AddFailure(""{relation.RelatedEntity} is not found"");
                   }}
                }});";

                string ruleNullable = $@" 
            RuleFor(l => l.{relation.RelatedEntity}Id)
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                   if (!await Is{relation.RelatedEntity}Existed(context.InstanceToValidate))
                   {{
                       context.AddFailure(""{relation.RelatedEntity} is not found"");
                   }}
                }});";

                string ruleSelfJoin = $@" 
            RuleFor(l => l.{relation.RelatedEntity}ParentId)
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                   if (!await Is{relation.RelatedEntity}ParentExisted(context.InstanceToValidate))
                   {{
                       context.AddFailure(""{relation.RelatedEntity} Parent is not found"");
                   }}
                }});";

                string ruleManyToMany = $@" 
            RuleFor(l => l.{relation.RelatedEntity}Ids)
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                   if (!await IsMany{relation.RelatedEntity}Existed(context.InstanceToValidate))
                   {{
                       context.AddFailure("" Some {relation.RelatedEntity} are not found"");
                   }}
                }});";

                switch (relation.Type)
                {
                    case RelationType.OneToOneSelfJoin:
                        rules.AppendLine(ruleSelfJoin);
                        rules.AppendLine();
                        break;

                    case RelationType.OneToOne:
                        rules.AppendLine(ruleNotNullable);
                        rules.AppendLine();
                        break;

                    case RelationType.OneToOneNullable:
                        rules.AppendLine(ruleNullable);
                        rules.AppendLine();
                        break;

                    case RelationType.OneToMany:
                        break;

                    case RelationType.OneToManyNullable:
                        break;

                    case RelationType.ManyToOne:
                        rules.AppendLine(ruleNotNullable);
                        rules.AppendLine();
                        break;

                    case RelationType.ManyToOneNullable:
                        rules.AppendLine(ruleNullable);
                        rules.AppendLine();
                        break;
                    case RelationType.ManyToMany:
                        rules.AppendLine(ruleManyToMany);
                        rules.AppendLine();
                        break;
                    default:
                        break;
                }
            }
            return rules.ToString();
        }

        static string GenerateRelationMethods(List<Relation> relations, string commandName)
        {
            var methods = new StringBuilder();
            foreach (var relation in relations)
            {
                string x = relation.RelatedEntity;
                string relatedEntityName = char.ToLower(x[0]) + x.Substring(1);
                string relatedEntityRepoName = $"_{relatedEntityName}Repository";
                string methodNotNullable = $@" 
        public async Task<bool> Is{relation.RelatedEntity}Existed({commandName} command)
        {{
            var {relation.RelatedEntity.ToLower()} = await {relatedEntityRepoName}.GetByIdAsync(command.{relation.RelatedEntity}Id);

            if ({relation.RelatedEntity.ToLower()} is null)
            {{
                return false;
            }}
        
            return true;
        }}";

                string methodNullable = $@" 
        public async Task<bool> Is{relation.RelatedEntity}Existed({commandName} command)
        {{
            if (command.{relation.RelatedEntity}Id is null)
                return true;
            var {relation.RelatedEntity.ToLower()} = await {relatedEntityRepoName}.GetByIdAsync(command.{relation.RelatedEntity}Id.Value);

            if ({relation.RelatedEntity.ToLower()} is null)
            {{
                return false;
            }}
        
            return true;
        }}";

                string methodSelfJoin = $@" 
        public async Task<bool> Is{relation.RelatedEntity}ParentExisted({commandName} command)
        {{
            if (command.{relation.RelatedEntity}ParentId is null)
                return true;
            var {relation.RelatedEntity.ToLower()} = await {relatedEntityRepoName}.GetByIdAsync(command.{relation.RelatedEntity}ParentId.Value);

            if ({relation.RelatedEntity.ToLower()} is null)
            {{
                return false;
            }}
        
            return true;
        }}";

                string methodManyToMany = $@" 
        public async Task<bool> IsMany{relation.RelatedEntity}Existed({commandName} command)
        {{
            foreach (var id in command.{relation.RelatedEntity}Ids)
            {{
                var res = await {relatedEntityRepoName}.GetAll().AnyAsync(x => x.Id == id);
                if(!res)
                    return false;
            }}
            return true;
        }}";


                switch (relation.Type)
                {
                    case RelationType.OneToOneSelfJoin:
                        methods.AppendLine(methodSelfJoin);
                        methods.AppendLine();
                        break;

                    case RelationType.OneToOne:
                        methods.AppendLine(methodNotNullable);
                        methods.AppendLine();
                        break;

                    case RelationType.OneToOneNullable:
                        methods.AppendLine(methodNullable);
                        methods.AppendLine();
                        break;

                    case RelationType.OneToMany:
                        break;

                    case RelationType.OneToManyNullable:
                        break;

                    case RelationType.ManyToOne:
                        methods.AppendLine(methodNotNullable);
                        methods.AppendLine();
                        break;

                    case RelationType.ManyToOneNullable:
                        methods.AppendLine(methodNullable);
                        methods.AppendLine();
                        break;
                    case RelationType.ManyToMany:
                        methods.AppendLine(methodManyToMany);
                        methods.AppendLine();
                        break;
                    default:
                        break;
                }
            }
            return methods.ToString();
        }

        public static void GenerateSingleEntity(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, bool hasLocalization, List<Relation> relations, bool hasVersioning, bool hasNotification, bool hasUserAction)
        {
            string fileName = $"Single{entityName}.cs";
            string filePath = Path.Combine(path, fileName);
            var propList = new List<string>();
            StringBuilder mapperEnum = new StringBuilder();
            StringBuilder imageCode = new StringBuilder();
            foreach (var prop in properties)
            {
                if (prop.Type == "GPG")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        propList.Add($"\t\tpublic FileDto {prop.Name}File {{ get; set; }} = new FileDto();");
                        
                    }
                    else
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");
                        imageCode.Append($@"
                {entityName.ToLower()}.{prop.Name} = request.{prop.Name}File != null ? await _fileService.UploadFileAsync(request.{prop.Name}File) : null;

");
                    }
                }
                else if (prop.Type == "VD")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        propList.Add($"\t\tpublic FileDto {prop.Name}File {{ get; set; }} = new FileDto();");
                       
                    }
                    else
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");
                        
                    }
                }
                else if (prop.Type == "PNGs")
                {
                    propList.Add($"\t\tpublic List<FileDto> {prop.Name}Files {{ get; set; }} = new List<FileDto>();");
                    
                }
                else
                {
                    if (enumProps.Any(p => p.prop == prop.Name))
                    {
                        if (prop.Validation != null && prop.Validation.Required)
                        {
                            propList.Add($"\t\tpublic {entityName}{prop.Name} {prop.Name} {{ get; set; }}");
                            mapperEnum.Append($".ForMember(dest => dest.{prop.Name}, opt => opt.MapFrom(src => (int)src.{prop.Name}))");
                            mapperEnum.AppendLine();
                        }
                        else
                        {
                            propList.Add($"\t\tpublic {entityName}{prop.Name}? {prop.Name} {{ get; set; }}");
                            mapperEnum.Append($".ForMember(dest => dest.{prop.Name}, opt => opt.MapFrom(src => (int?)src.{prop.Name}))");
                            mapperEnum.AppendLine();
                        }
                    }
                    else
                        propList.Add($"\t\tpublic {prop.Type} {prop.Name} {{ get; set; }}");
                }

            }
            var props = string.Join(Environment.NewLine, propList);
            List<string> relationPropsList = new List<string>();
            foreach (var relation in relations)
            {

                string prop = null!;

                switch (relation.Type)
                {
                    case RelationType.OneToOneSelfJoin:
                        prop = $"\t\tpublic Guid? {relation.RelatedEntity}ParentId {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;

                    case RelationType.OneToOne:
                        prop = $"\t\tpublic Guid {relation.RelatedEntity}Id {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;

                    case RelationType.OneToOneNullable:
                        prop = $"\t\tpublic Guid? {relation.RelatedEntity}Id {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;

                    case RelationType.OneToMany:
                        break;

                    case RelationType.OneToManyNullable:
                        break;

                    case RelationType.ManyToOne:
                        prop = $"\t\tpublic Guid {relation.RelatedEntity}Id {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;

                    case RelationType.ManyToOneNullable:
                        prop = $"\t\tpublic Guid? {relation.RelatedEntity}Id {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;
                    case RelationType.ManyToMany:
                        prop = $"\t\tpublic List<Guid> {relation.RelatedEntity}Ids {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;
                    default:
                        break;
                }
            }

            string relationProps = string.Join(Environment.NewLine, relationPropsList);
            string? localizationList = hasLocalization ? $"\t\tpublic List<{entityName}LocalizationApp> {entityName}LocalizationApps {{ get; set; }} = new List<{entityName}LocalizationApp>();" : null;

            string content = $@"using System;
using Application.Common.Models.Assets;
using Application.Common.Models.Localization;
using Domain.Entities;
using Domain.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.{entityPlural}.Commands.CreateBulk{entityName}
{{
    public class Single{entityName}
    {{
        public Guid {entityName}Id {{ get; set; }}
        {props}
        {localizationList}
        {relationProps}
        public class Mapping : Profile
        {{
            public Mapping()
            {{
                CreateMap<Single{entityName}, {entityName}>()
                {mapperEnum}
                ;
            }}
        }}
    }}
}}
";
            File.WriteAllText(filePath, content);
        }
        public static void GenerateSingleUpdateEntity(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, bool hasLocalization, List<Relation> relations, bool hasVersioning, bool hasNotification, bool hasUserAction)
        {
            string fileName = $"SingleUpdated{entityName}.cs";
            string filePath = Path.Combine(path, fileName);
            var propList = new List<string>();
            StringBuilder mapperEnum = new StringBuilder();
            foreach (var prop in properties)
            {
                if (prop.Type == "GPG")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");
                        propList.Add($"\t\tpublic string? Old{prop.Name}Url {{ get; set; }}");
                    }
                    else
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");
                        propList.Add($"\t\tpublic bool? DeleteOld{prop.Name} {{ get; set; }}");
                    }
                }
                else if (prop.Type == "VD")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");
                        propList.Add($"\t\tpublic string? Old{prop.Name}Url {{ get; set; }}");
                    }
                    else
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");
                        propList.Add($"\t\tpublic bool? DeleteOld{prop.Name} {{ get; set; }}");
                    }
                }
                else if (prop.Type == "PNGs")
                {
                    propList.Add($"\t\tpublic List<FileDto>? {prop.Name}Files {{ get; set; }} = new List<FileDto>();");
                    propList.Add($"\t\tpublic List<string>? Deleted{prop.Name}URLs {{ get; set; }}");
                }
                else
                {
                    if (enumProps.Any(p => p.prop == prop.Name))
                    {
                        if (prop.Validation != null && prop.Validation.Required)
                        {
                            propList.Add($"\t\tpublic {entityName}{prop.Name} {prop.Name} {{ get; set; }}");
                            mapperEnum.Append($".ForMember(dest => dest.{prop.Name}, opt => opt.MapFrom(src => (int)src.{prop.Name}))");
                            mapperEnum.AppendLine();
                        }
                        else
                        {
                            propList.Add($"\t\tpublic {entityName}{prop.Name}? {prop.Name} {{ get; set; }}");
                            mapperEnum.Append($".ForMember(dest => dest.{prop.Name}, opt => opt.MapFrom(src => (int?)src.{prop.Name}))");
                            mapperEnum.AppendLine();
                        }

                    }
                    else
                        propList.Add($"\t\tpublic {prop.Type} {prop.Name} {{ get; set; }}");
                }

            }
            var props = string.Join(Environment.NewLine, propList);
            List<string> relationPropsList = new List<string>();
            foreach (var relation in relations)
            {

                string prop = null!;

                switch (relation.Type)
                {
                    case RelationType.OneToOneSelfJoin:
                        prop = $"\t\tpublic Guid? {relation.RelatedEntity}ParentId {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;

                    case RelationType.OneToOne:
                        prop = $"\t\tpublic Guid {relation.RelatedEntity}Id {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;

                    case RelationType.OneToOneNullable:
                        prop = $"\t\tpublic Guid? {relation.RelatedEntity}Id {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;

                    case RelationType.OneToMany:
                        break;

                    case RelationType.OneToManyNullable:
                        break;

                    case RelationType.ManyToOne:
                        prop = $"\t\tpublic Guid {relation.RelatedEntity}Id {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;

                    case RelationType.ManyToOneNullable:
                        prop = $"\t\tpublic Guid? {relation.RelatedEntity}Id {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;
                    case RelationType.ManyToMany:
                        prop = $"\t\tpublic List<Guid> {relation.RelatedEntity}Ids {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;
                    default:
                        break;
                }
            }

            string relationProps = string.Join(Environment.NewLine, relationPropsList);
            string? localizationList = hasLocalization ? $"\t\tpublic List<{entityName}LocalizationApp> {entityName}LocalizationApps {{ get; set; }} = new List<{entityName}LocalizationApp>();" : null;

            string content = $@"using System;
using Application.Common.Models.Assets;
using Application.Common.Models.Localization;
using Domain.Entities;
using Domain.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.{entityPlural}.Commands.UpdateBulk{entityName}
{{
    public class SingleUpdated{entityName}
    {{
        public Guid {entityName}Id {{ get; set; }}
        {props}
        {localizationList}
        {relationProps}
        public class Mapping : Profile
        {{
            public Mapping()
            {{
                CreateMap<SingleUpdated{entityName}, {entityName}>()
                {mapperEnum}
                ;
            }}
        }}
    }}
}}
";
            File.WriteAllText(filePath, content);
        }
    }
}
