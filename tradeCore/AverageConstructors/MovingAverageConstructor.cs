using core.Model;

namespace core.AverageConstructors
{
    internal class MovingAverageConstructor : AverageConstructor
    {
        public MovingAverageConstructor()
        {
        }

        public override double average(IValue[] values, int start, int depth)
        {
            double summ = 0;

            for (int i = depth; i > 0; i--)
                if (start - i + 1 >= 0)
                    summ += values[start - i + 1].getValue();

            return summ/depth;
        }
    }
}