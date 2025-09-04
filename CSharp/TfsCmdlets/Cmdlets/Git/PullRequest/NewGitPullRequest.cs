using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System.Management.Automation;
using System.Runtime.Serialization;

namespace TfsCmdlets.Cmdlets.PullRequest
{
    /// <summary>
    /// Creates a new pull request.
    /// </summary>
    [TfsCmdlet(CmdletScope.Project, SupportsShouldProcess = true, OutputType = typeof(GitPullRequest))]
    partial class NewGitPullRequest
    {


        /// <summary>
        /// Specifies the title of the pull request .
        /// </summary>
        [Parameter(Mandatory = true)]
        public string Title { get; set; }

        /// <summary>
        /// Specifies the Description of the pull request .
        /// </summary>
        [Parameter]
        public string Description { get; set; }

        /// <summary>
        /// Specifies the name of the source branch 
        /// </summary>
        [Parameter(Mandatory = true)]
        public string SourceBranch { get; set; }


        /// <summary>
        /// Specifies the name of the target branch
        /// </summary>
        [Parameter(Mandatory = true)]
        public string TargetBranch { get; set; }

        /// <summary>
        /// HELP_PARAM_GIT_REPOSITORY
        /// </summary>
        [Parameter(ValueFromPipeline = true, Mandatory = true)]
        public object Repository { get; set; }


        /// <summary>
        /// If set set pullrequest to  autocomplete
        /// </summary>
        [Parameter()]
        public SwitchParameter SetAutoComplete { get; set; }

        /// <summary>
        /// pullrequest  autocomplete merge strategy (default == Squash)
        /// </summary>
        [Parameter()]
        public  GitPullRequestMergeStrategy MergeStrategy  { get; set; } = GitPullRequestMergeStrategy.Squash;

        /// <summary>
        /// If true delete source branch after merge  (default == false)
        /// </summary>
        [Parameter()]
        public bool DeleteSourceBranch { get; set; } = false;


    }

    [CmdletController(typeof(GitPullRequest), Client=typeof(IGitHttpClient))]
    partial class NewGitPullRequestController
    {
         

        protected override IEnumerable Run()
        { 
            var Links = new Microsoft.VisualStudio.Services.WebApi.ReferenceLinks();
            //Links.AddLinkIfIsNotEmpty(,)
            var mergeStrategy = Parameters.Get<GitPullRequestMergeStrategy>(nameof(NewGitPullRequest.MergeStrategy));
            
            
            if (!PowerShell.ShouldProcess(Project, $"Create new pull request")) yield break;

            var repo = GetItem<GitRepository>(new { Repository, Default = false });
            var reqToCreate = new GitPullRequest
            {
                Title = Title,
                Description = Description,
                CompletionOptions  = new GitPullRequestCompletionOptions 
                { 
                    MergeStrategy = GitPullRequestMergeStrategy.Squash, 
                    DeleteSourceBranch = this.DeleteSourceBranch
                },
                SourceRefName = "refs/heads/" + SourceBranch,
                TargetRefName = "refs/heads/" + TargetBranch,
                Repository = repo,
               Links = Links
            };
       

            var result = Client.CreatePullRequestAsync(reqToCreate, Project.Id, repo.Id)
                .GetResult("Error creating pull request");

            if (SetAutoComplete) // toggle by param
            {
                var reqToUpdate = new GitPullRequest
                {
                    AutoCompleteSetBy = result.CreatedBy,
                    CompletionOptions = new GitPullRequestCompletionOptions
                    {
                        MergeStrategy = mergeStrategy,
                        DeleteSourceBranch = this.DeleteSourceBranch
                    }

                };

                result = Client.UpdatePullRequestAsync(reqToUpdate, repo.Id, result.PullRequestId).GetResult("Error updating pull request to autocomplete.");

            }

            yield return result;
        }
 

    }
}