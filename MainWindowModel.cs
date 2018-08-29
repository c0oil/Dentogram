﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Dentogram.Clustering;

namespace Dentogram
{
    public class MainWindowModel : ViewModelBase
    {
        private int fileIndex;
        private List<string> files;
        private List<string> textes;
        private List<string> parsedRegions;
        private List<string> dataSets;

        private List<int> shindels;
        public List<int> Shindels
        {
            get { return shindels; }
            set
            {
                shindels = value;
                OnPropertyChanged(nameof(Shindels));
            }
        }

        private int activeShindel;
        public int ActiveShindel
        {
            get { return activeShindel; }
            set
            {
                activeShindel = value;
                OnPropertyChanged(nameof(ActiveShindel));

                ClusterDistance.Shindel = value;
            }
        }

        private List<ClusterDistance.Strategy> strateges;
        public List<ClusterDistance.Strategy> Strateges
        {
            get { return strateges; }
            set
            {
                strateges = value;
                OnPropertyChanged(nameof(Strateges));
            }
        }

        private ClusterDistance.Strategy activeStratege;
        public ClusterDistance.Strategy ActiveStratege
        {
            get { return activeStratege; }
            set
            {
                activeStratege = value;
                OnPropertyChanged(nameof(ActiveStratege));
            }
        }

        private List<string> modes;
        public List<string> Modes
        {
            get { return modes; }
            set
            {
                modes = value;
                OnPropertyChanged(nameof(Modes));
            }
        }

        private string activeMode;
        public string ActiveMode
        {
            get { return activeMode; }
            set
            {
                activeMode = value;
                OnPropertyChanged(nameof(ActiveMode));

                ClusterDistance.Mode = value;
            }
        }

        private Node selectedNode;
        public Node SelectedNode
        {
            get { return selectedNode; }
            set
            {
                if (selectedNode == value)
                {
                    return;
                }
                selectedNode = value;

                if (IsCheckedText1)
                {
                    Header1 = SelectedNode.Name;
                    ClusterText1 = SelectedNode.Text;
                    Text1 = LoadFile(SelectedNode.Name);
                }
                else if (IsCheckedText2)
                {
                    Header2 = SelectedNode.Name;
                    ClusterText2 = SelectedNode.Text;
                    Text2 = LoadFile(SelectedNode.Name);
                }
            }
        }

        private bool isCheckedText1 = true;
        public bool IsCheckedText1
        {
            get { return isCheckedText1; }
            set
            {
                if (value == isCheckedText1)
                {
                    return;
                }
                isCheckedText1 = value;
                OnPropertyChanged(nameof(IsCheckedText1));

                IsCheckedText2 = !value;
            }
        }

        private bool isCheckedText2;
        public bool IsCheckedText2
        {
            get { return isCheckedText2; }
            set
            {
                if (value == isCheckedText2)
                {
                    return;
                }
                isCheckedText2 = value;
                OnPropertyChanged(nameof(IsCheckedText2));

                IsCheckedText1 = !value;
            }
        }

        private List<Node> items;
        public List<Node> Items
        {
            get { return items; }
            set
            {
                items = value;
                OnPropertyChanged(nameof(Items));
            }
        }

        private string header1;
        public string Header1
        {
            get { return header1; }
            set
            {
                header1 = value;
                OnPropertyChanged(nameof(Header1));
            }
        }

        private string header2;
        public string Header2
        {
            get { return header2; }
            set
            {
                header2 = value;
                OnPropertyChanged(nameof(Header2));
            }
        }

        private string clusterText1;
        public string ClusterText1
        {
            get { return clusterText1; }
            set
            {
                clusterText1 = value;
                OnPropertyChanged(nameof(ClusterText1));
            }
        }

        private string clusterText2;
        public string ClusterText2
        {
            get { return clusterText2; }
            set
            {
                clusterText2 = value;
                OnPropertyChanged(nameof(ClusterText2));
            }
        }

        private string text1;
        public string Text1
        {
            get { return text1; }
            set
            {
                text1 = value;
                OnPropertyChanged(nameof(Text1));
            }
        }
        
        private string text2;
        public string Text2
        {
            get { return text2; }
            set
            {
                text2 = value;
                OnPropertyChanged(nameof(Text2));
            }
        }

        private string filesDescription;
        public string FilesDescription
        {
            get { return filesDescription; }
            set
            {
                filesDescription = value;
                OnPropertyChanged(nameof(FilesDescription));
            }
        }

        public MainWindowModel()
        {
            Modes = new List<string>(ClusterDistance.AllModes);
            ActiveMode = "JaccardDistance";

            Strateges = new List<ClusterDistance.Strategy>()
            {
                ClusterDistance.Strategy.MinLinkage,
                ClusterDistance.Strategy.MaxLinkage,
                ClusterDistance.Strategy.AverageLinkage,
                ClusterDistance.Strategy.AverageLinkageWeighted,
            };
            ActiveStratege = ClusterDistance.Strategy.AverageLinkage;

            var t = new List<int>();
            for (int i = 1; i < 40; i++)
            {
                t.Add(i);
            }

            Shindels = t;
            ActiveShindel = 5;

            FilesDescription = "Wait. Loading files...";

            Task.Run(() =>
            {
                LoadFiles();
                Start();
            });
        }
        
        /*
        private HtmlParser htmlParser = new HtmlParser();
        private string LoadFile(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open))
            {
                using (var gZipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                {
                    using (IHtmlDocument document = htmlParser.Parse(gZipStream)) //performance: 34%
                    {
                        var elements = document.All.OfType<IText>();
                        string text = elements.Any()
                            ? string.Join(Environment.NewLine, elements.Select(x => x.Text))
                            : document.All.FirstOrDefault(x => x.LocalName == "text")?.TextContent;
                        if (document.All.Where(x => x.LocalName == "type")
                            .Any(x => x.TextContent.StartsWith("GRAPHIC")))
                        {
                            return string.Empty;
                        }
                        
                        return text;
                    }
                }
            }
        }
        */

        private string LoadFile(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open))
            {
                using (var gZipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                {
                    using (var reader = new StreamReader(gZipStream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        private void LoadFiles()
        {
            try
            {


            ParsingText parcer = new ParsingText();
            var fileInfos = GetFiles().
                Zip(GetTextes(), (fileName, text) => new { fileName, text, parsedResult = parcer.ParseTableNew(text) }).
                Take(2000).
                Where(x => File.Exists(x.fileName)).
                ToList();
            
            
            var parced = fileInfos.
                Where(x => 
                    !string.IsNullOrEmpty(x.text) && 
                    !string.IsNullOrEmpty(x.parsedResult.NamePersonPrefix)).
                ToList();

            var notParced = fileInfos.
                Where(x => 
                    !string.IsNullOrEmpty(x.text) && 
                    string.IsNullOrEmpty(x.parsedResult.NamePersonPrefix)).
                ToList();

                /*
            notParced = notParced.
                Select(x => new { x.fileName, x.text, parsedResult = parcer.ParseTable(x.text) }).
                Where(x => 
                    !string.IsNullOrEmpty(x.text) && 
                    !string.IsNullOrEmpty(x.parsedResult.NamePersonPrefix)).
                ToList();
                */


            HashSet<string> namePersonHash= new HashSet<string>(parced.Select(x => x.parsedResult.NamePersonPrefix));
            HashSet<string> names= new HashSet<string>(parced.Select(x => x.parsedResult.NamePersonValue));
            //return;


            textes = notParced.Select(x => x.text).ToList();
            files = notParced.Select(x => x.fileName).ToList();
            //parsedRegions = notParced.Select(x => x.parsedResult.Region).ToList();
            //dataSets = notParced.Select(x => parcer.TrimForClustering(x.parsedResult.Region)).ToList();
            parsedRegions = notParced.Select(x => x.text).ToList();
            dataSets = notParced.Select(x => parcer.TrimForClustering(x.text)).ToList();

            FilesDescription = $"All files: {fileInfos.Count}; Not parced files: {notParced.Count}";

            }
            catch (Exception e)
            {
                Debug.Fail("LoadFiles");
                Console.WriteLine(e);
                throw;
            }
        }

        /*
        public void LoadFiles()
        {
            ParsingText parcer = new ParsingText();
            var fileInfos = GetFiles().
                Zip(GetTextes(), (fileName, text) => new { fileName, text, parsedResult = parcer.ParseTable(text) }).
                Take(4000).
                Where(x => File.Exists(x.fileName)).
                ToList();
            
            var notParced = fileInfos.
                Where(x => 
                    !string.IsNullOrEmpty(x.text) && 
                    !string.IsNullOrEmpty(x.parsedResult.NamePersonValue) && 
                    string.IsNullOrEmpty(x.parsedResult.AggregatedAmountValue)).
                ToList();
            
            var parced = fileInfos.
                Where(x => 
                    !string.IsNullOrEmpty(x.text) && 
                    !string.IsNullOrEmpty(x.parsedResult.NamePersonValue) && 
                    !string.IsNullOrEmpty(x.parsedResult.AggregatedAmountValue)).
                ToList();

            HashSet<string> namePersonHash= new HashSet<string>(parced.Select(x => x.parsedResult.NamePersonPrefix));
            //HashSet<string> aggregatedAmountHash= new HashSet<string>(parced.Select(x => x.parsedResult.AggregatedAmountPrefix));

            //var fileInfos1 = notParced.Select(x => new { x.fileName, x.text, parsedText = parcer.ParseTableTest(x.text) }).Where(x => !string.IsNullOrEmpty(x.parsedText[0])).ToList();

            //var tt = notParced.Select(x => x.text).ToArray();
            //var t = notParced.Select(x => x.fileName).ToArray();
            
            textes = notParced.Select(x => x.text).ToList();
            files = notParced.Select(x => x.fileName).ToList();
            //dataSets = notParced.Select(x => parcer.TrimForClustering(x.text)).ToList();
            parsedRegions = notParced.Select(x => x.parsedResult.Region).ToList();
            dataSets = notParced.Select(x => parcer.TrimForClustering(x.parsedResult.Region)).ToList();

            FilesDescription = $"All files: {fileInfos.Count}; Not parced files: {notParced.Count}";
        }
        */

        public void Start()
        {

            if (textes.Count < 3)
            {
                return;
            }

            ClusteringTreeModel clusteringModel = new ClusteringTreeModel(dataSets, parsedRegions, textes, files);
            ClusterNodeCollection clusters = clusteringModel.ExecuteClustering(ActiveStratege, 1);
            
            Items = new List<Node> { BuildRootNode(clusters.FirstOrDefault()) };
        }

        /*
        public void StartDouble()
        {
            Random rand = new Random();
            List<double> dataSets = new List<double>();

            for (int i = 0; i < 100; i++)
            {
                dataSets.Add(rand.NextDouble() % 30);
            }

            ClusteringModel clusteringModel = new ClusteringModel(dataSets);
            Clusters clusters = clusteringModel.ExecuteClustering(ClusterDistance.Strategy.AverageLinkageWPGMA, 1);
            
            Items = new List<Node> { BuildRootNode(clusters.FirstOrDefault()) };
        }
        */

        private Node BuildRootNode(ClusterNode cluster)
        {
            Node child0 = null;
            Node child1 = null;
            
            if (cluster.NodesCount == 0)
            {
                if (cluster.LeafsCount == 1)
                {
                    return GetNodeFromCluster(cluster.Leafs[0]);
                }
                if (cluster.LeafsCount == 2)
                {
                    child0 = GetNodeFromCluster(cluster.Leafs[0]);
                    child1 = GetNodeFromCluster(cluster.Leafs[1]);
                }
            }
            else if (cluster.NodesCount == 1)
            {
                child0 = GetNodeFromCluster(cluster.Leafs[0]);
                child1 = BuildRootNode(cluster.Nodes[0]);
            }
            else
            {
                child0 = BuildRootNode(cluster.Nodes[0]);
                child1 = BuildRootNode(cluster.Nodes[1]);
            }

            return Create(child0, child1, cluster.Distance.ToString("F"));
        }

        private Node GetNodeFromCluster(ClusterLeaf pattern)
        {
            return new Node(pattern.FileName) { Text = pattern.Region };
        }

        private Node Create(Node child0, Node child1, string name)
        {
            return new Node(child0, child1) { Name = name };
        }

        /*
        public void WriteTextes()
        {
            HtmlParser htmlParser = new HtmlParser();
            List<string> toWrite = new List<string>();
            
            foreach (string path in GetFiles())
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                using (var fileStream = new FileStream(path, FileMode.Open))
                {
                    using (var gZipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                    {
                        using (MemoryStream buffStrm = new MemoryStream())
                        {
                            gZipStream.CopyTo(buffStrm);
                            buffStrm.Position = 0;

                            using (IHtmlDocument document = htmlParser.Parse(buffStrm)) //performance: 34%
                            {
                                //performance: 8.7%
                                var elements = document.All.OfType<IText>();
                                string text = elements.Any()
                                    ? string.Join(Environment.NewLine, elements.Select(x => x.Text))
                                    : document.All.FirstOrDefault(x => x.LocalName == "text")?.TextContent;
                                if (document.All.Where(x => x.LocalName == "type")
                                    .Any(x => x.TextContent.StartsWith("GRAPHIC")))
                                {
                                    toWrite.Add(string.Empty);
                                    continue;
                                }

                                string trimText = Regex.Replace(text.ToUpperInvariant(), @"\s+", " ");

                                toWrite.Add(trimText);
                            }
                        }
                    }
                }

            }
            File.WriteAllLines(@"E:\SecDaily\textes.txt", toWrite);
        }
        */

        public static IEnumerable<string> GetFiles()
        {
            using (var fileStream = new FileStream(FileManager.FileNames, FileMode.Open))
            {
                using (var reader = new StreamReader(fileStream))
                {
                    string line = reader.ReadLine();
                    while (line != null)
                    {
                        //yield return string.IsNullOrEmpty(line) ? string.Empty: "d" + line.Substring(1);
                        yield return line;
                        line = reader.ReadLine();
                    }
                }
            }
        }

        public IEnumerable<string> GetTextes()
        {
            fileIndex = 1;
            ParsingText parcer = new ParsingText();
            using (var fileStream = new FileStream(FileManager.FileTextes, FileMode.Open))
            {
                using (var reader = new StreamReader(fileStream))
                {
                    string line = parcer.TrimForParsing(reader.ReadLine());
                    while (line != null)
                    {
                        fileIndex++;
                        yield return line;
                        if (fileIndex % 100 == 0)
                        {
                            FilesDescription = $"Loading files[{fileIndex}]...";
                        }
                        line = parcer.TrimForParsing(reader.ReadLine());
                    }
                }
            }
        }

        /*
        public static List<string> GetTextes(List<string> files, out List<string> correctedFiles)
        {
            ParsingText parser = new ParsingText();

            var resultFiles = new List<string>();

            List<string> textes = new List<string>();
            HtmlParser htmlParser = new HtmlParser();
            foreach (string path in files)
            {
                using (var fileStream = new FileStream(path, FileMode.Open))
                {
                    using (var gZipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                    {
                        using (MemoryStream buffStrm = new MemoryStream())
                        {
                            gZipStream.CopyTo(buffStrm);
                            buffStrm.Position = 0;
                            
                            using (IHtmlDocument document = htmlParser.Parse(buffStrm)) //performance: 34%
                            {
                                //performance: 8.7%
                                var elements = document.All.OfType<IText>();
                                string text = elements.Any() 
                                    ? string.Join(Environment.NewLine, elements.Select(x => x.Text)) 
                                    : document.All.FirstOrDefault(x => x.LocalName == "text")?.TextContent;
                                if (document.All.Where(x => x.LocalName == "type").Any(x => x.TextContent.StartsWith("GRAPHIC")))
                                {
                                    continue;
                                }

                                resultFiles.Add(path);
                                string trimText = Regex.Replace(text.ToUpperInvariant(), @"\s+", " ");
                                string parsedText = parser.ParseTable(trimText);
                                textes.Add(parsedText);
                            }
                        }
                    }
                }
            }

            correctedFiles = resultFiles;
            return textes;
        }
        */
    }
    
    public class Node : ViewModelBase
    {
        private string name;
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        } 

        public string Text { get; set; }

        public List<Node> Children { get; set; }

        public Node()
        {
            Name = String.Empty;
            Children = new List<Node>();
        }

        public Node(string name)
        {
            Name = name;
            Children = new List<Node>();
        }

        public Node(Node child0, Node child1)
        {
            Name = String.Empty;

            List<Node> list = new List<Node>();
            if (child0 != null)
            {
                list.Add(child0);
            }
            if (child1 != null)
            {
                list.Add(child1);
            }

            Children = list;
        }
    }

    public class WebBrowserBehavior
    {
        public static readonly DependencyProperty BodyProperty =
            DependencyProperty.RegisterAttached("Body", typeof(string), typeof(WebBrowserBehavior),
                new PropertyMetadata(OnChanged));

        public static string GetBody(DependencyObject dependencyObject)
        {
            return (string)dependencyObject.GetValue(BodyProperty);
        }

        public static void SetBody(DependencyObject dependencyObject, string body)
        {
            dependencyObject.SetValue(BodyProperty, body);
        }

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            WebBrowser wb = (WebBrowser)d;
            wb.NavigateToString((string)e.NewValue);}
    }
}
