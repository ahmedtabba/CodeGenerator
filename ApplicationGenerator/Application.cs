using SharedClasses;
using System.Data;
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
        public static void GenerateCreateCommand(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties,bool hasLocalization,List<Relation> relations, bool hasVersioning,bool hasNotification, bool hasUserAction)
        {
            string className = $"Create{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            string entityRepoName = $"_{lowerEntityName}";
            var inheritVersion = (hasVersioning || hasNotification || hasUserAction) ? "VersionRequestOfTBase," : null;
            var neededUsing = (hasVersioning || hasNotification || hasUserAction) ? $"using Domain.Events.{entityName}Events;using Application.Common.Models.Versioning;" : null;
            var eventCode = !(hasVersioning || hasNotification || hasUserAction) ? null :
                $@"
                var {lowerEntityName}Event = new {entityName}CreatedEvent({entityName.ToLower()});
                {lowerEntityName}Event.RollbackedToVersionId = request.RollbackedToVersionId;
                {lowerEntityName}Event.IsVersionedCommand = request.IsVersionedCommand;
                {entityName.ToLower()}.AddDomainEvent({lowerEntityName}Event);
";

            var propList = new List<string>();
            StringBuilder imageCode = new StringBuilder();
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
                    propList.Add($"\t\tpublic string? {prop.Name} {{ get; set; }}");
                }
                else
                {
                    propList.Add($"\t\tpublic {prop.Type} {prop.Name} {{ get; set; }}");
                }

            }
            var props = string.Join(Environment.NewLine,propList);
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
                    default:
                        break;
                }
            }

            string relationProps = string.Join(Environment.NewLine, relationPropsList);
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
                    CreateMap<Create{entityName}Command, {entityName}>();
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

        public {className}Handler(ILogger<{className}Handler> logger,
                                        IMapper mapper,
                                        IUnitOfWorkAsync unitOfWork,
                                        IFileService fileService,
                                        I{entityName}Repository repository{localizationIRepo})
        {{
            {entityRepoName}Repository = repository;
            _mapper=mapper;
            _unitOfWork=unitOfWork;
            _logger=logger;
            _fileService = fileService;
            {localizationInjectIRepo}
        }}
    
        public async Task<string> Handle({className} request, CancellationToken cancellationToken)
        {{
          try
            {{
                await _unitOfWork.BeginTransactionAsync();
                var {entityName.ToLower()} = _mapper.Map<{entityName}>(request);
                await {entityRepoName}Repository.AddAsync({entityName.ToLower()});
                {eventCode}
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                {localizationCode}
                {imageCode}
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
            //string className = $"Create{entityName}CommandValidator";
            //string commandName = $"Create{entityName}Command";
            //string filePath = Path.Combine(path, $"{className}.cs");

            //string rules = string.Join(Environment.NewLine, properties
            //    .Where(p => p.Type == "string")
            //    .Select(p => $"            RuleFor(x => x.{p.Name}).NotEmpty().WithMessage(\"{p.Name} is required.\");"));

            //string content = $@"using FluentValidation;

            //                namespace Application.{entityPlural}.Commands.Create{entityName}
            //                {{
            //                    public class {className} : AbstractValidator<{commandName}>
            //                    {{
            //                        public {className}()
            //                        {{
            //                         {rules}
            //                        }}
            //                    }}
            //                }}";
            //File.WriteAllText(filePath, content);
            string className = $"Create{entityName}CommandValidator";
            string commandName = $"Create{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            string? methodsUnique = string.Empty;
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
                        injectCTOR2.AppendLine($"_{char.ToLower(entityName[0]) + entityName.Substring(1)}Repository = {char.ToLower(entityName[0]) + entityName.Substring(1)}Repository;");
                        injectCTOR3.AppendLine($"private readonly I{entityName}Repository _{char.ToLower(entityName[0]) + entityName.Substring(1)}Repository;");
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
        public static void GenerateUpdateCommand(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, bool hasLocalization ,List<Relation> relations, bool hasVersioning, bool hasNotification, bool hasUserAction)
        {
            string className = $"Update{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            string entityRepoName = $"_{lowerEntityName}";

            var inheritVersion = (hasVersioning || hasUserAction || hasNotification) ? "VersionRequestBase," : null;
            var neededUsing = (hasVersioning || hasUserAction || hasNotification) ? $"using Domain.Events.{entityName}Events;using Application.Common.Models.Versioning;" : null;
            var deepCopyCode = (hasVersioning || hasUserAction || hasNotification) ? $"var old{entityName} = existingObj.DeepCopyJsonDotNet();" : null;
            var eventCode = !(hasVersioning || hasNotification || hasUserAction) ? null :
                $@"
                var {lowerEntityName}Event = new {entityName}EditedEvent(old{entityName},existingObj);
                {lowerEntityName}Event.RollbackedToVersionId = request.RollbackedToVersionId;
                {lowerEntityName}Event.IsVersionedCommand = request.IsVersionedCommand;
                existingObj.AddDomainEvent({lowerEntityName}Event);
";

            var propList = new List<string>();
            StringBuilder imageCode = new StringBuilder();
            string? oldImageUrl = string.Empty;
            StringBuilder oldImageToDeleteCode = new StringBuilder();
            StringBuilder oldImagesToDeleteCode = new StringBuilder();

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
                    propList.Add($"\t\tpublic string? {prop.Name} {{ get; set; }}");
                }
                else
                {
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
                    default:
                        break;
                }
            }
            string relationProps = string.Join(Environment.NewLine, relationPropsList);
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
using Application.Common.Models.Localization;
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
                CreateMap<{className}, {entityName}>();
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

        public {className}Handler(ILogger<{className}Handler> logger,
                                            IMapper mapper,
                                            IUnitOfWorkAsync unitOfWork,
                                            IFileService fileService,
                                            I{entityName}Repository repository{localizationIRepo})
                                            
        {{
            _logger = logger;
            _mapper = mapper;
            {entityRepoName}Repository = repository;
            _fileService = fileService;
            _unitOfWork = unitOfWork;
            {localizationInjectIRepo}
        }}
        public async Task Handle({className} request, CancellationToken cancellationToken)
        {{
            try
            {{
                await _unitOfWork.BeginTransactionAsync();
                var existingObj = await {entityRepoName}Repository.GetByIdAsync(request.{entityName}Id);
                {deepCopyCode}
                {oldImageUrl}
                _mapper.Map(request, existingObj);
                {localizationCode}

                {imageCode}

                {eventCode}

                await {entityRepoName}Repository.UpdateAsync(existingObj);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _unitOfWork.CommitAsync();

                {oldImageToDeleteCode}
                
                {oldImagesToDeleteCode}
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
        public static void GenerateUpdateCommandValidator(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<Relation> relations)
        {
            string className = $"Update{entityName}CommandValidator";
            string commandName = $"Update{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            string? methodsUnique = string.Empty;
            StringBuilder injectCTOR1 = new($"ILogger<{className}> logger,I{entityName}Repository {char.ToLower(entityName[0]) + entityName.Substring(1)}Repository");
            foreach (var relation in relations)
            {
                if (relation.Type == RelationType.OneToOneSelfJoin)
                    continue;
                injectCTOR1.Append($",I{relation.RelatedEntity}Repository {char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository");
            }
            StringBuilder injectCTOR2 = new StringBuilder($"\t\t\t_logger = logger;_{char.ToLower(entityName[0]) + entityName.Substring(1)}Repository = {char.ToLower(entityName[0]) + entityName.Substring(1)}Repository;");
            foreach (var relation in relations)
            {
                if (relation.Type == RelationType.OneToOneSelfJoin)
                    continue;
                injectCTOR2.AppendLine($"_{char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository = {char.ToLower(relation.RelatedEntity[0]) + relation.RelatedEntity.Substring(1)}Repository;");
            }
            StringBuilder injectCTOR3 = new($"\t\tprivate readonly ILogger<{className}> _logger; private readonly I{entityName}Repository _{char.ToLower(entityName[0]) + entityName.Substring(1)}Repository;");
            foreach (var relation in relations)
            {
                if (relation.Type == RelationType.OneToOneSelfJoin)
                    continue;
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
        public static void GenerateDeleteCommand(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties,bool hasVersioning, bool hasNotification, bool hasUserAction)
        {
            string className = $"Delete{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");
            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            string entityRepoName = $"_{lowerEntityName}";

            var inheritVersion = (hasVersioning || hasUserAction || hasNotification) ? "VersionRequestBase," : null;
            var neededUsing = (hasVersioning || hasUserAction || hasNotification) ? $"using Domain.Events.{entityName}Events;using Application.Common.Models.Versioning;" : null;
            var eventCode1 = !(hasVersioning || hasUserAction || hasNotification) ? null :
                $@"
                var {lowerEntityName}Event = new {entityName}DeletedEvent({entityName.ToLower()});
                {lowerEntityName}Event.RollbackedToVersionId = request.RollbackedToVersionId;
                {lowerEntityName}Event.IsVersionedCommand = request.IsVersionedCommand;
";
            var eventCode2 = !(hasVersioning || hasUserAction || hasNotification) ? null :
                $@"
                {entityName.ToLower()}.AddDomainEvent({lowerEntityName}Event);
";

            string? deletedImagesVar = properties.Any(p=>(p.Type == "GPG" || p.Type =="PNGs")) ?  $"var deletedImagesPaths = new List<string>();" : null;
            StringBuilder ImageSaveCode = new StringBuilder();
            string? DeleteImagesCode = properties.Any(p => (p.Type == "GPG" || p.Type == "PNGs")) ? $@"
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
                if (prop.Type == "PNGs")
                    ImageSaveCode.Append($@"
                foreach(var path in {entityName.ToLower()}.{prop.Name})
                    deletedImagesPaths.Add(path);

");
            }
            string content = $@"
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces.Db;
using Application.Common.Interfaces.IRepositories;
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
                {ImageSaveCode}
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
        public static void GenerateGetByIdQuery(string entityName, string entityPlural, string path, bool hasLocalization, List<Relation> relations)
        {
            var folderPath = Path.Combine(path, $"Get{entityName}Query");
            Directory.CreateDirectory(folderPath);

            GenerateGetByIdDto(entityName, entityPlural, folderPath,relations);
            GenerateGetByIdQueryFile(entityName, entityPlural, folderPath,hasLocalization);
            GenerateGetByIdValidator(entityName, entityPlural, folderPath);
        }
        public static void GenerateGetByIdDto(string entityName, string entityPlural, string path,List<Relation> relations)
        {
            string fileName = $"Get{entityName}Dto.cs";
            string filePath = Path.Combine(path, fileName);
            string? dtoNavProps = string.Empty;
            foreach( var relation in relations )
            {
                string navigationDtoPath = Path.Combine(path, "..", "..", "..", "Common", "Models", "AssistantModels", $"{relation.RelatedEntity}NavigationDto.cs");
                var navigationDtoContent = $@"using Domain.Entities;
namespace Application.Common.Models.AssistantModels
{{
    public class {relation.RelatedEntity}NavigationDto 
    {{
        public Guid Id {{ get; set; }}
        //Add Props Here

        public class Mapping : Profile
        {{
            public Mapping()
            {{
                CreateMap<{relation.RelatedEntity}, {relation.RelatedEntity}NavigationDto>();
            }}
        }}
    }}
}}
";
                if (File.Exists(navigationDtoPath))
                {
                    continue;
                }
                else
                {
                    string dtoNavProp = null; 
                    switch (relation.Type)
                    {
                        case RelationType.OneToOneSelfJoin:
                            if (!File.Exists(navigationDtoPath))
                                File.WriteAllText(navigationDtoPath, navigationDtoContent);
                            dtoNavProp = $"\t\tpublic {relation.RelatedEntity}NavigationDto? {entityName}Parent {{  get; set; }}";
                            break;
                        case RelationType.OneToOne:
                            if (!File.Exists(navigationDtoPath))
                                File.WriteAllText(navigationDtoPath, navigationDtoContent);
                            dtoNavProp = $"\t\tpublic {relation.RelatedEntity}NavigationDto {relation.RelatedEntity} {{  get; set; }}";
                            break;
                        case RelationType.OneToOneNullable:
                            if (!File.Exists(navigationDtoPath))
                                File.WriteAllText(navigationDtoPath, navigationDtoContent);
                            dtoNavProp = $"\t\tpublic {relation.RelatedEntity}NavigationDto? {relation.RelatedEntity} {{  get; set; }}";
                            break;
                        case RelationType.ManyToOne:
                            if (!File.Exists(navigationDtoPath))
                                File.WriteAllText(navigationDtoPath, navigationDtoContent);
                            dtoNavProp = $"\t\tpublic {relation.RelatedEntity}NavigationDto {relation.RelatedEntity} {{  get; set; }}";
                            break;
                        case RelationType.ManyToOneNullable:
                            if (!File.Exists(navigationDtoPath))
                                File.WriteAllText(navigationDtoPath, navigationDtoContent);
                            dtoNavProp = $"\t\tpublic {relation.RelatedEntity}NavigationDto? {relation.RelatedEntity} {{  get; set; }}";
                            break;
                        default:
                            break;
                    }
                    if (dtoNavProp != null)
                        dtoNavProps += dtoNavProp;
                }
                
            }
            string content = $@"using Domain.Entities;
using Application.Common.Models.AssistantModels;

namespace Application.{entityPlural}.Queries.Get{entityName}Query
{{
    public class Get{entityName}Dto : {entityName}BaseDto
    {{
{dtoNavProps}    
        public class Mapping : Profile
        {{
            public Mapping()
            {{
                CreateMap<{entityName}, Get{entityName}Dto>();
            }}
        }}
    }}
}}";
            File.WriteAllText(filePath, content);
        }
        public static void GenerateGetByIdQueryFile(string entityName, string entityPlural, string path,bool hasLocalization)
        {
            string fileName = $"Get{entityName}Query.cs";
            string filePath = Path.Combine(path, fileName);
            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            string entityRepoName = $"_{lowerEntityName}";
            string? languageIdProp =hasLocalization ?  $"public Guid? LanguageId {{ get; set; }}" : null;
            string content = $@"
using Microsoft.Extensions.Logging;
using Application.Common.Interfaces.IRepositories;

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
            var {entityName.ToLower()} = await {entityRepoName}Repository.GetByIdAsync(request.{entityName}Id);
            var dto = _mapper.Map<Get{entityName}Dto>({entityName.ToLower()});

            //if (request.LanguageId != null) //If entity has localization, create and implement FillLocalization in LocalizationService
            //    await _localizationService.FillGovernorateLocalization(result, request.LanguageId.Value);
            return dto;
        }}
    }}
}}";
            File.WriteAllText(filePath, content);
        }
        public static void GenerateGetByIdValidator(string entityName, string entityPlural, string path)
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
        public static void GenerateGetWithPaginationQuery(string entityName, string entityPlural, string path, bool hasLocalization, List<Relation> relations)
        {
            var folderPath = Path.Combine(path, $"Get{entityPlural}WithPagination");
            Directory.CreateDirectory(folderPath);

            GenerateGetWithPaginationDto(entityName, entityPlural, folderPath);
            GenerateGetWithPaginationQueryFile(entityName, entityPlural, folderPath,hasLocalization,relations);
            GenerateGetWithPaginationValidator(entityName, entityPlural, folderPath);
        }
        public static void GenerateGetWithPaginationDto(string entityName, string entityPlural, string path)
        {
            string fileName = $"Get{entityPlural}WithPaginationDto.cs";
            string filePath = Path.Combine(path, fileName);

            string content = $@"
using Domain.Entities;

namespace Application.{entityPlural}.Queries.Get{entityPlural}WithPagination
{{
    public class Get{entityPlural}WithPaginationDto : {entityName}BaseDto
    {{
        public class Mapping : Profile
        {{
            public Mapping()
            {{
                CreateMap<{entityName}, Get{entityPlural}WithPaginationDto>();
            }}
        }}
    }}
}}";
            File.WriteAllText(filePath, content);
        }
        public static void GenerateGetWithPaginationQueryFile(string entityName, string entityPlural, string path,bool hasLocalization, List<Relation> relations)
        {
            string fileName = $"Get{entityPlural}WithPaginationQuery.cs";
            string filePath = Path.Combine(path, fileName);

            string x = entityName;
            string lowerEntityName = char.ToLower(x[0]) + x.Substring(1);
            string? languageIdProp = hasLocalization ? $"public Guid? LanguageId {{ get; set; }}" : null;
            StringBuilder filtersProps = new StringBuilder();
            List<string> filtersList = new List<string>();
            StringBuilder filters = new StringBuilder();
            foreach (var relation in relations)
            {
                if (relation.Type == RelationType.OneToOneSelfJoin || relation.Type == RelationType.OneToOne || relation.Type == RelationType.OneToOneNullable ||
                    relation.Type == RelationType.ManyToOne || relation.Type == RelationType.ManyToOneNullable)
                {
                    filtersProps.Append($"public Guid? {relation.RelatedEntity}Id {{get; set; }}\n");
                    filtersList.Add($"{relation.RelatedEntity}Id");
                }
            }
            foreach(var prop in filtersList)
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
            var query = _{lowerEntityName}Repository.GetAll();

            if (!string.IsNullOrWhiteSpace(request.SearchText))
                query = query.Where(x => x.Name.ToLower().Contains(request.SearchText.ToLower()));

            {filters}

            var result = await query
                .ProjectTo<Get{entityPlural}WithPaginationDto>(_mapper.ConfigurationProvider)
                .ApplyFilters(request.Filters)
                .OrderBy(request.Sort)
                .PaginatedListAsync(request.PageNumber, request.PageSize);

            //if (request.LanguageId != null)
            //    await _localizationService.FillCityLocalization(result, request.LanguageId.Value);

            return result;
        }}
    }}
}}";
            File.WriteAllText(filePath, content);
        }
        public static void GenerateGetWithPaginationValidator(string entityName, string entityPlural, string path)
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
        public static void GenerateBaseDto(string entityName, string entityPlural, List<(string Type, string Name, PropertyValidation Validation)> properties, string solutionDir,List<Relation> relations)
        {
            var filePath = Path.Combine(solutionDir, "Application", entityPlural, "Queries", $"{entityName}BaseDto.cs");

            if (File.Exists(filePath))
            {
                Console.WriteLine($"ℹ️ {entityName}BaseDto.cs already exists. Skipping...");
                return;
            }
            List<string> relationsProps = new List<string>();
            foreach (var relation in relations)
            {
                switch (relation.Type)
                {
                    case RelationType.OneToOneSelfJoin:
                        relationsProps.Add($"\t\tpublic Guid? {relation.RelatedEntity}Id {{  get; set; }}\n");
                        break;
                    case RelationType.OneToOne:
                        relationsProps.Add($"\t\tpublic Guid {relation.RelatedEntity}Id {{  get; set; }}\n");
                        break;
                    case RelationType.OneToOneNullable:
                        relationsProps.Add($"\t\tpublic Guid? {relation.RelatedEntity}Id {{  get; set; }}\n");
                        break;
                    case RelationType.ManyToOne:
                        relationsProps.Add($"\t\tpublic Guid {relation.RelatedEntity}Id {{  get; set; }}\n");
                        break;
                    case RelationType.ManyToOneNullable:
                        relationsProps.Add($"\t\tpublic Guid? {relation.RelatedEntity}Id {{  get; set; }}\n");
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
                    propList.Add($"\t\tpublic string? {prop.Name} {{ get; set; }}");
                }
                else
                {
                    propList.Add($"\t\tpublic {prop.Type} {prop.Name} {{ get; set; }}");
                }

            }
            //
            var props = string.Join(Environment.NewLine, propList);

            string content = $@"
using System;

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
            Console.WriteLine($"✅ {entityName}BaseDto.cs created.");
        }

        private static string? GeneratePropertyRules((string Type, string Name, PropertyValidation? Validation) property)
        {
            var rules = new StringBuilder();
            if (property.Validation == null)
            {
                return null;
            }
            // Start with the RuleFor declaration
            if(property.Type == "GPG")
            {
                rules.AppendLine($"            RuleFor(x => x.{property.Name}File)");
            }
            else if (property.Type == "PNGs")
            {
                rules.AppendLine($"            RuleFor(x => x.{property.Name}Files)");
            }
            else 
                rules.AppendLine($"            RuleFor(x => x.{property.Name})");

            // Handle required validation differently based on type
            if (property.Validation.Required)
            {
                rules.Append(".");

                // Use NotNull() for numeric types, NotEmpty() for strings
                if (property.Type == "int" || property.Type == "decimal" || property.Type == "float" || property.Type == "double" || property.Type == "GPG")
                {
                    rules.AppendLine($"\t\t\t\tNotNull().WithMessage(\"{property.Name} is required.\")");
                }
                else if (property.Type == "string" || property.Type == "PNGs")
                {
                    rules.AppendLine($"\t\t\t\tNotEmpty().WithMessage(\"{property.Name} is required.\")");
                }
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

        private static string GenerateRelationRules(List<Relation> relations)
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
                    default:
                        break;
                }
            }
            return rules.ToString();
        }

        private static string GenerateRelationMethods(List<Relation> relations, string commandName)
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
                    default:
                        break;
                }
            }
            return methods.ToString();
        }
    }
}
