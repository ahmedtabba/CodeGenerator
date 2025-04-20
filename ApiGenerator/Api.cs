using SharedClasses;

namespace ApiGenerator
{
    public static class Api
    {
        public static void GenerateNeededDtos(string entityName, string entityPlural, List<(string Type, string Name, PropertyValidation Validation)> properties, string solutionDir)
        {
            var dtoPath = Path.Combine(solutionDir, "Api", "NeededDto", entityName);
            Directory.CreateDirectory(dtoPath);

            GenerateUpdateCommandDto(entityName, dtoPath, properties);
            GenerateGetWithPaginationQueryDto(entityName, entityPlural, dtoPath);
        }
        public static void GenerateUpdateCommandDto(string entityName, string path, List<(string Type, string Name, PropertyValidation Validation)> properties)
        {
            string fileName = $"Update{entityName}CommandDto.cs";
            string filePath = Path.Combine(path, fileName);

            var props = string.Join(Environment.NewLine, properties.Select(p => $"        public {p.Type} {p.Name} {{ get; set; }}"));

            string content = $@"namespace Api.NeededDto.{entityName}
{{
    public class Update{entityName}CommandDto
    {{
{props}
    }}
}}";
            File.WriteAllText(filePath, content);
        }
        public static void GenerateGetWithPaginationQueryDto(string entityName, string entityPlural, string path)
        {
            string fileName = $"Get{entityPlural}WithPaginationQueryDto.cs";
            string filePath = Path.Combine(path, fileName);

            string content = $@"namespace Api.NeededDto.{entityName}
{{
    public class Get{entityPlural}WithPaginationQueryDto : FilterableRequestDto
    {{
        public int PageNumber {{ get; init; }} = 1;
        public int PageSize {{ get; init; }} = 10;
        public string? SearchText {{ get; set; }}

        // Add your Filters Here
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

            content = content.Insert(insertIndex, "\n\n" + routeClass + "\n");
            File.WriteAllText(filePath, content);

            Console.WriteLine($"✅ ApiRoutes updated with {entityName} routes.");
        }


        public static void GenerateController(string entityName, string entityPlural, List<(string Type, string Name, PropertyValidation Validation)> properties, string solutionDir)
        {
            var controllerName = $"{entityPlural}Controller.cs";
            var filePath = Path.Combine(solutionDir, "Api", "Controllers", controllerName);

            var lowerEntity = entityName.ToLower();
            var pluralLower = entityPlural.ToLower();

            string filledProperties = string.Join(Environment.NewLine, properties.Select(p =>
                $"                    {p.Name} = dto.{p.Name},"));

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
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Api.Controllers
{{
    [ApiController]
    public class {entityPlural}Controller : ControllerBase
    {{
        private readonly ILogger<{entityPlural}Controller> _logger;
        private readonly ISender _sender;

        public {entityPlural}Controller(ILogger<{entityPlural}Controller> logger, ISender sender)
        {{
            _logger = logger;
            _sender = sender;
        }}

        [Route(ApiRoutes.{entityName}.Create)]
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Create([FromBody] Create{entityName}Command command)
        {{
            try
            {{
                var result = await _sender.Send(command);
                return Ok(result);
            }}
            catch (Exception ex)
            {{
                List<string> messages = JsonParser.ParseMessages(ex.Message);
                return BadRequest(new {{ Errors = messages }});
            }}
        }}

        [Route(ApiRoutes.{entityName}.GetAll)]
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetAll([FromQuery] Get{entityPlural}WithPaginationQueryDto dto)
        {{
            try
            {{
                var query = new Get{entityPlural}WithPaginationQuery
                {{
                    PageNumber = dto.PageNumber,
                    PageSize = dto.PageSize,
                    SearchText = dto.SearchText,
                    Filters = dto.Filters.ToFilterRequest()
                }};
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Update([FromBody] Update{entityName}CommandDto dto, Guid {lowerEntity}Id)
        {{
            try
            {{
                var command = new Update{entityName}Command
                {{
                    Id = {lowerEntity}Id,
{filledProperties}
                }};
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
