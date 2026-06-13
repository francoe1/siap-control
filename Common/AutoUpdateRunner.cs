using SiapControl.Data;
using SiapControl.Data.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SiapControl.Common
{
    public sealed class AutoUpdateRunResult
    {
        public int CheckedModules { get; set; }
        public int UpdatedModules { get; set; }
        public int SkippedModules { get; set; }
        public List<string> Messages { get; } = new List<string>();

        public string Summary => $"Modulos revisados: {CheckedModules}. Actualizados: {UpdatedModules}. Omitidos: {SkippedModules}.";
    }

    public enum AutoUpdatePlanStatus
    {
        UpToDate,
        UpdateAvailable,
        NoSafeMatch,
        NoCompatibleDownload,
        VersionNotComparable,
        MissingUser
    }

    public sealed class AutoUpdatePlanItem
    {
        public UserModel User { get; set; } = new UserModel();
        public ModuleModel Module { get; set; } = new ModuleModel();
        public AfipApplicationCatalogItem? CatalogItem { get; set; }
        public AfipApplicationPackage? Package { get; set; }
        public AutoUpdatePlanStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool CanUpdate => Status == AutoUpdatePlanStatus.UpdateAvailable && Package != null;
    }

    public sealed class AutoUpdateRunner
    {
        private const string AFIP_PATH = @"C:\Windows\afipPath.sys";
        private readonly IAfipApplicationCatalogService _catalogService;
        private readonly IAfipInstallerPackageService _packageService;
        private readonly AfipModuleMatcher _matcher;

        public AutoUpdateRunner()
            : this(new AfipApplicationCatalogService(), new AfipInstallerPackageService(), new AfipModuleMatcher())
        {
        }

        public AutoUpdateRunner(IAfipApplicationCatalogService catalogService, IAfipInstallerPackageService packageService, AfipModuleMatcher matcher)
        {
            _catalogService = catalogService;
            _packageService = packageService;
            _matcher = matcher;
        }

        public async Task<AutoUpdateRunResult> RunAsync()
        {
            IReadOnlyList<AutoUpdatePlanItem> plan = await BuildPlanAsync();
            AutoUpdateRunResult result = await RunPlanAsync(plan.Where(item => item.CanUpdate));
            result.CheckedModules = plan.Count;
            result.SkippedModules += plan.Count(item => item.Status != AutoUpdatePlanStatus.UpToDate && !item.CanUpdate);
            foreach (AutoUpdatePlanItem item in plan.Where(item => item.Status != AutoUpdatePlanStatus.UpToDate && !item.CanUpdate))
            {
                result.Messages.Add($"{item.Module.AppName}: {item.Message}");
            }

            return result;
        }

        public async Task<IReadOnlyList<AutoUpdatePlanItem>> BuildPlanAsync(int? userId = null, IEnumerable<int>? moduleIds = null)
        {
            var result = new AutoUpdateRunResult();
            IReadOnlyList<AfipApplicationCatalogItem> catalog = await _catalogService.GetCatalogAsync();
            var packageCache = new Dictionary<string, AfipApplicationPackage>(StringComparer.OrdinalIgnoreCase);
            HashSet<int>? moduleFilter = moduleIds == null ? null : new HashSet<int>(moduleIds);
            var plan = new List<AutoUpdatePlanItem>();

            IEnumerable<ModuleModel> modules = Database.UserModules.FindAll();
            if (userId.HasValue)
            {
                modules = modules.Where(module => module.UserId == userId.Value);
            }

            if (moduleFilter != null)
            {
                modules = modules.Where(module => moduleFilter.Contains(module.Id));
            }

            foreach (ModuleModel module in modules.ToList())
            {
                result.CheckedModules++;
                UserModel? user = Database.Users.FindById(module.UserId);
                if (user == null)
                {
                    plan.Add(new AutoUpdatePlanItem
                    {
                        Module = module,
                        Status = AutoUpdatePlanStatus.MissingUser,
                        Message = "No se encontro la instalacion."
                    });
                    continue;
                }

                AfipModuleMatch? match = _matcher.FindSafeMatch(module, catalog);
                if (match == null)
                {
                    plan.Add(new AutoUpdatePlanItem
                    {
                        User = user,
                        Module = module,
                        Status = AutoUpdatePlanStatus.NoSafeMatch,
                        Message = "Sin coincidencia segura en ARCA."
                    });
                    continue;
                }

                string cacheKey = match.CatalogItem.Link;
                if (!packageCache.TryGetValue(cacheKey, out AfipApplicationPackage package))
                {
                    AfipApplicationPackage? resolved = await _catalogService.ResolvePackageAsync(match.CatalogItem);
                    if (resolved == null || resolved.DownloadUri == null)
                    {
                        plan.Add(new AutoUpdatePlanItem
                        {
                            User = user,
                            Module = module,
                            CatalogItem = match.CatalogItem,
                            Status = AutoUpdatePlanStatus.NoCompatibleDownload,
                            Message = "No se encontro descarga oficial compatible."
                        });
                        continue;
                    }

                    package = resolved;
                    packageCache[cacheKey] = package;
                }

                if (!AfipVersionComparer.TryCompare(module.AppVersion, package.VersionText, out int comparison))
                {
                    plan.Add(new AutoUpdatePlanItem
                    {
                        User = user,
                        Module = module,
                        CatalogItem = match.CatalogItem,
                        Package = package,
                        Status = AutoUpdatePlanStatus.VersionNotComparable,
                        Message = $"Version no comparable ({module.AppVersion} vs {package.VersionText})."
                    });
                    continue;
                }

                bool hasUpdate = comparison < 0;
                plan.Add(new AutoUpdatePlanItem
                {
                    User = user,
                    Module = module,
                    CatalogItem = match.CatalogItem,
                    Package = package,
                    Status = hasUpdate ? AutoUpdatePlanStatus.UpdateAvailable : AutoUpdatePlanStatus.UpToDate,
                    Message = hasUpdate ? "Actualizacion disponible." : "Actualizado."
                });
            }

            return plan;
        }

        public async Task<AutoUpdateRunResult> RunPlanAsync(IEnumerable<AutoUpdatePlanItem> items)
        {
            var result = new AutoUpdateRunResult();
            AutoUpdatePlanItem[] pending = items.Where(item => item.CanUpdate).ToArray();
            result.CheckedModules = pending.Length;

            foreach (IGrouping<string, AutoUpdatePlanItem> group in pending.GroupBy(item => item.Package!.DownloadUri.ToString()))
            {
                AfipApplicationPackage package = group.First().Package!;
                SetupReader? setup = await _packageService.DownloadAndPrepareAsync(package);
                if (setup == null)
                {
                    foreach (AutoUpdatePlanItem item in group)
                    {
                        Skip(result, $"{item.Module.AppName}: no se pudo preparar el instalador.");
                    }
                    continue;
                }

                foreach (AutoUpdatePlanItem item in group)
                {
                    if (await InstallForUserAsync(setup, item.User))
                    {
                        result.UpdatedModules++;
                    }
                    else
                    {
                        Skip(result, $"{item.Module.AppName}: instalacion no finalizada.");
                    }
                }
            }

            return result;
        }

        private static void Skip(AutoUpdateRunResult result, string message)
        {
            result.SkippedModules++;
            result.Messages.Add(message);
        }

        private static async Task<bool> InstallForUserAsync(SetupReader setup, UserModel user)
        {
            File.WriteAllText(AFIP_PATH, user.Path);
            await setup.CreateBackupAsync(user.Path);

            SetupReader.Parameter titleParameter = setup.Parameters.FirstOrDefault(x => x.Name.Equals("Title", StringComparison.OrdinalIgnoreCase));
            string title = titleParameter.IsValid ? titleParameter.Value : setup.AppName;
            var installer = new SetupAutoInstaller(title, setup.Path);
            if (!await installer.InstallAsync())
            {
                return false;
            }

            await UpdateInstalledModuleAsync(setup, user);
            return true;
        }

        private static async Task UpdateInstalledModuleAsync(SetupReader setup, UserModel user)
        {
            string file = Path.Combine(user.Path, setup.AppName, setup.AppName + ".exe");
            if (!File.Exists(file))
            {
                return;
            }

            ModuleModel currentModule = await Task.Run(() => SiapReader.GetModuleModel(file));
            ModuleModel module = Database.UserModules.FindByUserAndAppName(user.Id, currentModule.AppName);

            if (module == null)
            {
                Database.UserModules.Insert(new ModuleModel
                {
                    UserId = user.Id,
                    AppName = currentModule.AppName,
                    AppVersion = currentModule.AppVersion,
                    ExecutableName = currentModule.ExecutableName,
                    IconName = currentModule.IconName,
                    ProductName = currentModule.ProductName,
                    FileDescription = currentModule.FileDescription,
                    InternalName = currentModule.InternalName,
                    OriginalFilename = currentModule.OriginalFilename,
                    CompanyName = currentModule.CompanyName,
                    Comments = currentModule.Comments,
                    FileVersion = currentModule.FileVersion,
                    LastUpdate = DateTime.Now
                });
            }
            else
            {
                module.AppName = currentModule.AppName;
                module.AppVersion = currentModule.AppVersion;
                module.ExecutableName = currentModule.ExecutableName;
                module.IconName = currentModule.IconName;
                module.ProductName = currentModule.ProductName;
                module.FileDescription = currentModule.FileDescription;
                module.InternalName = currentModule.InternalName;
                module.OriginalFilename = currentModule.OriginalFilename;
                module.CompanyName = currentModule.CompanyName;
                module.Comments = currentModule.Comments;
                module.FileVersion = currentModule.FileVersion;
                module.LastUpdate = DateTime.Now;
                Database.UserModules.Update(module);
            }
        }
    }
}
