import { useTranslation } from 'react-i18next';
import { useEntity } from '@/hooks/useEntity';
import { useCashFlowForecast } from '@/hooks/useCashFlow';
import PageHeader from '@/components/shared/PageHeader';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from '@/components/ui/table';
import { Skeleton } from '@/components/ui/skeleton';
import AreaChart from '@/components/charts/AreaChart';
import { formatCurrency } from '@/lib/format';

export function Component() {
  const { t } = useTranslation('cashflow');
  const { selectedEntityId } = useEntity();
  const { data: forecast, isLoading } =
    useCashFlowForecast(selectedEntityId);

  const chartData =
    forecast?.weeks.map((w) => ({
      week: w.weekStart,
      [t('forecast.inflow')]: w.inflow,
      [t('forecast.outflow')]: w.outflow,
      [t('forecast.net')]: w.netFlow,
      [t('forecast.cumulative')]: w.cumulativeBalance,
    })) ?? [];

  const chartCategories = [
    t('forecast.inflow'),
    t('forecast.outflow'),
    t('forecast.net'),
    t('forecast.cumulative'),
  ];

  return (
    <div>
      <PageHeader
        title={t('forecast.title')}
        description={t('forecast.description')}
      />

      {/* Area Chart */}
      <Card>
        <CardHeader>
          <CardTitle>{t('forecast.weeklyForecastCard')}</CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Skeleton className="h-72 w-full" />
          ) : (
            <AreaChart
              data={chartData}
              categories={chartCategories}
              index="week"
              colors={['#10b981', '#ef4444', '#3b82f6', '#8b5cf6']}
              valueFormatter={(v) => formatCurrency(v)}
            />
          )}
        </CardContent>
      </Card>

      {/* Week-by-Week Table */}
      <Card className="mt-6">
        <CardHeader>
          <CardTitle>{t('forecast.breakdownCard')}</CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Skeleton className="h-64 w-full" />
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t('forecast.weekStart')}</TableHead>
                    <TableHead className="text-right">{t('forecast.inflow')}</TableHead>
                    <TableHead className="text-right">{t('forecast.outflow')}</TableHead>
                    <TableHead className="text-right">{t('forecast.net')}</TableHead>
                    <TableHead className="text-right">{t('forecast.cumulative')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {forecast?.weeks.map((week) => (
                    <TableRow key={week.weekStart}>
                      <TableCell className="font-medium">
                        {week.weekStart}
                      </TableCell>
                      <TableCell className="text-right">
                        {formatCurrency(week.inflow)}
                      </TableCell>
                      <TableCell className="text-right">
                        {formatCurrency(week.outflow)}
                      </TableCell>
                      <TableCell className="text-right">
                        <span
                          className={
                            week.netFlow >= 0
                              ? 'text-green-600'
                              : 'text-red-600'
                          }
                        >
                          {formatCurrency(week.netFlow)}
                        </span>
                      </TableCell>
                      <TableCell className="text-right">
                        {formatCurrency(week.cumulativeBalance)}
                      </TableCell>
                    </TableRow>
                  ))}

                  {/* Totals Row */}
                  {forecast && (
                    <TableRow className="border-t-2 font-bold">
                      <TableCell>{t('forecast.total')}</TableCell>
                      <TableCell className="text-right">
                        {formatCurrency(forecast.totalInflow)}
                      </TableCell>
                      <TableCell className="text-right">
                        {formatCurrency(forecast.totalOutflow)}
                      </TableCell>
                      <TableCell className="text-right">
                        <span
                          className={
                            forecast.totalInflow - forecast.totalOutflow >= 0
                              ? 'text-green-600'
                              : 'text-red-600'
                          }
                        >
                          {formatCurrency(
                            forecast.totalInflow - forecast.totalOutflow,
                          )}
                        </span>
                      </TableCell>
                      <TableCell className="text-right">
                        {forecast.weeks.length > 0
                          ? formatCurrency(
                              forecast.weeks[forecast.weeks.length - 1]
                                .cumulativeBalance,
                            )
                          : '-'}
                      </TableCell>
                    </TableRow>
                  )}

                  {(!forecast || forecast.weeks.length === 0) && (
                    <TableRow>
                      <TableCell
                        colSpan={5}
                        className="text-center text-muted-foreground"
                      >
                        {t('forecast.noData')}
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
