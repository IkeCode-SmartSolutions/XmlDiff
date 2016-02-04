using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;

namespace IkeCode.XmlDiff
{
    public partial class CompareWindow : MetroWindow
    {
        public List<XmlDifferences> Diffs { get; set; }

        private string RootFolder { get; set; }

        private string ExampleXmlsPath { get; set; }

        private string Sprint
        {
            get { return string.IsNullOrWhiteSpace(txbSprint.Text) ? "0" : txbSprint.Text; }
        }

        public CompareWindow()
        {
            InitializeComponent();

            Diffs = new List<XmlDifferences>();

            RootFolder = Directory.GetParent(System.Windows.Forms.Application.StartupPath).Parent.FullName;
            ExampleXmlsPath = System.IO.Path.Combine(RootFolder, "example");

            if (SelectedFolders.UseExample)
            {
                if (SelectedFolders.IsFile)
                {
                    SelectedFolders.FolderOld = System.IO.Path.Combine(ExampleXmlsPath, @"old\_gvp.main\MibMainToCompare.mibconfig");
                    SelectedFolders.FolderNew = System.IO.Path.Combine(ExampleXmlsPath, @"new\_gvp.main\MibMainToCompare.mibconfig");
                }
                else if (SelectedFolders.IsFolder)
                {
                    if (SelectedFolders.IncludeSubFolders)
                    {
                        SelectedFolders.FolderOld = System.IO.Path.Combine(ExampleXmlsPath, @"old\");
                        SelectedFolders.FolderNew = System.IO.Path.Combine(ExampleXmlsPath, @"new\");
                    }
                    else
                    {
                        SelectedFolders.FolderOld = System.IO.Path.Combine(ExampleXmlsPath, @"old\_gvp.main\");
                        SelectedFolders.FolderNew = System.IO.Path.Combine(ExampleXmlsPath, @"new\_gvp.main\");
                    }
                }
            }

            txbFolder1.Text = SelectedFolders.FolderOld;
            txbFolder2.Text = SelectedFolders.FolderNew;
        }

        private void btnCompare_Click(object sender, RoutedEventArgs e)
        {
            Diffs = new List<XmlDifferences>();

            var pathOld = string.Empty;
            var pathNew = string.Empty;

            if (SelectedFolders.IsFile)
            {

                pathOld = SelectedFolders.FolderOld;
                pathNew = SelectedFolders.FolderNew;

                CompareFiles(pathOld, pathNew);
            }
            else if (SelectedFolders.IsFolder)
            {
                if (SelectedFolders.IncludeSubFolders)
                {
                    pathOld = SelectedFolders.FolderOld;
                    pathNew = SelectedFolders.FolderNew;

                    CompareFolderStructure(pathOld, pathNew, CompareAction.remove);
                    CompareFolderStructure(pathNew, pathOld, CompareAction.add);
                }
                else
                {
                    pathOld = SelectedFolders.FolderOld;
                    pathNew = SelectedFolders.FolderNew;

                    AddOrRemoveFile(pathOld, pathNew, CompareAction.remove);
                    AddOrRemoveFile(pathNew, pathOld, CompareAction.add);

                    CompareFolderFiles(pathOld, pathNew);
                }
            }

            grid.ItemsSource = null;
            Diffs = Diffs.Distinct().OrderBy(i => i.Component).ToList();
            grid.ItemsSource = Diffs;
        }

        private void CompareFolderFiles(string pathOld, string pathNew)
        {
            var oldFiles = Directory.GetFiles(pathOld).ToList();
            foreach (var oldFile in oldFiles)
            {
                var fileName = GetFileName(oldFile);
                var newFile = Directory.GetFiles(pathNew, fileName).ToList().FirstOrDefault();
                CompareFiles(oldFile, newFile, false, false);
            }
        }

        private void CompareFiles(string filePathOld, string filePathNew, bool throwMessage = true, bool checkFile = true)
        {
            var xmlOldElements = new List<XElement>(0);
            var xmlNewElements = new List<XElement>(0);

            var fileNameWithoutExtension = string.Empty;

            var fileName = GetFileName(filePathOld);
            var component = GetComponent(filePathOld);

            if (File.Exists(filePathOld))
            {
                //todo: check if file is a valid xml
                var xml = !string.IsNullOrWhiteSpace(filePathOld) ? XDocument.Load(filePathOld) : new XDocument(new XDeclaration("1.0", "UTF-8", "yes"));
                xmlOldElements = xml.Root != null ? xml.Root.Elements().ToList() : new List<XElement>(0);

                fileName = GetFileName(filePathOld);
                component = GetComponent(filePathOld);

                fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(filePathOld);

                if (checkFile && !File.Exists(filePathNew))
                {
                    Diffs.Add(new XmlDifferences
                    {
                        Action = "remove config file",
                        Component = component,
                        Config = fileName,
                        Sprint = Sprint
                    });

                    AllKeys(xmlOldElements, CompareAction.remove, component, fileName);
                }
            }

            if (File.Exists(filePathNew))
            {
                //todo: check if file is a valid xml
                var xml = !string.IsNullOrWhiteSpace(filePathNew) ? XDocument.Load(filePathNew) : new XDocument(new XDeclaration("1.0", "UTF-8", "yes"));
                xmlNewElements = xml.Root != null ? xml.Root.Elements().ToList() : new List<XElement>(0);

                fileName = GetFileName(filePathNew);
                component = GetComponent(filePathNew);

                fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(filePathNew);

                if (!File.Exists(filePathOld))
                {
                    if (checkFile)
                    {
                        Diffs.Add(new XmlDifferences
                        {
                            Action = "add config file",
                            Component = component,
                            Config = fileName,
                            Sprint = Sprint
                        });
                    }

                    AllKeys(xmlNewElements, CompareAction.add, component, fileName);
                }
            }

            if (!File.Exists(filePathOld) && !File.Exists(filePathNew) && throwMessage)
            {
                var inexistentFile = filePathOld.Split(System.IO.Path.DirectorySeparatorChar).LastOrDefault();
                System.Windows.Forms.MessageBox.Show(@"Impossivel fazer a comparação, nenhum dos arquivos escolhidos existe!"
                                                + Environment.NewLine +
                                                @"Arquivo: " + inexistentFile, "Ooops!");
            }

            if (File.Exists(filePathOld) && File.Exists(filePathNew))
            {
                CompareKeys(xmlOldElements, xmlNewElements, CompareAction.remove, component, fileName);
                CompareKeys(xmlNewElements, xmlOldElements, CompareAction.add, component, fileName, true);
            }
        }

        private void CompareKeys(List<XElement> xmlOldElements, List<XElement> xmlNewElements, CompareAction action, string component, string fileName, bool includeAllLevels = false)
        {
            foreach (var oldElement in xmlOldElements)
            {
                var xmlNewElement = (from xmlNew in xmlNewElements
                                     where xmlNew.Name.LocalName == oldElement.Name.LocalName
                                     select xmlNew).FirstOrDefault();

                if (xmlNewElement == null)
                {
                    Keys(oldElement, action, component, fileName, includeAllLevels);
                }
                else
                {
                    CompareKeys(oldElement.Elements().ToList(), xmlNewElement.Elements().ToList(), action, component, fileName, includeAllLevels);
                }
            }
        }

        private void AllKeys(string xmlPath, CompareAction action, string component, string fileName, bool includeAllLevels = true)
        {
            //todo: check if file is a valid xml
            var xml = !string.IsNullOrWhiteSpace(xmlPath) ? XDocument.Load(xmlPath) : new XDocument(new XDeclaration("1.0", "UTF-8", "yes"));
            var xmlElements = xml.Root != null ? xml.Root.Elements().ToList() : new List<XElement>(0);

            if (xmlElements.Count > 0)
            {
                foreach (var element in xmlElements)
                {
                    Keys(element, action, component, fileName, includeAllLevels);
                }
            }
        }

        private void AllKeys(List<XElement> elements, CompareAction action, string component, string fileName, bool includeAllLevels = true)
        {
            if (elements.Count > 0)
            {
                foreach (var element in elements)
                {
                    Keys(element, action, component, fileName, includeAllLevels);
                }
            }
        }

        private void Keys(XElement element, CompareAction action, string component, string fileName, bool includeAllLevels = true)
        {
            var ancestors = element.AncestorsAndSelf().Select(i => i.Name.LocalName).Reverse().ToList();
            var xpath = string.Concat("\\\\", string.Join("\\", ancestors));

            var parsedAction = element.HasElements ? string.Format("{0} section", action.ToString()) : string.Format("{0} key", action.ToString());

            Diffs.Add(new XmlDifferences
            {
                Action = parsedAction,
                Component = component,
                Config = fileName,
                Sprint = Sprint,
                XPath = xpath
            });

            if (element.HasElements && includeAllLevels)
            {
                AllKeys(element.Elements().ToList(), action, component, fileName, includeAllLevels);
            }
        }

        private void CompareFolderStructure(string path1, string path2, CompareAction action)
        {
            var path1Directories = Directory.GetDirectories(path1).ToList();
            var path2Directories = Directory.GetDirectories(path2).ToList();

            foreach (var path1Dir in path1Directories)
            {
                var path1Name = path1Dir.Split(System.IO.Path.DirectorySeparatorChar).LastOrDefault();

                var path2Dir = path2Directories.FirstOrDefault(i => i.EndsWith(path1Name));
                var path2FolderNames = path2Directories.Select(i => i.Split(System.IO.Path.DirectorySeparatorChar).LastOrDefault()).ToList();
                var path2Name = path2FolderNames.FirstOrDefault(i => i == path1Name);

                if (path2Name == null)
                {
                    var parsedAction = string.Format("{0} folder", action.ToString());

                    Diffs.Add(new XmlDifferences
                    {
                        Action = parsedAction,
                        Component = path1Name,
                        Sprint = Sprint
                    });

                    if (action == CompareAction.add)
                    {
                        AddAllFiles(path1Dir);
                    }
                }
                else
                {
                    AddOrRemoveFile(path1Dir, path2Dir, action);
                }
            }
        }

        private static string GetComponent(string filePath)
        {
            return System.IO.Path.GetDirectoryName(filePath).Split(System.IO.Path.DirectorySeparatorChar).LastOrDefault();
        }

        private static string GetFileName(string filePath)
        {
            return System.IO.Path.GetFileName(filePath);
        }

        private void AddOrRemoveFile(string path1, string path2, CompareAction action)
        {
            var path1Files = Directory.GetFiles(path1).ToList();
            foreach (var path1File in path1Files)
            {
                var componentName = System.IO.Path.GetDirectoryName(path1File).Split(System.IO.Path.DirectorySeparatorChar).LastOrDefault();
                var fileName = System.IO.Path.GetFileName(path1File);

                var path2File = Directory.GetFiles(path2, System.IO.Path.GetFileName(path1File)).FirstOrDefault();
                var actionParsed = string.Format("{0} config file", action.ToString());

                if (path2File == null)
                {
                    var xmlDiff = new XmlDifferences
                    {
                        Action = actionParsed,
                        Component = componentName,
                        Config = fileName,
                        Sprint = txbSprint.Text
                    };

                    if (action == CompareAction.add)
                    {
                        //todo: check if file is a valid xml
                        xmlDiff.XmlExample = XDocument.Load(path1File).ToString();
                    }

                    Diffs.Add(xmlDiff);

                    if (action == CompareAction.add)
                    {
                        AllKeys(path1File, action, componentName, fileName);
                    }
                }
            }
        }

        private void AddAllFiles(string path)
        {
            var files = Directory.GetFiles(path).ToList();
            foreach (var file in files)
            {
                var componentName = System.IO.Path.GetDirectoryName(file).Split(System.IO.Path.DirectorySeparatorChar).LastOrDefault();
                var fileName = System.IO.Path.GetFileName(file);
                var actionParsed = string.Format("{0} config file", CompareAction.add.ToString());

                //todo: check if file is a valid xml
                var xmlExample = XDocument.Load(file).ToString();

                Diffs.Add(new XmlDifferences
                {
                    Action = actionParsed,
                    Component = componentName,
                    Config = fileName,
                    Sprint = txbSprint.Text,
                    XmlExample = xmlExample
                });

                AllKeys(file, CompareAction.add, componentName, fileName);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            var window = new SelectRepositoryWindow();
            window.Show();

            this.Close();
        }
    }
}
