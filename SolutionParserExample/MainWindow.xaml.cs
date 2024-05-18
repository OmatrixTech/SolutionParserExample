using EnvDTE80;
using Microsoft.Build.Construction;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using Window = System.Windows.Window;

namespace SolutionParserExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
       
        Type? s_SolutionParserFromSolutionFile;
        PropertyInfo? s_SolutionParserReader;
        MethodInfo? s_SolutionParserSolution;
        PropertyInfo? s_SolutionParserProjects;
        public List<ProjectSolution> ProjectsList { get;  set; }
        ProjectInSolution[] arrayOfProjects = null;
        
        //Change below path with your solution file
        string solutionFilePath = @"C:\Users\sanjay\Desktop\MediaElementSampleProject (3)\MediaElementSampleProject\MediaElementSampleProject.sln";
        private ObservableCollection<ProjectDetail> _ProjectsListWithoutDTE;

        public ObservableCollection<ProjectDetail> ProjectsListWithoutDTE
        {
            get { return _ProjectsListWithoutDTE; }
            set { _ProjectsListWithoutDTE = value; }
        }

        private ObservableCollection<ProjectDetail> _ProjectsListWithDTE;

        public ObservableCollection<ProjectDetail> ProjectsListWithDTE
        {
            get { return _ProjectsListWithDTE; }
            set { _ProjectsListWithDTE = value; }
        }

        private ObservableCollection<ProjectDetail> _ProjectsListThirdApproachExample;

        public ObservableCollection<ProjectDetail> ProjectsListThirdApproachExample
        {
            get { return _ProjectsListThirdApproachExample; }
            set { _ProjectsListThirdApproachExample = value; }
        }

        public MainWindow()
        {
            InitializeComponent();
            GetProjectDetailsWithDTE();//First Approch
            GetProjectDetailsWithoutDTE();//Second Approach
            GetProjectInformationByThirdApproach();//Third Approach
            DataridWithoutDTE.ItemsSource = ProjectsListWithoutDTE;
            DataridWithDTE.ItemsSource = ProjectsListWithDTE;
            DataridThirdApproach.ItemsSource= ProjectsListThirdApproachExample;
        }
        private void GetProjectInformationByThirdApproach()
        {
            // Load the solution file
            SolutionFile solutionFile = SolutionFile.Parse(solutionFilePath);
            ProjectsListThirdApproachExample = new ObservableCollection<ProjectDetail>();
            // Iterate through each project in the solution
            foreach (var project in solutionFile.ProjectsInOrder)
            {
                string projectName = project.ProjectName;
                string projectRelativePath = project.RelativePath;
                string projectFullPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(solutionFilePath), projectRelativePath);
                string projectGuid = project.ProjectGuid;
                string projectType=project.ProjectType.ToString();
                ProjectsListThirdApproachExample.Add(new ProjectDetail
                {
                    ProjectName = projectName,
                    ProjectGuid = projectGuid,
                    ProjectType= projectType,
                    RelativePath= projectRelativePath,
                });
            }
        }
       private string GetRelativePath(string basePath, string fullPath)
        {
            Uri baseUri = new Uri(basePath + Path.DirectorySeparatorChar);
            Uri fullUri = new Uri(fullPath);
            Uri relativeUri = baseUri.MakeRelativeUri(fullUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }
        private void GetProjectDetailsWithDTE()
        {
            string solutionDirectory = Path.GetDirectoryName(solutionFilePath);
            var dte = GetDTE();
            dte.Solution.Open(solutionFilePath);
            ProjectsListWithDTE = new ObservableCollection<ProjectDetail>();
            foreach (EnvDTE.Project project in dte.Solution.Projects)
            {
                
                ProjectsListWithDTE.Add(new ProjectDetail
                {
                    ProjectName = project.Name,
                    RelativePath = GetRelativePath(solutionDirectory, project.FullName),
                });
            }
            dte.Solution.Close(false);
        }

        private DTE2 GetDTE()
        {
            Type dteType = Type.GetTypeFromProgID("VisualStudio.DTE.17.0", true);
            object obj = Activator.CreateInstance(dteType, true);
            return (DTE2)obj;
        }
        private void GetProjectDetailsWithoutDTE()
        {
            s_SolutionParserFromSolutionFile = Type.GetType("Microsoft.Build.Construction.SolutionFile, Microsoft.Build, Version=15.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", false, true);//@SJ
            if (s_SolutionParserFromSolutionFile != null)
            {
                s_SolutionParserReader = s_SolutionParserFromSolutionFile.GetProperty("SolutionReader", BindingFlags.NonPublic | BindingFlags.Instance);
                s_SolutionParserProjects = s_SolutionParserFromSolutionFile.GetProperty("ProjectsInOrder", BindingFlags.Public | BindingFlags.Instance);//@SJ
                s_SolutionParserSolution = s_SolutionParserFromSolutionFile.GetMethod("ParseSolution", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            if (s_SolutionParserFromSolutionFile != null)
            {
                var solutionResultParser = s_SolutionParserFromSolutionFile.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).First().Invoke(null);
                using (var streamReader = new StreamReader(solutionFilePath))
                {
                    s_SolutionParserReader.SetValue(solutionResultParser, streamReader, null);
                    s_SolutionParserSolution.Invoke(solutionResultParser, null);
                }

                var projects = new List<ProjectSolution>();
                var projectsobj= s_SolutionParserProjects.GetValue(solutionResultParser, null) as IReadOnlyList<ProjectInSolution>;

                if (projectsobj != null)
                {
                    arrayOfProjects = projectsobj.ToArray();
                }
                for (int i = 0; i < arrayOfProjects.Length; i++)
                {
                    projects.Add(new ProjectSolution(arrayOfProjects.GetValue(i)));
                }

                this.ProjectsList = projects;
                ProjectsListWithoutDTE = new ObservableCollection<ProjectDetail>();
                foreach (var item in ProjectsList)
                {
                    ProjectsListWithoutDTE.Add(new ProjectDetail { 
                    ProjectName= item.ProjectName,
                    ProjectGuid= item.ProjectGuid,
                    ProjectType= item.ProjectType,
                    RelativePath= item.RelativePath,
                    });
                }
            }
        }
    }

    public class ProjectSolution
    {
        static readonly PropertyInfo? s_ProjectName;
        static readonly PropertyInfo? s_RelativePath;
        static readonly PropertyInfo? s_ProjectGuid;
        static readonly PropertyInfo? s_ProjectType;

        static ProjectSolution()
        {
            Type? ProjectInSolution = Type.GetType("Microsoft.Build.Construction.ProjectInSolution, Microsoft.Build, Version=15.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", false, true);//@SJ
            if (ProjectInSolution != null)
            {
                s_ProjectName = ProjectInSolution.GetProperty("ProjectName", BindingFlags.Public | BindingFlags.Instance);
                s_RelativePath = ProjectInSolution.GetProperty("RelativePath", BindingFlags.Public | BindingFlags.Instance);
                s_ProjectGuid = ProjectInSolution.GetProperty("ProjectGuid", BindingFlags.Public | BindingFlags.Instance);
                s_ProjectType = ProjectInSolution.GetProperty("ProjectType", BindingFlags.Public | BindingFlags.Instance);
            }
        }

        public string ProjectName { get; private set; }
        public string RelativePath { get; private set; }
        public string ProjectGuid { get; private set; }
        public string ProjectType { get; private set; }

        public ProjectSolution(object solutionProject)
        {
            this.ProjectName = (s_ProjectName == null ? "" : s_ProjectName.GetValue(solutionProject, null) as string);
            this.RelativePath = (s_RelativePath == null ? "" : s_RelativePath.GetValue(solutionProject, null) as string);
            this.ProjectGuid = (s_ProjectGuid == null ? "" : s_ProjectGuid.GetValue(solutionProject, null) as string);
            this.ProjectType = (s_ProjectType == null ? "" : s_ProjectType.GetValue(solutionProject, null).ToString());
        }
    }
}