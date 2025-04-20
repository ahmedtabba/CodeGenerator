using SharedClasses;
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
        public static void GenerateCreateCommand(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties)
        {
            string className = $"Create{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");

            var props = string.Join(Environment.NewLine, properties.Select(p => $"\t\tpublic {p.Type} {p.Name} {{ get; set; }}"));

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
        public static void GenerateCreateCommandValidator(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties)
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
            StringBuilder rulesStore = new StringBuilder();
            foreach (var item in properties)
            {
                string rule = GeneratePropertyRules(item);
                rulesStore.AppendLine(rule);
                rulesStore.AppendLine();
            }
            string rules = string.Join(Environment.NewLine, rulesStore.ToString());

            string content = $@"
using FluentValidation;

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
        public static void GenerateUpdateCommand(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties)
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
        public static void GenerateUpdateCommandValidator(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties)
        {
            string className = $"Update{entityName}CommandValidator";
            string commandName = $"Update{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");

            StringBuilder rulesStore = new StringBuilder();
            foreach (var item in properties)
            {
                string rule = GeneratePropertyRules(item);
                rulesStore.AppendLine(rule);
                rulesStore.AppendLine();
            }
            string rules = string.Join(Environment.NewLine, rulesStore.ToString());

            string content = $@"
using FluentValidation;
using Application.{entityPlural}.Commands.Update{entityName};

namespace Application.{entityPlural}.Commands.Update{entityName}
{{
    public class {className} : AbstractValidator<{commandName}>
    {{
        public {className}(ILogger<{className}> logger,
                           I{entityName}Repository repository)
        {{
            _logger = logger;
            _repository = repository;    
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
        public static void GenerateDeleteCommand(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties)
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
        public static void GenerateDeleteCommandValidator(string entityName, string entityPlural, string path, List<(string Type, string Name, PropertyValidation Validation)> properties)
        {
            string className = $"Delete{entityName}CommandValidator";
            string commandName = $"Delete{entityName}Command";
            string filePath = Path.Combine(path, $"{className}.cs");


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
        public static void GenerateGetByIdQuery(string entityName, string entityPlural, string path)
        {
            var folderPath = Path.Combine(path, $"Get{entityName}Query");
            Directory.CreateDirectory(folderPath);

            GenerateGetByIdDto(entityName, entityPlural, folderPath);
            GenerateGetByIdQueryFile(entityName, entityPlural, folderPath);
            GenerateGetByIdValidator(entityName, entityPlural, folderPath);
        }
        public static void GenerateGetByIdDto(string entityName, string entityPlural, string path)
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
        public static void GenerateGetByIdQueryFile(string entityName, string entityPlural, string path)
        {
            string fileName = $"Get{entityName}Query.cs";
            string filePath = Path.Combine(path, fileName);

            string content = $@"
using Microsoft.Extensions.Logging;
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
        public static void GenerateGetByIdValidator(string entityName, string entityPlural, string path)
        {
            string fileName = $"Get{entityName}QueryValidator.cs";
            string filePath = Path.Combine(path, fileName);

            string content = $@"
using FluentValidation;
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
        public static void GenerateGetWithPaginationQuery(string entityName, string entityPlural, string path)
        {
            var folderPath = Path.Combine(path, $"Get{entityPlural}WithPagination");
            Directory.CreateDirectory(folderPath);

            GenerateGetWithPaginationDto(entityName, entityPlural, folderPath);
            GenerateGetWithPaginationQueryFile(entityName, entityPlural, folderPath);
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
        public static void GenerateGetWithPaginationQueryFile(string entityName, string entityPlural, string path)
        {
            string fileName = $"Get{entityPlural}WithPaginationQuery.cs";
            string filePath = Path.Combine(path, fileName);

            string content = $@"
using Microsoft.Extensions.Logging;
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
        public static void GenerateBaseDto(string entityName, string entityPlural, List<(string Type, string Name, PropertyValidation Validation)> properties, string solutionDir)
        {
            var filePath = Path.Combine(solutionDir, "Application", entityPlural, $"{entityName}BaseDto.cs");

            if (File.Exists(filePath))
            {
                Console.WriteLine($"ℹ️ {entityName}BaseDto.cs already exists. Skipping...");
                return;
            }

            var props = string.Join(Environment.NewLine, properties.Select(p =>
                $"        public {p.Type} {p.Name} {{ get; set; }}"));

            string content = $@"
using System;

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

        private static string GeneratePropertyRules((string Type, string Name, PropertyValidation Validation) property)
        {
            var rules = new StringBuilder();

            // Start with the RuleFor declaration
            rules.AppendLine($"            RuleFor(x => x.{property.Name})");

            // Handle required validation differently based on type
            if (property.Validation.Required)
            {
                rules.Append(".");
                
                // Use NotNull() for numeric types, NotEmpty() for strings
                if (property.Type == "int" || property.Type == "decimal" || property.Type == "float" || property.Type == "double")
                {
                    rules.AppendLine($"\t\t\tNotNull().WithMessage(\"{property.Name} is required.\")");
                }
                else if (property.Type == "string")
                {
                    rules.AppendLine($"\t\t\tNotEmpty().WithMessage(\"{property.Name} is required.\")");
                }
            }

            // Handle string-specific validations
            if (property.Type == "string")
            {
                if (property.Validation.MinLength.HasValue)
                {
                    rules.AppendLine($"\t\t\t.MinimumLength({property.Validation.MinLength.Value}).WithMessage(\"Minimum Length of {property.Name} is {property.Validation.MinLength.Value} char.\")");
                }

                if (property.Validation.MaxLength.HasValue)
                {
                    rules.AppendLine($"\t\t\t.MaximumLength({property.Validation.MaxLength.Value}).WithMessage(\"Maximum Length of {property.Name} is {property.Validation.MaxLength.Value} char.\")");
                }
            }
            // Handle numeric validations
            else if (property.Type == "int" || property.Type == "decimal" || property.Type == "float" || property.Type == "double")
            {
                if (property.Validation.MinRange.HasValue)
                {
                    rules.AppendLine($"\t\t\t.GreaterThanOrEqualTo({property.Validation.MinRange.Value}).WithMessage(\"{property.Name} must be Greater Than Or Equal To {property.Validation.MinRange}.\")");
                }

                if (property.Validation.MaxRange.HasValue)
                {
                    rules.AppendLine($"\t\t\t.LessThanOrEqualTo({property.Validation.MaxRange.Value}).WithMessage(\"{property.Name} must Less Than Or Equal To {property.Validation.MaxRange}.\")");
                }
            }
            rules.AppendLine();
            //Add ;
            if (!string.IsNullOrWhiteSpace(rules.ToString().TrimEnd()))
            {
                rules.Append(";");
            }

            return rules.ToString();
        }
    }

}
