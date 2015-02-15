using core.AverageConstructors;

namespace core.Factories
{
    internal class AverageConstructorFactory
    {
        public static AverageConstructor createConstructor()
        {
            return createSinjavinMovingAverageConstructor();
        }

        protected static AverageConstructor createMovingAverageConstructor()
        {
            return new MovingAverageConstructor();
        }

        protected static AverageConstructor createTwoDepthAverageConstructor()
        {
            return new TwoDepthAverageConstructor(createMovingAverageConstructor());
        }

        protected static AverageConstructor createSinjavinMovingAverageConstructor()
        {
            return new SinjavinMovingAverageConstructor();
        }
    }
}