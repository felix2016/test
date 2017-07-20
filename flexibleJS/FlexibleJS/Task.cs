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
        public int type;

        public Task(int j, List<int> m, List<int> d)
        {
            job_id = j;
            machines = m; durations = d;
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
}
