using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csjobshop_flexiblesched
{
    public class FlexibleJobShopData
    {
        public string name { get; set; }
        public int machine_count { get; set; }
        public int job_count { get; set; }
        public int horizon { get; set; }
        public int current_job_index { get; set; }
        public Dictionary<int, List<Task>> all_tasks_ { get; set; }

        public FlexibleJobShopData()

        {
            name = "";
            machine_count = -1;
            job_count = -1;
            horizon = 0;
            current_job_index = 0;

        }
        public void Load()
        {
            job_count = 2;
            machine_count = 2;

        }

        public List<Task> TasksOfJob(int job_id)
        {
            return all_tasks_[job_id];
        }


        public string DebugString()
        {
            StringBuilder ret = new StringBuilder();
            ret.Append(string.Format("FlexibleJobshop(name = {0}, {1} machines, {2} jobs)\n",
                         name, machine_count, job_count));
            for (int j = 0; j < all_tasks_.Count; ++j)
            {
                ret.Append(string.Format("  job {0}: ", j));
                for (int k = 0; k < all_tasks_[j].Count; ++k)
                {
                    ret.Append(all_tasks_[j][k].DebugString());
                    if (k < all_tasks_[j].Count - 1)
                    {
                        ret.Append(" -> ");
                    }
                    else
                    {
                        ret.Append("\n");
                    }
                }
            }
            return ret.ToString();
        }

        public int SumOfDurations(List<int> durations)
        {
            int result = 0;
            for (int i = 0; i < durations.Count; ++i)
            {
                result += durations[i];
            }
            return result;
        }

        void AddTask(int job_id, List<int> machines,
               List<int> durations)
        {
            all_tasks_[job_id].Add(new Task(job_id, machines, durations));
            horizon += SumOfDurations(durations);
        }

    }
}
