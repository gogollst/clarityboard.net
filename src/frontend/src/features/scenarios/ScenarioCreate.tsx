import { useNavigate } from 'react-router-dom';
import { useForm, useFieldArray } from 'react-hook-form';
import { useEntity } from '@/hooks/useEntity';
import { useCreateScenario } from '@/hooks/useScenarios';
import { useKpiDefinitions } from '@/hooks/useKpis';
import type { ScenarioType, ScenarioParameter } from '@/types/scenario';
import PageHeader from '@/components/shared/PageHeader';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from '@/components/ui/select';
import { Plus, Trash2, Loader2 } from 'lucide-react';

interface FormValues {
  name: string;
  type: ScenarioType;
  parameters: {
    kpiId: string;
    adjustmentType: 'absolute' | 'percentage';
    adjustmentValue: string;
  }[];
}

export function Component() {
  const { selectedEntityId } = useEntity();
  const navigate = useNavigate();
  const createScenario = useCreateScenario();
  const { data: kpiDefinitions } = useKpiDefinitions();

  const {
    register,
    handleSubmit,
    control,
    setValue,
    watch,
    formState: { errors },
  } = useForm<FormValues>({
    defaultValues: {
      name: '',
      type: 'custom',
      parameters: [
        { kpiId: '', adjustmentType: 'percentage', adjustmentValue: '' },
      ],
    },
  });

  const { fields, append, remove } = useFieldArray({
    control,
    name: 'parameters',
  });

  const onSubmit = (values: FormValues) => {
    if (!selectedEntityId) return;

    const params: ScenarioParameter[] = values.parameters
      .filter((p) => p.kpiId && p.adjustmentValue)
      .map((p) => ({
        kpiId: p.kpiId,
        adjustmentType: p.adjustmentType,
        adjustmentValue: Number(p.adjustmentValue),
      }));

    createScenario.mutate(
      {
        entityId: selectedEntityId,
        name: values.name,
        type: values.type,
        parameters: params,
      },
      {
        onSuccess: (scenario) => {
          navigate(`/scenarios/${scenario.id}`);
        },
      },
    );
  };

  const watchedType = watch('type');

  return (
    <div>
      <PageHeader
        title="Create Scenario"
        description="Define parameters and run what-if analysis"
      />

      <form onSubmit={handleSubmit(onSubmit)}>
        <Card>
          <CardHeader>
            <CardTitle>Scenario Details</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <Label htmlFor="name">Name</Label>
              <Input
                id="name"
                {...register('name', { required: 'Name is required' })}
                placeholder="e.g. Q3 Revenue Increase"
              />
              {errors.name && (
                <p className="mt-1 text-sm text-red-500">
                  {errors.name.message}
                </p>
              )}
            </div>

            <div>
              <Label>Type</Label>
              <Select
                value={watchedType}
                onValueChange={(v) => setValue('type', v as ScenarioType)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="best_case">Best Case</SelectItem>
                  <SelectItem value="worst_case">Worst Case</SelectItem>
                  <SelectItem value="custom">Custom</SelectItem>
                  <SelectItem value="stress_test">Stress Test</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </CardContent>
        </Card>

        {/* Parameters */}
        <Card className="mt-6">
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle>Parameter Adjustments</CardTitle>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() =>
                  append({
                    kpiId: '',
                    adjustmentType: 'percentage',
                    adjustmentValue: '',
                  })
                }
              >
                <Plus className="mr-1 h-4 w-4" />
                Add Parameter
              </Button>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            {fields.map((field, index) => (
              <div
                key={field.id}
                className="flex flex-col gap-3 rounded-md border p-4 sm:flex-row sm:items-end"
              >
                <div className="flex-1">
                  <Label>KPI</Label>
                  <Select
                    value={watch(`parameters.${index}.kpiId`)}
                    onValueChange={(v) =>
                      setValue(`parameters.${index}.kpiId`, v)
                    }
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Select KPI" />
                    </SelectTrigger>
                    <SelectContent>
                      {kpiDefinitions?.map((kpi) => (
                        <SelectItem key={kpi.id} value={kpi.id}>
                          {kpi.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="w-[140px]">
                  <Label>Type</Label>
                  <Select
                    value={watch(`parameters.${index}.adjustmentType`)}
                    onValueChange={(v) =>
                      setValue(
                        `parameters.${index}.adjustmentType`,
                        v as 'absolute' | 'percentage',
                      )
                    }
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="percentage">Percentage</SelectItem>
                      <SelectItem value="absolute">Absolute</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className="w-[120px]">
                  <Label>Value</Label>
                  <Input
                    type="number"
                    {...register(`parameters.${index}.adjustmentValue`)}
                    placeholder="0"
                  />
                </div>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => remove(index)}
                  disabled={fields.length <= 1}
                >
                  <Trash2 className="h-4 w-4 text-muted-foreground" />
                </Button>
              </div>
            ))}
          </CardContent>
        </Card>

        {/* Submit */}
        <div className="mt-6 flex justify-end gap-2">
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate('/scenarios')}
          >
            Cancel
          </Button>
          <Button type="submit" disabled={createScenario.isPending}>
            {createScenario.isPending && (
              <Loader2 className="mr-1 h-4 w-4 animate-spin" />
            )}
            Create Scenario
          </Button>
        </div>
      </form>
    </div>
  );
}
