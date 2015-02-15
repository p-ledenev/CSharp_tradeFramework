using core.Model;

namespace core.AverageConstructors
{
    internal class TwoDepthAverageConstructor : AverageConstructor
    {
        public AverageConstructor constructor { get; set; }

        public TwoDepthAverageConstructor(AverageConstructor constructor)
        {
            this.constructor = constructor;
        }

        // Average(1/2k) - Average(k)
        public override double average(IValue[] values, int start, int depth)
        {
            double fast = constructor.average(values, start, depth/2);
            double slow = constructor.average(values, start, depth);

            return 2*fast - slow;
        }
    }
}