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


using csjobshop_flexiblesched;
using FlexibleJS;
using Google.OrTools.ConstraintSolver;

using System;
using System.Collections.Generic;
using System.Text;

namespace JobShop_flexible
{

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

            Dictionary<int, List<Task>> finalSolution = new Dictionary<int, List<Task>>();
            Solver solver = new Solver("flexible_jobshop");
            FlexibleJobShopData data = new FlexibleJobShopData();
            data.Load();
            // int machine_count = data.machine_count;
            int job_count = data.job_count;
            int horizon = data.horizon;
            int[] setupTime = new int[3];
            int[] type = new int[3];
            //setupTime = new int[4] { 2, 5, 7, 7 };

            SetupTime.TaskTypeSetupTime = new Dictionary<int, long>();
            SetupTime.TaskTypeSetupTime.Add(0, 0);
            SetupTime.TaskTypeSetupTime.Add(1, 0);

            SetupTime.TaskIntervalToTaskType = new Dictionary<IntervalVar, int>();

            Console.WriteLine(data.DebugString());

            Dictionary<int, List<TaskAlternative>> jobs_to_tasks = new Dictionary<int, List<TaskAlternative>>();
            foreach (KeyValuePair<int, List<Task>> job in data.all_tasks_)
            // for (int i = 0; i < job_count; i++)
            {
                jobs_to_tasks[job.Key] = new List<TaskAlternative>();
            }
            // machines_to_tasks stores the same interval variables as above, but
            // grouped my machines instead of grouped by jobs.
            Dictionary<int, IntervalVarVector> machines_to_tasks = new Dictionary<int, IntervalVarVector>();
            foreach (int i in data.all_machines)
            //   for (int i = 0; i < machine_count; i++)
            {
                machines_to_tasks[i] = new IntervalVarVector();
            }

            foreach (KeyValuePair<int, List<Task>> job in data.all_tasks_)
            // Creates all individual interval variables.
            // for (int job_id = 0; job_id < job_count; ++job_id)
            {
                List<Task> tasks =
                    data.TasksOfJob(job.Key);
                for (int task_index = 0; task_index < tasks.Count; ++task_index)
                {
                    Task task = tasks[task_index];
                    //CHECK_EQ(job_id, task.job_id);
                    jobs_to_tasks[job.Key].Add(new TaskAlternative(job.Key));
                    bool optional = task.machines.Count > 1;
                    IntVarVector active_variables = new IntVarVector();
                    for (int alt = 0; alt < task.machines.Count; ++alt)
                    {
                        int machine_id = task.machines[alt];
                        int duration = task.durations[alt];
                        string name = string.Format("J{0}I{1}A{2}M{3}D{4}T{5}",
                                                        task.job_id,
                                                        task_index,
                                                        alt,
                                                        machine_id,
                                                        duration,
                                                        task.type);
                        IntervalVar interval = null;
                        if (task.IsFixedStart == false)
                        {
                            if (!task.IsFixedEnd)
                                //interval = solver.MakeFixedDurationIntervalVar(
                                //     0, horizon, duration, optional, name);
                                interval = solver.MakeIntervalVar(0, horizon - duration, 0, horizon, duration, horizon, optional, name);
                            else
                            {
                                //interval = solver.MakeFixedDurationIntervalVar(0, task.FixedEnd - duration, duration, optional, name);
                                interval = solver.MakeIntervalVar(0, horizon - duration, 0, horizon, task.FixedEnd, task.FixedEnd, optional, name);
                            }
                            jobs_to_tasks[job.Key][jobs_to_tasks[job.Key].Count - 1].intervals.Add(interval);
                            machines_to_tasks[machine_id].Add(interval);

                        }
                        else
                        {
                            if (task.IsFixedStart && machine_id == task.MachineID)
                            {
                                // interval = solver.MakeFixedDurationIntervalVar(task.FixedStart, task.FixedStart, duration, optional, name);
                                interval = solver.MakeIntervalVar(task.FixedStart, task.FixedStart, 0, horizon, duration, horizon, optional, name);
                                machines_to_tasks[machine_id].Add(interval);
                            }
                            //if (task.IsFixedEnd)
                            //{
                            //    interval = solver.MakeIntervalVar(0, horizon, duration, duration, task.End, task.End, optional, name);
                            //    machines_to_tasks[machine_id].Add(interval);
                            //}
                        }
                        if (interval != null)
                        {
                            SetupTime.TaskIntervalToTaskType.Add(interval, task.type);
                            if (optional)
                            {
                                active_variables.Add(interval.PerformedExpr().Var());
                            }
                        }

                    }
                    string alternative_name = string.Format("J{0}I{1}", job.Key, task_index);
                    IntVar alt_var =
                        solver.MakeIntVar(0, task.machines.Count - 1, alternative_name);
                    jobs_to_tasks[job.Key][jobs_to_tasks[job.Key].Count - 1].alternative_variable = alt_var;
                    if (optional)
                    {
                        solver.Add(solver.MakeMapDomain(alt_var, active_variables));
                    }
                }
            }

            // Collect alternative variables.
            IntVarVector all_alternative_variables = new IntVarVector();
            foreach (KeyValuePair<int, List<Task>> job in data.all_tasks_)
            //  for (int job_id = 0; job_id < job_count; ++job_id)
            {
                int task_count = jobs_to_tasks[job.Key].Count;
                for (int task_index = 0; task_index < task_count; ++task_index)
                {
                    IntVar alternative_variable =
                         jobs_to_tasks[job.Key][task_index].alternative_variable;

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
            List<SetupTime> all_distances = new List<SetupTime>();

            DisjunctiveConstraint ct;
            foreach (KeyValuePair<int, IntervalVarVector> machine in machines_to_tasks)
            //for (int machine_id = 0; machine_id < machine_count; ++machine_id)
            {
                int machine_id = machine.Key;
                string name = string.Format("Machine_{0}", machine_id);
                ct = solver.MakeDisjunctiveConstraint(machines_to_tasks[machine_id], name);
                for (int i = 0; i < machines_to_tasks[machine_id].Count; i++)
                {
                    Task orTask = data.TasksOf(machines_to_tasks[machine_id][i].Name());
                    AddDurationConstraints(data, machines_to_tasks, machine_id, i, orTask, solver );
                    
                }

                SetupTime distances = new SetupTime(machines_to_tasks[machine_id]);
                all_distances.Add(distances);

                ct.SetTransitionTime(distances);
                solver.Add(ct);
                all_sequences.Add(ct.SequenceVar());
            }

            // Creates array of end_times of jobs.
            IntVarVector all_ends = new IntVarVector();
            foreach (KeyValuePair<int, List<Task>> job in data.all_tasks_)
            //for (int job_id = 0; job_id < job_count; ++job_id)
            {


                for (int task_index = 0; task_index < jobs_to_tasks[job.Key].Count; ++task_index)
                {

                    TaskAlternative task_alt = jobs_to_tasks[job.Key][task_index];
                    for (int alt = 0; alt < task_alt.intervals.Count; ++alt)
                    {
                        IntervalVar t = task_alt.intervals[alt];

                        all_ends.Add(solver.MakeProd(t.PerformedExpr().Var(), t.EndExpr().Var()).Var());
                        //all_ends.Add(t.EndExpr().Var());
                    }
                }
            }



            // Add dependencies between the tasks related to a job.
            foreach (KeyValuePair<int, List<Task>> job in data.all_tasks_)
            //    for (int job_id = 0; job_id < job_count; ++job_id)
            {
                List<Task> tasks = data.TasksOfJob(job.Key);
                for (int task_index = 0; task_index < tasks.Count; ++task_index)
                {
                    if (tasks[task_index].dependencies != null)
                    {
                        foreach (var dep in tasks[task_index].dependencies)
                        {
                            var other_jobid = dep.OtherTask.job_id;
                            var other_taskid = data.TasksOfJob(other_jobid).IndexOf(dep.OtherTask);

                            TaskAlternative task_alt = jobs_to_tasks[job.Key][task_index];
                            TaskAlternative task_alt_other_task = jobs_to_tasks[other_jobid][other_taskid];
                            foreach (var alt in task_alt.intervals)
                            {
                                foreach (var other_alt in task_alt_other_task.intervals)
                                {
                                    solver.Add(alt.StartsAfterStartWithDelay(other_alt, dep.Delay));
                                }

                            }


                        }

                    }



                }
            }


            // Objective: minimize the makespan (maximum end times of all tasks)
            // of the problem.
            IntVar objective_var = solver.MakeMax(all_ends).Var();
            OptimizeVar objective_monitor = solver.MakeMinimize(objective_var, 1);

            // ----- Search monitors and decision builder -----

            // This decision builder will assign all alternative variables.
            DecisionBuilder alternative_phase =
                 solver.MakePhase(all_alternative_variables, Solver.CHOOSE_MIN_SIZE_LOWEST_MIN,
                                  Solver.ASSIGN_MIN_VALUE);
            //original CHOOSE_MIN_SIZE

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

            JobshopAssessment objectiveAssessment = new JobshopAssessment(solver) { all_ends = all_ends };

            solver.NewSearch(main_phase, objective_monitor, search_log);
            //solver.NewSearch(main_phase, objectiveAssessment, search_log);

            while (solver.NextSolution())
            {
                //  foreach (KeyValuePair<int, IntervalVarVector> machine in machines_to_tasks)
                for (int m = 0; m < data.all_machines.Count; ++m)
                {
                    Console.WriteLine("Machine " + m + " :");

                    SequenceVar seq = all_sequences[m];

                    for (int taskIndex = 0; taskIndex < seq.Size(); taskIndex++)
                    {

                        IntervalVar task = seq.Interval(taskIndex);

                        if (task.PerformedExpr().Var().Value() == 1)
                        {
                            long startMin = task.StartMin();
                            long startMax = task.StartMax();
                            if (startMin == startMax)
                            {
                                Console.Write("Task " + task.Name() + " starts at " +
                                                startMin);
                            }
                            else
                            {
                                Console.Write("Task " + task.Name() + " starts between " +
                                                startMin + " and " + startMax);
                            }

                            long endMin = task.EndMin();
                            long endMax = task.EndMax();
                            if (endMin == endMax)
                            {
                                Console.WriteLine(", ends at " +
                                                endMin + ".");
                            }
                            else
                            {
                                Console.WriteLine(", ends between " +
                                                endMin + " and " + endMax + ".");
                            }

                            Task solTask = data.TasksOf(task.Name());
                            solTask.MinStart = startMin;
                            solTask.MaxStart = startMax;
                            solTask.MinEnd = endMin;
                            solTask.MaxEnd = endMax;
                            if (!finalSolution.ContainsKey(solTask.FinalMachineAssignement))
                                finalSolution[solTask.FinalMachineAssignement] = new List<Task>();
                            finalSolution[solTask.FinalMachineAssignement].Add(solTask);
                            Console.WriteLine("J:" + solTask.job_id + "T:" + solTask.TaskIndexForJob);
                        }
                        else
                        {
                            Console.WriteLine("Task " + task.Name() + " was will not be performed on this machine.");
                        }



                    }
                }
                Console.WriteLine("------------------------------------------------");

            }


            Console.ReadLine();






        }

        private static void AddDurationConstraints(FlexibleJobShopData data, Dictionary<int, IntervalVarVector> machines_to_tasks, int machine_id, int i, Task orTask, Solver s)
        {
            var t = machines_to_tasks[machine_id][i];
            var tmpSum = new IntVarVector();
            
            foreach (var b in data.MachinesBlockedIntervals[machine_id])
            {
                Constraint sc = s.MakeNotBetweenCt(t.StartExpr().Var(), b.Start, b.End-1);
                Constraint ec = s.MakeNotBetweenCt(t.EndExpr().Var(), b.Start+1, b.End);
                Constraint bc1 = s.MakeGreater(t.EndExpr().Var(), b.Start);
                Constraint bc2 = s.MakeLess(t.StartExpr().Var(), b.End);
                 tmpSum.Add ( ( bc1.Var () * bc2.Var () * (b.End - b.Start)).Var ());

                s.Add(sc);
                s.Add(ec);
            }
            Constraint task_duration = s.MakeEquality(t.DurationExpr().Var(), s.MakeSum(s.MakeSum (tmpSum), orTask.durations[orTask.machines.IndexOf(machine_id)]).Var ());
            s.Add(task_duration);

            //return new DurationDemon { task = machines_to_tasks[machine_id][i], machin = machine_id, BlockedIntervals = data.MachinesBlockedIntervals[machine_id], Duration = orTask.durations[orTask.machines.IndexOf(machine_id)] };
        }
    }

    public class JobshopAssessment : SearchMonitor
    {

        public JobshopAssessment(Solver s) : base(s)
        {

        }

        public IntVarVector all_ends;
        private long bestObjectiveValue = Int64.MaxValue;
        int slNo = 0;
        public override void EnterSearch()
        {
            base.EnterSearch();
            slNo = 0;
        }
        public override bool AcceptSolution()
        {
            var result = base.AcceptSolution();

            if (result)
            {
                long max = 0;// Int64.MinValue;
                             // Console.WriteLine("===========");
                var s = "";
                foreach (var item in all_ends)
                {
                    if (item.Bound())
                    {
                        // max = Math.Max(item.Value(), max);
                        max += item.Value();
                        s += item.Value() + ", ";
                        //Console.WriteLine(item.Value() + "  "+ max);
                    }
                    else
                    {
                        //Console.WriteLine(item.Max() + "  " + max);
                        max += item.Min();
                        s += item.Min() + ", ";
                    }
                }
                if (bestObjectiveValue >= max)
                {
                    bestObjectiveValue = max;

                    Console.WriteLine("acepted=================== # " + slNo + " Objective Value:" + bestObjectiveValue + " s= " + s);
                    slNo++;
                    return true;
                }

            }
            return false;
        }
        public override bool AtSolution()
        {

            Console.WriteLine("Solution # " + slNo + " Objective Value:" + bestObjectiveValue);
            return base.AtSolution();
        }

    }
}

