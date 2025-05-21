using System.IO;
using System.Text;

namespace Frontend.VueJsHelper
{
    public class VueJsHelper
    {
        public static string VueJsSolutionPath = ""; // ضع المسار الجذري لمشروع Vue هنا

        public static void GenerateStoreFile(string entityName, SharedClasses.Properties properties)
        {
            if(VueJsSolutionPath.Length==0)
                throw new Exception("من فضلك ادخل المسار الجذري لمشروع Vue");
            
            var entityLower = char.ToLowerInvariant(entityName[0]) + entityName.Substring(1);
            var storeName = $"use{entityName}Store";
            var fileName = Path.Combine(VueJsSolutionPath, "store", $"{entityName}Store.js");

            var restEndpoint = entityName.ToLower() + "s";
            var initialStateBuilder = new StringBuilder();
            var propertyObjectBuilder = new StringBuilder();
            var requiredChecksBuilder = new StringBuilder();

            foreach (var prop in properties.PropertiesList)
            {
                initialStateBuilder.AppendLine($"        {prop.Name.ToLower()}: {GetDefaultValue(prop.Type)},");
                propertyObjectBuilder.AppendLine($"                {prop.Name.ToLower()}: this.{prop.Name.ToLower()},");
                if (prop.Validation!=null&& prop.Validation.Required)
                {
                    requiredChecksBuilder.AppendLine($@"
            if (!item.{prop.Name.ToLower()}) {{
                this[SAVE_FAIL]();
                this.sendErrorMessage(I18n.global.t('message.{prop.Name.ToLower()}Required'));
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
    state: () => ({{...INITIAL_STATE}}),
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
                generalBackend.save(this, REST_ENDPOINT(), PAGE_ROUTE(), item);
            }} else {{
                generalBackend.update(this, REST_ENDPOINT(item.id), PAGE_ROUTE(), item);
            }}
        }},
    }}
}});
";

            Directory.CreateDirectory(Path.Combine(VueJsSolutionPath, "store"));
            File.WriteAllText(fileName, content);
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
                default:
                    return "null";
            }
        }

        public static void GenerateStoreFileForEntityWithAsset(string entityName, List<(string propName, string propValue, bool isRequired)> properties)
        {
            if (VueJsSolutionPath.Length == 0)
                throw new Exception("من فضلك ادخل المسار الجذري لمشروع Vue");

            var entityLower = char.ToLowerInvariant(entityName[0]) + entityName.Substring(1);
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


    }
}
