using System;

namespace DataAccess.Entities
{
    public class WorkItemEntity
    {
        public int Id;
        public string Title;
        public string AssignedTo;
        public DateTime CreatedDate;
        public DateTime? CloseDate;
        public string State;
        public string AreaPath;
        public string Tags;
        public WitType WitType;
        public string SupportTeam;
        public string ServiceCategory;
        public string SubCategory;
        public int? TimeSpent;
        public int? Cost;
        public double AwaitingTimeInMinutesInTotal;
        public int ReopenTimes;
    }

    public enum WitType
    {
        ServiceTicket,
        UserStory,
        Bug,
        Others
    }
}