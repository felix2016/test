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


            Solver solver = new Solver("flexible_jobshop");
            FlexibleJobShopData data = new FlexibleJobShopData();
            data.Load();
            int machine_count = data.machine_count;
            int job_count = data.job_count;
            int horizon = data.horizon;
            int[] setupTime = new int[3];
            int[] type = new int[3];
            //setupTime = new int[4] { 2, 5, 7, 7 };

            SetupTime.TaskTypeSetupTime = new Dictionary<int, long>();
            SetupTime.TaskTypeSetupTime.Add(0, 5);
            SetupTime.TaskTypeSetupTime.Add(1, 2);

            SetupTime.TaskIntervalToTaskType = new Dictionary<IntervalVar, int>();
            
            Console.WriteLine(data.DebugString());

            Dictionary<int, List<TaskAlternative>> jobs_to_tasks = new Dictionary<int, List<TaskAlternative>>();
            for (int i = 0; i < job_count; i++)
            {
                jobs_to_tasks[i] = new List<TaskAlternative>();
            }
            // machines_to_tasks stores the same interval variables as above, but
            // grouped my machines instead of grouped by jobs.
            Dictionary<int, IntervalVarVector> machines_to_tasks = new Dictionary<int, IntervalVarVector>();
            for (int i = 0; i < machine_count; i++)
            {
                machines_to_tasks[i] = new IntervalVarVector();
            }

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
                        string name = string.Format("J{0}I{1}A{2}M{3}D{4}T{5}",
                                                        task.job_id,
                                                        task_index,
                                                        alt,
                                                        machine_id,
                                                        duration,
                                                        task.type );
                        IntervalVar interval = solver.MakeFixedDurationIntervalVar(
                             0, horizon, duration, optional, name);
                        jobs_to_tasks[job_id][jobs_to_tasks[job_id].Count - 1].intervals.Add(interval);
                        machines_to_tasks[machine_id].Add(interval);

                        SetupTime.TaskIntervalToTaskType.Add(interval, task.type);

                        if (optional)
                        {
                            active_variables.Add(interval.PerformedExpr().Var());
                        }
                    }
                    string alternative_name = string.Format("J{0}I{1}", job_id, task_index);
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
            List<SetupTime> all_distances= new List<SetupTime> ();

            DisjunctiveConstraint ct;
            for (int machine_id = 0; machine_id < machine_count; ++machine_id)
            {
                string name = string.Format("Machine_{0}", machine_id);
                 ct =   solver.MakeDisjunctiveConstraint(machines_to_tasks[machine_id], name);


                 SetupTime distances = new SetupTime(machines_to_tasks[machine_id]);
                 all_distances.Add(distances);

                ct.SetTransitionTime(distances);
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

            JobshopAssessment objectiveAssessment = new JobshopAssessment(solver) { all_ends = all_ends };

            //solver.NewSearch(main_phase, objective_monitor, search_log, limit);
            solver.NewSearch(main_phase, objectiveAssessment, search_log, limit);
            
            while (solver.NextSolution())
            {
                for (int m = 0; m < machine_count; ++m)
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
    }

    public class JobshopAssessment : SearchMonitor
    {
        
        public JobshopAssessment(Solver s): base(s)
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
            var max = Int64.MinValue;
           // Console.WriteLine("===========");
            foreach (var item in all_ends)
            {
                if (item.Bound())
                {
                    max = Math.Max(item.Value(), max);
                  //  Console.WriteLine(item.Value() + "  "+ max);
                }
                else
                {
                    //Console.WriteLine(item.Max() + "  " + max);
                    max = Math.Max(item.Max(), max);
                }
            }
            if (bestObjectiveValue >= max)
            {
                bestObjectiveValue = max;

               Console.WriteLine("acepted=================== # " + slNo + " Objective Value:" + bestObjectiveValue);
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

