using Google.OrTools.ConstraintSolver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleJS
{

    public class StartBoundedDemon : NetDemon
    {
        public int machin;
        public IntervalVar task;
        public List<FixedInterval> BlockedIntervals;
        public long Duration { get; set; }
        public override void Run(Solver solver)
        {

            Debug.WriteLine(" start is bound " + task.StartMin() + " " + task.StartMax() + " " + task.DurationMin() + " " + task.DurationMax());
        }
    }
    public class DurationBoundedDemon : NetDemon
    {
        public int machin;
        public IntervalVar task;
        public List<FixedInterval> BlockedIntervals;
        public long Duration { get; set; }
        public override void Run(Solver solver)
        {

            Debug.WriteLine("duration bound " + task.StartMin() + " " + task.StartMax() + " " + task.DurationMin() + " " + task.DurationMax());
        }
    }
    public class DurationDemon : NetDemon
    {
        public int machin;
        public IntervalVar task;
        public List<FixedInterval> BlockedIntervals;
        public long Duration { get; set; }
        public override void Run(Solver solver)
        {
            base.Run(solver);
            long duration = Duration;
            foreach (FixedInterval fixedInt in BlockedIntervals)
            {
                if (task.StartExpr().Var().Value() >= fixedInt.Start && task.StartExpr().Var().Value() < fixedInt.End)
                    duration = -1;
                else if (task.StartExpr().Var().Value() <= fixedInt.Start && task.StartExpr().Var().Value() + duration + 3 > fixedInt.Start && duration != -1)
                {
                    duration = duration + fixedInt.End - fixedInt.Start;
                }

            }

            if (duration == -1)
            {
                task.SafeDurationExpr(-1);

            }
            else
                task.DurationExpr().Var().SetValue(duration);
            solver.Fail();


        }

    }

}
