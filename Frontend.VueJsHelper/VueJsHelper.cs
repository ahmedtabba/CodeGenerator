using SharedClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;

namespace Frontend.VueJsHelper
{
    public class VueJsHelper
    {
        public static string VueJsSolutionPath = "F:\\Boulevard\\baseFrontTemplateV0\\baseFrontTemplateV0\\src"; // ضع المسار الجذري لمشروع Vue هنا

        public static void GenerateStoreFile(string entityName, SharedClasses.Properties properties,List<string> notGeneratedTableProperties,List<string> hiddenTableProperties, List<Relation> relations,string srcDir)
        {
            if(srcDir.Length == 0)
                throw new Exception("من فضلك ادخل المسار الجذري لمشروع Vue");
            string fileName = $"{entityName}Store";
            string filePath = Path.Combine(srcDir,"store",$"{fileName}.js");

            var entityLower = char.ToLower(entityName[0]) + entityName.Substring(1);
            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";
            string entityPluralLower = char.ToLower(entityPlural[0]) + entityPlural.Substring(1);
            string capitalEntityPlural = entityPlural.ToUpper();

            var storeName = $"use{entityName}Store";

            var restEndpoint = entityPluralLower;
            var initialStateBuilder = new StringBuilder();
            var constItemBuilder = new StringBuilder();
            var requiredChecks = new StringBuilder();
            List<string> allColumns = new List<string>();
            List<string> defaultColumns = new List<string>();
            foreach (var prop in properties.PropertiesList)
            {
                string camelCasePropName = char.ToLower(prop.Name[0]) + prop.Name.Substring(1);
                if (!notGeneratedTableProperties.Any(p => p == prop.Name) )
                {
                    allColumns.Add($"'{camelCasePropName}'");
                    if (!hiddenTableProperties.Any(p => p == prop.Name))
                    {
                        defaultColumns.Add($"'{camelCasePropName}'");
                    }
                }

                initialStateBuilder.AppendLine($"        {camelCasePropName}: {GetDefaultValue(prop.Type)},");
                constItemBuilder.AppendLine($"                {camelCasePropName}: this.{camelCasePropName},");
                if (prop.Validation != null && prop.Validation.Required)
                {
                    if(prop.Type == "string" || prop.Type.Contains("Date") || prop.Type.Contains("Time"))
                    {
                        requiredChecks.Append($@"
            if (!this.{camelCasePropName}) {{
                errors.{camelCasePropName} = true;
            }}");
                    }
                    else
                    {
                        requiredChecks.Append($@"
            if (this.{camelCasePropName} === null) {{
                errors.{camelCasePropName} = true;
            }}");
                    }
                    
                }
            }

            foreach (var rel in relations)
            {
                if (rel.Type == RelationType.ManyToOne || rel.Type == RelationType.ManyToOneNullable
                      || rel.Type == RelationType.OneToOne || rel.Type == RelationType.OneToOneNullable)
                {
                    string camelCasePropName = char.ToLower(rel.RelatedEntity[0]) + rel.RelatedEntity.Substring(1);
                    if (rel.IsGeneratedInTable)
                    {
                        allColumns.Add($"'{camelCasePropName + rel.DisplayedProperty}'");
                        if (!rel.HiddenInTable)
                        {
                            defaultColumns.Add($"'{camelCasePropName + rel.DisplayedProperty}'");
                        }
                    }
                    initialStateBuilder.AppendLine($"        {camelCasePropName}Id: null,");
                    constItemBuilder.AppendLine($"                {camelCasePropName}Id: this.{camelCasePropName}Id,");
                }
                if (rel.Type == RelationType.OneToOneSelfJoin)
                {
                    string camelCasePropName = char.ToLower(rel.RelatedEntity[0]) + rel.RelatedEntity.Substring(1);
                    if (rel.IsGeneratedInTable)
                    {
                        allColumns.Add($"'{camelCasePropName + "Parent" + rel.DisplayedProperty}'");
                        if (!rel.HiddenInTable)
                        {
                            defaultColumns.Add($"'{camelCasePropName + "Parent" + rel.DisplayedProperty}'");
                        }
                    }
                    initialStateBuilder.AppendLine($"        {camelCasePropName}ParentId: null,");
                    constItemBuilder.AppendLine($"                {camelCasePropName}ParentId: this.{camelCasePropName}ParentId,");
                }
                if (rel.Type == RelationType.ManyToMany)
                {
                    string camelCasePropNameIds = char.ToLower(rel.RelatedEntity[0]) + rel.RelatedEntity.Substring(1) + "Ids";
                    string camelCasePropName = char.ToLower(rel.RelatedEntity[0]) + rel.RelatedEntity.Substring(1);
                    string displayedPropertyPlural = rel.DisplayedProperty.EndsWith("y") ? rel.DisplayedProperty[..^1] + "ies" : rel.DisplayedProperty + "s";
                    if (rel.IsGeneratedInTable)
                    {
                        allColumns.Add($"'{camelCasePropName + displayedPropertyPlural}'");
                        if (!rel.HiddenInTable)
                        {
                            defaultColumns.Add($"'{camelCasePropName + displayedPropertyPlural}'");
                        }
                    }
                    initialStateBuilder.AppendLine($"        {camelCasePropNameIds}: [],");
                    constItemBuilder.AppendLine($"                {camelCasePropNameIds}: this.{camelCasePropNameIds},");
                }
                if (rel.Type == RelationType.ManyToOne || rel.Type == RelationType.OneToOne)
                {
                    string camelCasePropName = char.ToLower(rel.RelatedEntity[0]) + rel.RelatedEntity.Substring(1);
                    requiredChecks.Append($@"
            if (this.{camelCasePropName}Id === null) {{
                errors.{camelCasePropName}Id = true;
            }}");
                }
            }

            var content = $@"
import {{ SAVE, SAVE_FAIL, SAVE_ITEM, VALIDATE_FORM }} from '@/utils/StoreConstant';
import {{generalActions,generalState}} from './GeneralStore';
import I18n from '@/config/i18n';
import {{defineStore}} from 'pinia';
import * as generalBackend from '@/backend/Backend';
import {{{capitalEntityPlural}_ROUTE as PAGE_ROUTE}} from '@/utils/Constants';

const REST_ENDPOINT = (id) => `{restEndpoint}${{id ? '/' + id : ''}}`;

// all columns options
const ALL_COLUMNS = [{string.Join(',',allColumns)}];

// default columns that will be displayed
const DEFAULT_COLUMNS = [{string.Join(',', defaultColumns)}];

const INITIAL_STATE = {{
    ...generalState(),

    // columns
    allColumns: ALL_COLUMNS,
    defaultColumns: DEFAULT_COLUMNS,

{initialStateBuilder.ToString().TrimEnd()}
}};

export const {storeName} = defineStore('{entityLower}', {{
    state: () => ({{...INITIAL_STATE, selectedColumns: [...DEFAULT_COLUMNS]}}),
    actions: {{
        ...generalActions(INITIAL_STATE, REST_ENDPOINT, PAGE_ROUTE),
        [VALIDATE_FORM]() {{
            const errors = {{}};

            // validation rules
{requiredChecks}
            // validation end

            // if no errors return true, else false
            this.validationErrors = errors;
            return Object.keys(errors).length === 0;
        }},
        async [SAVE_ITEM]() {{
            this[SAVE]();
            const item = {{
            id: this.id,
{constItemBuilder}
            }};
            // validation
            const isValid = this.validateForm();
            if (!isValid) {{
                this.sendErrorMessage(I18n.global.t('message.pleaseFillAllRequiredFields'));
                this[SAVE_FAIL]();
                return;
            }}

            if (item.id === null) {{
                generalBackend.save(this, REST_ENDPOINT(), PAGE_ROUTE(), item);
            }} else {{
                generalBackend.update(this, REST_ENDPOINT(item.id), PAGE_ROUTE(), item);
            }}
        }}
    }},
    persist: {{
        pick: ['selectedColumns']
    }}
}});";

            Directory.CreateDirectory(Path.Combine(srcDir, "store"));
            File.WriteAllText(filePath, content);
        }
        public static void GenerateStoreFileWithAssets(string entityName, SharedClasses.Properties properties,List<string> notGeneratedTableProperties,List<string> hiddenTableProperties, List<Relation> relations,string srcDir)
        {
            if(srcDir.Length == 0)
                throw new Exception("من فضلك ادخل المسار الجذري لمشروع Vue");
            string fileName = $"{entityName}Store";
            string filePath = Path.Combine(srcDir,"store",$"{fileName}.js");

            var entityLower = entityName.GetCamelCaseName();
            string entityPlural = entityName.GetPluralName();
            string entityPluralLower = entityPlural.GetCamelCaseName();
            string capitalEntityPlural = entityPlural.GetCapitalName();

            var storeName = $"use{entityName}Store";

            var restEndpoint = entityPluralLower;
            var initialStateBuilder = new StringBuilder();
            var dataAppendBuilder = new StringBuilder();
            var dataAppendIfCheckBuilder = new StringBuilder();
            var requiredChecks = new StringBuilder();
            List<string> allColumns = new List<string>();
            List<string> defaultColumns = new List<string>();
            string? assetDefine = null;
            string? assetListDefine = null;
            string? videoDefine = null;
            string? vedioListDefine = null;
            foreach (var prop in properties.PropertiesList)
            {
                if(prop.Type != "GPG" && prop.Type != "PNGs" && prop.Type != "VD" && prop.Type != "VDs")
                {
                    string camelCasePropName = prop.Name.GetCamelCaseName();
                    if (!notGeneratedTableProperties.Any(p => p == prop.Name))
                    {
                        allColumns.Add($"'{camelCasePropName}'");
                        if (!hiddenTableProperties.Any(p => p == prop.Name))
                        {
                            defaultColumns.Add($"'{camelCasePropName}'");
                        }
                    }

                    initialStateBuilder.AppendLine($"        {camelCasePropName}: {GetDefaultValue(prop.Type)},");

                    if (prop.Type.Contains("Date") || prop.Type.Contains("Time"))
                    {
                        dataAppendBuilder.AppendLine($"            data.append('{camelCasePropName}', formatDate(this.{camelCasePropName}));");
                    }
                    else if(prop.Type == "string" || prop.Type == "string?")
                    {
                        dataAppendBuilder.AppendLine($"            data.append('{camelCasePropName}', this.{camelCasePropName});");
                    }
                    else
                    {
                        dataAppendBuilder.AppendLine($"            data.append('{camelCasePropName}', String(this.{camelCasePropName}));");
                    }

                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        if (prop.Type == "string" || prop.Type.Contains("Date") || prop.Type.Contains("Time"))
                        {
                            requiredChecks.Append($@"
            if (!this.{camelCasePropName}) {{
                errors.{camelCasePropName} = true;
            }}");
                        }
                        else
                        {
                            requiredChecks.Append($@"
            if (this.{camelCasePropName} === null) {{
                errors.{camelCasePropName} = true;
            }}");
                        }

                    }
                }
                else
                {
                    switch(prop.Type)
                    {
                        case "GPG":
                            assetDefine = $@"
    {prop.Name.GetCamelCaseName()}: null,
    {prop.Name.GetCamelCaseName()}Url: null,
    delete{prop.Name}: false,";

                            if (!notGeneratedTableProperties.Any(p => p == prop.Name))
                            {
                                allColumns.Add($"'{prop.Name.GetCamelCaseName()}'");
                                if (!hiddenTableProperties.Any(p => p == prop.Name))
                                {
                                    defaultColumns.Add($"'{prop.Name.GetCamelCaseName()}'");
                                }
                            }

                            if (prop.Validation != null && prop.Validation.Required)
                            {
                                requiredChecks.Append($@"
            if (!this.{prop.Name.GetCamelCaseName()} && !this.{prop.Name.GetCamelCaseName()}Url ) {{
                errors.{prop.Name.GetCamelCaseName()} = true;
            }}");
                            }

                            dataAppendBuilder.AppendLine($"            data.append('{prop.Name.GetCamelCaseName()}FormFile', this.{prop.Name.GetCamelCaseName()});");
                            dataAppendBuilder.AppendLine($"            data.append('{prop.Name.GetCamelCaseName()}Url', this.{prop.Name.GetCamelCaseName()}Url);");

                            dataAppendIfCheckBuilder.AppendLine($@"
            if (this.{prop.Name.GetCamelCaseName()}) {{
                data.append('delete{prop.Name}', 'false');
            }} else {{
                data.append('delete{prop.Name}', String(this.delete{prop.Name}));
            }}");
                            break;

                        case "PNGs":
                            assetListDefine = $@"
    {prop.Name.GetCamelCaseName()}: [],
    {prop.Name.GetCamelCaseName()}Urls: [],
    deleted{prop.Name}Urls: [],";

                            if (!notGeneratedTableProperties.Any(p => p == prop.Name))
                            {
                                allColumns.Add($"'{prop.Name.GetCamelCaseName()}'");
                                if (!hiddenTableProperties.Any(p => p == prop.Name))
                                {
                                    defaultColumns.Add($"'{prop.Name.GetCamelCaseName()}'");
                                }
                            }
                            if (prop.Validation != null && prop.Validation.Required)
                            {
                                requiredChecks.Append($@"
            if (!this.{prop.Name.GetCamelCaseName()}.length === 0 && !this.{prop.Name.GetCamelCaseName()}Urls.length ) {{
                errors.{prop.Name.GetCamelCaseName()} = true;
            }}");
                            }

                            dataAppendBuilder.AppendLine($"            this.{prop.Name.GetCamelCaseName()}.forEach((asset) => data.append('{prop.Name.GetCamelCaseName()}FormFiles', asset));");
                            dataAppendBuilder.AppendLine($"            this.deleted{prop.Name}Urls.forEach((url) => data.append('deleted{prop.Name}Urls', url));");

                            break;

                        case "VD":
                            videoDefine = $@"
    {prop.Name.GetCamelCaseName()}: null,
    {prop.Name.GetCamelCaseName()}Url: null,
    delete{prop.Name}: false,";

                            if (!notGeneratedTableProperties.Any(p => p == prop.Name))
                            {
                                allColumns.Add($"'{prop.Name.GetCamelCaseName()}'");
                                if (!hiddenTableProperties.Any(p => p == prop.Name))
                                {
                                    defaultColumns.Add($"'{prop.Name.GetCamelCaseName()}'");
                                }
                            }

                            if (prop.Validation != null && prop.Validation.Required)
                            {
                                requiredChecks.Append($@"
            if (!this.{prop.Name.GetCamelCaseName()} && !this.{prop.Name.GetCamelCaseName()}Url ) {{
                errors.{prop.Name.GetCamelCaseName()} = true;
            }}");
                            }

                            dataAppendBuilder.AppendLine($"            data.append('{prop.Name.GetCamelCaseName()}FormFile', this.{prop.Name.GetCamelCaseName()});");
                            dataAppendBuilder.AppendLine($"            data.append('{prop.Name.GetCamelCaseName()}Url', this.{prop.Name.GetCamelCaseName()}Url);");

                            dataAppendIfCheckBuilder.AppendLine($@"
            if (this.{prop.Name.GetCamelCaseName()}) {{
                data.append('delete{prop.Name}', 'false');
            }} else {{
                data.append('delete{prop.Name}', String(this.delete{prop.Name}));
            }}");
                            break;

                        case "VDs":
                            vedioListDefine = $@"
    {prop.Name.GetCamelCaseName()}: [],
    {prop.Name.GetCamelCaseName()}Urls: [],
    deleted{prop.Name}Urls: [],";

                            if (!notGeneratedTableProperties.Any(p => p == prop.Name))
                            {
                                allColumns.Add($"'{prop.Name.GetCamelCaseName()}'");
                                if (!hiddenTableProperties.Any(p => p == prop.Name))
                                {
                                    defaultColumns.Add($"'{prop.Name.GetCamelCaseName()}'");
                                }
                            }
                            if (prop.Validation != null && prop.Validation.Required)
                            {
                                requiredChecks.Append($@"
            if (!this.{prop.Name.GetCamelCaseName()}.length === 0 && !this.{prop.Name.GetCamelCaseName()}Urls.length ) {{
                errors.{prop.Name.GetCamelCaseName()} = true;
            }}");
                            }

                            dataAppendBuilder.AppendLine($"            this.{prop.Name.GetCamelCaseName()}.forEach((asset) => data.append('{prop.Name.GetCamelCaseName()}FormFiles', asset));");
                            dataAppendBuilder.AppendLine($"            this.deleted{prop.Name}Urls.forEach((url) => data.append('deleted{prop.Name}Urls', url));");

                            break;

                        default:
                            break;
                    }    
                }
                
            }

            foreach (var rel in relations)
            {
                if (rel.Type == RelationType.ManyToOne || rel.Type == RelationType.ManyToOneNullable
                      || rel.Type == RelationType.OneToOne || rel.Type == RelationType.OneToOneNullable)
                {
                    string camelCasePropName = rel.RelatedEntity.GetCamelCaseName();
                    if (rel.IsGeneratedInTable)
                    {
                        allColumns.Add($"'{camelCasePropName + rel.DisplayedProperty}'");
                        if (!rel.HiddenInTable)
                        {
                            defaultColumns.Add($"'{camelCasePropName + rel.DisplayedProperty}'");
                        }
                    }
                    initialStateBuilder.AppendLine($"        {camelCasePropName}Id: null,");
                    dataAppendBuilder.AppendLine($"            data.append('{camelCasePropName}Id', this.{camelCasePropName}Id);");
                }
                if (rel.Type == RelationType.OneToOneSelfJoin)
                {
                    string camelCasePropName = rel.RelatedEntity.GetCamelCaseName();
                    if (rel.IsGeneratedInTable)
                    {
                        allColumns.Add($"'{camelCasePropName + "Parent" + rel.DisplayedProperty}'");
                        if (!rel.HiddenInTable)
                        {
                            defaultColumns.Add($"'{camelCasePropName + "Parent" + rel.DisplayedProperty}'");
                        }
                    }
                    initialStateBuilder.AppendLine($"        {camelCasePropName}ParentId: null,");
                    dataAppendBuilder.AppendLine($"            data.append('{camelCasePropName}ParentId', String(this.{camelCasePropName}ParentId));");
                }
                if (rel.Type == RelationType.ManyToMany)
                {
                    string camelCasePropNameIds = rel.RelatedEntity.GetCamelCaseName() + "Ids";
                    string camelCasePropName = rel.RelatedEntity.GetCamelCaseName();
                    string displayedPropertyPlural = rel.DisplayedProperty.GetPluralName();
                    if (rel.IsGeneratedInTable)
                    {
                        allColumns.Add($"'{camelCasePropName + displayedPropertyPlural}'");
                        if (!rel.HiddenInTable)
                        {
                            defaultColumns.Add($"'{camelCasePropName + displayedPropertyPlural}'");
                        }
                    }
                    initialStateBuilder.AppendLine($"        {camelCasePropNameIds}: [],");
                    dataAppendBuilder.AppendLine($"            this.{camelCasePropNameIds}.forEach((id) => data.append('{camelCasePropNameIds}', id));");
                }
                if (rel.Type == RelationType.ManyToOne || rel.Type == RelationType.OneToOne)
                {
                    string camelCasePropName = rel.RelatedEntity.GetCamelCaseName();
                    requiredChecks.Append($@"
            if (this.{camelCasePropName}Id === null) {{
                errors.{camelCasePropName}Id = true;
            }}");
                }
            }

            var content = $@"
import {{ SAVE, SAVE_FAIL, SAVE_ITEM, VALIDATE_FORM }} from '@/utils/StoreConstant';
import {{generalActions,generalState}} from '@/store/GeneralStore';
import I18n from '@/config/i18n';
import {{defineStore}} from 'pinia';
import * as generalBackend from '@/backend/Backend';
import {{{capitalEntityPlural}_ROUTE as PAGE_ROUTE}} from '@/utils/Constants';
import {{formatDate}} from '@/utils/utils';

const REST_ENDPOINT = (id) => `{restEndpoint}${{id ? '/' + id : ''}}`;

// all columns options
const ALL_COLUMNS = [{string.Join(',',allColumns)}];

// default columns that will be displayed
const DEFAULT_COLUMNS = [{string.Join(',', defaultColumns)}];

const INITIAL_STATE = {{
    ...generalState(),

    // columns
    allColumns: ALL_COLUMNS,
    defaultColumns: DEFAULT_COLUMNS,

{initialStateBuilder.ToString().TrimEnd()}
{assetDefine}
{assetListDefine}
{videoDefine}
{vedioListDefine}
}};

export const {storeName} = defineStore('{entityLower}', {{
    state: () => ({{...INITIAL_STATE, selectedColumns: [...DEFAULT_COLUMNS]}}),
    actions: {{
        ...generalActions(INITIAL_STATE, REST_ENDPOINT, PAGE_ROUTE),
        [VALIDATE_FORM]() {{
            const errors = {{}};

            // validation rules
{requiredChecks}
            // validation end

            // if no errors return true, else false
            this.validationErrors = errors;
            return Object.keys(errors).length === 0;
        }},
        async [SAVE_ITEM]() {{
            this[SAVE]();
            // validation
            const isValid = this.validateForm();
            if (!isValid) {{
                this.sendErrorMessage(I18n.global.t('message.pleaseFillAllRequiredFields'));
                this[SAVE_FAIL]();
                return;
            }}
            const data = new FormData();
{dataAppendBuilder}
{dataAppendIfCheckBuilder}
            if (this.id === null) {{
                generalBackend.saveFormData(this, REST_ENDPOINT(), PAGE_ROUTE(), data);
            }} else {{
                generalBackend.updateFormData(this, REST_ENDPOINT(this.id), PAGE_ROUTE(), data);
            }}
        }}
    }},
    persist: {{
        pick: ['selectedColumns']
    }}
}});";

            Directory.CreateDirectory(Path.Combine(srcDir, "store"));
            File.WriteAllText(filePath, content);
        }

        private static object GetDefaultValue(string type)
        {
            //Returns the default value for the given type
            var typeWithoutNullable = type.TrimEnd('?');
            switch (typeWithoutNullable)
            {
                case "string":
                    return "\'\'";
                case "bool":
                    return "false";
                case "DateTime":
                    return "new Date()";
                case "DateTimeOffset":
                    return "new Date()";
                case "char":
                    return "\'\'";
                case "Guid":
                    return "null";
                case "List of":
                    return "[]";
                case "GPG":
                    return "null";
                case "VD":
                    return "null";
                case "PNGs":
                    return "[]";
                case "DateOnly":
                    return "new Date()";
                case "TimeOnly":
                    return "new time()";
                default:
                    return "null";
            }
        }

        public static void UpdateConstantsJs(string entityName, string srcDir)
        {
            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";
            string capitalEntityPlural = entityPlural.ToUpper();
            string entityPluralLower = char.ToLower(entityPlural[0]) + entityPlural.Substring(1);
            string constantsPath = Path.Combine(srcDir, "utils", "Constants.js");
            if (!File.Exists(constantsPath))
            {
                return;
            }

            string _ROUTE = $"export const {capitalEntityPlural}_ROUTE = (id) => `/{entityPluralLower}${{id ? '/' + id : ''}}`;" +
                $"\n//Add ROUTES Here";

            var lines = File.ReadAllLines(constantsPath).ToList();
            var index = lines.FindIndex(line => line.Contains("//Add ROUTES Here"));

            if (index >= 0)
            {
                lines[index] = _ROUTE;
                File.WriteAllLines(constantsPath, lines);
            }
        }

        public static void UpdateRouterIndexJs(string entityName, string srcDir)
        {
            var entityLower = char.ToLower(entityName[0]) + entityName.Substring(1);
            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";
            string capitalEntityPlural = entityPlural.ToUpper();
            string entityPluralLower = char.ToLower(entityPlural[0]) + entityPlural.Substring(1);
            string routerIndexPath = Path.Combine(srcDir, "router", "index.js");
            if (!File.Exists(routerIndexPath))
            {
                return;
            }

            string router = $@"
                {{
                    path: '{entityPluralLower}',
                    component: {{ render: () => h(resolveComponent('router-view')) }},
                    children: [
                        {{ path: '', name: '{entityPlural}', component: () => import('@/views/{entityLower}/{entityPlural}.vue') }},
                        {{ path: ':id', name: '{entityName} Details', component: () => import('@/views/{entityLower}/{entityName}.vue') }}
                    ]
                }}," +
                $"\n                //Add router Here";

            var lines = File.ReadAllLines(routerIndexPath).ToList();
            var index = lines.FindIndex(line => line.Contains("//Add router Here"));

            if (index >= 0)
            {
                lines[index] = router;
                File.WriteAllLines(routerIndexPath, lines);
            }
        }

        public static void UpdateAppMenu(string entityName, string srcDir)
        {
            var entityLower = char.ToLower(entityName[0]) + entityName.Substring(1);
            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";
            string capitalEntityPlural = entityPlural.ToUpper();
            string entityPluralLower = char.ToLower(entityPlural[0]) + entityPlural.Substring(1);
            string appMenuPath = Path.Combine(srcDir, "layout", "AppMenu.vue");
            if (!File.Exists(appMenuPath))
            {
                return;
            }

            string item = $"\t\t\t{{ label: t('title.{entityPluralLower}'), icon: 'pi pi-fw pi-star-fill', to: '/{entityPluralLower}' }}," + $"\n            //Add Menu Here";

            var lines = File.ReadAllLines(appMenuPath).ToList();
            var index = lines.FindIndex(line => line.Contains("//Add Menu Here"));

            if (index >= 0)
            {
                lines[index] = item;
                File.WriteAllLines(appMenuPath, lines);
            }
        }

        public static void GenerateTableView(string entityName, string srcDir, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, List<string> notGeneratedTableProperties, List<string> hiddenTableProperties, List<Relation> relations)
        {
            var entityLower = entityName.GetCamelCaseName();
            string entityPlural = entityName.GetPluralName();
            string capitalEntityPlural = entityPlural.GetCapitalName();
            string entityPluralLower = entityPlural.GetCamelCaseName();
            string viewDirectory = Path.Combine(srcDir, "views", $"{entityLower}");
            Directory.CreateDirectory(viewDirectory);
            string fileTableName = entityPlural;
            string viewTablePath = Path.Combine(viewDirectory, $"{entityPlural}.vue");

            StringBuilder enumFilters = new StringBuilder();
            foreach (var enm in enumProps)
            {
                List<string> st = new List<string>();
                for (int i = 0; i < enm.enumValues.Count; i++)
                {
                    var enmValueLower = char.ToLower(enm.enumValues[i][0]) + enm.enumValues[i].Substring(1);
                    string line = $"    {{label: t('field.{enmValueLower}'), value: '{i}' }},";
                    st.Add(line);
                }
                enumFilters.AppendLine($@"
const filter{entityName}{enm.prop}Options = [
{string.Join(Environment.NewLine,st)}
];");
            }


            //List<string> filterSectionSortFields = new List<string>();
            List<string> filterSectionGlobalFields = new List<string>();
            List<string> filterSectionInitFilters = new List<string>();
            foreach (var prop in properties)
            {
                if (!notGeneratedTableProperties.Any(p => p == prop.Name))
                {
                    var typeWithoutNullable = prop.Type.TrimEnd('?');
                    if (typeWithoutNullable == "string")
                    {
                        var propLower = prop.Name.GetCamelCaseName();
                        //string st = $"    {{field: '{propLower}', order: 0 }},";
                        //filterSectionSortFields.Add(st);
                        filterSectionGlobalFields.Add($"'{propLower}'");
                        filterSectionInitFilters.Add($"    {propLower}: {{ operator: FilterOperator.AND, constraints: [{{ value: null, matchMode: FilterMatchMode.STARTS_WITH }}] }},");
                    }
                    if (typeWithoutNullable == "bool")
                    {
                        var propLower = char.ToLower(prop.Name[0]) + prop.Name.Substring(1);
                        //string st = $"    {{field: '{propLower}', order: 0 }},";
                        //filterSectionSortFields.Add(st);
                        filterSectionGlobalFields.Add($"'{propLower}'");
                        filterSectionInitFilters.Add($"    {propLower}: {{ operator: FilterOperator.AND, constraints: [{{ value: null, matchMode: FilterMatchMode.EQUALS }}] }},");
                    }
                    if (typeWithoutNullable == "double" || typeWithoutNullable == "decimal" || typeWithoutNullable == "float")
                    {
                        var propLower = char.ToLower(prop.Name[0]) + prop.Name.Substring(1);
                        //string st = $"    {{field: '{propLower}', order: 0 }},";
                        //filterSectionSortFields.Add(st);
                        filterSectionGlobalFields.Add($"'{propLower}'");
                        filterSectionInitFilters.Add($"    {propLower}: {{ operator: FilterOperator.AND, constraints: [{{ value: null, matchMode: FilterMatchMode.EQUALS }}] }},");
                    }
                    if (typeWithoutNullable == "int" && enumProps.Any(ep => ep.prop == prop.Name))//enum case
                    {
                        var propLower = char.ToLower(prop.Name[0]) + prop.Name.Substring(1);
                        //string st = $"    {{field: '{propLower}', order: 0 }},";
                        //filterSectionSortFields.Add(st);
                        filterSectionGlobalFields.Add($"'{propLower}'");
                        filterSectionInitFilters.Add($"    {propLower}: {{ operator: FilterOperator.AND, constraints: [{{ value: null, matchMode: FilterMatchMode.EQUALS }}] }},");
                    }
                    if (typeWithoutNullable == "int" && !enumProps.Any(ep => ep.prop == prop.Name))//int property case
                    {
                        var propLower = char.ToLower(prop.Name[0]) + prop.Name.Substring(1);
                        //string st = $"    {{field: '{propLower}', order: 0 }},";
                        //filterSectionSortFields.Add(st);
                        filterSectionGlobalFields.Add($"'{propLower}'");
                        filterSectionInitFilters.Add($"    {propLower}: {{ operator: FilterOperator.AND, constraints: [{{ value: null, matchMode: FilterMatchMode.EQUALS }}] }},");
                    }
                    if (typeWithoutNullable.Contains("Date") || typeWithoutNullable.Contains("Time"))
                    {
                        var propLower = char.ToLower(prop.Name[0]) + prop.Name.Substring(1);
                        //string st = $"    {{field: '{propLower}', order: 0 }},";
                        //filterSectionSortFields.Add(st);
                        filterSectionGlobalFields.Add($"'{propLower}'");
                        filterSectionInitFilters.Add($"    {propLower}: {{ operator: FilterOperator.AND, constraints: [{{ value: null, matchMode: FilterMatchMode.DATE_IS }}] }},");
                    }
                }
                
            }
            StringBuilder relationImports = new StringBuilder();
            StringBuilder relationConsts = new StringBuilder();
            foreach ( var rel in relations )
            {
                if (rel.IsGeneratedInTable)
                {
                    if (rel.Type == RelationType.OneToOneSelfJoin || rel.Type == RelationType.ManyToOne || rel.Type == RelationType.ManyToOneNullable
                      || rel.Type == RelationType.OneToOne || rel.Type == RelationType.OneToOneNullable)
                    {
                        var propLower = rel.Type != RelationType.OneToOneSelfJoin ? rel.RelatedEntity.GetCamelCaseName() + $"{rel.DisplayedProperty}"
                            : rel.RelatedEntity.GetCamelCaseName() + $"Parent{rel.DisplayedProperty}";
                        //string st = $"    {{field: '{propLower}', order: 0 }},";
                        //filterSectionSortFields.Add(st);
                        filterSectionGlobalFields.Add($"'{propLower}'");
                        filterSectionInitFilters.Add($"    {propLower}: {{ operator: FilterOperator.AND, constraints: [{{ value: null, matchMode: FilterMatchMode.EQUALS }}] }},");
                        string entityRelatedPlural = rel.RelatedEntity.GetPluralName();
                        string entityRelatedPluralLower = entityRelatedPlural.GetCamelCaseName();
                        relationImports.AppendLine($"import {{ use{rel.RelatedEntity}Store }} from '@/store/{rel.RelatedEntity}Store';");
                        relationConsts.AppendLine($"const {{ items: {entityRelatedPluralLower}, loading: loading{entityRelatedPlural}, search: search{entityRelatedPlural} }} = useList(use{rel.RelatedEntity}Store(), '', {{}});");
                    }
                    if (rel.Type == RelationType.ManyToMany)
                    {
                        string displayedPropPlural = rel.DisplayedProperty.GetPluralName();
                        var propLower = rel.RelatedEntity.GetCamelCaseName() + $"{displayedPropPlural}";
                        filterSectionGlobalFields.Add($"'{propLower}'");
                        filterSectionInitFilters.Add($"    {propLower}: {{ operator: FilterOperator.AND, constraints: [{{ value: null, matchMode: FilterMatchMode.CONTAINS }}] }},");
                        string entityRelatedPlural = rel.RelatedEntity.GetPluralName();
                        string entityRelatedPluralLower = entityRelatedPlural.GetCamelCaseName();
                        relationImports.AppendLine($"import {{ use{rel.RelatedEntity}Store }} from '@/store/{rel.RelatedEntity}Store';");
                        relationConsts.AppendLine($"const {{ items: {entityRelatedPluralLower}, loading: loading{entityRelatedPlural}, search: search{entityRelatedPlural} }} = useList(use{rel.RelatedEntity}Store(), '', {{}});");
                    }
                }
            }

            StringBuilder colomnBuilder = new StringBuilder();
            foreach( var prop in properties )
            {
                if (!notGeneratedTableProperties.Any(p => p == prop.Name))
                {
                    string colomn = GetTableColomnControl(entityName, prop, enumProps);
                    colomnBuilder.AppendLine(colomn);
                }
                    
            }
            string? relationColomn = GetTableColomnRelationControl(entityName, relations);
            if (relationColomn != null)
                colomnBuilder.AppendLine(relationColomn);

            string content = $@"
<script setup>
import useList from '@/composables/useList';
import {{ reactive, ref }} from 'vue';
import {{ use{entityName}Store as useStore }} from '@/store/{entityName}Store';
import {{ {capitalEntityPlural}_ROUTE as PAGE_ROUTE }} from '@/utils/Constants';
import ListTemplate from '@/components/table/ListTemplate.vue';
import {{ FilterMatchMode, FilterOperator }} from '@primevue/core/api';
{relationImports}
const {{ t, formattedDate, dataTableDateFormatter, isColumnSelected }} = useList(useStore(), PAGE_ROUTE, {{ autoLoad: false }});
{enumFilters.ToString().TrimEnd()}

const globalFields = ref([{string.Join(",", filterSectionGlobalFields)}]);
const filters = ref();
const initFilters = () => {{
    filters.value = {{
        global: {{ value: null, matchMode: FilterMatchMode.CONTAINS }},
{string.Join(Environment.NewLine, filterSectionInitFilters)}
    }};
}};
{relationConsts}
initFilters();

</script>
<template>
    <ListTemplate :pageRoute=""PAGE_ROUTE"" :use-store=""useStore""  title=""title.{entityPluralLower}"" 
                :filters=""filters""
                :init-filters=""initFilters""
                :global-filter-fields=""globalFields"">
        <template #columns>
{colomnBuilder}
        </template>
    </ListTemplate>
</template>
";
            
            File.WriteAllText(viewTablePath, content);
        }

        private static string GetTableColomnControl(string entityName ,(string Type, string Name, PropertyValidation Validation) prop, List<(string prop, List<string> enumValues)> enumProps)
        {
            var propLower = prop.Name.GetCamelCaseName();
            var typeWithoutNullable = prop.Type.TrimEnd('?');
            if (typeWithoutNullable == "string" || (typeWithoutNullable == "int" && !enumProps.Any(ep => ep.prop == prop.Name)) || typeWithoutNullable == "double" || typeWithoutNullable == "decimal" || typeWithoutNullable == "float")
            {
                return $@"
            <Column v-if=""isColumnSelected('{propLower}')"" field=""{propLower}"" :header=""$t('field.{propLower}')"" :show-add-button=""false"" :show-filter-operator=""false"" :sortable=""true"">
                <template #body=""{{ data }}"">
                    {{{{ data.{propLower} }}}}
                </template>
                <template #filter=""{{ filterModel }}"">
                    <InputText v-model=""filterModel.value"" type=""text""
                            :placeholder=""$t('search.searchBy{prop.Name}')"" />
                </template>
            </Column>";
            }
            
            if (typeWithoutNullable == "int" && enumProps.Any(ep => ep.prop == prop.Name))//enum case
            {
                return $@"
            <Column v-if=""isColumnSelected('{propLower}')"" field=""{propLower}"" :header=""$t('field.{propLower}')"" :show-add-button=""false"" :show-filter-match-modes=""false"" :show-filter-operator=""false"" :sortable=""true"" >
                <template #body=""{{ data }}"">
                    {{{{ data.{propLower} }}}}
                </template>
                <template #filter=""{{ filterModel }}"">
                        <Select v-model=""filterModel.value"" :options=""filter{entityName}{prop.Name}Options"" option-value=""value"" option-label=""label"" showClear :placeholder=""$t('field.{propLower}')""></Select>
                    </template>
            </Column>";
            }
            if (typeWithoutNullable.Contains("Date") || typeWithoutNullable.Contains("Time"))
            {
                return $@"
                <Column v-if=""isColumnSelected('{propLower}')"" field=""{propLower}"" dataType=""date"" style=""width: 80px"" :header=""$t('field.{propLower}')"" :show-add-button=""false"" :show-filter-operator=""false"" :sortable=""true"">
                    <template #body=""{{ data }}"">
                        <div class=""flex justify-center"">{{{{ formattedDate(data.{propLower}) }}}}</div>
                    </template>
                    <template #filter=""{{ filterModel }}"">
                        <DatePicker v-model=""filterModel.value"" @update:model-value=""dataTableDateFormatter(filterModel)"" dateFormat=""dd/mm/yy"" placeholder=""dd/mm/yyyy""/>
                    </template>
                </Column>";
            }
            if (typeWithoutNullable == "bool")
            {
                return $@"
            <Column v-if=""isColumnSelected('{propLower}')"" field=""{propLower}"" :header=""$t('field.{propLower}')"" :show-add-button=""false""
                    :show-filter-match-modes=""false"" :show-filter-operator=""false"" :sortable=""true"" >
                <template #body=""{{ data }}"">
                    <i class=""pi""
                       :class=""{{ 'pi-check-circle text-green-500 ': data.{propLower}, 'pi-times-circle text-red-500': !data.{propLower} }}""></i>
                </template>
                <template #filter=""{{ filterModel }}"">
                    <label for=""{propLower}"" class=""font-bold"">{{{{ $t('field.{propLower}') }}}}</label>
                    <Checkbox v-model=""filterModel.value"" :indeterminate=""filterModel.value === null"" binary
                            inputId=""{propLower}"" />
                </template>
            </Column>";
            }

            if (typeWithoutNullable == "VD" || typeWithoutNullable == "GPG" || typeWithoutNullable == "PNGs" || typeWithoutNullable == "VDs")
            {
                //TODO : handle assets states
                return null;
            }

            return $@"
            <Column v-if=""isColumnSelected('{propLower}')"" field=""{propLower}"" :header=""$t('field.{propLower}')"" :show-add-button=""false"" :show-filter-operator=""false"" :sortable=""true"">
                <template #body=""{{ data }}"">
                    {{{{ data.{propLower} }}}}
                </template>
            </Column>";
        }

        private static string GetTableColomnRelationControl(string entityName, List<Relation> relations)
        {
            StringBuilder sb = new StringBuilder();
            if (relations != null && relations.Any())
            {
                foreach (var rel in relations)
                {
                    if (rel.IsGeneratedInTable)
                    {
                        if (rel.Type == RelationType.OneToOneSelfJoin || rel.Type == RelationType.ManyToOne || rel.Type == RelationType.ManyToOneNullable
                            || rel.Type == RelationType.OneToOne || rel.Type == RelationType.OneToOneNullable)
                        {
                            string entityRelatedPlural = rel.RelatedEntity.GetPluralName();
                            string entityRelatedPluralLower = entityRelatedPlural.GetCamelCaseName();
                            var lowerRelatedEntity = rel.RelatedEntity.GetCamelCaseName();
                            var displayedProp = rel.DisplayedProperty.GetCamelCaseName();
                            var propLower = rel.Type != RelationType.OneToOneSelfJoin ? lowerRelatedEntity + $"{rel.DisplayedProperty}"
                                : lowerRelatedEntity + $"Parent{rel.DisplayedProperty}";
                            sb.AppendLine($@"
                <Column v-if=""isColumnSelected('{propLower}')"" field=""{propLower}"" :header=""$t('field.{propLower}')"" :show-add-button=""false"" :show-filter-match-modes=""false"" :show-filter-operator=""false"" :sortable=""true"">
                    <template #body=""{{ data }}"">
                        {{{{ data.{propLower} }}}}
                    </template>
                    <template #filter=""{{ filterModel }}"">
                        <Select v-model=""filterModel.value"" filter  @filter=""(e) => search{entityRelatedPlural}(e.value)"" :options=""{entityRelatedPluralLower}"" :loading=""loading{entityRelatedPlural}"" option-value=""{displayedProp}"" option-label=""{displayedProp}"" :placeholder=""$t('field.select')""></Select>
                    </template>
                </Column>");
                        }
                        if (rel.Type == RelationType.ManyToMany)
                        {
                            string entityRelatedPlural = rel.RelatedEntity.GetPluralName();
                            string entityRelatedPluralLower = entityRelatedPlural.GetCamelCaseName();
                            var lowerRelatedEntity = rel.RelatedEntity.GetCamelCaseName();
                            string displayedPropPlural = rel.DisplayedProperty.GetPluralName();
                            var displayedProp = rel.DisplayedProperty.GetCamelCaseName();
                            sb.AppendLine($@"
                <Column v-if=""isColumnSelected('{lowerRelatedEntity}{displayedPropPlural}')"" field=""{lowerRelatedEntity}{displayedPropPlural}"" :header=""$t('field.{lowerRelatedEntity}{displayedPropPlural}')"" :show-add-button=""false"" :show-filter-match-modes=""false"" :show-filter-operator=""false"" :sortable=""true"">
                    <template #body=""{{ data }}"">
                        {{{{ data.{lowerRelatedEntity}{displayedPropPlural} }}}}
                    </template>
                    <template #filter=""{{ filterModel }}"">
                        <Select v-model=""filterModel.value"" filter  @filter=""(e) => search{entityRelatedPlural}(e.value)"" :options=""{entityRelatedPluralLower}"" :loading=""loading{entityRelatedPlural}"" option-value=""{displayedProp}"" option-label=""{displayedProp}"" :placeholder=""$t('field.select')""></Select>
                    </template>
                </Column> <!-- this column for many to many relation, needs customization-->");
                        }
                    }
                }
            }
            if (sb.Length > 0)
                return sb.ToString().TrimEnd();
            else
                return null;
        }
        public static void GenerateSingleView(string entityName, string srcDir, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps,List<Relation> relations,bool hasAssets)
        {
            var entityLower = entityName.GetCamelCaseName();
            string entityPlural = entityName.GetPluralName();
            string capitalEntityPlural = entityPlural.GetCapitalName();
            string entityPluralLower = entityPlural.GetCamelCaseName();
            string viewDirectory = Path.Combine(srcDir, "views", $"{entityLower}");
            Directory.CreateDirectory(viewDirectory);
            string fileSingleName = entityName;
            string viewSinglePath = Path.Combine(viewDirectory, $"{entityName}.vue");
            string? importRef = hasAssets ? $"import {{ ref }} from 'vue';" : null;
            string? importAssetEndpoint = hasAssets ? ", ASSET_ENDPOINT" : null;
            string? assetSection = null;
            string? assetListSection = null;
            string? videoSection = null;
            string? videoListSection = null;

            StringBuilder enumConsts = new StringBuilder();
            foreach (var enm in enumProps)
            {
                List<string> st = new List<string>();
                for (int i = 0; i < enm.enumValues.Count; i++)
                {
                    string line = $"    {{label: t('field.{enm.enumValues[i]}'), value: {i} }},";
                    st.Add(line);
                }
                enumConsts.AppendLine($@"
const {entityLower}{enm.prop}Options = [
{string.Join(Environment.NewLine, st)}
];");
            }
            StringBuilder relationImports = new StringBuilder();
            StringBuilder relationConsts = new StringBuilder();
            foreach (var rel in relations)
            {
                if (rel.Type == RelationType.OneToOneSelfJoin || rel.Type == RelationType.ManyToOne || rel.Type == RelationType.ManyToOneNullable
                    || rel.Type == RelationType.OneToOne || rel.Type == RelationType.OneToOneNullable || rel.Type == RelationType.ManyToMany)
                {
                    //var propLower = rel.Type == RelationType.OneToOneSelfJoin ? char.ToLower(rel.RelatedEntity[0]) + rel.RelatedEntity.Substring(1) + "ParentId"
                    //    : rel.Type != RelationType.ManyToMany ?
                    //    char.ToLower(rel.RelatedEntity[0]) + rel.RelatedEntity.Substring(1) + "Id"
                    //    : null;
                    string entityRelatedPlural = rel.RelatedEntity.EndsWith("y") ? rel.RelatedEntity[..^1] + "ies" : rel.RelatedEntity + "s";
                    string entityRelatedPluralLower = char.ToLower(entityRelatedPlural[0]) + entityRelatedPlural.Substring(1);

                    relationImports.AppendLine($"import {{ use{rel.RelatedEntity}Store }} from '@/store/{rel.RelatedEntity}Store';");
                    relationConsts.AppendLine($"const {{ items: {entityRelatedPluralLower}, loading: loading{entityRelatedPlural}, search: search{entityRelatedPlural} }} = useList(use{rel.RelatedEntity}Store(), '', {{}});");
                }
            }

            StringBuilder colomnBuilder = new StringBuilder();
            foreach (var prop in properties)
            {
                if(prop.Type == "GPG")
                {
                    assetSection = $@"
// Single Asset Section
const assetSrc = ref(null);
const onSelectAsset = (e) => {{
    // remove old asset
    store.delete{prop.Name} = true;
    store.{prop.Name.GetCamelCaseName()}Url = null;

    const file = e.files[0];
    onPropChanged(file, '{prop.Name.GetCamelCaseName()}');
    const reader = new FileReader();
    reader.onload = async (e) => {{
        assetSrc.value = e.target.result;
    }};
    reader.readAsDataURL(file);
}};

const removeAsset = () => {{
    // Reset delete asset
    store.delete{prop.Name} = true;
    store.{prop.Name.GetCamelCaseName()}Url = null;
    onPropChanged(null, '{prop.Name.GetCamelCaseName()}');
    assetSrc.value = null;
}};";
                }

                else if(prop.Type == "PNGs")
                {
                    assetListSection = $@"
// Multiple Assets Section
const assetSrcs = ref([]); // New uploaded previews
const selectedFiles = ref([]); // Actual new File objects

const onSelectAssets = (event) => {{
    // Convert FileList to array
    const filesArray = Array.from(event.files);

    filesArray.forEach((file) => {{
        // Add to array that will go into state.assets
        selectedFiles.value.push(file);

        // Create a preview URL and push into assetSrcs
        const reader = new FileReader();
        reader.onload = (evt) => {{
            assetSrcs.value.push(evt.target.result);
        }};
        reader.readAsDataURL(file);
    }});

    onPropChanged(selectedFiles.value, '{prop.Name.GetCamelCaseName()}');
}};

// Remove new uploaded asset
const removeNewAsset = (index) => {{
    assetSrcs.value.splice(index, 1);
    selectedFiles.value.splice(index, 1);
    onPropChanged(selectedFiles.value, '{prop.Name.GetCamelCaseName()}');
}};

// Remove existing backend asset
const removeExistingAsset = (index) => {{
    const removedUrl = store.{prop.Name.GetCamelCaseName()}Urls[index];
    store.deleted{prop.Name}Urls.push(removedUrl); // add to store.deleted{prop.Name}Urls
    store.{prop.Name.GetCamelCaseName()}Urls.splice(index, 1); // Remove from store.{prop.Name.GetCamelCaseName()}Urls (UI)
}};";
                }

                else if (prop.Type == "VD")
                {
                    videoSection = $@"
// upload single video
const videoSrc = ref(null);
const onSelectVideo = (e) => {{
    // reset old video
    store.delete{prop.Name} = true;
    store.{prop.Name.GetCamelCaseName()}Url = null;

    const file = e.files[0];
    onPropChanged(file, '{prop.Name.GetCamelCaseName()}');
    const reader = new FileReader();
    reader.onload = async (e) => {{
        videoSrc.value = e.target.result;
    }};
    reader.readAsDataURL(file);
}};

const removeVideo = () => {{
    // Reset delete video
    store.delete{prop.Name} = true;
    store.{prop.Name.GetCamelCaseName()}Url = null;
    onPropChanged(null, '{prop.Name.GetCamelCaseName()}');
    videoSrc.value = null;
}};";
                }
                else if (prop.Type == "VDs")
                {
                    videoListSection = $@"
// upload multiple videos
const videoSrcs = ref([]); // New uploaded previews
const selectedVideos = ref([]); // Actual new File objects

const onSelectVideos = (event) => {{
    // Convert FileList to array
    const filesArray = Array.from(event.files);

    filesArray.forEach((file) => {{
        // Add to array that will go into state.assets
        selectedVideos.value.push(file);

        // Create a preview URL and push into videoSrcs
        const reader = new FileReader();
        reader.onload = (evt) => {{
            videoSrcs.value.push(evt.target.result);
        }};
        reader.readAsDataURL(file);
    }});

    onPropChanged(selectedVideos.value, '{prop.Name.GetCamelCaseName()}');
}};

const removeNewVideo = (index) => {{
    videoSrcs.value.splice(index, 1);
    selectedVideos.value.splice(index, 1);
    onPropChanged(selectedVideos.value, '{prop.Name.GetCamelCaseName()}');
}}

const removeExistingVideo = (index) => {{
    const removedUrl = store.{prop.Name.GetCamelCaseName()}Urls[index];
    store.deleted{prop.Name}Urls.push(removedUrl); // add to store.deleted{prop.Name}Urls
    store.{prop.Name.GetCamelCaseName()}Urls.splice(index, 1); // Remove from store.{prop.Name.GetCamelCaseName()}Urls (UI)
}}";
                }

                string colomn = GetSingleColomnControl(entityName, prop,enumProps);
                colomnBuilder.AppendLine(colomn);
            }
            string? relationColomn = GetSingleColomnRelationControl(entityName, relations);
            if (relationColomn != null)
                colomnBuilder.AppendLine(relationColomn);
            string content = $@"
<script setup>
import {{ use{entityName}Store as useStore }} from '@/store/{entityName}Store';
import {{ {capitalEntityPlural}_ROUTE as PAGE_ROUTE{importAssetEndpoint} }} from '@/utils/Constants';
{relationImports}
import useSingle from '@/composables/useSingle';
import useList from '@/composables/useList';
{importRef}

const store = useStore();

const {{ state, onPropChanged, onSave, onCancel, t }} = useSingle(store, PAGE_ROUTE);
{relationConsts}
{enumConsts}
{assetSection}
{assetListSection}
{videoSection}
{videoListSection}

</script>
<template>
    <div class=""theCard"">
        <h2 class=""text-3xl font-semibold"">{{{{ $t('title.{entityName}') }}}}</h2>

        <!-- form -->
        <form @submit.prevent="""" class=""mt-12 w-full"">
            <div class=""grid grid-cols-1 md:grid-cols-2 gap-4 w-full"">                                      
{colomnBuilder}
            </div>
            <!-- actions -->
            <div class=""flex items-center gap-5 mt-12 w-full"">
                <Button severity=""primary"" text :label=""$t('button.cancel')"" class=""rounded float-start"" outlined @click=""onCancel"" :disabled=""state.saving""></Button>
                <Button
                    severity=""primary""
                    :loading=""state.saving""
                    style=""""
                    :label=""$t('button.save')""
                    class=""rounded float-start""
                    @click=""onSave""
                    :disabled=""state.saving || state.finding""
                    v-if=""state.itemPageState !== $StoreConstant('VIEW_PAGE_STATE')""
                />
            </div>
        </form>
    </div>
</template>";
            
            File.WriteAllText(viewSinglePath, content);
        }

        private static string GetSingleColomnControl(string entityName, (string Type, string Name, PropertyValidation Validation) prop, List<(string prop, List<string> enumValues)> enumProps)
        {
            var entityLower = char.ToLower(entityName[0]) + entityName.Substring(1);
            var propLower = char.ToLower(prop.Name[0]) + prop.Name.Substring(1);
            string? requiredRule = prop.Validation != null && prop.Validation.Required ? "rules=\"required\"" : null;
            string? requiredValidate = prop.Validation != null && prop.Validation.Required ? ":validateOnInput=\"true\"" : null;
            var typeWithoutNullable = prop.Type.TrimEnd('?');
            if (typeWithoutNullable == "string" || typeWithoutNullable == "Guid")
            {
                return $@"
                <div class=""flex flex-wrap gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <InputText
                        id=""{propLower}""
                        class=""w-full""
                        :placeholder=""$t('field.{propLower}')""
                        :invalid=""state.validationErrors.{propLower}""
                        v-model=""state.{propLower}""
                        :disabled=""state.finding || state.saving || state.itemPageState === $StoreConstant('VIEW_PAGE_STATE')""
                        @input=""(e) => onPropChanged(e.target.value, '{propLower}')""
                    />
                </div>";
            }
            if ((typeWithoutNullable == "int" && !enumProps.Any(ep => ep.prop == prop.Name)) || typeWithoutNullable == "double" || typeWithoutNullable == "decimal" || typeWithoutNullable == "float")
            {
                string? NumOfDigit = (typeWithoutNullable == "double" || typeWithoutNullable == "decimal" || typeWithoutNullable == "float") ? ":maxFractionDigits=\"10\"" : null;
                return $@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <InputNumber
                        :useGrouping=""false""
                        {NumOfDigit}
                        id=""{propLower}""
                        fluid
                        :placeholder=""$t('field.{propLower}')""
                        class=""w-full""
                        :invalid=""state.validationErrors.{propLower}""
                        v-model=""state.{propLower}""
                        @input=""(e) => onPropChanged(e.value, '{propLower}')""
                        :disabled=""state.finding || state.saving || state.itemPageState === $StoreConstant('VIEW_PAGE_STATE')""
                    />
                </div>";
            }
            if (typeWithoutNullable == "int" && enumProps.Any(ep => ep.prop == prop.Name))//enum case
            {
                return $@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <Select
                        :options=""{entityLower}{prop.Name}Options""
                        optionLabel=""label""
                        optionValue=""value""
                        v-model=""state.{propLower}""
                        :placeholder=""$t('field.select{prop.Name}')""
                        class=""w-full""
                        :invalid=""state.validationErrors.{propLower}""
                        @change=""(e) => onPropChanged(e.value, '{propLower}')""
                        :disabled=""state.finding || state.saving || state.itemPageState === $StoreConstant('VIEW_PAGE_STATE')""
                    >
                        <template #option=""slotProps"">
                            <div class=""flex items-center"">
                                <div>{{{{ slotProps.option.label }}}}</div>
                            </div>
                        </template>
                    </Select>
                </div>";
            }
            if (typeWithoutNullable.Contains("Date") || typeWithoutNullable.Contains("Time"))
            {
                return $@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <DatePicker
                        v-model=""state.{propLower}""
                        showIcon
                        fluid
                        iconDisplay=""input""
                        dateFormat=""dd/mm/yy""
                        @change=""(e) => onPropChanged(e.value, '{propLower}')""
                        :invalid=""state.validationErrors.{propLower}""
                        showButtonBar
                        :placeholder=""$t('field.{propLower}')""
                        class=""!w-full""
                        :disabled=""state.finding || state.saving || state.itemPageState === $StoreConstant('VIEW_PAGE_STATE')""
                    />
                </div>";
            }
            if (typeWithoutNullable == "bool")
            {
                return $@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <Checkbox
                        v-model=""state.{propLower}""
                        inputId=""{propLower}""
                        name=""{propLower}""
                        :binary=""true""
                        :invalid=""state.validationErrors.{propLower}""
                        :disabled=""state.finding || state.saving || state.itemPageState === $StoreConstant('VIEW_PAGE_STATE')""
                        @change=""onPropChanged($event.target.checked, '{propLower}')""
                    />
                </div>";
            }

            if (typeWithoutNullable == "GPG")
            {
                return $@"
                <div class=""flex flex-col items-start col-span-1 md:col-span-2 gap-2 w-full"">
                    <label>{{{{ $t('field.singleAsset(photo)') }}}}</label>
                    <p v-if=""state.validationErrors.{prop.Name.GetCamelCaseName()}"" class=""text-red-500"">{{{{ $t('message.assetRequired') }}}}</p>
                    <FileUpload mode=""basic"" accept=""image/*"" @select=""onSelectAsset"" customUpload auto class=""p-button-outlined"" :disabled=""state.finding || state.saving || state.itemPageState === $StoreConstant('VIEW_PAGE_STATE')"" />
                    <div v-if=""assetSrc"" class=""relative"">
                        <button
                            @click=""removeAsset""
                            :disabled=""state.finding || state.saving || state.itemPageState === $StoreConstant('VIEW_PAGE_STATE')""
                            class=""absolute z-50 top-2.5 right-2.5 bg-black/50 border border-white text-white flex justify-center items-center rounded-lg w-[32px] h-[32px] hover:bg-[#FF4C51] transition""
                        >
                            <i class=""pi pi-trash""></i>
                        </button>
                        <img :src=""assetSrc"" alt=""Image"" class=""shadow-md w-[300px] aspect-video object-cover"" />
                    </div>

                    <div v-if=""state.{prop.Name.GetCamelCaseName()}Url"" class=""relative"">
                        <button
                            @click=""removeAsset""
                            :disabled=""state.finding || state.saving || state.itemPageState === $StoreConstant('VIEW_PAGE_STATE')""
                            class=""absolute z-50 top-2.5 right-2.5 bg-black/50 border border-white text-white flex justify-center items-center rounded-lg w-[32px] h-[32px] hover:bg-[#FF4C51] transition""
                        >
                            <i class=""pi pi-trash""></i>
                        </button>
                        <img :src=""ASSET_ENDPOINT(state.{prop.Name.GetCamelCaseName()}Url)"" alt=""Image"" class=""shadow-md w-[300px] aspect-video object-cover"" />
                    </div>
                </div>";
            }
            if (typeWithoutNullable == "PNGs")
            {
                return $@"
                <div class=""flex flex-col items-start col-span-1 md:col-span-2 gap-2 w-full"">
                    <label>{{{{ $t('field.multipleAssets(photos)') }}}}</label>
                    <p v-if=""state.validationErrors.{prop.Name.GetCamelCaseName()}"" class=""text-red-500"">{{{{ $t('message.imagesRequired') }}}}</p>
                    <FileUpload
                        :multiple=""true""
                        accept=""image/*""
                        mode=""basic""
                        @select=""onSelectAssets""
                        customUpload
                        auto
                        class=""p-button-outlined""
                        :disabled=""state.finding || state.saving || state.itemPageState === $StoreConstant('VIEW_PAGE_STATE')""
                    />

                    <!-- images -->
                    <div class=""w-full"">
                        <!-- old -->
                        <div v-if=""state.{prop.Name.GetCamelCaseName()}Urls"" class=""flex flex-wrap gap-4 w-full"">
                            <div v-for=""(assetUrl, index) in state.{prop.Name.GetCamelCaseName()}Urls"" :key=""index"" class=""relative"">
                                <button
                                    @click=""removeExistingAsset(index)""
                                    :disabled=""state.finding || state.saving || state.itemPageState === $StoreConstant('VIEW_PAGE_STATE')""
                                    class=""absolute z-50 top-2.5 right-2.5 bg-black/50 border border-white text-white flex justify-center items-center rounded-lg w-[32px] h-[32px] hover:bg-[#FF4C51] transition""
                                >
                                    <i class=""pi pi-trash""></i>
                                </button>
                                <img :src=""ASSET_ENDPOINT(assetUrl)"" alt=""Image"" class=""shadow-md w-[300px] aspect-video object-cover"" />
                            </div>
                        </div>
                        <!-- new -->
                        <div v-if=""assetSrcs"" class=""flex flex-wrap gap-4 w-full"">
                            <div v-for=""(img, index) in assetSrcs"" :key=""index"" class=""relative"">
                                <button
                                    @click=""removeNewAsset(index)""
                                    :disabled=""state.finding || state.saving || state.itemPageState === $StoreConstant('VIEW_PAGE_STATE')""
                                    class=""absolute z-50 top-2.5 right-2.5 bg-black/50 border border-white text-white flex justify-center items-center rounded-lg w-[32px] h-[32px] hover:bg-[#FF4C51] transition""
                                >
                                    <i class=""pi pi-trash""></i>
                                </button>
                                <img :src=""img"" alt=""Image"" class=""w-[300px] shadow-md aspect-video object-cover"" />
                            </div>
                        </div>
                    </div>
                </div>";
            }
            if (typeWithoutNullable == "VD")
            {
                return $@"
                <div class=""flex flex-col items-start col-span-1 md:col-span-2 gap-2 w-full"">
                    <label>{{{{ $t('field.singleAsset(video)') }}}}</label>
                    <p v-if=""state.validationErrors.{prop.Name.GetCamelCaseName()}"" class=""text-red-500"">{{{{ $t('message.videoRequired') }}}}</p>
                    <FileUpload mode=""basic"" accept=""video/*"" @select=""onSelectVideo"" customUpload auto class=""p-button-outlined"" :disabled=""state.finding || state.saving || state.itemPageState === $StoreConstant('VIEW_PAGE_STATE')"" />
                    <!-- new video -->
                    <div v-if=""videoSrc"" class=""relative"">
                        <button
                            @click=""removeVideo""
                            :disabled=""state.finding || state.saving || state.itemPageState === $StoreConstant('VIEW_PAGE_STATE')""
                            class=""absolute z-50 top-2.5 right-2.5 bg-black/50 border border-white text-white flex justify-center items-center rounded-lg w-[32px] h-[32px] hover:bg-[#FF4C51] transition""
                        >
                            <i class=""pi pi-trash""></i>
                        </button>
                        <video :src=""videoSrc"" alt=""Video"" controls class=""shadow-md w-[300px] aspect-video object-cover"" ></video>
                    </div>

                    <!-- old video -->
                    <div v-if=""state.{prop.Name.GetCamelCaseName()}Url"" class=""relative"">
                        <button
                            @click=""removeVideo""
                            :disabled=""state.finding || state.saving || state.itemPageState === $StoreConstant('VIEW_PAGE_STATE')""
                            class=""absolute z-50 top-2.5 right-2.5 bg-black/50 border border-white text-white flex justify-center items-center rounded-lg w-[32px] h-[32px] hover:bg-[#FF4C51] transition""
                        >
                            <i class=""pi pi-trash""></i>
                        </button>
                        <video :src=""ASSET_ENDPOINT(state.{prop.Name.GetCamelCaseName()}Url)"" alt=""Video"" controls class=""shadow-md w-[300px] aspect-video object-cover"" ></video>
                    </div>
                </div>";
            }
            if (typeWithoutNullable == "VDs")
            {
                return $@"
                <div class=""flex flex-col items-start col-span-1 md:col-span-2 gap-2 w-full"">
                    <label>{{{{ $t('field.multipleAssets(videos)') }}}}</label>
                    <p v-if=""state.validationErrors.{prop.Name.GetCamelCaseName()}"" class=""text-red-500"">{{{{ $t('message.videosRequired') }}}}</p>
                    <FileUpload
                        :multiple=""true""
                        accept=""video/*""
                        mode=""basic""
                        @select=""onSelectVideos""
                        customUpload
                        auto
                        class=""p-button-outlined""
                        :disabled=""state.finding || state.saving || state.itemPageState === $StoreConstant('VIEW_PAGE_STATE')""
                    />

                    <!-- videos -->
                    <div class=""w-full"">
                        <!-- old -->
                        <div v-if=""state.{prop.Name.GetCamelCaseName()}Urls"" class=""flex flex-wrap gap-4 w-full"">
                            <div v-for=""(videoUrl, index) in state.{prop.Name.GetCamelCaseName()}Urls"" :key=""index"" class=""relative"">
                                <button
                                    @click=""removeExistingVideo(index)""
                                    :disabled=""state.finding || state.saving || state.itemPageState === $StoreConstant('VIEW_PAGE_STATE')""
                                    class=""absolute z-50 top-2.5 right-2.5 bg-black/50 border border-white text-white flex justify-center items-center rounded-lg w-[32px] h-[32px] hover:bg-[#FF4C51] transition""
                                >
                                    <i class=""pi pi-trash""></i>
                                </button>
                                <video :src=""ASSET_ENDPOINT(videoUrl)"" alt=""Viedo"" controls class=""shadow-md w-[300px] aspect-video object-cover"" />
                            </div>
                        </div>
                        <!-- new -->
                        <div v-if=""videoSrcs"" class=""flex flex-wrap gap-4 w-full"">
                            <div v-for=""(video, index) in videoSrcs"" :key=""index"" class=""relative"">
                                <button
                                    @click=""removeNewVideo(index)""
                                    :disabled=""state.finding || state.saving || state.itemPageState === $StoreConstant('VIEW_PAGE_STATE')""
                                    class=""absolute z-50 top-2.5 right-2.5 bg-black/50 border border-white text-white flex justify-center items-center rounded-lg w-[32px] h-[32px] hover:bg-[#FF4C51] transition""
                                >
                                    <i class=""pi pi-trash""></i>
                                </button>
                                <video :src=""video"" alt=""Video"" controls class=""w-[300px] shadow-md aspect-video object-cover"" />
                            </div>
                        </div>
                    </div>
                </div>";
            }
            if (typeWithoutNullable.Contains("List<"))
            {
                //TODO : handle List case
                return null;
            }
            return null;
        }


        private static string GetSingleColomnRelationControl(string entityName, List<Relation> relations)
        {
            StringBuilder sb = new StringBuilder();
            if (relations != null && relations.Any())
            {
                foreach (var rel in relations)
                {
                    if (rel.Type == RelationType.OneToOneSelfJoin || rel.Type == RelationType.ManyToOne || rel.Type == RelationType.ManyToOneNullable
                        || rel.Type == RelationType.OneToOne || rel.Type == RelationType.OneToOneNullable)
                    {
                        string entityRelatedPlural = rel.RelatedEntity.EndsWith("y") ? rel.RelatedEntity[..^1] + "ies" : rel.RelatedEntity + "s";
                        string entityRelatedPluralLower = char.ToLower(entityRelatedPlural[0]) + entityRelatedPlural.Substring(1);
                        var lowerRelatedEntity = char.ToLower(rel.RelatedEntity[0]) + rel.RelatedEntity.Substring(1);
                        var propLower = rel.Type != RelationType.OneToOneSelfJoin ? lowerRelatedEntity
                            : lowerRelatedEntity + "Parent";
                        var displayedProp = char.ToLower(rel.DisplayedProperty[0]) + rel.DisplayedProperty.Substring(1);
                        sb.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}{rel.DisplayedProperty}"">{{{{ $t('field.{propLower}{rel.DisplayedProperty}') }}}}</label>
                    <Select
                        :emptyMessage=""$t('message.noAvailableOptions')""
                        :options=""{entityRelatedPluralLower}""
                        optionLabel=""{displayedProp}""
                        optionValue=""id""
                        v-model=""state.{propLower}Id""
                        :invalid=""state.validationErrors.{propLower}Id""
                        :loading=""loading{entityRelatedPlural}""
                        :placeholder=""$t('field.select{rel.RelatedEntity}')""
                        class=""w-full""
                        filter
                        @filter=""(e) => search{entityRelatedPlural}(e.value)""
                        @change=""(e) => onPropChanged(e.value, '{propLower}Id')""
                        :disabled=""state.finding || state.saving || state.itemPageState === $StoreConstant('VIEW_PAGE_STATE')""
                    >
                        <template #option=""slotProps"">
                            <div class=""flex items-center"">
                                <div>{{{{ slotProps.option.{displayedProp} }}}}</div>
                            </div>
                        </template>
                    </Select>
                </div>");
                    }
                    if (rel.Type == RelationType.ManyToMany)
                    {
                        string entityRelatedPlural = rel.RelatedEntity.EndsWith("y") ? rel.RelatedEntity[..^1] + "ies" : rel.RelatedEntity + "s";
                        string entityRelatedPluralLower = char.ToLower(entityRelatedPlural[0]) + entityRelatedPlural.Substring(1);
                        var lowerRelatedEntity = char.ToLower(rel.RelatedEntity[0]) + rel.RelatedEntity.Substring(1);
                        string displayedPropPlural = rel.DisplayedProperty.EndsWith("y") ? rel.DisplayedProperty[..^1] + "ies" : rel.DisplayedProperty + "s";
                        var propLower =  lowerRelatedEntity + displayedPropPlural;
                        var displayedProp = char.ToLower(rel.DisplayedProperty[0]) + rel.DisplayedProperty.Substring(1);
                        sb.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <MultiSelect class=""w-full"" id=""{propLower}"" type=""text"" dataKey=""id""
                        optionLabel=""{displayedProp}"" optionValue='id'
                        :placeholder=""$t('field.select{rel.DisplayedProperty}')"" 
                        v-model=""state.{lowerRelatedEntity}Ids""
                        :options=""{entityRelatedPluralLower}""
                        :loading=""loading{entityRelatedPlural}""
                        :maxSelectedLabels=""3""
                        :invalid=""state.validationErrors.{propLower}""
                        filter
                        @filter=""(e) => search{entityRelatedPlural}(e.value)""
                        @change=""(e) => onPropChanged(JSON.parse(JSON.stringify(e.value)), '{propLower}')""
                        :disabled=""state.finding || state.saving || state.itemPageState === $StoreConstant('VIEW_PAGE_STATE')""
                    />
                </div>");
                    }
                }
            }
            if (sb.Length > 0)
                return sb.ToString().TrimEnd();
            else
                return null;
        }
    }
}
