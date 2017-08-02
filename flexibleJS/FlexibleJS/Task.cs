using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csjobshop_flexiblesched
{
    public class Task
    {
        public int job_id;
        public List<int> machines;
        public List<int> durations;
        public List<TaskDependency> dependencies;
        public int type;
        public int Start { get; set; }
        public int End { get; set; }
        public int MachineID { get; set; }
        public bool IsFixedStart { get; set; }
        public bool IsFixedEnd { get; set; }
        public int TaskIndexForJob;

        public Task(int j, List<int> m, List<int> d, int t,int index, List<TaskDependency> dp =null)
        {
            job_id = j;
            machines = m; durations = d;
            type = t;
            dependencies = dp;
            TaskIndexForJob = index;
            if (dependencies == null)
            {
                dependencies = new List<TaskDependency>();
            }
        }

        public string DebugString()
        {
            string ret = "Task(" + job_id;
            for (int k = 0; k < machines.Count; ++k)
            {
                ret = ret + string.Format("<m{0},{1}>", machines[k], durations[k]);
                if (k < machines.Count - 1)
                {
                    ret = ret + " | ";
                }
            }
            ret = ret + ")";
            return ret;
        }

    }

    public class TaskDependency 
    {
        public Task OtherTask;
        public long Delay; // delay after starting the other task;

        
    }
}

