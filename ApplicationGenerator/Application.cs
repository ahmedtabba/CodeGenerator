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
        public static void GenerateIRepositoryInterface(string entityName, string path, List<Relation>? relations = null, string? parentEntityName = null)
        {
            string fileName = $"I{entityName}Repository.cs";
            string filePath = Path.Combine(path, fileName);
            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";
            string? GetByParent = parentEntityName == null ? null : $"Task<{entityName}> Get{entityName}ByParent(Guid id);";
            string? GetWithInclude = null;
            if (relations != null && relations.Any())
            {
                if (!(relations.Count == 1 && relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable)))
                GetWithInclude = $@"
        Task<{entityName}> Get{entityName}(Guid id);
        IQueryable<{entityName}> Get{entityPlural}();
        {GetByParent}";

            }

            else if (parentEntityName != null)
                GetWithInclude = $@"
        {GetByParent}";

            string content = $@"using Domain.Entities;

namespace Application.Common.Interfaces.IRepositories
{{
    public interface I{entityName}Repository : IRepositoryAsync<{entityName}>
    {{
{GetWithInclude}
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
            StringBuilder fileCode = new StringBuilder();
            foreach (var prop in properties)
            {
                if (prop.Type == "GPG")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        propList.Add($"\t\tpublic FileDto {prop.Name}File {{ get; set; }} = new FileDto();");

                        string thisImageCode = $@"
                {entityName.ToLower()}.{prop.Name} =  await _fileService.UploadFileAsync(request.{prop.Name}File);";

                        imageCode.Append(thisImageCode);
                    }
                    else
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");

                        string thisImageCode = $@"
                {entityName.ToLower()}.{prop.Name} = request.{prop.Name}File != null ? await _fileService.UploadFileAsync(request.{prop.Name}File) : null;";

                        imageCode.Append(thisImageCode);
                    }
                }
                else if (prop.Type == "PNGs")
                {
                    propList.Add($"\t\tpublic List<FileDto> {prop.Name}Files {{ get; set; }} = new List<FileDto>();");

                    string thisImagesCode = $@"
                foreach (var item in request.{prop.Name}Files)
                    {{
                        var path = await _fileService.UploadFileAsync(item);
                        {entityName.ToLower()}.{prop.Name}.Add(path);
                    }}";

                    imageCode.Append(thisImagesCode);
                }
                else if (prop.Type == "VD")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        propList.Add($"\t\tpublic FileDto {prop.Name}File {{ get; set; }} = new FileDto();");

                        string thisVideoCode = $@"
                {entityName.ToLower()}.{prop.Name} =  await _fileService.UploadFileAsync(request.{prop.Name}File);";

                        videoCode.Append(thisVideoCode);
                    }
                    else
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");

                        string thisVideoCode = $@"
                {entityName.ToLower()}.{prop.Name} = request.{prop.Name}File != null ? await _fileService.UploadFileAsync(request.{prop.Name}File) : null;";

                        videoCode.Append(thisVideoCode);

                    }
                }
                else if (prop.Type == "VDs")
                {
                    propList.Add($"\t\tpublic List<FileDto> {prop.Name}Files {{ get; set; }} = new List<FileDto>();");

                    string thisVideosCode = $@"
                foreach (var item in request.{prop.Name}Files)
                    {{
                        var path = await _fileService.UploadFileAsync(item);
                        {entityName.ToLower()}.{prop.Name}.Add(path);
                    }}";

                    videoCode.Append(thisVideosCode);
                }
                else if (prop.Type == "FL")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        propList.Add($"\t\tpublic FileDto {prop.Name}File {{ get; set; }} = new FileDto();");

                        string thisFileCode = $@"
                {entityName.ToLower()}.{prop.Name} =  await _fileService.UploadFileAsync(request.{prop.Name}File);";

                        fileCode.Append(thisFileCode);
                    }
                    else
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");

                        string thisFileCode = $@"
                {entityName.ToLower()}.{prop.Name} = request.{prop.Name}File != null ? await _fileService.UploadFileAsync(request.{prop.Name}File) : null;";

                        fileCode.Append(thisFileCode);
                    }
                }
                else if (prop.Type == "FLs")
                {
                    propList.Add($"\t\tpublic List<FileDto> {prop.Name}Files {{ get; set; }} = new List<FileDto>();");

                    string thisFilesCode = $@"
                foreach (var item in request.{prop.Name}Files)
                    {{
                        var path = await _fileService.UploadFileAsync(item);
                        {entityName.ToLower()}.{prop.Name}.Add(path);
                    }}";

                    fileCode.Append(thisFilesCode);
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
                    case RelationType.UserSingle:
                        prop = $"\t\tpublic string {relation.DisplayedProperty}Id {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;
                    case RelationType.UserSingleNullable:
                        prop = $"\t\tpublic string? {relation.DisplayedProperty}Id {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;
                    case RelationType.UserMany:
                        prop = $"\t\tpublic List<string> {relation.DisplayedProperty.GetPluralName()}Ids {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;
                    default:
                        break;
                }
            }
            string relationProps = string.Join(Environment.NewLine, relationPropsList);
            string? injectCTORMany1 = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $",I{relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity}Repository {relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.GetCamelCaseName()}Repository";
            string? injectCTORMany2 = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $"_{relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.GetCamelCaseName()}Repository = {relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.GetCamelCaseName()}Repository;";
            string? injectCTORMany3 = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $"private readonly I{relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity}Repository _{relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.GetCamelCaseName()}Repository;";
            string? relatedEntityManyPlural = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.GetPluralName();
            string? relatedEntityManyRepo = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $"_{relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.GetCamelCaseName()}Repository";
            string? relationManyToManyCode = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $@"                                                                            
                var objs = await {relatedEntityManyRepo}.GetAllAsTracking()
                        .Where(x => request.{relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity}Ids.Contains(x.Id))
                        .ToListAsync();                

                foreach (var obj in objs)
                {{
                    {entityName.ToLower()}.{relatedEntityManyPlural}.Add(obj);
                }}";

            //string? injectCTORUserMany1 = !relations.Any(r => r.Type == RelationType.UserMany) ? null : $",I{entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository {entityName.GetCamelCaseName()}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository";
            //string? injectCTORUserMany2 = !relations.Any(r => r.Type == RelationType.UserMany) ? null : $"_{entityName.GetCamelCaseName()}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository = {entityName.GetCamelCaseName()}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository;";
            //string? injectCTORUserMany3 = !relations.Any(r => r.Type == RelationType.UserMany) ? null : $"private readonly I{entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository _{entityName.GetCamelCaseName()}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository;";
            string? relationUserManyCode = !relations.Any(r => r.Type == RelationType.UserMany) ? null : $@"                                                                            
                foreach (var id in request.{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty.GetPluralName()}Ids)
                    {{
                        {entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty} user = new {entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}
                        {{
                            {entityName}Id = {entityName.ToLower()}.Id,
                            UserId = id
                        }};
                        {entityName.ToLower()}.{entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty.GetPluralName()}.Add(user);
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
                {fileCode}
                {relationManyToManyCode}
                {relationUserManyCode}
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
        public static void GenerateCreateCommandValidator(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<Relation> relations)
        {
            string className = $"Create{entityName}CommandValidator";
            string commandName = $"Create{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            string? methodsUnique = string.Empty;
            string? identityUsing = relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable || r.Type == RelationType.UserMany) ? "using Application.Common.Interfaces.Identity;" : null;
            StringBuilder injectCTOR1 = new($"ILogger<{className}> logger");
            foreach (var relation in relations)
            {
                if (relation.Type != RelationType.OneToMany && relation.Type != RelationType.OneToManyNullable && relation.Type != RelationType.UserSingle && relation.Type != RelationType.UserSingleNullable && relation.Type != RelationType.UserMany)
                    injectCTOR1.Append($",I{relation.RelatedEntity}Repository {relation.RelatedEntity.GetCamelCaseName()}Repository");
            }
            StringBuilder injectCTOR2 = new StringBuilder($"\t\t\t_logger = logger;");
            foreach (var relation in relations)
            {
                if (relation.Type != RelationType.OneToMany && relation.Type != RelationType.OneToManyNullable && relation.Type != RelationType.UserSingle && relation.Type != RelationType.UserSingleNullable && relation.Type != RelationType.UserMany)
                    injectCTOR2.AppendLine($"_{relation.RelatedEntity.GetCamelCaseName()}Repository = {relation.RelatedEntity.GetCamelCaseName()}Repository;");
            }
            StringBuilder injectCTOR3 = new($"\t\tprivate readonly ILogger<{className}> _logger;");
            foreach (var relation in relations)
            {
                if (relation.Type != RelationType.OneToMany && relation.Type != RelationType.OneToManyNullable && relation.Type != RelationType.UserSingle && relation.Type != RelationType.UserSingleNullable && relation.Type != RelationType.UserMany)
                    injectCTOR3.AppendLine($"private readonly I{relation.RelatedEntity}Repository _{relation.RelatedEntity.GetCamelCaseName()}Repository;");
            }
            if (relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable || r.Type == RelationType.UserMany))
            {
                injectCTOR1.Append($",IIdentityService identityService");
                injectCTOR2.AppendLine($"\t\t\t_identityService = identityService;");
                injectCTOR3.AppendLine($"\tprivate readonly IIdentityService _identityService;");
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
            rulesStore.AppendLine(GenerateRelationRules(relations, entityName));
            string rules = string.Join(Environment.NewLine, rulesStore.ToString());

            string content = $@"
using FluentValidation;
using Application.Common.Interfaces.IRepositories;
using Microsoft.Extensions.Logging;
{identityUsing}

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
        ////
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
            string? identityUsing = relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable || r.Type == RelationType.UserMany) ? "using Application.Common.Interfaces.Identity;" : null;
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
            StringBuilder fileCode = new StringBuilder();
            List<string> oldImageUrlSection = new List<string>();
            List<string> oldVideoUrlSection = new List<string>();
            List<string> oldFileUrlSection = new List<string>();
            StringBuilder oldImageToDeleteCode = new StringBuilder();
            StringBuilder oldVideoToDeleteCode = new StringBuilder();
            StringBuilder oldImagesToDeleteCode = new StringBuilder();
            StringBuilder oldVideosToDeleteCode = new StringBuilder();
            StringBuilder oldFileToDeleteCode = new StringBuilder();
            StringBuilder oldFilesToDeleteCode = new StringBuilder();
            StringBuilder mapperEnum = new StringBuilder();
            foreach (var prop in properties)
            {
                if (prop.Type == "GPG")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");
                        propList.Add($"\t\tpublic string? {prop.Name}Url {{ get; set; }}");

                        var thisOldImageUrl = $"\t\t\t\tvar old{prop.Name}Url = existingObj.{prop.Name};";
                        oldImageUrlSection.Add(thisOldImageUrl);

                        var thisImageCode = $@"
                if (request.{prop.Name}File != null)
                    existingObj.{prop.Name} = await _fileService.UploadFileAsync(request.{prop.Name}File);
                else
                    existingObj.{prop.Name} = request.{prop.Name}Url!;";

                        imageCode.Append(thisImageCode);

                        var thisOldImageToDeleteCode = $@"
                if (request.{prop.Name}File != null)
                    await _fileService.DeleteFileAsync(old{prop.Name}Url);";

                        oldImageToDeleteCode.Append(thisOldImageToDeleteCode);
                    }
                    else
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");
                        propList.Add($"\t\tpublic bool? DeleteOld{prop.Name} {{ get; set; }}");

                        var thisOldImageUrl = $"\t\t\t\tvar old{prop.Name}Url = existingObj.{prop.Name};";
                        oldImageUrlSection.Add(thisOldImageUrl);

                        var thisImageCode = $@"
                if (request.{prop.Name}File != null)
                    existingObj.{prop.Name} = await _fileService.UploadFileAsync(request.{prop.Name}File);
                if (request.DeleteOld{prop.Name} != null && request.DeleteOld{prop.Name}.Value)
                    existingObj.{prop.Name} = null;";

                        imageCode.Append(thisImageCode);

                        var thisOldImageToDeleteCode = $@"
                if (request.{prop.Name}File != null || (request.DeleteOld{prop.Name} != null && request.DeleteOld{prop.Name}.Value))
                    if(old{prop.Name}Url != null )
                    await _fileService.DeleteFileAsync(old{prop.Name}Url);";

                        oldImageToDeleteCode.Append(thisOldImageToDeleteCode);
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
                        propList.Add($"\t\tpublic string? {prop.Name}Url {{ get; set; }}");

                        var thisOldVideoUrl = $"\t\t\t\tvar old{prop.Name}Url = existingObj.{prop.Name};";
                        oldVideoUrlSection.Add(thisOldVideoUrl);

                        var thisVideoCode = $@"
                if (request.{prop.Name}File != null)
                    existingObj.{prop.Name} = await _fileService.UploadFileAsync(request.{prop.Name}File);
                else
                    existingObj.{prop.Name} = request.{prop.Name}Url!;";

                        videoCode.Append(thisVideoCode);

                        var thisOldVideoToDeleteCode = $@"
                if (request.{prop.Name}File != null)
                    await _fileService.DeleteFileAsync(old{prop.Name}Url);";

                        oldVideoToDeleteCode.Append(thisOldVideoToDeleteCode);
                    }
                    else
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");
                        propList.Add($"\t\tpublic bool? DeleteOld{prop.Name} {{ get; set; }}");

                        var thisOldVideoUrl = $"\t\t\t\tvar old{prop.Name}Url = existingObj.{prop.Name};";
                        oldVideoUrlSection.Add(thisOldVideoUrl);

                        var thisVideoCode = $@"
                if (request.{prop.Name}File != null)
                    existingObj.{prop.Name} = await _fileService.UploadFileAsync(request.{prop.Name}File);
                if (request.DeleteOld{prop.Name} != null && request.DeleteOld{prop.Name}.Value)
                    existingObj.{prop.Name} = null;";

                        videoCode.Append(thisVideoCode);

                        var thisOldVideoToDeleteCode = $@"
                if (request.{prop.Name}File != null || (request.DeleteOld{prop.Name} != null && request.DeleteOld{prop.Name}.Value))
                    if(old{prop.Name}Url != null )
                    await _fileService.DeleteFileAsync(old{prop.Name}Url);";

                        oldVideoToDeleteCode.Append(thisOldVideoToDeleteCode);
                    }
                }
                else if (prop.Type == "VDs")
                {
                    propList.Add($"\t\tpublic List<FileDto>? {prop.Name}Files {{ get; set; }} = new List<FileDto>();");
                    propList.Add($"\t\tpublic List<string>? Deleted{prop.Name}URLs {{ get; set; }}");
                    videoCode.Append($@"
                if (request.{prop.Name}Files != null && request.{prop.Name}Files.Any())
                {{

                    //Save old urls
                    var oldVideosURLs = new List<string>();
                    foreach (var item in existingObj.{prop.Name})
                    {{
                        oldVideosURLs.Add(item);
                    }}
                    existingObj.{prop.Name}.Clear();
                    //Add new videos
                    foreach (var video in request.{prop.Name}Files)
                    {{
                        var videoUrl = await _fileService.UploadFileAsync(video);
                        // Add the new URL
                        existingObj.{prop.Name}.Add(videoUrl);
                    }}
                    //Add old videos to entity
                    if (request.Deleted{prop.Name}URLs != null)
                        foreach (var item in oldVideosURLs)
                        {{
                            if (!request.Deleted{prop.Name}URLs.Contains(item))
                                existingObj.{prop.Name}.Add(item);
                        }}
                    else
                        foreach (var item in oldVideosURLs)
                        {{
                            existingObj.{prop.Name}.Add(item);
                        }}
                }}
                else
                {{
                    if (request.Deleted{prop.Name}URLs != null && request.Deleted{prop.Name}URLs.Any())
                    {{
                        var remainingVideosURLs = new List<string>();
                        foreach (var item in existingObj.{prop.Name})
                        {{
                            if (!request.Deleted{prop.Name}URLs.Contains(item))
                            {{
                                remainingVideosURLs.Add(item);
                            }}
                        }}
                        existingObj.{prop.Name} = remainingVideosURLs;
                    }}
                }}
");
                    oldVideosToDeleteCode.Append($@"
                if(request.Deleted{prop.Name}URLs != null)
                    foreach (var path in request.Deleted{prop.Name}URLs)
                    {{
                        await _fileService.DeleteFileAsync(path);
                    }}
");
                }
                else if (prop.Type == "FL")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");
                        propList.Add($"\t\tpublic string? {prop.Name}Url {{ get; set; }}");

                        var thisOldFileUrl = $"\t\t\t\tvar old{prop.Name}Url = existingObj.{prop.Name};";
                        oldFileUrlSection.Add(thisOldFileUrl);

                        var thisFileCode = $@"
                if (request.{prop.Name}File != null)
                    existingObj.{prop.Name} = await _fileService.UploadFileAsync(request.{prop.Name}File);
                else
                    existingObj.{prop.Name} = request.{prop.Name}Url!;";

                        fileCode.Append(thisFileCode);

                        var thisOldFileToDeleteCode = $@"
                if (request.{prop.Name}File != null)
                    await _fileService.DeleteFileAsync(old{prop.Name}Url);";

                        oldFileToDeleteCode.Append(thisOldFileToDeleteCode);
                    }
                    else
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");
                        propList.Add($"\t\tpublic bool? DeleteOld{prop.Name} {{ get; set; }}");

                        var thisOldFileUrl = $"\t\t\t\tvar old{prop.Name}Url = existingObj.{prop.Name};";
                        oldFileUrlSection.Add(thisOldFileUrl);

                        var thisFileCode = $@"
                if (request.{prop.Name}File != null)
                    existingObj.{prop.Name} = await _fileService.UploadFileAsync(request.{prop.Name}File);
                if (request.DeleteOld{prop.Name} != null && request.DeleteOld{prop.Name}.Value)
                    existingObj.{prop.Name} = null;";

                        fileCode.Append(thisFileCode);

                        var thisOldFileToDeleteCode = $@"
                if (request.{prop.Name}File != null || (request.DeleteOld{prop.Name} != null && request.DeleteOld{prop.Name}.Value))
                    if(old{prop.Name}Url != null )
                    await _fileService.DeleteFileAsync(old{prop.Name}Url);";

                        oldFileToDeleteCode.Append(thisOldFileToDeleteCode);
                    }
                }
                else if (prop.Type == "FLs")
                {
                    propList.Add($"\t\tpublic List<FileDto>? {prop.Name}Files {{ get; set; }} = new List<FileDto>();");
                    propList.Add($"\t\tpublic List<string>? Deleted{prop.Name}URLs {{ get; set; }}");
                    fileCode.Append($@"
                if (request.{prop.Name}Files != null && request.{prop.Name}Files.Any())
                {{

                    //Save old urls
                    var oldFilesURLs = new List<string>();
                    foreach (var item in existingObj.{prop.Name})
                    {{
                        oldFilesURLs.Add(item);
                    }}
                    existingObj.{prop.Name}.Clear();
                    //Add new files
                    foreach (var file in request.{prop.Name}Files)
                    {{
                        var fileUrl = await _fileService.UploadFileAsync(file);
                        // Add the new URL
                        existingObj.{prop.Name}.Add(fileUrl);
                    }}
                    //Add old files to entity
                    if (request.Deleted{prop.Name}URLs != null)
                        foreach (var item in oldFilesURLs)
                        {{
                            if (!request.Deleted{prop.Name}URLs.Contains(item))
                                existingObj.{prop.Name}.Add(item);
                        }}
                    else
                        foreach (var item in oldFilesURLs)
                        {{
                            existingObj.{prop.Name}.Add(item);
                        }}
                }}
                else
                {{
                    if (request.Deleted{prop.Name}URLs != null && request.Deleted{prop.Name}URLs.Any())
                    {{
                        var remainingFilesURLs = new List<string>();
                        foreach (var item in existingObj.{prop.Name})
                        {{
                            if (!request.Deleted{prop.Name}URLs.Contains(item))
                            {{
                                remainingFilesURLs.Add(item);
                            }}
                        }}
                        existingObj.{prop.Name} = remainingFilesURLs;
                    }}
                }}
");
                    oldFilesToDeleteCode.Append($@"
                if(request.Deleted{prop.Name}URLs != null)
                    foreach (var path in request.Deleted{prop.Name}URLs)
                    {{
                        await _fileService.DeleteFileAsync(path);
                    }}
");
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
                    case RelationType.UserSingle:
                        prop = $"\t\tpublic string {relation.DisplayedProperty}Id {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;
                    case RelationType.UserSingleNullable:
                        prop = $"\t\tpublic string? {relation.DisplayedProperty}Id {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;
                    case RelationType.UserMany:
                        prop = $"\t\tpublic List<string> {relation.DisplayedProperty.GetPluralName()}Ids {{ get; set; }}";
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

            string? injectCTORUserMany1 = !relations.Any(r => r.Type == RelationType.UserMany) ? null : $",I{entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository {entityName.GetCamelCaseName()}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository";
            string? injectCTORUserMany2 = !relations.Any(r => r.Type == RelationType.UserMany) ? null : $"_{entityName.GetCamelCaseName()}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository = {entityName.GetCamelCaseName()}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository;";
            string? injectCTORUserMany3 = !relations.Any(r => r.Type == RelationType.UserMany) ? null : $"private readonly I{entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository _{entityName.GetCamelCaseName()}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository;";

            string? injectCTORIdentity1 = !relations.Any(r => r.Type == RelationType.UserMany) ? null : ",IIdentityService identityService";
            string? injectCTORIdentity2 = !relations.Any(r => r.Type == RelationType.UserMany) ? null : "_identityService = identityService;";
            string? injectCTORIdentity3 = !relations.Any(r => r.Type == RelationType.UserMany) ? null : "private readonly IIdentityService _identityService;";

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

                // Remove {relatedEntityManyPlural} that are no longer in the updated list
                foreach (var existing{relatedEntityManyName} in current{relatedEntityManyPlural})
                {{
                    if (!new{relatedEntityManyName}Ids.Contains(existing{relatedEntityManyName}.Id))
                    {{
                        existingObj.{relatedEntityManyPlural}.Remove(existing{relatedEntityManyName});
                    }}
                }}";

            string? relationUserManyCode = !relations.Any(r => r.Type == RelationType.UserMany) ? null : $@"
                //Handel {relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty.GetPluralName()}

                var currentUsers = existingObj.{entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty.GetPluralName()}.ToList();
                var newUsersIds = request.{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty.GetPluralName()}Ids;
                // Get the new users from the repository
                var newUsers = await _identityService.GetAllUsers()
                    .Where(u => newUsersIds.Contains(u.Id))
                    .ToListAsync();
                // Add new users if not already in the existing collection
                foreach (var user in newUsers)
                {{
                    if (!currentUsers.Any(u => u.UserId == user.Id))
                    {{
                        {entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty} obj = new {entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}
                        {{
                            {entityName} = existingObj,
                            UserId = user.Id
                        }};
                        await _{entityName.GetCamelCaseName()}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository.AddAsync(obj);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }}
                }}
                // Remove users that are no longer in the updated list
                foreach (var currentUser in currentUsers)
                {{
                    if (!newUsersIds.Contains(currentUser.UserId))
                    {{
                        existingObj.{entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty.GetPluralName()}.Remove(currentUser);
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
            string? oldImageUrlLine = hasVersioning ? null : string.Join(Environment.NewLine, oldImageUrlSection);
            string? oldVideoUrlLine = hasVersioning ? null : string.Join(Environment.NewLine, oldVideoUrlSection); ;
            string? oldFileUrlLine = hasVersioning ? null : string.Join(Environment.NewLine, oldFileUrlSection);
            string? oldImageToDeleteCodeLine = hasVersioning ? null : oldImageToDeleteCode.ToString();
            string? oldVideoToDeleteCodeLine = hasVersioning ? null : oldVideoToDeleteCode.ToString();
            string? oldFileToDeleteCodeLine = hasVersioning ? null : oldFileToDeleteCode.ToString();
            string? oldImagesToDeleteCodeLine = hasVersioning ? null : oldImagesToDeleteCode.ToString();
            string? oldVideosToDeleteCodeLine = hasVersioning ? null : oldVideosToDeleteCode.ToString();
            string? oldFilesToDeleteCodeLine = hasVersioning ? null : oldFilesToDeleteCode.ToString();

            bool getMethodCondition = relations.Any() && !(relations.Count == 1 && relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable));
            string GetMethod = getMethodCondition ? $"Get{entityName}" : "GetByIdAsync";
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
{identityUsing}

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
        {injectCTORUserMany3}
        {injectCTORIdentity3}

        public {className}Handler(ILogger<{className}Handler> logger,
                                            IMapper mapper,
                                            IUnitOfWorkAsync unitOfWork,
                                            IFileService fileService,
                                            I{entityName}Repository repository{localizationIRepo}{injectCTORMany1}{injectCTORUserMany1}{injectCTORIdentity1})
                                            
        {{
            _logger = logger;
            _mapper = mapper;
            {entityRepoName}Repository = repository;
            _fileService = fileService;
            _unitOfWork = unitOfWork;
            {localizationInjectIRepo}
            {injectCTORMany2}
            {injectCTORUserMany2}
            {injectCTORIdentity2}
        }}
        public async Task Handle({className} request, CancellationToken cancellationToken)
        {{
            try
            {{
                await _unitOfWork.BeginTransactionAsync();
                var existingObj = await {entityRepoName}Repository.{GetMethod}(request.{entityName}Id);
                {deepCopyCode}
                {oldImageUrlLine}
                {oldVideoUrlLine}
                {oldFileUrlLine}
                _mapper.Map(request, existingObj);
                {localizationCode}

                {imageCode}
                {videoCode}                
                {fileCode}
                {relationManyToManyCode}
                {relationUserManyCode}
                {eventCode}

                await {entityRepoName}Repository.UpdateAsync(existingObj);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _unitOfWork.CommitAsync();

                {oldImageToDeleteCodeLine}
                {oldVideoToDeleteCodeLine}      
                {oldFileToDeleteCodeLine}
                
                {oldImagesToDeleteCodeLine}
                {oldVideosToDeleteCodeLine}   
                {oldFilesToDeleteCodeLine}

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
        public static void GenerateUpdatePartialCommand(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, bool hasLocalization, List<Relation> relations, bool hasVersioning, bool hasNotification, bool hasUserAction, string? parentEntityName)
        {
            string className = $"Update{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            string x = entityName;
            string lowerEntityName = entityName.GetCamelCaseName();
            string entityRepoName = $"_{lowerEntityName}";

            var inheritVersion = hasVersioning ? "VersionRequestOfTBase," : null;
            var eventVersionCode = !hasVersioning ? null : $@"
                {lowerEntityName}Event.RollbackedToVersionId = request.RollbackedToVersionId;
                {lowerEntityName}Event.IsVersionedCommand = request.IsVersionedCommand;
";
            string? identityUsing = relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable || r.Type == RelationType.UserMany) ? "using Application.Common.Interfaces.Identity;" : null;
            var neededUsing = (hasVersioning || hasUserAction || hasNotification) ? $"using Domain.Events.{entityName}Events;using Application.Common.Models.Versioning;" : null;
            var deepCopyCode = (hasVersioning || hasUserAction || hasNotification) ? $"var old{entityName} = existingObj.DeepCopyJsonDotNet();" : null;
            var eventCodeCreate = !(hasVersioning || hasNotification || hasUserAction) ? null :
                $@"
                var {lowerEntityName}Event = new {entityName}CreatedEvent(objAdd);
                {eventVersionCode}
                objAdd.AddDomainEvent({lowerEntityName}Event);
";
            var eventCodeEdit = !(hasVersioning || hasNotification || hasUserAction) ? null :
                $@"
                var {lowerEntityName}Event = new {entityName}EditedEvent(old{entityName},existingObj);
                {eventVersionCode}
                existingObj.AddDomainEvent({lowerEntityName}Event);
";

            var propList = new List<string>();
            // asset and file code in create case
            StringBuilder imageCreateCode = new StringBuilder();
            StringBuilder videoCreateCode = new StringBuilder();
            StringBuilder fileCreateCode = new StringBuilder();
            // asset and file code in update case
            StringBuilder imageCode = new StringBuilder();
            StringBuilder videoCode = new StringBuilder();
            StringBuilder fileCode = new StringBuilder();

            List<string> oldImageUrlSection = new List<string>();
            List<string> oldVideoUrlSection = new List<string>();
            List<string> oldFileUrlSection = new List<string>();
            //string? oldImageUrl = string.Empty;
            //string? oldVideoUrl = string.Empty;
            //string? oldFileUrl = string.Empty;
            StringBuilder oldImageToDeleteCode = new StringBuilder();
            StringBuilder oldVideoToDeleteCode = new StringBuilder();
            StringBuilder oldImagesToDeleteCode = new StringBuilder();
            StringBuilder oldVideosToDeleteCode = new StringBuilder();
            StringBuilder oldFileToDeleteCode = new StringBuilder();
            StringBuilder oldFilesToDeleteCode = new StringBuilder();
            StringBuilder mapperEnum = new StringBuilder();
            foreach (var prop in properties)
            {
                if (prop.Type == "GPG")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");
                        propList.Add($"\t\tpublic string? {prop.Name}Url {{ get; set; }}");

                        var thisOldImageUrl = $"\t\t\t\tvar old{prop.Name}Url = existingObj.{prop.Name};";
                        oldImageUrlSection.Add(thisOldImageUrl);

                        var thisImageCreateCode = $"objAdd.{prop.Name} = await _fileService.UploadFileAsync(request.{prop.Name}File);";
                        imageCreateCode.AppendLine(thisImageCreateCode);

                        var thisImageCode = $@"
                if (request.{prop.Name}File != null)
                    existingObj.{prop.Name} = await _fileService.UploadFileAsync(request.{prop.Name}File);
                else
                    existingObj.{prop.Name} = request.{prop.Name}Url!;";

                        imageCode.Append(thisImageCode);

                        var thisOldImageToDeleteCode = $@"
                if (request.{prop.Name}File != null)
                    await _fileService.DeleteFileAsync(old{prop.Name}Url);";

                        oldImageToDeleteCode.Append(thisOldImageToDeleteCode);
                    }
                    else
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");
                        propList.Add($"\t\tpublic bool? DeleteOld{prop.Name} {{ get; set; }}");


                        var thisOldImageUrl = $"\t\t\t\tvar old{prop.Name}Url = existingObj.{prop.Name};";
                        oldImageUrlSection.Add(thisOldImageUrl);

                        var thisImageCreateCode = $"objAdd.{prop.Name} = request.{prop.Name}File != null ? await _fileService.UploadFileAsync(request.{prop.Name}File) : null;";
                        imageCreateCode.AppendLine(thisImageCreateCode);

                        var thisImageCode = $@"
                if (request.{prop.Name}File != null)
                    existingObj.{prop.Name} = await _fileService.UploadFileAsync(request.{prop.Name}File);
                if (request.DeleteOld{prop.Name} != null && request.DeleteOld{prop.Name}.Value)
                    existingObj.{prop.Name} = null;";

                        imageCode.Append(thisImageCode);

                        var thisOldImageToDeleteCode = $@"
                if (request.{prop.Name}File != null || (request.DeleteOld{prop.Name} != null && request.DeleteOld{prop.Name}.Value))
                    if(old{prop.Name}Url != null )
                    await _fileService.DeleteFileAsync(old{prop.Name}Url);";

                        oldImageToDeleteCode.Append(thisOldImageToDeleteCode);
                    }
                }
                else if (prop.Type == "PNGs")
                {
                    propList.Add($"\t\tpublic List<FileDto>? {prop.Name}Files {{ get; set; }} = new List<FileDto>();");
                    propList.Add($"\t\tpublic List<string>? Deleted{prop.Name}URLs {{ get; set; }}");

                    var thisImageCreateCode = $@"
                foreach (var item in request.{prop.Name}Files)
                    {{
                        var path = await _fileService.UploadFileAsync(item);
                        objAdd.{prop.Name}.Add(path);
                    }}";
                    imageCreateCode.Append(thisImageCreateCode);

                    var thisImageCode = $@"
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
                }}";
                    imageCode.Append(thisImageCode);

                    var thisOldImagesToDeleteCode = $@"
                if(request.Deleted{prop.Name}URLs != null)
                    foreach (var path in request.Deleted{prop.Name}URLs)
                    {{
                        await _fileService.DeleteFileAsync(path);
                    }}";

                    oldImagesToDeleteCode.Append(thisOldImagesToDeleteCode);
                }
                else if (prop.Type == "VD")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");
                        propList.Add($"\t\tpublic string? {prop.Name}Url {{ get; set; }}");

                        var thisOldVideoUrl = $"\t\t\t\tvar old{prop.Name}Url = existingObj.{prop.Name};";
                        oldVideoUrlSection.Add(thisOldVideoUrl);

                        var thisVideoCreateCode = $"objAdd.{prop.Name} = await _fileService.UploadFileAsync(request.{prop.Name}File);";
                        videoCreateCode.AppendLine(thisVideoCreateCode);

                        var thisVideoCode = $@"
                if (request.{prop.Name}File != null)
                    existingObj.{prop.Name} = await _fileService.UploadFileAsync(request.{prop.Name}File);
                else
                    existingObj.{prop.Name} = request.{prop.Name}Url!;";

                        videoCode.Append(thisVideoCode);

                        var thisOldVideoToDeleteCode = $@"
                if (request.{prop.Name}File != null)
                    await _fileService.DeleteFileAsync(old{prop.Name}Url);";

                        oldVideoToDeleteCode.Append(thisOldVideoToDeleteCode);
                    }
                    else
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");
                        propList.Add($"\t\tpublic bool? DeleteOld{prop.Name} {{ get; set; }}");


                        var thisOldVideoUrl = $"\t\t\t\tvar old{prop.Name}Url = existingObj.{prop.Name};";
                        oldVideoUrlSection.Add(thisOldVideoUrl);

                        var thisVideoCreateCode = $"objAdd.{prop.Name} = request.{prop.Name}File != null ? await _fileService.UploadFileAsync(request.{prop.Name}File) : null;";
                        videoCreateCode.AppendLine(thisVideoCreateCode);

                        var thisVideoCode = $@"
                if (request.{prop.Name}File != null)
                    existingObj.{prop.Name} = await _fileService.UploadFileAsync(request.{prop.Name}File);
                if (request.DeleteOld{prop.Name} != null && request.DeleteOld{prop.Name}.Value)
                    existingObj.{prop.Name} = null;";

                        videoCode.Append(thisVideoCode);

                        var thisOldVideoToDeleteCode = $@"
                if (request.{prop.Name}File != null || (request.DeleteOld{prop.Name} != null && request.DeleteOld{prop.Name}.Value))
                    if(old{prop.Name}Url != null )
                    await _fileService.DeleteFileAsync(old{prop.Name}Url);";

                        oldVideoToDeleteCode.Append(thisOldVideoToDeleteCode);
                    }
                }
                else if (prop.Type == "VDs")
                {
                    propList.Add($"\t\tpublic List<FileDto>? {prop.Name}Files {{ get; set; }} = new List<FileDto>();");
                    propList.Add($"\t\tpublic List<string>? Deleted{prop.Name}URLs {{ get; set; }}");

                    var thisVideoCreateCode = $@"
                foreach (var item in request.{prop.Name}Files)
                    {{
                        var path = await _fileService.UploadFileAsync(item);
                        objAdd.{prop.Name}.Add(path);
                    }}";
                    videoCreateCode.Append(thisVideoCreateCode);

                    var thisVideoCode = $@"
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
                }}";
                    videoCode.Append(thisVideoCode);

                    var thisOldVideosToDeleteCode = $@"
                if(request.Deleted{prop.Name}URLs != null)
                    foreach (var path in request.Deleted{prop.Name}URLs)
                    {{
                        await _fileService.DeleteFileAsync(path);
                    }}";

                    oldVideosToDeleteCode.Append(thisOldVideosToDeleteCode);
                }
                else if (prop.Type == "FL")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");
                        propList.Add($"\t\tpublic string? {prop.Name}Url {{ get; set; }}");

                        var thisOldFileUrl = $"\t\t\t\tvar old{prop.Name}Url = existingObj.{prop.Name};";
                        oldFileUrlSection.Add(thisOldFileUrl);

                        var thisFileCreateCode = $"objAdd.{prop.Name} = await _fileService.UploadFileAsync(request.{prop.Name}File);";
                        fileCreateCode.AppendLine(thisFileCreateCode);

                        var thisFileCode = $@"
                if (request.{prop.Name}File != null)
                    existingObj.{prop.Name} = await _fileService.UploadFileAsync(request.{prop.Name}File);
                else
                    existingObj.{prop.Name} = request.{prop.Name}Url!;";

                        fileCode.Append(thisFileCode);

                        var thisOldFileToDeleteCode = $@"
                if (request.{prop.Name}File != null)
                    await _fileService.DeleteFileAsync(old{prop.Name}Url);";

                        oldFileToDeleteCode.Append(thisOldFileToDeleteCode);
                    }
                    else
                    {
                        propList.Add($"\t\tpublic FileDto? {prop.Name}File {{ get; set; }}");
                        propList.Add($"\t\tpublic bool? DeleteOld{prop.Name} {{ get; set; }}");


                        var thisOldFileUrl = $"\t\t\t\tvar old{prop.Name}Url = existingObj.{prop.Name};";
                        oldFileUrlSection.Add(thisOldFileUrl);

                        var thisFileCreateCode = $"objAdd.{prop.Name} = request.{prop.Name}File != null ? await _fileService.UploadFileAsync(request.{prop.Name}File) : null;";
                        fileCreateCode.AppendLine(thisFileCreateCode);

                        var thisFileCode = $@"
                if (request.{prop.Name}File != null)
                    existingObj.{prop.Name} = await _fileService.UploadFileAsync(request.{prop.Name}File);
                if (request.DeleteOld{prop.Name} != null && request.DeleteOld{prop.Name}.Value)
                    existingObj.{prop.Name} = null;";

                        fileCode.Append(thisFileCode);

                        var thisOldFileToDeleteCode = $@"
                if (request.{prop.Name}File != null || (request.DeleteOld{prop.Name} != null && request.DeleteOld{prop.Name}.Value))
                    if(old{prop.Name}Url != null )
                    await _fileService.DeleteFileAsync(old{prop.Name}Url);";

                        oldFileToDeleteCode.Append(thisOldFileToDeleteCode);
                    }
                }
                else if (prop.Type == "FLs")
                {
                    propList.Add($"\t\tpublic List<FileDto>? {prop.Name}Files {{ get; set; }} = new List<FileDto>();");
                    propList.Add($"\t\tpublic List<string>? Deleted{prop.Name}URLs {{ get; set; }}");

                    var thisFileCreateCode = $@"
                foreach (var item in request.{prop.Name}Files)
                    {{
                        var path = await _fileService.UploadFileAsync(item);
                        objAdd.{prop.Name}.Add(path);
                    }}";
                    fileCreateCode.Append(thisFileCreateCode);

                    var thisFileCode = $@"
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
                }}";
                    fileCode.Append(thisFileCode);

                    var thisOldFilesToDeleteCode = $@"
                if(request.Deleted{prop.Name}URLs != null)
                    foreach (var path in request.Deleted{prop.Name}URLs)
                    {{
                        await _fileService.DeleteFileAsync(path);
                    }}";

                    oldFileToDeleteCode.Append(thisOldFilesToDeleteCode);
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
                    case RelationType.UserSingle:
                        prop = $"\t\tpublic string {relation.DisplayedProperty}Id {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;
                    case RelationType.UserSingleNullable:
                        prop = $"\t\tpublic string? {relation.DisplayedProperty}Id {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;
                    case RelationType.UserMany:
                        prop = $"\t\tpublic List<string> {relation.DisplayedProperty.GetPluralName()}Ids {{ get; set; }}";
                        relationPropsList.Add(prop);
                        break;
                    default:
                        break;
                }
            }

            relationPropsList.Add($"\t\tpublic Guid {parentEntityName}Id {{ get; set; }}");

            string relationProps = string.Join(Environment.NewLine, relationPropsList);

            string? injectCTORMany1 = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $",I{relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity}Repository {char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository";
            string? injectCTORMany2 = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $"_{char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository = {char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository;";
            string? injectCTORMany3 = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $"private readonly I{relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity}Repository _{char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository;";

            string? injectCTORUserMany1 = !relations.Any(r => r.Type == RelationType.UserMany) ? null : $",I{entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository {entityName.GetCamelCaseName()}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository";
            string? injectCTORUserMany2 = !relations.Any(r => r.Type == RelationType.UserMany) ? null : $"_{entityName.GetCamelCaseName()}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository = {entityName.GetCamelCaseName()}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository;";
            string? injectCTORUserMany3 = !relations.Any(r => r.Type == RelationType.UserMany) ? null : $"private readonly I{entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository _{entityName.GetCamelCaseName()}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository;";

            string? injectCTORIdentity1 = !relations.Any(r => r.Type == RelationType.UserMany) ? null : ",IIdentityService identityService";
            string? injectCTORIdentity2 = !relations.Any(r => r.Type == RelationType.UserMany) ? null : "_identityService = identityService;";
            string? injectCTORIdentity3 = !relations.Any(r => r.Type == RelationType.UserMany) ? null : "private readonly IIdentityService _identityService;";

            string? relatedEntityManyName = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity;
            string? relatedEntityManyPlural = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.EndsWith("y") ? relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[..^1] + "ies" : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity + "s";
            string? relatedEntityManyRepo = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $"_{char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository";

            string? relationManyToManyCreateCode = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $@"
                // Get the new {relatedEntityManyPlural} from the repository
                var new{relatedEntityManyPlural} = await {relatedEntityManyRepo}.GetAllAsTracking()
                        .Where(x => request.{relatedEntityManyName}Ids.Contains(x.Id))
                        .ToListAsync();

                foreach (var item in newBigOnes)
                    {{
                        objAdd.{relatedEntityManyPlural}.Add(item);

                    }}";
            string? relationManyToManyEditCode = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $@"
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

                // Remove {relatedEntityManyPlural} that are no longer in the updated list
                foreach (var existing{relatedEntityManyName} in current{relatedEntityManyPlural})
                {{
                    if (!new{relatedEntityManyName}Ids.Contains(existing{relatedEntityManyName}.Id))
                    {{
                        existingObj.{relatedEntityManyPlural}.Remove(existing{relatedEntityManyName});
                    }}
                }}";

            string? relationUserManyCreateCode = !relations.Any(r => r.Type == RelationType.UserMany) ? null : $@"
            //Handel {relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty.GetPluralName()}
            var newUsers = await _identityService.GetAllUsers()
                    .Where(u => request.{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty.GetPluralName()}Ids.Contains(u.Id))
                    .ToListAsync();
            foreach (var user in newUsers)
            {{
                {entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty} obj = new {entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}
                    {{
                        {entityName} = objAdd,
                        UserId = user.Id
                    }};
                    await _{entityName.GetCamelCaseName()}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository.AddAsync(obj);
                    //await _unitOfWork.SaveChangesAsync(cancellationToken);
            }}";
            string? relationUserManyEditCode = !relations.Any(r => r.Type == RelationType.UserMany) ? null : $@"
                //Handel {relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty.GetPluralName()}

                var currentUsers = existingObj.{entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty.GetPluralName()}.ToList();
                var newUsersIds = request.{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty.GetPluralName()}Ids;
                // Get the new users from the repository
                var newUsers = await _identityService.GetAllUsers()
                    .Where(u => newUsersIds.Contains(u.Id))
                    .ToListAsync();
                // Add new users if not already in the existing collection
                foreach (var user in newUsers)
                {{
                    if (!currentUsers.Any(u => u.UserId == user.Id))
                    {{
                        {entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty} obj = new {entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}
                        {{
                            {entityName} = existingObj,
                            UserId = user.Id
                        }};
                        await _{entityName.GetCamelCaseName()}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository.AddAsync(obj);
                        //await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }}
                }}
                // Remove users that are no longer in the updated list
                foreach (var currentUser in currentUsers)
                {{
                    if (!newUsersIds.Contains(currentUser.UserId))
                    {{
                        existingObj.{entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty.GetPluralName()}.Remove(currentUser);
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
            string? oldImageUrlLine = hasVersioning ? null : string.Join(Environment.NewLine, oldImageUrlSection);
            string? oldVideoUrlLine = hasVersioning ? null : string.Join(Environment.NewLine, oldVideoUrlSection);
            string? oldFileUrlLine = hasVersioning ? null : string.Join(Environment.NewLine, oldFileUrlSection);
            string? oldImageToDeleteCodeLine = hasVersioning ? null : oldImageToDeleteCode.ToString();
            string? oldVideoToDeleteCodeLine = hasVersioning ? null : oldVideoToDeleteCode.ToString();
            string? oldFileToDeleteCodeLine = hasVersioning ? null : oldFileToDeleteCode.ToString();
            string? oldImagesToDeleteCodeLine = hasVersioning ? null : oldImagesToDeleteCode.ToString();
            string? oldVideosToDeleteCodeLine = hasVersioning ? null : oldVideosToDeleteCode.ToString();
            string? oldFilesToDeleteCodeLine = hasVersioning ? null : oldFilesToDeleteCode.ToString();

            bool getMethodCondition = relations.Any() && !(relations.Count == 1 && relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable));
            string GetMethod = getMethodCondition ? $"Get{entityName}" : "GetByIdAsync";

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
{identityUsing}

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
        {injectCTORUserMany3}
        {injectCTORIdentity3}

        public {className}Handler(ILogger<{className}Handler> logger,
                                            IMapper mapper,
                                            IUnitOfWorkAsync unitOfWork,
                                            IFileService fileService,
                                            I{entityName}Repository repository{localizationIRepo}{injectCTORMany1}{injectCTORUserMany1}{injectCTORIdentity1})
                                            
        {{
            _logger = logger;
            _mapper = mapper;
            {entityRepoName}Repository = repository;
            _fileService = fileService;
            _unitOfWork = unitOfWork;
            {localizationInjectIRepo}
            {injectCTORMany2}
            {injectCTORUserMany2}
            {injectCTORIdentity2}
        }}
        public async Task Handle({className} request, CancellationToken cancellationToken)
        {{
            try
            {{
                await _unitOfWork.BeginTransactionAsync();
                if (request.{entityName}Id == Guid.Empty)
                {{
                    var objAdd = _mapper.Map<{entityName}>(request);
                    {imageCreateCode}
                    {videoCreateCode}
                    {fileCreateCode}

                    {relationManyToManyCreateCode}
                    {relationUserManyCreateCode}
                    await {entityRepoName}Repository.AddAsync(objAdd);

                    {eventCodeCreate}
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    await _unitOfWork.CommitAsync();
                }}
                else
                {{
                    var existingObj = await {entityRepoName}Repository.{GetMethod}(request.{entityName}Id);
                    var old{entityName} = existingObj.DeepCopyJsonDotNet();
                    {oldImageUrlLine}
                    {oldVideoUrlLine}
                    {oldFileUrlLine}
                    _mapper.Map(request, existingObj);
                    {imageCode}
                    {videoCode}                
                    {fileCode}
                    {relationManyToManyEditCode}
                    {relationUserManyEditCode}
                    {eventCodeEdit}
                    await {entityRepoName}Repository.UpdateAsync(existingObj);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    await _unitOfWork.CommitAsync();
                    {oldImageToDeleteCodeLine}
                    {oldVideoToDeleteCodeLine}      
                    {oldFileToDeleteCodeLine}
                
                    {oldImagesToDeleteCodeLine}
                    {oldVideosToDeleteCodeLine}   
                    {oldFilesToDeleteCodeLine}

                }}
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

            //Add delete event to parent delete command
            if (hasVersioning)
            {
                var parentCommandDeletePath = Path.Combine(path, "..", "..", "..", $"{parentEntityName.GetPluralName()}", "Commands", $"Delete{parentEntityName}", $"Delete{parentEntityName}Command.cs");
                if (!File.Exists(parentCommandDeletePath))
                {
                    //Console.WriteLine("⚠️ IApplicationDbContext.cs not found.");
                    return;
                }
                string childEvent = $@"
                var {entityName.GetCamelCaseName()} = await _{entityName.GetCamelCaseName()}Repository.Get{entityName}ByParent({parentEntityName.GetCamelCaseName()}.Id);
                var {entityName.GetCamelCaseName()}Event = new {entityName}DeletedEvent({entityName.GetCamelCaseName()});
                {parentEntityName.GetCamelCaseName()}.AddDomainEvent({entityName.GetCamelCaseName()}Event);"
                    + "\n\t\t\t\t//Add Child Deleted Events";

                var lines = File.ReadAllLines(parentCommandDeletePath).ToList();
                var index = lines.FindIndex(line => line.Contains("//Add Child Deleted Events"));

                if (index >= 0)
                {
                    lines[index] = childEvent;
                    File.WriteAllLines(parentCommandDeletePath, lines);
                }
                lines.Clear();
                index = -1;
                string childInject1 = $",I{entityName}Repository {entityName.GetCamelCaseName()}Repository" +
                    "\n\t\t\t\t\t\t//Inject1 Here";

                lines = File.ReadAllLines(parentCommandDeletePath).ToList();
                index = lines.FindIndex(line => line.Contains("//Inject1 Here"));

                if (index >= 0)
                {
                    lines[index] = childInject1;
                    File.WriteAllLines(parentCommandDeletePath, lines);
                }

                lines.Clear();
                index = -1;
                string childInject2 = $"_{entityName.GetCamelCaseName()}Repository = {entityName.GetCamelCaseName()}Repository;" +
                    "\n\t\t\t//Inject2 Here";

                lines = File.ReadAllLines(parentCommandDeletePath).ToList();
                index = lines.FindIndex(line => line.Contains("//Inject2 Here"));

                if (index >= 0)
                {
                    lines[index] = childInject2;
                    File.WriteAllLines(parentCommandDeletePath, lines);
                }

                lines.Clear();
                index = -1;
                string childInject3 = $"private readonly I{entityName}Repository _{entityName.GetCamelCaseName()}Repository;" +
                    "\n\t\t//Inject3 Here";

                lines = File.ReadAllLines(parentCommandDeletePath).ToList();
                index = lines.FindIndex(line => line.Contains("//Inject3 Here"));

                if (index >= 0)
                {
                    lines[index] = childInject3;
                    File.WriteAllLines(parentCommandDeletePath, lines);
                }

                lines.Clear();
                index = -1;
                string childUsing = $"using Domain.Events.{entityName}Events;" +
                    "\n\t\t//Add child event using Here";

                lines = File.ReadAllLines(parentCommandDeletePath).ToList();
                index = lines.FindIndex(line => line.Contains("//Add child event using Here"));

                if (index >= 0)
                {
                    lines[index] = childUsing;
                    File.WriteAllLines(parentCommandDeletePath, lines);
                }
            }
        }
        public static void GenerateUpdateBulkCommand(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, bool hasLocalization, List<Relation> relations, bool hasVersioning, bool hasNotification, bool hasUserAction, string? parentEntityName)
        {
            string className = $"UpdateBulk{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            string lowerEntityPlural = char.ToLower(entityPlural[0]) + entityPlural.Substring(1);
            string entityRepoName = $"_{lowerEntityName}";

            var inheritVersion = hasVersioning ? "VersionRequestBase," : null;

            string aggregator = parentEntityName;
            string aggregatorField = $"public Guid {aggregator}Id {{ get; set; }}";

            var eventVersionCode = !hasVersioning ? null
             : $@"{lowerEntityName}Event.RollbackedToVersionId = request.RollbackedToVersionId;
                {lowerEntityName}Event.IsVersionedCommand = request.IsVersionedCommand;
";

            var neededUsing = (hasVersioning || hasNotification || hasUserAction) ? $"using Domain.Events.{entityName}Events;using Application.Common.Models.Versioning;" : null;

            var eventCodeCreate = !(hasVersioning || hasNotification || hasUserAction) ? null :
                $@"
                var {lowerEntityName}Event = new {entityName}CreatedBulkEvent(lstObjs, request.{aggregator}Id);
                {eventVersionCode}
                lstObjs.First().AddDomainEvent({lowerEntityName}Event);";

            var eventDeleteAndDeleteLineCode = !(hasVersioning || hasNotification || hasUserAction) ? $"await {entityRepoName}Repository.DeleteBulk(existingObjects);" :
                $@"
                var {lowerEntityName}Event = new {entityName}DeletedBulkEvent(existingObjects, request.{aggregator}Id);
                {eventVersionCode}
                await {entityRepoName}Repository.DeleteBulk(existingObjects);
                existingObjects.First().AddDomainEvent({lowerEntityName}Event);";

            var eventCodeEdit = !(hasVersioning || hasNotification || hasUserAction) ? null :
                $@"
                var {lowerEntityName}Event = new {entityName}EditedBulkEvent(old{entityPlural},newObjects, request.{aggregator}Id);
                {eventVersionCode}
                existingObjects.First().AddDomainEvent({lowerEntityName}Event);";


            //delete case
            List<string> DeleteLines = new List<string>();

            string? garbageDeclaration = hasVersioning ? null : !properties.Any(p => p.Type == "GPG" || p.Type == "PNGs" || p.Type == "VD" || p.Type == "VDs" || p.Type == "FL" || p.Type == "FLs") ? null : "List<string> garbage = new List<string>();";
            // delete (update case)
            List<string> imageDeleteLines = new List<string>();
            List<string> videoDeleteLines = new List<string>();
            List<string> fileDeleteLines = new List<string>();
            // update case
            string? imageDeleteUpdateLine = null;//temp var used in UpdateCode
            string? videoDeleteUpdateLine = null;//temp var used in UpdateCode
            string? fileDeleteUpdateLine = null;//temp var used in UpdateCode

            StringBuilder imageUpdateCode = new StringBuilder();
            StringBuilder imageListUpdateCode = new StringBuilder();
            StringBuilder videoUpdateCode = new StringBuilder();
            StringBuilder videoListUpdateCode = new StringBuilder();
            StringBuilder fileUpdateCode = new StringBuilder();
            StringBuilder fileListUpdateCode = new StringBuilder();


            string? deleteOldImageCode = hasVersioning ? null : !properties.Any(p => p.Type == "GPG" || p.Type == "PNGs" || p.Type == "VD" || p.Type == "VDs" || p.Type == "FL" || p.Type == "FLs") ? null : $@"
                foreach (var item in garbage)
                {{
                    if(item != null)
                        await _fileService.DeleteFileAsync(item);
                }}
";
            StringBuilder imageCode = new StringBuilder();//first create
            StringBuilder videoCode = new StringBuilder();//first create
            StringBuilder fileCode = new StringBuilder();//first create
            StringBuilder imageUpdateAddCode = new StringBuilder();//add new to existing ones
            StringBuilder videoUpdateAddCode = new StringBuilder();//add new to existing ones
            StringBuilder fileUpdateAddCode = new StringBuilder();//add new to existing ones

            foreach (var prop in properties)
            {
                if (prop.Type == "GPG")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        var thisImageCode = $"lstObjs[i].{prop.Name} = await _fileService.UploadFileAsync(request.Bulk{entityPlural}[i].{prop.Name}File);";
                        imageCode.AppendLine(thisImageCode);

                        DeleteLines.Add($"garbage.Add(item.{prop.Name});");

                        var thisImageUpdateAddCode = $"objToAdd.{prop.Name} = await _fileService.UploadFileAsync({lowerEntityName}.{prop.Name}File);";
                        imageUpdateAddCode.AppendLine(thisImageUpdateAddCode);

                        imageDeleteLines.Add($"garbage.Add({lowerEntityName}.{prop.Name});");


                        imageDeleteUpdateLine = hasVersioning ? null : $"garbage.Add({lowerEntityName}ToUpdate.{prop.Name});";
                        var thisImageUpdateCode = $@"
                        if ({lowerEntityName}.{prop.Name}File != null)
                        {{
                            {imageDeleteUpdateLine}
                            {lowerEntityName}ToUpdate.{prop.Name} = await _fileService.UploadFileAsync({lowerEntityName}.{prop.Name}File);
                        }}
                        else
                            {lowerEntityName}ToUpdate.{prop.Name} = {lowerEntityName}.Old{prop.Name}Url!;";
                        imageUpdateCode.AppendLine(thisImageUpdateCode);
                    }
                    else
                    {
                        var thisImageCode = $"lstObjs[i].{prop.Name} = request.Bulk{entityPlural}[i].{prop.Name}File != null ? await _fileService.UploadFileAsync(request.Bulk{entityPlural}[i].{prop.Name}File) : null;";
                        imageCode.AppendLine(thisImageCode);

                        DeleteLines.Add($"garbage.Add(item.{prop.Name});");

                        var thisImageUpdateAddCode = $"objToAdd.{prop.Name} = {lowerEntityName}.{prop.Name}File != null ? await _fileService.UploadFileAsync({lowerEntityName}.{prop.Name}File) : null;";
                        imageUpdateAddCode.AppendLine(thisImageUpdateAddCode);

                        imageDeleteLines.Add($"garbage.Add({lowerEntityName}.{prop.Name});");

                        imageDeleteUpdateLine = hasVersioning ? null : $"garbage.Add({lowerEntityName}ToUpdate.{prop.Name});";
                        var thisImageUpdateCode = $@"
                        if ({lowerEntityName}.{prop.Name}File != null)
                        {{
                            {imageDeleteUpdateLine}
                            {lowerEntityName}ToUpdate.{prop.Name} = await _fileService.UploadFileAsync({lowerEntityName}.{prop.Name}File);
                        }}
                        if ({lowerEntityName}.DeleteOld{prop.Name} != null)
                        {{
                            {imageDeleteUpdateLine}
                            {lowerEntityName}ToUpdate.{prop.Name} = null;
                        }}";
                        imageUpdateCode.AppendLine(thisImageUpdateCode);
                    }
                }
                else if (prop.Type == "VD")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        var thisVideoCode = $"lstObjs[i].{prop.Name} = await _fileService.UploadFileAsync(request.Bulk{entityPlural}[i].{prop.Name}File);";
                        videoCode.AppendLine(thisVideoCode);

                        DeleteLines.Add($"garbage.Add(item.{prop.Name});");

                        var thisVideoUpdateAddCode = $"objToAdd.{prop.Name} = await _fileService.UploadFileAsync({lowerEntityName}.{prop.Name}File);";
                        videoUpdateAddCode.AppendLine(thisVideoUpdateAddCode);

                        videoDeleteLines.Add($"garbage.Add({lowerEntityName}.{prop.Name});");


                        videoDeleteUpdateLine = hasVersioning ? null : $"garbage.Add({lowerEntityName}ToUpdate.{prop.Name});";
                        var thisVideoUpdateCode = $@"
                        if ({lowerEntityName}.{prop.Name}File != null)
                        {{
                            {videoDeleteUpdateLine}
                            {lowerEntityName}ToUpdate.{prop.Name} = await _fileService.UploadFileAsync({lowerEntityName}.{prop.Name}File);
                        }}
                        else
                            {lowerEntityName}ToUpdate.{prop.Name} = {lowerEntityName}.Old{prop.Name}Url!;";
                        videoUpdateCode.AppendLine(thisVideoUpdateCode);
                    }
                    else
                    {
                        var thisVideoCode = $"lstObjs[i].{prop.Name} = request.Bulk{entityPlural}[i].{prop.Name}File != null ? await _fileService.UploadFileAsync(request.Bulk{entityPlural}[i].{prop.Name}File) : null;";
                        videoCode.AppendLine(thisVideoCode);

                        DeleteLines.Add($"garbage.Add(item.{prop.Name});");

                        var thisVideoUpdateAddCode = $"objToAdd.{prop.Name} = {lowerEntityName}.{prop.Name}File != null ? await _fileService.UploadFileAsync({lowerEntityName}.{prop.Name}File) : null;";
                        videoUpdateAddCode.AppendLine(thisVideoUpdateAddCode);

                        videoDeleteLines.Add($"garbage.Add({lowerEntityName}.{prop.Name});");

                        videoDeleteUpdateLine = hasVersioning ? null : $"garbage.Add({lowerEntityName}ToUpdate.{prop.Name});";
                        var thisVideoUpdateCode = $@"
                        if ({lowerEntityName}.{prop.Name}File != null)
                        {{
                            {videoDeleteUpdateLine}
                            {lowerEntityName}ToUpdate.{prop.Name} = await _fileService.UploadFileAsync({lowerEntityName}.{prop.Name}File);
                        }}
                        if ({lowerEntityName}.DeleteOld{prop.Name} != null)
                        {{
                            {videoDeleteUpdateLine}
                            {lowerEntityName}ToUpdate.{prop.Name} = null;
                        }}";
                        videoUpdateCode.AppendLine(thisVideoUpdateCode);
                    }
                }
                else if (prop.Type == "FL")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        var thisFileCode = $"lstObjs[i].{prop.Name} = await _fileService.UploadFileAsync(request.Bulk{entityPlural}[i].{prop.Name}File);";
                        fileCode.AppendLine(thisFileCode);

                        DeleteLines.Add($"garbage.Add(item.{prop.Name});");

                        var thisFileUpdateAddCode = $"objToAdd.{prop.Name} = await _fileService.UploadFileAsync({lowerEntityName}.{prop.Name}File);";
                        fileUpdateAddCode.AppendLine(thisFileUpdateAddCode);

                        fileDeleteLines.Add($"garbage.Add({lowerEntityName}.{prop.Name});");


                        fileDeleteUpdateLine = hasVersioning ? null : $"garbage.Add({lowerEntityName}ToUpdate.{prop.Name});";
                        var thisFileUpdateCode = $@"
                        if ({lowerEntityName}.{prop.Name}File != null)
                        {{
                            {fileDeleteUpdateLine}
                            {lowerEntityName}ToUpdate.{prop.Name} = await _fileService.UploadFileAsync({lowerEntityName}.{prop.Name}File);
                        }}
                        else
                            {lowerEntityName}ToUpdate.{prop.Name} = {lowerEntityName}.Old{prop.Name}Url!;";
                        fileUpdateCode.AppendLine(thisFileUpdateCode);
                    }
                    else
                    {
                        var thisFileCode = $"lstObjs[i].{prop.Name} = request.Bulk{entityPlural}[i].{prop.Name}File != null ? await _fileService.UploadFileAsync(request.Bulk{entityPlural}[i].{prop.Name}File) : null;";
                        fileCode.AppendLine(thisFileCode);

                        DeleteLines.Add($"garbage.Add(item.{prop.Name});");

                        var thisFileUpdateAddCode = $"objToAdd.{prop.Name} = {lowerEntityName}.{prop.Name}File != null ? await _fileService.UploadFileAsync({lowerEntityName}.{prop.Name}File) : null;";
                        fileUpdateAddCode.AppendLine(thisFileUpdateAddCode);

                        fileDeleteLines.Add($"garbage.Add({lowerEntityName}.{prop.Name});");

                        fileDeleteUpdateLine = hasVersioning ? null : $"garbage.Add({lowerEntityName}ToUpdate.{prop.Name});";
                        var thisFileUpdateCode = $@"
                        if ({lowerEntityName}.{prop.Name}File != null)
                        {{
                            {fileDeleteUpdateLine}
                            {lowerEntityName}ToUpdate.{prop.Name} = await _fileService.UploadFileAsync({lowerEntityName}.{prop.Name}File);
                        }}
                        if ({lowerEntityName}.DeleteOld{prop.Name} != null)
                        {{
                            {fileDeleteUpdateLine}
                            {lowerEntityName}ToUpdate.{prop.Name} = null;
                        }}";
                        fileUpdateCode.AppendLine(thisFileUpdateCode);
                    }
                }
                else if (prop.Type == "PNGs")
                {
                    var thisImageCode = $@"
                        foreach (var item in request.Bulk{entityPlural}[i].{prop.Name}Files)
                        {{
                            var path = await _fileService.UploadFileAsync(item);
                            lstObjs[i].{prop.Name}.Add(path);
                        }}";
                    imageCode.AppendLine(thisImageCode);

                    DeleteLines.Add($"garbage.AddRange(item.{prop.Name});");

                    var thisImageUpdateAddCode = $@"
                        foreach (var item in {lowerEntityName}.{prop.Name}Files)
                        {{
                            var path = await _fileService.UploadFileAsync(item);
                            objToAdd.{prop.Name}.Add(path);
                        }}";
                    imageUpdateAddCode.AppendLine(thisImageUpdateAddCode);

                    imageDeleteLines.Add($"garbage.AddRange({lowerEntityName}.{prop.Name});");

                    string? imagesDeleteRange = hasVersioning ? null : $@"
                        if ({lowerEntityName}.Deleted{prop.Name}URLs != null)
                            garbage.AddRange({lowerEntityName}.Deleted{prop.Name}URLs);
";
                    var thisImageListUpdateCode = $@"
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
                        {imagesDeleteRange}";

                    imageListUpdateCode.AppendLine(thisImageListUpdateCode);
                }
                else if (prop.Type == "VDs")
                {
                    var thisVideoCode = $@"
                        foreach (var item in request.Bulk{entityPlural}[i].{prop.Name}Files)
                        {{
                            var path = await _fileService.UploadFileAsync(item);
                            lstObjs[i].{prop.Name}.Add(path);
                        }}";
                    videoCode.AppendLine(thisVideoCode);

                    DeleteLines.Add($"garbage.AddRange(item.{prop.Name});");

                    var thisVideoUpdateAddCode = $@"
                        foreach (var item in {lowerEntityName}.{prop.Name}Files)
                        {{
                            var path = await _fileService.UploadFileAsync(item);
                            objToAdd.{prop.Name}.Add(path);
                        }}";
                    videoUpdateAddCode.AppendLine(thisVideoUpdateAddCode);

                    videoDeleteLines.Add($"garbage.AddRange({lowerEntityName}.{prop.Name});");

                    string? videosDeleteRange = hasVersioning ? null : $@"
                        if ({lowerEntityName}.Deleted{prop.Name}URLs != null)
                            garbage.AddRange({lowerEntityName}.Deleted{prop.Name}URLs);
";
                    var thisVideoListUpdateCode = $@"
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
                        {videosDeleteRange}
";

                    videoListUpdateCode.AppendLine(thisVideoListUpdateCode);
                }
                else if (prop.Type == "FLs")
                {
                    var thisFileCode = $@"
                        foreach (var item in request.Bulk{entityPlural}[i].{prop.Name}Files)
                        {{
                            var path = await _fileService.UploadFileAsync(item);
                            lstObjs[i].{prop.Name}.Add(path);
                        }}";
                    fileCode.AppendLine(thisFileCode);

                    DeleteLines.Add($"garbage.AddRange(item.{prop.Name});");

                    var thisFileUpdateAddCode = $@"
                        foreach (var item in {lowerEntityName}.{prop.Name}Files)
                        {{
                            var path = await _fileService.UploadFileAsync(item);
                            objToAdd.{prop.Name}.Add(path);
                        }}";
                    fileUpdateAddCode.AppendLine(thisFileUpdateAddCode);

                    fileDeleteLines.Add($"garbage.AddRange({lowerEntityName}.{prop.Name});");

                    string? filesDeleteRange = hasVersioning ? null : $@"
                        if ({lowerEntityName}.Deleted{prop.Name}URLs != null)
                            garbage.AddRange({lowerEntityName}.Deleted{prop.Name}URLs);
";
                    var thisFileListUpdateCode = $@"
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
                        {filesDeleteRange}";

                    fileListUpdateCode.AppendLine(thisFileListUpdateCode);

                }


            }
            string? deleteCaseGarbage = hasVersioning ? null : $@"
                    foreach (var item in existingObjects)
                    {{
                        {string.Join(Environment.NewLine, DeleteLines)}
                    }}
";
            string? deleteUpdatedCaseGarbage = hasVersioning ? null : $@"
{string.Join(Environment.NewLine, imageDeleteLines)}
{string.Join(Environment.NewLine, videoDeleteLines)}
{string.Join(Environment.NewLine, fileDeleteLines)}
";
            string? injectCTORMany1 = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $",I{relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity}Repository {char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository";
            string? injectCTORMany2 = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $"_{char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository = {char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository;";
            string? injectCTORMany3 = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $"private readonly I{relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity}Repository _{char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository;";
            string? relatedEntityManyPlural = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.EndsWith("y") ? relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[..^1] + "ies" : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity + "s";
            string? relatedEntityManyName = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity;
            string? relatedEntityManyRepo = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $"_{char.ToLower(relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity[0]) + relations.First(r => r.Type == RelationType.ManyToMany).RelatedEntity.Substring(1)}Repository";

            string? injectCTORUserMany1 = !relations.Any(r => r.Type == RelationType.UserMany) ? null : $",I{entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository {entityName.GetCamelCaseName()}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository";
            string? injectCTORUserMany2 = !relations.Any(r => r.Type == RelationType.UserMany) ? null : $"_{entityName.GetCamelCaseName()}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository = {entityName.GetCamelCaseName()}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository;";
            string? injectCTORUserMany3 = !relations.Any(r => r.Type == RelationType.UserMany) ? null : $"private readonly I{entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository _{entityName.GetCamelCaseName()}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository;";

            string? injectCTORIdentity1 = !relations.Any(r => r.Type == RelationType.UserMany) ? null : ",IIdentityService identityService";
            string? injectCTORIdentity2 = !relations.Any(r => r.Type == RelationType.UserMany) ? null : "_identityService = identityService;";
            string? injectCTORIdentity3 = !relations.Any(r => r.Type == RelationType.UserMany) ? null : "private readonly IIdentityService _identityService;";


            string? relationManyToManyCreateCode = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $@"
                        var objs = await {relatedEntityManyRepo}.GetAllAsTracking()
                        .Where(x => request.Bulk{entityPlural}[i].{relatedEntityManyName}Ids.Contains(x.Id))
                        .ToListAsync();                

                        foreach (var obj in objs)
                        {{
                            lstObjs[i].{relatedEntityManyPlural}.Add(obj);
                        }}";

            string? relationManyToManyAddCode = !relations.Any(r => r.Type == RelationType.ManyToMany) ? null : $@"
                        var newObjIds = {lowerEntityName}.{relatedEntityManyName}Ids;

                        // Get the many objects from the repository
                        var newObjs = await {relatedEntityManyRepo}.GetAllAsTracking()
                            .Where(x => newObjIds.Contains(x.Id))
                            .ToListAsync();

                        // Add new ones if not already in the existing collection
                        foreach (var newObj in newObjs)
                        {{
                            objToAdd.{relatedEntityManyPlural}.Add(newObj);
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

            string? relationUserManyCreateCode = !relations.Any(r => r.Type == RelationType.UserMany) ? null : $@"
            //Handel {relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty.GetPluralName()}
            
            var newUsers = await _identityService.GetAllUsers()
                    .Where(u => request.Bulk{entityPlural}[i].{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty.GetPluralName()}Ids.Contains(u.Id))
                    .ToListAsync();
            foreach (var user in newUsers)
            {{
                var obj = new {entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}
                    {{
                        {entityName} = lstObjs[i],
                        UserId = user.Id
                    }};
                    await _{entityName.GetCamelCaseName()}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository.AddAsync(obj);
                    //await _unitOfWork.SaveChangesAsync(cancellationToken);
            }}";
            string? relationUserManyAddCode = !relations.Any(r => r.Type == RelationType.UserMany) ? null : $@"
            //Handel {relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty.GetPluralName()}
            
            var newUsers = await _identityService.GetAllUsers()
                    .Where(u => {lowerEntityName}.{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty.GetPluralName()}Ids.Contains(u.Id))
                    .ToListAsync();
            foreach (var user in newUsers)
            {{
                var obj = new {entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}
                    {{
                        {entityName} = objToAdd,
                        UserId = user.Id
                    }};
                    await _{entityName.GetCamelCaseName()}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository.AddAsync(obj);
                    //await _unitOfWork.SaveChangesAsync(cancellationToken);
            }}";

            string? relationUserManyUpdateCode = !relations.Any(r => r.Type == RelationType.UserMany) ? null : $@"
                //Handel {relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty.GetPluralName()}
                var currentUsers = {lowerEntityName}ToUpdate.{entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty.GetPluralName()}.ToList();
                var newUsersIds = {lowerEntityName}.{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty.GetPluralName()}Ids;
                // Get the new users from the repository
                var newUsers = await _identityService.GetAllUsers()
                    .Where(u => newUsersIds.Contains(u.Id))
                    .ToListAsync();
                // Add new users if not already in the existing collection
                foreach (var user in newUsers)
                {{
                    if (!currentUsers.Any(u => u.UserId == user.Id))
                    {{
                        {entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty} obj = new {entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}
                        {{
                            {entityName} = {lowerEntityName}ToUpdate,
                            UserId = user.Id
                        }};
                        await _{entityName.GetCamelCaseName()}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty}Repository.AddAsync(obj);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }}
                }}
                // Remove users that are no longer in the updated list
                foreach (var currentUser in currentUsers)
                {{
                    if (!newUsersIds.Contains(currentUser.UserId))
                    {{
                        {lowerEntityName}ToUpdate.{entityName}{relations.First(r => r.Type == RelationType.UserMany).DisplayedProperty.GetPluralName()}.Remove(currentUser);
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

            bool getMethodCondition = relations.Any() && !(relations.Count == 1 && relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable));
            string GetMethod = getMethodCondition ? $"Get{entityName.GetPluralName()}" : "GetAllAsTracking";


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
using Application.Common.Interfaces.Identity;

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
        {injectCTORUserMany3}
        {injectCTORIdentity3}

        public {className}Handler(ILogger<{className}Handler> logger,
                                        IMapper mapper,
                                        IUnitOfWorkAsync unitOfWork,
                                        IFileService fileService,
                                        I{entityName}Repository repository{localizationIRepo}{injectCTORMany1}{injectCTORUserMany1}{injectCTORIdentity1})
        {{
            {entityRepoName}Repository = repository;
            _mapper=mapper;
            _unitOfWork=unitOfWork;
            _logger=logger;
            _fileService = fileService;
            {localizationInjectIRepo}
            {injectCTORMany2}
            {injectCTORUserMany2}
            {injectCTORIdentity2}
        }}
    
        public async Task Handle({className} request, CancellationToken cancellationToken)
        {{
          try
            {{
                await _unitOfWork.BeginTransactionAsync();
                var existingObjects = await _{lowerEntityName}Repository.{GetMethod}()
                    .Where(x => x.{aggregator}Id == request.{aggregator}Id)
                    .ToListAsync();

                var old{entityPlural} = existingObjects.DeepCopyJsonDotNet();
                {garbageDeclaration}

                {localizationDeleteCode}
                if (!existingObjects.Any())// create case
                {{
                    var lstObjs = _mapper.Map<List<{entityName}>>(request.Bulk{entityPlural});
                    for (int i = 0; i < lstObjs.Count; i++)
                    {{
                        {imageCode}
                        {videoCode}
                        {fileCode}
                        {relationManyToManyCreateCode}
                        {relationUserManyCreateCode}
                    }}
                    await {entityRepoName}Repository.AddBulk(lstObjs);
                    {eventCodeCreate}
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _unitOfWork.CommitAsync();
                }}
                else if (!request.Bulk{entityPlural}.Any())// delete case
                {{
                    {deleteCaseGarbage}
                    {eventDeleteAndDeleteLineCode}
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _unitOfWork.CommitAsync();
                }}
                else //update case
                {{
                //Add ones that dose not existing
                foreach (var {lowerEntityName} in request.Bulk{entityPlural})
                {{
                    if (!existingObjects.Any(x => x.Id == {lowerEntityName}.{entityName}Id))
                    {{
                        var objToAdd = _mapper.Map<{entityName}>({lowerEntityName});
                        {imageUpdateAddCode}
                        {videoUpdateAddCode}
                        {fileUpdateAddCode}
                        {relationManyToManyAddCode}
                        {relationUserManyAddCode}
                        await _{lowerEntityName}Repository.AddAsync(objToAdd);
                        {localizationAddCode}
                        //await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }}
                }}

                //Delete ones that are no longer existed
                foreach (var {lowerEntityName} in existingObjects)
                {{
                    if (!request.Bulk{entityPlural}.Any(x => x.{entityName}Id == {lowerEntityName}.Id))
                    {{
                        {deleteUpdatedCaseGarbage}
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
                        {fileUpdateCode}
                        {imageListUpdateCode}                       
                        {videoListUpdateCode}
                        {fileListUpdateCode}

                        {relationManyToManyUpdateCode}
                        {relationUserManyUpdateCode}
                        await _{lowerEntityName}Repository.UpdateAsync({lowerEntityName}ToUpdate);
                        {localizationUpdateCode}
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }}
                }}
                var newObjects = await _{lowerEntityName}Repository.GetAll()
                    .Where(x => x.{aggregator}Id == request.{aggregator}Id)
                    .ToListAsync();

                {eventCodeEdit}
    
                await _unitOfWork.CommitAsync();
                
                }}
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

            //Add delete event to parent delete command
            if (hasVersioning)
            {
                var parentCommandDeletePath = Path.Combine(path, "..", "..", "..", $"{parentEntityName.GetPluralName()}", "Commands", $"Delete{parentEntityName}", $"Delete{parentEntityName}Command.cs");
                if (!File.Exists(parentCommandDeletePath))
                {
                    //Console.WriteLine("⚠️ IApplicationDbContext.cs not found.");
                    return;
                }
                string childEvent = $@"
                var {entityName.GetCamelCaseName().GetPluralName()} = await _{entityName.GetCamelCaseName()}Repository.GetAll().Where(x => x.{parentEntityName}Id == {parentEntityName.GetCamelCaseName()}.Id).ToListAsync();//AfterGenerateCode : use Get all with navigation if there are relations
                var {entityName.GetCamelCaseName()}Event = new {entityName}DeletedBulkEvent({entityName.GetCamelCaseName().GetPluralName()}, {parentEntityName.GetCamelCaseName()}.Id);
                {parentEntityName.GetCamelCaseName()}.AddDomainEvent({entityName.GetCamelCaseName()}Event);"
                    + "\n\t\t\t\t//Add Child Deleted Events";

                var lines = File.ReadAllLines(parentCommandDeletePath).ToList();
                var index = lines.FindIndex(line => line.Contains("//Add Child Deleted Events"));

                if (index >= 0)
                {
                    lines[index] = childEvent;
                    File.WriteAllLines(parentCommandDeletePath, lines);
                }
                lines.Clear();
                index = -1;
                string childInject1 = $",I{entityName}Repository {entityName.GetCamelCaseName()}Repository" +
                    "\n\t\t\t\t\t\t//Inject1 Here";

                lines = File.ReadAllLines(parentCommandDeletePath).ToList();
                index = lines.FindIndex(line => line.Contains("//Inject1 Here"));

                if (index >= 0)
                {
                    lines[index] = childInject1;
                    File.WriteAllLines(parentCommandDeletePath, lines);
                }

                lines.Clear();
                index = -1;
                string childInject2 = $"_{entityName.GetCamelCaseName()}Repository = {entityName.GetCamelCaseName()}Repository;" +
                    "\n\t\t\t//Inject2 Here";

                lines = File.ReadAllLines(parentCommandDeletePath).ToList();
                index = lines.FindIndex(line => line.Contains("//Inject2 Here"));

                if (index >= 0)
                {
                    lines[index] = childInject2;
                    File.WriteAllLines(parentCommandDeletePath, lines);
                }

                lines.Clear();
                index = -1;
                string childInject3 = $"private readonly I{entityName}Repository _{entityName.GetCamelCaseName()}Repository;" +
                    "\n\t\t//Inject3 Here";

                lines = File.ReadAllLines(parentCommandDeletePath).ToList();
                index = lines.FindIndex(line => line.Contains("//Inject3 Here"));

                if (index >= 0)
                {
                    lines[index] = childInject3;
                    File.WriteAllLines(parentCommandDeletePath, lines);
                }

                lines.Clear();
                index = -1;
                string childUsing = $"using Domain.Events.{entityName}Events;" +
                    "\n\t\t//Add child event using Here";

                lines = File.ReadAllLines(parentCommandDeletePath).ToList();
                index = lines.FindIndex(line => line.Contains("//Add child event using Here"));

                if (index >= 0)
                {
                    lines[index] = childUsing;
                    File.WriteAllLines(parentCommandDeletePath, lines);
                }
            }
        }
        public static void GenerateSingleUpdateEntity(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, bool hasLocalization, List<Relation> relations, string? parentEntityName)
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
                else if (prop.Type == "FL")
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
                else if (prop.Type == "VDs")
                {
                    propList.Add($"\t\tpublic List<FileDto>? {prop.Name}Files {{ get; set; }} = new List<FileDto>();");
                    propList.Add($"\t\tpublic List<string>? Deleted{prop.Name}URLs {{ get; set; }}");
                }
                else if (prop.Type == "FLs")
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

                    case RelationType.UserSingle:
                        relationPropsList.Add($"\t\tpublic string {relation.DisplayedProperty}Id {{  get; set; }}\n");
                        break;
                    case RelationType.UserSingleNullable:
                        relationPropsList.Add($"\t\tpublic string? {relation.DisplayedProperty}Id {{  get; set; }}\n");
                        break;
                    case RelationType.UserMany:
                        relationPropsList.Add($"\t\tpublic List<string> {relation.DisplayedProperty.GetPluralName()}Ids {{  get; set; }}\n");
                        break;
                    default:
                        break;
                }
            }
            if (parentEntityName != null)
                relationPropsList.Add($"\t\tpublic Guid {parentEntityName}Id {{ get; set; }}");

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
        public static void GenerateUpdateCommandValidator(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<Relation> relations)
        {
            string className = $"Update{entityName}CommandValidator";
            string commandName = $"Update{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            string? methodsUnique = string.Empty;
            string? identityUsing = relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable || r.Type == RelationType.UserMany) ? "using Application.Common.Interfaces.Identity;" : null;
            StringBuilder injectCTOR1 = new($"ILogger<{className}> logger,I{entityName}Repository {char.ToLower(entityName[0]) + entityName.Substring(1)}Repository");
            foreach (var relation in relations)
            {
                if (relation.Type != RelationType.OneToMany && relation.Type != RelationType.OneToManyNullable && relation.Type != RelationType.OneToOneSelfJoin && relation.Type != RelationType.UserSingle && relation.Type != RelationType.UserSingleNullable && relation.Type != RelationType.UserMany)
                    injectCTOR1.Append($",I{relation.RelatedEntity}Repository {char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository");
            }
            StringBuilder injectCTOR2 = new StringBuilder($"\t\t\t_logger = logger;_{char.ToLower(entityName[0]) + entityName.Substring(1)}Repository = {char.ToLower(entityName[0]) + entityName.Substring(1)}Repository;");
            foreach (var relation in relations)
            {
                if (relation.Type != RelationType.OneToMany && relation.Type != RelationType.OneToManyNullable && relation.Type != RelationType.OneToOneSelfJoin && relation.Type != RelationType.UserSingle && relation.Type != RelationType.UserSingleNullable && relation.Type != RelationType.UserMany)
                    injectCTOR2.AppendLine($"_{char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository = {char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository;");
            }
            StringBuilder injectCTOR3 = new($"\t\tprivate readonly ILogger<{className}> _logger; private readonly I{entityName}Repository _{char.ToLower(entityName[0]) + entityName.Substring(1)}Repository;");
            foreach (var relation in relations)
            {
                if (relation.Type != RelationType.OneToMany && relation.Type != RelationType.OneToManyNullable && relation.Type != RelationType.OneToOneSelfJoin && relation.Type != RelationType.UserSingle && relation.Type != RelationType.UserSingleNullable && relation.Type != RelationType.UserMany)
                    injectCTOR3.AppendLine($"private readonly I{relation.RelatedEntity}Repository _{char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository;");
            }
            if (relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable || r.Type == RelationType.UserMany))
            {
                injectCTOR1.Append($",IIdentityService identityService");
                injectCTOR2.AppendLine($"\t\t\t_identityService = identityService;");
                injectCTOR3.AppendLine($"\tprivate readonly IIdentityService _identityService;");
            }
            StringBuilder rulesStore = new StringBuilder();
            foreach (var item in properties)
            {
                if (item.Type != "GPG" && item.Type != "PNGs" && item.Type != "VD" && item.Type != "VDs" && item.Type != "FL" && item.Type != "FLs")
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

            }
            rulesStore.AppendLine(GenerateRelationRules(relations, entityName));
            string rules = string.Join(Environment.NewLine, rulesStore.ToString());

            string content = $@"
using FluentValidation;
using Application.Common.Interfaces.IRepositories;
using Microsoft.Extensions.Logging;
using Application.{entityPlural}.Commands.Update{entityName};
{identityUsing}

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
        ////
        public static void GenerateDeleteCommand(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<Relation> relations, bool hasVersioning, bool hasNotification, bool hasUserAction, bool? isParent = null)
        {
            string className = $"Delete{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            string entityRepoName = $"_{lowerEntityName}";
            //string? addChildEvent = isParent != null ? "//AfterGenerateCode: Add Child Deleted Events" : null;
            string? addChildEvent = isParent != null && hasVersioning ? "//Add Child Deleted Events" : null;
            string? inject1 = isParent != null && hasVersioning ? "//Inject1 Here" : null;
            string? inject2 = isParent != null && hasVersioning ? "//Inject2 Here" : null;
            string? inject3 = isParent != null && hasVersioning ? "//Inject3 Here" : null;
            string? childEventUsing = isParent != null && hasVersioning ? "//Add child event using Here" : null;
            var inheritVersion = hasVersioning ? "VersionRequestOfTBase," : null;
            var eventVersionCode = !hasVersioning ? null : $@"
                {lowerEntityName}Event.RollbackedToVersionId = request.RollbackedToVersionId;
                {lowerEntityName}Event.IsVersionedCommand = request.IsVersionedCommand;
";
            var neededUsing = (hasVersioning || hasUserAction || hasNotification) ? $"using Domain.Events.{entityName}Events;using Application.Common.Models.Versioning;" : null;
            var eventCode1 = !(hasVersioning || hasUserAction || hasNotification) ? null :
                $@"
                var {lowerEntityName}Event = new {entityName}DeletedEvent({entityName.GetCamelCaseName()});
                {eventVersionCode}
";
            var eventCode2 = !(hasVersioning || hasUserAction || hasNotification) ? null :
                $@"
                {entityName.GetCamelCaseName()}.AddDomainEvent({lowerEntityName}Event);
";

            string? deletedAssetsVar = hasVersioning ? null : properties.Any(p => (p.Type == "GPG" || p.Type == "PNGs" || p.Type == "VD" || p.Type == "VDs" || p.Type == "FL" || p.Type == "FLs")) ? $"var deletedAssetsPaths = new List<string>();" : null;
            StringBuilder AssetSaveCode = new StringBuilder();
            string? deleteAssetsCode = hasVersioning ? null : properties.Any(p => (p.Type == "GPG" || p.Type == "PNGs" || p.Type == "VD" || p.Type == "VDs" || p.Type == "FL" || p.Type == "FLs"))
                ? $@"
                foreach (var path in deletedAssetsPaths)
                    {{
                        if (path != null)
                            await _fileService.DeleteFileAsync(path);
                    }}"
                : null;
            foreach (var prop in properties)
            {
                if (prop.Type == "GPG")
                    AssetSaveCode.Append($@"
                deletedAssetsPaths.Add({entityName.GetCamelCaseName()}.{prop.Name});");
                if (prop.Type == "VD")
                    AssetSaveCode.Append($@"
                deletedAssetsPaths.Add({entityName.GetCamelCaseName()}.{prop.Name});");
                if (prop.Type == "FL")
                    AssetSaveCode.Append($@"
                deletedAssetsPaths.Add({entityName.GetCamelCaseName()}.{prop.Name});");
                if (prop.Type == "PNGs")
                    AssetSaveCode.Append($@"
                foreach(var path in {entityName.GetCamelCaseName()}.{prop.Name})
                    deletedAssetsPaths.Add(path);");
                if (prop.Type == "VDs")
                    AssetSaveCode.Append($@"
                foreach(var path in {entityName.GetCamelCaseName()}.{prop.Name})
                    deletedAssetsPaths.Add(path);");
                if (prop.Type == "FLs")
                    AssetSaveCode.Append($@"
                foreach(var path in {entityName.GetCamelCaseName()}.{prop.Name})
                    deletedAssetsPaths.Add(path);");
            }
            bool getMethodCondition = relations.Any() && !(relations.Count == 1 && relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable));
            string GetMethod = getMethodCondition ? $"Get{entityName}" : "GetByIdAsync";
            string? AssetSaveCodeLine = hasVersioning ? null : AssetSaveCode.ToString();
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
{childEventUsing}

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
        {inject3}

        public {className}Handler(ILogger<{className}Handler> logger,
                                            I{entityName}Repository {lowerEntityName}Repository,
                                            IUnitOfWorkAsync unitOfWork,
                                            IFileService fileService
                                            {inject1}
                                                    )
        {{
            _logger = logger;
            {entityRepoName}Repository = {lowerEntityName}Repository;
            _unitOfWork = unitOfWork;
            _fileService = fileService;
            {inject2}
        }}

        public async Task Handle({className} request, CancellationToken cancellationToken)
        {{
            try
            {{
                await _unitOfWork.BeginTransactionAsync();
                var {entityName.GetCamelCaseName()} = await {entityRepoName}Repository.{GetMethod}(request.{entityName}Id);
                {deletedAssetsVar}
                {AssetSaveCodeLine}
                {eventCode1}

                await {entityRepoName}Repository.DeleteAsync({entityName.GetCamelCaseName()});
                {addChildEvent}
                {eventCode2}
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitAsync();
                {deleteAssetsCode}
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
        public static void GenerateDeleteCommandValidator(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<Relation> relations)
        {
            string className = $"Delete{entityName}CommandValidator";
            string commandName = $"Delete{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            string entityRepoName = $"_{lowerEntityName}";

            StringBuilder injectCTOR1 = new($"ILogger<{className}> logger,I{entityName}Repository {lowerEntityName}Repository");
            foreach (var relation in relations)
            {
                if (relation.Type == RelationType.OneToMany || relation.Type == RelationType.OneToManyNullable)
                    injectCTOR1.Append($",I{relation.RelatedEntity}Repository {char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository");
            }
            StringBuilder injectCTOR2 = new StringBuilder($"\t\t\t_logger = logger;{entityRepoName}Repository = {lowerEntityName}Repository;");
            foreach (var relation in relations)
            {
                if (relation.Type == RelationType.OneToMany || relation.Type == RelationType.OneToManyNullable)
                    injectCTOR2.AppendLine($"_{char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository = {char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository;");
            }
            StringBuilder injectCTOR3 = new($"\t\tprivate readonly ILogger<{className}> _logger; private readonly I{entityName}Repository {entityRepoName}Repository;");
            foreach (var relation in relations)
            {
                if (relation.Type == RelationType.OneToMany || relation.Type == RelationType.OneToManyNullable)
                    injectCTOR3.AppendLine($"private readonly I{relation.RelatedEntity}Repository _{char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository;");
            }
            string? customCanDelete = null;
            if (relations.Any(r => r.Type == RelationType.OneToMany || r.Type == RelationType.OneToManyNullable || r.Type == RelationType.OneToOneSelfJoin))
            {
                customCanDelete = $@"
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                    if (!await CanDeleteEntity(context.InstanceToValidate))
                    {{
                        context.AddFailure(""Delete {entityName}"", ""{entityName} has children !"");
                    }}
                }})";
            }

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
{injectCTOR3}
        public {className}({injectCTOR1})
        {{
{injectCTOR2}
            RuleFor(l => l.{entityName}Id)
                .NotEmpty().WithMessage(""Id Must be passed"")
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                    if (!await Is{entityName}Existed(context.InstanceToValidate))
                    {{
                        context.AddFailure(""Delete {entityName}"", ""{entityName} is not found !"");
                    }}
                }})
{customCanDelete}
                ;
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
{GenerateRelationDeleteMethod(relations, entityName)}
    }}
}}
";
            File.WriteAllText(filePath, content);
        }
        ////
        public static void GenerateGetByIdQuery(string entityName, string entityPlural, string path, bool hasLocalization, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, List<Relation> relations,bool hasUserAction, bool? isParent = null, string? parentEntityName = null)
        {
            var folderPath = Path.Combine(path, $"Get{entityName}");
            Directory.CreateDirectory(folderPath);

            GenerateGetByIdDto(entityName, entityPlural, folderPath, properties, enumProps, relations, isParent, parentEntityName);
            GenerateGetByIdQueryFile(entityName, entityPlural, folderPath, hasLocalization, relations, hasUserAction,parentEntityName);
            if (parentEntityName == null)
                GenerateGetByIdValidator(entityName, entityPlural, folderPath);
        }
        static void GenerateGetByIdDto(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, List<Relation> relations, bool? isParent = null, string? parentEntityName = null)
        {
            string fileName = $"Get{entityName}Dto.cs";
            string filePath = Path.Combine(path, fileName);
            StringBuilder mapperEnum = new StringBuilder();
            StringBuilder mapperAssets = new StringBuilder();
            List<string> propList = new List<string>();
            foreach (var prop in properties)
            {
                if (enumProps.Any(p => p.prop == prop.Name))
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        mapperEnum.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}, opt => opt.MapFrom(src => ({entityName}{prop.Name})src.{prop.Name}))");
                    }
                    else
                    {
                        mapperEnum.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}, opt => opt.MapFrom(src => ({entityName}{prop.Name}?)src.{prop.Name}))");
                    }
                }
                if (prop.Type == "GPG")
                {
                    mapperAssets.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}Url, opt => opt.MapFrom(src => src.{prop.Name}))");
                    if (isParent != null || parentEntityName != null)
                    {
                        propList.Add($"\t\tpublic object {prop.Name} {{ get; set; }} = null!;");
                        propList.Add($"\t\tpublic bool Delete{prop.Name} {{ get; set; }} = false;");
                        propList.Add($"\t\tpublic object {prop.Name}Src {{ get; set; }} = null!;");
                        mapperAssets.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}, opt => opt.Ignore())");
                    }
                }
                else if (prop.Type == "PNGs")
                {
                    mapperAssets.AppendLine($"\t\t\t\t\t.ForMember(dest => dest.{prop.Name}Urls, opt => opt.MapFrom(src => src.{prop.Name}))");
                    if (isParent != null || parentEntityName != null)
                    {
                        propList.Add($"\t\tpublic List<string> {prop.Name} {{ get; set; }} = new List<string>();");
                        propList.Add($"\t\tpublic List<string> Deleted{prop.Name}Urls {{ get; set; }} = new List<string>();");
                        propList.Add($"\t\tpublic List<string> {prop.Name}Srcs {{ get; set; }} = new List<string>();");
                        mapperAssets.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}, opt => opt.Ignore())");
                    }
                }
                else if (prop.Type == "VD")
                {
                    mapperAssets.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}Url, opt => opt.MapFrom(src => src.{prop.Name}))");
                    if (isParent != null || parentEntityName != null)
                    {
                        propList.Add($"\t\tpublic object {prop.Name} {{ get; set; }} = null!;");
                        propList.Add($"\t\tpublic bool Delete{prop.Name} {{ get; set; }} = false;");
                        propList.Add($"\t\tpublic object {prop.Name}Src {{ get; set; }} = null!;");
                        mapperAssets.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}, opt => opt.Ignore())");
                    }
                }
                else if (prop.Type == "VDs")
                {
                    mapperAssets.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}Urls, opt => opt.MapFrom(src => src.{prop.Name}))");
                    if (isParent != null || parentEntityName != null)
                    {
                        propList.Add($"\t\tpublic List<string> {prop.Name} {{ get; set; }} = new List<string>();");
                        propList.Add($"\t\tpublic List<string> Deleted{prop.Name}Urls {{ get; set; }} = new List<string>();");
                        propList.Add($"\t\tpublic List<string> {prop.Name}Srcs {{ get; set; }} = new List<string>();");
                        mapperAssets.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}, opt => opt.Ignore())");
                    }
                }
                else if (prop.Type == "FL")
                {
                    mapperAssets.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}Url, opt => opt.MapFrom(src => src.{prop.Name}))");
                    if (isParent != null || parentEntityName != null)
                    {
                        propList.Add($"\t\tpublic object {prop.Name} {{ get; set; }} = null!;");
                        propList.Add($"\t\tpublic bool Delete{prop.Name} {{ get; set; }} = false;");
                        propList.Add($"\t\tpublic object {prop.Name}Src {{ get; set; }} = null!;");
                        mapperAssets.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}, opt => opt.Ignore())");
                    }
                }
                else if (prop.Type == "FLs")
                {
                    mapperAssets.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}Urls, opt => opt.MapFrom(src => src.{prop.Name}))");
                    if (isParent != null || parentEntityName != null)
                    {
                        propList.Add($"\t\tpublic List<string> {prop.Name} {{ get; set; }} = new List<string>();");
                        propList.Add($"\t\tpublic List<string> Deleted{prop.Name}Urls {{ get; set; }} = new List<string>();");
                        propList.Add($"\t\tpublic List<string> {prop.Name}Srcs {{ get; set; }} = new List<string>();");
                        mapperAssets.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}, opt => opt.Ignore())");
                    }
                }
            }

            StringBuilder relationMapp = new StringBuilder();
            foreach (var rel in relations)
            {
                switch (rel.Type)
                {
                    case RelationType.OneToOneSelfJoin:
                        relationMapp.AppendLine($"\t\t\t\t.ForMember(dest => dest.{rel.RelatedEntity}Parent{rel.DisplayedProperty}, opt => opt.MapFrom(src => src.{rel.RelatedEntity}Parent != null ? src.{rel.RelatedEntity}Parent.{rel.DisplayedProperty} : null))");
                        break;
                    case RelationType.OneToOne:
                        relationMapp.AppendLine($"\t\t\t\t.ForMember(dest => dest.{rel.RelatedEntity}{rel.DisplayedProperty}, opt => opt.MapFrom(src => src.{rel.RelatedEntity}.{rel.DisplayedProperty}))");
                        break;
                    case RelationType.OneToOneNullable:
                        relationMapp.AppendLine($"\t\t\t\t.ForMember(dest => dest.{rel.RelatedEntity}{rel.DisplayedProperty}, opt => opt.MapFrom(src => src.{rel.RelatedEntity} != null ? src.{rel.RelatedEntity}.{rel.DisplayedProperty} : null))");
                        break;
                    case RelationType.ManyToOne:
                        relationMapp.AppendLine($"\t\t\t\t.ForMember(dest => dest.{rel.RelatedEntity}{rel.DisplayedProperty}, opt => opt.MapFrom(src => src.{rel.RelatedEntity}.{rel.DisplayedProperty}))");
                        break;
                    case RelationType.ManyToOneNullable:
                        relationMapp.AppendLine($"\t\t\t\t.ForMember(dest => dest.{rel.RelatedEntity}{rel.DisplayedProperty}, opt => opt.MapFrom(src => src.{rel.RelatedEntity} != null ? src.{rel.RelatedEntity}.{rel.DisplayedProperty} : null))");
                        break;
                    case RelationType.ManyToMany:
                        string entityRelatedPlural = rel.RelatedEntity.EndsWith("y") ? rel.RelatedEntity[..^1] + "ies" : rel.RelatedEntity + "s";
                        string displayedPropertyPlural = rel.DisplayedProperty.EndsWith("y") ? rel.DisplayedProperty[..^1] + "ies" : rel.DisplayedProperty + "s";
                        relationMapp.AppendLine($"\t\t\t\t.ForMember(dest => dest.{rel.RelatedEntity}{displayedPropertyPlural}, opt => opt.MapFrom(src => src.{entityRelatedPlural}.Select(x => x.{rel.DisplayedProperty})))");
                        relationMapp.AppendLine($"\t\t\t\t.ForMember(dest => dest.{rel.RelatedEntity}Ids, opt => opt.MapFrom(src => src.{entityRelatedPlural}.Select(x => x.Id)))");
                        break;
                    default:
                        break;
                }
            }

            string content = $@"using Domain.Entities;
using Application.Common.Models.AssistantModels;
using Domain.Enums;
namespace Application.{entityPlural}.Queries.Get{entityName}Query
{{
    public class Get{entityName}Dto : {entityName}BaseDto
    {{
{string.Join(Environment.NewLine, propList)}
        public class Mapping : Profile
        {{
            public Mapping()
            {{
                CreateMap<{entityName}, Get{entityName}Dto>()
                {mapperEnum}
                {mapperAssets}
                {relationMapp}
                ;
            }}
        }}
    }}
}}";
            File.WriteAllText(filePath, content);
        }
        static void GenerateGetByIdQueryFile(string entityName, string entityPlural, string path, bool hasLocalization, List<Relation> relations,bool hasUserAction, string? parentEntityName = null)
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

            string? identityUsing = relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable || r.Type == RelationType.UserMany) ? "using Application.Common.Interfaces.Identity;" : null;

            //string getMethod =  relations.Any() ? $"Get{entityName}" : parentEntityName != null ? $"Get{entityName}ByParent" : "GetByIdAsync";
            bool getMethodCondition = relations.Any() && !(relations.Count == 1 && relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable));
            string getMethod = parentEntityName != null ? $"Get{entityName}ByParent" : getMethodCondition ? $"Get{entityName}" : "GetByIdAsync";
            StringBuilder UserRelationCode = new StringBuilder();
            string? injectCTORIdentity1 = null;
            string? injectCTORIdentity2 = null;
            string? injectCTORIdentity3 = null;
            if (relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable || r.Type == RelationType.UserMany))
            {
                injectCTORIdentity1 = ",IIdentityService identityService";
                injectCTORIdentity2 = "_identityService = identityService;";
                injectCTORIdentity3 = "private readonly IIdentityService _identityService;";
                foreach (var rel in relations)
                {
                    string code = null!;
                    switch (rel.Type)
                    {
                        case RelationType.UserSingle:
                            code = $@"
            var {rel.DisplayedProperty.GetCamelCaseName()} = await _identityService.GetUserByIdAsync({entityName.ToLower()}.{rel.DisplayedProperty}Id);
            dto.{rel.DisplayedProperty}Name = {rel.DisplayedProperty.GetCamelCaseName()}.FullName;";
                            UserRelationCode.Append(code);
                            break;

                        case RelationType.UserSingleNullable:
                            code = $@"
            if({entityName.ToLower()}.{rel.DisplayedProperty}Id != null)
            {{
                var {rel.DisplayedProperty.GetCamelCaseName()} = await _identityService.GetUserByIdAsync({entityName.ToLower()}.{rel.DisplayedProperty}Id);
                dto.{rel.DisplayedProperty}Name = {rel.DisplayedProperty.GetCamelCaseName()}.FullName;
            }}";
                            UserRelationCode.Append(code);
                            break;

                        case RelationType.UserMany:
                            code = $@"
            //Fill {rel.DisplayedProperty.GetPluralName()}
            var usersIds = {entityName.ToLower()}.{entityName}{rel.DisplayedProperty.GetPluralName()}.Select(x => x.UserId).ToList();
            var users = await _identityService.GetAllUsers()
                .Where(u => usersIds.Contains(u.Id))
                .ToListAsync();
            foreach (var user in users)
            {{
                dto.{rel.DisplayedProperty.GetPluralName()}Names.Add(user.FullName);
                dto.{rel.DisplayedProperty.GetPluralName()}Ids.Add(user.Id);

            }}";
                            UserRelationCode.Append(code);
                            break;
                        default:
                            break;
                    }
                }
            }
            string? childCode = parentEntityName == null ? null : $@"
            dto.{parentEntityName}Id = request.{parentEntityName}Id;
            if ({entityName.ToLower()} == null)
            {{
                return dto;
            }}";
            string? queryParam = parentEntityName == null ? $"{entityName}Id" : $"{parentEntityName}Id";

            string? injectCTORMediateR1 = hasUserAction ? ",IMediator mediator" : null;
            string? injectCTORMediateR2 = hasUserAction ? "_mediator = mediator;" : null;
            string? injectCTORMediateR3 = hasUserAction ? "private readonly IMediator _mediator;" : null;
            string? usingEvent = hasUserAction ? $"using Domain.Events.{entityName}Events;" : null;
            string ? getEventCode = !hasUserAction ? null : $@"
            {entityName}GetEvent getEvent = new {entityName}GetEvent({entityName.ToLower()});
            await _mediator.Publish(getEvent);";
            string content = $@"
using Microsoft.Extensions.Logging;
using Application.Common.Interfaces.IRepositories;
using Application.Common.Interfaces.Services;
{identityUsing}
{usingEvent}

namespace Application.{entityPlural}.Queries.Get{entityName}Query
{{
    public class Get{entityName}Query : IRequest<Get{entityName}Dto>
    {{
        public Guid {queryParam} {{ get; set; }}
        {languageIdProp}
    }}

    public class Get{entityName}QueryHandler : IRequestHandler<Get{entityName}Query, Get{entityName}Dto>
    {{
        private readonly ILogger<Get{entityName}QueryHandler> _logger;
        private readonly IMapper _mapper;
        private readonly I{entityName}Repository {entityRepoName}Repository;
        private readonly ILocalizationService _localizationService;
        {injectCTORIdentity3}
        {injectCTORMediateR3}

        public Get{entityName}QueryHandler(ILogger<Get{entityName}QueryHandler> logger,
                                           IMapper mapper,
                                           ILocalizationService localizationService,
                                           I{entityName}Repository {lowerEntityName}Repository{injectCTORIdentity1}{injectCTORMediateR1})
        {{
            _logger = logger;
            _mapper = mapper;
            _localizationService = localizationService;
            {entityRepoName}Repository = {lowerEntityName}Repository;
            {injectCTORIdentity2}
            {injectCTORMediateR2}
        }}

        public async Task<Get{entityName}Dto> Handle(Get{entityName}Query request, CancellationToken cancellationToken)
        {{
            var {entityName.ToLower()} = await {entityRepoName}Repository.{getMethod}(request.{queryParam});
            Get{entityName}Dto dto = new Get{entityName}Dto();
{childCode}
            dto = _mapper.Map<Get{entityName}Dto>({entityName.ToLower()});
            {UserRelationCode}
            {localizationCode}
            {getEventCode}
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
                        context.AddFailure(""Get {entityName}"", ""{entityName} is not found"");
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
        ////
        public static void GenerateGetWithLocalizationQuery(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, List<Relation> relations)
        {
            var folderPath = Path.Combine(path, $"Get{entityName}WithLocalization");
            Directory.CreateDirectory(folderPath);
            GenerateGetWithLocalizationDto(entityName, entityPlural, folderPath, properties, enumProps, relations);
            GenerateGetWithLocalizationQueryFile(entityName, entityPlural, folderPath, relations);
            GenerateGetWithLocalizationValidator(entityName, entityPlural, folderPath);
        }
        static void GenerateGetWithLocalizationDto(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, List<Relation> relations)
        {
            string fileName = $"Get{entityName}WithLocalizationDto.cs";
            string filePath = Path.Combine(path, fileName);
            StringBuilder mapperEnum = new StringBuilder();
            StringBuilder mapperAssets = new StringBuilder();

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
                    if (prop.Type == "GPG")
                    {
                        mapperAssets.Append($".ForMember(dest => dest.{prop.Name}Url, opt => opt.MapFrom(src => src.{prop.Name}))");

                    }
                    else if (prop.Type == "PNGs")
                    {
                        mapperAssets.Append($".ForMember(dest => dest.{prop.Name}Urls, opt => opt.MapFrom(src => src.{prop.Name}))");
                    }
                    else if (prop.Type == "VD")
                    {
                        mapperAssets.Append($".ForMember(dest => dest.{prop.Name}Url, opt => opt.MapFrom(src => src.{prop.Name}))");
                    }
                    else if (prop.Type == "VDs")
                    {
                        mapperAssets.Append($".ForMember(dest => dest.{prop.Name}Urls, opt => opt.MapFrom(src => src.{prop.Name}))");
                    }
                    else if (prop.Type == "FL")
                    {
                        mapperAssets.Append($".ForMember(dest => dest.{prop.Name}Url, opt => opt.MapFrom(src => src.{prop.Name}))");
                    }
                    else if (prop.Type == "FLs")
                    {
                        mapperAssets.Append($".ForMember(dest => dest.{prop.Name}Urls, opt => opt.MapFrom(src => src.{prop.Name}))");
                    }
                }
            }
            StringBuilder relationMapp = new StringBuilder();
            foreach (var rel in relations)
            {
                switch (rel.Type)
                {
                    case RelationType.OneToOneSelfJoin:
                        relationMapp.AppendLine($"\t\t\t\t.ForMember(dest => dest.{rel.RelatedEntity}Parent{rel.DisplayedProperty}, opt => opt.MapFrom(src => src.{rel.RelatedEntity}Parent != null ? src.{rel.RelatedEntity}Parent.{rel.DisplayedProperty} : null))");
                        break;
                    case RelationType.OneToOne:
                        relationMapp.AppendLine($"\t\t\t\t.ForMember(dest => dest.{rel.RelatedEntity}{rel.DisplayedProperty}, opt => opt.MapFrom(src => src.{rel.RelatedEntity}.{rel.DisplayedProperty}))");
                        break;
                    case RelationType.OneToOneNullable:
                        relationMapp.AppendLine($"\t\t\t\t.ForMember(dest => dest.{rel.RelatedEntity}{rel.DisplayedProperty}, opt => opt.MapFrom(src => src.{rel.RelatedEntity} != null ? src.{rel.RelatedEntity}.{rel.DisplayedProperty} : null))");
                        break;
                    case RelationType.ManyToOne:
                        relationMapp.AppendLine($"\t\t\t\t.ForMember(dest => dest.{rel.RelatedEntity}{rel.DisplayedProperty}, opt => opt.MapFrom(src => src.{rel.RelatedEntity}.{rel.DisplayedProperty}))");
                        break;
                    case RelationType.ManyToOneNullable:
                        relationMapp.AppendLine($"\t\t\t\t.ForMember(dest => dest.{rel.RelatedEntity}{rel.DisplayedProperty}, opt => opt.MapFrom(src => src.{rel.RelatedEntity} != null ? src.{rel.RelatedEntity}.{rel.DisplayedProperty} : null))");
                        break;
                    case RelationType.ManyToMany:
                        string entityRelatedPlural = rel.RelatedEntity.EndsWith("y") ? rel.RelatedEntity[..^1] + "ies" : rel.RelatedEntity + "s";
                        string displayedPropertyPlural = rel.DisplayedProperty.EndsWith("y") ? rel.DisplayedProperty[..^1] + "ies" : rel.DisplayedProperty + "s";
                        relationMapp.AppendLine($"\t\t\t\t.ForMember(dest => dest.{rel.RelatedEntity}{displayedPropertyPlural}, opt => opt.MapFrom(src => src.{entityRelatedPlural}.Select(x => x.{rel.DisplayedProperty})))");
                        relationMapp.AppendLine($"\t\t\t\t.ForMember(dest => dest.{rel.RelatedEntity}Ids, opt => opt.MapFrom(src => src.{entityRelatedPlural}.Select(x => x.Id)))");
                        break;
                    default:
                        break;
                }
            }
            string content = $@"using Domain.Entities;
using Application.Common.Models.AssistantModels;
using Domain.Enums;
namespace Application.{entityPlural}.Queries.Get{entityName}WithLocalization
{{
    public class Get{entityName}WithLocalizationDto : {entityName}BaseDto
    {{
        public List<{entityName}LocalizationDto> {entityName}Localizations {{ get; set; }} = new List<{entityName}LocalizationDto>();

        public class Mapping : Profile
        {{
            public Mapping()
            {{
                CreateMap<{entityName}, Get{entityName}WithLocalizationDto>()
                {mapperEnum}
                {mapperAssets}
                {relationMapp}
                ;
            }}
        }}
    }}
}}";
            File.WriteAllText(filePath, content);

        }
        static void GenerateGetWithLocalizationQueryFile(string entityName, string entityPlural, string path, List<Relation> relations)
        {
            string fileName = $"Get{entityName}WithLocalizationQuery.cs";
            string filePath = Path.Combine(path, fileName);
            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            string entityRepoName = $"_{lowerEntityName}";
            string? identityUsing = relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable || r.Type == RelationType.UserMany) ? "using Application.Common.Interfaces.Identity;" : null;
            bool getMethodCondition = relations.Any() && !(relations.Count == 1 && relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable));

            string getMethod = getMethodCondition ? $"Get{entityName}" : "GetByIdAsync";
            StringBuilder UserRelationCode = new StringBuilder();
            string? injectCTORIdentity1 = null;
            string? injectCTORIdentity2 = null;
            string? injectCTORIdentity3 = null;
            if (relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable || r.Type == RelationType.UserMany))
            {
                injectCTORIdentity1 = ",IIdentityService identityService";
                injectCTORIdentity2 = "_identityService = identityService;";
                injectCTORIdentity3 = "private readonly IIdentityService _identityService;";
                foreach (var rel in relations)
                {
                    string code = null!;
                    switch (rel.Type)
                    {
                        case RelationType.UserSingle:
                            code = $@"
            var {rel.DisplayedProperty.GetCamelCaseName()} = await _identityService.GetUserByIdAsync({entityName.ToLower()}.{rel.DisplayedProperty}Id);
            result.{rel.DisplayedProperty}Name = {rel.DisplayedProperty.GetCamelCaseName()}.FullName;";
                            UserRelationCode.Append(code);
                            break;

                        case RelationType.UserSingleNullable:
                            code = $@"
            if({entityName.ToLower()}.{rel.DisplayedProperty}Id != null)
            {{
                var {rel.DisplayedProperty.GetCamelCaseName()} = await _identityService.GetUserByIdAsync({entityName.ToLower()}.{rel.DisplayedProperty}Id);
                result.{rel.DisplayedProperty}Name = {rel.DisplayedProperty.GetCamelCaseName()}.FullName;
            }}";
                            UserRelationCode.Append(code);
                            break;

                        case RelationType.UserMany:
                            code = $@"
            //Fill {rel.DisplayedProperty.GetPluralName()}
            var usersIds = {entityName.ToLower()}.{entityName}{rel.DisplayedProperty.GetPluralName()}.Select(x => x.UserId).ToList();
            var users = await _identityService.GetAllUsers()
                .Where(u => usersIds.Contains(u.Id))
                .ToListAsync();
            foreach (var user in users)
            {{
                result.{rel.DisplayedProperty.GetPluralName()}Names.Add(user.FullName);
            }}";
                            UserRelationCode.Append(code);
                            break;
                        default:
                            break;
                    }
                }
            }
            string content = $@"using System;
using Microsoft.Extensions.Logging;
using Application.Common.Interfaces.IRepositories;
{identityUsing}

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
        {injectCTORIdentity3}

        public Get{entityName}WithLocalizationQueryHandler(ILogger<Get{entityName}WithLocalizationQueryHandler> logger,
                                           IMapper mapper,
                                           I{entityName}Repository {lowerEntityName}Repository{injectCTORIdentity1})
        {{
            _logger = logger;
            _mapper = mapper;
            {entityRepoName}Repository = {lowerEntityName}Repository;
            {injectCTORIdentity2}
        }}

        public async Task<Get{entityName}WithLocalizationDto> Handle(Get{entityName}WithLocalizationQuery request, CancellationToken cancellationToken)
        {{
            var {entityName.ToLower()} = await {entityRepoName}Repository.{getMethod}(request.{entityName}Id);//TODO:AfterGenerateCode: add method to repository to get object include Localization
            var result = _mapper.Map<Get{entityName}WithLocalizationDto>({entityName.ToLower()});
            {UserRelationCode}
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
                        context.AddFailure(""Get {entityName}"", ""{entityName} is not found"");
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
        public static void GenerateGetAllQuery(string entityName, string entityPlural, string path, bool hasLocalization, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, List<Relation> relations, string? parentEntityName)
        {
            var folderPath = Path.Combine(path, $"Get{entityPlural}WithPagination");
            Directory.CreateDirectory(folderPath);

            GenerateGetAllDto(entityName, entityPlural, folderPath, properties, enumProps, relations, parentEntityName);
            GenerateGetWithPaginationQueryFile(entityName, entityPlural, folderPath, hasLocalization, relations);
            GenerateGetWithPaginationValidator(entityName, entityPlural, folderPath);
        }
        public static void GenerateGetBulkQuery(string entityName, string entityPlural, string path, bool hasLocalization, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, List<Relation> relations, bool hasUserAction, string? parentEntityName)
        {
            var folderPath = Path.Combine(path, $"GetBulk{entityPlural}");
            Directory.CreateDirectory(folderPath);

            GenerateGetAllDto(entityName, entityPlural, folderPath, properties, enumProps, relations, parentEntityName);
            GenerateGetBulkQueryFile(entityName, entityPlural, folderPath, hasLocalization, relations, hasUserAction, parentEntityName);
        }
        static void GenerateGetAllDto(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, List<Relation> relations, string? parentEntityName)
        {
            string fileName = parentEntityName == null ? $"Get{entityPlural}WithPaginationDto.cs" : $"GetBulk{entityPlural}Dto.cs";
            string className = parentEntityName == null ? $"Get{entityPlural}WithPaginationDto" : $"GetBulk{entityPlural}Dto";
            string namespaceName = parentEntityName == null ? $"Get{entityPlural}WithPagination" : $"GetBulk{entityPlural}";
            string filePath = Path.Combine(path, fileName);

            StringBuilder mapperEnum = new StringBuilder();
            StringBuilder mapperAssets = new StringBuilder();
            List<string> propList = new List<string>();
            foreach (var prop in properties)
            {
                if (enumProps.Any(p => p.prop == prop.Name))
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        mapperEnum.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}, opt => opt.MapFrom(src => ({entityName}{prop.Name})src.{prop.Name}))");
                    }
                    else
                    {
                        mapperEnum.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}, opt => opt.MapFrom(src => ({entityName}{prop.Name}?)src.{prop.Name}))");
                    }
                }
                if (prop.Type == "GPG")
                {
                    mapperAssets.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}Url, opt => opt.MapFrom(src => src.{prop.Name}))");
                    if (parentEntityName != null)
                    {
                        propList.Add($"\t\t\tpublic object {prop.Name} {{ get; set; }} = null!;");
                        propList.Add($"\t\t\tpublic bool Delete{prop.Name} {{ get; set; }} = false;");
                        propList.Add($"\t\t\tpublic object {prop.Name}Src {{ get; set; }} = null!;");
                        mapperAssets.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}, opt => opt.Ignore())");
                    }

                }
                else if (prop.Type == "PNGs")
                {
                    mapperAssets.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}Urls, opt => opt.MapFrom(src => src.{prop.Name}))");
                    if (parentEntityName != null)
                    {
                        propList.Add($"\t\t\tpublic List<string> {prop.Name} {{ get; set; }} = new List<string>();");
                        propList.Add($"\t\t\tpublic List<string> Deleted{prop.Name}Urls {{ get; set; }} = new List<string>();");
                        propList.Add($"\t\t\tpublic List<string> {prop.Name}Srcs {{ get; set; }} = new List<string>();");
                        mapperAssets.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}, opt => opt.Ignore())");
                    }
                }
                else if (prop.Type == "VD")
                {
                    mapperAssets.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}Url, opt => opt.MapFrom(src => src.{prop.Name}))");
                    if (parentEntityName != null)
                    {
                        propList.Add($"\t\t\tpublic object {prop.Name} {{ get; set; }} = null!;");
                        propList.Add($"\t\t\tpublic bool Delete{prop.Name} {{ get; set; }} = false;");
                        propList.Add($"\t\t\tpublic object {prop.Name}Src {{ get; set; }} = null!;");
                        mapperAssets.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}, opt => opt.Ignore())");
                    }
                }
                else if (prop.Type == "VDs")
                {
                    mapperAssets.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}Urls, opt => opt.MapFrom(src => src.{prop.Name}))");
                    if (parentEntityName != null)
                    {
                        propList.Add($"\t\t\tpublic List<string> {prop.Name} {{ get; set; }} = new List<string>();");
                        propList.Add($"\t\t\tpublic List<string> Deleted{prop.Name}Urls {{ get; set; }} = new List<string>();");
                        propList.Add($"\t\t\tpublic List<string> {prop.Name}Srcs {{ get; set; }} = new List<string>();");
                        mapperAssets.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}, opt => opt.Ignore())");
                    }
                }
                else if (prop.Type == "FL")
                {
                    mapperAssets.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}Url, opt => opt.MapFrom(src => src.{prop.Name}))");
                    if (parentEntityName != null)
                    {
                        propList.Add($"\t\t\tpublic object {prop.Name} {{ get; set; }} = null!;");
                        propList.Add($"\t\t\tpublic bool Delete{prop.Name} {{ get; set; }} = false;");
                        propList.Add($"\t\t\tpublic object {prop.Name}Src {{ get; set; }} = null!;");
                        mapperAssets.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}, opt => opt.Ignore())");
                    }
                }
                else if (prop.Type == "FLs")
                {
                    mapperAssets.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}Urls, opt => opt.MapFrom(src => src.{prop.Name}))");
                    if (parentEntityName != null)
                    {
                        propList.Add($"\t\t\tpublic List<string> {prop.Name} {{ get; set; }} = new List<string>();");
                        propList.Add($"\t\t\tpublic List<string> Deleted{prop.Name}Urls {{ get; set; }} = new List<string>();");
                        propList.Add($"\t\t\tpublic List<string> {prop.Name}Srcs {{ get; set; }} = new List<string>();");
                        propList.Add($"\t\t\tpublic bool Is{prop.Name}Fetched {{ get; set; }} = false;");
                        mapperAssets.AppendLine($"\t\t\t\t.ForMember(dest => dest.{prop.Name}, opt => opt.Ignore())");
                    }
                }
            }

            StringBuilder relationMapp = new StringBuilder();
            foreach (var rel in relations)
            {
                switch (rel.Type)
                {
                    case RelationType.OneToOneSelfJoin:
                        relationMapp.AppendLine($"\t\t\t\t.ForMember(dest => dest.{rel.RelatedEntity}Parent{rel.DisplayedProperty}, opt => opt.MapFrom(src => src.{rel.RelatedEntity}Parent != null ? src.{rel.RelatedEntity}Parent.{rel.DisplayedProperty} : null))");
                        break;
                    case RelationType.OneToOne:
                        relationMapp.AppendLine($"\t\t\t\t.ForMember(dest => dest.{rel.RelatedEntity}{rel.DisplayedProperty}, opt => opt.MapFrom(src => src.{rel.RelatedEntity}.{rel.DisplayedProperty}))");
                        break;
                    case RelationType.OneToOneNullable:
                        relationMapp.AppendLine($"\t\t\t\t.ForMember(dest => dest.{rel.RelatedEntity}{rel.DisplayedProperty}, opt => opt.MapFrom(src => src.{rel.RelatedEntity} != null ? src.{rel.RelatedEntity}.{rel.DisplayedProperty} : null))");
                        break;
                    case RelationType.ManyToOne:
                        relationMapp.AppendLine($"\t\t\t\t.ForMember(dest => dest.{rel.RelatedEntity}{rel.DisplayedProperty}, opt => opt.MapFrom(src => src.{rel.RelatedEntity}.{rel.DisplayedProperty}))");
                        break;
                    case RelationType.ManyToOneNullable:
                        relationMapp.AppendLine($"\t\t\t\t.ForMember(dest => dest.{rel.RelatedEntity}{rel.DisplayedProperty}, opt => opt.MapFrom(src => src.{rel.RelatedEntity} != null ? src.{rel.RelatedEntity}.{rel.DisplayedProperty} : null))");
                        break;
                    case RelationType.ManyToMany:
                        string entityRelatedPlural = rel.RelatedEntity.EndsWith("y") ? rel.RelatedEntity[..^1] + "ies" : rel.RelatedEntity + "s";
                        string displayedPropertyPlural = rel.DisplayedProperty.EndsWith("y") ? rel.DisplayedProperty[..^1] + "ies" : rel.DisplayedProperty + "s";
                        if (parentEntityName != null)
                        {
                            relationMapp.AppendLine($"\t\t\t\t.ForMember(dest => dest.{rel.RelatedEntity.GetPluralName()}{displayedPropertyPlural}, opt => opt.MapFrom(src => src.{entityRelatedPlural}.Select(x => x.{rel.DisplayedProperty})))");
                            relationMapp.AppendLine($"\t\t\t\t.ForMember(dest => dest.{rel.RelatedEntity.GetPluralName()}Ids, opt => opt.MapFrom(src => src.{entityRelatedPlural}.Select(x => x.Id)))");
                        }
                        else
                        {
                            relationMapp.AppendLine($"\t\t\t\t.ForMember(dest => dest.{rel.RelatedEntity}{displayedPropertyPlural}, opt => opt.MapFrom(src => src.{entityRelatedPlural}.Select(x => x.{rel.DisplayedProperty})))");
                            relationMapp.AppendLine($"\t\t\t\t.ForMember(dest => dest.{rel.RelatedEntity}Ids, opt => opt.MapFrom(src => src.{entityRelatedPlural}.Select(x => x.Id)))");
                        }
                        break;
                    default:
                        break;
                }
            }

            string? bulkDto = parentEntityName == null ? null : $@"

    public class {entityName}BulkDto
    {{
        public List<GetBulk{entityPlural}Dto> {entityPlural} {{ get; set; }} = new List<GetBulk{entityPlural}Dto>();
        public Guid {parentEntityName}Id {{ get; set; }}

    }}";
            string content = $@"
using Domain.Entities;
using Domain.Enums;

namespace Application.{entityPlural}.Queries.{namespaceName}
{{
    public class {className} : {entityName}BaseDto
    {{ 
{string.Join(Environment.NewLine, propList)}
        public class Mapping : Profile
        {{ 
            public Mapping()
            {{
                CreateMap<{entityName}, {className}>()
                {mapperEnum}
                {mapperAssets}
                {relationMapp}
                ;
            }}
        }}
    }}
{bulkDto}
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
            string? identityUsing = relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable || r.Type == RelationType.UserMany) ? "using Application.Common.Interfaces.Identity;" : null;

            StringBuilder filtersProps = new StringBuilder();
            List<string> filtersList = new List<string>();
            StringBuilder filters = new StringBuilder();
            foreach (var relation in relations)
            {
                if (relation.Type == RelationType.OneToOneSelfJoin)
                {
                    filtersProps.Append($"public Guid? {relation.RelatedEntity}ParentId {{get; set; }}\n");
                    filtersList.Add($"{relation.RelatedEntity}ParentId");
                }

                if (relation.Type == RelationType.OneToOne || relation.Type == RelationType.OneToOneNullable ||
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

            StringBuilder UserRelationCode = new StringBuilder();
            string? injectCTORIdentity1 = null;
            string? injectCTORIdentity2 = null;
            string? injectCTORIdentity3 = null;
            string? ifUserFilters = null;
            StringBuilder forUsersFilters = new StringBuilder();
            string applyFilterParam = !relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable || r.Type == RelationType.UserMany) ? "request.Filters" : "FiltersListWithoutUsers";
            string sortParam = !relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable || r.Type == RelationType.UserMany) ? "request.Filters" : "newSort";

            if (relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable || r.Type == RelationType.UserMany))
            {
                if (relations.Any(r => r.Type == RelationType.UserMany))
                {
                    injectCTORIdentity1 = $",IIdentityService identityService,I{entityName}{relations.First(re => re.Type == RelationType.UserMany).DisplayedProperty}Repository {entityName.GetCamelCaseName()}{relations.First(re => re.Type == RelationType.UserMany).DisplayedProperty}Repository";
                    injectCTORIdentity2 = $"_identityService = identityService; _{entityName.GetCamelCaseName()}{relations.First(re => re.Type == RelationType.UserMany).DisplayedProperty}Repository = {entityName.GetCamelCaseName()}{relations.First(re => re.Type == RelationType.UserMany).DisplayedProperty}Repository;";
                    injectCTORIdentity3 = $"private readonly IIdentityService _identityService; private readonly I{entityName}{relations.First(re => re.Type == RelationType.UserMany).DisplayedProperty}Repository _{entityName.GetCamelCaseName()}{relations.First(re => re.Type == RelationType.UserMany).DisplayedProperty}Repository;";
                }
                else
                {
                    injectCTORIdentity1 = ",IIdentityService identityService";
                    injectCTORIdentity2 = "_identityService = identityService;";
                    injectCTORIdentity3 = "private readonly IIdentityService _identityService;";
                }
            }
            if (relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable || r.Type == RelationType.UserMany))
            {

                foreach (var rel in relations)
                {
                    string code = null!;
                    switch (rel.Type)
                    {
                        case RelationType.UserSingle:
                            code = $@"
                var {rel.DisplayedProperty.GetCamelCaseName()} = await _identityService.GetUserByIdAsync(item.{rel.DisplayedProperty}Id);
                item.{rel.DisplayedProperty}Name = {rel.DisplayedProperty.GetCamelCaseName()}.FullName;";
                            UserRelationCode.Append(code);

                            if (ifUserFilters == null)
                                ifUserFilters = $"item.FieldName == \"{rel.DisplayedProperty.GetCamelCaseName()}Name\"";
                            else
                                ifUserFilters += $" || item.FieldName == \"{rel.DisplayedProperty.GetCamelCaseName()}Name\"";

                            forUsersFilters.Append($@"
                    if (item.FieldName == ""{rel.DisplayedProperty.GetCamelCaseName()}Name"")
                    {{
                        var userIds = helper.ApplyUser(item.Operation,item.Value).GetAwaiter().GetResult();
                        query = query.Where(x => userIds.Contains(x.{rel.DisplayedProperty}Id));
                    }}");
                            break;

                        case RelationType.UserSingleNullable:
                            code = $@"
                if(item.{rel.DisplayedProperty}Id != null)
                {{
                    var {rel.DisplayedProperty.GetCamelCaseName()} = await _identityService.GetUserByIdAsync(item.{rel.DisplayedProperty}Id);
                    item.{rel.DisplayedProperty}Name = {rel.DisplayedProperty.GetCamelCaseName()}.FullName;
                }}";
                            UserRelationCode.Append(code);

                            if (ifUserFilters == null)
                                ifUserFilters = $"item.FieldName == \"{rel.DisplayedProperty.GetCamelCaseName()}Name\"";
                            else
                                ifUserFilters += $" || item.FieldName == \"{rel.DisplayedProperty.GetCamelCaseName()}Name\"";

                            forUsersFilters.Append($@"
                    if (item.FieldName == ""{rel.DisplayedProperty.GetCamelCaseName()}Name"")
                    {{
                        var userIds = helper.ApplyUser(item.Operation,item.Value).GetAwaiter().GetResult();
                        query = query.Where(x => userIds.Contains(x.{rel.DisplayedProperty}Id));
                    }}");
                            break;

                        case RelationType.UserMany:
                            code = $@"
                //Fill {rel.DisplayedProperty.GetPluralName()}
                var usersIds = await _{entityName.GetCamelCaseName()}{relations.First(re => re.Type == RelationType.UserMany).DisplayedProperty}Repository.GetAll()
                        .Where(x => x.{entityName}Id == item.Id)
                        .Select(x => x.UserId)
                        .ToListAsync();

                var users = await _identityService.GetAllUsers()
                    .Where(u => usersIds.Contains(u.Id))
                    .ToListAsync();
                foreach (var user in users)
                {{
                    item.{rel.DisplayedProperty.GetPluralName()}Ids.Add(user.Id);
                    item.{rel.DisplayedProperty.GetPluralName()}Names.Add(user.FullName);
                }}";
                            UserRelationCode.Append(code);

                            if (ifUserFilters == null)
                                ifUserFilters = $"item.FieldName == \"{rel.DisplayedProperty.GetCamelCaseName()}Names\"";
                            else
                                ifUserFilters += $" || item.FieldName == \"{rel.DisplayedProperty.GetCamelCaseName().GetPluralName()}Names\"";

                            forUsersFilters.Append($@"
                    if (item.FieldName == ""{rel.DisplayedProperty.GetCamelCaseName().GetPluralName()}Names"")
                    {{
                        var userIds = helper.ApplyUser(item.Operation,item.Value).GetAwaiter().GetResult();
                        query = query.Where(x => x.{entityName}{rel.DisplayedProperty.GetPluralName()}.Any(e => userIds.Contains(e.UserId)));
                    }}");
                            break;
                        default:
                            break;
                    }
                }
            }
            string? filterUsersCode = !relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable || r.Type == RelationType.UserMany) ? null :
                $@"
                List<FilterCriteria> FiltersListWithoutUsers = new List<FilterCriteria>();
                List<FilterCriteria> FiltersListUsers = new List<FilterCriteria>();
                request.Filters.ForEach(FiltersListWithoutUsers.Add);
                foreach (var item in request.Filters)
                {{
                    if ({ifUserFilters})
                    {{
                        FiltersListWithoutUsers.RemoveAll(f => f.FieldName == item.FieldName);
                        FiltersListUsers.Add(item);
                    }}
                }}
                UserMethodsHelper helper = new UserMethodsHelper(_identityService);
                foreach (var item in FiltersListUsers)
                {{
                    {forUsersFilters.ToString().TrimEnd()}
                }}";
            foreach (var prop in filtersList)
            {
                filters.Append($@"
            if (request.{prop} != null)
                query = query
                    .Where(x => x.{prop} == request.{prop});
");
            }


            string? forUserRelationCode = relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable || r.Type == RelationType.UserMany) ? $@"
            foreach (var item in result.Items)
            {{
                {UserRelationCode}
            }}"
            : null;
            bool getMethodCondition = relations.Any() && !(relations.Count == 1 && relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable));
            string GetAllMethod = getMethodCondition ? $"Get{entityPlural}()" : "GetAll()";
            string content = $@"
using Microsoft.Extensions.Logging;
using Application.Common.Interfaces.IRepositories;
using Application.Common.Models;
using Application.Utilities;
using AutoMapper.QueryableExtensions;
using Application.Common.Mappings;
using Application.Common.Interfaces.Services;
using Application.Common.Extensions;
{identityUsing}

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
        {injectCTORIdentity3}

        public Get{entityPlural}WithPaginationQueryHandler(ILogger<Get{entityPlural}WithPaginationQueryHandler> logger,
                                                           IMapper mapper,
                                                           ILocalizationService localizationService,
                                                           I{entityName}Repository repository{injectCTORIdentity1})
        {{
            _logger = logger;
            _mapper = mapper;
            _localizationService = localizationService;
            _{lowerEntityName}Repository = repository;
            {injectCTORIdentity2}
        }}

        public async Task<PaginatedList<Get{entityPlural}WithPaginationDto>> Handle(Get{entityPlural}WithPaginationQuery request, CancellationToken cancellationToken)
        {{
            var query = _{lowerEntityName}Repository.{GetAllMethod};

            //if (!string.IsNullOrWhiteSpace(request.SearchText)) //TODO:AfterGenerateCode: replace Name with proper property to apply SearchText filter
            //    query = query.Where(x => x.Name.ToLower().Contains(request.SearchText.ToLower()));

            {filters}
            {filterUsersCode}
            var result = await query
                .ProjectTo<Get{entityPlural}WithPaginationDto>(_mapper.ConfigurationProvider)
                .ApplyFilters({applyFilterParam})
                .OrderBy(request.Sort)
                .PaginatedListAsync(request.PageNumber, request.PageSize);

            {localizationCode}
            {forUserRelationCode}
            return result;
        }}
    }}
}}";
            File.WriteAllText(filePath, content);
        }
        static void GenerateGetBulkQueryFile(string entityName, string entityPlural, string path, bool hasLocalization, List<Relation> relations, bool hasUserAction, string? parentEntityName)
        {
            string fileName = $"GetBulk{entityPlural}Query.cs";
            string filePath = Path.Combine(path, fileName);

            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            //string? localizationCode = !hasLocalization ? null : $@"
            //if (request.LanguageId != null) 
            //    await _localizationService.Fill{entityName}Localization(result, request.LanguageId.Value);";
            string? identityUsing = relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable || r.Type == RelationType.UserMany) ? "using Application.Common.Interfaces.Identity;" : null;

            StringBuilder UserRelationCode = new StringBuilder();
            string? injectCTORIdentity1 = null;
            string? injectCTORIdentity2 = null;
            string? injectCTORIdentity3 = null;

            if (relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable || r.Type == RelationType.UserMany))
            {
                if (relations.Any(r => r.Type == RelationType.UserMany))
                {
                    injectCTORIdentity1 = $",IIdentityService identityService,I{entityName}{relations.First(re => re.Type == RelationType.UserMany).DisplayedProperty}Repository {entityName.GetCamelCaseName()}{relations.First(re => re.Type == RelationType.UserMany).DisplayedProperty}Repository";
                    injectCTORIdentity2 = $"_identityService = identityService; _{entityName.GetCamelCaseName()}{relations.First(re => re.Type == RelationType.UserMany).DisplayedProperty}Repository = {entityName.GetCamelCaseName()}{relations.First(re => re.Type == RelationType.UserMany).DisplayedProperty}Repository;";
                    injectCTORIdentity3 = $"private readonly IIdentityService _identityService; private readonly I{entityName}{relations.First(re => re.Type == RelationType.UserMany).DisplayedProperty}Repository _{entityName.GetCamelCaseName()}{relations.First(re => re.Type == RelationType.UserMany).DisplayedProperty}Repository;";
                }
                else
                {
                    injectCTORIdentity1 = ",IIdentityService identityService";
                    injectCTORIdentity2 = "_identityService = identityService;";
                    injectCTORIdentity3 = "private readonly IIdentityService _identityService;";
                }
            }
            if (relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable || r.Type == RelationType.UserMany))
            {

                foreach (var rel in relations)
                {
                    string code = null!;
                    switch (rel.Type)
                    {
                        case RelationType.UserSingle:
                            code = $@"
                var {rel.DisplayedProperty.GetCamelCaseName()} = await _identityService.GetUserByIdAsync(item.{rel.DisplayedProperty}Id);
                item.{rel.DisplayedProperty}Name = {rel.DisplayedProperty.GetCamelCaseName()}.FullName;";
                            UserRelationCode.Append(code);

                            break;

                        case RelationType.UserSingleNullable:
                            code = $@"
                if(item.{rel.DisplayedProperty}Id != null)
                {{
                    var {rel.DisplayedProperty.GetCamelCaseName()} = await _identityService.GetUserByIdAsync(item.{rel.DisplayedProperty}Id);
                    item.{rel.DisplayedProperty}Name = {rel.DisplayedProperty.GetCamelCaseName()}.FullName;
                }}";
                            UserRelationCode.Append(code);

                            break;

                        case RelationType.UserMany:
                            code = $@"
                //Fill {rel.DisplayedProperty.GetPluralName()}
                var usersIds = await _{entityName.GetCamelCaseName()}{relations.First(re => re.Type == RelationType.UserMany).DisplayedProperty}Repository.GetAll()
                        .Where(x => x.{entityName}Id == item.Id)
                        .Select(x => x.UserId)
                        .ToListAsync();

                var users = await _identityService.GetAllUsers()
                    .Where(u => usersIds.Contains(u.Id))
                    .ToListAsync();
                foreach (var user in users)
                {{
                    item.{rel.DisplayedProperty.GetPluralName()}Ids.Add(user.Id);
                    item.{rel.DisplayedProperty.GetPluralName()}Names.Add(user.FullName);
                }}";
                            UserRelationCode.Append(code);

                            break;
                        default:
                            break;
                    }
                }
            }
            string? forUserRelationCode = relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable || r.Type == RelationType.UserMany) ? $@"
            foreach (var item in result)
            {{
                {UserRelationCode}
            }}"
            : null;

            bool getMethodCondition = relations.Any() && !(relations.Count == 1 && relations.Any(r => r.Type == RelationType.UserSingle || r.Type == RelationType.UserSingleNullable));
            string GetAllMethod = getMethodCondition ? $"Get{entityPlural}()" : "GetAll()";

            string? injectCTORMediateR1 = hasUserAction ? ",IMediator mediator" : null;
            string? injectCTORMediateR2 = hasUserAction ? "_mediator = mediator;" : null;
            string? injectCTORMediateR3 = hasUserAction ? "private readonly IMediator _mediator;" : null;
            string? usingEvent = hasUserAction ? $"using Domain.Events.{entityName}Events;" : null;
            string? getEventCode = !hasUserAction ? null : $@"
            {entityName}GetBulkEvent getEvent = new {entityName}GetBulkEvent(request.{parentEntityName}Id);
            await _mediator.Publish(getEvent);";
            string content = $@"
using Microsoft.Extensions.Logging;
using Application.Common.Interfaces.IRepositories;
using Application.Common.Models;
using Application.Utilities;
using AutoMapper.QueryableExtensions;
using Application.Common.Mappings;
using Application.Common.Interfaces.Services;
using Application.Common.Extensions;
{identityUsing}
{usingEvent}

namespace Application.{entityPlural}.Queries.GetBulk{entityPlural}
{{
    public class GetBulk{entityPlural}Query : IRequest<{entityName}BulkDto>
    {{
        public Guid {parentEntityName}Id {{ get; set; }}
    }}

    public class GetBulk{entityPlural}QueryHandler : IRequestHandler<GetBulk{entityPlural}Query, {entityName}BulkDto>
    {{
        private readonly ILogger<GetBulk{entityPlural}QueryHandler> _logger;
        private readonly IMapper _mapper;
        private readonly I{entityName}Repository _{lowerEntityName}Repository;
        private readonly ILocalizationService _localizationService;
        {injectCTORIdentity3}
        {injectCTORMediateR3}

        public GetBulk{entityPlural}QueryHandler(ILogger<GetBulk{entityPlural}QueryHandler> logger,
                                                           IMapper mapper,
                                                           ILocalizationService localizationService,
                                                           I{entityName}Repository repository{injectCTORIdentity1}{injectCTORMediateR1})
        {{
            _logger = logger;
            _mapper = mapper;
            _localizationService = localizationService;
            _{lowerEntityName}Repository = repository;
            {injectCTORIdentity2}
            {injectCTORMediateR2}
        }}

        public async Task<{entityName}BulkDto> Handle(GetBulk{entityPlural}Query request, CancellationToken cancellationToken)
        {{
            var query = _{lowerEntityName}Repository.{GetAllMethod}.Where(x => x.{parentEntityName}Id == request.{parentEntityName}Id);

            var result = await query
                .ProjectTo<GetBulk{entityPlural}Dto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            {forUserRelationCode}
            var res = new {entityName}BulkDto();
            res.{parentEntityName}Id = request.{parentEntityName}Id;
            res.{entityPlural} = result;
            {getEventCode}
            return res;
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
        ////

        public static void GenerateBaseDto(string entityName, string entityPlural, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, string solutionDir, List<Relation> relations, bool hasLocalization, bool bulk, string? parentEntityName = null)
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
                        relationsProps.Add($"\t\tpublic Guid? {relation.RelatedEntity}ParentId {{  get; set; }}\n");
                        relationsProps.Add($"\t\tpublic string? {relation.RelatedEntity}Parent{relation.DisplayedProperty} {{  get; set; }}\n");
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
                        if (parentEntityName != null)
                        {
                            if (!bulk)
                            {
                                relationsProps.Add($"\t\tpublic List<Guid> {relation.RelatedEntity}Ids {{  get; set; }}\n");
                                relationsProps.Add($"\t\tpublic List<string> {relation.RelatedEntity}{displayedPropertyPlural} {{  get; set; }}\n");
                            }
                            else
                            {
                                relationsProps.Add($"\t\tpublic List<Guid> {relation.RelatedEntity.GetPluralName()}Ids {{  get; set; }}\n");
                                relationsProps.Add($"\t\tpublic List<string> {relation.RelatedEntity.GetPluralName()}{displayedPropertyPlural} {{  get; set; }}\n");
                            }
                        }
                        else
                        {
                            relationsProps.Add($"\t\tpublic List<Guid> {relation.RelatedEntity}Ids {{  get; set; }}\n");
                            relationsProps.Add($"\t\tpublic List<string> {relation.RelatedEntity}{displayedPropertyPlural} {{  get; set; }}\n");
                        }
                        break;

                    case RelationType.UserSingle:
                        relationsProps.Add($"\t\tpublic string {relation.DisplayedProperty}Id {{  get; set; }}\n");
                        relationsProps.Add($"\t\tpublic string {relation.DisplayedProperty}Name {{  get; set; }}\n");
                        break;
                    case RelationType.UserSingleNullable:
                        relationsProps.Add($"\t\tpublic string? {relation.DisplayedProperty}Id {{  get; set; }}\n");
                        relationsProps.Add($"\t\tpublic string? {relation.DisplayedProperty}Name {{  get; set; }}\n");
                        break;

                    case RelationType.UserMany:
                        relationsProps.Add($"\t\tpublic List<string> {relation.DisplayedProperty.GetPluralName()}Ids {{  get; set; }} = new List<string>();\n");
                        relationsProps.Add($"\t\tpublic List<string> {relation.DisplayedProperty.GetPluralName()}Names {{  get; set; }} = new List<string>();\n");

                        break;
                    default:
                        break;
                }
            }

            if (parentEntityName != null)
            {
                relationsProps.Add($"\t\tpublic Guid {parentEntityName}Id {{  get; set; }}\n");
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
                        propList.Add($"\t\tpublic string {prop.Name}Url {{ get; set; }}");
                    }
                    else
                    {
                        propList.Add($"\t\tpublic string? {prop.Name}Url {{ get; set; }}");
                    }
                }
                else if (prop.Type == "PNGs")
                {
                    propList.Add($"\t\tpublic List<string> {prop.Name}Urls {{ get; set; }} = new List<string>();");
                }
                else if (prop.Type == "VD")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        propList.Add($"\t\tpublic string {prop.Name}Url {{ get; set; }}");
                    }
                    else
                    {
                        propList.Add($"\t\tpublic string? {prop.Name}Url {{ get; set; }}");
                    }
                }
                else if (prop.Type == "VDs")
                {
                    propList.Add($"\t\tpublic List<string> {prop.Name}Urls {{ get; set; }} = new List<string>();");
                }
                else if (prop.Type == "FL")
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        propList.Add($"\t\tpublic string {prop.Name}Url {{ get; set; }}");
                    }
                    else
                    {
                        propList.Add($"\t\tpublic string? {prop.Name}Url {{ get; set; }}");
                    }
                }
                else if (prop.Type == "FLs")
                {
                    propList.Add($"\t\tpublic List<string> {prop.Name}Urls {{ get; set; }} = new List<string>();");
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
            else if (property.Type == "VDs")
            {
                rules.AppendLine($"            RuleFor(x => x.{property.Name}Files)");
            }
            else if (property.Type == "FL")
            {
                rules.AppendLine($"            RuleFor(x => x.{property.Name}File)");
            }
            else if (property.Type == "FLs")
            {
                rules.AppendLine($"            RuleFor(x => x.{property.Name}Files)");
            }
            else
                rules.AppendLine($"            RuleFor(x => x.{property.Name})");

            // Handle required validation differently based on type
            if (property.Validation.Required)
            {
                //rules.Append(".");

                // Use NotNull() for numeric types, NotEmpty() for strings
                if (property.Type == "int" || property.Type == "decimal" || property.Type == "float" || property.Type == "double" || property.Type == "GPG" || property.Type == "VD" || property.Type == "FL")
                {
                    rules.AppendLine($"\t\t\t\t.NotNull().WithMessage(\"{property.Name} is required.\")");
                }
                else if (property.Type == "string" || property.Type == "PNGs" || property.Type == "VDs" || property.Type == "FLs" || property.Type.Contains("List<") || property.Type.Contains("Date") || property.Type.Contains("Time"))
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
       
        static string GenerateRelationRules(List<Relation> relations, string entityName)
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

                string ruleUserSingleNotNullable = $@" 
            RuleFor(l => l.{relation.DisplayedProperty}Id)
                .NotEmpty().WithMessage(""{relation.DisplayedProperty}Id Must be passed"")
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                   if (!await Is{relation.DisplayedProperty}Existed(context.InstanceToValidate))
                   {{
                       context.AddFailure(""{relation.DisplayedProperty} is not found"");
                   }}
                }});";

                string ruleUserSingleNullable = $@" 
            RuleFor(l => l.{relation.DisplayedProperty}Id)
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                   if (!await Is{relation.DisplayedProperty}Existed(context.InstanceToValidate))
                   {{
                       context.AddFailure(""{relation.DisplayedProperty} is not found"");
                   }}
                }});";

                string ruleUserMany = $@" 
            RuleFor(l => l.{relation.DisplayedProperty.GetPluralName()}Ids)
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                   if (!await AreMany{relation.DisplayedProperty.GetPluralName()}Existed(context.InstanceToValidate))
                   {{
                       context.AddFailure("" Some {relation.DisplayedProperty.GetPluralName()} are not found"");
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
                    case RelationType.UserSingle:
                        rules.AppendLine(ruleUserSingleNotNullable);
                        rules.AppendLine();
                        break;
                    case RelationType.UserSingleNullable:
                        rules.AppendLine(ruleUserSingleNullable);
                        rules.AppendLine();
                        break;
                    case RelationType.UserMany:
                        rules.AppendLine(ruleUserMany);
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

                string methodUserSingleNotNullable = $@" 
        public async Task<bool> Is{relation.DisplayedProperty}Existed({commandName} command)
        {{
            var user = await _identityService.GetUserByIdAsync(command.{relation.DisplayedProperty}Id);
            if (user == null) 
                return false;
        
            return true;
        }}";

                string methodUserSingleNullable = $@" 
        public async Task<bool> Is{relation.DisplayedProperty}Existed({commandName} command)
        {{
            if (command.{relation.DisplayedProperty}Id is null)
                return true;

            var user = await _identityService.GetUserByIdAsync(command.{relation.DisplayedProperty}Id);
            if (user == null) 
                return false;
        
            return true;
        }}";
                string methodUserMany = $@" 
        public async Task<bool> AreMany{relation.DisplayedProperty.GetPluralName()}Existed({commandName} command)
        {{
            if (!command.{relation.DisplayedProperty.GetPluralName()}Ids.Any())
                return true;

            foreach (var id in command.{relation.DisplayedProperty.GetPluralName()}Ids)
            {{
                var user = await _identityService.GetUserByIdAsync(id);

                if (user == null)
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
                    case RelationType.UserSingle:
                        methods.AppendLine(methodUserSingleNotNullable);
                        methods.AppendLine();
                        break;
                    case RelationType.UserSingleNullable:
                        methods.AppendLine(methodUserSingleNullable);
                        methods.AppendLine();
                        break;
                    case RelationType.UserMany:
                        methods.AppendLine(methodUserMany);
                        methods.AppendLine();
                        break;
                    default:
                        break;
                }
            }
            return methods.ToString();
        }

        static string? GenerateRelationDeleteMethod(List<Relation> relations, string entityName)
        {
            string entityNameLower = char.ToLower(entityName[0]) + entityName.Substring(1);
            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";
            string? resSelfJoin = null;
            string? resOneToMany = null;
            if (!relations.Any(r => r.Type == RelationType.OneToMany || r.Type == RelationType.OneToManyNullable || r.Type == RelationType.OneToOneSelfJoin))
            {
                return null;
            }
            foreach (var relation in relations)
            {
                string x = relation.RelatedEntity;
                string relatedEntityName = char.ToLower(x[0]) + x.Substring(1);
                string relatedEntityRepoName = $"_{relatedEntityName}Repository";

                if (relation.Type == RelationType.OneToOneSelfJoin)
                {
                    resSelfJoin = $@"
            var entities = _{entityNameLower}Repository.Get{entityPlural}();
            var resSelf = await entities.AnyAsync(x => x.{entityName}ParentId == command.{entityName}Id);
            if (resSelf)
            {{
                return false;
            }}
";
                }
                if (relation.Type == RelationType.OneToMany || relation.Type == RelationType.OneToManyNullable)
                {
                    string relatedEntityPlural = relation.RelatedEntity.EndsWith("y") ? relation.RelatedEntity[..^1] + "ies" : relation.RelatedEntity + "s";
                    resOneToMany = $@"
            var obj = await _{entityName}Repository.Get{entityName}(command.{entityName}Id);
            if (obj.{relatedEntityPlural}.Count > 0)
            {{
                return false;
            }}
";
                }
            }
            string deletedMethod = $@"
        public async Task<bool> CanDeleteEntity(Delete{entityName}Command command)
        {{
{resSelfJoin}
{resOneToMany}
            return true;
        }}";
            return deletedMethod;
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
                else if (prop.Type == "VD")
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
        public static void GenerateDeleteBulkCommand(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, bool hasLocalization, List<Relation> relations, bool hasVersioning, bool hasNotification, bool hasUserAction)

        {
            string className = $"DeleteBulk{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            string lowerEntityPlural = char.ToLower(entityPlural[0]) + entityPlural.Substring(1);
            string entityRepoName = $"_{lowerEntityName}";

            string? deletedImagesDeclaration = hasVersioning ? null : !properties.Any(p => p.Type == "GPG" || p.Type == "PNGs" || p.Type == "VD") ? null : "List<string> deletedImages = new List<string>();";
            string? deleteOldImageCode = hasVersioning ? null : !properties.Any(p => p.Type == "GPG" || p.Type == "PNGs" || p.Type == "VD") ? null : $@"
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
            string? imageCode = hasVersioning ? null : !properties.Any(p => p.Type == "GPG" || p.Type == "PNGs" || p.Type == "VD") ? null : $@"
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



        public static void UpdateProfileQuery(string entityName, string domainPath)
        {
            string profileQueryPath = Path.Combine(domainPath, "..", "..", "Application", "Identity", "Queries", "GetProfile", "GetProfileQuery.cs");
            if (!File.Exists(profileQueryPath))
            {
                //Console.WriteLine("⚠️ RoleConsistent.cs not found.");
                return;
            }
            string entityBasePermissions = $@"
            EntityBasePermissions {entityName.GetCamelCaseName()} = new EntityBasePermissions
            {{
                EntityName = ""{entityName}"",
                Add = true,
                Delete = true,
                View = true,
                Edit = true,
                Browse = true
            }};
" + "\n\t\t\t//Add No Permission Entity Here";

            var lines = File.ReadAllLines(profileQueryPath).ToList();
            var index = lines.FindIndex(line => line.Contains("//Add No Permission Entity Here"));

            if (index >= 0)
            {
                lines[index] = entityBasePermissions;
                File.WriteAllLines(profileQueryPath, lines);
            }

            lines.Clear();
            index = -1;

            string profileAddPermissions = $"\t\t\tprofile.Permissions.Add({entityName.GetCamelCaseName()});" + "\n\t\t\t//Add To Permissions Here";

            lines = File.ReadAllLines(profileQueryPath).ToList();
            index = lines.FindIndex(line => line.Contains("//Add To Permissions Here"));

            if (index >= 0)
            {
                lines[index] = profileAddPermissions;
                File.WriteAllLines(profileQueryPath, lines);
            }

        }
    }
}
