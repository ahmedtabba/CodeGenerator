using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Frontend.VueJsHelper
{
    public class VueJsHelper
    {
        public static string VueJsSolutionPath = ""; // ضع المسار الجذري لمشروع Vue هنا

        public static void GenerateStoreFile(string entityName, SharedClasses.Properties properties,string srcDir)
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
            var propertyObjectBuilder = new StringBuilder();
            var requiredChecksBuilder = new StringBuilder();

            foreach (var prop in properties.PropertiesList)
            {
                string camelCasePropName = char.ToLower(prop.Name[0]) + prop.Name.Substring(1);
                initialStateBuilder.AppendLine($"        {camelCasePropName}: {GetDefaultValue(prop.Type)},");
                propertyObjectBuilder.AppendLine($"                data.append('{camelCasePropName}', this.{camelCasePropName});");
                if (prop.Validation!=null&& prop.Validation.Required)
                {
                    requiredChecksBuilder.AppendLine($@"
            if (!this.{camelCasePropName}) {{
                this[SAVE_FAIL]();
                this.sendErrorMessage(I18n.global.t('message.{camelCasePropName}Required'));
                return;
            }}");
                }
            }

            var content = $@"
import {{
    EDIT_PAGE_STATE,
    LOAD_SUCCESS,
    SAVE,
    SAVE_FAIL,
    SAVE_ITEM,
    VIEW_ITEM,
    VIEW_PAGE_STATE
}} from '@/utils/StoreConstant';
import {{generalActions,generalState}} from './GeneralStore';
import I18n from '../config/i18n/index';
import {{defineStore}} from 'pinia';
import * as generalBackend from '@/backend/Backend';
import {{{capitalEntityPlural}_ROUTE as PAGE_ROUTE}} from '@/utils/Constants';
import {{groupBy}} from '@/utils/utils';

const REST_ENDPOINT = (id) => `{restEndpoint}${{id ? '/' + id : ''}}`;

const INITIAL_STATE = {{
    ...generalState(),
{initialStateBuilder.ToString().TrimEnd()}
}};

export const {storeName} = defineStore('{entityLower}', {{
    state: () => ({{...INITIAL_STATE}}),
    actions: {{
        ...generalActions(INITIAL_STATE, REST_ENDPOINT, PAGE_ROUTE),
        async [SAVE_ITEM]() {{
            this[SAVE]();
{requiredChecksBuilder.ToString().TrimEnd()}
            let data = new FormData();
{propertyObjectBuilder}
            if (this.id === null) {{
                generalBackend.saveFormData(this, REST_ENDPOINT(), PAGE_ROUTE(), item);
            }} else {{
                generalBackend.updateFormData(this, REST_ENDPOINT(this.id), PAGE_ROUTE(), data);
            }}
        }},
    }}
}});
";

            Directory.CreateDirectory(Path.Combine(srcDir, "store"));
            File.WriteAllText(filePath, content);
        }

        private static object GetDefaultValue(string type)
        {
            //Returns the default value for the given type
            switch (type)
            {
                case "string":
                    return "\'\'";
                case "int":
                    return "0";
                //add more cases as needed
                case "bool":
                    return "false";
                case "float":
                    return "0.0";
                case "double":
                    return "0.0";
                case "long":
                    return "0";
                case "DateTime":
                    return "new Date()";
                case "DateTimeOffset":
                    return "new Date()";
                case "decimal":
                    return "0.0";
                case "byte":
                    return "0";
                case "short":
                    return "0";
                case "char":
                    return "\'\'";
                case "Guid":
                    return "new Guid()";
                case "List of":
                    return "[]";
                case "GPG":
                    return "null";
                case "VD":
                    return "\'\'";
                case "PNGs":
                    return "[]";
                case "enum":
                    return "0";
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
                    path: '/{entityPluralLower}',
                    name: '{entityPlural}',
                    component: {{
                        render() {{
                            return h(resolveComponent('router-view'));
                        }}
                    }},
                    children: [
                        {{
                            path: '',
                            meta: {{
                                label: '{entityPlural}'
                            }},
                            component: () => import('@/views/{entityLower}/{entityPlural}.vue')
                        }},
                        {{
                            path: ':id',
                            name: '{entityName} Details',
                            component: () => import('@/views/{entityLower}/{entityName}.vue')
                        }}
                    ]
                }},
" +
                $"\n                //Add router Here";

            var lines = File.ReadAllLines(routerIndexPath).ToList();
            var index = lines.FindIndex(line => line.Contains("//Add router Here"));

            if (index >= 0)
            {
                lines[index] = router;
                File.WriteAllLines(routerIndexPath, lines);
            }
        }

        public static void GenerateViews(string entityName, string srcDir)
        {
            var entityLower = char.ToLower(entityName[0]) + entityName.Substring(1);
            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";
            string capitalEntityPlural = entityPlural.ToUpper();
            string entityPluralLower = char.ToLower(entityPlural[0]) + entityPlural.Substring(1);
            string viewDirectory = Path.Combine(srcDir, "views", $"{entityLower}");
            Directory.CreateDirectory(viewDirectory);
            string fileTableName = entityPlural;
            string viewTablePath = Path.Combine(viewDirectory, $"{entityPlural}.vue");

            string content = $@"

";

            File.WriteAllText(viewTablePath, content);
        }

    }
}
