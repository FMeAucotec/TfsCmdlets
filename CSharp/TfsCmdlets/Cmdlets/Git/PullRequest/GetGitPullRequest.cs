using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System.Management.Automation;
using TfsCmdlets.Extensions;

namespace TfsCmdlets.Cmdlets.PullRequest
{
    /// <summary>
    /// Sets the contents of one or more work items.
    /// </summary>
    [TfsCmdlet(CmdletScope.Project,  OutputType = typeof(GitPullRequest))]
    partial class GetGitPullRequest
    {
        /// <summary>
        /// Specifies a work item. Valid values are the work item ID or an instance of
        /// Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem.
        /// </summary>
        [Parameter]
        public int PullRequest { get; set; }


        /// <summary>
        /// HELP_PARAM_GIT_REPOSITORY
        /// </summary>
        [Parameter(ValueFromPipeline = true, Mandatory = true)]
        public object Repository { get; set; }

    }

    [CmdletController(typeof(GitPullRequest), Client = typeof(IGitHttpClient))]
    partial class GetGitPullRequestController
    {
        protected override IEnumerable Run()
        {

            var repo = GetItem<GitRepository>(new { Repository, Default = false });

            var result = Client.GetPullRequestAsync(repo.ProjectReference.Name, repo.Id.ToString(), Parameters.Get<int>(nameof(GetGitPullRequest.PullRequest)))
                .GetResult("Error finding  pull request");

            yield return result;
         
        }
    }
}