using core.Model;

namespace core.AverageConstructors
{
    internal abstract class AverageConstructor
    {
        public abstract double average(IValue[] values, int start, int depth);
    }
}