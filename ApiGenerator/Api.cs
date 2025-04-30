using Microsoft.Win32;
using SharedClasses;

namespace ApiGenerator
{
    public static class Api
    {
        public static void GenerateNeededDtos(string entityName, string entityPlural, List<(string Type, string Name, PropertyValidation Validation)> properties, string solutionDir,bool hasLocalization,List<Relation> relations)
        {
            var dtoPath = Path.Combine(solutionDir, "Api", "NeededDto", entityName);
            Directory.CreateDirectory(dtoPath);
            var hasImages = properties.Any(p => p.Type == "GPG" || p.Type == "PNGs");
            if (hasLocalization)
                GenerateLocalizationDto(entityName, dtoPath);
            if (hasLocalization || hasImages)
                GenerateCreateCommandDto(entityName, dtoPath,properties,entityPlural,relations,hasLocalization,hasImages);
            GenerateUpdateCommandDto(entityName, dtoPath, properties,hasLocalization,hasImages,entityPlural,relations);
            GenerateGetWithPaginationQueryDto(entityName, entityPlural, dtoPath);
        }

        public static void GenerateCreateCommandDto(string entityName, string path, List<(string Type, string Name, PropertyValidation Validation)> properties,string entityPlural,List<Relation> relations,bool hasLocalization,bool hasImages)
        {
            string fileName = $"Create{entityName}CommandDto.cs";
            string filePath = Path.Combine(path, fileName);
            string? localizationProp = hasLocalization ? $"\t\tpublic {entityName}LocalizationDto[] {entityName}LocalizationDtos {{ get; set; }} = [];" : null;
            string? localizationMapp = hasLocalization ? $".ForMember(dest => dest.{entityName}LocalizationApps, opt => opt.MapFrom(src => src.{entityName}LocalizationDtos.To{entityName}LocalizationAppList()))" : null;
            string? ImageProp = properties.Any(p => p.Type == "GPG") ? 
                properties.First(t => t.Type == "GPG").Validation !=null ? $"\t\tpublic IFormFile {properties.First(t => t.Type == "GPG").Name}FormFile {{ get; set;}}" : $"\t\tpublic IFormFile? {properties.First(t => t.Type == "GPG").Name}FormFile {{ get; set;}}"
                :null;

            string? ImageMapp = properties.Any(p => p.Type == "GPG") ?
                properties.First(t => t.Type == "GPG").Validation != null ? $".ForMember(dest => dest.{properties.First(t => t.Type == "GPG").Name}File, opt => opt.MapFrom(src => src.{properties.First(t => t.Type == "GPG").Name}FormFile.ToFileDto()))" : $".ForMember(dest => dest.{properties.First(t => t.Type == "GPG").Name}File, opt => opt.MapFrom(src => src.{properties.First(t => t.Type == "GPG").Name}FormFile != null ? src.{properties.First(t => t.Type == "GPG").Name}FormFile.ToFileDto(): null))"
                : null;

            string? ListImageProp = properties.Any(p => p.Type == "PNGs") ?
                $"\t\tpublic List<IFormFile> {properties.First(t => t.Type == "PNGs").Name}FormFiles {{ get; set;}} = new List<IFormFile>();"
                : null;

            string? ListImageMapp = properties.Any(p => p.Type == "PNGs") ?
                $".ForMember(dest => dest.{properties.First(t => t.Type == "PNGs").Name}Files, opt => opt.MapFrom(src => src.{properties.First(t => t.Type == "PNGs").Name}FormFiles.ToFileDtoList()))"
                : null;

            string? VideoProp = properties.Any(p => p.Type == "VD") ? $"\t\tpublic string? {properties.First(t => t.Type == "VD").Name} {{ get; set;}}" : null;

            List<string> filtersProps = new List<string>();
            foreach (var relation in relations)
            {
                switch (relation.Type)
                {
                    case RelationType.OneToOneSelfJoin:
                        filtersProps.Add($"\t\tpublic Guid? {relation.RelatedEntity}Id {{  get; set; }}\n");
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
                    {ListImageMapp}
                    ;
            }}
        }}
";
            var props = string.Join(Environment.NewLine, properties
                .Where(p=>p.Type!="GPG" && p.Type != "PNGs" && p.Type != "VD")
                .Select(p => $"        public {p.Type} {p.Name} {{ get; set; }}"));

            string content = $@"using AutoMapper;
using Application.{entityPlural}.Commands.Create{entityName};
using Api.Utilities;

namespace Api.NeededDto.{entityName}
{{
    public class Create{entityName}CommandDto
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

        public static void GenerateUpdateCommandDto(string entityName, string path, List<(string Type, string Name, PropertyValidation Validation)> properties,bool hasLocalization,bool hasImages,string entityPlural,List<Relation> relations)
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
                : $"\t\tpublic string? Old{properties.First(t => t.Type == "GPG").Name}Url {{ get; set; }}"
                : null;
            string? ImageMapp = properties.Any(p => p.Type == "GPG") ?
                $".ForMember(dest => dest.{properties.First(t => t.Type == "GPG").Name}File, opt => opt.MapFrom(src => src.{properties.First(t => t.Type == "GPG").Name}FormFile != null ? src.{properties.First(t => t.Type == "GPG").Name}FormFile.ToFileDto(): null))"
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

            string? VideoProp = properties.Any(p => p.Type == "VD") ? $"\t\tpublic string? {properties.First(t => t.Type == "VD").Name} {{ get; set;}}" : null;

            List<string> filtersProps = new List<string>();
            foreach (var relation in relations)
            {
                switch (relation.Type)
                {
                    case RelationType.OneToOneSelfJoin:
                        filtersProps.Add($"\t\tpublic Guid? {relation.RelatedEntity}Id {{  get; set; }}\n");
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
                    {ListImageMapp}
                    ;
            }}
        }}
";
            var props = string.Join(Environment.NewLine, properties
                .Where(p => p.Type != "GPG" && p.Type != "PNGs" && p.Type != "VD")
                .Select(p => $"        public {p.Type} {p.Name} {{ get; set; }}"));

            string content = $@"using AutoMapper;
using Application.{entityPlural}.Commands.Update{entityName};
using Api.Utilities;

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
{VideoProp}
{filtersPropsList}
{mapper}
    }}
}}";
            File.WriteAllText(filePath, content);
        }

        public static void GenerateLocalizationDto(string entityName, string path)
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
                Console.WriteLine("⚠️ Api Extensions.cs not found.");
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
                Console.WriteLine("✅ Api Extensions updated.");
            }
        }
        public static void GenerateGetWithPaginationQueryDto(string entityName, string entityPlural, string path)
        {
            string fileName = $"Get{entityPlural}WithPaginationQueryDto.cs";
            string filePath = Path.Combine(path, fileName);

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

        // Add your Filters Here

        public class Mapping : Profile
        {{
            public Mapping()
            {{
                CreateMap<Get{entityPlural}WithPaginationQueryDto, Get{entityPlural}WithPaginationQuery>()
                    .ForMember(dest => dest.Filters, opt => opt.MapFrom(src => src.Filters.ToFilterRequest()));
            }}
        }}
    }}
}}";
            File.WriteAllText(filePath, content);
        }


        public static void AddRoutesToApiRoutes(string entityName, string entityPlural, string solutionDir)
        {
            string filePath = Path.Combine(solutionDir, "Api", "Utilities", "ApiRoutes.cs");

            if (!File.Exists(filePath))
            {
                Console.WriteLine("❌ ApiRoutes.cs not found.");
                return;
            }

            string content = File.ReadAllText(filePath);
            string className = $"public static class {entityName}";
            string routeClass = $@"
        public static class {entityName}
        {{
            public const string Create = Base + ""/{entityPlural.ToLower()}"";
            public const string Get = Base + ""/{entityPlural.ToLower()}/{{{entityName.ToLower()}Id}}"";
            public const string GetAll = Base + ""/{entityPlural.ToLower()}"";
            public const string Update = Base + ""/{entityPlural.ToLower()}/{{{entityName.ToLower()}Id}}"";
            public const string Delete = Base + ""/{entityPlural.ToLower()}/{{{entityName.ToLower()}Id}}"";
        }}";

            if (content.Contains(className))
            {
                Console.WriteLine($"⚠️ ApiRoutes already contains routes for {entityName}.");
                return;
            }

            // Add before the last closing brace
            int insertIndex = content.LastIndexOf("}") - 3;

            if (insertIndex < 0)
            {
                Console.WriteLine("❌ Failed to find insertion point in ApiRoutes.cs");
                return;
            }

            content = content.Insert(insertIndex, "\n" + routeClass + "\n\t");
            File.WriteAllText(filePath, content);

            Console.WriteLine($"✅ ApiRoutes updated with {entityName} routes.");
        }


        public static void GenerateController(string entityName, string entityPlural, List<(string Type, string Name, PropertyValidation Validation)> properties, string solutionDir,bool hazLocalization,bool hasPermissions)
        {
            var controllerName = $"{entityPlural}Controller.cs";
            var filePath = Path.Combine(solutionDir, "Api", "Controllers", controllerName);
            string? createPermission = null!;
            string? UpdatePermission = null!;
            string? GetPermission = null!;
            string? DeletePermission = null!;
            if (hasPermissions)
            {
                createPermission = $"[Permission(RoleConsistent.{entityName}.Add)]";
                UpdatePermission = $"[Permission(RoleConsistent.{entityName}.Edit)]";
                GetPermission = $"[Permission(RoleConsistent.{entityName}.Browse)]";
                DeletePermission = $"[Permission(RoleConsistent.{entityName}.Delete)]";
            }
            var lowerEntity = entityName.ToLower();
            var pluralLower = entityPlural.ToLower();
            string createParam = $"Create{entityName}Command command";
            string createCode = $@"
                var result = await _sender.Send(command);
                return Ok(result);";
            if (hazLocalization)
            {
                createParam = $"Create{entityName}CommandDto dto";
                createCode = $@"
                var command = _mapper.Map<Create{entityName}Command>(dto);
                var result = await _sender.Send(command);
                return Ok(result);";
            }

            //string filledProperties = string.Join(Environment.NewLine, properties.Select(p =>
            //    $"                    {p.Name} = dto.{p.Name},"));

            string content = $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Api.Helpers;
using Api.NeededDto.{entityName};
using Api.Utilities;
using Application.{entityPlural}.Commands.Create{entityName};
using Application.{entityPlural}.Commands.Delete{entityName};
using Application.{entityPlural}.Commands.Update{entityName};
using Application.{entityPlural}.Queries.Get{entityName}Query;
using Application.{entityPlural}.Queries.Get{entityPlural}WithPagination;
using Infrastructure.Utilities;
using AutoMapper;

namespace Api.Controllers
{{
    [ApiController]
    public class {entityPlural}Controller : ControllerBase
    {{
        private readonly ILogger<{entityPlural}Controller> _logger;
        private readonly ISender _sender;
        private readonly IMapper _mapper;

        public {entityPlural}Controller(ILogger<{entityPlural}Controller> logger, ISender sender,IMapper mapper)
        {{
            _logger = logger;
            _sender = sender;
            _mapper = mapper;
        }}

        [Route(ApiRoutes.{entityName}.Create)]
        [HttpPost]
        {createPermission}
        public async Task<IActionResult> Create([FromBody] {createParam})
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
                Get{entityPlural}WithPaginationQuery query = _mapper.Map<Get{entityPlural}WithPaginationQuery>(dto);
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
        public async Task<IActionResult> Get([FromRoute] Get{entityName}Query query)
        {{
            try
            {{
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
        public async Task<IActionResult> Update([FromBody] Update{entityName}CommandDto dto, Guid {lowerEntity}Id)
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
    }}
}}";

            File.WriteAllText(filePath, content);
            Console.WriteLine($"✅ {controllerName} created.");
        }


    }
}
