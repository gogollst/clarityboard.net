import {
  ResponsiveContainer,
  BarChart as RechartsBarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
} from 'recharts';
import { cn } from '@/lib/utils';

const DEFAULT_COLORS = [
  '#3b82f6', // blue
  '#10b981', // green
  '#f59e0b', // amber
  '#ef4444', // red
  '#8b5cf6', // purple
];

interface BarChartProps {
  data: Record<string, unknown>[];
  categories: string[];
  index: string;
  colors?: string[];
  valueFormatter?: (value: number) => string;
  showLegend?: boolean;
  showGridLines?: boolean;
  stacked?: boolean;
  className?: string;
}

export default function BarChart({
  data,
  categories,
  index,
  colors = DEFAULT_COLORS,
  valueFormatter,
  showLegend = true,
  showGridLines = true,
  stacked = false,
  className,
}: BarChartProps) {
  return (
    <div className={cn('h-72 w-full', className)}>
      <ResponsiveContainer width="100%" height="100%">
        <RechartsBarChart
          data={data}
          margin={{ top: 8, right: 16, left: 0, bottom: 0 }}
        >
          {showGridLines && (
            <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
          )}
          <XAxis
            dataKey={index}
            tick={{ fontSize: 12 }}
            tickLine={false}
            axisLine={false}
            className="text-muted-foreground"
          />
          <YAxis
            tick={{ fontSize: 12 }}
            tickLine={false}
            axisLine={false}
            tickFormatter={valueFormatter}
            className="text-muted-foreground"
            width={60}
          />
          <Tooltip
            contentStyle={{
              backgroundColor: 'hsl(var(--popover))',
              borderColor: 'hsl(var(--border))',
              borderRadius: '0.375rem',
              fontSize: '0.875rem',
            }}
            formatter={(value: number | undefined) =>
              value != null && valueFormatter ? valueFormatter(value) : (value ?? 0)
            }
            cursor={{ fill: 'hsl(var(--accent))', opacity: 0.5 }}
          />
          {showLegend && (
            <Legend
              verticalAlign="top"
              height={36}
              iconType="rect"
              iconSize={10}
              wrapperStyle={{ fontSize: '0.75rem' }}
            />
          )}
          {categories.map((category, i) => (
            <Bar
              key={category}
              dataKey={category}
              fill={colors[i % colors.length]}
              radius={stacked ? [0, 0, 0, 0] : [4, 4, 0, 0]}
              stackId={stacked ? 'stack' : undefined}
              maxBarSize={48}
            />
          ))}
        </RechartsBarChart>
      </ResponsiveContainer>
    </div>
  );
}
