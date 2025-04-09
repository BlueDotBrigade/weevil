namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using BlueDotBrigade.Weevil.Statistics;

	internal sealed class StatisticalAnalyzerNew
    {
        private readonly IReadOnlyList<ICalculator> _calculators;

        public StatisticalAnalyzerNew()
        {
            _calculators = new List<ICalculator>
            {
                new CountCalculator(),
                new RangeCalculator(),
                new MinCalculator(),
                new MaxCalculator(),
                new MeanCalculator(),
                new MedianCalculator(),
                new TrimmedMeanCalculator(trimPercent: 0.1),
            };
        }

        public IDictionary<string, object> Analyze(IReadOnlyList<double> values, IReadOnlyList<DateTimeOffset> timestamps)
        {
            return _calculators
                .Select(c => c.Calculate(values, timestamps))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
}