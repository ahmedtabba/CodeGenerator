using SharedClasses;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;

namespace Frontend.VueJsHelper
{
    public class VueJsHelper
    {
        public static string VueJsSolutionPath = "C:\\baseFrontTemplate\\baseFrontTemplateV1.0\\src"; // ضع المسار الجذري لمشروع Vue هنا

        public static void GenerateStoreFile(string entityName, SharedClasses.Properties properties,List<string> notGeneratedTableProperties,List<string> hiddenTableProperties, List<Relation> relations,string srcDir,bool? isParent = null )
        {
            if(srcDir.Length == 0)
                throw new Exception("من فضلك ادخل المسار الجذري لمشروع Vue");
            string fileName = $"{entityName}Store";
            string? directory = isParent != null ? Path.Combine(srcDir,"store", $"{entityName.GetCamelCaseName()}") : null;
            if(directory != null)
                Directory.CreateDirectory(directory);


            string filePath = directory == null ? Path.Combine(srcDir,"store",$"{fileName}.js") : Path.Combine(srcDir, "store",entityName, $"{fileName}.js");
            
            string saveLine = directory == null ? "generalBackend.save(this, REST_ENDPOINT(), PAGE_ROUTE(), item);" : "generalBackend.saveBulk(this, REST_ENDPOINT(), PAGE_ROUTE(), item);";
            string updateLine = directory == null ? "generalBackend.update(this, REST_ENDPOINT(item.id), PAGE_ROUTE(), item);" : "generalBackend.updateBulk(this, REST_ENDPOINT(item.id), null, item, item.id);";
            
            
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
import {{generalActions,generalState}} from '@/store/GeneralStore';
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
                {saveLine}
            }} else {{
                {updateLine}
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
        public static void GeneratePartialStoreFile(string entityName, SharedClasses.Properties properties,List<string> notGeneratedTableProperties,List<string> hiddenTableProperties, List<Relation> relations,string srcDir, string parentEntityName)
        {
            if(srcDir.Length == 0)
                throw new Exception("من فضلك ادخل المسار الجذري لمشروع Vue");
            string fileName = $"{parentEntityName}{entityName}Store";
            string filePath = Path.Combine(srcDir, "store", parentEntityName, $"{fileName}.js");



            var entityLower = entityName.GetCamelCaseName();
            string entityPlural = entityName.GetCapitalName();
            string entityPluralLower = entityPlural.GetCamelCaseName();
            string capitalEntityPlural = entityPlural.GetCapitalName();

            var storeName = $"use{parentEntityName}{entityName}Store";

            var initialStateBuilder = new StringBuilder();
            initialStateBuilder.AppendLine($"        {parentEntityName.GetCamelCaseName()}Id: null,");
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
import {{generalActions,generalState}} from '@/store/GeneralStore';
import i18n from '@/config/i18n';
import {{defineStore}} from 'pinia';
import * as generalBackend from '@/backend/Backend';
import {{{parentEntityName.GetPluralName().GetCapitalName()}_ROUTE as PAGE_ROUTE}} from '@/utils/Constants';

const REST_ENDPOINT = (id) => `{parentEntityName.GetCamelCaseName().GetPluralName()}${{id ? '/' + id : ''}}/{parentEntityName.GetCamelCaseName()}{entityName}`;

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

            console.log('item', item);
            console.log('item.id', this.id);

            generalBackend.updateBulk(this, REST_ENDPOINT(this.{parentEntityName.GetCamelCaseName()}Id), null, item, this.{parentEntityName.GetCamelCaseName()}Id);
        }}
    }},
    persist: {{
        pick: ['selectedColumns', 'itemPageState']
    }}
}});";

            Directory.CreateDirectory(Path.Combine(srcDir, "store"));
            File.WriteAllText(filePath, content);
        }
        public static void GeneratePartialBulkStoreFile(string entityName, SharedClasses.Properties properties,List<string> notGeneratedTableProperties,List<string> hiddenTableProperties, List<Relation> relations,string srcDir, string parentEntityName )
        {
            if(srcDir.Length == 0)
                throw new Exception("من فضلك ادخل المسار الجذري لمشروع Vue");
            string fileName = $"{parentEntityName}{entityName}Store";


            string filePath = Path.Combine(srcDir,"store",parentEntityName,$"{fileName}.js");
            
           
            
            var entityLower = char.ToLower(entityName[0]) + entityName.Substring(1);
            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";
            string entityPluralLower = char.ToLower(entityPlural[0]) + entityPlural.Substring(1);
            string capitalEntityPlural = entityPlural.ToUpper();

            var storeName = $"use{parentEntityName}{entityName}Store";

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
                }
            }

            var content = $@"
import {{ SAVE, SAVE_FAIL, SAVE_ITEM, VALIDATE_FORM }} from '@/utils/StoreConstant';
import {{generalActions,generalState}} from '@/store/GeneralStore';
import i18n from '@/config/i18n';
import {{defineStore}} from 'pinia';
import * as generalBackend from '@/backend/Backend';
import {{{parentEntityName.GetPluralName().GetCapitalName()}_ROUTE as PAGE_ROUTE}} from '@/utils/Constants';

const REST_ENDPOINT = (id) => `{parentEntityName.GetCamelCaseName().GetPluralName()}${{id ? '/' + id : ''}}/{parentEntityName.GetCamelCaseName()}{entityName.GetPluralName()}`;

// all columns options
const ALL_COLUMNS = [{string.Join(',',allColumns)}];

// default columns that will be displayed
const DEFAULT_COLUMNS = [{string.Join(',', defaultColumns)}];

const INITIAL_STATE = {{
    ...generalState(),

    // columns
    allColumns: ALL_COLUMNS,
    defaultColumns: DEFAULT_COLUMNS,
    {parentEntityName.GetCamelCaseName()}Id: null,
    {parentEntityName.GetCamelCaseName()}{entityName.GetPluralName()}: []
}};

export const {storeName} = defineStore('{parentEntityName.GetCamelCaseName()}{entityName}', {{
    state: () => ({{...INITIAL_STATE, selectedColumns: [...DEFAULT_COLUMNS]}}),
    actions: {{
        ...generalActions(INITIAL_STATE, REST_ENDPOINT, PAGE_ROUTE),
        [VALIDATE_FORM]() {{
            const errors = {{}};

            // validation rules

            // validation end

            // if no errors return true, else false
            this.validationErrors = errors;
            return Object.keys(errors).length === 0;
        }},
        async [SAVE_ITEM]() {{
            this[SAVE]();
            const item = this.{parentEntityName.GetCamelCaseName()}{entityName.GetPluralName()}
            // validation
            const isValid = this[VALIDATE_FORM]();
            if (!isValid) {{
                this.sendErrorMessage(i18n.global.t('message.pleaseFillAllRequiredFields'));
                this[SAVE_FAIL]();
                return;
            }}

            generalBackend.updateBulk(this, REST_ENDPOINT(this.{parentEntityName.GetCamelCaseName()}Id), null, item, this.{parentEntityName.GetCamelCaseName()}Id);
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

        public static void GenerateTableView(string entityName, string srcDir, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, List<string> notGeneratedTableProperties, List<string> hiddenTableProperties, List<Relation> relations,bool? isParent = null)
        {
            var entityLower = entityName.GetCamelCaseName();
            string entityPlural = entityName.GetPluralName();
            string capitalEntityPlural = entityPlural.GetCapitalName();
            string entityPluralLower = entityPlural.GetCamelCaseName();
            string viewDirectory = Path.Combine(srcDir, "views", $"{entityLower}");
            Directory.CreateDirectory(viewDirectory);
            string fileTableName = entityPlural;
            string viewTablePath = Path.Combine(viewDirectory, $"{entityPlural}.vue");
            string importStore = isParent == null ? $"import {{ use{entityName}Store as useStore }} from '@/store/{entityName}Store';" : $"import {{use{entityName}Store as useStore}} from '@/store/{entityName.GetCamelCaseName()}/{entityName}Store';";
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
{importStore}
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
        private static string GetBulkTableColomnControl(string entityName, (string Type, string Name, PropertyValidation Validation) prop, List<(string prop, List<string> enumValues)> enumProps)
        {
            var propLower = prop.Name.GetCamelCaseName();
            var typeWithoutNullable = prop.Type.TrimEnd('?');
            if (typeWithoutNullable == "string")
            {
                return $@"
                <Column v-if=""isColumnSelected('{propLower}')"" field=""{propLower}"" :header=""$t('field.{propLower}')"" :show-add-button=""false"" :show-filter-operator=""false"" sortable>
                    <template #body=""{{ data }}"">
                        {{{{ data.{propLower} }}}}
                    </template>
                    <template #filter=""{{ filterModel }}"">
                        <InputText v-model=""filterModel.value"" type=""text"" :placeholder=""$t('field.{propLower}')"" />
                    </template>
                </Column>";
            }
            if ((typeWithoutNullable == "int" && !enumProps.Any(ep => ep.prop == prop.Name)) || typeWithoutNullable == "double" || typeWithoutNullable == "decimal" || typeWithoutNullable == "float")
            {
                return $@"
                <Column v-if=""isColumnSelected('{propLower}')"" field=""{propLower}"" data-type=""numeric"" :header=""$t('field.{propLower}')"" :show-add-button=""false"" :show-filter-operator=""false"" sortable>
                    <template #body=""{{ data }}"">
                        {{{{ data.{propLower} }}}}
                    </template>
                    <template #filter=""{{ filterModel }}"">
                        <InputNumber v-model=""filterModel.value"" :useGrouping=""false"" :placeholder=""$t('field.{propLower}')"" />
                    </template>
                </Column>";
            }

            if (typeWithoutNullable == "int" && enumProps.Any(ep => ep.prop == prop.Name))//enum case
            {
                return $@"
                <Column v-if=""isColumnSelected('{propLower}')"" field=""{propLower}"" :show-add-button=""false"" :show-filter-match-modes=""false"" :show-filter-operator=""false"" :header=""$t('field.{propLower}')"">
                    <template #body=""{{ data }}"">
                        {{{{ getOptionLabel({entityName.GetCamelCaseName()}{prop.Name}Options, data.{propLower}) }}}}
                    </template>
                    <template #filter=""{{ filterModel }}"">
                        <Select v-model=""filterModel.value"" :options=""{entityName.GetCamelCaseName()}{prop.Name}Options"" option-value=""value"" option-label=""label"" showClear :placeholder=""$t('field.{propLower}')""></Select>
                    </template>
                </Column>";
            }
            if (typeWithoutNullable.Contains("Date") || typeWithoutNullable.Contains("Time"))
            {
                return $@"
                <Column v-if=""isColumnSelected('{propLower}')"" field=""{propLower}"" data-type=""date"" :show-filter-operator=""false"" :header=""$t('field.{propLower}')"" sortable>
                    <template #body=""{{ data }}"">
                        {{{{ formattedDate(data.{propLower}) }}}}
                    </template>
                    <template #filter=""{{ filterModel }}"">
                        <DatePicker v-model=""filterModel.value"" dateFormat=""dd/mm/yy"" :manualInput=""false"" placeholder=""dd/mm/yyyy"" />
                    </template>
                </Column>";
            }
            if (typeWithoutNullable == "bool")
            {
                return $@"
                <Column v-if=""isColumnSelected('{propLower}')"" field=""{propLower}"" :show-add-button=""false"" :show-filter-match-modes=""false"" :show-filter-operator=""false"" :header=""$t('field.{propLower}')"">
                    <template #body=""{{ data }}"">
                        <i class=""pi"" :class=""{{ 'pi-check-circle text-green-500 ': data.{propLower}, 'pi-times-circle text-red-500': !data.{propLower} }}""></i>
                    </template>
                    <template #filter=""{{ filterModel }}"">
                        <Checkbox v-model=""filterModel.value"" :indeterminate=""filterModel.value === null"" binary inputId=""{propLower}"" />
                    </template>
                </Column>";
            }

            if (typeWithoutNullable == "VD" || typeWithoutNullable == "GPG" || typeWithoutNullable == "PNGs" || typeWithoutNullable == "VDs" || typeWithoutNullable == "FLs" || typeWithoutNullable == "FL")
            {
                //TODO : handle assets states
                return null;
            }

            return $@"
            <Column v-if=""isColumnSelected('{propLower}')"" field=""{propLower}"" :header=""$t('field.{propLower}')"" :show-add-button=""false"" :show-filter-operator=""false"" sortable>
                    <template #body=""{{ data }}"">
                        {{{{ data.{propLower} }}}}
                    </template>
                    <template #filter=""{{ filterModel }}"">
                        <InputText v-model=""filterModel.value"" type=""text"" :placeholder=""$t('field.{propLower}')"" />
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
        private static string GetBulkTableColomnRelationControl(string entityName, List<Relation> relations)
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
                            var propLower = rel.Type != RelationType.OneToOneSelfJoin ? lowerRelatedEntity + rel.DisplayedProperty
                                : lowerRelatedEntity + $"Parent{rel.DisplayedProperty}";
                            var propLowerData = rel.Type != RelationType.OneToOneSelfJoin ? lowerRelatedEntity + "Id"
                                : lowerRelatedEntity + $"ParentId";

                            sb.AppendLine($@"
                <Column v-if=""isColumnSelected('{propLower}')"" field=""{propLower}"" :show-add-button=""false"" :show-filter-match-modes=""false"" :show-filter-operator=""false"" :header=""$t('field.{propLower}')"">
                    <template #body=""{{ data }}"">
                        {{{{ getOptionLabel({entityRelatedPluralLower}, data.{propLowerData}, 'id', '{displayedProp}') }}}}
                    </template>
                    <template #filter=""{{ filterModel }}"">
                        <Select v-model=""filterModel.value"" :options=""{entityRelatedPluralLower}"" :loading=""loading{entityRelatedPlural}"" option-value=""id"" option-label=""{displayedProp}"" showClear :placeholder=""$t('field.{propLower}')""></Select>
                    </template>
                </Column>");
                        }
                        if (rel.Type == RelationType.ManyToMany)
                        {
                            string entityRelatedPlural = rel.RelatedEntity.GetPluralName();
                            string entityRelatedPluralLower = entityRelatedPlural.GetCamelCaseName();
                            var propLower = entityRelatedPluralLower + rel.DisplayedProperty.GetPluralName();
                            var displayedProp = rel.DisplayedProperty.GetCamelCaseName();
                            sb.AppendLine($@"
                <Column v-if=""isColumnSelected('{propLower}')"" field=""{propLower}"" :header=""$t('field.{propLower}')"" :show-add-button=""false"" :show-filter-match-modes=""false"" :show-filter-operator=""false"">
                    <template #body=""{{ data }}"">
                        <span v-for=""(item, index) in data.{entityRelatedPluralLower}Ids"" :key=""index"" class=""me-2"">{{{{ getOptionLabel({entityRelatedPluralLower}, item, 'id', '{displayedProp}') }}}} <span v-if=""index < data.{entityRelatedPluralLower}Ids.length - 1"">,</span> </span>
                    </template>
                    <template #filter=""{{ filterModel }}"">
                        <MultiSelect
                            v-model=""filterModel.value""
                            :show-toggle-all=""false""
                            append-to=""self""
                            :options=""{entityRelatedPluralLower}""
                            :loading=""loading{entityRelatedPlural}""
                            option-label=""{displayedProp}""
                            option-value=""id""
                            :maxSelectedLabels=""1""
                            showClear
                            :placeholder=""$t('field.{propLower}')""
                        />
                    </template>
                </Column>");
                        }

                        if (rel.Type == RelationType.UserSingle || rel.Type == RelationType.UserSingleNullable)
                        {
                            var propLower = rel.DisplayedProperty.GetCamelCaseName() + "Name";
                            var propLowerData = rel.DisplayedProperty.GetCamelCaseName() + "Id";
                            sb.AppendLine($@"
                <Column v-if=""isColumnSelected('{propLower}')"" field=""{propLower}"" :show-add-button=""false"" :show-filter-match-modes=""false"" :show-filter-operator=""false"" :header=""$t('field.{propLower}')"">
                    <template #body=""{{ data }}"">
                        {{{{ getOptionLabel(users, data.{propLowerData}, 'id', 'fullName') }}}}
                    </template>
                    <template #filter=""{{ filterModel }}"">
                        <Select v-model=""filterModel.value"" :options=""users"" :loading=""loadingUsers"" option-value=""id"" option-label=""fullName"" showClear :placeholder=""$t('field.{propLower}')""></Select>
                    </template>
                </Column>");
                        }

                        if (rel.Type == RelationType.UserMany)
                        {
                            var propLower = rel.DisplayedProperty.GetCamelCaseName().GetPluralName() + "Names";
                            var propLowerData = rel.DisplayedProperty.GetCamelCaseName().GetPluralName() + "Ids";
                            sb.AppendLine($@"
                <Column v-if=""isColumnSelected('{propLower}')"" field=""{propLower}"" :header=""$t('field.{propLower}')"" :show-add-button=""false"" :show-filter-match-modes=""false"" :show-filter-operator=""false"">
                    <template #body=""{{ data }}"">
                        <span v-for=""(item, index) in data.{propLowerData}"" :key=""index"" class=""me-2"">{{{{ getOptionLabel(users, item, 'id', 'fullName') }}}} <span v-if=""index < data.{propLowerData}.length - 1"">,</span> </span>
                    </template>
                    <template #filter=""{{ filterModel }}"">
                        <MultiSelect
                            v-model=""filterModel.value""
                            :show-toggle-all=""false""
                            append-to=""self""
                            :options=""users""
                            :loading=""loadingUsers""
                            option-label=""fullName""
                            option-value=""id""
                            :maxSelectedLabels=""1""
                            showClear
                            :placeholder=""$t('field.{propLower}')""
                        />
                    </template>
                </Column>");
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
            string? filePreviewAndNameHelper = null;
            string? previewDialog = null;
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

            string? relationColomn = GetSingleColomnRelationControl(entityName, relations);
            if (relationColomn != null)
                colomnBuilder.AppendLine(relationColomn);

            string? colomnFLs = null;
            foreach (var prop in properties)
            {
                #region script
                if (prop.Type == "GPG")
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
                    filePreviewAndNameHelper = $@"
// preview related + name helper
const isPreviewModalOpen = ref(false);
const selectedPreviewFile = ref(null);

const openPreviewModal = (file) => {{
    selectedPreviewFile.value = file;
    isPreviewModalOpen.value = true;
}};

const shortenFileName = (name) => {{
    const maxLength = 15;

    // separate name and extension
    const dotIndex = name.lastIndexOf('.');
    if (dotIndex === -1 || dotIndex === 0) return name;

    const base = name.slice(0, dotIndex);
    const ext = name.slice(dotIndex); // includes the "".""

    if (base.length <= maxLength * 2) return name;

    const first = base.slice(0, maxLength);
    const last = base.slice(-maxLength);
    return `${{first}}...${{last}}${{ext}}`;
}};";
                    previewDialog = $@"
        <Dialog v-model:visible=""isPreviewModalOpen"" :style=""{{ width: '95%', height: '90%' }}"" :header=""$t('title.filePreview')"" :modal=""true"" :draggable=""false"" :dismissableMask=""true"" block-scroll>
            <div class=""!h-full"">
                <VueFilesPreview v-if=""selectedPreviewFile"" :file=""selectedPreviewFile"" overflow=""auto"" />
            </div>
        </Dialog>
 
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
// —————————————

// Archive files only (zip / rar)
const archiveFiles = computed(() => previews.value.filter((p) => isArchive(p.name)));
";
                    }
                }
                #endregion

                string colomn = null;
                if (prop.Type != "FLs")
                {
                    colomn = GetSingleColomnControl(entityName, prop, enumProps);
                    colomnBuilder.AppendLine(colomn);
                }
                else
                    colomnFLs = GetSingleColomnControl(entityName, prop, enumProps);
            }
            if (colomnFLs != null)
                colomnBuilder.AppendLine(colomnFLs);

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
{filePreviewAndNameHelper}

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
{previewDialog}
    </div>
</template>";
            
            File.WriteAllText(viewSinglePath, content);
        }
        public static void GenerateParentBasicInfoView(string entityName, string srcDir, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps,List<Relation> relations,bool hasAssets)
        {
            var entityLower = entityName.GetCamelCaseName();
            string entityPlural = entityName.GetPluralName();
            string capitalEntityPlural = entityPlural.GetCapitalName();
            string entityPluralLower = entityPlural.GetCamelCaseName();
            string viewParentDirectory = Path.Combine(srcDir, "views", $"{entityLower}");
            Directory.CreateDirectory(viewParentDirectory);
            string viewDirectory = Path.Combine(srcDir, "views", $"{entityLower}","parts");
            Directory.CreateDirectory(viewDirectory);

            //string fileSingleName = entityName;
            string viewSinglePath = Path.Combine(viewDirectory, $"{entityName}BasicInfo.vue");
            //string? fileImportsRef = properties.Any(p => p.Type == "FL" || p.Type == "FLs") ? ", watch, onUnmounted, computed" : null;
            //string? fileImportPreview = properties.Any(p => p.Type == "FL" || p.Type == "FLs") ? "import { VueFilesPreview } from 'vue-files-preview';" : null;
            
            //string ? importRef = hasAssets ? $"import {{ ref{fileImportsRef} }} from 'vue';" : null;
            //string? importAssetEndpoint = hasAssets ? ", ASSET_ENDPOINT" : null;
            //string? assetSection = null;
            //string? assetListSection = null;
            //string? videoSection = null;
            //string? videoListSection = null;
            //string? fileHelperSection = null;
            //string? fileSection = null;
            //string? fileListSection = null;
            //string? filePreviewAndNameHelper = null;
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

            string? relationColomn = GetSingleColomnRelationControl(entityName, relations);
            if (relationColomn != null)
                colomnBuilder.AppendLine(relationColomn);

            //string? colomnFLs = null;
            foreach (var prop in properties)
            {
                #region script
//                if (prop.Type == "GPG")
//                {
//                    assetSection = $@"
//// Single Asset Section
//const assetSrc = ref(null);
//const onSelectAsset = (e) => {{
//    // remove old asset
//    store.delete{prop.Name} = true;
//    store.{prop.Name.GetCamelCaseName()}Url = null;

//    const file = e.files[0];
//    onPropChanged(file, '{prop.Name.GetCamelCaseName()}');
//    const reader = new FileReader();
//    reader.onload = async (e) => {{
//        assetSrc.value = e.target.result;
//    }};
//    reader.readAsDataURL(file);
//}};

//const removeAsset = () => {{
//    // Reset delete asset
//    store.delete{prop.Name} = true;
//    store.{prop.Name.GetCamelCaseName()}Url = null;
//    onPropChanged(null, '{prop.Name.GetCamelCaseName()}');
//    assetSrc.value = null;
//}};";
//                }

//                else if(prop.Type == "PNGs")
//                {
//                    assetListSection = $@"
//// Multiple Assets Section
//const assetSrcs = ref([]); // New uploaded previews
//const selectedFiles = ref([]); // Actual new File objects

//const onSelectAssets = (event) => {{
//    // Convert FileList to array
//    const filesArray = Array.from(event.files);

//    filesArray.forEach((file) => {{
//        // Add to array that will go into state.assets
//        selectedFiles.value.push(file);

//        // Create a preview URL and push into assetSrcs
//        const reader = new FileReader();
//        reader.onload = (evt) => {{
//            assetSrcs.value.push(evt.target.result);
//        }};
//        reader.readAsDataURL(file);
//    }});

//    onPropChanged(selectedFiles.value, '{prop.Name.GetCamelCaseName()}');
//}};

//// Remove new uploaded asset
//const removeNewAsset = (index) => {{
//    assetSrcs.value.splice(index, 1);
//    selectedFiles.value.splice(index, 1);
//    onPropChanged(selectedFiles.value, '{prop.Name.GetCamelCaseName()}');
//}};

//// Remove existing backend asset
//const removeExistingAsset = (index) => {{
//    const removedUrl = store.{prop.Name.GetCamelCaseName()}Urls[index];
//    store.deleted{prop.Name}Urls.push(removedUrl); // add to store.deleted{prop.Name}Urls
//    store.{prop.Name.GetCamelCaseName()}Urls.splice(index, 1); // Remove from store.{prop.Name.GetCamelCaseName()}Urls (UI)
//}};";
//                }

//                else if (prop.Type == "VD")
//                {
//                    videoSection = $@"
//// upload single video
//const videoSrc = ref(null);
//const onSelectVideo = (e) => {{
//    // reset old video
//    store.delete{prop.Name} = true;
//    store.{prop.Name.GetCamelCaseName()}Url = null;

//    const file = e.files[0];
//    onPropChanged(file, '{prop.Name.GetCamelCaseName()}');
//    const reader = new FileReader();
//    reader.onload = async (e) => {{
//        videoSrc.value = e.target.result;
//    }};
//    reader.readAsDataURL(file);
//}};

//const removeVideo = () => {{
//    // Reset delete video
//    store.delete{prop.Name} = true;
//    store.{prop.Name.GetCamelCaseName()}Url = null;
//    onPropChanged(null, '{prop.Name.GetCamelCaseName()}');
//    videoSrc.value = null;
//}};";
//                }
//                else if (prop.Type == "VDs")
//                {
//                    videoListSection = $@"
//// upload multiple videos
//const videoSrcs = ref([]); // New uploaded previews
//const selectedVideos = ref([]); // Actual new File objects

//const onSelectVideos = (event) => {{
//    // Convert FileList to array
//    const filesArray = Array.from(event.files);

//    filesArray.forEach((file) => {{
//        // Add to array that will go into state.assets
//        selectedVideos.value.push(file);

//        // Create a preview URL and push into videoSrcs
//        const reader = new FileReader();
//        reader.onload = (evt) => {{
//            videoSrcs.value.push(evt.target.result);
//        }};
//        reader.readAsDataURL(file);
//    }});

//    onPropChanged(selectedVideos.value, '{prop.Name.GetCamelCaseName()}');
//}};

//const removeNewVideo = (index) => {{
//    videoSrcs.value.splice(index, 1);
//    selectedVideos.value.splice(index, 1);
//    onPropChanged(selectedVideos.value, '{prop.Name.GetCamelCaseName()}');
//}}

//const removeExistingVideo = (index) => {{
//    const removedUrl = store.{prop.Name.GetCamelCaseName()}Urls[index];
//    store.deleted{prop.Name}Urls.push(removedUrl); // add to store.deleted{prop.Name}Urls
//    store.{prop.Name.GetCamelCaseName()}Urls.splice(index, 1); // Remove from store.{prop.Name.GetCamelCaseName()}Urls (UI)
//}}";
//                }
//                else if(prop.Type == "FL" || prop.Type == "FLs")
//                {
//                    fileHelperSection = $@"
//// FILES SECTION

//const newFiles = ref([]);
//const previews = ref([]); // our unified preview items

//// Helper: fetch a URL → Blob → File
//async function fetchUrlAsFile(url) {{
//    const resp = await fetch(url);
//    if (!resp.ok) throw new Error(`Fetch failed: ${{resp.status}}`);
//    const blob = await resp.blob();
//    const filename = url.split('/').pop().slice(37);
//    return new File([blob], filename, {{ type: blob.type }});
//}}

//// Helper: returns true if the file is an archive we can’t preview
//function isArchive(fileName) {{
//    return /\.(zip|rar)$/i.test(fileName);
//}}

//// Helper: Build a preview‐object from a File
//function makePreviewObj(file, source) {{
//    const downloadUrl = URL.createObjectURL(file);
//    return {{
//        source, // 'remote' or 'new'
//        file,
//        downloadUrl,
//        name: file.name,
//        type: file.type || '—',
//        size: file.size
//    }};
//}}
//";
//                    filePreviewAndNameHelper = $@"
//// preview related + name helper
//const isPreviewModalOpen = ref(false);
//const selectedPreviewFile = ref(null);

//const openPreviewModal = (file) => {{
//    selectedPreviewFile.value = file;
//    isPreviewModalOpen.value = true;
//}};

//const shortenFileName = (name) => {{
//    const maxLength = 15;

//    // separate name and extension
//    const dotIndex = name.lastIndexOf('.');
//    if (dotIndex === -1 || dotIndex === 0) return name;

//    const base = name.slice(0, dotIndex);
//    const ext = name.slice(dotIndex); // includes the "".""

//    if (base.length <= maxLength * 2) return name;

//    const first = base.slice(0, maxLength);
//    const last = base.slice(-maxLength);
//    return `${{first}}...${{last}}${{ext}}`;
//}};";
//                    previewDialog = $@"
//        <Dialog v-model:visible=""isPreviewModalOpen"" :style=""{{ width: '95%', height: '90%' }}"" :header=""$t('title.filePreview')"" :modal=""true"" :draggable=""false"" :dismissableMask=""true"" block-scroll>
//            <div class=""!h-full"">
//                <VueFilesPreview v-if=""selectedPreviewFile"" :file=""selectedPreviewFile"" overflow=""auto"" />
//            </div>
//        </Dialog>
 
//";
//                    if (prop.Type == "FL")
//                    {
//                        fileSection = $@"
//// —————————————
//// SINGLE FILE SECTION
//// —————————————

//// 1. refs to hold the chosen file and its preview URL
//const singleFile = ref(null);
//const singlePreviewUrl = ref(null);

//// 2. Helper to clear current single-file state
//function clearSingle() {{
//    if (singlePreviewUrl.value) {{
//        URL.revokeObjectURL(singlePreviewUrl.value);
//    }}
//    singleFile.value = null;
//    singlePreviewUrl.value = null;
//    onPropChanged(null, '{prop.Name.GetCamelCaseName()}'); // reset in store // <-- [property]
//}}

//// 3. Watch the backend’s fileUrl
//watch(
//    () => store.{prop.Name.GetCamelCaseName()}Url, // <-- store.[fileUrl]
//    async (url) => {{
//        clearSingle();
//        // if there’s a URL, fetch and convert
//        if (url) {{
//            try {{
//                const fullUrl = ASSET_ENDPOINT(url);
//                const file = await fetchUrlAsFile(fullUrl);
//                singleFile.value = file;

//                // create a blob URL for preview & download
//                singlePreviewUrl.value = URL.createObjectURL(file);
//            }} catch (err) {{
//                console.error('Failed to load single file from', url, err);
//            }}
//        }}
//    }},
//    {{ immediate: true }}
//);

//// When the user picks a new file manually, clear any backend one:
//function onSelectFile(event) {{
//    clearSingle();

//    const file = event.files[0];
//    if (!file) return;
//    singleFile.value = file;
//    singlePreviewUrl.value = URL.createObjectURL(file);
//    onPropChanged(file, '{prop.Name.GetCamelCaseName()}');// <-- [property]

//    store.delete{prop.Name} = true; //<-- store.[property]
//}}

//// Remove single file
//function removeFile() {{
//    clearSingle();
//    store.delete{prop.Name} = true;// <-- store.[property]
//    store.{prop.Name.GetCamelCaseName()}Url = null;// <-- store.[property]
//}}

//onUnmounted(() => {{
//    clearSingle();
//}});
//";
//                    }
//                    else
//                    {
//                        fileListSection = $@"
//// —————————————
//// MULTIPLE FILES SECTION
//// —————————————

//// Watch backend URLs: whenever store.fileUrls changes, re-build those previews
//watch(
//    () => store.{prop.Name.GetCamelCaseName()}Urls,//<-- store.[property]
//    async (urls) => {{
//        previews.value = previews.value.filter((p) => p.source === 'new');

//        // then fetch each URL and add to previews
//        for (const url of urls) {{
//            try {{
//                const fullUrl = ASSET_ENDPOINT(url);
//                const file = await fetchUrlAsFile(fullUrl);
//                previews.value.push(makePreviewObj(file, 'remote'));
//            }} catch (err) {{
//                console.error('Error fetching', url, err);
//            }}
//        }}
//    }},
//    {{ immediate: true }}
//);

//// Handle new uploads
//function onSelectFiles(event) {{
//    const files = Array.from(event.files);
//    for (const f of files) {{
//        newFiles.value.push(f);
//        previews.value.push(makePreviewObj(f, 'new'));
//    }}
//    onPropChanged(newFiles.value, '{prop.Name.GetCamelCaseName()}');// [property]
//}}

//// Remove a preview (both UI and store)
//function removePreview(index) {{
//    const p = previews.value[index];
//    URL.revokeObjectURL(p.downloadUrl);
//    previews.value.splice(index, 1);

//    if (p.source === 'remote') {{
//        // tell store to delete this URL
//        store.deleted{prop.Name}Urls.push(store.{prop.Name.GetCamelCaseName()}Urls[index]);//<-- store.[property] / store.[property]
//        store.{prop.Name.GetCamelCaseName()}Urls.splice(index, 1);//<-- store.[property]
//    }} else {{
//        // a new file: remove from newFiles
//        const nfIndex = newFiles.value.findIndex((f) => f === p.file);
//        if (nfIndex > -1) newFiles.value.splice(nfIndex, 1);
//        onPropChanged(newFiles.value, '{prop.Name.GetCamelCaseName()}');//<-- [property]
//    }}
//}}

//// Cleanup on unmount
//onUnmounted(() => {{
//    for (const p of previews.value) {{
//        URL.revokeObjectURL(p.downloadUrl);
//    }}
//}});

//// Files we can show with <VueFilesPreview>
//const previewableFiles = computed(() => previews.value.filter((p) => !isArchive(p.name)));
//// —————————————

//// Archive files only (zip / rar)
//const archiveFiles = computed(() => previews.value.filter((p) => isArchive(p.name)));
//";
//                    }
//                }
                #endregion

                string colomn = null;
                //if (prop.Type != "FLs")
                //{
                    colomn = GetSingleColomnControl(entityName, prop, enumProps);
                    colomnBuilder.AppendLine(colomn);
                //}
                //else
                //    colomnFLs = GetSingleColomnControl(entityName, prop, enumProps);
            }
            //if (colomnFLs != null)
            //    colomnBuilder.AppendLine(colomnFLs);

            string content = $@"
<script setup>
import {{ use{entityName}Store as useStore }} from '@/store/{entityName.GetCamelCaseName()}/{entityName}Store';
import {{ {capitalEntityPlural}_ROUTE as PAGE_ROUTE }} from '@/utils/Constants';
{relationImports}
import useSingle from '@/composables/useSingle';
import useList from '@/composables/useList';
import {{EDIT_ITEM,FIND_ITEM}} from '@/utils/StoreConstant';

const store = useStore();

const {{ state, onPropChanged, onSave, t }} = useSingle(store, PAGE_ROUTE);
{relationConsts}
{enumConsts}

const handleCancel = () => {{
    if (store.id) {{
        store[FIND_ITEM]({{
            id: store.id,
            viewState: EDIT_ITEM
        }});
    }} else {{
        store.$reset();
    }}
}};
</script>
<template>
    <div class=""theCard"">
        <h2 class=""text-3xl font-semibold"">{{{{ $t('title.{entityName}BasicInfo') }}}}</h2>

        <div v-if=""store.finding"" class=""flex justify-center items-center w-full mt-[32px]"" >
            <atom-spinner :size=""50"" color=""#1B80E4"" />
        </div>

        <!-- form -->
        <form v-else @submit.prevent="""" class=""mt-12 w-full"">
            <div class=""grid grid-cols-1 md:grid-cols-2 gap-4 w-full"">                                      
{colomnBuilder}
            </div>
            <!-- actions -->
            <div class=""flex items-center gap-5 mt-12 w-full"">
                <Button severity=""primary"" text :label=""$t('button.cancel')"" class=""rounded float-start"" outlined @click=""handleCancel"" :disabled=""state.saving""></Button>
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
        public static void GeneratePartialFormView(string entityName, string srcDir, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps,List<Relation> relations,bool hasAssets,string parentEntityName)
        {
            var entityLower = entityName.GetCamelCaseName();
            string entityPlural = entityName.GetPluralName();
            string capitalEntityPlural = entityPlural.GetCapitalName();
            string entityPluralLower = entityPlural.GetCamelCaseName();
            string viewParentDirectory = Path.Combine(srcDir, "views", $"{parentEntityName.GetCamelCaseName()}");
            Directory.CreateDirectory(viewParentDirectory);
            string viewDirectory = Path.Combine(srcDir, "views", $"{parentEntityName.GetCamelCaseName()}","parts");
            Directory.CreateDirectory(viewDirectory);

            //string fileSingleName = entityName;
            string viewSinglePath = Path.Combine(viewDirectory, $"{parentEntityName}{entityName}.vue");
            //string? fileImportsRef = properties.Any(p => p.Type == "FL" || p.Type == "FLs") ? ", watch, onUnmounted, computed" : null;
            //string? fileImportPreview = properties.Any(p => p.Type == "FL" || p.Type == "FLs") ? "import { VueFilesPreview } from 'vue-files-preview';" : null;
            
            //string ? importRef = hasAssets ? $"import {{ ref{fileImportsRef} }} from 'vue';" : null;
            //string? importAssetEndpoint = hasAssets ? ", ASSET_ENDPOINT" : null;
            //string? assetSection = null;
            //string? assetListSection = null;
            //string? videoSection = null;
            //string? videoListSection = null;
            //string? fileHelperSection = null;
            //string? fileSection = null;
            //string? fileListSection = null;
            //string? filePreviewAndNameHelper = null;
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

            string? relationColomn = GetSingleColomnRelationControl(entityName, relations);
            if (relationColomn != null)
                colomnBuilder.AppendLine(relationColomn);

            //string? colomnFLs = null;
            foreach (var prop in properties)
            {
                #region script
//                if (prop.Type == "GPG")
//                {
//                    assetSection = $@"
//// Single Asset Section
//const assetSrc = ref(null);
//const onSelectAsset = (e) => {{
//    // remove old asset
//    store.delete{prop.Name} = true;
//    store.{prop.Name.GetCamelCaseName()}Url = null;

//    const file = e.files[0];
//    onPropChanged(file, '{prop.Name.GetCamelCaseName()}');
//    const reader = new FileReader();
//    reader.onload = async (e) => {{
//        assetSrc.value = e.target.result;
//    }};
//    reader.readAsDataURL(file);
//}};

//const removeAsset = () => {{
//    // Reset delete asset
//    store.delete{prop.Name} = true;
//    store.{prop.Name.GetCamelCaseName()}Url = null;
//    onPropChanged(null, '{prop.Name.GetCamelCaseName()}');
//    assetSrc.value = null;
//}};";
//                }

//                else if(prop.Type == "PNGs")
//                {
//                    assetListSection = $@"
//// Multiple Assets Section
//const assetSrcs = ref([]); // New uploaded previews
//const selectedFiles = ref([]); // Actual new File objects

//const onSelectAssets = (event) => {{
//    // Convert FileList to array
//    const filesArray = Array.from(event.files);

//    filesArray.forEach((file) => {{
//        // Add to array that will go into state.assets
//        selectedFiles.value.push(file);

//        // Create a preview URL and push into assetSrcs
//        const reader = new FileReader();
//        reader.onload = (evt) => {{
//            assetSrcs.value.push(evt.target.result);
//        }};
//        reader.readAsDataURL(file);
//    }});

//    onPropChanged(selectedFiles.value, '{prop.Name.GetCamelCaseName()}');
//}};

//// Remove new uploaded asset
//const removeNewAsset = (index) => {{
//    assetSrcs.value.splice(index, 1);
//    selectedFiles.value.splice(index, 1);
//    onPropChanged(selectedFiles.value, '{prop.Name.GetCamelCaseName()}');
//}};

//// Remove existing backend asset
//const removeExistingAsset = (index) => {{
//    const removedUrl = store.{prop.Name.GetCamelCaseName()}Urls[index];
//    store.deleted{prop.Name}Urls.push(removedUrl); // add to store.deleted{prop.Name}Urls
//    store.{prop.Name.GetCamelCaseName()}Urls.splice(index, 1); // Remove from store.{prop.Name.GetCamelCaseName()}Urls (UI)
//}};";
//                }

//                else if (prop.Type == "VD")
//                {
//                    videoSection = $@"
//// upload single video
//const videoSrc = ref(null);
//const onSelectVideo = (e) => {{
//    // reset old video
//    store.delete{prop.Name} = true;
//    store.{prop.Name.GetCamelCaseName()}Url = null;

//    const file = e.files[0];
//    onPropChanged(file, '{prop.Name.GetCamelCaseName()}');
//    const reader = new FileReader();
//    reader.onload = async (e) => {{
//        videoSrc.value = e.target.result;
//    }};
//    reader.readAsDataURL(file);
//}};

//const removeVideo = () => {{
//    // Reset delete video
//    store.delete{prop.Name} = true;
//    store.{prop.Name.GetCamelCaseName()}Url = null;
//    onPropChanged(null, '{prop.Name.GetCamelCaseName()}');
//    videoSrc.value = null;
//}};";
//                }
//                else if (prop.Type == "VDs")
//                {
//                    videoListSection = $@"
//// upload multiple videos
//const videoSrcs = ref([]); // New uploaded previews
//const selectedVideos = ref([]); // Actual new File objects

//const onSelectVideos = (event) => {{
//    // Convert FileList to array
//    const filesArray = Array.from(event.files);

//    filesArray.forEach((file) => {{
//        // Add to array that will go into state.assets
//        selectedVideos.value.push(file);

//        // Create a preview URL and push into videoSrcs
//        const reader = new FileReader();
//        reader.onload = (evt) => {{
//            videoSrcs.value.push(evt.target.result);
//        }};
//        reader.readAsDataURL(file);
//    }});

//    onPropChanged(selectedVideos.value, '{prop.Name.GetCamelCaseName()}');
//}};

//const removeNewVideo = (index) => {{
//    videoSrcs.value.splice(index, 1);
//    selectedVideos.value.splice(index, 1);
//    onPropChanged(selectedVideos.value, '{prop.Name.GetCamelCaseName()}');
//}}

//const removeExistingVideo = (index) => {{
//    const removedUrl = store.{prop.Name.GetCamelCaseName()}Urls[index];
//    store.deleted{prop.Name}Urls.push(removedUrl); // add to store.deleted{prop.Name}Urls
//    store.{prop.Name.GetCamelCaseName()}Urls.splice(index, 1); // Remove from store.{prop.Name.GetCamelCaseName()}Urls (UI)
//}}";
//                }
//                else if(prop.Type == "FL" || prop.Type == "FLs")
//                {
//                    fileHelperSection = $@"
//// FILES SECTION

//const newFiles = ref([]);
//const previews = ref([]); // our unified preview items

//// Helper: fetch a URL → Blob → File
//async function fetchUrlAsFile(url) {{
//    const resp = await fetch(url);
//    if (!resp.ok) throw new Error(`Fetch failed: ${{resp.status}}`);
//    const blob = await resp.blob();
//    const filename = url.split('/').pop().slice(37);
//    return new File([blob], filename, {{ type: blob.type }});
//}}

//// Helper: returns true if the file is an archive we can’t preview
//function isArchive(fileName) {{
//    return /\.(zip|rar)$/i.test(fileName);
//}}

//// Helper: Build a preview‐object from a File
//function makePreviewObj(file, source) {{
//    const downloadUrl = URL.createObjectURL(file);
//    return {{
//        source, // 'remote' or 'new'
//        file,
//        downloadUrl,
//        name: file.name,
//        type: file.type || '—',
//        size: file.size
//    }};
//}}
//";
//                    filePreviewAndNameHelper = $@"
//// preview related + name helper
//const isPreviewModalOpen = ref(false);
//const selectedPreviewFile = ref(null);

//const openPreviewModal = (file) => {{
//    selectedPreviewFile.value = file;
//    isPreviewModalOpen.value = true;
//}};

//const shortenFileName = (name) => {{
//    const maxLength = 15;

//    // separate name and extension
//    const dotIndex = name.lastIndexOf('.');
//    if (dotIndex === -1 || dotIndex === 0) return name;

//    const base = name.slice(0, dotIndex);
//    const ext = name.slice(dotIndex); // includes the "".""

//    if (base.length <= maxLength * 2) return name;

//    const first = base.slice(0, maxLength);
//    const last = base.slice(-maxLength);
//    return `${{first}}...${{last}}${{ext}}`;
//}};";
//                    previewDialog = $@"
//        <Dialog v-model:visible=""isPreviewModalOpen"" :style=""{{ width: '95%', height: '90%' }}"" :header=""$t('title.filePreview')"" :modal=""true"" :draggable=""false"" :dismissableMask=""true"" block-scroll>
//            <div class=""!h-full"">
//                <VueFilesPreview v-if=""selectedPreviewFile"" :file=""selectedPreviewFile"" overflow=""auto"" />
//            </div>
//        </Dialog>
 
//";
//                    if (prop.Type == "FL")
//                    {
//                        fileSection = $@"
//// —————————————
//// SINGLE FILE SECTION
//// —————————————

//// 1. refs to hold the chosen file and its preview URL
//const singleFile = ref(null);
//const singlePreviewUrl = ref(null);

//// 2. Helper to clear current single-file state
//function clearSingle() {{
//    if (singlePreviewUrl.value) {{
//        URL.revokeObjectURL(singlePreviewUrl.value);
//    }}
//    singleFile.value = null;
//    singlePreviewUrl.value = null;
//    onPropChanged(null, '{prop.Name.GetCamelCaseName()}'); // reset in store // <-- [property]
//}}

//// 3. Watch the backend’s fileUrl
//watch(
//    () => store.{prop.Name.GetCamelCaseName()}Url, // <-- store.[fileUrl]
//    async (url) => {{
//        clearSingle();
//        // if there’s a URL, fetch and convert
//        if (url) {{
//            try {{
//                const fullUrl = ASSET_ENDPOINT(url);
//                const file = await fetchUrlAsFile(fullUrl);
//                singleFile.value = file;

//                // create a blob URL for preview & download
//                singlePreviewUrl.value = URL.createObjectURL(file);
//            }} catch (err) {{
//                console.error('Failed to load single file from', url, err);
//            }}
//        }}
//    }},
//    {{ immediate: true }}
//);

//// When the user picks a new file manually, clear any backend one:
//function onSelectFile(event) {{
//    clearSingle();

//    const file = event.files[0];
//    if (!file) return;
//    singleFile.value = file;
//    singlePreviewUrl.value = URL.createObjectURL(file);
//    onPropChanged(file, '{prop.Name.GetCamelCaseName()}');// <-- [property]

//    store.delete{prop.Name} = true; //<-- store.[property]
//}}

//// Remove single file
//function removeFile() {{
//    clearSingle();
//    store.delete{prop.Name} = true;// <-- store.[property]
//    store.{prop.Name.GetCamelCaseName()}Url = null;// <-- store.[property]
//}}

//onUnmounted(() => {{
//    clearSingle();
//}});
//";
//                    }
//                    else
//                    {
//                        fileListSection = $@"
//// —————————————
//// MULTIPLE FILES SECTION
//// —————————————

//// Watch backend URLs: whenever store.fileUrls changes, re-build those previews
//watch(
//    () => store.{prop.Name.GetCamelCaseName()}Urls,//<-- store.[property]
//    async (urls) => {{
//        previews.value = previews.value.filter((p) => p.source === 'new');

//        // then fetch each URL and add to previews
//        for (const url of urls) {{
//            try {{
//                const fullUrl = ASSET_ENDPOINT(url);
//                const file = await fetchUrlAsFile(fullUrl);
//                previews.value.push(makePreviewObj(file, 'remote'));
//            }} catch (err) {{
//                console.error('Error fetching', url, err);
//            }}
//        }}
//    }},
//    {{ immediate: true }}
//);

//// Handle new uploads
//function onSelectFiles(event) {{
//    const files = Array.from(event.files);
//    for (const f of files) {{
//        newFiles.value.push(f);
//        previews.value.push(makePreviewObj(f, 'new'));
//    }}
//    onPropChanged(newFiles.value, '{prop.Name.GetCamelCaseName()}');// [property]
//}}

//// Remove a preview (both UI and store)
//function removePreview(index) {{
//    const p = previews.value[index];
//    URL.revokeObjectURL(p.downloadUrl);
//    previews.value.splice(index, 1);

//    if (p.source === 'remote') {{
//        // tell store to delete this URL
//        store.deleted{prop.Name}Urls.push(store.{prop.Name.GetCamelCaseName()}Urls[index]);//<-- store.[property] / store.[property]
//        store.{prop.Name.GetCamelCaseName()}Urls.splice(index, 1);//<-- store.[property]
//    }} else {{
//        // a new file: remove from newFiles
//        const nfIndex = newFiles.value.findIndex((f) => f === p.file);
//        if (nfIndex > -1) newFiles.value.splice(nfIndex, 1);
//        onPropChanged(newFiles.value, '{prop.Name.GetCamelCaseName()}');//<-- [property]
//    }}
//}}

//// Cleanup on unmount
//onUnmounted(() => {{
//    for (const p of previews.value) {{
//        URL.revokeObjectURL(p.downloadUrl);
//    }}
//}});

//// Files we can show with <VueFilesPreview>
//const previewableFiles = computed(() => previews.value.filter((p) => !isArchive(p.name)));
//// —————————————

//// Archive files only (zip / rar)
//const archiveFiles = computed(() => previews.value.filter((p) => isArchive(p.name)));
//";
//                    }
//                }
                #endregion

                string colomn = null;
                //if (prop.Type != "FLs")
                //{
                    colomn = GetSingleColomnControl(entityName, prop, enumProps);
                    colomnBuilder.AppendLine(colomn);
                //}
                //else
                //    colomnFLs = GetSingleColomnControl(entityName, prop, enumProps);
            }
            //if (colomnFLs != null)
            //    colomnBuilder.AppendLine(colomnFLs);

            string content = $@"
<script setup>
import {{ use{parentEntityName}{entityName}Store as useStore }} from '@/store/{parentEntityName.GetCamelCaseName()}/{parentEntityName}{entityName}Store';
import {{ {parentEntityName.GetPluralName().GetCapitalName()}_ROUTE as PAGE_ROUTE }} from '@/utils/Constants';
{relationImports}
import useSingle from '@/composables/useSingle';
import useList from '@/composables/useList';
import {{EDIT_ITEM,FIND_ITEM}} from '@/utils/StoreConstant';

const store = useStore();

const {{ state, onPropChanged, onSave, t }} = useSingle(store, PAGE_ROUTE);
{relationConsts}
{enumConsts}

const handleCancel = () => {{
    if (store.id) {{
        store[FIND_ITEM]({{
            id: store.id,
            viewState: EDIT_ITEM
        }});
    }} else {{
        store.$reset();
    }}
}};
</script>
<template>
    <div class=""theCard"">
        <h2 class=""text-3xl font-semibold"">{{{{ $t('title.{parentEntityName}{entityName}') }}}}</h2>

        <div v-if=""!store.finding && !store.{parentEntityName.GetCamelCaseName()}Id"">
            {{{{ $t('message.createEntityFirstMessage', {{ entity: $t('field.{parentEntityName.GetCamelCaseName()}') }}) }}}}
        </div>
        <div v-if=""store.finding"" class=""flex justify-center items-center w-full mt-[32px]"">
            <atom-spinner :size=""50"" color=""#1B80E4"" />
        </div>

        <!-- form -->
        <form v-if=""!store.finding && store.{parentEntityName.GetCamelCaseName()}Id"" @submit.prevent="""" class=""mt-12 w-full"">
            <div class=""grid grid-cols-1 md:grid-cols-2 gap-4 w-full"">                                      
{colomnBuilder}
            </div>
            <!-- actions -->
            <div class=""flex items-center gap-5 mt-12 w-full"">
                <Button severity=""primary"" text :label=""$t('button.cancel')"" class=""rounded float-start"" outlined @click=""handleCancel"" :disabled=""state.saving""></Button>
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
            //update parent single view
            string parentViewPath = Path.Combine(srcDir, "views", $"{parentEntityName.GetCamelCaseName()}", $"{parentEntityName}.vue");
            if (!File.Exists(parentViewPath))
            {
                return;
            }
            string importPartial = $"import {parentEntityName}{entityName} from './parts/{parentEntityName}{entityName}.vue'" +
                $"\n//Add import Partials Here";
            var lines = File.ReadAllLines(parentViewPath).ToList();
            var index = lines.FindIndex(line => line.Contains("//Add import Partials Here"));

            if (index >= 0)
            {
                lines[index] = importPartial;
                File.WriteAllLines(parentViewPath, lines);
            }

            lines.Clear();
            index = -1;

            string tabPartial = $"{{ key: '{entityName.GetCamelCaseName()}', label: t('title.{entityName.GetCamelCaseName()}') }}," +
                $"\n\t//Add tab Partials Here";

            lines = File.ReadAllLines(parentViewPath).ToList();
            index = lines.FindIndex(line => line.Contains("//Add tab Partials Here"));
            if (index >= 0)
            {
                lines[index] = tabPartial;
                File.WriteAllLines(parentViewPath, lines);
            }

            lines.Clear();
            index = -1;

            string componentPartial = $"if (selected.value === '{entityName.GetCamelCaseName()}') return {parentEntityName}{entityName};" +
                $"\n\t//Add component Partials Here";

            lines = File.ReadAllLines(parentViewPath).ToList();
            index = lines.FindIndex(line => line.Contains("//Add component Partials Here"));
            if (index >= 0)
            {
                lines[index] = componentPartial;
                File.WriteAllLines(parentViewPath, lines);
            }
        }

        public static void GeneratePartialBulkView(string entityName, string srcDir, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps, List<string> notGeneratedTableProperties, List<string> hiddenTableProperties, List<Relation> relations, string parentEntityName)
        {
            var entityLower = entityName.GetCamelCaseName();
            string entityPlural = entityName.GetPluralName();
            string capitalEntityPlural = entityPlural.GetCapitalName();
            string entityPluralLower = entityPlural.GetCamelCaseName();
            string viewParentDirectory = Path.Combine(srcDir, "views", $"{parentEntityName.GetCamelCaseName()}");
            Directory.CreateDirectory(viewParentDirectory);
            string viewDirectory = Path.Combine(srcDir, "views", $"{parentEntityName.GetCamelCaseName()}", "parts");
            Directory.CreateDirectory(viewDirectory);

            //string fileSingleName = entityName;
            string viewBulkPath = Path.Combine(viewDirectory, $"{parentEntityName}{entityName.GetPluralName()}.vue");
            StringBuilder enumFilters = new StringBuilder();
            StringBuilder enumDisplayOption = new StringBuilder();
            var initialStateBuilder = new StringBuilder();

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
{string.Join(Environment.NewLine, st)}
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
            List<string> validationList = new List<string>();
            StringBuilder addDialog = new StringBuilder();
            StringBuilder editDialog = new StringBuilder();
            foreach (var prop in properties)
            {
                if (!notGeneratedTableProperties.Any(p => p == prop.Name))
                {
                    initialStateBuilder.AppendLine($"        {prop.Name.GetCamelCaseName()}: {GetDefaultValue(prop.Type)},");
                    var typeWithoutNullable = prop.Type.TrimEnd('?');
                    if (typeWithoutNullable == "string")
                    {
                        var propLower = prop.Name.GetCamelCaseName();
                        //string st = $"    {{field: '{propLower}', order: 0 }},";
                        //filterSectionSortFields.Add(st);
                        filterSectionGlobalFields.Add($"'{propLower}'");
                        filterSectionInitFilters.Add($"    {propLower}: {{ operator: FilterOperator.AND, constraints: [{{ value: null, matchMode: FilterMatchMode.STARTS_WITH }}] }},");
                        
                        addDialog.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""title"">{{{{ $t('field.{propLower}') }}}}</label>
                    <InputText :placeholder=""$t('field.title')"" class=""w-full"" v-model=""newItem.{propLower}"" />
                </div>");

                        editDialog.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""title"">{{{{ $t('field.{propLower}') }}}}</label>
                    <InputText :placeholder=""$t('field.title')"" class=""w-full"" v-model=""selectedItem.{propLower}"" />
                </div>");
                    }
                    if (typeWithoutNullable == "bool")
                    {
                        var propLower = char.ToLower(prop.Name[0]) + prop.Name.Substring(1);
                        //string st = $"    {{field: '{propLower}', order: 0 }},";
                        //filterSectionSortFields.Add(st);
                        filterSectionGlobalFields.Add($"'{propLower}'");
                        filterSectionInitFilters.Add($"    {propLower}: {{ operator: FilterOperator.AND, constraints: [{{ value: null, matchMode: FilterMatchMode.EQUALS }}] }},");
                        
                        addDialog.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <Checkbox v-model=""newItem.{propLower}"" inputId=""{propLower}"" name=""{propLower}"" :binary=""true"" />
                </div>");

                        editDialog.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <Checkbox v-model=""selectedItem.{propLower}"" inputId=""{propLower}"" name=""{propLower}"" :binary=""true"" />
                </div>");

                    }
                    if (typeWithoutNullable == "double" || typeWithoutNullable == "decimal" || typeWithoutNullable == "float")
                    {
                        var propLower = char.ToLower(prop.Name[0]) + prop.Name.Substring(1);
                        //string st = $"    {{field: '{propLower}', order: 0 }},";
                        //filterSectionSortFields.Add(st);
                        filterSectionGlobalFields.Add($"'{propLower}'");
                        filterSectionInitFilters.Add($"    {propLower}: {{ operator: FilterOperator.AND, constraints: [{{ value: null, matchMode: FilterMatchMode.EQUALS }}] }},");
                        
                        addDialog.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <InputNumber :useGrouping=""false"" :maxFractionDigits=""4"" fluid :placeholder=""$t('field.{propLower}')"" class=""w-full"" v-model=""newItem.{propLower}"" />
                </div>");

                        editDialog.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <InputNumber :useGrouping=""false"" :maxFractionDigits=""4"" fluid :placeholder=""$t('field.{propLower}')"" class=""w-full"" v-model=""selectedItem.{propLower}"" />
                </div>");

                    }
                    if (typeWithoutNullable == "int" && enumProps.Any(ep => ep.prop == prop.Name))//enum case
                    {
                        var propLower = char.ToLower(prop.Name[0]) + prop.Name.Substring(1);
                        //string st = $"    {{field: '{propLower}', order: 0 }},";
                        //filterSectionSortFields.Add(st);
                        filterSectionGlobalFields.Add($"'{propLower}'");
                        filterSectionInitFilters.Add($"    {propLower}: {{ operator: FilterOperator.AND, constraints: [{{ value: null, matchMode: FilterMatchMode.EQUALS }}] }},");

                        addDialog.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <Select :options=""{entityName.GetCamelCaseName()}{prop.Name}Options"" optionLabel=""label"" filter optionValue=""value"" v-model=""newItem.{propLower}"" :placeholder=""$t('field.select{prop.Name}')"" class=""w-full"">
                        <template #option=""slotProps"">
                            <div class=""flex items-center"">
                                <div>{{{{ slotProps.option.label }}}}</div>
                            </div>
                        </template>
                    </Select>
                </div>");

                        editDialog.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <Select :options=""{entityName.GetCamelCaseName()}{prop.Name}Options"" optionLabel=""label"" filter optionValue=""value"" v-model=""selectedItem.{propLower}"" :placeholder=""$t('field.select{prop.Name}')"" class=""w-full"">
                        <template #option=""slotProps"">
                            <div class=""flex items-center"">
                                <div>{{{{ slotProps.option.label }}}}</div>
                            </div>
                        </template>
                    </Select>
                </div>");
                    }
                    if (typeWithoutNullable == "int" && !enumProps.Any(ep => ep.prop == prop.Name))//int property case
                    {
                        var propLower = char.ToLower(prop.Name[0]) + prop.Name.Substring(1);
                        //string st = $"    {{field: '{propLower}', order: 0 }},";
                        //filterSectionSortFields.Add(st);
                        filterSectionGlobalFields.Add($"'{propLower}'");
                        filterSectionInitFilters.Add($"    {propLower}: {{ operator: FilterOperator.AND, constraints: [{{ value: null, matchMode: FilterMatchMode.EQUALS }}] }},");

                        addDialog.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <InputNumber :useGrouping=""false"" fluid :placeholder=""$t('field.{propLower}')"" class=""w-full"" v-model=""newItem.{propLower}"" />
                </div>");

                        editDialog.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <InputNumber :useGrouping=""false"" fluid :placeholder=""$t('field.{propLower}')"" class=""w-full"" v-model=""selectedItem.{propLower}"" />
                </div>");
                    }
                    if (typeWithoutNullable.Contains("Date") || typeWithoutNullable.Contains("Time"))
                    {
                        var propLower = char.ToLower(prop.Name[0]) + prop.Name.Substring(1);
                        //string st = $"    {{field: '{propLower}', order: 0 }},";
                        //filterSectionSortFields.Add(st);
                        filterSectionGlobalFields.Add($"'{propLower}'");
                        filterSectionInitFilters.Add($"    {propLower}: {{ operator: null, constraints: [{{value: null, matchMode: FilterMatchMode.DATE_IS }}] }},");

                        addDialog.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <DatePicker v-model=""newItem.{propLower}"" showIcon fluid iconDisplay=""input"" dateFormat=""dd/mm/yy"" showButtonBar :placeholder=""$t('field.{propLower}')"" class=""!w-full"" />
                </div>");

                        editDialog.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <DatePicker v-model=""selectedItem.{propLower}"" showIcon fluid iconDisplay=""input"" dateFormat=""dd/mm/yy"" showButtonBar :placeholder=""$t('field.{propLower}')"" class=""!w-full"" />
                </div>");
                    }

                    if (prop.Validation != null && prop.Validation.Required)
                    {
                        if (prop.Type == "string" || prop.Type.Contains("Date") || prop.Type.Contains("Time"))
                        {
                            if (validationList.Any())
                                validationList.Add($"|| !newItem.value.{prop.Name.GetCamelCaseName()}");
                            else
                                validationList.Add($"!newItem.value.{prop.Name.GetCamelCaseName()}");
                        }
                        else
                        {
                            if (validationList.Any())
                                validationList.Add($"|| newItem.value.{prop.Name.GetCamelCaseName()} === null");
                            else
                                validationList.Add($"!newItem.value.{prop.Name.GetCamelCaseName()} === null");
                        }
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
            foreach (var rel in relations)
            {
                if (rel.IsGeneratedInTable)
                {

                    if (rel.Type == RelationType.OneToOneSelfJoin || rel.Type == RelationType.ManyToOne || rel.Type == RelationType.ManyToOneNullable
                      || rel.Type == RelationType.OneToOne || rel.Type == RelationType.OneToOneNullable)
                    {
                        var propLower = rel.Type != RelationType.OneToOneSelfJoin ? rel.RelatedEntity.GetCamelCaseName() + rel.DisplayedProperty
                            : rel.RelatedEntity.GetCamelCaseName() + $"Parent{rel.DisplayedProperty}";
                        //string st = $"    {{field: '{propLower}', order: 0 }},";
                        //filterSectionSortFields.Add(st);
                        filterSectionGlobalFields.Add($"'{propLower}'");
                        filterSectionInitFilters.Add($"    {propLower}: {{ operator: FilterOperator.AND, constraints: [{{ value: null, matchMode: FilterMatchMode.EQUALS }}] }},");
                        string entityRelatedPlural = rel.RelatedEntity.GetPluralName();
                        string entityRelatedPluralLower = entityRelatedPlural.GetCamelCaseName();
                        relationImports.AppendLine($"import {{ use{rel.RelatedEntity}Store }} from '@/store/{rel.RelatedEntity}Store';");
                        relationConsts.AppendLine($"const {{ items: {entityRelatedPluralLower}, loading: loading{entityRelatedPlural}, search: search{entityRelatedPlural} }} = useList(use{rel.RelatedEntity}Store(), '', {{}});");

                        addDialog.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <Select
                        :emptyMessage=""$t('message.noAvailableOptions')""
                        :options=""{entityRelatedPluralLower}""
                        optionLabel=""{rel.DisplayedProperty.GetCamelCaseName()}""
                        optionValue=""id""
                        v-model=""newItem.{propLower}""
                        :loading=""loading{entityRelatedPlural}""
                        :placeholder=""$t('field.{propLower}')""
                        class=""w-full""
                        filter
                        @filter=""(e) => search{entityRelatedPlural}(e.value)""
                    >
                        <template #option=""slotProps"">
                            <div class=""flex items-center"">
                                <div>{{{{ slotProps.option.{rel.DisplayedProperty.GetCamelCaseName()} }}}}</div>
                            </div>
                        </template>
                    </Select>
                </div>");

                        editDialog.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <Select
                        :emptyMessage=""$t('message.noAvailableOptions')""
                        :options=""{entityRelatedPluralLower}""
                        optionLabel=""{rel.DisplayedProperty.GetCamelCaseName()}""
                        optionValue=""id""
                        v-model=""selectedItem.{propLower}""
                        :loading=""loading{entityRelatedPlural}""
                        :placeholder=""$t('field.{propLower}')""
                        class=""w-full""
                        filter
                        @filter=""(e) => search{entityRelatedPlural}(e.value)""
                    >
                        <template #option=""slotProps"">
                            <div class=""flex items-center"">
                                <div>{{{{ slotProps.option.{rel.DisplayedProperty.GetCamelCaseName()} }}}}</div>
                            </div>
                        </template>
                    </Select>
                </div>");
                    }
                    if (rel.Type == RelationType.ManyToMany)
                    {
                        string displayedPropPlural = rel.DisplayedProperty.GetPluralName();
                        var propLower = rel.RelatedEntity.GetCamelCaseName().GetPluralName() + rel.DisplayedProperty.GetPluralName();
                        filterSectionGlobalFields.Add($"'{propLower}'");
                        filterSectionInitFilters.Add($"    {propLower}: {{ operator: FilterOperator.AND, constraints: [{{ value: null, matchMode: 'arrayIncludes' }}] }},");
                        string entityRelatedPlural = rel.RelatedEntity.GetPluralName();
                        string entityRelatedPluralLower = entityRelatedPlural.GetCamelCaseName();
                        relationImports.AppendLine($"import {{ use{rel.RelatedEntity}Store }} from '@/store/{rel.RelatedEntity}Store';");
                        relationConsts.AppendLine($"const {{ items: {entityRelatedPluralLower}, loading: loading{entityRelatedPlural}, search: search{entityRelatedPlural} }} = useList(use{rel.RelatedEntity}Store(), '', {{}});");
                        
                        addDialog.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <MultiSelect
                        id=""{propLower}""
                        class=""w-full""
                        v-model=""newItem.{propLower}""
                        :options=""{entityRelatedPluralLower}""
                        optionLabel=""{rel.DisplayedProperty.GetCamelCaseName()}""
                        optionValue=""id""
                        :loading=""loading{entityRelatedPlural}""
                        type=""text""
                        dataKey=""id""
                        filter
                        @filter=""(e) => search{entityRelatedPlural}(e.value)""
                        :placeholder=""$t('field.{propLower}')""
                        :maxSelectedLabels=""3""
                    />
                </div>");

                        editDialog.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <MultiSelect
                        id=""{propLower}""
                        class=""w-full""
                        v-model=""selectedItem.{propLower}""
                        :options=""{entityRelatedPluralLower}""
                        optionLabel=""{rel.DisplayedProperty.GetCamelCaseName()}""
                        optionValue=""id""
                        :loading=""loading{entityRelatedPlural}""
                        type=""text""
                        dataKey=""id""
                        filter
                        @filter=""(e) => search{entityRelatedPlural}(e.value)""
                        :placeholder=""$t('field.{propLower}')""
                        :maxSelectedLabels=""3""
                    />
                </div>");
                    }
                    if (rel.Type == RelationType.UserSingle || rel.Type == RelationType.UserSingleNullable)
                    {
                        var propLower = rel.DisplayedProperty.GetCamelCaseName() + "Name";
                        filterSectionGlobalFields.Add($"'{propLower}'");
                        filterSectionInitFilters.Add($"    {propLower}: {{ operator: FilterOperator.AND, constraints: [{{ value: null, matchMode: FilterMatchMode.EQUALS }}] }},");
                        
                        addDialog.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <Select
                        :emptyMessage=""$t('message.noAvailableOptions')""
                        :options=""users""
                        optionLabel=""fullName""
                        optionValue=""id""
                        v-model=""newItem.{propLower}""
                        :loading=""loadingUsers""
                        :placeholder=""$t('field.{propLower}')""
                        class=""w-full""
                        filter
                        @filter=""(e) => searchUsers(e.value)""
                    >
                        <template #option=""slotProps"">
                            <div class=""flex items-center"">
                                <div>{{{{ slotProps.option.fullName }}}}</div>
                            </div>
                        </template>
                    </Select>
                </div>");

                        editDialog.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <Select
                        :emptyMessage=""$t('message.noAvailableOptions')""
                        :options=""users""
                        optionLabel=""fullName""
                        optionValue=""id""
                        v-model=""selectedItem.{propLower}""
                        :loading=""loadingUsers""
                        :placeholder=""$t('field.{propLower}')""
                        class=""w-full""
                        filter
                        @filter=""(e) => searchUsers(e.value)""
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
                        var propLower = rel.DisplayedProperty.GetCamelCaseName().GetPluralName() + "Names";
                        filterSectionGlobalFields.Add($"'{propLower}'");
                        filterSectionInitFilters.Add($"    {propLower}: {{ operator: FilterOperator.AND, constraints: [{{ value: null, matchMode: 'arrayIncludes' }}] }},");

                        addDialog.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <MultiSelect
                        id=""{propLower}""
                        class=""w-full""
                        v-model=""newItem.{propLower}""
                        :options=""users""
                        optionLabel=""fullName""
                        optionValue=""id""
                        :loading=""loadingUsers""
                        type=""text""
                        dataKey=""id""
                        filter
                        @filter=""(e) => searchUsers(e.value)""
                        :placeholder=""$t('field.{propLower}')""
                        :maxSelectedLabels=""3""
                    />
                </div>");

                        editDialog.AppendLine($@"
                <div class=""flex flex-col gap-2 w-full"">
                    <label for=""{propLower}"">{{{{ $t('field.{propLower}') }}}}</label>
                    <MultiSelect
                        id=""{propLower}""
                        class=""w-full""
                        v-model=""selectedItem.{propLower}""
                        :options=""users""
                        optionLabel=""fullName""
                        optionValue=""id""
                        :loading=""loadingUsers""
                        type=""text""
                        dataKey=""id""
                        filter
                        @filter=""(e) => searchUsers(e.value)""
                        :placeholder=""$t('field.{propLower}')""
                        :maxSelectedLabels=""3""
                    />
                </div>");
                    }

                    if (rel.Type == RelationType.ManyToOne || rel.Type == RelationType.OneToOne)
                    {
                        string camelCasePropName = rel.RelatedEntity.GetCamelCaseName();
                        if (validationList.Any())
                            validationList.Add($"|| newItem.value.{camelCasePropName}Id === null");
                        else
                            validationList.Add($"newItem.value.{camelCasePropName}Id === null");
                    }

                    if (rel.Type == RelationType.UserSingle)
                    {
                        string camelCasePropName = rel.DisplayedProperty.GetCamelCaseName();
                        if (validationList.Any())
                            validationList.Add($"|| newItem.value.{camelCasePropName}Id === null");
                        else
                            validationList.Add($"newItem.value.{camelCasePropName}Id === null");
                    }
                }
                //fill initialStateBuilder
                if (rel.Type == RelationType.ManyToOne || rel.Type == RelationType.ManyToOneNullable
                      || rel.Type == RelationType.OneToOne || rel.Type == RelationType.OneToOneNullable)
                {
                    string camelCasePropName = char.ToLower(rel.RelatedEntity[0]) + rel.RelatedEntity.Substring(1);
                    initialStateBuilder.AppendLine($"        {camelCasePropName}Id: null,");
                }
                if (rel.Type == RelationType.OneToOneSelfJoin)
                {
                    string camelCasePropName = char.ToLower(rel.RelatedEntity[0]) + rel.RelatedEntity.Substring(1);
                    initialStateBuilder.AppendLine($"        {camelCasePropName}ParentId: null,");
                }
                if (rel.Type == RelationType.ManyToMany)
                {
                    string camelCasePropNameIds = rel.RelatedEntity.GetCamelCaseName().GetPluralName() + "Ids";
                    initialStateBuilder.AppendLine($"        {camelCasePropNameIds}: [],");
                }
                if (rel.Type == RelationType.UserSingle || rel.Type == RelationType.UserSingleNullable)
                {
                    string camelCasePropName = rel.DisplayedProperty.GetCamelCaseName();
                    initialStateBuilder.AppendLine($"        {camelCasePropName}Id: null,");
                }
                if (rel.Type == RelationType.UserMany)
                {
                    string camelCasePropNameIds = (rel.DisplayedProperty.GetCamelCaseName()).GetPluralName() + "Ids";
                    initialStateBuilder.AppendLine($"        {camelCasePropNameIds}: [],");
                }

            }

            StringBuilder colomnBuilder = new StringBuilder();
            foreach (var prop in properties)
            {
                if (!notGeneratedTableProperties.Any(p => p == prop.Name))
                {
                    string colomn = GetBulkTableColomnControl(entityName, prop, enumProps);
                    colomnBuilder.AppendLine(colomn);
                }

            }
            string? relationColomn = GetBulkTableColomnRelationControl(entityName, relations);
            if (relationColomn != null)
                colomnBuilder.AppendLine(relationColomn);

            string content = $@"
<script setup>
import LocalListTemplate from '@/components/table/LocalListTemplate.vue';
import {{ use{parentEntityName}{entityName}Store as useStore }} from '@/store/{parentEntityName.GetCamelCaseName()}/{parentEntityName}{entityName}Store';
import {{ {parentEntityName.GetPluralName().GetCapitalName()}_ROUTE as PAGE_ROUTE }} from '@/utils/Constants';
import {{ ref }} from 'vue';
import {{ FilterMatchMode, FilterService, FilterOperator }} from '@primevue/core';
import {{ EDIT_ITEM, FIND_ITEM }} from '@/utils/StoreConstant';
import useSingle from '@/composables/useSingle';
import useList from '@/composables/useList';
{relationImports}


const store = useStore();

const {{ state, t, onSave }} = useSingle(store, PAGE_ROUTE);
const {{ isColumnSelected, formattedDate, getOptionLabel }} = useList(store, PAGE_ROUTE, {{ autoLoad: false }});
{enumFilters.ToString().TrimEnd()}
{enumDisplayOption.ToString().TrimEnd()}

const globalFields = ref([{string.Join(",", filterSectionGlobalFields)}]);
const filters = ref();

// multi-select filter (local only)
FilterService.register('arrayIncludes', (value, filter) => {{
    return Array.isArray(value) && Array.isArray(filter) ? filter.some((f) => value.includes(f)) : false;
}});


const initFilters = () => {{
    filters.value = {{
        global: {{ value: null, matchMode: FilterMatchMode.CONTAINS }},
{string.Join(Environment.NewLine, filterSectionInitFilters)}
    }};
}};
{relationConsts}
initFilters();

// —- ADD LOGIC —-
const isAddModalOpen = ref(false);
const tempCounter = ref(1);

const newItem = ref({{
    tempId: null,
{initialStateBuilder.ToString().TrimEnd()}
}});

const onAdd = () => {{
    newItem.value = {{
        tempId: tempCounter.value++,
{initialStateBuilder.ToString().TrimEnd()}
    }};
    isAddModalOpen.value = true;
}};

const onAddSave = () => {{
    // validation 
    if ({string.Join(" ", validationList)}) {{
        store.sendErrorMessage(t('message.allFieldsAreRequired'));
        return;
    }}

    store.{parentEntityName.GetCamelCaseName()}{entityName.GetPluralName()}.push({{ ...newItem.value }});
    isAddModalOpen.value = false;
}};

const onAddCancel = () => {{
    isAddModalOpen.value = false;
}};

function findIndex(item) {{
    return store.{parentEntityName.GetCamelCaseName()}{entityName.GetPluralName()}.findIndex((x) => (item.id && x.id === item.id) || (item.tempId && x.tempId === item.tempId));
}}

// -- EDIT LOGIC --
const selectedItem = ref(null);
const originalItem = ref(null); // store a backup copy
const isEditModalOpen = ref(false);

const onEdit = (item) => {{
    // make a deep copy so we can revert later
    originalItem.value = JSON.parse(JSON.stringify(item));
    // work on a fresh object, not the one in the array
    selectedItem.value = {{ ...item }};
    isEditModalOpen.value = true;
}};

const onEditSave = () => {{
    const idx = findIndex(selectedItem.value);
    if (idx !== -1) {{
        // replace the array item with the edited version
        store.{parentEntityName.GetCamelCaseName()}{entityName.GetPluralName()}.splice(idx, 1, selectedItem.value);
    }}
    isEditModalOpen.value = false;
}};

const onEditCancel = () => {{
    // restore original into the array
    const idx = store.{parentEntityName.GetCamelCaseName()}{entityName.GetPluralName()}.findIndex((x) => x.id === originalItem.value.id);
    if (idx !== -1) {{
        store.{parentEntityName.GetCamelCaseName()}{entityName.GetPluralName()}.splice(idx, 1, originalItem.value);
    }}
    isEditModalOpen.value = false;
}};

// -- DELETE LOGIC --
const isDeleteModalOpen = ref(false);
const itemToDelete = ref(null);

const onDelete = (item) => {{
    itemToDelete.value = item;
    isDeleteModalOpen.value = true;
}};

const onDeleteConfirm = () => {{
    const idx = findIndex(itemToDelete.value);
    console.log('idx', idx);
    if (idx !== -1) {{
        store.{parentEntityName.GetCamelCaseName()}{entityName.GetPluralName()}.splice(idx, 1);
    }}
    isDeleteModalOpen.value = false;
}};

const onDeleteCancel = () => {{
    isDeleteModalOpen.value = false;
}};

const saveAll = () => {{
    // Make a deep copy backup of the original array
    const backup = JSON.parse(JSON.stringify(store.{parentEntityName.GetCamelCaseName()}{entityName.GetPluralName()}));

    // Existing items: change the id → rename it to [partialId]
    const existing = store.{parentEntityName.GetCamelCaseName()}{entityName.GetPluralName()}
        .filter((item) => !!item.id)
        .map(({{ id, tempId, ...rest }}) => ({{
            {entityLower}Id: id,
            ...rest
        }}));

    // New items: only tempId → strip tempId
    const newOnes = store.{parentEntityName.GetCamelCaseName()}{entityName.GetPluralName()}
        .filter((item) => !item.id && item.tempId)
        .map(({{ tempId, ...rest }}) => ({{
            ...rest
        }}));

    // Merge back: existing first, then new
    const merged = [...existing, ...newOnes];

    // Reassign into store
    store.{parentEntityName.GetCamelCaseName()}{entityName.GetPluralName()} = merged;

    // save
    onSave();

    // --- RESTORE the original array! ---
    store.{parentEntityName.GetCamelCaseName()}{entityName.GetPluralName()} = backup;
}};


const handleCancel = () => {{
    store[FIND_ITEM]({{
        id: store.{parentEntityName.GetCamelCaseName()}Id,
        viewState: EDIT_ITEM
    }});
}};
</script>
<template>
    <div class=""theCard"">
        <div class=""flex flex-wrap items-center justify-between gap-4 mb-8"">
            <h2 class=""text-3xl font-semibold"">{{{{ $t('title.{parentEntityName.GetCamelCaseName()}{entityName.GetPluralName()}') }}}}</h2>
            <!-- actions -->
            <div v-if=""!store.finding && store.{parentEntityName.GetCamelCaseName()}Id"" class="""">
                <Button text :label=""$t('button.cancelAll')"" class=""mr-2"" @click=""handleCancel"" />
                <Button :label=""$t('button.saveAll')"" class=""mr-2"" @click=""saveAll"" />
            </div>
        </div>
        <div v-if=""!store.finding && !store.{parentEntityName.GetCamelCaseName()}Id"">
            {{{{ $t('message.createEntityFirstMessage', {{ entity: $t('field.{parentEntityName.GetCamelCaseName()}') }}) }}}}
        </div>
        <div v-if=""store.finding"" class=""flex justify-center items-center w-full mt-[32px]"">
            <atom-spinner :size=""50"" color=""#1B80E4"" />
        </div>
        <LocalListTemplate v-if=""!store.finding && store.{parentEntityName.GetCamelCaseName()}Id"" :items=""store.{parentEntityName.GetCamelCaseName()}{entityPlural}"" :use-store=""useStore"" :filters=""filters"" @add=""onAdd"" :global-filter-fields=""globalFields"">
            <template #columns>
{colomnBuilder}

                <Column field=""actions"" :header=""$t('field.actions')"">
                    <template #body=""{{ data }}"">
                        <div class=""flex flex-row gap-2 items-center"">
                            <Button v-tooltip=""$t('tooltip.edit')"" text :severity=""$AppColor('ICON_EDIT_COLOR')"" class=""p-button-rounded mr-2 mb-2"" @click=""onEdit(data)""><i class=""fa-solid fa-pen""></i></Button>
                            <Button v-tooltip=""$t('tooltip.delete')"" text :severity=""$AppColor('ICON_DELETE_COLOR')"" class=""p-button-rounded mr-2 mb-2"" @click=""onDelete(data)""><i class=""fa-solid fa-trash""></i></Button>
                        </div>
                    </template>
                </Column>
            </template>
        </LocalListTemplate>

        <!-- -- Dialogs -- -->
        <!-- ADD DIALOG -->
        <Dialog v-model:visible=""isAddModalOpen"" :header=""t('title.addClientContentPerson')"" modal dismissableMask :draggable=""false"" block-scroll class=""!w-[90%] !max-w-[600px]"">
            <div class=""grid grid-cols-1 md:grid-cols-2 gap-4"">
{addDialog}
            </div>
            <template #footer>
                <Button :label=""$t('button.cancel')"" text @click=""onAddCancel"" />
                <Button :label=""$t('button.save')"" @click=""onAddSave"" />
            </template>
        </Dialog>

        <!-- EDIT DIALOG -->
        <Dialog v-model:visible=""isEditModalOpen"" dismissableMask modal :header=""$t('title.editClientContentPerson')"" :modal=""true"" :draggable=""false"" block-scroll class=""!w-[90%] !max-w-[600px]"">
            <!-- your form fields, bound to selectedItem.fullName, etc. -->
            <div class=""w-full grid grid-cols-1 md:grid-cols-2 gap-4"">
{editDialog}
            </div>
            <template #footer>
                <Button :label=""$t('button.cancel')"" text @click=""onEditCancel"" />
                <Button :label=""$t('button.save')"" @click=""onEditSave"" />
            </template>
        </Dialog>

        <!-- DELETE CONFIRM DIALOG -->
        <Dialog v-model:visible=""isDeleteModalOpen"" :header=""$t('message.confirmDelete')"" :modal=""true"" :draggable=""false"" :dismissableMask=""true"" block-scroll>
            <p>
                {{{{ $t('message.deleteMessage', {{ title: itemToDelete.id }}) }}}}<!-- اسم البربتي التي تعبر عن الانتتي المحذوف -->
            </p>
            <template #footer>
                <Button :label=""$t('button.no')"" text @click=""onDeleteCancel"" />
                <Button :label=""$t('button.yes')"" severity=""danger"" @click=""onDeleteConfirm"" />
            </template>
        </Dialog>
    </div>
</template>
";

            File.WriteAllText(viewBulkPath, content);

            //update parent single view
            string parentViewPath = Path.Combine(srcDir, "views", $"{parentEntityName.GetCamelCaseName()}", $"{parentEntityName}.vue");
            if (!File.Exists(parentViewPath))
            {
                return;
            }
            string importPartial = $"import {parentEntityName}{entityName.GetPluralName()} from './parts/{parentEntityName}{entityName.GetPluralName()}.vue'" +
                $"\n//Add import Partials Here";
            var lines = File.ReadAllLines(parentViewPath).ToList();
            var index = lines.FindIndex(line => line.Contains("//Add import Partials Here"));

            if (index >= 0)
            {
                lines[index] = importPartial;
                File.WriteAllLines(parentViewPath, lines);
            }

            lines.Clear();
            index = -1;

            string tabPartial = $"{{ key: '{entityName.GetCamelCaseName().GetPluralName()}', label: t('title.{entityName.GetCamelCaseName().GetPluralName()}') }}," +
                $"\n\t//Add tab Partials Here";

            lines = File.ReadAllLines(parentViewPath).ToList();
            index = lines.FindIndex(line => line.Contains("//Add tab Partials Here"));
            if (index >= 0)
            {
                lines[index] = tabPartial;
                File.WriteAllLines(parentViewPath, lines);
            }

            lines.Clear();
            index = -1;

            string componentPartial = $"if (selected.value === '{entityName.GetCamelCaseName().GetPluralName()}') return {parentEntityName}{entityName.GetPluralName()};" +
                $"\n\t//Add component Partials Here";

            lines = File.ReadAllLines(parentViewPath).ToList();
            index = lines.FindIndex(line => line.Contains("//Add component Partials Here"));
            if (index >= 0)
            {
                lines[index] = componentPartial;
                File.WriteAllLines(parentViewPath, lines);
            }
        }

        public static void GenerateParentSingleView(string entityName, string srcDir)
        {
            var entityLower = entityName.GetCamelCaseName();
            string viewDirectory = Path.Combine(srcDir, "views", $"{entityLower}");
            Directory.CreateDirectory(viewDirectory);
            string viewSinglePath = Path.Combine(viewDirectory, $"{entityName}.vue");

            string content = $@"
<script setup>
import {{ ref, computed }} from 'vue'
import {entityName}BasicInfo from './parts/{entityName}BasicInfo.vue'
//Add import Partials Here
import {{ useI18n }} from 'vue-i18n'

// back button
import {{ {entityName.GetPluralName().GetCapitalName()}_ROUTE as PAGE_ROUTE }} from '@/utils/Constants'
import {{ getLocale }} from '@/utils/Storage'
import {{ useRouter }} from 'vue-router'

const router = useRouter()

const goBack = () => {{
    router.push(PAGE_ROUTE())
}}
//

const {{ t }} = useI18n()
const selected = ref('basicInfo')

const tabs = [
    {{ key: 'basicInfo', label: t('title.{entityName.GetCamelCaseName()}') }},
    //Add tab Partials Here
]


// return the right component
const currentComponent = computed(() => {{
    //add components here
    //Add component Partials Here

    // set default component here ->
    return {entityName}BasicInfo
}});

</script>


<template>
    <div class=""flex relative flex-col gap-5"">
    <!-- back button -->
        <div class=""flex items-center w-fit gap-5"">
            <button @click=""goBack"" class=""flex items-center gap-3 px-3 py-2 w-full transition-colors hover:bg-gray-200 rounded-lg"" >
            <i class=""pi pi-arrow-left text-gray-600"" :class=""getLocale() === 'ar' ? 'rotate-180' : ''""></i>
            {{{{ $t('button.back') }}}}
            </button>
        </div>

    <!-- Sidebar (for sticky: md:sticky md:top-20) -->
        <aside class=""w-fit shrink-0 bg-white rounded-lg h-fit border p-2 overflow-y-auto"">
            <div class=""flex flex-wrap gap-2"">

            <button
                v-for=""tab in tabs""
                :key=""tab.key""
                @click=""selected = tab.key""
                class=""cursor-pointer px-3 py-2 rounded border border-[#EAEAEA] transition-colors""
                :class=""selected === tab.key ? 'bg-blue-500 border-blue-500 text-white font-medium' : 'hover:bg-gray-200'""
            >
                {{{{ tab.label }}}}
            </button>
            </div>
        </aside>



    <!-- Main Content -->
        <main class=""flex-1"">
            <component :is=""currentComponent"" />
        </main>
    </div>
</template>


<style scoped>

</style>";
            
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
                        filter
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
                    <div v-if=""singlePreviewUrl && !isArchive(singleFile.name)"" class=""bg-slate-100 flex-wrap p-2 px-3 border border-[#EAEAEA] rounded-lg flex gap-4 justify-between items-center"">
                        <!-- File preview -->

                        <div class=""flex items-center gap-4"">
                            <div class=""flex items-center justify-center h-[40px] bg-white border shrink-0 rounded-full w-[40px] gap-2"">
                                <i class=""fa-solid fa-file text-lg""></i>
                            </div>
                            <p
                                v-tooltip.top=""{{
                                    value: singleFile.name,
                                    pt: {{
                                        root: {{
                                            style: {{
                                                maxWidth: '350px',
                                                whiteSpace: 'normal',
                                                wordBreak: 'break-word'
                                            }}
                                        }}
                                    }}
                                }}""
                                class=""flex items-center flex-wrap gap-1 gap-y-0.5""
                            >
                                {{{{ shortenFileName(singleFile.name) }}}} <span class="""">({{{{ (singleFile.size / 1024).toFixed(1) }}}} KB)</span>
                            </p>
                        </div>
                        <div class=""flex gap-2"">
                            <!-- preview button -->
                            <button @click=""openPreviewModal(singleFile)"" class=""px-3 py-2 bg-blue-500 text-white rounded text-sm"">
                                <i class=""fa-solid fa-eye""></i>
                            </button>
                            <a :href=""singlePreviewUrl"" :download=""singleFile.name"" class=""px-3 py-2 bg-green-500 text-white rounded text-sm"">
                                <i class=""fa-solid fa-download""></i>
                            </a>
                            <button @click=""removeFile"" class=""px-3 py-2 bg-red-500 text-white rounded text-sm"">
                                <i class=""fa-solid fa-trash""></i>
                            </button>
                        </div>
                    </div>
                    <!-- single archive -->
                    <div v-if=""singlePreviewUrl && isArchive(singleFile.name)"" class="""">
                        <label class=""block mb-3"">{{{{ $t('title.{prop.Name.GetCamelCaseName()}') }}}}</label>
                        <div class=""bg-slate-100 flex-wrap p-2 px-3 border border-[#EAEAEA] rounded-lg flex gap-4 justify-between items-center"">
                            <div class=""flex items-center gap-4"">
                                <div class=""flex items-center justify-center h-[40px] bg-white shrink-0 border rounded-full w-[40px] gap-2"">
                                    <i class=""fa-solid fa-file-zipper text-lg""></i>
                                </div>
                                <p
                                    v-tooltip.top=""{{
                                        value: singleFile.name,
                                        pt: {{
                                            root: {{
                                                style: {{
                                                    maxWidth: '350px',
                                                    whiteSpace: 'normal',
                                                    wordBreak: 'break-word'
                                                }}
                                            }}
                                        }}
                                    }}""
                                >
                                    {{{{ singleFile.name }}}} ({{{{ (singleFile.size / 1024).toFixed(1) }}}} KB)
                                </p>
                            </div>
                            <div class=""flex gap-2"">
                                <a :href=""singlePreviewUrl"" :download=""singleFile.name"" class=""px-3 py-2 bg-green-500 text-white rounded text-sm"">
                                    <i class=""fa-solid fa-download""></i>
                                </a>
                                <button @click=""removeFile"" class=""px-3 py-2 bg-red-500 text-white rounded text-sm"">
                                    <i class=""fa-solid fa-trash""></i>
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
                <div v-if=""previewableFiles.length"" class=""md:col-span-2 grid grid-cols-1 lg:grid-cols-2 gap-6"">
                    <!-- <VueFilesPreview :file=""p.file"" height=""200px"" overflow=""auto"" /> -->
                    <ul class=""space-y-2"">
                        <li v-for=""(p, i) in previewableFiles"" :key=""i"" class=""bg-slate-100 flex-wrap p-2 px-3 border border-[#EAEAEA] rounded-lg flex gap-4 justify-between items-center"">
                            <div class=""flex items-center gap-4"">
                                <div class=""flex items-center justify-center h-[40px] bg-white border shrink-0 rounded-full w-[40px] gap-2"">
                                    <i class=""fa-solid fa-file text-lg""></i>
                                </div>
                                <p
                                    v-tooltip.top=""{{
                                        value: p.name,
                                        pt: {{
                                            root: {{
                                                style: {{
                                                    maxWidth: '350px',
                                                    whiteSpace: 'normal',
                                                    wordBreak: 'break-word'
                                                }}
                                            }}
                                        }}
                                    }}""
                                    class=""flex items-center flex-wrap gap-1 gap-y-0.5""
                                >
                                    {{{{ shortenFileName(p.name) }}}} <span class="""">({{{{ (p.size / 1024).toFixed(1) }}}} KB)</span>
                                </p>
                            </div>
                            <div class=""flex gap-2"">
                                <!-- preview button -->
                                <button @click=""openPreviewModal(p.file)"" class=""px-3 py-2 bg-blue-500 text-white rounded text-sm"">
                                    <i class=""fa-solid fa-eye""></i>
                                </button>
                                <a :href=""p.downloadUrl"" :download=""p.name"" class=""px-3 py-2 bg-green-500 text-white rounded text-sm"">
                                    <i class=""fa-solid fa-download""></i>
                                </a>
                                <button @click=""removePreview(previews.indexOf(p))"" class=""px-3 py-2 bg-red-500 text-white rounded text-sm"">
                                    <i class=""fa-solid fa-trash""></i>
                                </button>
                            </div>
                        </li>
                    </ul>
                </div>

                <!-- Archived files list -->
                <div v-if=""archiveFiles.length"" class=""mt-8"">
                    <label class=""block mb-3"">{{{{ $t('title.{prop.Name.GetCamelCaseName()}') }}}}</label>
                    <ul class=""space-y-2"">
                        <li v-for=""(p, i) in archiveFiles"" :key=""`arch-${{i}}`"" class=""bg-slate-100 flex-wrap p-2 px-3 border border-[#EAEAEA] rounded-lg flex gap-4 justify-between items-center"">
                            <div class=""flex items-center gap-4"">
                                <div class=""flex items-center justify-center h-[40px] bg-white shrink-0 border rounded-full w-[40px] gap-2"">
                                    <i class=""fa-solid fa-file-zipper text-lg""></i>
                                </div>
                                <p
                                    v-tooltip.top=""{{
                                        value: p.name,
                                        pt: {{
                                            root: {{
                                                style: {{
                                                    maxWidth: '350px',
                                                    whiteSpace: 'normal',
                                                    wordBreak: 'break-word'
                                                }}
                                            }}
                                        }}
                                    }}""
                                >
                                    {{{{ shortenFileName(p.name) }}}} ({{{{ (p.size / 1024).toFixed(1) }}}} KB)
                                </p>
                            </div>
                            <div class=""flex gap-2"">
                                <a :href=""p.downloadUrl"" :download=""p.name"" class=""px-3 py-2 bg-green-500 text-white rounded text-sm"">
                                    <i class=""fa-solid fa-download""></i>
                                </a>
                                <button @click=""removePreview(previews.indexOf(p))"" class=""px-3 py-2 bg-red-500 text-white rounded text-sm"">
                                    <i class=""fa-solid fa-trash""></i>
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
