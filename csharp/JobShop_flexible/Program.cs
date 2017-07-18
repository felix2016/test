// This model implements a simple jobshop problem.
//
// A jobshop is a standard scheduling problem where you must schedule a
// set of jobs on a set of machines.  Each job is a sequence of tasks
// (a task can only start when the preceding task finished), each of
// which occupies a single specific machine during a specific
// duration. Therefore, a job is simply given by a sequence of pairs
// (machine id, duration).

// The objective is to minimize the 'makespan', which is the duration
// between the start of the first task (across all machines) and the
// completion of the last task (across all machines).
//
// This will be modelled by sets of intervals variables (see class
// IntervalVar in constraint_solver/constraint_solver.h), one per
// task, representing the [start_time, end_time] of the task.  Tasks
// in the same job will be linked by precedence constraints.  Tasks on
// the same machine will be covered by Sequence constraints.
//
// Search will then be applied on the sequence constraints.


using Google.OrTools.ConstraintSolver;
using System;
using System.Collections.Generic;
using System.Text;

namespace JobShop_flexible
{
    class FlexibleJobShopData
    {
        public string name { get; set; }
        public int machine_count { get; set; }
        public int job_count { get; set; }
        public int horizon { get; set; }
        public int current_job_index { get; set; }
        public Dictionary<int, List<Task>> all_tasks_ { get; set; }

        public FlexibleJobShopData()

        {
            name= "";
            machine_count = -1;
            job_count = -1;
            horizon = 0;
            current_job_index = 0;

        }
        public void Load()
        {

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





    public class Task
    {
        int job_id;
        List<int> machines;
        List<int> durations;

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

        class TaskAlternative
        {
            public TaskAlternative(int j)
            {
                job_id = j;
                alternative_variable = 0;

            }
            int job_id;
            List<int> intervals;
            int alternative_variable;

        };

        public class Program
        {
            string data_file = "" +
        "Required: input file description the scheduling problem to solve, " +
        "in our jssp format:\n" +
        "  - the first line is \"instance <instance name>\"\n" +
        "  - the second line is \"<number of jobs> <number of machines>\"\n" +
        "  - then one line per job, with a single space-separated " +
        "list of \"<machine index> <duration>\"\n" +
        "note: jobs with one task are not supported";



            int inttime_limit_in_ms = 0;

            static void Main(string[] args)
            {
                Solver solver =new Solver("flexible_jobshop");
                FlexibleJobShopData data = new FlexibleJobShopData();
                data.Load();
                 int machine_count = data.machine_count;
                 int job_count = data.job_count;
                 int horizon = data.horizon;
                Console.WriteLine(data.DebugString());

                Dictionary<int,List<TaskAlternative>> jobs_to_tasks=new Dictionary<int, List<TaskAlternative>>();
                // machines_to_tasks stores the same interval variables as above, but
                // grouped my machines instead of grouped by jobs.
                Dictionary < int,List < IntervalVar>> machines_to_tasks;

                // Creates all individual interval variables.
                for (int job_id = 0; job_id < job_count; ++job_id)
                {
                    List<Task> tasks =
                        data.TasksOfJob(job_id);
                    for (int task_index = 0; task_index < tasks.Count; ++task_index)
                    {
                        Task task = tasks[task_index];
                        //CHECK_EQ(job_id, task.job_id);
                        jobs_to_tasks[job_id].Add(new TaskAlternative(job_id));
                         bool optional = task.machines.Count > 1;
                        List<IntVar> active_variables;
                        for (int alt = 0; alt < task.machines.Count; ++alt)
                        {
                             int machine_id = task.machines[alt];
                             int duration = task.durations[alt];
                             string name = string.Format("J%dI%dA%dM%dD%d",
                                                             task.job_id,
                                                             task_index,
                                                             alt,
                                                             machine_id,
                                                             duration);
                            IntervalVar  interval = solver.MakeFixedDurationIntervalVar(
                                 0, horizon, duration, optional, name);
                            jobs_to_tasks[job_id].back().intervals.push_back(interval);
                            machines_to_tasks[machine_id].push_back(interval);
                            if (optional)
                            {
                                active_variables.push_back(interval->PerformedExpr()->Var());
                            }
                        }
                        string alternative_name = StringPrintf("J%dI%d", job_id, task_index);
                        IntVar* alt_var =
                            solver.MakeIntVar(0, task.machines.size() - 1, alternative_name);
                        jobs_to_tasks[job_id].back().alternative_variable = alt_var;
                        if (optional)
                        {
                            solver.AddConstraint(solver.MakeMapDomain(alt_var, active_variables));
                        }
                    }
                }





            }
        }
    }
}