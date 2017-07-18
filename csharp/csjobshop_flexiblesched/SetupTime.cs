using Google.OrTools.ConstraintSolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csjobshop_flexiblesched
{
    class SetupTime : LongLongToLong
    {
        public SetupTime(int[] type, int[] setupTime)
        {
            type_ = type;
            setupTime_ = setupTime;
        }

        public override long Run(long first_index, long second_index)
        {
            if (type_[first_index] != type_[second_index])
                return setupTime_[second_index];
            else
                return 0;
        }

        private int[] type_;
        private int[] setupTime_;
    }
}
