using DataAccess.Entities;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WITDataAccess
{
    public class QueryExecutor
    {
        private const string project = "devdiv";
        private readonly Uri projectUri;
        private readonly string PAT;

        public QueryExecutor(string pat)
        {
            projectUri = new Uri($"https://dev.azure.com/{project}/");
            PAT = pat;
        }

        public IList<WorkItemEntity> GenerateWorkItemEntity(string alias, DateTime startTime, DateTime endTime)
        {
            int[] ids = GetWorkItemIds(alias, startTime, endTime);

            IList<WorkItemEntity> entities = new List<WorkItemEntity>();

            foreach (var id in ids)
            {
                // id, title, AssignedTo createdDate closeDate state witType supportTeam serviceCategory subCategory timeSpent awaitingTimeInMinutesInTotal reopenTimes
                WorkItemEntity workItemEntity = new WorkItemEntity();

                var workItem = GetWorkItemById(id);
                var fields = workItem.Fields;

                workItemEntity.Id = id;

                //[System.WorkItemType],
                var type = workItem.Fields["System.WorkItemType"].ToString().ToLowerInvariant();

                switch (type)
                {
                    case "service ticket":
                        workItemEntity.WitType = WitType.ServiceTicket;
                        break;
                    case "Bug":
                        workItemEntity.WitType = WitType.Bug;
                        break;
                    case "UserStory":
                        workItemEntity.WitType = WitType.UserStory;
                        break;
                    default:
                        workItemEntity.WitType = WitType.Others;
                        break;
                }

                workItemEntity.Title = fields["System.Title"].ToString();

                var identityRef = fields["System.AssignedTo"] as IdentityRef;
                workItemEntity.AssignedTo = identityRef.UniqueName;

                workItemEntity.State = fields["System.State"].ToString();

                if (fields.ContainsKey("System.Tags"))
                {
                    workItemEntity.Tags = fields["System.Tags"].ToString();
                }

                workItemEntity.CreatedDate = DateTime.Parse(fields["System.CreatedDate"].ToString());

                if (fields.ContainsKey("Microsoft.VSTS.Common.ClosedDate"))
                {
                    var closedDate = fields["Microsoft.VSTS.Common.ClosedDate"].ToString();
                    if (!string.IsNullOrEmpty(closedDate))
                        workItemEntity.CloseDate = DateTime.Parse(closedDate);
                }

                workItemEntity.AreaPath = fields["System.AreaPath"].ToString();

                // below fields are for service tickets.
                if (workItemEntity.WitType == WitType.ServiceTicket)
                {
                    foreach (var field in fields)
                    {
                        var lowerKeyName = field.Key.ToLowerInvariant();
                        switch (lowerKeyName)
                        {
                            case "microsoft.devdiv.supportteam":
                                workItemEntity.SupportTeam = field.Value.ToString();
                                break;
                            case "microsoft.devdiv.servicerequest":
                                workItemEntity.ServiceCategory = field.Value.ToString();
                                break;
                            case "microsoft.devdiv.subcategory":
                                workItemEntity.SubCategory = field.Value.ToString();
                                break;
                            case "microsoft.devdiv.completedworkminutes":
                                var value = field.Value.ToString();
                                if (!string.IsNullOrEmpty(value))
                                    workItemEntity.TimeSpent = int.Parse(value);
                                else
                                    workItemEntity.TimeSpent = null;
                                break;
                        }
                    }

                    // Try to get AwaitingTimeInMinutesInTotal and ReopenTimes;
                    var workItemUpdates = GetWorkItemFullHistory(id);

                    double totalAwaitingTimeInMin = 0;
                    int reopenTimes = 0;

                    foreach (var update in workItemUpdates)
                    {
                        if (update.Fields != null
                            && update.Fields.ContainsKey("System.State"))
                        {
                            var state = update.Fields["System.State"];
                            var oldState = string.Empty;

                            if (state.OldValue != null)
                                oldState = state.OldValue.ToString();
                            else
                                oldState = "New";

                            var newState = string.Empty;
                            if (state.NewValue != null)
                                newState = state.NewValue.ToString();

                            // Awaiting time in total => Old: AwaitingForCustomer New: AnyValues.
                            if (oldState.Equals("Awaiting Customer Feedback", StringComparison.OrdinalIgnoreCase)
                                && !newState.Equals("Awaiting Customer Feedback", StringComparison.OrdinalIgnoreCase))
                            {
                                if (update.Fields["System.ChangedDate"].OldValue != null
                                    && update.Fields["System.ChangedDate"].NewValue != null)
                                {
                                    totalAwaitingTimeInMin += (Convert.ToDateTime(update.Fields["System.ChangedDate"].NewValue) - Convert.ToDateTime(update.Fields["System.ChangedDate"].OldValue)).TotalMinutes;
                                }
                            }

                            // Reopen times => Old: Closed, New: AnValues
                            if (oldState.Equals("Closed", StringComparison.OrdinalIgnoreCase)
                                && !newState.Equals("Closed", StringComparison.OrdinalIgnoreCase))
                            {
                                reopenTimes++;
                            }
                        }
                    }

                    workItemEntity.ReopenTimes = reopenTimes;
                    workItemEntity.AwaitingTimeInMinutesInTotal = totalAwaitingTimeInMin;
                }
                else
                {
                    // Cost is for bugs, user stories and tasks.
                    if (fields.ContainsKey("Microsoft.DevDiv.Cost"))
                    {
                        var cost = fields["Microsoft.DevDiv.Cost"].ToString();
                        if (!string.IsNullOrEmpty(cost))
                            workItemEntity.Cost = int.Parse(fields["Microsoft.DevDiv.Cost"].ToString());
                        else
                            workItemEntity.Cost = null;
                    }
                }

                entities.Add(workItemEntity);
            }

            return entities;
        }

        public int[] GetWorkItemIds(string alias, DateTime startTime, DateTime endTime)
        {
            var wiql = new Wiql()
            {
                Query = $"Select [Id] From WorkItems WHERE [System.AssignedTo] ='{alias}' AND [System.CreatedDate] >= '{startTime}' AND [System.CreatedDate] < '{endTime}'"
            };

            using (var httpClient = new WorkItemTrackingHttpClient(projectUri, new VssBasicCredential(string.Empty, PAT)))
            {
                var result = httpClient.QueryByWiqlAsync(wiql).Result;

                if (result.WorkItems.Any())
                {
                    return result.WorkItems.Select(item => item.Id).ToArray();
                }

                // TODO:: for testing, will need to be deleted afterwards.
                return new int[] { 1346878 };
            }
        }

        public WorkItem GetWorkItemById(int id)
        {
            using (var httpClient = new WorkItemTrackingHttpClient(projectUri, new VssBasicCredential(string.Empty, PAT)))
            {
                var result = httpClient.GetWorkItemAsync(id).Result;

                return result;
            }
        }

        public IList<WorkItemUpdate> GetWorkItemFullHistory(int id)
        {
            using (var httpClient = new WorkItemTrackingHttpClient(projectUri, new VssBasicCredential(string.Empty, PAT)))
            {
                var result = httpClient.GetUpdatesAsync(id).Result;

                return result;
            }
        }
    }
}
