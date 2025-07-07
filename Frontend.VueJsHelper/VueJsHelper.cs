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
        public static string VueJsSolutionPath = "C:\\baseFrontTemplate\\baseFrontTemplateV1.0\\src"; // ضع المسار الجذري لمشروع Vue هنا

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
                if(rel.Type == RelationType.UserSingle || rel.Type == RelationType.UserSingleNullable)
                {
                    string camelCasePropName = rel.DisplayedProperty.GetCamelCaseName();
                    if (rel.IsGeneratedInTable)
                    {
                        string temp = camelCasePropName + "Name";
                        allColumns.Add($"'{temp}'");
                        if (!rel.HiddenInTable)
                        {
                            defaultColumns.Add($"'{temp}'");
                        }
                    }
                    initialStateBuilder.AppendLine($"        {camelCasePropName}Id: null,");
                    constItemBuilder.AppendLine($"                {camelCasePropName}Id: this.{camelCasePropName}Id,");

                }
                if (rel.Type == RelationType.UserMany)
                {
                    string camelCasePropNameIds = (rel.DisplayedProperty.GetCamelCaseName()).GetPluralName() + "Ids";
                    string camelCasePropName = (rel.DisplayedProperty.GetCamelCaseName()).GetPluralName() + "Names";
                    if (rel.IsGeneratedInTable)
                    {
                        allColumns.Add($"'{camelCasePropName}'");
                        if (!rel.HiddenInTable)
                        {
                            defaultColumns.Add($"'{camelCasePropName}'");
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

                if (rel.Type == RelationType.UserSingle)
                {
                    string camelCasePropName = rel.DisplayedProperty.GetCamelCaseName();
                    requiredChecks.Append($@"
            if (this.{camelCasePropName}Id === null) {{
                errors.{camelCasePropName}Id = true;
            }}");
                }
            }

            var content = $@"
import {{ SAVE, SAVE_FAIL, SAVE_ITEM, VALIDATE_FORM }} from '@/utils/StoreConstant';
import {{generalActions,generalState}} from './GeneralStore';
import i18n from '@/config/i18n';
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
            const isValid = this[VALIDATE_FORM]();
            if (!isValid) {{
                this.sendErrorMessage(i18n.global.t('message.pleaseFillAllRequiredFields'));
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
        pick: ['selectedColumns', 'itemPageState']
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
            string? videoListDefine = null;
            string? fileDefine = null;
            string? fileListDefine = null;
            foreach (var prop in properties.PropertiesList)
            {
                if(prop.Type != "GPG" && prop.Type != "PNGs" && prop.Type != "VD" && prop.Type != "VDs" && prop.Type != "FL" && prop.Type != "FLs")
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
                            videoListDefine = $@"
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

                        case "FL":
                            fileDefine = $@"
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
                            dataAppendBuilder.AppendLine($@"
            if(!this.{prop.Name.GetCamelCaseName()}){{
                data.append('{prop.Name.GetCamelCaseName()}Url', this.{prop.Name.GetCamelCaseName()}Url);
            }}");
                            
                            dataAppendIfCheckBuilder.AppendLine($@"
            if (this.{prop.Name.GetCamelCaseName()}) {{
                data.append('delete{prop.Name}', 'false');
            }} else {{
                data.append('delete{prop.Name}', String(this.delete{prop.Name}));
            }}");
                            break;

                        case "FLs":
                            fileListDefine = $@"
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

                            dataAppendBuilder.AppendLine($"            this.{prop.Name.GetCamelCaseName()}.forEach((file) => data.append('{prop.Name.GetCamelCaseName()}FormFiles', file));");
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

                if (rel.Type == RelationType.UserSingle || rel.Type == RelationType.UserSingleNullable)
                {
                    string camelCasePropName = rel.DisplayedProperty.GetCamelCaseName();
                    if (rel.IsGeneratedInTable)
                    {
                        allColumns.Add($"'{camelCasePropName}Name'");
                        if (!rel.HiddenInTable)
                        {
                            defaultColumns.Add($"'{camelCasePropName}Name'");
                        }
                    }
                    initialStateBuilder.AppendLine($"        {camelCasePropName}Id: null,");
                    dataAppendBuilder.AppendLine($"                data.append('{camelCasePropName}Id', this.{camelCasePropName}Id);");

                }
                if (rel.Type == RelationType.UserMany)
                {
                    string camelCasePropNameIds = (rel.DisplayedProperty.GetCamelCaseName()).GetPluralName() + "Ids";
                    string camelCasePropName = (rel.DisplayedProperty.GetCamelCaseName()).GetPluralName() + "Names";
                    if (rel.IsGeneratedInTable)
                    {
                        allColumns.Add($"'{camelCasePropName}'");
                        if (!rel.HiddenInTable)
                        {
                            defaultColumns.Add($"'{camelCasePropName}'");
                        }
                    }
                    initialStateBuilder.AppendLine($"        {camelCasePropNameIds}: [],");
                    dataAppendBuilder.AppendLine($"                this.{camelCasePropNameIds}.forEach((id) => data.append('{camelCasePropNameIds}', id));");
                }

                if (rel.Type == RelationType.ManyToOne || rel.Type == RelationType.OneToOne)
                {
                    string camelCasePropName = rel.RelatedEntity.GetCamelCaseName();
                    requiredChecks.Append($@"
            if (this.{camelCasePropName}Id === null) {{
                errors.{camelCasePropName}Id = true;
            }}");
                }

                if (rel.Type == RelationType.UserSingle)
                {
                    string camelCasePropName = rel.DisplayedProperty.GetCamelCaseName();
                    requiredChecks.Append($@"
            if (this.{camelCasePropName}Id === null) {{
                errors.{camelCasePropName}Id = true;
            }}");
                }
            }

            var content = $@"
import {{ SAVE, SAVE_FAIL, SAVE_ITEM, VALIDATE_FORM }} from '@/utils/StoreConstant';
import {{generalActions,generalState}} from '@/store/GeneralStore';
import i18n from '@/config/i18n';
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
{videoListDefine}
{fileDefine}
{fileListDefine}
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
            const isValid = this[VALIDATE_FORM]();
            if (!isValid) {{
                this.sendErrorMessage(i18n.global.t('message.pleaseFillAllRequiredFields'));
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
        pick: ['selectedColumns', 'itemPageState']
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
            StringBuilder enumDisplayOption = new StringBuilder();
            StringBuilder watchDate = new StringBuilder();
            foreach (var enm in enumProps)
            {
                List<string> st = new List<string>();
                List<string> stOptions = new List<string>();
                for (int i = 0; i < enm.enumValues.Count; i++)
                {
                    var enmValueLower = char.ToLower(enm.enumValues[i][0]) + enm.enumValues[i].Substring(1);
                    string line = $"    {{label: t('field.{enmValueLower}'), value: '{i}' }},";
                    string lineOption = $"    {{label: t('field.{enmValueLower}'), value: {i} }},";
                    st.Add(line);
                    stOptions.Add(lineOption);
                }
                enumFilters.AppendLine($@"
const filter{entityName}{enm.prop}Options = [
{string.Join(Environment.NewLine,st)}
];");

                enumDisplayOption.AppendLine($@"
const {entityName.GetCamelCaseName()}{enm.prop}Options = [
{string.Join(Environment.NewLine, stOptions)}
];
");

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
                        filterSectionInitFilters.Add($"    {propLower}: {{ operator: null, constraints: [{{value: null, matchMode: 'dateIs' }}] }},");
                        watchDate.Append($@"
watch(
    () => filters.value.{propLower}.constraints[0].matchMode,
    (newMode) => {{
        if (newMode) filters.value.{propLower}.constraints[0].value = null;
    }}
);");
                    }
                }
                
            }
            StringBuilder relationImports = new StringBuilder();
            StringBuilder relationConsts = new StringBuilder();
            if (relations.Any(rel => (rel.Type == RelationType.UserMany && rel.IsGeneratedInTable) || (rel.Type == RelationType.UserSingle && rel.IsGeneratedInTable) || (rel.Type == RelationType.UserSingleNullable && rel.IsGeneratedInTable)))
            {
                relationImports.AppendLine($"import {{ useUserStore }} from '@/store/UserStore';");
                relationConsts.AppendLine($"const {{ items: users, loading: loadingUsers, search: searchUsers }} = useList(useUserStore(), '', {{}});");
            }
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
                    if(rel.Type == RelationType.UserSingle || rel.Type == RelationType.UserSingleNullable)
                    {
                        var propLower = rel.DisplayedProperty.GetCamelCaseName();
                        filterSectionGlobalFields.Add($"'{propLower}Name'");
                        filterSectionInitFilters.Add($"    {propLower}Name: {{ operator: FilterOperator.AND, constraints: [{{ value: null, matchMode: FilterMatchMode.EQUALS }}] }},");
                    }
                    if (rel.Type == RelationType.UserMany)
                    {
                        var propLower = rel.DisplayedProperty.GetCamelCaseName().GetPluralName();
                        filterSectionGlobalFields.Add($"'{propLower}Names'");
                        filterSectionInitFilters.Add($"    {propLower}Names: {{ operator: FilterOperator.AND, constraints: [{{ value: null, matchMode: FilterMatchMode.CONTAINS }}] }},");
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
import {{ watch, ref }} from 'vue';
import {{ use{entityName}Store as useStore }} from '@/store/{entityName}Store';
import {{ {capitalEntityPlural}_ROUTE as PAGE_ROUTE }} from '@/utils/Constants';
import ListTemplate from '@/components/table/ListTemplate.vue';
import {{ FilterMatchMode, FilterOperator }} from '@primevue/core/api';
{relationImports}
const {{ t, formattedDate, onDateBetweenFilterApply, dateMatchModes, clearDateFilter, isColumnSelected, getOptionLabel }} = useList(useStore(), PAGE_ROUTE, {{ }});
{enumFilters.ToString().TrimEnd()}
{enumDisplayOption.ToString().TrimEnd()}

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
{watchDate.ToString().TrimEnd()}
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
            if (typeWithoutNullable == "string")
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
            if ((typeWithoutNullable == "int" && !enumProps.Any(ep => ep.prop == prop.Name)) || typeWithoutNullable == "double" || typeWithoutNullable == "decimal" || typeWithoutNullable == "float")
            {
                return $@"
            <Column v-if=""isColumnSelected('{propLower}')"" field=""{propLower}"" data-type=""numeric"" :header=""$t('field.{propLower}')"" :show-add-button=""false"" :show-filter-operator=""false"" :sortable=""true"">
                <template #body=""{{ data }}"">
                    {{{{ data.{propLower} }}}}
                </template>
                <template #filter=""{{ filterModel }}"">
                    <InputText v-keyfilter.int v-model=""filterModel.value"" type=""text""
                            :placeholder=""$t('search.searchBy{prop.Name}')"" />
                </template>
            </Column>";
            }

            if (typeWithoutNullable == "int" && enumProps.Any(ep => ep.prop == prop.Name))//enum case
            {
                return $@"
            <Column v-if=""isColumnSelected('{propLower}')"" field=""{propLower}"" :header=""$t('field.{propLower}')"" :show-add-button=""false"" :show-filter-match-modes=""false"" :show-filter-operator=""false"" :sortable=""true"" >
                <template #body=""{{ data }}"">
                    {{{{ getOptionLabel({entityName.GetCamelCaseName()}{prop.Name}Options,data.{propLower}) }}}}
                </template>
                <template #filter=""{{ filterModel }}"">
                        <Select v-model=""filterModel.value"" :options=""filter{entityName}{prop.Name}Options"" option-value=""value"" option-label=""label"" showClear :placeholder=""$t('field.{propLower}')""></Select>
                    </template>
            </Column>";
            }
            if (typeWithoutNullable.Contains("Date") || typeWithoutNullable.Contains("Time"))
            {
                return $@"
                <Column v-if=""isColumnSelected('{propLower}')"" field=""{propLower}"" dataType=""date"" :filter-match-mode-options=""dateMatchModes"" style=""width: 80px"" :header=""$t('field.{propLower}')"" :show-add-button=""false"" :show-filter-operator=""false"" :sortable=""true"">
                    <template #body=""{{ data }}"">
                        <div class=""flex justify-center"">{{{{ formattedDate(data.{propLower}) }}}}</div>
                    </template>
                    <template #filter=""{{ filterModel }}"">
                        <DatePicker v-if=""filterModel.matchMode === 'dateBetween'"" v-model=""filterModel.value"" selectionMode=""range"" dateFormat=""dd/mm/yy"" :manualInput=""false"" placeholder=""dd/mm/yyyy"" />
                        <DatePicker v-else v-model=""filterModel.value"" dateFormat=""dd/mm/yy"" :manualInput=""false"" placeholder=""dd/mm/yyyy"" />
                    </template>
                    <template #filterapply=""{{ filterCallback }}"">
                        <Button size=""small"" :label=""$t('button.apply')"" @click=""onDateBetweenFilterApply(filters, filterCallback, '{propLower}')"" />
                    </template>
                    <template #filterclear=""{{ filterCallback, filterModel }}"">
                        <Button size=""small"" variant=""outlined"" :label=""$t('button.clear')"" @click=""clearDateFilter(filterModel, filterCallback)"" />
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

            if (typeWithoutNullable == "VD" || typeWithoutNullable == "GPG" || typeWithoutNullable == "PNGs" || typeWithoutNullable == "VDs" || typeWithoutNullable == "FLs" || typeWithoutNullable == "FL")
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

                        if (rel.Type == RelationType.UserSingle || rel.Type == RelationType.UserSingleNullable)
                        {
                            var propLower = rel.DisplayedProperty.GetCamelCaseName() + "Name";
                            sb.AppendLine($@"
                <Column v-if=""isColumnSelected('{propLower}')"" field=""{propLower}"" :header=""$t('field.{propLower}')"" :show-add-button=""false"" :show-filter-match-modes=""false"" :show-filter-operator=""false"" :sortable=""true"">
                    <template #body=""{{ data }}"">
                        {{{{ data.{propLower}}}}}
                    </template>
                    <template #filter=""{{ filterModel }}"">
                        <Select v-model=""filterModel.value"" filter  @filter=""(e) => searchUsers(e.value)"" :options=""users"" :loading=""loadingUsers"" option-value=""fullName"" option-label=""fullName"" :placeholder=""$t('field.select')""></Select>
                    </template>
                </Column>");
                        }

                        if (rel.Type == RelationType.UserMany)
                        {
                            var propLower = rel.DisplayedProperty.GetCamelCaseName().GetPluralName() + "Names";
                            sb.AppendLine($@"
                <Column v-if=""isColumnSelected('{propLower}')"" field=""{propLower}"" :header=""$t('field.{propLower}')"" :show-add-button=""false"" :show-filter-match-modes=""false"" :show-filter-operator=""false"" :sortable=""true"">
                    <template #body=""{{ data }}"">
                        {{{{ data.{propLower} }}}}
                    </template>
                    <template #filter=""{{ filterModel }}"">
                        <Select v-model=""filterModel.value"" filter  @filter=""(e) => searchUsers(e.value)"" :options=""users"" :loading=""loadingUsers"" option-value=""fullName"" option-label=""fullName"" :placeholder=""$t('field.select')""></Select>
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
            string? fileImportsRef = properties.Any(p => p.Type == "FL" || p.Type == "FLs") ? ", watch, onUnmounted, computed" : null;
            string? fileImportPreview = properties.Any(p => p.Type == "FL" || p.Type == "FLs") ? "import { VueFilesPreview } from 'vue-files-preview';" : null;
            
            string ? importRef = hasAssets ? $"import {{ ref{fileImportsRef} }} from 'vue';" : null;
            string? importAssetEndpoint = hasAssets ? ", ASSET_ENDPOINT" : null;
            string? assetSection = null;
            string? assetListSection = null;
            string? videoSection = null;
            string? videoListSection = null;
            string? fileHelperSection = null;
            string? fileSection = null;
            string? fileListSection = null;

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
            if (relations.Any(rel => rel.Type == RelationType.UserMany || rel.Type == RelationType.UserSingle || rel.Type == RelationType.UserSingleNullable))
            {
                relationImports.AppendLine($"import {{ useUserStore }} from '@/store/UserStore';");
                relationConsts.AppendLine($"const {{ items: users, loading: loadingUsers, search: searchUsers }} = useList(useUserStore(), '', {{}});");
            }
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
                else if(prop.Type == "FL" || prop.Type == "FLs")
                {
                    fileHelperSection = $@"
// FILES SECTION

const newFiles = ref([]);
const previews = ref([]); // our unified preview items

// Helper: fetch a URL → Blob → File
async function fetchUrlAsFile(url) {{
    const resp = await fetch(url);
    if (!resp.ok) throw new Error(`Fetch failed: ${{resp.status}}`);
    const blob = await resp.blob();
    const filename = url.split('/').pop().slice(37);
    return new File([blob], filename, {{ type: blob.type }});
}}

// Helper: returns true if the file is an archive we can’t preview
function isArchive(fileName) {{
    return /\.(zip|rar)$/i.test(fileName);
}}

// Helper: Build a preview‐object from a File
function makePreviewObj(file, source) {{
    const downloadUrl = URL.createObjectURL(file);
    return {{
        source, // 'remote' or 'new'
        file,
        downloadUrl,
        name: file.name,
        type: file.type || '—',
        size: file.size
    }};
}}
";
                    if (prop.Type == "FL")
                    {
                        fileSection = $@"
// —————————————
// SINGLE FILE SECTION
// —————————————

// 1. refs to hold the chosen file and its preview URL
const singleFile = ref(null);
const singlePreviewUrl = ref(null);

// 2. Helper to clear current single-file state
function clearSingle() {{
    if (singlePreviewUrl.value) {{
        URL.revokeObjectURL(singlePreviewUrl.value);
    }}
    singleFile.value = null;
    singlePreviewUrl.value = null;
    onPropChanged(null, '{prop.Name.GetCamelCaseName()}'); // reset in store // <-- [property]
}}

// 3. Watch the backend’s fileUrl
watch(
    () => store.{prop.Name.GetCamelCaseName()}Url, // <-- store.[fileUrl]
    async (url) => {{
        clearSingle();
        // if there’s a URL, fetch and convert
        if (url) {{
            try {{
                const fullUrl = ASSET_ENDPOINT(url);
                const file = await fetchUrlAsFile(fullUrl);
                singleFile.value = file;

                // create a blob URL for preview & download
                singlePreviewUrl.value = URL.createObjectURL(file);
            }} catch (err) {{
                console.error('Failed to load single file from', url, err);
            }}
        }}
    }},
    {{ immediate: true }}
);

// When the user picks a new file manually, clear any backend one:
function onSelectFile(event) {{
    clearSingle();

    const file = event.files[0];
    if (!file) return;
    singleFile.value = file;
    singlePreviewUrl.value = URL.createObjectURL(file);
    onPropChanged(file, '{prop.Name.GetCamelCaseName()}');// <-- [property]

    store.delete{prop.Name} = true; //<-- store.[property]
}}

// Remove single file
function removeFile() {{
    clearSingle();
    store.delete{prop.Name} = true;// <-- store.[property]
    store.{prop.Name.GetCamelCaseName()}Url = null;// <-- store.[property]
}}

onUnmounted(() => {{
    clearSingle();
}});
";
                    }
                    else
                    {
                        fileListSection = $@"
// —————————————
// MULTIPLE FILES SECTION
// —————————————

// Watch backend URLs: whenever store.fileUrls changes, re-build those previews
watch(
    () => store.{prop.Name.GetCamelCaseName()}Urls,//<-- store.[property]
    async (urls) => {{
        previews.value = previews.value.filter((p) => p.source === 'new');

        // then fetch each URL and add to previews
        for (const url of urls) {{
            try {{
                const fullUrl = ASSET_ENDPOINT(url);
                const file = await fetchUrlAsFile(fullUrl);
                previews.value.push(makePreviewObj(file, 'remote'));
            }} catch (err) {{
                console.error('Error fetching', url, err);
            }}
        }}
    }},
    {{ immediate: true }}
);

// Handle new uploads
function onSelectFiles(event) {{
    const files = Array.from(event.files);
    for (const f of files) {{
        newFiles.value.push(f);
        previews.value.push(makePreviewObj(f, 'new'));
    }}
    onPropChanged(newFiles.value, '{prop.Name.GetCamelCaseName()}');// [property]
}}

// Remove a preview (both UI and store)
function removePreview(index) {{
    const p = previews.value[index];
    URL.revokeObjectURL(p.downloadUrl);
    previews.value.splice(index, 1);

    if (p.source === 'remote') {{
        // tell store to delete this URL
        store.deleted{prop.Name}Urls.push(store.{prop.Name.GetCamelCaseName()}Urls[index]);//<-- store.[property] / store.[property]
        store.{prop.Name.GetCamelCaseName()}Urls.splice(index, 1);//<-- store.[property]
    }} else {{
        // a new file: remove from newFiles
        const nfIndex = newFiles.value.findIndex((f) => f === p.file);
        if (nfIndex > -1) newFiles.value.splice(nfIndex, 1);
        onPropChanged(newFiles.value, '{prop.Name.GetCamelCaseName()}');//<-- [property]
    }}
}}

// Cleanup on unmount
onUnmounted(() => {{
    for (const p of previews.value) {{
        URL.revokeObjectURL(p.downloadUrl);
    }}
}});

// Files we can show with <VueFilesPreview>
const previewableFiles = computed(() => previews.value.filter((p) => !isArchive(p.name)));

// Archive files only (zip / rar)
const archiveFiles = computed(() => previews.value.filter((p) => isArchive(p.name)));
// —————————————
";
                    }
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
{fileImportPreview}

const store = useStore();

const {{ state, onPropChanged, onSave, onCancel, t }} = useSingle(store, PAGE_ROUTE);
{relationConsts}
{enumConsts}
{assetSection}
{assetListSection}
{videoSection}
{videoListSection}
{fileHelperSection}
{fileSection}
{fileListSection}

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
                string? NumOfDigit = (typeWithoutNullable == "double" || typeWithoutNullable == "decimal" || typeWithoutNullable == "float") ? ":maxFractionDigits=\"4\"" : null;
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
                        showClear
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
                        <a
                            :href=""state.{prop.Name.GetCamelCaseName()}Url""
                            :download=""state.{prop.Name.GetCamelCaseName()}Url.slice(46)""
                            class=""absolute z-[100] top-2.5 left-2.5 bg-black/50 border border-white text-white flex justify-center items-center rounded-lg w-[32px] h-[32px] hover:bg-green-500 transition""
                        >
                            <i class=""pi pi-download""></i>
                        </a>
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
                                <a
                                    :href=""assetUrl""
                                    :download=""assetUrl.slice(46)""
                                    class=""absolute z-[100] top-2.5 left-2.5 bg-black/50 border border-white text-white flex justify-center items-center rounded-lg w-[32px] h-[32px] hover:bg-green-500 transition""
                                >
                                    <i class=""pi pi-download""></i>
                                </a>
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
                        <a
                            :href=""state.{prop.Name.GetCamelCaseName()}Url""
                            :download=""state.{prop.Name.GetCamelCaseName()}Url.slice(46)""
                            class=""absolute z-[100] top-2.5 left-2.5 bg-black/50 border border-white text-white flex justify-center items-center rounded-lg w-[32px] h-[32px] hover:bg-green-500 transition""
                        >
                            <i class=""pi pi-download""></i>
                        </a>
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
                                <a
                                    :href=""videoUrl""
                                    :download=""videoUrl.slice(46)""
                                    class=""absolute z-[100] top-2.5 left-2.5 bg-black/50 border border-white text-white flex justify-center items-center rounded-lg w-[32px] h-[32px] hover:bg-green-500 transition""
                                >
                                    <i class=""pi pi-download""></i>
                                </a>
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
            if(typeWithoutNullable == "FL")
            {
                return $@"
                <div class=""mb-8 md:col-span-2 grid grid-cols-1 md:grid-cols-2 gap-6"">
                    <div class=""flex flex-col items-start gap-2 md:col-span-2 w-full"">
                        <label>{{{{ $t('field.{prop.Name.GetCamelCaseName()}') }}}}</label> <!-- [property] -->
                        <p v-if=""state.validationErrors.{prop.Name.GetCamelCaseName()}"" class=""text-red-500"">{{{{ $t('message.fileRequired') }}}}</p> <!-- [validationErrors property] -->
                        <FileUpload :multiple=""false"" accept="".pdf, .txt, .xlsx, .xls, .docx, .rar, .zip"" mode=""basic"" @select=""onSelectFile"" customUpload auto class=""p-button-outlined"" :disabled=""state.finding || state.saving"" />
                    </div>

                    <!-- Preview of the single file -->
                    <div v-if=""singlePreviewUrl && !isArchive(singleFile.name)"" class=""p-4 border shadow rounded space-y-2"">
                        <!-- File preview -->
                        <VueFilesPreview :file=""singleFile"" :height=""'200px'"" :overflow=""'auto'"" />

                        <div class=""text-sm"">
                            <p>
                                <strong>{{{{ $t('field.name') }}}}:</strong> {{{{ singleFile.name }}}}
                            </p>
                            <!-- <p><strong>Type:</strong> {{{{ p.type }}}}</p> -->
                            <p>
                                <strong>{{{{ $t('field.size') }}}}:</strong> {{{{ (singleFile.size / 1024).toFixed(1) }}}} KB
                            </p>
                        </div>

                        <!-- Download & Remove buttons -->
                        <div class=""flex gap-2"">
                            <!-- Download -->
                            <a :href=""singlePreviewUrl"" :download=""singleFile.name"" class=""px-3 py-1 bg-green-500 text-white rounded"">
                                {{{{ $t('button.download') }}}}
                            </a>
                            <!-- Remove -->
                            <button @click=""removeFile"" class=""px-3 py-1 bg-red-500 text-white rounded"">
                                {{{{ $t('button.remove') }}}}
                            </button>
                        </div>
                    </div>
                    <!-- single archive -->
                    <div v-if=""singlePreviewUrl && isArchive(singleFile.name)"" class=""md:col-span-2"">
                        <label class=""block mb-3"">{{{{ $t('field.{prop.Name.GetCamelCaseName()}') }}}}</label> <!-- [property] -->
                        <div class=""flex flex-wrap gap-4 justify-between items-center"">
                            <div class=""flex items-center gap-4"">
                                <div class=""flex items-center justify-center h-[40px] bg-gray-100 rounded-full w-[40px] gap-2"">
                                    <svg xmlns=""http://www.w3.org/2000/svg"" fill=""none"" viewBox=""0 0 24 24"" stroke-width=""1.5"" stroke=""currentColor"" class=""w-6 h-6"">
                                        <path
                                            stroke-linecap=""round""
                                            stroke-linejoin=""round""
                                            d=""M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z""
                                        />
                                    </svg>
                                </div>
                                <span>{{{{ singleFile.name }}}} ({{{{ (singleFile.size / 1024).toFixed(1) }}}} KB)</span>
                            </div>
                            <div class=""flex gap-2"">
                                <a :href=""singlePreviewUrl"" :download=""singleFile.name"" class=""px-3 py-2 bg-green-500 text-white rounded text-sm"">
                                    {{{{ $t('button.download') }}}}
                                </a>
                                <button @click=""removeFile"" class=""px-3 py-2 bg-red-500 text-white rounded text-sm"">
                                    {{{{ $t('button.remove') }}}}
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
";
            }
            if (typeWithoutNullable == "FLs")
            {
                return $@"
                <div class=""flex flex-col items-start gap-2 md:col-span-2 w-full"">
                    <label>{{{{ $t('field.{prop.Name.GetCamelCaseName()}') }}}}</label> <!-- [property] -->
                    <p v-if=""state.validationErrors.{prop.Name.GetCamelCaseName()}"" class=""text-red-500"">{{{{ $t('message.filesRequired') }}}}</p><!-- [validationErrors property] -->
                    <FileUpload :multiple=""true"" accept="".pdf, .txt, .xlsx, .xls, .docx, .rar, .zip"" mode=""basic"" @select=""onSelectFiles"" customUpload auto class=""p-button-outlined"" :disabled=""state.finding || state.saving"" />
                </div>

                <!-- Previewable files grid -->
                <div v-if=""previewableFiles.length"" class=""md:col-span-2 grid grid-cols-1 md:grid-cols-2 gap-6"">
                    <div v-for=""(p, i) in previewableFiles"" :key=""i"" class=""p-4 border shadow rounded space-y-2"">
                        <!-- Preview component -->
                        <VueFilesPreview :file=""p.file"" height=""200px"" overflow=""auto"" />

                        <!-- File info -->
                        <div class=""text-sm"">
                            <p>
                                <strong>{{{{ $t('field.name') }}}}:</strong> {{{{ p.name }}}}
                            </p>
                            <!-- <p><strong>Type:</strong> {{{{ p.type }}}}</p> -->
                            <p>
                                <strong>{{{{ $t('field.size') }}}}:</strong> {{{{ (p.size / 1024).toFixed(1) }}}} KB
                            </p>
                        </div>

                        <!-- Actions -->
                        <div class=""flex gap-2"">
                            <!-- Download -->
                            <a :href=""p.downloadUrl"" :download=""p.name"" class=""px-3 py-1 bg-green-500 text-white rounded"">
                                {{{{ $t('button.download') }}}}
                            </a>
                            <!-- Remove -->
                            <button @click=""removePreview(previews.indexOf(p))"" class=""px-3 py-1 bg-red-500 text-white rounded"">
                                {{{{ $t('button.remove') }}}}
                            </button>
                        </div>
                    </div>
                </div>

                <!-- Archived files list -->
                <div v-if=""archiveFiles.length"" class=""md:col-span-2 mt-8"">
                    <label class=""block mb-3"">{{{{ $t('field.{prop.Name.GetCamelCaseName()}') }}}}</label> <!-- [property] -->
                    <ul class=""space-y-2"">
                        <li v-for=""(p, i) in archiveFiles"" :key=""`arch-${{i}}`"" class=""flex flex-wrap gap-4 justify-between items-center"">
                            <div class=""flex items-center gap-4"">
                                <div class=""flex items-center justify-center h-[40px] bg-gray-100 rounded-full w-[40px] gap-2"">
                                    <svg xmlns=""http://www.w3.org/2000/svg"" fill=""none"" viewBox=""0 0 24 24"" stroke-width=""1.5"" stroke=""currentColor"" class=""w-6 h-6"">
                                        <path
                                            stroke-linecap=""round""
                                            stroke-linejoin=""round""
                                            d=""M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z""
                                        />
                                    </svg>
                                </div>
                                <span>{{{{ p.name }}}} ({{{{ (p.size / 1024).toFixed(1) }}}} KB)</span>
                            </div>
                            <div class=""flex gap-2"">
                                <a :href=""p.downloadUrl"" :download=""p.name"" class=""px-3 py-2 bg-green-500 text-white rounded text-sm"">
                                    {{{{ $t('button.download') }}}}
                                </a>
                                <button @click=""removePreview(previews.indexOf(p))"" class=""px-3 py-2 bg-red-500 text-white rounded text-sm"">
                                    {{{{ $t('button.remove') }}}}
                                </button>
                            </div>
                        </li>
                    </ul>
                </div>
";
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
                        showClear
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

                    if (rel.Type == RelationType.UserSingle || rel.Type == RelationType.UserSingleNullable)
                    {
                        var propLower = rel.DisplayedProperty.GetCamelCaseName();
                        sb.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}Name"">{{{{ $t('field.{propLower}Name') }}}}</label>
                    <Select
                        :emptyMessage=""$t('message.noAvailableOptions')""
                        :options=""users""
                        optionLabel=""fullName""
                        optionValue=""id""
                        v-model=""state.{propLower}Id""
                        :invalid=""state.validationErrors.{propLower}Id""
                        :loading=""loadingUsers""
                        :placeholder=""$t('field.selectUser')""
                        class=""w-full""
                        showClear
                        filter
                        @filter=""(e) => searchUsers(e.value)""
                        @change=""(e) => onPropChanged(e.value, '{propLower}Id')""
                        :disabled=""state.finding || state.saving || state.itemPageState === $StoreConstant('VIEW_PAGE_STATE')""
                    >
                        <template #option=""slotProps"">
                            <div class=""flex items-center"">
                                <div>{{{{ slotProps.option.fullName }}}}</div>
                            </div>
                        </template>
                    </Select>
                </div>");
                    }

                    if (rel.Type == RelationType.UserMany)
                    {
                        var propLower = rel.DisplayedProperty.GetCamelCaseName().GetPluralName();
                        sb.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}Names"">{{{{ $t('field.{propLower}Names') }}}}</label>
                    <MultiSelect class=""w-full"" id=""{propLower}"" type=""text"" dataKey=""id""
                        optionLabel=""fullName"" optionValue='id'
                        :placeholder=""$t('field.selectUser')"" 
                        v-model=""state.{propLower}Ids""
                        :options=""users""
                        :loading=""loadingUsers""
                        :maxSelectedLabels=""3""
                        :invalid=""state.validationErrors.{propLower}Names""
                        filter
                        @filter=""(e) => searchUsers(e.value)""
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
