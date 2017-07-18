using Google.OrTools.ConstraintSolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csjobshop_flexiblesched
{
    public class TaskAlternative
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

    }
}
