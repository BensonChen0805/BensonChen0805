using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalcWorkItemsAwaitingTime
{
    public class RequestArgs
    {
        public string PAT;
        public string Alias;
        public DateTime StartTime;
        public DateTime EndTime;

        public static bool TryParse(string[] args, out RequestArgs requestArgs)
        {
            requestArgs = new RequestArgs();
            switch (args.Length)
            {
                case 1:
                    requestArgs.PAT = args[0];
                    break;
                case 2:
                    requestArgs.PAT = args[0];
                    requestArgs.Alias = args[1];
                    break;
                case 3:
                    requestArgs.PAT = args[0];
                    requestArgs.Alias = args[1];

                    if (DateTime.TryParse(args[2], out DateTime tmp))
                        requestArgs.StartTime = tmp;
                    else
                        return false;

                    break;
                case 4:
                    requestArgs.PAT = args[0];
                    requestArgs.Alias = args[1];

                    if (DateTime.TryParse(args[2], out DateTime start))
                        requestArgs.StartTime = start;
                    else
                        requestArgs.StartTime = DateTime.MinValue;

                    if (DateTime.TryParse(args[3], out DateTime end))
                        requestArgs.EndTime = end;
                    else
                        requestArgs.EndTime = DateTime.MinValue;

                    break;
            }

            if (string.IsNullOrEmpty(requestArgs.Alias))
            {
                requestArgs.Alias = Environment.UserName;
            }
            else
            {
                if (!requestArgs.Alias.Contains("@"))
                {
                    requestArgs.Alias = $"{requestArgs.Alias}@microsoft.com";
                }
            }

            return true;
        }

        public string Validate()
        {
            var msg = string.Empty;

            if (string.IsNullOrEmpty(PAT)
                || string.IsNullOrEmpty(Alias))
            {
                msg += "Please provide your alias and the corresponding PAT, without it the tool cannot query WorkItems.";
            }

            if (StartTime == DateTime.MinValue
                || StartTime == DateTime.MaxValue)
            {
                StartTime = DateTime.UtcNow;
                msg += "You didn't set start time, using UTC current time for start time.";
            }

            if (EndTime == DateTime.MinValue
                || EndTime == DateTime.MaxValue)
            {
                EndTime = DateTime.UtcNow;
                msg += "You didn't set end time, using UTC current time for end time.";
            }

            return msg;
        }
    }
}
