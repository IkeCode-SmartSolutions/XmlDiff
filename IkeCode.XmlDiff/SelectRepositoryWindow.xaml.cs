using IkeCode.XmlDiff.Helpers;
using IkeCode.XmlDiff.Models;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Octokit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;

namespace IkeCode.XmlDiff
{
    public partial class SelectRepositoryWindow : MetroWindow
    {
        #region Properties

        GitHubClient _client = null;
        bool _authenticated = false;

        private string _bashPath = @"C:\Program Filess (x86)\Git\Bin\sh.exe";
        private string BashPath
        {
            get { return txbBashPath.Text; }
        }

        private TreeViewModel OldGitPath { get; set; }
        private TreeViewModel NewGitPath { get; set; }

        private bool UseExample { get { return cbUseExample.IsChecked.HasValue && cbUseExample.IsChecked.Value; } }

        private bool IsFolder { get { return rbFolder.IsChecked.HasValue && rbFolder.IsChecked.Value; } }

        private bool IncludeSubFolders { get { return cbSubFolders.IsChecked.HasValue && cbSubFolders.IsChecked.Value; } }

        private bool IsFile { get { return rbFiles.IsChecked.HasValue && rbFiles.IsChecked.Value; } }

        private string TempBaseFolder { get { return string.IsNullOrWhiteSpace(txbTempFolder.Text) ? @"C:\DiffConfigTemp\" : txbTempFolder.Text; } }

        #endregion Properties

        private void CreateClient()
        {
            if (_client == null)
            {
                _client = new GitHubClient(new ProductHeaderValue("XMLDiff"));
            }
        }

        public SelectRepositoryWindow()
        {
            InitializeComponent();

            CreateClient();

            loadingPanel.Message = "GitHub";
            loadingPanel.ClosePanelCommand = PanelCloseCommand;

            btnFetch.IsEnabled = false;

            SelectedFolders.IsFolder = IsFolder;
            SelectedFolders.IncludeSubFolders = IncludeSubFolders;
            SelectedFolders.IsFile = IsFile;
            SelectedFolders.UseExample = UseExample;

            txbTempFolder.Text = TempBaseFolder;
            txbBashPath.Text = _bashPath;

            txbUser.Text = GitHubCredentials.GitUser;
            txbPass.Password = GitHubCredentials.GitPassword;

            if (GitHubSelecteds.TagsAndBranches.Count > 0)
            {
                FillComboboxTags(cbOldTag, GitHubSelecteds.TagsAndBranches, CbOldTag_SelectionChanged);
                cbOldTag.SelectedItem = GitHubSelecteds.OldTagSelected;

                FillComboboxTags(cbNewTag, GitHubSelecteds.TagsAndBranches, CbNewTag_SelectionChanged);
                cbNewTag.SelectedItem = GitHubSelecteds.NewTagSelected;

                if (GitHubSelecteds.OldTree.Count > 0)
                {
                    FillTree(treeOld, GitHubSelecteds.OldTree);
                }

                if (GitHubSelecteds.NewTree.Count > 0)
                {
                    FillTree(treeNew, GitHubSelecteds.NewTree);
                }

                btnNext.IsEnabled = true;
            }
        }

        #region Events

        private async void btnAuthGit_Click(object sender, RoutedEventArgs e)
        {
            await AuthenticateGitHub();
        }

        private bool BashPathExists(string path = "", bool checkDefaultLocations = true)
        {
            var result = false;
            path = string.IsNullOrWhiteSpace(path) ? BashPath : path;

            if (checkDefaultLocations)
            {
                if (!BashPathExists(path, false))
                    BashPathExists(@"C:\Program Filess\Git\Bin\sh.exe", false);
            }

            if (File.Exists(path))
            {
                txbBashPath.Text = path;
                return true;
            }

            return result;
        }

        private async Task FindBashAsync(string path = "", bool checkDefaultLocations = true, Action callback = null)
        {
            if (!BashPathExists(path, checkDefaultLocations: checkDefaultLocations))
            {
                var result = await this.ShowInputAsync(@"Ooops...", $@"Não encontrei o arquivo {path} no seu computador, insira o caminho onde ele e encontra antes de continuar.");
                if (result == null)
                {
                    await this.ShowMessageAsync("Ooops...", "Não podemos continuar antes de saber onde se encontra seu bash =/" + Environment.NewLine + "Obs.: Você pode utilizar o menu de configurações e incluir o caminho por lá.", MessageDialogStyle.Affirmative);
                }
                else
                {
                    await FindBashAsync(result, false, callback);
                }
            }
            else
            {
                await this.ShowMessageAsync("Tudo certo!", "Novo caminho -> " + BashPath);

                if (callback != null)
                {
                    callback?.Invoke();
                }
            }
        }

        private async void btnFetch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                loadingPanel.SubMessage = "Carregando Tags...";
                loadingPanel.IsLoading = true;

                CreateClient();
                var gitTags = await _client.Repository.GetAllTags(GitHubCredentials.GitOwner, GitHubCredentials.GitName);

                var tagsList = gitTags.Select(i => new TagsModel { Name = i.Name + " (tag)", OriginalName = i.Name, Commit = i.Commit.Sha, ZipballUrl = i.ZipballUrl }).ToList();

                loadingPanel.SubMessage = "Carregando Branches...";

                var gitBranchs = await _client.Repository.GetAllBranches(GitHubCredentials.GitOwner, GitHubCredentials.GitName);
                var branchesList = gitBranchs.Select(i => new TagsModel { Name = i.Name + " (branch)", OriginalName = i.Name, Commit = i.Commit.Sha }).ToList();

                var tagsAndBranches = new List<TagsModel>();
                tagsAndBranches.AddRange(tagsList);
                tagsAndBranches.AddRange(branchesList);

                GitHubSelecteds.TagsAndBranches = tagsAndBranches;

                FillComboboxTags(cbOldTag, GitHubSelecteds.TagsAndBranches, CbOldTag_SelectionChanged);

                FillComboboxTags(cbNewTag, GitHubSelecteds.TagsAndBranches, CbNewTag_SelectionChanged);

                PanelCloseCommand.Execute(null);

                btnNext.IsEnabled = true;
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("Ocorreu um erro não tratado.", "Ooops...");
            }
        }

        private void btnOldLocalFolder_Click(object sender, RoutedEventArgs e)
        {
            OpenDialog(path =>
            {
                SelectedFolders.FolderOld = path;

                treeOld.ItemsSource = new List<TreeViewModel> { new TreeViewModel { Title = path, FullPath = path } };
            }, SelectedFolders.FolderOld);
        }

        private void btnNewLocalFolder_Click(object sender, RoutedEventArgs e)
        {
            OpenDialog(path =>
            {
                SelectedFolders.FolderNew = path;

                treeNew.ItemsSource = new List<TreeViewModel> { new TreeViewModel { Title = path, FullPath = path } };
            }, SelectedFolders.FolderNew);
        }

        private async void CbNewTag_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GitHubSelecteds.NewTagSelected = (TagsModel)cbNewTag.SelectedItem;
            var newCommit = GitHubSelecteds.NewTagSelected.Commit;
            GitHubSelecteds.NewTree = await GetTree(newCommit);
            FillTree(treeNew, GitHubSelecteds.NewTree);
        }

        private async void CbOldTag_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GitHubSelecteds.OldTagSelected = (TagsModel)cbOldTag.SelectedItem;
            var oldCommit = GitHubSelecteds.OldTagSelected.Commit;
            GitHubSelecteds.OldTree = await GetTree(oldCommit);
            FillTree(treeOld, GitHubSelecteds.OldTree);
        }

        private void btnConfig_Click(object sender, RoutedEventArgs e)
        {
            flyoutConfigs.IsOpen = true;
        }

        private async void btnNext_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var oldSelected = (TreeViewModel)treeOld.SelectedItem;
                var newSelected = (TreeViewModel)treeNew.SelectedItem;

                if (!UseExample && (
                        string.IsNullOrWhiteSpace(SelectedFolders.FolderOld)
                        && (oldSelected == null
                                || (oldSelected != null && string.IsNullOrWhiteSpace(oldSelected.FullPath))))
                    )
                {
                    System.Windows.MessageBox.Show("Selecione o Path da versão mais antiga antes de continuar.", "Ooops...");
                }
                else if (!UseExample && (
                                string.IsNullOrWhiteSpace(SelectedFolders.FolderNew)
                                && (newSelected == null
                                        || (newSelected != null && string.IsNullOrWhiteSpace(newSelected.FullPath))))
                        )
                {
                    System.Windows.MessageBox.Show("Selecione o Path da versão mais nova antes de continuar.", "Ooops...");
                }
                else
                {
                    loadingPanel.SubMessage = "Baixando pastas de config do servidor... (Pode demorar um pouco, seja paciente)";
                    loadingPanel.IsLoading = true;

                    if (!UseExample && GitHubSelecteds.OldTagSelected != null && !string.IsNullOrWhiteSpace(GitHubSelecteds.OldTagSelected.Commit))
                    {
                        var oldTreePath = ((TreeViewModel)treeOld.SelectedItem).FullPath;
                        var parsedOldTreePath = oldTreePath.EndsWith("/") ? oldTreePath : oldTreePath + "/";
                        await ExecuteAsync("old", parsedOldTreePath, GitHubSelecteds.OldTagSelected.OriginalName);

                        var oldPulledConfigPath = System.IO.Path.Combine(TempBaseFolder, "old", parsedOldTreePath.Replace("/", "\\"));
                        SelectedFolders.FolderOld = oldPulledConfigPath;
                    }

                    if (!UseExample && GitHubSelecteds.NewTagSelected != null && !string.IsNullOrWhiteSpace(GitHubSelecteds.NewTagSelected.Commit))
                    {
                        var newTreePath = ((TreeViewModel)treeNew.SelectedItem).FullPath;
                        var parsedNewTreePath = newTreePath.EndsWith("/") ? newTreePath : newTreePath + "/";
                        await ExecuteAsync("new", parsedNewTreePath, GitHubSelecteds.NewTagSelected.OriginalName);

                        var newPulledConfigPath = System.IO.Path.Combine(TempBaseFolder, "new", parsedNewTreePath.Replace("/", "\\"));
                        SelectedFolders.FolderNew = newPulledConfigPath;
                    }

                    SelectedFolders.UseExample = UseExample;
                    SelectedFolders.IsFile = IsFile;
                    SelectedFolders.IsFolder = IsFolder;
                    SelectedFolders.IncludeSubFolders = IncludeSubFolders;

                    var window = new CompareWindow();
                    window.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                var a = ex;
                throw;
            }
        }

        #endregion Events

        #region Methods

        private async Task<List<TreeViewModel>> GetTree(string commit)
        {
            await AuthenticateGitHub();

            loadingPanel.SubMessage = "Carregando Arvore de Pastas... (Pode demorar um pouco dependendo do tamanho do projeto)";
            loadingPanel.IsLoading = true;

            CreateClient();
            var treeResponse = await _client.Git.Tree.GetRecursive(GitHubCredentials.GitOwner, GitHubCredentials.GitName, commit);
            var folders = treeResponse.Tree.Select(i => i.Path).ToList();

            var parsed = new TreeViewModel { Title = "/", Children = new List<TreeViewModel>(), FullPath = "/" };
            BuildTree(folders, parsed);

            PanelCloseCommand.Execute(null);

            return new List<TreeViewModel> { parsed };
        }

        private static void BuildTree(List<string> sourcePaths, TreeViewModel oldParsed, char pathSeparator = '/')
        {
            Action<TreeViewModel, IEnumerable<string>, string> ensureExists = null;
            ensureExists = (ftm, ts, fullPath) =>
            {
                if (ts.Any())
                {
                    var title = ts.First();
                    var child = ftm.Children.SingleOrDefault(x => x.Title == title);
                    if (child == null)
                    {
                        child = new TreeViewModel
                        {
                            Title = title,
                            FullPath = fullPath,
                            Children = new List<TreeViewModel>(),
                        };
                        ftm.Children.Add(child);
                    }
                    ensureExists(child, ts.Skip(1), fullPath);
                }
            };

            foreach (var path in sourcePaths)
            {
                if (path.EndsWith(".cs")
                        //|| path.EndsWith("config")
                        || path.EndsWith(".xml")
                        || path.EndsWith(".cmd")
                        || path.EndsWith(".txt")
                        || path.EndsWith(".ini")
                        || path.EndsWith(".build")
                        || path.EndsWith(".html")
                        || path.EndsWith(".xaml")
                        || path.EndsWith(".gitignore")
                        || path.Contains(".nuget")
                        || path.EndsWith(".sln")
                        || path.EndsWith(".csproj")
                        || path.EndsWith(".suo")
                        || path.EndsWith(".build")
                        || path.EndsWith(".tmp")
                        || path.EndsWith(".json")
                        || path.EndsWith(".vsmdi")
                        || path.EndsWith(".testsettings")
                        || path.EndsWith(".svn")
                        || path.Contains(".git")
                        || path.Contains(".vs")
                        || path.Contains("layout"))
                {
                    continue;
                }

                var separator = new char[] { pathSeparator };
                var parts = path.Split(separator);
                ensureExists(oldParsed, parts, path);
            }
        }

        private async Task AuthenticateGitHub()
        {
            if (!_authenticated)
            {
                if (string.IsNullOrWhiteSpace(txbUrl.Text))
                {
                    loadingPanel.SubMessage = "Por favor, preencha a Url do repositorio do GitHub.";
                    loadingPanel.IsLoading = true;
                }
                else
                {
                    GitHubCredentials.GitUrl = new Uri(txbUrl.Text);
                    GitHubCredentials.GitUser = txbUser.Text;
                    GitHubCredentials.GitPassword = txbPass.Password;

                    if (string.IsNullOrWhiteSpace(GitHubCredentials.GitUser))
                    {
                        loadingPanel.SubMessage = "Por favor, preencha suas credenciais.";
                        loadingPanel.IsLoading = true;
                    }
                    else
                    {
                        try
                        {
                            loadingPanel.SubMessage = "Autenticando usuário do GitHub...";
                            loadingPanel.IsLoading = true;

                            CreateClient();

                            var basicAuth = new Credentials(GitHubCredentials.GitUser, GitHubCredentials.GitPassword);
                            _client.Credentials = basicAuth;

                            //just check with any request
                            await _client.Repository.Get(GitHubCredentials.GitOwner, GitHubCredentials.GitName);

                            btnFetch.IsEnabled = true;

                            loadingPanel.SubMessage = "Autenticação realizada com sucesso!";
                            loadingPanel.IsLoading = false;

                            _authenticated = true;

                        }
                        catch (AuthorizationException)
                        {
                            loadingPanel.SubMessage = "Ocorreu um erro na Autenticação do usuário, por favor revise as credenciais.";
                            loadingPanel.IsLoading = true;
                        }
                    }
                }
            }
        }

        public ICommand PanelCloseCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    // Your code here.
                    // You may want to terminate the running thread etc.
                    loadingPanel.IsLoading = false;
                });
            }
        }

        private async Task ExecuteAsync(string oldOrNew, string configFolder, string tagOrBranchName)
        {
            await FindBashAsync(BashPath, callback: () =>
            {
                var userWithPass = $"{GitHubCredentials.GitUser}:{GitHubCredentials.GitPassword}@";
                var gitUrlWithCreds = GitHubCredentials.GitUrl.ToString().Insert(GitHubCredentials.GitUrl.ToString().IndexOf("github"), userWithPass).TrimEnd('/');
                var clonePath = string.Concat(TempBaseFolder.Replace('\\', '/'), oldOrNew);
                var gitLocation = string.Concat("/", clonePath.Replace(":", string.Empty));

                var bashArgs = $@" --login -i -c ""rm -rf {clonePath} && git clone -n {gitUrlWithCreds}.git {clonePath} && cd {gitLocation} && git config core.sparsecheckout true && echo {configFolder} >> .git/info/sparse-checkout && git checkout {tagOrBranchName}""";

                ExecuteCommand(BashPath, bashArgs);
            });
        }

        public static string ExecuteCommand(string executable, string arguments, bool standardOutput = false, bool standardError = false, bool throwOnError = false)
        {
            var standardOutputString = string.Empty;
            var standardErrorString = string.Empty;

            Process process = null;

            try
            {
#pragma warning disable CC0009 // Use object initializer
                process = new Process();
#pragma warning restore CC0009 // Use object initializer
                process.StartInfo = new ProcessStartInfo(executable, arguments);

                process.StartInfo.UseShellExecute = false;

                if (standardOutput)
                    process.StartInfo.RedirectStandardOutput = true;

                if (standardError || throwOnError)
                    process.StartInfo.RedirectStandardError = true;

                process.Start();

                if (standardError || throwOnError) standardErrorString = process.StandardError.ReadToEnd();

                if (throwOnError && !string.IsNullOrEmpty(standardErrorString))
                    throw new Exception($"Error in ConsoleCommand while executing {executable} with arguments {arguments}.");

                if (standardOutput) standardOutputString = process.StandardOutput.ReadToEnd();

                if (standardError) standardOutputString += standardErrorString;

                process.WaitForExit();
            }
            catch (Exception e)
            {
                throw new Exception($"Error in ConsoleCommand while executing {executable} with arguments {arguments}.", e);
            }
            finally
            {
                if (process != null)
                    process.Dispose();
            }

            return standardOutputString;
        }

        private static void OpenDialog(Action<string> action, string selectedPath = "")
        {
            if (SelectedFolders.IsFolder)
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    fbd.SelectedPath = selectedPath;
                    var dialog = fbd.ShowDialog();
                    if (dialog == System.Windows.Forms.DialogResult.OK)
                    {
                        action.Invoke(fbd.SelectedPath);
                    }
                }
            }
            else if (SelectedFolders.IsFile)
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    var dialog = ofd.ShowDialog();
                    if (dialog == System.Windows.Forms.DialogResult.OK)
                    {
                        action.Invoke(ofd.FileName);
                    }
                }
            }
        }

        private static void FillComboboxTags(System.Windows.Controls.ComboBox cb, IEnumerable source, SelectionChangedEventHandler changedEvent)
        {
            cb.SelectedValuePath = "Name";
            cb.DisplayMemberPath = "Name";
            cb.ItemsSource = null;
            cb.ItemsSource = source;
            cb.SelectionChanged += changedEvent;
        }

        private static void FillTree(System.Windows.Controls.TreeView tree, IEnumerable source)
        {
            tree.ItemsSource = null;
            tree.ItemsSource = source;
        }

        #endregion Methods
    }
}
