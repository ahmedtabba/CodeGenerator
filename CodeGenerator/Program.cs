using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        string solutionDir = "C:\\Users\\Asus\\source\\repos\\EvaVehicles\\EvaVehicles";
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

        GenerateEntityClass(entityName, domainPath, properties);
        //GenerateEntityLocalizationClass(entityName, domainPath);
        UpdateAppDbContext(entityName, domainPath);
        GenerateIRepositoryInterface(entityName, repoInterfacePath);
        GenerateRepository(entityName, repoPath);
        GenerateCreateCommand(entityName, entityPlural, createCommandPath, properties);
        GenerateCreateCommandValidator(entityName, entityPlural, createCommandPath, properties);

        GenerateUpdateCommand(entityName, entityPlural, updateCommandPath, properties);
        GenerateUpdateCommandValidator(entityName, entityPlural, updateCommandPath, properties);


        GenerateDeleteCommand(entityName, entityPlural, deleteCommandPath, properties);
        GenerateDeleteCommandValidator(entityName, entityPlural, deleteCommandPath, properties);


        GenerateGetByIdQuery(entityName, entityPlural, queryPath);

        GenerateGetWithPaginationQuery(entityName, entityPlural, queryPath);

        GenerateNeededDtos(entityName, entityPlural, properties, solutionDir);

        AddRoutesToApiRoutes(entityName, entityPlural, solutionDir);

        GenerateController(entityName, entityPlural, properties, solutionDir);

        GenerateBaseDto(entityName, entityPlural, properties, solutionDir);


        Console.WriteLine("✅ Done! All files were generated successfully.");
    }



    static List<(string Type, string Name)> GetPropertiesFromUser(string entityName)
    {
        var properties = new List<(string Type, string Name)>
        {
            ("string", "Name")
        };

        while (true)
        {
            Console.Write("Add new property? (y/n): ");
            var answer = Console.ReadLine();
            if (answer?.ToLower() != "y") break;

            Console.Write(" - Property Name: ");
            var name = Console.ReadLine()?.Trim();

            Console.Write(" - Property Type: ");
            var type = Console.ReadLine()?.Trim();

            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(type))
                properties.Add((type, name));
        }

        return properties;
    }

    static void GenerateEntityClass(string entityName, string path, List<(string Type, string Name)> properties)
    {
        string fileName = $"{entityName}.cs";
        string filePath = Path.Combine(path, fileName);

        var props = string.Join(Environment.NewLine, properties.Select(p => $"        public {p.Type} {p.Name} {{ get; set; }}"));
        string content = $@"using Domain.Common;
using System;
using System.Collections.Generic;

namespace Domain.Entities
{{
    public class {entityName} : BaseAuditableEntity
    {{
{props}
        public virtual ICollection<{entityName}Localization> {entityName}Localizations {{ get; set; }}
    }}
}}";

        File.WriteAllText(filePath, content);
    }

    static void GenerateEntityLocalizationClass(string entityName, string path)
    {
        string fileName = $"{entityName}Localization.cs";
        string filePath = Path.Combine(path, fileName);

        string content = $@"using Domain.Common;
using System;

namespace Domain.Entities
{{
    public class {entityName}Localization : BaseAuditableEntity
    {{
        public Guid {entityName}Id {{ get; set; }}
        public virtual {entityName} {entityName} {{ get; set; }}
    }}
}}";
        File.WriteAllText(filePath, content);
    }

    static void UpdateAppDbContext(string entityName, string domainPath)
    {
        string contextPath = Path.Combine(domainPath, "..", "..", "Infrastructure", "Data", "AppDbContext.cs");
        if (!File.Exists(contextPath))
        {
            Console.WriteLine("⚠️ AppDbContext.cs not found.");
            return;
        }

        string dbSet = $"\t\tpublic DbSet<{entityName}> {entityName}s => Set<{entityName}>();" +
            //$"\n\t\tpublic DbSet<{entityName}Localization> {entityName}sLocalization => Set<{entityName}Localization>();" +
            $"\n\t\t//Generate Here";

        var lines = File.ReadAllLines(contextPath).ToList();
        var index = lines.FindIndex(line => line.Contains("//Generate Here"));

        if (index >= 0)
        {
            lines[index] = dbSet;
            File.WriteAllLines(contextPath, lines);
            Console.WriteLine("✅ AppDbContext updated.");
        }
    }

    static void GenerateIRepositoryInterface(string entityName, string path)
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
    static void GenerateRepository(string entityName, string path)
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

    //Handle Commands
    static void GenerateCreateCommand(string entityName, string entityPlural, string path, List<(string Type, string Name)> properties)
    {
        string className = $"Create{entityName}Command";
        string filePath = Path.Combine(path, $"{className}.cs");

        var props = string.Join(Environment.NewLine, properties.Select(p => $"        public {p.Type} {p.Name} {{ get; set; }}"));

        string content = $@"using Amazon.Runtime.Internal.Util;
using Application.Common.Interfaces.Db;
using Application.Common.Interfaces.IRepositories;
using Application.Common.Models.Localization;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
                            
                            namespace Application.{entityPlural}.Commands.Create{entityName}
                            {{
                                public class {className} : IRequest<string>
                                {{
                            {props}

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
                                   
                            
                                    private readonly I{entityName}Repository _repository;
                            
                            
                                    public {className}Handler(ILogger<{className}Handler> logger,
                                                                    IMapper mapper,
                                                                    IUnitOfWorkAsync unitOfWork,
                                                                    I{entityName}Repository repository)
                                    {{
                                        _repository = repository;
                                        _mapper=mapper;
                                        _unitOfWork=unitOfWork;
                                        _logger=logger;
                                    }}
                            
                                    public async Task<string> Handle({className} request, CancellationToken cancellationToken)
                                    {{
                                      try
                                        {{
                                            await _unitOfWork.BeginTransactionAsync();
                                            var {entityName.ToLower()} = _mapper.Map<{entityName}>(request);
                                            await _repository.AddAsync({entityName.ToLower()});
                                            await _unitOfWork.SaveChangesAsync(cancellationToken);
                            
                            
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
    static void GenerateCreateCommandValidator(string entityName, string entityPlural, string path, List<(string Type, string Name)> properties)
    {
        string className = $"Create{entityName}CommandValidator";
        string commandName = $"Create{entityName}Command";
        string filePath = Path.Combine(path, $"{className}.cs");

        string rules = string.Join(Environment.NewLine, properties
            .Where(p => p.Type == "string")
            .Select(p => $"            RuleFor(x => x.{p.Name}).NotEmpty().WithMessage(\"{p.Name} is required.\");"));

        string content = $@"using FluentValidation;

                            namespace Application.{entityPlural}.Commands.Create{entityName}
                            {{
                                public class {className} : AbstractValidator<{commandName}>
                                {{
                                    public {className}()
                                    {{
                                     {rules}
                                    }}
                                }}
                            }}";
        File.WriteAllText(filePath, content);
    }


    static void GenerateUpdateCommand(string entityName, string entityPlural, string path, List<(string Type, string Name)> properties)
    {
        string className = $"Update{entityName}Command";
        string filePath = Path.Combine(path, $"{className}.cs");

        var props = string.Join(Environment.NewLine, properties.Select(p => $"        public {p.Type} {p.Name} {{ get; set; }}"));

        string content = $@"
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces.Db;
using Application.Common.Interfaces.IRepositories;
using Domain.Entities;

namespace Application.{entityPlural}.Commands.Update{entityName}
{{
    public class {className} : IRequest
    {{
       public Guid Id {{ get; set; }}
       {props}
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
        private readonly I{entityName}Repository _repository;
        private readonly IUnitOfWorkAsync _unitOfWork;

        public {className}Handler(ILogger<{className}Handler> logger,
                                            IMapper mapper,
                                            I{entityName}Repository repository,
                                            IUnitOfWorkAsync unitOfWork)
        {{
            _logger = logger;
            _mapper = mapper;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }}
        public async Task Handle({className} request, CancellationToken cancellationToken)
        {{
            try
            {{
                await _unitOfWork.BeginTransactionAsync();
                var existingObj = await _repository.GetByIdAsync(request.Id);

                _mapper.Map(request, existingObj);

                await _repository.UpdateAsync(existingObj);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitAsync();
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
    static void GenerateUpdateCommandValidator(string entityName, string entityPlural, string path, List<(string Type, string Name)> properties)
    {
        string className = $"Update{entityName}CommandValidator";
        string commandName = $"Update{entityName}Command";
        string filePath = Path.Combine(path, $"{className}.cs");

        string rules = string.Join(Environment.NewLine, properties
            .Where(p => p.Type == "string")
            .Select(p => $"            RuleFor(x => x.{p.Name}).NotEmpty().WithMessage(\"{p.Name} is required.\");"));

        string content = $@"using FluentValidation;
using Application.{entityPlural}.Commands.Update{entityName};

                            namespace Application.{entityPlural}.Commands.Update{entityName}
                            {{
                                public class {className} : AbstractValidator<{commandName}>
                                {{
                                    public {className}()
                                    {{
                                     {rules}
                                    }}
                                }}
                            }}";
        File.WriteAllText(filePath, content);
    }

    static void GenerateDeleteCommand(string entityName, string entityPlural, string path, List<(string Type, string Name)> properties)
    {
        string className = $"Delete{entityName}Command";
        string filePath = Path.Combine(path, $"{className}.cs");


        string content = $@"
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Interfaces.Db;
using Application.Common.Interfaces.IRepositories;

namespace Application.{entityPlural}.Commands.Delete{entityName}
{{
    public class {className} : IRequest
    {{
        public Guid {entityName}Id {{ get; set; }}
    }}

    public class {className}Handler : IRequestHandler<{className}>
    {{
        private readonly ILogger<{className}Handler> _logger;
        private readonly I{entityName}Repository _repository;
        private readonly IUnitOfWorkAsync _unitOfWork;

        public {className}Handler(ILogger<{className}Handler> logger,
                                            I{entityName}Repository repository,
                                            IUnitOfWorkAsync unitOfWork)
        {{
            _logger = logger;
            _repository = repository;
            _unitOfWork = unitOfWork;
        }}

        public async Task Handle({className} request, CancellationToken cancellationToken)
        {{
            try
            {{
                await _unitOfWork.BeginTransactionAsync();
                var {entityName.ToLower()} = await _repository.GetByIdAsync(request.{entityName}Id);
                await _repository.DeleteAsync({entityName.ToLower()});
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitAsync();
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
    static void GenerateDeleteCommandValidator(string entityName, string entityPlural, string path, List<(string Type, string Name)> properties)
    {
        string className = $"Delete{entityName}CommandValidator";
        string commandName = $"Delete{entityName}Command";
        string filePath = Path.Combine(path, $"{className}.cs");


        string content = $@"using Microsoft.Extensions.Logging;
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
        private readonly I{entityName}Repository _repository;


        public {className}(ILogger<{className}> logger,
                                              I{entityName}Repository repository)
        {{
            _logger = logger;
            _repository = repository;
            RuleFor(l => l.{entityName}Id)
                .NotEmpty().WithMessage(""Id Must be passed"")
                .CustomAsync(async (name, context, cancellationToken) =>
                {{
                    if (!await CanDelete{entityName}(context.InstanceToValidate))
                    {{
                        context.AddFailure(""Delete {entityName}"", ""{entityName} is not found !"");
                    }}
                }});
        }}

        public async Task<bool> CanDelete{entityName}({commandName} command)
        {{
            var {entityName.ToLower()} = await _repository.GetByIdAsync(command.{entityName}Id);

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


    static void GenerateGetByIdQuery(string entityName, string entityPlural, string path)
    {
        var folderPath = Path.Combine(path, $"Get{entityName}Query");
        Directory.CreateDirectory(folderPath);

        GenerateGetByIdDto(entityName, entityPlural, folderPath);
        GenerateGetByIdQueryFile(entityName, entityPlural, folderPath);
        GenerateGetByIdValidator(entityName, entityPlural, folderPath);
    }

    static void GenerateGetByIdDto(string entityName, string entityPlural, string path)
    {
        string fileName = $"Get{entityName}Dto.cs";
        string filePath = Path.Combine(path, fileName);

        string content = $@"using Domain.Entities;

namespace Application.{entityPlural}.Queries.Get{entityName}Query
{{
    public class Get{entityName}Dto : {entityName}BaseDto
    {{
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

    static void GenerateGetByIdQueryFile(string entityName, string entityPlural, string path)
    {
        string fileName = $"Get{entityName}Query.cs";
        string filePath = Path.Combine(path, fileName);

        string content = $@"using Microsoft.Extensions.Logging;
using Application.Common.Interfaces.IRepositories;

namespace Application.{entityPlural}.Queries.Get{entityName}Query
{{
    public class Get{entityName}Query : IRequest<Get{entityName}Dto>
    {{
        public Guid {entityName}Id {{ get; set; }}
    }}

    public class Get{entityName}QueryHandler : IRequestHandler<Get{entityName}Query, Get{entityName}Dto>
    {{
        private readonly ILogger<Get{entityName}QueryHandler> _logger;
        private readonly IMapper _mapper;
        private readonly I{entityName}Repository _{entityName.ToLower()}Repository;

        public Get{entityName}QueryHandler(ILogger<Get{entityName}QueryHandler> logger,
                                           IMapper mapper,
                                           I{entityName}Repository {entityName.ToLower()}Repository)
        {{
            _logger = logger;
            _mapper = mapper;
            _{entityName.ToLower()}Repository = {entityName.ToLower()}Repository;
        }}

        public async Task<Get{entityName}Dto> Handle(Get{entityName}Query request, CancellationToken cancellationToken)
        {{
            var {entityName.ToLower()} = await _{entityName.ToLower()}Repository.GetByIdAsync(request.{entityName}Id);
            var dto = _mapper.Map<Get{entityName}Dto>({entityName.ToLower()});
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

        string content = $@"using FluentValidation;
using Microsoft.Extensions.Logging;
using Application.Common.Interfaces.IRepositories;

namespace Application.{entityPlural}.Queries.Get{entityName}Query
{{
    public class Get{entityName}QueryValidator : AbstractValidator<Get{entityName}Query>
    {{
        private readonly ILogger<Get{entityName}QueryValidator> _logger;
        private readonly I{entityName}Repository _{entityName.ToLower()}Repository;

        public Get{entityName}QueryValidator(ILogger<Get{entityName}QueryValidator> logger,
                                             I{entityName}Repository {entityName.ToLower()}Repository)
        {{
            _logger = logger;
            _{entityName.ToLower()}Repository = {entityName.ToLower()}Repository;

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
            return await _{entityName.ToLower()}Repository.GetByIdAsync(query.{entityName}Id) != null;
        }}
    }}
}}";
        File.WriteAllText(filePath, content);
    }


    static void GenerateGetWithPaginationQuery(string entityName, string entityPlural, string path)
    {
        var folderPath = Path.Combine(path, $"Get{entityPlural}WithPagination");
        Directory.CreateDirectory(folderPath);

        GenerateGetWithPaginationDto(entityName, entityPlural, folderPath);
        GenerateGetWithPaginationQueryFile(entityName, entityPlural, folderPath);
        GenerateGetWithPaginationValidator(entityName, entityPlural, folderPath);
    }

    static void GenerateGetWithPaginationDto(string entityName, string entityPlural, string path)
    {
        string fileName = $"Get{entityPlural}WithPaginationDto.cs";
        string filePath = Path.Combine(path, fileName);

        string content = $@"using Domain.Entities;

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

    static void GenerateGetWithPaginationQueryFile(string entityName, string entityPlural, string path)
    {
        string fileName = $"Get{entityPlural}WithPaginationQuery.cs";
        string filePath = Path.Combine(path, fileName);

        string content = $@"using Microsoft.Extensions.Logging;
using Application.Common.Interfaces.IRepositories;
using Application.Common.Models;
using Application.Utilities;
using AutoMapper.QueryableExtensions;

namespace Application.{entityPlural}.Queries.Get{entityPlural}WithPagination
{{
    public class Get{entityPlural}WithPaginationQuery : IRequest<PaginatedList<Get{entityPlural}WithPaginationDto>>
    {{
        public int PageNumber {{ get; init; }} = 1;
        public int PageSize {{ get; init; }} = 10;
        public string? SearchText {{ get; set; }}
        public List<FilterCriteria> Filters {{ get; set; }} = new();
    }}

    public class Get{entityPlural}WithPaginationQueryHandler : IRequestHandler<Get{entityPlural}WithPaginationQuery, PaginatedList<Get{entityPlural}WithPaginationDto>>
    {{
        private readonly ILogger<Get{entityPlural}WithPaginationQueryHandler> _logger;
        private readonly IMapper _mapper;
        private readonly I{entityName}Repository _repository;

        public Get{entityPlural}WithPaginationQueryHandler(ILogger<Get{entityPlural}WithPaginationQueryHandler> logger,
                                                           IMapper mapper,
                                                           I{entityName}Repository repository)
        {{
            _logger = logger;
            _mapper = mapper;
            _repository = repository;
        }}

        public async Task<PaginatedList<Get{entityPlural}WithPaginationDto>> Handle(Get{entityPlural}WithPaginationQuery request, CancellationToken cancellationToken)
        {{
            var query = _repository.GetAll();

            if (!string.IsNullOrWhiteSpace(request.SearchText))
                query = query.Where(x => x.Name.ToLower().Contains(request.SearchText.ToLower()));

            var result = await query
                .ProjectTo<Get{entityPlural}WithPaginationDto>(_mapper.ConfigurationProvider)
                .ApplyFilters(request.Filters)
                .OrderBy(x => x.Name)
                .PaginatedListAsync(request.PageNumber, request.PageSize);

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

        string content = $@"using Microsoft.Extensions.Logging;

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



    static void GenerateNeededDtos(string entityName, string entityPlural, List<(string Type, string Name)> properties, string solutionDir)
    {
        var dtoPath = Path.Combine(solutionDir, "Api", "NeededDto", entityName);
        Directory.CreateDirectory(dtoPath);

        GenerateUpdateCommandDto(entityName, dtoPath, properties);
        GenerateGetWithPaginationQueryDto(entityName, entityPlural, dtoPath);
    }
    static void GenerateUpdateCommandDto(string entityName, string path, List<(string Type, string Name)> properties)
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
    static void GenerateGetWithPaginationQueryDto(string entityName, string entityPlural, string path)
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


    static void AddRoutesToApiRoutes(string entityName, string entityPlural, string solutionDir)
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
        int insertIndex = content.LastIndexOf("}") - 2;

        if (insertIndex < 0)
        {
            Console.WriteLine("❌ Failed to find insertion point in ApiRoutes.cs");
            return;
        }

        content = content.Insert(insertIndex, "\n\n" + routeClass + "\n");
        File.WriteAllText(filePath, content);

        Console.WriteLine($"✅ ApiRoutes updated with {entityName} routes.");
    }


    static void GenerateController(string entityName, string entityPlural, List<(string Type, string Name)> properties, string solutionDir)
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


    static void GenerateBaseDto(string entityName, string entityPlural, List<(string Type, string Name)> properties, string solutionDir)
    {
        var filePath = Path.Combine(solutionDir, "Application", entityPlural, $"{entityName}BaseDto.cs");

        if (File.Exists(filePath))
        {
            Console.WriteLine($"ℹ️ {entityName}BaseDto.cs already exists. Skipping...");
            return;
        }

        var props = string.Join(Environment.NewLine, properties.Select(p =>
            $"        public {p.Type} {p.Name} {{ get; set; }}"));

        string content = $@"using System;

namespace Application.{entityPlural}
{{
    public class {entityName}BaseDto
    {{
        public Guid Id {{ get; set; }}
{props}
    }}
}}";

        File.WriteAllText(filePath, content);
        Console.WriteLine($"✅ {entityName}BaseDto.cs created.");
    }


}
