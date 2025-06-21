using Microsoft.Win32;
using SharedClasses;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ApiGenerator
{
    public static class Api
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
        public static void GenerateNeededDtos(string entityName, string entityPlural, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, string solutionDir,bool hasLocalization,List<Relation> relations,bool bulk)
        {
            var dtoPath = Path.Combine(solutionDir, "Api", "NeededDto", entityName);
            Directory.CreateDirectory(dtoPath);
            var hasImages = properties.Any(p => p.Type == "GPG" || p.Type == "PNGs" || p.Type == "VD" || p.Type == "VDs");
            if (hasLocalization)
                GenerateLocalizationDto(entityName, dtoPath);
            if (hasLocalization || hasImages || enumProps.Any())
                GenerateCreateCommandDto(entityName, dtoPath,properties,enumProps,entityPlural,relations,hasLocalization,hasImages);
            if (bulk)
            {
                GenerateSingleEntityDto(entityName, dtoPath, properties, enumProps, entityPlural, relations, hasLocalization, hasImages);
                GenerateCreateBulkCommandDto(entityName, dtoPath, properties, enumProps, entityPlural, relations, hasLocalization, hasImages);
                GenerateSingleUpdatedEntityDto(entityName, dtoPath, properties, enumProps, hasLocalization, hasImages, entityPlural, relations);
                GenerateUpdateBulkCommandDto(entityName, dtoPath, properties, enumProps, entityPlural, relations, hasLocalization, hasImages);
            }
            GenerateUpdateCommandDto(entityName, dtoPath, properties,enumProps,hasLocalization,hasImages,entityPlural,relations);
            GenerateGetWithPaginationQueryDto(entityName, entityPlural, dtoPath,hasLocalization);
        }

        static void GenerateCreateCommandDto(string entityName, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, string entityPlural,List<Relation> relations,bool hasLocalization,bool hasImages)
        {
            string fileName = $"Create{entityName}CommandDto.cs";
            string filePath = Path.Combine(path, fileName);
            string? localizationProp = hasLocalization ? $"\t\tpublic {entityName}LocalizationDto[] {entityName}LocalizationDtos {{ get; set; }} = [];" : null;
            string? localizationMapp = hasLocalization ? $".ForMember(dest => dest.{entityName}LocalizationApps, opt => opt.MapFrom(src => src.{entityName}LocalizationDtos.To{entityName}LocalizationAppList()))" : null;
            string? ImageProp = properties.Any(p => p.Type == "GPG") ? 
                properties.First(t => t.Type == "GPG").Validation !=null ? $"\t\tpublic IFormFile {properties.First(t => t.Type == "GPG").Name}FormFile {{ get; set;}}" : $"\t\tpublic IFormFile? {properties.First(t => t.Type == "GPG").Name}FormFile {{ get; set;}}"
                :null;

            string? ImageMapp = properties.Any(p => p.Type == "GPG") ?
                properties.First(t => t.Type == "GPG").Validation != null 
                ? $".ForMember(dest => dest.{properties.First(t => t.Type == "GPG").Name}File, opt => opt.MapFrom(src => src.{properties.First(t => t.Type == "GPG").Name}FormFile.ToFileDto()))" 
                : $".ForMember(dest => dest.{properties.First(t => t.Type == "GPG").Name}File, opt => opt.MapFrom(src => src.{properties.First(t => t.Type == "GPG").Name}FormFile != null ? src.{properties.First(t => t.Type == "GPG").Name}FormFile.ToFileDto(): null))"
                : null;

            string? videoProp = properties.Any(p => p.Type == "VD") ?
                properties.First(t => t.Type == "VD").Validation != null ? $"\t\tpublic IFormFile {properties.First(t => t.Type == "VD").Name}FormFile {{ get; set;}}" : $"\t\tpublic IFormFile? {properties.First(t => t.Type == "VD").Name}FormFile {{ get; set;}}"
                : null;

            string? videoMapp = properties.Any(p => p.Type == "VD") ?
                properties.First(t => t.Type == "VD").Validation != null
                ? $".ForMember(dest => dest.{properties.First(t => t.Type == "VD").Name}File, opt => opt.MapFrom(src => src.{properties.First(t => t.Type == "VD").Name}FormFile.ToFileDto()))"
                : $".ForMember(dest => dest.{properties.First(t => t.Type == "VD").Name}File, opt => opt.MapFrom(src => src.{properties.First(t => t.Type == "VD").Name}FormFile != null ? src.{properties.First(t => t.Type == "VD").Name}FormFile.ToFileDto(): null))"
                : null;

            string? ListImageProp = properties.Any(p => p.Type == "PNGs") ?
                $"\t\tpublic List<IFormFile> {properties.First(t => t.Type == "PNGs").Name}FormFiles {{ get; set;}} = new List<IFormFile>();"
                : null;

            string? ListImageMapp = properties.Any(p => p.Type == "PNGs") ?
                $".ForMember(dest => dest.{properties.First(t => t.Type == "PNGs").Name}Files, opt => opt.MapFrom(src => src.{properties.First(t => t.Type == "PNGs").Name}FormFiles.ToFileDtoList()))"
                : null;

            string? ListVideoProp = properties.Any(p => p.Type == "VDs") ?
                $"\t\tpublic List<IFormFile> {properties.First(t => t.Type == "VDs").Name}FormFiles {{ get; set;}} = new List<IFormFile>();"
                : null;

            string? ListVideoMapp = properties.Any(p => p.Type == "VDs") ?
                $".ForMember(dest => dest.{properties.First(t => t.Type == "VDs").Name}Files, opt => opt.MapFrom(src => src.{properties.First(t => t.Type == "VDs").Name}FormFiles.ToFileDtoList()))"
                : null;


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
            List<string> filtersProps = new List<string>();
            foreach (var relation in relations)
            {
                switch (relation.Type)
                {
                    case RelationType.OneToOneSelfJoin:
                        filtersProps.Add($"\t\tpublic Guid? {relation.RelatedEntity}ParentId {{  get; set; }}\n");
                        break;
                    case RelationType.OneToOne:
                        filtersProps.Add($"\t\tpublic Guid {relation.RelatedEntity}Id {{  get; set; }}\n");
                        break;
                    case RelationType.OneToOneNullable:
                        filtersProps.Add($"\t\tpublic Guid? {relation.RelatedEntity}Id {{  get; set; }}\n");
                        break;
                    case RelationType.ManyToOne:
                        filtersProps.Add($"\t\tpublic Guid {relation.RelatedEntity}Id {{  get; set; }}\n");
                        break;
                    case RelationType.ManyToOneNullable:
                        filtersProps.Add($"\t\tpublic Guid? {relation.RelatedEntity}Id {{  get; set; }}\n");
                        break;
                    case RelationType.ManyToMany:
                        filtersProps.Add($"\t\tpublic List<Guid> {relation.RelatedEntity}Ids {{  get; set; }}\n");
                        break;
                    default:
                        break;
                }
            }
            var filtersPropsList = string.Join(Environment.NewLine, filtersProps);
            string mapper = $@"
        public class Mapping : Profile
        {{
            public Mapping()
            {{
                CreateMap<Create{entityName}CommandDto, Create{entityName}Command>()
                    {localizationMapp}
                    {ImageMapp}
                    {videoMapp}
                    {ListImageMapp}
                    {ListVideoMapp}
                    {mapperEnum}
                    ;
            }}
        }}
";
            var props = string.Join(Environment.NewLine, properties
                .Where(p=>p.Type != "GPG" && p.Type != "PNGs" && p.Type != "VD" && p.Type != "VDs")
                .Select(p => $"        public {p.Type} {p.Name} {{ get; set; }}"));

            string content = $@"using AutoMapper;
using Application.{entityPlural}.Commands.Create{entityName};
using Api.Utilities;
using Domain.Enums;
namespace Api.NeededDto.{entityName}
{{
    public class Create{entityName}CommandDto
    {{
{props}
{ImageProp}
{ListImageProp}
{videoProp}
{ListVideoProp}
{localizationProp}
{filtersPropsList}
{mapper}
    }}

}}";
            File.WriteAllText(filePath, content);
        }

        static void GenerateUpdateCommandDto(string entityName, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, bool hasLocalization,bool hasImages,string entityPlural,List<Relation> relations)
        {
            string fileName = $"Update{entityName}CommandDto.cs";
            string filePath = Path.Combine(path, fileName);
            string? localizationProp = hasLocalization ? $"\t\tpublic {entityName}LocalizationDto[] {entityName}LocalizationDtos {{ get; set; }} = [];" : null;
            string? localizationMapp = hasLocalization ? $".ForMember(dest => dest.{entityName}LocalizationApps, opt => opt.MapFrom(src => src.{entityName}LocalizationDtos.To{entityName}LocalizationAppList()))" : null;
            
            string? ImageProp = properties.Any(p => p.Type == "GPG") ?
                 $"\t\tpublic IFormFile? {properties.First(t => t.Type == "GPG").Name}FormFile {{ get; set;}}"
                : null;
            string? DeleteImageOrOldUrlProp = properties.Any(p => p.Type == "GPG") ?
                properties.First(t => t.Type == "GPG").Validation == null ? $"\t\tpublic bool? DeleteOld{properties.First(t => t.Type == "GPG").Name} {{ get; set; }}"
                : $"\t\tpublic string? {properties.First(t => t.Type == "GPG").Name}Url {{ get; set; }}"
                : null;
            string? ImageMapp = properties.Any(p => p.Type == "GPG") ?
                $".ForMember(dest => dest.{properties.First(t => t.Type == "GPG").Name}File, opt => opt.MapFrom(src => src.{properties.First(t => t.Type == "GPG").Name}FormFile != null ? src.{properties.First(t => t.Type == "GPG").Name}FormFile.ToFileDto(): null))"
                : null;

            string? videoProp = properties.Any(p => p.Type == "VD") ?
                 $"\t\tpublic IFormFile? {properties.First(t => t.Type == "VD").Name}FormFile {{ get; set;}}"
                : null;
            string? deleteVideoOrOldUrlProp = properties.Any(p => p.Type == "VD") ?
                properties.First(t => t.Type == "VD").Validation == null ? $"\t\tpublic bool? DeleteOld{properties.First(t => t.Type == "VD").Name} {{ get; set; }}"
                : $"\t\tpublic string? {properties.First(t => t.Type == "VD").Name}Url {{ get; set; }}"
                : null;
            string? videoMapp = properties.Any(p => p.Type == "VD") ?
                $".ForMember(dest => dest.{properties.First(t => t.Type == "VD").Name}File, opt => opt.MapFrom(src => src.{properties.First(t => t.Type == "VD").Name}FormFile != null ? src.{properties.First(t => t.Type == "VD").Name}FormFile.ToFileDto(): null))"
                : null;

            string? ListImageProp = properties.Any(p => p.Type == "PNGs") ?
                $"\t\tpublic List<IFormFile> {properties.First(t => t.Type == "PNGs").Name}FormFiles {{ get; set;}} = new List<IFormFile>();"
                : null;
            string? ListImageMapp = properties.Any(p => p.Type == "PNGs") ?
                $".ForMember(dest => dest.{properties.First(t => t.Type == "PNGs").Name}Files, opt => opt.MapFrom(src => src.{properties.First(t => t.Type == "PNGs").Name}FormFiles.ToFileDtoList()))"
                : null;
            string? DeletedOldImagesListProp = properties.Any(p => p.Type == "PNGs") ?
                $"\t\tpublic List<string>? Deleted{properties.First(t => t.Type == "PNGs").Name}URLs {{ get; set; }}"
                : null;

            string? ListVideoProp = properties.Any(p => p.Type == "VDs") ?
                $"\t\tpublic List<IFormFile> {properties.First(t => t.Type == "VDs").Name}FormFiles {{ get; set;}} = new List<IFormFile>();"
                : null;
            string? ListVideoMapp = properties.Any(p => p.Type == "VDs") ?
                $".ForMember(dest => dest.{properties.First(t => t.Type == "VDs").Name}Files, opt => opt.MapFrom(src => src.{properties.First(t => t.Type == "VDs").Name}FormFiles.ToFileDtoList()))"
                : null;
            string? DeletedOldVideosListProp = properties.Any(p => p.Type == "VDs") ?
                $"\t\tpublic List<string>? Deleted{properties.First(t => t.Type == "VDs").Name}URLs {{ get; set; }}"
                : null;

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
            List<string> filtersProps = new List<string>();
            foreach (var relation in relations)
            {
                switch (relation.Type)
                {
                    case RelationType.OneToOneSelfJoin:
                        filtersProps.Add($"\t\tpublic Guid? {relation.RelatedEntity}ParentId {{  get; set; }}\n");
                        break;
                    case RelationType.OneToOne:
                        filtersProps.Add($"\t\tpublic Guid {relation.RelatedEntity}Id {{  get; set; }}\n");
                        break;
                    case RelationType.OneToOneNullable:
                        filtersProps.Add($"\t\tpublic Guid? {relation.RelatedEntity}Id {{  get; set; }}\n");
                        break;
                    case RelationType.ManyToOne:
                        filtersProps.Add($"\t\tpublic Guid {relation.RelatedEntity}Id {{  get; set; }}\n");
                        break;
                    case RelationType.ManyToOneNullable:
                        filtersProps.Add($"\t\tpublic Guid? {relation.RelatedEntity}Id {{  get; set; }}\n");
                        break;
                    case RelationType.ManyToMany:
                        filtersProps.Add($"\t\tpublic List<Guid> {relation.RelatedEntity}Ids {{  get; set; }}\n");
                        break;
                    default:
                        break;
                }
            }
            var filtersPropsList = string.Join(Environment.NewLine, filtersProps);

            string? mapper =  $@"
        public class Mapping : Profile
        {{
            public Mapping()
            {{
                CreateMap<Update{entityName}CommandDto, Update{entityName}Command>()
                    {localizationMapp}
                    {ImageMapp}
                    {videoMapp}
                    {ListImageMapp}
                    {ListVideoMapp}
                    {mapperEnum}
                    ;
            }}
        }}
";
            var props = string.Join(Environment.NewLine, properties
                .Where(p => p.Type != "GPG" && p.Type != "PNGs" && p.Type != "VD" && p.Type != "VDs")
                .Select(p => $"        public {p.Type} {p.Name} {{ get; set; }}"));

            string content = $@"using AutoMapper;
using Application.{entityPlural}.Commands.Update{entityName};
using Api.Utilities;
using Domain.Enums;

namespace Api.NeededDto.{entityName}
{{
    public class Update{entityName}CommandDto
    {{
{props}
{localizationProp}
{ImageProp}
{DeleteImageOrOldUrlProp}
{ListImageProp}
{DeletedOldImagesListProp}
{videoProp}
{deleteVideoOrOldUrlProp}
{ListVideoProp}
{DeletedOldVideosListProp}
{filtersPropsList}
{mapper}
    }}
}}";
            File.WriteAllText(filePath, content);
        }

        static void GenerateLocalizationDto(string entityName, string path)
        {
            string fileName = $"{entityName}LocalizationDto.cs";
            string filePath = Path.Combine(path, fileName);

            string content = $@"namespace Api.NeededDto.{entityName}
{{
    public class {entityName}LocalizationDto
    {{
        public Guid LanguageId {{ get; set; }}
        public int FieldType {{ get; set; }}
        public string Value {{ get; set; }} = null!;
    }}
}}";
            File.WriteAllText(filePath, content);

            string extensionsPath = Path.Combine(path, "..", "..", "Utilities", "Extensions.cs");
            if (!File.Exists(extensionsPath))
            {
                //Console.WriteLine("⚠️ Api Extensions.cs not found.");
                return;
            }

            string usingExtension = $"using Api.NeededDto.{entityName};" + "\n//Add Using Here";
            string extension = $@"
        public static List<{entityName}LocalizationApp> To{entityName}LocalizationAppList(this {entityName}LocalizationDto[] dtoArray)
        {{
            var res = new List<{entityName}LocalizationApp>();
            foreach (var item in dtoArray)
            {{
                var temp = new {entityName}LocalizationApp
                {{
                    LanguageId = item.LanguageId,
                    FieldType = ({entityName}LocalizationFieldType)item.FieldType,
                    Value = item.Value,
                }};
                res.Add(temp);
            }}
    
            return res;
        }}" 
        +$"\n\t\t//Add Extension Here";

            var lines = File.ReadAllLines(extensionsPath).ToList();
            var index1 = lines.FindIndex(line => line.Contains("//Add Extension Here"));
            var index2 = lines.FindIndex(line => line.Contains("//Add Using Here"));

            if (index1 >= 0 || index2 >=0)
            {
                if(index1 >= 0)
                    lines[index1] = extension;
                if(index2 >= 0)
                    lines[index2] = usingExtension;
                File.WriteAllLines(extensionsPath, lines);
                //Console.WriteLine("✅ Api Extensions updated.");
            }
        }
        static void GenerateGetWithPaginationQueryDto(string entityName, string entityPlural, string path, bool hasLocalization)
        {
            string fileName = $"Get{entityPlural}WithPaginationQueryDto.cs";
            string filePath = Path.Combine(path, fileName);
            string? languageProp = !hasLocalization ? null : $"public string? LanguageCode {{ get; set; }}";
            string content = $@"
using Api.Utilities;
using Application.{entityPlural}.Queries.Get{entityPlural}WithPagination;
using AutoMapper;

namespace Api.NeededDto.{entityName}
{{
    public class Get{entityPlural}WithPaginationQueryDto : FilterableRequestDto
    {{
        public int PageNumber {{ get; init; }} = 1;
        public int PageSize {{ get; init; }} = 10;
        public string? SearchText {{ get; set; }}  
        public string? Sort {{ get; set; }}
        {languageProp}
        // Add your Filters Here

        public class Mapping : Profile
        {{
            public Mapping()
            {{
                CreateMap<Get{entityPlural}WithPaginationQueryDto, Get{entityPlural}WithPaginationQuery>()
                    .ForMember(dest => dest.Filters, opt => opt.MapFrom(src => src.Filters.ToFilterRequest()));
                    //Add mapper for enum if needed as filters
            }}
        }}
    }}
}}";
            File.WriteAllText(filePath, content);
        }


        public static void AddRoutesToApiRoutes(string entityName, string entityPlural, string solutionDir,bool hasLocalization,bool bulk)
        {
            string filePath = Path.Combine(solutionDir, "Api", "Utilities", "ApiRoutes.cs");

            if (!File.Exists(filePath))
            {
                //Console.WriteLine("❌ ApiRoutes.cs not found.");
                return;
            }

            string content = File.ReadAllText(filePath);
            string className = $"public static class {entityName}";
            string? GetWithLocalizationRoute = hasLocalization? $"public const string GetWithLocalization = Base + \"/{entityPlural.ToLower()}/{{{entityName.ToLower()}Id}}/localization\";" : null;
            string? bulkRouts = !bulk ? null :
                $@"
            public const string CreateBulk = Base + ""/{entityPlural.ToLower()}/bulk"";            
            public const string UpdateBulk = Base + ""/{entityPlural.ToLower()}/bulk"";
            public const string DeleteBulk = Base + ""/{entityPlural.ToLower()}/bulk"";
";
            string routeClass = $@"
        public static class {entityName}
        {{
            public const string Create = Base + ""/{entityPlural.ToLower()}"";
            public const string Get = Base + ""/{entityPlural.ToLower()}/{{{entityName.ToLower()}Id}}"";
            public const string GetAll = Base + ""/{entityPlural.ToLower()}"";
            public const string Update = Base + ""/{entityPlural.ToLower()}/{{{entityName.ToLower()}Id}}"";
            public const string Delete = Base + ""/{entityPlural.ToLower()}/{{{entityName.ToLower()}Id}}"";
            {GetWithLocalizationRoute}
            {bulkRouts}
        }}";

            var matches = ClassPattern.Matches(content);
            foreach (Match match in matches)
            {
                if (match.Groups[1].Value == entityName)
                {
                    Console.WriteLine($"⚠️ ApiRoutes already contains routes for {entityName}.");
                    return;
                }
            }

            //if (content.Contains(className))
            //{
            //    Console.WriteLine($"⚠️ ApiRoutes already contains routes for {entityName}.");
            //    return;
            //}

            // Add before the last closing brace
            int insertIndex = content.LastIndexOf("}") - 3;

            if (insertIndex < 0)
            {
                //Console.WriteLine("❌ Failed to find insertion point in ApiRoutes.cs");
                return;
            }

            content = content.Insert(insertIndex, "\n" + routeClass + "\n\t");
            File.WriteAllText(filePath, content);

            //Console.WriteLine($"✅ ApiRoutes updated with {entityName} routes.");
        }


        public static void GenerateController(string entityName, string entityPlural, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, string solutionDir,bool hasLocalization,bool hasPermissions,bool bulk)
        {
            var controllerName = $"{entityPlural}Controller.cs";
            var filePath = Path.Combine(solutionDir, "Api", "Controllers", controllerName);
            string? createPermission = null!;
            string? UpdatePermission = null!;
            string? GetPermission = null!;
            string? GetWithLocalizationPermission = null!;
            string? DeletePermission = null!;
            string fromType = "[FromBody]";
            var hasImages = properties.Any(p => p.Type == "GPG" || p.Type == "PNGs" || p.Type == "VD");
            if (hasPermissions)
            {
                createPermission = $"[Permission(RoleConsistent.{entityName}.Add)]";
                UpdatePermission = $"[Permission(RoleConsistent.{entityName}.Edit)]";
                GetPermission = $"[Permission(RoleConsistent.{entityName}.Browse)]";
                DeletePermission = $"[Permission(RoleConsistent.{entityName}.Delete)]";
                GetWithLocalizationPermission = $"[Permission(RoleConsistent.{entityName}.BrowseWithLocalization)]";
            }
            var lowerEntity = entityName.ToLower();
            var pluralLower = entityPlural.ToLower();
            string createParam = $"Create{entityName}Command command";
            string createCode = $@"
                var result = await _sender.Send(command);
                return Ok(result);";
            if (hasLocalization || enumProps.Any() || hasImages)
            {
                createParam = $"Create{entityName}CommandDto dto";
                createCode = $@"
                var command = _mapper.Map<Create{entityName}Command>(dto);
                var result = await _sender.Send(command);
                return Ok(result);";
                if (hasImages)
                    fromType = "[FromForm]";
            }
            
            string? localizationCode1 = !hasLocalization ? null : $@"
                Language language = null!;
                if (dto.LanguageCode != null)
                {{
                    language = await _languageRepository.GetLanguageByCodeAsync(dto.LanguageCode);
                    if (language == null)
                        throw new Exception(""Language is not found"");
                }}
";
            string? localizationCode2 = !hasLocalization ? null : $"query.LanguageId = language != null ? language.Id : null;";
            string getParam = !hasLocalization ? $"[FromRoute] Get{entityName}Query query" : $"[FromRoute] Guid {lowerEntity}Id, [FromQuery] string? languageCode";
            string getCode = !hasLocalization ? $"return Ok(await _sender.Send(query));" : $@"
                Language language = null!;
                if (languageCode != null)
                {{
                    language = await _languageRepository.GetLanguageByCodeAsync(languageCode);
                    if (language == null)
                        throw new Exception(""Language is not found"");
                }}

                Get{entityName}Query query = new Get{entityName}Query
                {{
                    LanguageId = language != null ? language.Id : null,
                    {entityName}Id = {lowerEntity}Id
                }};
                return Ok(await _sender.Send(query));
";
            string? getWithLocalizationEndpoint = !hasLocalization ? null : $@"
        [Route(ApiRoutes.{entityName}.GetWithLocalization)]
        [HttpGet]
        {GetWithLocalizationPermission}
        public async Task<IActionResult> Get{entityName}WithLocalization([FromRoute] Get{entityName}WithLocalizationQuery query)
        {{
            try
            {{
                return Ok(await _sender.Send(query));
            }}
            catch (Exception ex)
            {{
                List<string> messages = JsonParser.ParseMessages(ex.Message);
                return BadRequest(new {{ Errors = messages }});
            }}
        }}
"; 

            string? bulkEndpoints = !bulk ? null :
                $@"
        [Route(ApiRoutes.{entityName}.CreateBulk)]
        [HttpPost]
        {createPermission}
        public async Task<IActionResult> CreateBulk({fromType} CreateBulk{entityName}CommandDto dto)
        {{
            try
            {{
                var command = _mapper.Map<CreateBulk{entityName}Command>(dto);
                await _sender.Send(command);
                return Ok();
            }}
            catch (Exception ex)
            {{
                List<string> messages = JsonParser.ParseMessages(ex.Message);
                return BadRequest(new {{ Errors = messages }});
            }}
        }}

        [Route(ApiRoutes.{entityName}.UpdateBulk)]
        [HttpPut]
        {UpdatePermission}
        public async Task<IActionResult> UpdateBulk({fromType} UpdateBulk{entityName}CommandDto dto)
        {{
            try
            {{
                var command = _mapper.Map<UpdateBulk{entityName}Command>(dto);
                await _sender.Send(command);
                return NoContent();
            }}
            catch (Exception ex)
            {{
                List<string> messages = JsonParser.ParseMessages(ex.Message);
                return BadRequest(new {{ Errors = messages }});
            }}
        }}

        [Route(ApiRoutes.{entityName}.DeleteBulk)]
        [HttpDelete]
        {DeletePermission}
        public async Task<IActionResult> DeleteBulk([FromBody] DeleteBulk{entityName}Command command)
        {{
            try
            {{
                await _sender.Send(command);
                return NoContent();
            }}
            catch (Exception ex)
            {{
                List<string> messages = JsonParser.ParseMessages(ex.Message);
                return BadRequest(new {{ Errors = messages }});
            }}
        }}
";
            //string filledProperties = string.Join(Environment.NewLine, properties.Select(p =>
            //    $"                    {p.Name} = dto.{p.Name},"));
            string? usingLocalizationQuery = hasLocalization ? $"using Application.{entityPlural}.Queries.Get{entityName}WithLocalization;" : null ;
            string? usingBulk = bulk ? $"using Application.{entityPlural}.Commands.CreateBulk{entityName};using Application.{entityPlural}.Commands.UpdateBulk{entityName};using Application.{entityPlural}.Commands.DeleteBulk{entityName};" : null;
            string content = $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Api.Helpers;
using Application.Common.Interfaces.IRepositories;
using Api.NeededDto.{entityName};
using Api.Utilities;
using Application.{entityPlural}.Commands.Create{entityName};
using Application.{entityPlural}.Commands.Delete{entityName};
using Application.{entityPlural}.Commands.Update{entityName};
using Application.{entityPlural}.Queries.Get{entityName}Query;
using Application.{entityPlural}.Queries.Get{entityPlural}WithPagination;
{usingLocalizationQuery}
{usingBulk}
using Infrastructure.Utilities;
using AutoMapper;
using Domain.Entities;

namespace Api.Controllers
{{
    [ApiController]
    public class {entityPlural}Controller : ControllerBase
    {{
        private readonly ILogger<{entityPlural}Controller> _logger;
        private readonly ISender _sender;
        private readonly IMapper _mapper;
        private readonly ILanguageRepository _languageRepository;

        public {entityPlural}Controller(ILogger<{entityPlural}Controller> logger, ISender sender,IMapper mapper,ILanguageRepository languageRepository)
        {{
            _logger = logger;
            _sender = sender;
            _mapper = mapper;
            _languageRepository = languageRepository;
        }}

        [Route(ApiRoutes.{entityName}.Create)]
        [HttpPost]
        {createPermission}
        public async Task<IActionResult> Create({fromType} {createParam})
        {{
            try
            {{
{createCode}
            }}
            catch (Exception ex)
            {{
                List<string> messages = JsonParser.ParseMessages(ex.Message);
                return BadRequest(new {{ Errors = messages }});
            }}
        }}

        [Route(ApiRoutes.{entityName}.GetAll)]
        [HttpGet]
        {GetPermission}
        public async Task<IActionResult> GetAll([FromQuery] Get{entityPlural}WithPaginationQueryDto dto)
        {{
            try
            {{
                {localizationCode1}
                Get{entityPlural}WithPaginationQuery query = _mapper.Map<Get{entityPlural}WithPaginationQuery>(dto);
                {localizationCode2}
                return Ok(await _sender.Send(query));
            }}
            catch (ValidationException ex)
            {{
                List<string> messages = JsonParser.ParseMessages(ex.Message);
                return BadRequest(new {{ Errors = messages }});
            }}
            catch (Exception ex)
            {{
                List<string> messages = JsonParser.ParseMessages(ex.Message);
                return BadRequest(new {{ Errors = messages }});
            }}
        }}

        [Route(ApiRoutes.{entityName}.Get)]
        [HttpGet]
        {GetPermission}
        public async Task<IActionResult> Get({getParam})
        {{
            try
            {{
                {getCode}
            }}
            catch (ValidationException ex)
            {{
                List<string> messages = JsonParser.ParseMessages(ex.Message);
                return BadRequest(new {{ Errors = messages }});
            }}
            catch (Exception ex)
            {{
                List<string> messages = JsonParser.ParseMessages(ex.Message);
                return BadRequest(new {{ Errors = messages }});
            }}
        }}

        {getWithLocalizationEndpoint}

        [Route(ApiRoutes.{entityName}.Delete)]
        [HttpDelete]
        {DeletePermission}
        public async Task<IActionResult> Delete([FromRoute] Delete{entityName}Command command)
        {{
            try
            {{
                await _sender.Send(command);
                return NoContent();
            }}
            catch (ValidationException ex)
            {{
                List<string> messages = JsonParser.ParseMessages(ex.Message);
                return BadRequest(new {{ Errors = messages }});
            }}
            catch (Exception ex)
            {{
                List<string> messages = JsonParser.ParseMessages(ex.Message);
                return BadRequest(new {{ Errors = messages }});
            }}
        }}

        [Route(ApiRoutes.{entityName}.Update)]
        [HttpPut]
        {UpdatePermission}
        public async Task<IActionResult> Update({fromType} Update{entityName}CommandDto dto, Guid {lowerEntity}Id)
        {{
            try
            {{
                var command = _mapper.Map<Update{entityName}Command>(dto);
                command.{entityName}Id = {lowerEntity}Id;
                await _sender.Send(command);
                return NoContent();
            }}
            catch (Exception ex)
            {{
                List<string> messages = JsonParser.ParseMessages(ex.Message);
                return BadRequest(new {{ Errors = messages }});
            }}
        }}

        {bulkEndpoints}
    }}
}}";

            File.WriteAllText(filePath, content);
            //Console.WriteLine($"✅ {controllerName} created.");
        }
        static void GenerateSingleEntityDto(string entityName, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, string entityPlural, List<Relation> relations, bool hasLocalization, bool hasImages)
        {
            string fileName = $"Single{entityName}Dto.cs";
            string filePath = Path.Combine(path, fileName);
            string? localizationProp = hasLocalization ? $"\t\tpublic {entityName}LocalizationDto[] {entityName}LocalizationDtos {{ get; set; }} = [];" : null;
            string? ImageProp = properties.Any(p => p.Type == "GPG") ?
                properties.First(t => t.Type == "GPG").Validation != null ? $"\t\tpublic IFormFile {properties.First(t => t.Type == "GPG").Name}FormFile {{ get; set;}}" : $"\t\tpublic IFormFile? {properties.First(t => t.Type == "GPG").Name}FormFile {{ get; set;}}"
                : null;


            string? ListImageProp = properties.Any(p => p.Type == "PNGs") ?
                $"\t\tpublic List<IFormFile> {properties.First(t => t.Type == "PNGs").Name}FormFiles {{ get; set;}} = new List<IFormFile>();"
                : null;


            string? VideoProp = properties.Any(p => p.Type == "VD") ?
                properties.First(t => t.Type == "VD").Validation != null ? $"\t\tpublic IFormFile {properties.First(t => t.Type == "VD").Name}FormFile {{ get; set;}}" : $"\t\tpublic IFormFile? {properties.First(t => t.Type == "VD").Name}FormFile {{ get; set;}}"
                : null;

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
            List<string> filtersProps = new List<string>();
            foreach (var relation in relations)
            {
                switch (relation.Type)
                {
                    case RelationType.OneToOneSelfJoin:
                        filtersProps.Add($"\t\tpublic Guid? {relation.RelatedEntity}ParentId {{  get; set; }}\n");
                        break;
                    case RelationType.OneToOne:
                        filtersProps.Add($"\t\tpublic Guid {relation.RelatedEntity}Id {{  get; set; }}\n");
                        break;
                    case RelationType.OneToOneNullable:
                        filtersProps.Add($"\t\tpublic Guid? {relation.RelatedEntity}Id {{  get; set; }}\n");
                        break;
                    case RelationType.ManyToOne:
                        filtersProps.Add($"\t\tpublic Guid {relation.RelatedEntity}Id {{  get; set; }}\n");
                        break;
                    case RelationType.ManyToOneNullable:
                        filtersProps.Add($"\t\tpublic Guid? {relation.RelatedEntity}Id {{  get; set; }}\n");
                        break;
                    case RelationType.ManyToMany:
                        filtersProps.Add($"\t\tpublic List<Guid> {relation.RelatedEntity}Ids {{  get; set; }}\n");
                        break;
                    default:
                        break;
                }
            }
            var filtersPropsList = string.Join(Environment.NewLine, filtersProps);
            string mapper = $@"
        public class Mapping : Profile
        {{
            public Mapping()
            {{
                CreateMap<Single{entityName}Dto, Single{entityName}>()
                    {mapperEnum}
                    ;
            }}
        }}
";
            var props = string.Join(Environment.NewLine, properties
                .Where(p => p.Type != "GPG" && p.Type != "PNGs" && p.Type != "VD")
                .Select(p => $"        public {p.Type} {p.Name} {{ get; set; }}"));

            string content = $@"using AutoMapper;
using Application.{entityPlural}.Commands.CreateBulk{entityName};
using Api.Utilities;
using Domain.Enums;
namespace Api.NeededDto.{entityName}
{{
    public class Single{entityName}Dto
    {{
{props}
{ImageProp}
{ListImageProp}
{VideoProp}
{localizationProp}
{filtersPropsList}
{mapper}
    }}

}}";
            File.WriteAllText(filePath, content);
        }
        static void GenerateCreateBulkCommandDto(string entityName, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, string entityPlural, List<Relation> relations, bool hasLocalization, bool hasImages)
        {
            string fileName = $"CreateBulk{entityName}CommandDto.cs";
            string filePath = Path.Combine(path, fileName);
            string aggregator = relations.First(r => r.Type == RelationType.ManyToOne || r.Type == RelationType.ManyToOneNullable).RelatedEntity;
            string aggregatorField = relations.First(r => r.Type == RelationType.ManyToOne || r.Type == RelationType.ManyToOneNullable).Type == RelationType.ManyToOne ? $"public Guid {aggregator}Id {{ get; set; }}" : $"public Guid? {aggregator}Id {{ get; set; }}";

            List<(string Type, string Name, PropertyValidation Validation)> tempProps = new List<(string Type, string Name, PropertyValidation Validation)>();
            properties.ForEach(tempProps.Add);
            tempProps.RemoveAll(p => p.Type == "GPG" || p.Type == "PNGs" || p.Type == "VD" || enumProps.Any(t => t.prop == p.Name));

            string? localizationMapp = hasLocalization ? $"{entityName}LocalizationApps = obj.{entityName}LocalizationDtos.To{entityName}LocalizationAppList()," : null;
            
            string? ImageMapp = properties.Any(p => p.Type == "GPG") ?
                properties.First(t => t.Type == "GPG").Validation != null
                ? $"{properties.First(t => t.Type == "GPG").Name}File = obj.{properties.First(t => t.Type == "GPG").Name}FormFile.ToFileDto(),"
                : $"{properties.First(t => t.Type == "GPG").Name}File = obj.{properties.First(t => t.Type == "GPG").Name}FormFile != null ? obj.{properties.First(t => t.Type == "GPG").Name}FormFile.ToFileDto() : null,"
                : null;

            string? videoMapp = properties.Any(p => p.Type == "VD") ?
                properties.First(t => t.Type == "VD").Validation != null
                ? $"{properties.First(t => t.Type == "VD").Name}File = obj.{properties.First(t => t.Type == "VD").Name}FormFile.ToFileDto(),"
                : $"{properties.First(t => t.Type == "VD").Name}File = obj.{properties.First(t => t.Type == "VD").Name}FormFile != null ? obj.{properties.First(t => t.Type == "VD").Name}FormFile.ToFileDto() : null,"
                : null;

            string? ListImageMapp = properties.Any(p => p.Type == "PNGs") ?
                $"{properties.First(t => t.Type == "PNGs").Name}Files = obj.{properties.First(t => t.Type == "PNGs").Name}FormFiles.ToFileDtoList(),"
                : null;
            
            StringBuilder mapperEnum = new StringBuilder();
            foreach (var prop in properties)
            {
                if (enumProps.Any(p => p.prop == prop.Name))
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        mapperEnum.Append($"{prop.Name} = ({entityName}{prop.Name})obj.{prop.Name},");
                        mapperEnum.AppendLine();
                    }
                    else
                    {
                        mapperEnum.Append($"{prop.Name} = ({entityName}{prop.Name}?)obj.{prop.Name},");
                        mapperEnum.AppendLine();
                    }

                }
            }
            StringBuilder otherProps = new StringBuilder();
            foreach (var item in tempProps)
            {
                otherProps.Append($"\t\t\t\t\t{item.Name} = obj.{item.Name},");
                otherProps.AppendLine();
            }

            List<string> relationsProps = new List<string>();
            foreach (var relation in relations)
            {
                if (relation.RelatedEntity != aggregator)
                {
                    if(relation.Type ==  RelationType.OneToOne || relation.Type ==  RelationType.OneToOneNullable
                        || relation.Type == RelationType.ManyToOne || relation.Type == RelationType.ManyToOneNullable)

                        relationsProps.Add($"{relation.RelatedEntity}Id = obj.{relation.RelatedEntity}Id,");

                    if (relation.Type == RelationType.OneToOneSelfJoin)
                        relationsProps.Add($"{relation.RelatedEntity}ParentId = obj.{relation.RelatedEntity}ParentId,");

                    if (relation.Type == RelationType.ManyToMany)
                        relationsProps.Add($"{relation.RelatedEntity}Ids = obj.{relation.RelatedEntity}Ids,");
                }
            }
            string relPropMapper = string.Join(Environment.NewLine, relationsProps);
            string content = $@"using AutoMapper;
using Application.{entityPlural}.Commands.CreateBulk{entityName};
using Api.Utilities;
using Domain.Enums;

namespace Api.NeededDto.{entityName}
{{
    public class CreateBulk{entityName}CommandDto
    {{
        public Single{entityName}Dto[] Bulk{entityPlural} {{ get; set; }} = [];
        {aggregatorField}
        public class Mapping : Profile
        {{
            public Mapping()
            {{
                CreateMap<CreateBulk{entityName}CommandDto, CreateBulk{entityName}Command>()
            .ForMember(dest => dest.Bulk{entityPlural}, opt => opt.MapFrom(src =>
                src.Bulk{entityPlural}.Select(obj => new Single{entityName}
                {{
                    {aggregator}Id = src.{aggregator}Id,
                    {ImageMapp}
                    {videoMapp}
                    {ListImageMapp}
                    {mapperEnum}
                    {otherProps}
                    {relPropMapper}
                    {localizationMapp}
                }})));
            }}
        }}
    }}

}}";
            File.WriteAllText(filePath, content);
        }

        static void GenerateSingleUpdatedEntityDto(string entityName, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, bool hasLocalization, bool hasImages, string entityPlural, List<Relation> relations)
        {
            string fileName = $"SingleUpdated{entityName}Dto.cs";
            string filePath = Path.Combine(path, fileName);
            string? localizationProp = hasLocalization ? $"\t\tpublic {entityName}LocalizationDto[] {entityName}LocalizationDtos {{ get; set; }} = [];" : null;
            string? ImageProp = properties.Any(p => p.Type == "GPG") ?
                 $"\t\tpublic IFormFile? {properties.First(t => t.Type == "GPG").Name}FormFile {{ get; set;}}"
                : null;
            string? DeleteImageOrOldUrlProp = properties.Any(p => p.Type == "GPG") ?
                properties.First(t => t.Type == "GPG").Validation == null ? $"\t\tpublic bool? DeleteOld{properties.First(t => t.Type == "GPG").Name} {{ get; set; }}"
                : $"\t\tpublic string? Old{properties.First(t => t.Type == "GPG").Name}Url {{ get; set; }}"
                : null;

            string? videoProp = properties.Any(p => p.Type == "VD") ?
                 $"\t\tpublic IFormFile? {properties.First(t => t.Type == "VD").Name}FormFile {{ get; set;}}"
                : null;
            string? deleteVideoOrOldUrlProp = properties.Any(p => p.Type == "VD") ?
                properties.First(t => t.Type == "VD").Validation == null ? $"\t\tpublic bool? DeleteOld{properties.First(t => t.Type == "VD").Name} {{ get; set; }}"
                : $"\t\tpublic string? Old{properties.First(t => t.Type == "VD").Name}Url {{ get; set; }}"
                : null;

            string? ListImageProp = properties.Any(p => p.Type == "PNGs") ?
                $"\t\tpublic List<IFormFile> {properties.First(t => t.Type == "PNGs").Name}FormFiles {{ get; set;}} = new List<IFormFile>();"
                : null;

            string? DeletedOldImagesListProp = properties.Any(p => p.Type == "PNGs") ?
                $"\t\tpublic List<string>? Deleted{properties.First(t => t.Type == "PNGs").Name}URLs {{ get; set; }}"
                : null;

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
            List<string> relationsProps = new List<string>();
            foreach (var relation in relations)
            {
                switch (relation.Type)
                {
                    case RelationType.OneToOneSelfJoin:
                        relationsProps.Add($"\t\tpublic Guid? {relation.RelatedEntity}ParentId {{  get; set; }}");
                        break;
                    case RelationType.OneToOne:
                        relationsProps.Add($"\t\tpublic Guid {relation.RelatedEntity}Id {{  get; set; }}");
                        break;
                    case RelationType.OneToOneNullable:
                        relationsProps.Add($"\t\tpublic Guid? {relation.RelatedEntity}Id {{  get; set; }}");
                        break;
                    case RelationType.ManyToOne:
                        relationsProps.Add($"\t\tpublic Guid {relation.RelatedEntity}Id {{  get; set; }}");
                        break;
                    case RelationType.ManyToOneNullable:
                        relationsProps.Add($"\t\tpublic Guid? {relation.RelatedEntity}Id {{  get; set; }}");
                        break;
                    case RelationType.ManyToMany:
                        relationsProps.Add($"\t\tpublic List<Guid> {relation.RelatedEntity}Ids {{  get; set; }}");
                        break;
                    default:
                        break;
                }
            }
            var relationsPropsList = string.Join(Environment.NewLine, relationsProps);

            string? mapper = $@"
        public class Mapping : Profile
        {{
            public Mapping()
            {{
                CreateMap<SingleUpdated{entityName}Dto, SingleUpdated{entityName}>()
                    {mapperEnum}
                    ;
            }}
        }}
";
            var props = string.Join(Environment.NewLine, properties
                .Where(p => p.Type != "GPG" && p.Type != "PNGs" && p.Type != "VD")
                .Select(p => $"        public {p.Type} {p.Name} {{ get; set; }}"));

            string content = $@"using AutoMapper;
using Application.{entityPlural}.Commands.UpdateBulk{entityName};
using Api.Utilities;
using Domain.Enums;

namespace Api.NeededDto.{entityName}
{{
    public class SingleUpdated{entityName}Dto
    {{
        public Guid {entityName}Id {{ get; set; }} = GenerateGuid();
{props}
{localizationProp}
{ImageProp}
{DeleteImageOrOldUrlProp}
{videoProp}
{deleteVideoOrOldUrlProp}
{ListImageProp}
{DeletedOldImagesListProp}
{relationsPropsList}
{mapper}

        static Guid GenerateGuid()
        {{
            Random r = new Random();
            int a = r.Next(1, 1000);
            return new Guid(a, 2, 3, new byte[] {{ 0, 1, 2, 3, 4, 5, 6, 7 }});
        }}
    }}
}}";
            File.WriteAllText(filePath, content);
        }

        static void GenerateUpdateBulkCommandDto(string entityName, string path, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, string entityPlural, List<Relation> relations, bool hasLocalization, bool hasImages)
        {
            string fileName = $"UpdateBulk{entityName}CommandDto.cs";
            string filePath = Path.Combine(path, fileName);
            string aggregator = relations.First(r => r.Type == RelationType.ManyToOne || r.Type == RelationType.ManyToOneNullable).RelatedEntity;
            string aggregatorField = relations.First(r => r.Type == RelationType.ManyToOne || r.Type == RelationType.ManyToOneNullable).Type == RelationType.ManyToOne ? $"public Guid {aggregator}Id {{ get; set; }}" : $"public Guid? {aggregator}Id {{ get; set; }}";

            List<(string Type, string Name, PropertyValidation Validation)> tempProps = new List<(string Type, string Name, PropertyValidation Validation)>();
            properties.ForEach(tempProps.Add);
            tempProps.RemoveAll(p => p.Type == "GPG" || p.Type == "PNGs" || p.Type == "VD" || enumProps.Any(t => t.prop == p.Name));

            string? localizationMapp = hasLocalization ? $"{entityName}LocalizationApps = obj.{entityName}LocalizationDtos.To{entityName}LocalizationAppList()," : null;

            string? ImageMapp1 = properties.Any(p => p.Type == "GPG") 
                ? $"{properties.First(t => t.Type == "GPG").Name}File = obj.{properties.First(t => t.Type == "GPG").Name}FormFile != null ? obj.{properties.First(t => t.Type == "GPG").Name}FormFile.ToFileDto() : null,"
                : null;

            string? ImageMapp2 = properties.Any(p => p.Type == "GPG") ?
                properties.First(t => t.Type == "GPG").Validation != null
                ? $"Old{properties.First(t => t.Type == "GPG").Name}Url = obj.Old{properties.First(t => t.Type == "GPG").Name}Url,"
                : $"DeleteOld{properties.First(t => t.Type == "GPG").Name} = obj.DeleteOld{properties.First(t => t.Type == "GPG").Name},"
                : null;

            string? videoMapp1 = properties.Any(p => p.Type == "VD")
                ? $"{properties.First(t => t.Type == "VD").Name}File = obj.{properties.First(t => t.Type == "VD").Name}FormFile != null ? obj.{properties.First(t => t.Type == "VD").Name}FormFile.ToFileDto() : null,"
                : null;

            string? videoMapp2 = properties.Any(p => p.Type == "VD") ?
                properties.First(t => t.Type == "VD").Validation != null
                ? $"Old{properties.First(t => t.Type == "VD").Name}Url = obj.Old{properties.First(t => t.Type == "VD").Name}Url,"
                : $"DeleteOld{properties.First(t => t.Type == "VD").Name} = obj.DeleteOld{properties.First(t => t.Type == "VD").Name},"
                : null;

            string? ListImageMapp1 = properties.Any(p => p.Type == "PNGs") ?
                $"{properties.First(t => t.Type == "PNGs").Name}Files = obj.{properties.First(t => t.Type == "PNGs").Name}FormFiles.ToFileDtoList(),"
                : null;

            string? ListImageMapp2 = properties.Any(p => p.Type == "PNGs") ?
                $"Deleted{properties.First(t => t.Type == "PNGs").Name}URLs = obj.Deleted{properties.First(t => t.Type == "PNGs").Name}URLs,"
                : null;

            StringBuilder mapperEnum = new StringBuilder();
            foreach (var prop in properties)
            {
                if (enumProps.Any(p => p.prop == prop.Name))
                {
                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        mapperEnum.Append($"{prop.Name} = ({entityName}{prop.Name})obj.{prop.Name},");
                        mapperEnum.AppendLine();
                    }
                    else
                    {
                        mapperEnum.Append($"{prop.Name} = ({entityName}{prop.Name}?)obj.{prop.Name},");
                        mapperEnum.AppendLine();
                    }

                }
            }
            StringBuilder otherProps = new StringBuilder();
            foreach (var item in tempProps)
            {
                otherProps.Append($"\t\t\t\t\t{item.Name} = obj.{item.Name},");
                otherProps.AppendLine();
            }

            List<string> relationsProps = new List<string>();
            foreach (var relation in relations)
            {
                if (relation.RelatedEntity != aggregator)
                {
                    if (relation.Type == RelationType.OneToOne || relation.Type == RelationType.OneToOneNullable
                        || relation.Type == RelationType.ManyToOne || relation.Type == RelationType.ManyToOneNullable)

                        relationsProps.Add($"{relation.RelatedEntity}Id = obj.{relation.RelatedEntity}Id,");

                    if (relation.Type == RelationType.OneToOneSelfJoin)
                        relationsProps.Add($"{relation.RelatedEntity}ParentId = obj.{relation.RelatedEntity}ParentId,");


                    if (relation.Type == RelationType.ManyToMany)
                        relationsProps.Add($"{relation.RelatedEntity}Ids = obj.{relation.RelatedEntity}Ids,");
                }
            }
            string relPropMapper = string.Join(Environment.NewLine, relationsProps);
            string content = $@"using AutoMapper;
using Application.{entityPlural}.Commands.UpdateBulk{entityName};
using Api.Utilities;
using Domain.Enums;

namespace Api.NeededDto.{entityName}
{{
    public class UpdateBulk{entityName}CommandDto
    {{
        public SingleUpdated{entityName}Dto[] Bulk{entityPlural} {{ get; set; }} = [];
        {aggregatorField}
        public class Mapping : Profile
        {{
            public Mapping()
            {{
                CreateMap<UpdateBulk{entityName}CommandDto, UpdateBulk{entityName}Command>()
            .ForMember(dest => dest.Bulk{entityPlural}, opt => opt.MapFrom(src =>
                src.Bulk{entityPlural}.Select(obj => new SingleUpdated{entityName}
                {{
                    {aggregator}Id = src.{aggregator}Id,
                    {entityName}Id = obj.{entityName}Id,
                    {ImageMapp1}
                    {ImageMapp2}
                    {videoMapp1}
                    {videoMapp2}
                    {ListImageMapp1}
                    {ListImageMapp2}
                    {mapperEnum}
                    {otherProps}
                    {relPropMapper}
                    {localizationMapp}
                }})));
            }}
        }}
    }}

}}";
            File.WriteAllText(filePath, content);
        }

    }
}
