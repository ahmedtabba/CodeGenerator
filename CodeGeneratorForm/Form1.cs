using DomainGenerator;
using ApiGenerator;
using InfrastructureGenerator;
using SharedClasses;
using Application = ApplicationGenerator.Application;
using ApplicationGenerator;
using System.Text.RegularExpressions;

namespace CodeGeneratorForm
{
    public partial class Form1 : Form
    {
        private static readonly Regex ClassPattern = new Regex(
    @"public\s+class\s+(\w+)\s*\{",
    RegexOptions.Multiline | RegexOptions.IgnoreCase);

        public List<Relation> Relations { get; set; } = new List<Relation>();
        public SharedClasses.Properties Properties { get; set; } = new SharedClasses.Properties();
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void btnProperty_Click(object sender, EventArgs e)
        {
            var isLocalizaed = this.checkBoxLocalization.Checked;
            PropertyForm propertyForm = new PropertyForm(isLocalizaed);
            propertyForm.ShowDialog();
            var propertyInfo = propertyForm.PropertyInfo;

            if (propertyInfo.IsSaved)
            {

                Properties.PropertiesList.Add((propertyInfo.GeneralInfo.Type, propertyInfo.GeneralInfo.Name, propertyInfo.GeneralInfo.Validation));
                if (propertyInfo.Localized)
                    Properties.LocalizedProp.Add(propertyInfo.GeneralInfo.Name);
                if (propertyInfo.EnumValues.enumValues != null && propertyInfo.EnumValues.enumValues.Any())
                    Properties.EnumProps.Add(propertyInfo.EnumValues);

                richtxtProps.AppendText($"Property {propertyInfo.GeneralInfo.Name} has been added." + Environment.NewLine);
            }
        }


        private void btnSave_Click(object sender, EventArgs e)
        {

            var hasLocalization = checkBoxLocalization.Checked;
            var hasPermissions = checkBoxPermissions.Checked;
            var hasVersioning = checkBoxVersioning.Checked;
            var hasUserAction = checkBoxUserActions.Checked;
            var hasNotification = checkBoxNotifications.Checked;
            var entityName = txtEntityName.Text;
            var solutionDir = $"{txtDir.Text}";

            if (!ValidateSolution()) 
            {
                ClearForm();
                return;
            }
            #region Generate Code

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
            try
            {
                if (hasPermissions)
                {
                    Infrastructure.GeneratePermission(entityName, domainPath, hasLocalization);
                }
                Domain.GenerateEntityClass(entityName, domainPath, (Properties.PropertiesList, Properties.LocalizedProp, Properties.EnumProps), hasLocalization, Relations);
                //GenerateEntityLocalizationClass(entityName, domainPath);
                Infrastructure.UpdateAppDbContext(entityName, domainPath);
                if (hasLocalization)
                    Infrastructure.UpdateAppDbContext($@"{entityName}Localization", domainPath);
                List<string> relatedEntitiesList = new List<string>();
                foreach (var relation in Relations)
                {
                    if (relation.Type == RelationType.ManyToMany)
                    {
                        relatedEntitiesList.Add(relation.RelatedEntity);
                        List<(string Type, string Name, PropertyValidation Validation)> props = new List<(string Type, string Name, PropertyValidation Validation)>
                {
                    ("Guid",$"{entityName}Id",new PropertyValidation()),
                    ("Guid",$"{relation.RelatedEntity}Id",new PropertyValidation())
                };
                        Domain.GenerateEntityClass($"{entityName}{relation.RelatedEntity}", domainPath, (props, new List<string>(), new List<(string, List<string>)>()), false, new List<Relation>());
                        Infrastructure.UpdateAppDbContext($"{entityName}{relation.RelatedEntity}", domainPath);
                        Application.GenerateIRepositoryInterface($"{entityName}{relation.RelatedEntity}", repoInterfacePath);
                        Infrastructure.GenerateRepository($"{entityName}{relation.RelatedEntity}", repoPath);
                        Infrastructure.UpdateDependencyInjection($"{entityName}{relation.RelatedEntity}", domainPath);
                    }
                }

                Infrastructure.GenerateConfiguration(entityName, domainPath, relatedEntitiesList);
                Application.GenerateIRepositoryInterface(entityName, repoInterfacePath);
                if (hasLocalization)
                    Application.GenerateIRepositoryInterface($@"{entityName}Localization", repoInterfacePath);
                Infrastructure.GenerateRepository(entityName, repoPath);
                if (hasLocalization)
                    Infrastructure.GenerateRepository($@"{entityName}Localization", repoPath);
                Infrastructure.UpdateDependencyInjection(entityName, domainPath);
                if (hasLocalization)
                    Infrastructure.UpdateDependencyInjection($@"{entityName}Localization", domainPath);
                if (hasVersioning)
                    ApplicationAssistant.GenerateVersionNeeds(entityName, domainPath, Properties.PropertiesList, Relations);
                if (hasNotification)
                    ApplicationAssistant.GenerateNotificationNeeds(entityName, domainPath);
                if (hasUserAction)
                    ApplicationAssistant.GenerateUserActionNeeds(entityName, domainPath);
                if (hasLocalization)
                    Infrastructure.UpdateLocalizationService(entityName, domainPath, Properties.LocalizedProp);
                if (hasNotification || hasVersioning || hasUserAction)
                {
                    ApplicationAssistant.GenerateEvents(entityName, domainPath, hasVersioning);
                    ApplicationAssistant.GenerateHandlers(entityName, domainPath, Properties.PropertiesList, Relations, hasVersioning, hasUserAction, hasNotification);
                }
                Application.GenerateCreateCommand(entityName, entityPlural, createCommandPath, Properties.PropertiesList, Properties.EnumProps, hasLocalization, Relations, hasVersioning, hasNotification, hasUserAction);
                Application.GenerateCreateCommandValidator(entityName, entityPlural, createCommandPath, Properties.PropertiesList, Relations);

                Application.GenerateUpdateCommand(entityName, entityPlural, updateCommandPath, Properties.PropertiesList, Properties.EnumProps, hasLocalization, Relations, hasVersioning, hasNotification, hasUserAction);
                Application.GenerateUpdateCommandValidator(entityName, entityPlural, updateCommandPath, Properties.PropertiesList, Relations);


                Application.GenerateDeleteCommand(entityName, entityPlural, deleteCommandPath, Properties.PropertiesList, hasVersioning, hasNotification, hasUserAction);
                Application.GenerateDeleteCommandValidator(entityName, entityPlural, deleteCommandPath, Properties.PropertiesList);


                Application.GenerateGetByIdQuery(entityName, entityPlural, queryPath, hasLocalization, Properties.PropertiesList, Properties.EnumProps, Relations);

                Application.GenerateGetWithPaginationQuery(entityName, entityPlural, queryPath, hasLocalization, Relations);
                Application.GenerateBaseDto(entityName, entityPlural, Properties.PropertiesList, Properties.EnumProps, solutionDir, Relations, hasLocalization);

                if (hasLocalization)
                    Application.GenerateGetWithLocalizationQuery(entityName, entityPlural, queryPath, Properties.PropertiesList, Properties.EnumProps, Relations);

                Api.GenerateNeededDtos(entityName, entityPlural, Properties.PropertiesList, Properties.EnumProps, solutionDir, hasLocalization, Relations);

                Api.AddRoutesToApiRoutes(entityName, entityPlural, solutionDir, hasLocalization);

                Api.GenerateController(entityName, entityPlural, Properties.PropertiesList, Properties.EnumProps, solutionDir, hasLocalization, hasPermissions);
            }
            catch( Exception ex )
            {
                throw;
            }

            #endregion
            MessageBox.Show($"Entity : {entityName} has been added.");
            ClearForm();
        }

        private void checkBoxLocalization_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxLocalization.Checked)
                lblLocalization.ForeColor = Color.Green;
            else
                lblLocalization.ForeColor = Color.Black;
        }

        private void checkBoxPermissions_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxPermissions.Checked)
                lblPermissions.ForeColor = Color.Green;
            else
                lblPermissions.ForeColor = Color.Black;
        }

        private void checkBoxUserActions_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxUserActions.Checked)
                lblUserActions.ForeColor = Color.Green;
            else
                lblUserActions.ForeColor = Color.Black;
        }

        private void checkBoxVersioning_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxVersioning.Checked)
                lblVersioning.ForeColor = Color.Green;
            else
                lblVersioning.ForeColor = Color.Black;
        }

        private void checkBoxNotifications_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxNotifications.Checked)
                lblNotifications.ForeColor = Color.Green;
            else
                lblNotifications.ForeColor = Color.Black;
        }

        private void btnNewRelation_Click(object sender, EventArgs e)
        {
            RelationForm relationForm = new RelationForm();
            relationForm.ShowDialog();
            var relation = relationForm.Relation;
            if (relationForm.IsSaved)
            {
                Relations.Add(relation);
                richtxtRelations.AppendText($"Relation with {relation.RelatedEntity}, Type {relation.Type.ToString()} has bee added." + Environment.NewLine);
            }
        }

        private void ClearForm()
        {
            checkBoxLocalization.Checked = false;
            checkBoxNotifications.Checked = false;
            checkBoxPermissions.Checked = false;
            checkBoxUserActions.Checked = false;
            checkBoxVersioning.Checked = false;
            txtEntityName.Clear();
            richtxtProps.Clear();
            richtxtRelations.Clear();
            Properties = null!;
            Properties = new SharedClasses.Properties();
            Relations.Clear();
        }
        private bool ValidateSolution()
        {
            var entityName = txtEntityName.Text;

            if (string.IsNullOrWhiteSpace(entityName))
            {
                MessageBox.Show("Entity Name missing");
                return false;
            }
            string entityPlural = entityName.EndsWith("y") ? entityName[..^1] + "ies" : entityName + "s";

            if (!Directory.Exists($"{txtDir.Text}"))
            {
                MessageBox.Show("Solution Dir is invalid");
                return false;
            }
            //Api Validation
            string filePath = Path.Combine($"{txtDir.Text}", "Api", "Utilities", "ApiRoutes.cs");

            if (!File.Exists(filePath))
            {
                MessageBox.Show("ApiRoutes.cs is not found");
                return false;
            }
            string content = File.ReadAllText(filePath);
            var matches = ClassPattern.Matches(content);
            foreach (Match match in matches)
            {
                if (match.Groups[1].Value == entityName)
                {
                    MessageBox.Show($"?? ApiRoutes already contains routes for {entityName}, the generator will continue.");
                }
            }

            filePath = Path.Combine($"{txtDir.Text}", "Api", "Utilities", "Extensions.cs");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("Extensions.cs is not found");
                return false;
            }
            content = File.ReadAllText(filePath);
            if(!content.Contains("//Add Using Here") || !content.Contains("//Add Extension Here"))
            {
                MessageBox.Show("Extensions.cs dose not contain necessary strings for generator");
                return false;
            }
            //Application Validation
            filePath = Path.Combine($"{txtDir.Text}", "Application", "Common", "Interfaces", "Db", "IApplicationDbContext.cs");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("IApplicationDbContext.cs is not found");
                return false;
            }
            content = File.ReadAllText(filePath);
            if (!content.Contains("//Generate Here"))
            {
                MessageBox.Show("IApplicationDbContext.cs dose not contain necessary strings for generator");
                return false;
            }
            if (checkBoxLocalization.Checked)
            {
                filePath = Path.Combine($"{txtDir.Text}", "Application", "Common", "Interfaces", "Services", "ILocalizationService.cs");
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("ILocalizationService.cs is not found");
                    return false;
                }
                content = File.ReadAllText(filePath);
                if (!content.Contains("//Add Using Here") || !content.Contains("//Define Localization Method Here"))
                {
                    MessageBox.Show("ILocalizationService.cs dose not contain necessary strings for generator");
                    return false;
                }
            }
            
            filePath = Path.Combine($"{txtDir.Text}", "Application", "Common", "Models", "Localization");
            if (!Directory.Exists(filePath))
            {
                MessageBox.Show("Localization Model Directory not found");
                return false;
            }
            if (checkBoxVersioning.Checked)
            {
                filePath = Path.Combine($"{txtDir.Text}", "Application", "Common", "Models", "Versioning");
                if (!Directory.Exists(filePath))
                {
                    MessageBox.Show("Versioning Model Directory not found");
                    return false;
                }
            }
            filePath = Path.Combine($"{txtDir.Text}", "Application", "Utilities", "NotificationConsistent.cs");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("NotificationConsistent.cs is not found");
                return false;
            }
            content = File.ReadAllText(filePath);
            if (!content.Contains("//Add To Group Here"))
            {
                MessageBox.Show("NotificationConsistent.cs dose not contain necessary strings for generator");
                return false;
            }
            if (checkBoxNotifications.Checked)
            {
                content = File.ReadAllText(filePath);
                matches = ClassPattern.Matches(content);
                foreach (Match match in matches)
                {
                    if (match.Groups[1].Value == entityPlural)
                    {
                        MessageBox.Show($"?? NotificationConsistent already contains notifications for {entityName}, the generator will continue.");
                    }
                }
            }

            //Domain Validation
            filePath = Path.Combine($"{txtDir.Text}", "Domain", "Entities", "Language.cs");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("Language.cs is not found");
                return false;
            }
            content = File.ReadAllText(filePath);
            if (!content.Contains("//Generate Here"))
            {
                MessageBox.Show("Language.cs dose not contain necessary strings for generator");
                return false;
            }

            filePath = Path.Combine($"{txtDir.Text}", "Domain", "Enums", "NotificationObjectTypes.cs");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("NotificationObjectTypes.cs is not found");
                return false;
            }
            content = File.ReadAllText(filePath);
            if (!content.Contains("//Add Here"))
            {
                MessageBox.Show("NotificationObjectTypes.cs dose not contain necessary strings for generator");
                return false;
            }

            filePath = Path.Combine($"{txtDir.Text}", "Domain", "Enums", "UserActionEntityType.cs");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("UserActionEntityType.cs is not found");
                return false;
            }
            content = File.ReadAllText(filePath);
            if (!content.Contains("//Add Here"))
            {
                MessageBox.Show("UserActionEntityType.cs dose not contain necessary strings for generator");
                return false;
            }

            filePath = Path.Combine($"{txtDir.Text}", "Domain", "Enums", "VersionEntityType.cs");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("VersionEntityType.cs is not found");
                return false;
            }
            content = File.ReadAllText(filePath);
            if (!content.Contains("//Add Here"))
            {
                MessageBox.Show("VersionEntityType.cs dose not contain necessary strings for generator");
                return false;
            }

            //Infrastructure Validation
            filePath = Path.Combine($"{txtDir.Text}", "Infrastructure", "Data", "Configurations");
            if (!Directory.Exists(filePath))
            {
                MessageBox.Show("Configurations Directory not found");
                return false;
            }

            filePath = Path.Combine($"{txtDir.Text}", "Infrastructure", "Data", "AppDbContext.cs");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("AppDbContext.cs is not found");
                return false;
            }
            content = File.ReadAllText(filePath);
            if (!content.Contains("//Generate Here"))
            {
                MessageBox.Show("AppDbContext.cs dose not contain necessary strings for generator");
                return false;
            }

            filePath = Path.Combine($"{txtDir.Text}", "Infrastructure", "Data", "ApplicationDbContextInitialiser.cs");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("ApplicationDbContextInitialiser.cs is not found");
                return false;
            }
            content = File.ReadAllText(filePath);
            if (!content.Contains("//Add Permission Here") || !content.Contains("//Add To rolesAfterInitialize Here") || !content.Contains("//Except rolesAfterInitialize Here")
                || !content.Contains("//Add RoleConsistent Loop") || !content.Contains("//Add Notifications Here"))
            {
                MessageBox.Show("ApplicationDbContextInitialiser.cs dose not contain necessary strings for generator");
                return false;
            }

            filePath = Path.Combine($"{txtDir.Text}", "Infrastructure", "Utilities", "RoleConsistent.cs");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("RoleConsistent.cs is not found");
                return false;
            }
            content = File.ReadAllText(filePath);
            if (!content.Contains("//Add To Group Here"))
            {
                MessageBox.Show("RoleConsistent.cs dose not contain necessary strings for generator");
                return false;
            }
            if (checkBoxPermissions.Checked)
            {
                content = File.ReadAllText(filePath);
                matches = ClassPattern.Matches(content);
                foreach (Match match in matches)
                {
                    if (match.Groups[1].Value == entityName)
                    {
                        MessageBox.Show($"?? RoleConsistent already contains roles for {entityName}, the generator will continue.");
                    }
                }
            }
            if (checkBoxLocalization.Checked)
            {
                filePath = Path.Combine($"{txtDir.Text}", "Infrastructure", "Services", "LocalizationService.cs");
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("LocalizationService.cs is not found");
                    return false;
                }
                content = File.ReadAllText(filePath);
                if (!content.Contains("//Add Using Here") || !content.Contains("//Add Private Field Here")
                    || !content.Contains("//Inject Service Here") || !content.Contains("//Add Field Here") || !content.Contains("//Implement Method Here"))
                {
                    MessageBox.Show("LocalizationService.cs dose not contain necessary strings for generator");
                    return false;
                }
            }

            filePath = Path.Combine($"{txtDir.Text}", "Infrastructure", "DependencyInjection.cs");
            if (!File.Exists(filePath))
            {
                MessageBox.Show("DependencyInjection.cs is not found");
                return false;
            }
            content = File.ReadAllText(filePath);
            if (!content.Contains("//Register Here"))
            {
                MessageBox.Show("DependencyInjection.cs dose not contain necessary strings for generator");
                return false;
            }

            //Relations Validation
            filePath = Path.Combine($"{txtDir.Text}", "Infrastructure", "Data", "AppDbContext.cs");
            content = File.ReadAllText(filePath);
            foreach(var item in this.Relations)
            {
                if (!content.Contains($"DbSet<{item.RelatedEntity}>"))
                {
                    MessageBox.Show($"Relation with {item.RelatedEntity} is failed : DbSet of {item.RelatedEntity} dose not found in AppDbContext");
                    return false;
                }
            }
            if (!content.Contains("//Generate Here"))
            {
                MessageBox.Show("AppDbContext.cs dose not contain necessary strings for generator");
                return false;
            }

            return true;
        }
    }
}
