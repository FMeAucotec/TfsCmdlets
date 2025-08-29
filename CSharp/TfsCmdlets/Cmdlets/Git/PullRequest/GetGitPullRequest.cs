using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System.Linq;
using System.Management.Automation;
using TfsCmdlets.Extensions;

namespace TfsCmdlets.Cmdlets.PullRequest
{
    /// <summary>
    /// Returns a pull request object for a given pullrequest id 
    /// </summary>
    [TfsCmdlet(CmdletScope.Project,  OutputType = typeof(GitPullRequest))]
    partial class GetGitPullRequest
    {
        /// <summary>
        /// pull request id
        /// </summary>
        [Parameter]
        public int Id { get; set; }

        /// <summary>
        /// pull request status
        /// </summary>
        [Parameter]
        public PullRequestStatus Status { get; set; }


        /// <summary>
        /// Specifies the user or group to be retrieved. Supported values are: 
        /// User/group name, email, or ID
        /// </summary>
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        public object CreatorId { get; set; }


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
            object PRID = Parameters.Get<object>(nameof(GetGitPullRequest.Id));
            if(PRID is int && ((int) PRID) != 0)
            {
                var result = Client.GetPullRequestAsync(repo.ProjectReference.Name, repo.Id.ToString(), (int)PRID )
                     .GetResult("Error finding  pull request");

                yield return result;
                yield break;
            }
            else
            {
                PullRequestStatus state = Parameters.Get<PullRequestStatus>(nameof(GetGitPullRequest.Status));
                if(state == PullRequestStatus.NotSet) state = PullRequestStatus.All;


                object creator = Parameters.Get<object>(nameof(GetGitPullRequest.CreatorId));
                Guid? CreatorID = null;
                if (creator is not null)
                {
                    var id = Data.GetItems<Models.Identity>(new { Identity = creator });
                    if(id.Count() >0)
                        CreatorID = id.First().Id;
                    else
                    {
                        yield return null;
                        yield break;
                    }

                }


                var crit = new GitPullRequestSearchCriteria
                {
                    Status = state,
                    CreatorId = CreatorID,
                };

                var result = Client.GetPullRequestsAsync(repo.ProjectReference.Id, repo.Id, crit)
                    .GetResult("Error finding  pull request");

                yield return result;
            }

 



        }
    }
}