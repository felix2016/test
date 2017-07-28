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
        public Dictionary<int, List<Task>> all_tasks_ = new Dictionary<int, List<Task>>();

        public FlexibleJobShopData()

        {
            //job_count machine_count
            //10  10  1
            //operation_count alternative_count machine duration alternative_count machine duration ......
            //10  1   7   54  1   1   87  1   5   48  1   4   60  1   8   39  1   9   35  1   2   72  1   6   95  1   3   66  1   10  5
            //10  1   4   20  1   10  46  1   7   34  1   6   55  1   1   97  1   9   19  1   5   59  1   3   21  1   8   37  1   2   46
            //10  1   5   45  1   2   24  1   9   28  1   1   28  1   8   83  1   7   78  1   6   23  1   4   25  1   10  5   1   3   73
            //10  1   10  12  1   2   37  1   5   38  1   4   71  1   9   33  1   3   12  1   7   55  1   1   53  1   8   87  1   6   29
            //10  1   4   83  1   3   49  1   7   23  1   10  27  1   8   65  1   1   48  1   5   90  1   6   7   1   2   40  1   9   17
            //10  1   2   66  1   5   25  1   1   62  1   3   84  1   10  13  1   7   64  1   8   46  1   9   59  1   6   19  1   4   85
            //10  1   2   73  1   4   80  1   1   41  1   3   53  1   10  47  1   8   57  1   9   74  1   5   14  1   7   67  1   6   88
            //10  1   6   64  1   4   84  1   7   46  1   2   78  1   1   84  1   8   26  1   9   28  1   10  52  1   3   41  1   5   63
            //10  1   2   11  1   1   64  1   8   67  1   5   85  1   4   10  1   6   73  1   10  38  1   9   95  1   7   97  1   3   17
            //10  1   5   60  1   9   32  1   3   95  1   4   93  1   2   65  1   7   85  1   8   43  1   10  85  1   6   46  1   1   59
            name = "";
            machine_count = -1;
            job_count = -1;
            horizon = 0;
            for (int i = 0; i < 3; i++)
            {
                all_tasks_[i] = new List<Task>();
            }

        }
        public void Load()
        {

            job_count = 3;
            machine_count = 3;
            List<int> machines = new List<int>();
            List<int> durations = new List<int>();
            machines.Add(0);
            durations.Add(2);
            machines.Add(1);
            durations.Add(2);
            machines.Add(2);
            durations.Add(18);
            AddTask(0, machines, durations);
            machines = new List<int>();
            durations = new List<int>();
            machines.Add(0);
            durations.Add(20);
            machines.Add(1);
            durations.Add(25);
            machines.Add(2);
            durations.Add(27);
           // AddTask(0, machines, durations, 1);
            AddTask(0, machines, durations);
            var task = TasksOfJob(0)[0] ;
            var otherTask = TasksOfJob(0)[1];
            task.dependencies.Add(new TaskDependency() { OtherTask = otherTask, Delay =12 }); // task 0 starts after task 1 is started with 12 unit of time delay



            machines = new List<int>();
            durations = new List<int>();
            machines.Add(0);
            durations.Add(7);
            machines.Add(1);
            durations.Add(7);
            machines.Add(2);
            durations.Add(4);
            AddTask(0, machines, durations);

            task = TasksOfJob(0)[2];
            //make task fixed
            task.IsFixed = true;
            task.MachineID = 1;
            task.Start = 40;


            otherTask = TasksOfJob(0)[0];
            task.dependencies.Add(new TaskDependency() { OtherTask = otherTask, Delay = 10 }); // task 2 starts after task 0 is started with 10 unit of time delay

            current_job_index = 3;



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
               List<int> durations, int t =0)
        {
            all_tasks_[job_id].Add(new Task(job_id, machines, durations, t));
            horizon += SumOfDurations(durations);
        }

    }
}
