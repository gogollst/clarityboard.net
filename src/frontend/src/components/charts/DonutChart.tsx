import {
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Tooltip,
  Legend,
  type PieLabelRenderProps,
} from 'recharts';
import { cn } from '@/lib/utils';

const DEFAULT_COLORS = [
  '#3b82f6', // blue
  '#10b981', // green
  '#f59e0b', // amber
  '#ef4444', // red
  '#8b5cf6', // purple
  '#06b6d4', // cyan
  '#f97316', // orange
  '#ec4899', // pink
];

interface DonutDataItem {
  name: string;
  value: number;
  color?: string;
}

interface DonutChartProps {
  data: DonutDataItem[];
  valueFormatter?: (value: number) => string;
  showLabel?: boolean;
  className?: string;
}

export default function DonutChart({
  data,
  valueFormatter,
  showLabel = false,
  className,
}: DonutChartProps) {
  return (
    <div className={cn('h-72 w-full', className)}>
      <ResponsiveContainer width="100%" height="100%">
        <PieChart>
          <Pie
            data={data}
            cx="50%"
            cy="50%"
            innerRadius="55%"
            outerRadius="80%"
            paddingAngle={2}
            dataKey="value"
            nameKey="name"
            label={
              showLabel
                ? (props: PieLabelRenderProps) =>
                    `${String(props.name ?? '')} ${(((props.percent as number | undefined) ?? 0) * 100).toFixed(0)}%`
                : undefined
            }
            labelLine={showLabel}
          >
            {data.map((entry, i) => (
              <Cell
                key={entry.name}
                fill={entry.color ?? DEFAULT_COLORS[i % DEFAULT_COLORS.length]}
                strokeWidth={0}
              />
            ))}
          </Pie>
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
          />
          <Legend
            verticalAlign="bottom"
            height={36}
            iconType="circle"
            iconSize={8}
            wrapperStyle={{ fontSize: '0.75rem' }}
          />
        </PieChart>
      </ResponsiveContainer>
    </div>
  );
}
