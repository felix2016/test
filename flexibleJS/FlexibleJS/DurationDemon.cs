using Google.OrTools.ConstraintSolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexibleJS
{
    public class DurationDemon : NetDemon
    {
        public int machin;
        public IntervalVar task;
        public List<FixedInterval> BlockedIntervals;
        public override void Run(Solver solver)
        {
            base.Run(solver);
            long duration = 0;
            foreach (FixedInterval fixedInt in BlockedIntervals)
            {
                if (task.StartExpr().Var().Value() >= fixedInt.Start && task.StartExpr().Var().Value() < fixedInt.End)
                    duration = -1;
                else if (task.StartExpr().Var().Value() <= fixedInt.Start && task.EndExpr().Var().Value()> fixedInt.Start && duration != -1)
                {
                    duration = duration + task.DurationExpr().Var().Value();
                }

            }
            //task.StartExpr().Var().Value();
            if (duration != 0)
                task.SafeDurationExpr(duration);//.SetDurationMax(duration);

        }

    }
}
