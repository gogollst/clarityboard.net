import {
  ResponsiveContainer,
  LineChart,
  Line,
} from 'recharts';
import { cn } from '@/lib/utils';

const TREND_COLORS = {
  up: '#10b981',    // green
  down: '#ef4444',  // red
  neutral: '#6b7280', // gray
};

interface SparkLineProps {
  data: number[];
  color?: string;
  trend?: 'up' | 'down' | 'neutral';
  className?: string;
}

export default function SparkLine({
  data,
  color,
  trend = 'neutral',
  className,
}: SparkLineProps) {
  const strokeColor = color ?? TREND_COLORS[trend];
  const chartData = data.map((value, i) => ({ i, v: value }));

  return (
    <div className={cn('h-8 w-20', className)}>
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={chartData}>
          <Line
            type="monotone"
            dataKey="v"
            stroke={strokeColor}
            strokeWidth={1.5}
            dot={false}
            isAnimationActive={false}
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
