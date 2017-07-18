﻿// This model implements a simple jobshop problem.
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


    public class Task
    {
        int job_id;
        List<int> machines;
        List<int> durations;
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

        class TaskAlternative
        {
            public TaskAlternative(int j)
            {
                job_id = j;
                alternative_variable = null;

            }
            public int job_id;
            public List<IntervalVar> intervals = new List<IntervalVar>();
            public IntVar alternative_variable;
            public int type;

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
                //if (FLAGS_data_file.empty())
                //{
                //LOG(FATAL) << "Please supply a data file with --data_file=";
                //  }


                Solver solver = new Solver("flexible_jobshop");
                FlexibleJobShopData data = new FlexibleJobShopData();
                data.Load();
                int machine_count = data.machine_count;
                int job_count = data.job_count;
                int horizon = data.horizon;
                Console.WriteLine(data.DebugString());

                Dictionary<int, List<TaskAlternative>> jobs_to_tasks = new Dictionary<int, List<TaskAlternative>>();
                // machines_to_tasks stores the same interval variables as above, but
                // grouped my machines instead of grouped by jobs.
                Dictionary<int, IntervalVarVector> machines_to_tasks = new Dictionary<int, IntervalVarVector>();

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
                        IntVarVector active_variables = new IntVarVector();
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
                            IntervalVar interval = solver.MakeFixedDurationIntervalVar(
                                 0, horizon, duration, optional, name);
                            jobs_to_tasks[job_id][jobs_to_tasks[job_id].Count - 1].intervals.Add(interval);
                            machines_to_tasks[machine_id].Add(interval);
                            if (optional)
                            {
                                active_variables.Add(interval.PerformedExpr().Var());
                            }
                        }
                        string alternative_name = string.Format("J%dI%d", job_id, task_index);
                        IntVar alt_var =
                            solver.MakeIntVar(0, task.machines.Count - 1, alternative_name);
                        jobs_to_tasks[job_id][jobs_to_tasks[job_id].Count - 1].alternative_variable = alt_var;
                        if (optional)
                        {
                            solver.Add(solver.MakeMapDomain(alt_var, active_variables));
                        }
                    }
                }

                // Collect alternative variables.
                IntVarVector all_alternative_variables = new IntVarVector();
                for (int job_id = 0; job_id < job_count; ++job_id)
                {
                    int task_count = jobs_to_tasks[job_id].Count;
                    for (int task_index = 0; task_index < task_count; ++task_index)
                    {
                        IntVar alternative_variable =
                             jobs_to_tasks[job_id][task_index].alternative_variable;
                        if (!alternative_variable.Bound())
                        {
                            all_alternative_variables.Add(alternative_variable);
                        }
                    }
                }

                // Adds disjunctive constraints on unary resources, and creates
                // sequence variables. A sequence variable is a dedicated variable
                // whose job is to sequence interval variables.
                SequenceVarVector all_sequences = new SequenceVarVector();
                for (int machine_id = 0; machine_id < machine_count; ++machine_id)
                {
                    string name = string.Format("Machine_%d", machine_id);
                    DisjunctiveConstraint ct =
                         solver.MakeDisjunctiveConstraint(machines_to_tasks[machine_id], name);
                    //var distance_obj = Dist();
                    //distance_callback = distance_obj.distance;
                    //// Store all instances of the distance callbacks to have the same
                    ////  # life cycle as the solver.
                  
                    //all_transitions.append(distance_callback);
                    //ct.SetTransitionTime(distance_callback)
                    solver.Add(ct);
                    all_sequences.Add(ct.SequenceVar());
                }

                // Creates array of end_times of jobs.
                IntVarVector all_ends = new IntVarVector();
                for (int job_id = 0; job_id < job_count; ++job_id)
                {
                    TaskAlternative task_alt = jobs_to_tasks[job_id][jobs_to_tasks[job_id].Count - 1];
                    for (int alt = 0; alt < task_alt.intervals.Count; ++alt)
                    {
                        IntervalVar t = task_alt.intervals[alt];
                        all_ends.Add(t.EndExpr().Var());
                    }
                }


                // Objective: minimize the makespan (maximum end times of all tasks)
                // of the problem.
                IntVar objective_var = solver.MakeMax(all_ends).Var();
                OptimizeVar objective_monitor = solver.MakeMinimize(objective_var, 1);

                // ----- Search monitors and decision builder -----

                // This decision builder will assign all alternative variables.
                DecisionBuilder alternative_phase =
                     solver.MakePhase(all_alternative_variables, Solver.CHOOSE_MIN_SIZE,
                                      Solver.ASSIGN_MIN_VALUE);

                // This decision builder will rank all tasks on all machines.
                DecisionBuilder sequence_phase =
                     solver.MakePhase(all_sequences, Solver.SEQUENCE_DEFAULT);

                // After the ranking of tasks, the schedule is still loose and any
                // task can be postponed at will. But, because the problem is now a PERT
                // (http://en.wikipedia.org/wiki/Program_Evaluation_and_Review_Technique),
                // we can schedule each task at its earliest start time. This is
                // conveniently done by fixing the objective variable to its
                // minimum value.
                DecisionBuilder obj_phase =
                     solver.MakePhase(objective_var,
                                      Solver.CHOOSE_FIRST_UNBOUND,
                                      Solver.ASSIGN_MIN_VALUE);

                // The main decision builder (ranks all tasks, then fixes the
                // objective_variable).
                DecisionBuilder main_phase =
                     solver.Compose(alternative_phase, sequence_phase, obj_phase);

                // Search log.
                const int kLogFrequency = 1000000;
                SearchMonitor search_log =
                     solver.MakeSearchLog(kLogFrequency, objective_monitor);

                int FLAGS_time_limit_in_ms = 0;
                SearchLimit limit = null;
                if (FLAGS_time_limit_in_ms > 0)
                {
                    limit = solver.MakeTimeLimit(FLAGS_time_limit_in_ms);
                }

                SolutionCollector collector = solver.MakeLastSolutionCollector();
                collector.AddObjective(objective_var);
                collector.Add(all_alternative_variables);
                collector.Add(all_sequences);

                // Search.
                if (solver.Solve(main_phase,
                                 search_log,
                                 objective_monitor,
                                 limit,
                                 collector))
                {
                    for (int m = 0; m < machine_count; ++m)
                    {
                        SequenceVar seq = all_sequences[m];
                        //LOG(INFO) << seq->name() << ": "
                        //          << strings::Join(collector->ForwardSequence(0, seq), ", ");
                    }
                }


            }
        }
    }
}