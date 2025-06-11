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

        public static void GenerateStoreFile(string entityName, SharedClasses.Properties properties,List<Relation> relations,string srcDir)
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
            foreach (var prop in properties.PropertiesList)
            {
                string camelCasePropName = char.ToLower(prop.Name[0]) + prop.Name.Substring(1);
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
                    initialStateBuilder.AppendLine($"        {camelCasePropName}Id: null,");
                    constItemBuilder.AppendLine($"                {camelCasePropName}Id: this.{camelCasePropName}Id,");
                }
                if (rel.Type == RelationType.OneToOneSelfJoin)
                {
                    string camelCasePropName = char.ToLower(rel.RelatedEntity[0]) + rel.RelatedEntity.Substring(1);
                    initialStateBuilder.AppendLine($"        {camelCasePropName}ParentId: null,");
                    constItemBuilder.AppendLine($"                {camelCasePropName}ParentId: this.{camelCasePropName}ParentId,");
                }
                if (rel.Type == RelationType.ManyToMany)
                {
                    string camelCasePropName = char.ToLower(rel.RelatedEntity[0]) + rel.RelatedEntity.Substring(1) + "Ids";
                    initialStateBuilder.AppendLine($"        {camelCasePropName}: [],");
                    constItemBuilder.AppendLine($"                {camelCasePropName}: this.{camelCasePropName},");
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

const INITIAL_STATE = {{
    ...generalState(),
{initialStateBuilder.ToString().TrimEnd()}
}};

export const {storeName} = defineStore('{entityLower}', {{
    state: () => ({{...INITIAL_STATE}}),
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
    }}
}});
";

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

        public static void GenerateStoreFileForEntityWithAsset(string entityName, List<(string propName, string propValue, bool isRequired)> properties)
        {
            if (VueJsSolutionPath.Length == 0)
                throw new Exception("من فضلك ادخل المسار الجذري لمشروع Vue");

            var entityLower = char.ToLower(entityName[0]) + entityName.Substring(1);
            var storeName = $"use{entityName}Store";
            var fileName = Path.Combine(VueJsSolutionPath, "store", $"{entityName}Store.js");

            var restEndpoint = entityName.ToLower() + "s";
            var initialStateBuilder = new StringBuilder();
            var propertyObjectBuilder = new StringBuilder();
            var requiredChecksBuilder = new StringBuilder();

            foreach (var (name, value, isRequired) in properties)
            {
                initialStateBuilder.AppendLine($"    {name}: {value},");
                propertyObjectBuilder.AppendLine($"            {name}: this.{name},");

                if (isRequired)
                {
                    requiredChecksBuilder.AppendLine($@"
            if (!item.{name}) {{
                this[SAVE_FAIL]();
                this.sendErrorMessage(I18n.global.t('message.{name}Required'));
                return;
            }}");
                }
            }

            var content = $@"
import {{ generalState, generalActions }} from '../store/GeneralStore';
import {{ defineStore }} from 'pinia';
import {{ AREA_ROUTE as PAGE_ROUTE }} from '@/utils/Constants';
import {{
    EDIT_PAGE_STATE,
    LOAD_SUCCESS,
    SAVE,
    SAVE_FAIL,
    SAVE_ITEM,
    VIEW_ITEM,
    VIEW_PAGE_STATE
}} from '@/utils/StoreConstant';
import I18n from '@/config/i18n';
import * as generalBackend from '@/backend/Backend';

const REST_ENDPOINT = (id) => `{restEndpoint}${{id ? '/' + id : ''}}`;

const INITIAL_STATE = {{
    ...generalState(),
{initialStateBuilder.ToString().TrimEnd()}
}};

export const {storeName} = defineStore('{entityLower}', {{
    state: () => ({{ ...INITIAL_STATE }}),
    actions: {{
        ...generalActions(INITIAL_STATE, REST_ENDPOINT, PAGE_ROUTE),
        async [SAVE_ITEM]() {{
            this[SAVE]();
            const item = {{
                id: this.id,
{propertyObjectBuilder.ToString().TrimEnd()}
            }};
{requiredChecksBuilder.ToString().TrimEnd()}

            if (item.id === null) {{
                generalBackend.saveMultipart(this, REST_ENDPOINT(), PAGE_ROUTE(), item, item.asset, 'assetName');
            }} else {{
                generalBackend.updateMultipart(this, REST_ENDPOINT(item.id), PAGE_ROUTE(), item, item.asset, 'assetName');
            }}
        }}
    }}
}});
";

            Directory.CreateDirectory(Path.Combine(VueJsSolutionPath, "store"));
            File.WriteAllText(fileName, content);
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

        public static void GenerateTableView(string entityName, string srcDir, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps,List<Relation> relations)
        {
            var entityLower = char.ToLower(entityName[0]) + entityName.Substring(1);
            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";
            string capitalEntityPlural = entityPlural.ToUpper();
            string entityPluralLower = char.ToLower(entityPlural[0]) + entityPlural.Substring(1);
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
                var typeWithoutNullable = prop.Type.TrimEnd('?');
                if (typeWithoutNullable == "string")
                {
                    var propLower = char.ToLower(prop.Name[0]) + prop.Name.Substring(1);
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
            StringBuilder relationImports = new StringBuilder();
            StringBuilder relationConsts = new StringBuilder();
            foreach ( var rel in relations )
            {
                if (rel.Type == RelationType.OneToOneSelfJoin || rel.Type == RelationType.ManyToOne || rel.Type == RelationType.ManyToOneNullable
                      || rel.Type == RelationType.OneToOne || rel.Type == RelationType.OneToOneNullable)
                {
                    var propLower = rel.Type != RelationType.OneToOneSelfJoin ? char.ToLower(rel.RelatedEntity[0]) + rel.RelatedEntity.Substring(1) + $"{rel.DisplayedProperty}"
                        : char.ToLower(rel.RelatedEntity[0]) + rel.RelatedEntity.Substring(1) + $"Parent{rel.DisplayedProperty}";
                    //string st = $"    {{field: '{propLower}', order: 0 }},";
                    //filterSectionSortFields.Add(st);
                    filterSectionGlobalFields.Add($"'{propLower}'");
                    filterSectionInitFilters.Add($"    {propLower}: {{ operator: FilterOperator.AND, constraints: [{{ value: null, matchMode: FilterMatchMode.EQUALS }}] }},");
                    string entityRelatedPlural = rel.RelatedEntity.EndsWith("y") ? rel.RelatedEntity[..^1] + "ies" : rel.RelatedEntity + "s";
                    string entityRelatedPluralLower = char.ToLower(entityRelatedPlural[0]) + entityRelatedPlural.Substring(1);
                    relationImports.AppendLine($"import {{ use{rel.RelatedEntity}Store }} from '@/store/{rel.RelatedEntity}Store';");
                    relationConsts.AppendLine($"const {{ items: {entityRelatedPluralLower}, loading: loading{entityRelatedPlural}, search: search{entityRelatedPlural} }} = useList(use{rel.RelatedEntity}Store(), '', {{}});");
                }
                if(rel.Type == RelationType.ManyToMany)
                {
                    string displayedPropPlural = rel.DisplayedProperty.EndsWith("y") ? rel.DisplayedProperty[..^1] + "ies" : rel.DisplayedProperty + "s";
                    var propLower = char.ToLower(rel.RelatedEntity[0]) + rel.RelatedEntity.Substring(1) + $"{displayedPropPlural}";
                    filterSectionGlobalFields.Add($"'{propLower}'");
                    filterSectionInitFilters.Add($"    {propLower}: {{ operator: FilterOperator.AND, constraints: [{{ value: null, matchMode: FilterMatchMode.CONTAINS }}] }},");
                    string entityRelatedPlural = rel.RelatedEntity.EndsWith("y") ? rel.RelatedEntity[..^1] + "ies" : rel.RelatedEntity + "s";
                    string entityRelatedPluralLower = char.ToLower(entityRelatedPlural[0]) + entityRelatedPlural.Substring(1);
                    relationImports.AppendLine($"import {{ use{rel.RelatedEntity}Store }} from '@/store/{rel.RelatedEntity}Store';");
                    relationConsts.AppendLine($"const {{ items: {entityRelatedPluralLower}, loading: loading{entityRelatedPlural}, search: search{entityRelatedPlural} }} = useList(use{rel.RelatedEntity}Store(), '', {{}});");
                }
            }

            StringBuilder colomnBuilder = new StringBuilder();
            foreach( var prop in properties )
            {
                string colomn = GetTableColomnControl(entityName, prop, enumProps);
                colomnBuilder.AppendLine(colomn);
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
import ListTemplate from '@/components/ListTemplate.vue';
import {{ FilterMatchMode, FilterOperator }} from '@primevue/core/api';
{relationImports}
const {{ t, formattedDate, dataTableDateFormatter }} = useList(useStore(), PAGE_ROUTE, {{ autoLoad: false }});
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
            var propLower = char.ToLower(prop.Name[0]) + prop.Name.Substring(1);
            var typeWithoutNullable = prop.Type.TrimEnd('?');
            if (typeWithoutNullable == "string" || (typeWithoutNullable == "int" && !enumProps.Any(ep => ep.prop == prop.Name)) || typeWithoutNullable == "double" || typeWithoutNullable == "decimal" || typeWithoutNullable == "float")
            {
                return $@"
            <Column field=""{propLower}"" :header=""$t('field.{propLower}')"" :show-add-button=""false"" :show-filter-operator=""false"" :sortable=""true"">
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
            <Column field=""{propLower}"" :header=""$t('field.{propLower}')"" :show-add-button=""false"" :show-filter-match-modes=""false"" :show-filter-operator=""false"" :sortable=""true"" >
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
                <Column field=""{propLower}"" dataType=""date"" style=""width: 80px"" :header=""$t('field.{propLower}')"" :show-add-button=""false"" :show-filter-operator=""false"" :sortable=""true"">
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
            <Column field=""{propLower}"" :header=""$t('field.{propLower}')"" :show-add-button=""false""
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

            if (typeWithoutNullable == "VD" || typeWithoutNullable == "GPG" || typeWithoutNullable == "PNGs")
            {
                //TODO : handle assets states
                return null;
            }

            return $@"
            <Column field=""{propLower}"" :header=""$t('field.{propLower}')"" :show-add-button=""false"" :show-filter-operator=""false"" :sortable=""true"">
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
                    if (rel.Type == RelationType.OneToOneSelfJoin || rel.Type == RelationType.ManyToOne || rel.Type == RelationType.ManyToOneNullable
                         || rel.Type == RelationType.OneToOne || rel.Type == RelationType.OneToOneNullable)
                    {
                        string entityRelatedPlural = rel.RelatedEntity.EndsWith("y") ? rel.RelatedEntity[..^1] + "ies" : rel.RelatedEntity + "s";
                        string entityRelatedPluralLower = char.ToLower(entityRelatedPlural[0]) + entityRelatedPlural.Substring(1);
                        var lowerRelatedEntity = char.ToLower(rel.RelatedEntity[0]) + rel.RelatedEntity.Substring(1);
                        var displayedProp = char.ToLower(rel.DisplayedProperty[0]) + rel.DisplayedProperty.Substring(1);
                        var propLower = rel.Type != RelationType.OneToOneSelfJoin ? lowerRelatedEntity + $"{rel.DisplayedProperty}"
                            : lowerRelatedEntity + $"Parent{rel.DisplayedProperty}";
                        sb.AppendLine($@"
                <Column field=""{propLower}"" :header=""$t('field.{propLower}')"" :show-add-button=""false"" :show-filter-match-modes=""false"" :show-filter-operator=""false"" :sortable=""true"">
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
                        string entityRelatedPlural = rel.RelatedEntity.EndsWith("y") ? rel.RelatedEntity[..^1] + "ies" : rel.RelatedEntity + "s";
                        string entityRelatedPluralLower = char.ToLower(entityRelatedPlural[0]) + entityRelatedPlural.Substring(1);
                        var lowerRelatedEntity = char.ToLower(rel.RelatedEntity[0]) + rel.RelatedEntity.Substring(1);
                        string displayedPropPlural = rel.DisplayedProperty.EndsWith("y") ? rel.DisplayedProperty[..^1] + "ies" : rel.DisplayedProperty + "s";
                        var displayedProp = char.ToLower(rel.DisplayedProperty[0]) + rel.DisplayedProperty.Substring(1);
                        sb.AppendLine($@"
                <Column field=""{lowerRelatedEntity}{displayedPropPlural}"" :header=""$t('field.{lowerRelatedEntity}{displayedPropPlural}')"" :show-add-button=""false"" :show-filter-match-modes=""false"" :show-filter-operator=""false"" :sortable=""true"">
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
            if (sb.Length > 0)
                return sb.ToString().TrimEnd();
            else
                return null;
        }
        public static void GenerateSingleView(string entityName, string srcDir, List<(string Type, string Name, PropertyValidation Validation)> properties, List<(string prop, List<string> enumValues)> enumProps,List<Relation> relations)
        {
            var entityLower = char.ToLower(entityName[0]) + entityName.Substring(1);
            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";
            string capitalEntityPlural = entityPlural.ToUpper();
            string entityPluralLower = char.ToLower(entityPlural[0]) + entityPlural.Substring(1);
            string viewDirectory = Path.Combine(srcDir, "views", $"{entityLower}");
            Directory.CreateDirectory(viewDirectory);
            string fileSingleName = entityName;
            string viewSinglePath = Path.Combine(viewDirectory, $"{entityName}.vue");

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
                string colomn = GetSingleColomnControl(entityName, prop,enumProps);
                colomnBuilder.AppendLine(colomn);
            }
            string? relationColomn = GetSingleColomnRelationControl(entityName, relations);
            if (relationColomn != null)
                colomnBuilder.AppendLine(relationColomn);
            string content = $@"
<script setup>
import {{ use{entityName}Store as useStore }} from '@/store/{entityName}Store';
import {{ {capitalEntityPlural}_ROUTE as PAGE_ROUTE}} from '@/utils/Constants';
{relationImports}
import useSingle from '@/composables/useSingle';
import useList from '@/composables/useList';

const store = useStore();

const {{ state, onPropChanged, onSave, onCancel, t }} = useSingle(store, PAGE_ROUTE);
{relationConsts}
{enumConsts}
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

            if (typeWithoutNullable == "VD" || typeWithoutNullable == "GPG" || typeWithoutNullable == "PNGs")
            {
                //TODO : handle assets cases
                return null;
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
                        v-model=""state.{propLower}""
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
