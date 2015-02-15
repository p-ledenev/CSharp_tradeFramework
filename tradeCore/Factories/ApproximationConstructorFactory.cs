using core.ApproximationConstructors;

namespace core.Factories
{
    internal class ApproximationConstructorFactory
    {
        public static ApproximationConstructor createConstructor()
        {
            return new LinearApproximationConstructor();
            //return new WeightedGeometricalLinearApproximationConstructor(0.98);
        }
    }
}