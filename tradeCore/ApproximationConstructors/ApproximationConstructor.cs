using System.Collections.Generic;
using core.Model;

namespace core.ApproximationConstructors
{
    internal abstract class ApproximationConstructor
    {
        public abstract Approximation approximate(IValue[] values, int start, int depth);
    }
}