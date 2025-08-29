using System.Management.Automation;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace TfsCmdlets.Cmdlets.WorkItem.Linking
{
    /// <summary>
    /// Adds a link from a work item to a branch.
    /// </summary>
    [TfsCmdlet(CmdletScope.Collection)]
    partial class AddBranchLink
    {
        /// <summary>
        /// Specifies the work item to link from.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        [Alias("Id", "From")]
        [ValidateNotNull()]
        public object WorkItem { get; set; }


        /// <summary>
        /// Specifies the name of a branch in the supplied Git repository. Wildcards are supported. 
        /// When omitted, all branches are returned.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Get by name")]
        [ValidateNotNullOrEmpty]
        public string Branch { get; set; }

        /// <summary>
        /// HELP_PARAM_GIT_REPOSITORY
        /// </summary>
        [Parameter(ValueFromPipeline = true, Mandatory = true, Position = 1)]
        public object Repository { get; set; }


        /// <summary>
        /// Defines a comment to add to the link.
        /// </summary>
        [Parameter]
        public string Comment { get; set; }


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
    }

    [CmdletController(typeof(WebApiWorkItemRelation), Client = typeof(IWorkItemTrackingHttpClient))]
    partial class AddBranchLinkController
    {
        [Import]
        private IKnownWorkItemLinkTypes KnownLinkTypes { get; set; }

        protected override IEnumerable Run()
        {
            var sourceWi = Data.GetItem<WebApiWorkItem>();

            var repo = GetItem<GitRepository>(new { Repository, Default = false });

            if (repo.Size == 0)
            {
                Logger.Log($"Repository {repo.Name} is empty.");
                return null;
            }


            string targetURL = $"""vstfs:///Git/Ref/{repo.ProjectReference.Id}/{repo.Id}/GB""" + Branch.Replace("/", "%2F");

            {


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
                                    ["comment"] = Parameters.Get<string>(nameof(AddBranchLink.Comment), string.Empty),
                                    ["name"] = "Branch"
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