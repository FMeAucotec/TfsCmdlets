using System.Management.Automation;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace TfsCmdlets.Cmdlets.WorkItem.Linking
{
    /// <summary>
    /// Adds a link between two work items.
    /// </summary>
    [TfsCmdlet(CmdletScope.Collection)]
    partial class AddPullRequestLink
    {
        /// <summary>
        /// Specifies the work item to link from.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [Alias("Id", "From")]
        [ValidateNotNull()]
        public object WorkItem { get; set; }

        /// <summary>
        /// Specifies the work item to link to.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ParameterSetName = "Link to work item")]
        [Alias("To")]
        [ValidateNotNull()]
        public object PullRequest { get; set; }

        /// <summary>
        /// HELP_PARAM_GIT_REPOSITORY
        /// </summary>
        [Parameter(ValueFromPipeline = true)]
        public object Repository { get; set; }

        /// <summary>
        /// HELP_PARAM_PASSTHRU
        /// </summary>
        /// <value></value>
        [Parameter]
        public SwitchParameter Passthru { get; set; }

        /// <summary>
        /// Bypasses any rule validation when saving the work item. Use it with caution, as this 
        /// may leave the work item in an invalid state.
        /// </summary>
        [Parameter]
        public SwitchParameter BypassRules { get; set; }

        /// <summary>
        /// Do not fire any notifications for this change. Useful for bulk operations and automated processes.
        /// </summary>
        [Parameter]
        public SwitchParameter SuppressNotifications { get; set; }

        /// <summary>
        /// Defines a comment to add to the link.
        /// </summary>
        [Parameter]
        public string Comment { get; set; }
    }

    [CmdletController(typeof(WebApiWorkItemRelation), Client = typeof(IWorkItemTrackingHttpClient))]
    partial class AddPullRequestLinkController
    {
        [Import]
        private IKnownWorkItemLinkTypes KnownLinkTypes { get; set; }

        protected override IEnumerable Run()
        {
            {
                var sourceWi = Data.GetItem<WebApiWorkItem>();
                string targetURL = "";

                if(PullRequest is int)
                {
                    if(Parameters.Get<object>(nameof(Repository)) == null)
                    {
                        throw new ArgumentException("Specify Pullrequest as object or provide repository.");
                    }

                    var pr = GetItem<GitPullRequest>(new { PullRequest, Repository = Parameters.Get<object>(nameof(Repository)) });
                    targetURL = pr.ArtifactId;


                }
                else
                {
                    var targetWi = (GitPullRequest)Parameters.Get<object>(nameof(PullRequest));
                    targetURL = targetWi.ArtifactId;
                }
         

               
                var runId = Guid.NewGuid();

                var patch = new JsonPatchDocument() {
                   new JsonPatchOperation() {
                        Operation = Operation.Test,
                        Path = "/rev",
                        Value = sourceWi.Rev
                   },
                   new JsonPatchOperation() {
                        Operation = Operation.Add,
                        Path = "/relations/-",
                        Value = new WebApiWorkItemRelation() {
                            Rel = KnownLinkTypes.GetReferenceName(WorkItemLinkType.ArtifactLink),
                            Url = targetURL,

                            Attributes = new Dictionary<string,object>() {
                                ["comment"] = Parameters.Get<string>(nameof(AddWorkItemLink.Comment), string.Empty),
                                ["name"] = "Pull Request"
                            }
                        }
                   }
                };

                var result = Client.UpdateWorkItemAsync(patch, (int)sourceWi.Id,
                        bypassRules: BypassRules,
                        suppressNotifications: SuppressNotifications)
                    .GetResult("Error updating target work item");

                return result.Relations.Where(r =>
                    r.Url == targetURL &&
                    r.Rel == KnownLinkTypes.GetReferenceName(WorkItemLinkType.ArtifactLink)
                ).ToList();
            }
        }
    }
}